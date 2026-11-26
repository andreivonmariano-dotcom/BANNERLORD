"""Read-only D001 campaign import for bounded research, not native deserialization.
Only pinned saves. All object/container IDs are snapshot-local. Unknown values
are never substituted with synthetic defaults. No game object construction.
"""
import argparse,hashlib,json,math,struct,zlib
from pathlib import Path
from typed_save import Reader,archive,save_id,field,inspect
from compare_native import read_saves,SAVE_HASHES

class Graph:
    def __init__(self,path,sha):
        self.path=path;self.sha=sha
        data=path.read_bytes()
        if hashlib.sha256(data).hexdigest()!=sha:raise ValueError("wrong save")
        size=struct.unpack_from("<i",data)[0]
        d=zlib.decompressobj(-15);payload=d.decompress(data[4+size:],128*1024*1024+1)
        if not d.eof or d.unused_data or d.unconsumed_tail or len(payload)>128*1024*1024:raise ValueError("bad payload")
        r=Reader(payload);fs,es=archive(r.block());self.types={};self.ct={};self.cn={}
        h=Reader(es[-1,-1,7]);oc,sc,cc=h.count(),h.count(),h.count();h.end()
        for g,(p,l,k) in fs.items():
            if p==-1 and k in (1,3):
                h=Reader(es[g,-1,8 if k==1 else 9]);tid=save_id(h)
                target=self.types if k==1 else self.ct
                if l in target:raise ValueError("duplicate ID")
                target[l]=tid
                if k==3:
                    kind=h.b();self.cn[l]=h.count();h.end()
                else:h.s();h.s();h.end()
        if r.count()!=oc or set(self.types)!=set(range(oc)):raise ValueError("object count")
        self.bs=[r.block() for _ in range(oc)]
        if r.count()!=cc or set(self.ct)!=set(range(cc)):raise ValueError("container count")
        self.cs=[r.block() for _ in range(cc)]
        fs,es=archive(r.block());r.end();self.strings={}
        for (g,i,k),v in es.items():
            if k==10:
                h=Reader(v);n=h.i();self.strings[i]=bytes(h.take(n)).decode() if n>=0 else None;h.end()
        if len(self.strings)!=sc:raise ValueError("strings count")
    def finish(self):
        if hashlib.sha256(self.path.read_bytes()).hexdigest()!=self.sha:raise ValueError("save changed")
    def obj(self,i,t):
        if self.types.get(i)!=t:raise ValueError(f"object type mismatch {i}: {self.types.get(i)} expected {t}")
        f,e=archive(self.bs[i]);roots=[g for g,(p,l,k) in f.items() if p==-1 and l==i and k==1]
        if len(roots)!=1:raise ValueError("object root")
        return f,e,roots[0]
    def value(self,e,g,ext,level,local,kind):
        vals=[]
        for (p,j,k),v in e.items():
            if p!=g or k!=ext:continue
            h=Reader(v);a,b,c=h.b(),h.b(),h.s()
            if (b,c)==(level,local):
                if a!=kind:raise ValueError("member kind")
                a,b,c,val=field(v);vals.append(val)
        if len(vals)!=1 or vals[0] is None:raise ValueError(f"missing/opaque member {g}/{ext}/{level}/{local}")
        return vals[0]
    def get(self,i,t,ext,level,local,kind):
        f,e,g=self.obj(i,t);return self.value(e,g,ext,level,local,kind)
    def sid(self,i,t):
        if i==-1:return None
        return self.strings[self.get(i,t,3,2,1,2)]
    def select(self,t,name):
        ids=[i for i,x in self.types.items() if x==t and self.sid(i,t)==name]
        if len(ids)!=1:raise ValueError("ambiguous identity "+name)
        return ids[0]
    def container(self,i,t):
        if self.ct.get(i)!=t:raise ValueError("wrong container type")
        f,e=archive(self.cs[i]);roots=[g for g,(p,l,k) in f.items() if p==-1 and l==i and k==3]
        if len(roots)!=1:raise ValueError("container root")
        return f,e,roots[0]
    def element(self,f,e,g,index,t):
        raw=e[g,index,5];kind,level,local,v=field(raw)
        if kind!=4:raise ValueError("not struct element")
        if v==-1:return None
        folders=[j for j,(p,l,k) in f.items() if p==g and l==v and k==2]
        if not folders:return None
        if len(folders)!=1:raise ValueError("struct folder ambiguity")
        j=folders[0];h=Reader(e[j,-1,8])
        if save_id(h)!=t:raise ValueError("struct type mismatch")
        h.s();h.s();h.end()
        return j
    def equip_struct(self,e,g):
        item=self.value(e,g,3,1,1,0);mod=self.value(e,g,3,1,2,0)
        return {"item":self.sid(item,10032),"modifier":self.sid(mod,10030),"quest_item":self.value(e,g,3,1,3,6)}
    def equipment(self,i):
        if i==-1:return None
        a=self.get(i,10016,2,2,2,1);f,e,g=self.container(a,("container",3,(11011,)))
        rows=[]
        for index in range(self.cn[a]):
            j=self.element(f,e,g,index,11011)
            rows.append(None if j is None else self.equip_struct(e,j))
        return rows
    def roster(self,i):
        a=self.get(i,330093,2,2,0,1);n=self.get(i,330093,2,2,1,6);f,e,g=self.container(a,("container",3,(331005,)))
        if not 0<=n<=self.cn[a]:raise ValueError("roster count")
        rows=[]
        for index in range(n):
            j=self.element(f,e,g,index,331005)
            if j is None:raise ValueError("missing occupied troop")
            c=self.value(e,j,2,1,4,0);num=self.value(e,j,2,1,0,6);w=self.value(e,j,2,1,1,6);xp=self.value(e,j,2,1,2,6)
            if not 0<=w<=num or num<=0 or xp<0:raise ValueError("invalid roster entry")
            hero=self.get(c,330030,2,4,101,0)
            rows.append({"character":self.sid(c,330030),"hero":self.sid(hero,330048),"count":num,"wounded":w,"xp":xp})
        return rows
    def inventory(self,i):
        a=self.get(i,330091,2,2,0,1);n=self.get(i,330091,2,2,1,6);f,e,g=self.container(a,("container",3,(11004,)))
        if not 0<=n<=self.cn[a]:raise ValueError("inventory count")
        rows=[]
        for index in range(n):
            j=self.element(f,e,g,index,11004)
            if j is None:raise ValueError("missing occupied item")
            amount=self.value(e,j,2,1,11,6);v=self.value(e,j,3,1,21,4)
            kids=[k for k,(p,l,t) in f.items() if p==j and l==v and t==2]
            if len(kids)!=1 or amount<=0:raise ValueError("nested equipment")
            h=Reader(e[kids[0],-1,8])
            if save_id(h)!=11011:raise ValueError("nested type")
            rows.append(dict(self.equip_struct(e,kids[0]),amount=amount))
        return rows
    def skills(self,i):
        t=("generic",10040,(10039,));a=self.get(i,t,2,2,10,1);f,e,g=self.container(a,("container",2,(10039,30001)))
        out={}
        for j in range(self.cn[a]):
            k,_,_,key=field(e[g,j,4]);v,_,_,value=field(e[g,j,5])
            name=self.sid(key,10039)
            if k!=0 or v!=6 or name in out or type(value) is not int or value<0:raise ValueError("skill entry")
            out[name]=value
        return out
    def hero(self,name):
        i=self.select(330048,name)
        return {"id":name,"gold":self.get(i,330048,2,3,650,6),"health":self.get(i,330048,2,3,430,6),
          "state":self.get(i,330048,2,3,260,5),"party":self.sid(self.get(i,330048,2,3,560,0),330074),
          "clan":self.sid(self.get(i,330048,2,3,500,0),330032),"companion_of":self.sid(self.get(i,330048,2,3,380,0),330032),
          "skills":self.skills(self.get(i,330048,2,3,290,0)),
          "battle_equipment":self.equipment(self.get(i,330048,3,3,210,0)),
          "civilian_equipment":self.equipment(self.get(i,330048,3,3,220,0))}
    def town(self,village):
        i=self.select(330095,village);v=self.get(i,330095,3,3,152,0)
        b=self.get(v,330112,2,4,106,0);t=self.get(b,330095,3,3,152,0)
        return {"id":self.sid(b,330095),"component":self.sid(t,330103),"owner":self.sid(self.get(t,330103,2,5,1040,0),330032),
          "security":self.get(t,330103,2,5,1015,6),"prosperity":self.get(t,330103,2,5,1017,6),
          "gold":self.get(t,330103,3,3,50,6),"food":self.get(t,330103,3,4,100,6),"reserve":self.get(t,330103,2,5,1008,6)}
