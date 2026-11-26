"""Offline account-level reconciliation. No inferred flows from snapshot deltas."""
import argparse, collections, hashlib, json, pathlib, unittest
KINDS=("hero_gold","party_trade_gold","fort_gold","construction_reserve","village_gold","kingdom_budget")
def require(ok,msg):
    if not ok: raise ValueError(msg)
def accounts(w):
    n=w["nodes"]; result={}; duplicates=set()
    def put(kind,r,value):
        ident=r.get("id")
        require(isinstance(ident,str) and ident and type(value) is int,"account schema")
        k=(kind,ident)
        if k in result or k in duplicates:
            result.pop(k,None);duplicates.add(k)
        else: result[k]=value
    for group,field,kind in (("heroes","gold","hero_gold"),("parties","trade_gold","party_trade_gold"),("kingdoms","budget","kingdom_budget")):
        for r in n[group].values(): put(kind,r,r[field])
    for r in n["settlements"].values():
        c=r["component"]
        if not isinstance(c,str): continue
        if c in n["fortifications"]:
            f=n["fortifications"][c];put("fort_gold",r,f["gold"]);put("construction_reserve",r,f["reserve"])
        if c in n["villages"]: put("village_gold",r,n["villages"][c]["gold"])
    return result,duplicates
def reconcile(before,after,receipts,start,end):
    require(type(start) is int and type(end) is int and start<end,"clock")
    common=before.keys()&after.keys(); known=collections.Counter(); seen=set()
    for r in receipts:
        require(set(r)=={"id","tick","source","destination","amount","evidence"},"receipt schema")
        require(isinstance(r["id"],str) and r["id"] and r["id"] not in seen,"duplicate receipt")
        require(type(r["tick"]) is int and start<r["tick"]<=end,"receipt outside interval")
        require(type(r["amount"]) is int and r["amount"]>0,"receipt amount")
        require(isinstance(r["evidence"],str) and r["evidence"].strip(),"evidence required")
        a,b=(tuple(r[k]) if r[k] is not None else None for k in ("source","destination"))
        require(a!=b and (a is None or a in common) and (b is None or b in common),"unknown or identical endpoint")
        seen.add(r["id"])
        if a is not None: known[a]-=r["amount"]
        if b is not None: known[b]+=r["amount"]
    rows={}
    for k in sorted(common):
        delta=after[k]-before[k]
        rows[k]={"observed_delta":delta,"attributed_delta":known[k],"residual":delta-known[k]}
    return rows
def summary(before,after,rows,dup0,dup1):
    out={}
    for k in KINDS:
        rr=[v for key,v in rows.items() if key[0]==k]
        out[k]={"matched":len(rr),"changed":sum(v["observed_delta"]!=0 for v in rr),"net_observed_delta":sum(v["observed_delta"] for v in rr),"net_attributed_delta":sum(v["attributed_delta"] for v in rr),"absolute_unexplained":sum(abs(v["residual"]) for v in rr),"nonzero_residuals":sum(v["residual"]!=0 for v in rr),"base_only":sum(x[0]==k for x in before.keys()-after.keys()),"end_only":sum(x[0]==k for x in after.keys()-before.keys()),"base_duplicate_ids":sum(x[0]==k for x in dup0),"end_duplicate_ids":sum(x[0]==k for x in dup1)}
    return out
def pinned(p,h):
    b=p.read_bytes();require(hashlib.sha256(b).hexdigest()==h,"input hash changed");return json.loads(b)
class Tests(unittest.TestCase):
    def setUp(self):
        self.a=("hero_gold","a");self.b=("fort_gold","b");self.x={self.a:100,self.b:100};self.y={self.a:70,self.b:125}
        self.r={"id":"e1","tick":2,"source":list(self.a),"destination":list(self.b),"amount":25,"evidence":"synthetic"}
    def test_unexplained(self):
        z=reconcile(self.x,self.y,[],1,3);self.assertEqual(z[self.a]["residual"],-30)
    def test_transfer(self):
        z=reconcile(self.x,self.y,[self.r],1,3);self.assertEqual(z[self.a]["residual"],-5);self.assertEqual(z[self.b]["residual"],0)
    def test_duplicate(self):
        with self.assertRaises(ValueError):reconcile(self.x,self.y,[self.r,self.r],1,3)
    def test_outside(self):
        with self.assertRaises(ValueError):reconcile(self.x,self.y,[dict(self.r,tick=1)],1,3)
    def test_unknown(self):
        with self.assertRaises(ValueError):reconcile(self.x,self.y,[dict(self.r,destination=["hero_gold","unknown"])],1,3)
    def test_invalid(self):
        for amt in (0,-1,1.5,True):
            with self.assertRaises(ValueError):reconcile(self.x,self.y,[dict(self.r,amount=amt)],1,3)
    def test_missing_evidence(self):
        with self.assertRaises(ValueError):reconcile(self.x,self.y,[dict(self.r,evidence="")],1,3)
    def test_external(self):
        z=reconcile(self.x,self.y,[dict(self.r,source=None)],1,3);self.assertEqual(z[self.b]["residual"],0)
    def test_unmatched(self):
        z=reconcile(self.x,{self.a:99},[],1,3);self.assertNotIn(self.b,z)
    def test_duplicate_account(self):
        w={"nodes":{"heroes":{"a":{"id":"a","gold":1},"b":{"id":"a","gold":2}},"parties":{},"kingdoms":{},"settlements":{},"fortifications":{},"villages":{}}}
        a,d=accounts(w);self.assertEqual(a,{});self.assertEqual(d,{self.a})
def main():
    p=argparse.ArgumentParser();p.add_argument("base",type=pathlib.Path);p.add_argument("end",type=pathlib.Path);p.add_argument("report",type=pathlib.Path);a=p.parse_args()
    w0=pinned(a.base,"fa4da7be84a84e9ade3e57549df01e83e57921eaf7617b323ec4b23fa9ad1f60")
    w1=pinned(a.end,"e755134a0d388e98188bc45a650e0782b5bc1e8981ef700d6b1f35bb3a629d1b")
    x,d0=accounts(w0);y,d1=accounts(w1);rows=reconcile(x,y,[],78690902361252,78696616529799)
    result={"scope":"D001 selected endpoint money accounts only; no transaction receipts available; do not sum across account types or infer causes","receipts":0,"summary":summary(x,y,rows,d0,d1)}
    with a.report.open("x",encoding="utf8") as f:json.dump(result,f,indent=2,sort_keys=True)
    print(json.dumps(result));print("REPORT_SHA256",hashlib.sha256(a.report.read_bytes()).hexdigest())
if __name__=="__main__":main()
