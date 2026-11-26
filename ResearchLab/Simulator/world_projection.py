"""Version-pinned whole-world *projection*, not an engine or complete save loader.
Unknown references/unsupported types are preserved explicitly; source files read only.
"""
import argparse,collections,hashlib,json,math
from pathlib import Path
from campaign_import import Graph,totals
from typed_save import field,dictionary,signed_hash
from compare_native import read_saves,SAVE_HASHES
ENTITY={330048:"heroes",330032:"clans",330053:"kingdoms",330074:"parties",330095:"settlements",330103:"fortifications",330112:"villages"}
def pair_key(a,b):
    return tuple(sorted((a,b)))
def project(path,sha,meta):
    g=Graph(path,sha)
    nodes={k:{} for k in ENTITY.values()};local={};issues=collections.Counter()
    # All selected typed instances, not only active instances. Object IDs are local.
    for i,t in g.types.items():
        if t in ENTITY:
            name=g.sid(i,t);key=f"{t}:{i}"
            local[i]=key;nodes[ENTITY[t]][key]={"id":name,"type":t}
    def link(i,allowed):
        if i==-1:return None
        t=g.types.get(i)
        if t not in allowed:raise ValueError(f"reference type mismatch {i} {t} {allowed}")
        if i not in local:raise ValueError("unprojected reference")
        return local[i]
    def get(i,t,e,l,n,k):return g.get(i,t,e,l,n,k)
    def refs(i,t,e,l,n,allowed):return link(get(i,t,e,l,n,0),allowed)
    def children(i):
        a=get(i,330048,2,3,740,1)
        if a==-1:return None
        f,e,r=g.container(a,("container",5,(330048,)))
        out=[]
        for j in range(g.cn[a]):
            k,_,_,v=field(e[r,j,5])
            if k!=0:raise ValueError("child entry type")
            out.append(link(v,(330048,)))
        return out
    for i,key in local.items():
        t=g.types[i];r=nodes[ENTITY[t]][key]
        if t==330048:
            for n,label,allowed in ((500,"clan",(330032,)),(380,"companion_of",(330032,)),(700,"father",(330048,)),(710,"mother",(330048,)),(730,"spouse",(330048,)),(560,"party",(330074,)),(580,"settlement",(330095,))):
                r[label]=refs(i,t,2,3,n,allowed)
            r.update(guid=get(i,t,3,2,2,6),state=get(i,t,2,3,260,5),gold=get(i,t,2,3,650,6),
                     health=get(i,t,2,3,430,6),female=get(i,t,3,3,200,6),children=children(i))
        elif t==330032:
            r.update(kingdom=refs(i,t,2,3,57,(330053,)),leader=refs(i,t,2,3,83,(330048,)),renown=get(i,t,3,3,88,6))
        elif t==330053:
            r.update(ruling_clan=refs(i,t,2,3,18,(330032,)),budget=get(i,t,2,3,81,6))
        elif t==330074:
            p=get(i,t,3,4,1020,0)
            r.update(clan=refs(i,t,2,4,1120,(330032,)),settlement=refs(i,t,2,4,1001,(330095,)),trade_gold=get(i,t,2,4,1956,6))
            try:r["roster"]=g.roster(get(p,330077,3,2,3,0));r["personnel"]=totals(r["roster"])
            except (ValueError,KeyError) as ex:
                # Only optional detailed roster; never fabricate a zero count.
                r["roster"]=None;r["personnel"]=None;r["roster_error"]=str(ex);issues["party_roster_unresolved"]+=1
        elif t==330095:
            c=get(i,t,3,3,152,0)
            if c==-1:r["component"]=None
            elif c in local:r["component"]=local[c]
            else:r["component"]={"unsupported_type":g.types[c],"object_id":c};issues["unsupported_settlement_component"]+=1
        elif t==330103:
            r.update(owner=refs(i,t,2,5,1040,(330032,)),governor=refs(i,t,2,5,1012,(330048,)),
                prosperity=get(i,t,2,5,1017,6),security=get(i,t,2,5,1015,6),loyalty=get(i,t,2,5,1014,6),
                gold=get(i,t,3,3,50,6),food=get(i,t,3,4,100,6),reserve=get(i,t,2,5,1008,6))
        elif t==330112:
            r.update(bound=refs(i,t,2,4,106,(330095,)),hearth=get(i,t,3,4,108,6),gold=get(i,t,3,3,50,6))
    stances=[];seen=set()
    for i,t in g.types.items():
        if t!=330188:continue
        a=refs(i,t,3,2,20,(330032,330053));b=refs(i,t,3,2,30,(330032,330053))
        if a is None or b is None:raise ValueError("null faction")
        p=pair_key(a,b)
        if p in seen:issues["duplicate_stance_pair"]+=1
        seen.add(p)
        stances.append({"faction1":a,"faction2":b,"stance":get(i,t,2,2,0,5),"daily_tribute_1_to_2":get(i,t,2,2,220,6)})
    # All raw stored personal relation entries, including unknown GUIDs explicitly.
    guid={}
    for key,r in nodes["heroes"].items():
        h=signed_hash(r["guid"])
        if h in guid:raise ValueError("duplicate hero GUID")
        guid[h]=key
    managers=[i for i,t in g.types.items() if t==330031]
    if len(managers)!=1:raise ValueError("relation manager count")
    rel=get(managers[0],330031,2,2,1,0);cid=get(rel,330186,2,2,1,1)
    inner=("container",2,(30009,30001));outer=("container",2,(30009,inner))
    relations=[]
    for hi,(kind,ic) in dictionary(cid,outer,g.ct,g.cs).items():
        if kind!=1:raise ValueError("inner relation container")
        for lo,(kind,value) in dictionary(ic,inner,g.ct,g.cs).items():
            if kind!=6 or not -100<=value<=100:raise ValueError("relation value")
            a=guid.get(hi);b=guid.get(lo)
            if a is None or b is None:issues["unresolved_relation_endpoint"]+=1
            relations.append({"a":a,"b":b,"hash_a":hi,"hash_b":lo,"value":value})
    heroes=nodes["heroes"]
    for key,r in heroes.items():
        for p in ("father","mother"):
            if r[p] is not None and key not in (heroes[r[p]]["children"] or []):issues["parent_child_not_reciprocal"]+=1
        if r["spouse"] is not None and heroes[r["spouse"]]["spouse"]!=key:issues["spouse_not_reciprocal"]+=1
    pp=[r for r in nodes["parties"].values() if r["id"]=="player_party"]
    if len(pp)!=1 or pp[0]["personnel"]!={k:meta[k] for k in ("healthy","wounded")}:raise ValueError("player roster mismatch")
    g.finish()
    return {"version":1,"source_sha":sha,"scope":"selected typed native records, snapshot-local keys; no engine execution",
      "nodes":nodes,"stances":stances,"relations":relations,"observed_issues":dict(issues)}
def summary(w):
    n=w["nodes"];return {"counts":{k:len(v) for k,v in n.items()},"stances":dict(collections.Counter(r["stance"] for r in w["stances"])),
      "relations":len(w["relations"]),"resolved_party_rosters":sum(r["roster"] is not None for r in n["parties"].values()),
      "issues":w["observed_issues"],"hero_states":dict(collections.Counter(r["state"] for r in n["heroes"].values()))}
def main():
    p=argparse.ArgumentParser();p.add_argument("save_dir",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    records=read_saves(a.save_dir)
    for name,h,m in zip(("LABD001BASE","LABD001ACTIVE","LABD001RETURN"),SAVE_HASHES,records):
        w=project(a.save_dir/(name+".sav"),h,m);dest=a.out/(name+".json")
        with dest.open("x",encoding="utf8") as f:json.dump(w,f,allow_nan=False,sort_keys=True)
        print(name,json.dumps(summary(w),sort_keys=True),"SHA",hashlib.sha256(dest.read_bytes()).hexdigest(),flush=True)
if __name__=="__main__":main()