def totals(rows):
    return {"healthy":sum(r["count"]-r["wounded"] for r in rows),"wounded":sum(r["wounded"] for r in rows)}
def project(path,sha,meta):
    old=inspect(path,sha);g=Graph(path,sha)
    party=g.select(330074,"player_party");p=g.get(party,330074,3,4,1020,0)
    roster=g.roster(g.get(p,330077,3,2,3,0));inventory=g.inventory(g.get(p,330077,3,2,5,0))
    people={h:g.hero(h) for h in ("main_hero","CharacterObject_1864","CharacterObject_1883")}
    if totals(roster)!={k:meta[k] for k in ("healthy","wounded")} or people["main_hero"]["gold"]!=meta["gold"]:raise ValueError("metadata disagreement")
    issues=[i for i,t in g.types.items() if t==590001 and g.sid(i,t)=="issue_177"]
    if len(issues)>1:raise ValueError("issue ambiguity")
    issue=None
    if issues:
        i=issues[0];issue={"id":"issue_177","owner":g.sid(g.get(i,590001,2,3,13,0),330048),
          "state":g.get(i,590001,2,3,11,5),"risk":g.get(i,590001,2,3,34,6),"cached_loss":g.get(i,590001,2,3,33,6),
          "difficulty":g.get(i,590001,2,3,26,6),"effects_resolved":g.get(i,590001,2,3,32,6),"sent":g.roster(g.get(i,590001,2,3,30,0))}
    town=g.town("castle_village_N2_1");g.finish()
    return {"version":1,"source_sha":sha,"scope":"bounded D001 native state projection; no live engine or scheduler",
      "party":"player_party","roster":roster,"inventory":inventory,"people":people,"gold":meta["gold"],"food":meta["food"],
      "relation":old["relation"]["raw_personal_relation"],"town":town,"issue":issue}
def main():
    p=argparse.ArgumentParser();p.add_argument("save_dir",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    records=read_saves(a.save_dir)
    for n,h,m in zip(("LABD001BASE","LABD001ACTIVE","LABD001RETURN"),SAVE_HASHES,records):
        x=project(a.save_dir/(n+".sav"),h,m)
        with (a.out/(n+".json")).open("x",encoding="utf-8") as f:json.dump(x,f,sort_keys=True,allow_nan=False)
        print(n,json.dumps({"roster":x["roster"],"inventory_entries":len(x["inventory"]),"inventory_total":sum(i["amount"] for i in x["inventory"]),
          "skills":{k:v["skills"] for k,v in x["people"].items()},"equipment_slots":{k:len(v["battle_equipment"]) for k,v in x["people"].items()},
          "town":x["town"],"issue":x["issue"],"relation":x["relation"]},sort_keys=True))
if __name__=="__main__":main()
