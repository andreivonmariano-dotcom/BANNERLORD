"""Extract a compact, private, pinned campaign fixture; no game writes."""
import argparse,json,hashlib
from pathlib import Path
from geography import pinned_json,digest,DB_HASH
from build_dashboard import HASHES
from campaign_import import Graph
from compare_native import SAVE_HASHES,read_saves
from source_world import source_world
def build(saves,geo,db):
    read_saves(saves)
    if digest(db)!=DB_HASH:raise ValueError("source DB changed")
    w=pinned_json(geo/"LABD001BASE.json",HASHES[0]);n=w["nodes"]
    clocks=[]
    for name,sha in zip(("LABD001BASE","LABD001ACTIVE","LABD001RETURN"),SAVE_HASHES):
        g=Graph(saves/(name+".sav"),sha)
        ids=[i for i,t in g.types.items() if t==330016]
        if len(ids)!=1:raise ValueError("campaign count")
        i=ids[0];tracker=g.get(i,330016,3,3,100,0);options=g.get(i,330016,2,3,2,0)
        ticks=g.get(tracker,330024,2,2,0,6);mode=g.get(options,330261,2,2,19,5)
        if mode!="Default" or not 0<=ticks<2**53:raise ValueError("clock mode/range")
        clocks.append({"name":name,"ticks":ticks,"mode":mode});g.finish()
    sw=source_world(db,"war_sails")["settlements"]
    if digest(db)!=DB_HASH:raise ValueError("database changed")
    def label(r):return r.get("first_name") or r["id"]
    heroes={k:{a:r[a] for a in ("id","gold","state","clan","spouse","children","father","mother")} | {"name":label(r)} for k,r in n["heroes"].items()}
    clans={k:{a:r[a] for a in ("id","leader","kingdom")} for k,r in n["clans"].items()}
    kingdoms={k:{a:r[a] for a in ("id","ruling_clan")} for k,r in n["kingdoms"].items()}
    forts={}
    for k,r in n["settlements"].items():
        c=r["component"]
        if not isinstance(c,str) or c not in n["fortifications"]:continue
        t=n["fortifications"][c];kind=sw[r["id"]]["kind"]
        if kind not in ("town","castle"):raise ValueError("fort kind")
        forts[k]={"id":r["id"],"name":r["geography"]["name"],"kind":kind,"owner":t["owner"],"gold":t["gold"],"security":t["security"],"prosperity":t["prosperity"]}
    parties={k:{"id":r["id"],"clan":r["clan"],"settlement":r["settlement"],"active":r["active"],"at_sea":r["at_sea"],"healthy":r["personnel"]["healthy"]} for k,r in n["parties"].items()}
    return {"version":1,"source_sha":w["source_sha"],"projection_sha":HASHES[0],"clocks":clocks,"ticks_per_hour":36000000,"days_per_year":84,"days_per_season":21,"heroes":heroes,"clans":clans,"kingdoms":kingdoms,"forts":forts,"parties":parties,"relations":w["relations"],"wars":[x for x in w["stances"] if x["stance"]=="War"]}
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("saves",type=Path);p.add_argument("geo",type=Path);p.add_argument("db",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    w=build(a.saves,a.geo,a.db)
    with a.out.open("x",encoding="utf8") as f:json.dump(w,f,sort_keys=True,allow_nan=False)
    print("FIXTURE",digest(a.out),"CLOCKS",w["clocks"],"COUNTS",{k:len(w[k]) for k in ("heroes","clans","kingdoms","forts","parties","relations","wars")},flush=True)
