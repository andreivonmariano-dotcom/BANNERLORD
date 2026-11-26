"""Offline L003.1 role observation checks; not causal recovery estimates."""
import unittest
from urllib.parse import unquote
ROLES=("Surgeon","Quartermaster","Scout","Engineer","FirstMate","Navigator")
def parse(line):
    d={}
    for token in line.split():
        k,sep,v=token.partition("=")
        if not sep or not k or k in d:raise ValueError("invalid or duplicate key")
        d[k]=unquote(v)
    if d.get("kind")!="party":raise ValueError("not a party observation")
    for k in ("seq","tick","healthy","wounded","hero_hp","medicine"):
        d[k]=int(d[k])
    if min(d["seq"],d["tick"],d["healthy"],d["wounded"])<0 or d["seq"]==0:raise ValueError("invalid counts")
    if not d.get("id") or any(k not in d for role in ROLES for k in ("raw_"+role,"effective_"+role)):raise ValueError("missing identity or roles")
    return d
def assess(before,after,actor):
    if not actor or actor=="none":raise ValueError("actor")
    if before["id"]!=after["id"] or before["seq"]>=after["seq"] or before["tick"]>after["tick"]:raise ValueError("identity/order")
    unchanged=all(before[p+r]==after[p+r] for r in ROLES if r!="Surgeon" for p in ("raw_","effective_"))
    confirmed=before["raw_Surgeon"]=="none" and after["raw_Surgeon"]==actor and after["effective_Surgeon"]==actor and unchanged
    return {"assignment_observed":confirmed,"other_roles_unchanged":unchanged,"wounded_delta":after["wounded"]-before["wounded"],"healthy_delta":after["healthy"]-before["healthy"],"medicine_before":before["medicine"],"medicine_after":after["medicine"],"causal_recovery":"UNESTABLISHED"}
class Tests(unittest.TestCase):
    def fixture(self,seq,tick,raw="none",eff="leader",wounded=5,other="none"):
        s=f"seq={seq} tick={tick} kind=party id=p healthy=10 wounded={wounded} hero_hp=80 medicine=50"
        for role in ROLES:s+=" raw_"+role+"="+(raw if role=="Surgeon" else other if role=="Navigator" else "none")+" effective_"+role+"="+(eff if role=="Surgeon" else "leader")
        return parse(s)
    def test_confirm(self):
        r=assess(self.fixture(1,10),self.fixture(2,10,"a","a"),"a");self.assertTrue(r["assignment_observed"])
    def test_effective_mismatch(self):
        self.assertFalse(assess(self.fixture(1,10),self.fixture(2,10,"a","leader"),"a")["assignment_observed"])
    def test_displaced(self):
        self.assertFalse(assess(self.fixture(1,10),self.fixture(2,10,"a","a",other="b"),"a")["assignment_observed"])
    def test_incumbent(self):
        self.assertFalse(assess(self.fixture(1,10,"b","b"),self.fixture(2,10,"a","a"),"a")["assignment_observed"])
    def test_order(self):
        with self.assertRaises(ValueError):assess(self.fixture(2,10),self.fixture(1,10),"a")
    def test_no_causality(self):
        r=assess(self.fixture(1,10),self.fixture(2,20,"a","a",2),"a");self.assertEqual(r["wounded_delta"],-3);self.assertEqual(r["causal_recovery"],"UNESTABLISHED")
    def test_bad_input(self):
        with self.assertRaises(ValueError):parse("kind=party seq=1 seq=2")
if __name__=="__main__":unittest.main()
