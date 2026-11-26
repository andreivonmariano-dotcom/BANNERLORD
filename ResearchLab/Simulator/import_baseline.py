"""Single-save baseline import for the pinned 1.5.7 control schema. No native writes."""
import argparse, hashlib, json
from pathlib import Path
from save_reader import inspect_save
from campaign_import import Graph
from world_projection import project
from geography import digest, DB_HASH
from source_world import source_world
from compare_native import SAVE_HASHES

def require(ok,msg):
    if not ok: raise ValueError(msg)
def build(save,sha,reference,db):
    ref,_=inspect_save(reference)
    require(ref["sha256"]==SAVE_HASHES[0],"untrusted reference profile")
    meta,_=inspect_save(save)
    require(meta["sha256"]==sha,"save hash mismatch")
    require(meta["modules"]==ref["modules"] and meta["module_versions"]==ref["module_versions"],"unverified profile")
    require(digest(db)==DB_HASH,"source database hash")
    w=project(save,sha,meta);n=w["nodes"];g=Graph(save,sha)
    ids=[i for i,t in g.types.items() if t==330016]
    require(len(ids)==1,"campaign count")
    i=ids[0];tracker=g.get(i,330016,3,3,100,0);options=g.get(i,330016,2,3,2,0)
    ticks=g.get(tracker,330024,2,2,0,6);mode=g.get(options,330261,2,2,19,5)
    require(mode=="Default" and type(ticks) is int and 0<=ticks<2**53,"unsupported clock")
    sw=source_world(db,"war_sails")["settlements"]
    heroes={}
    for k,r in n["heroes"].items():
        i=int(k.split(":")[1]);name=g.get(i,330048,2,3,180,0)
        label=None if name==-1 else g.strings[g.get(name,20001,2,2,1,2)]
        heroes[k]={a:r[a] for a in ("id","gold","state","clan","spouse","children","father","mother")} | {"name":label or r["id"]}
    clans={k:{a:r[a] for a in ("id","leader","kingdom")} for k,r in n["clans"].items()}
    kingdoms={k:{a:r[a] for a in ("id","ruling_clan")} for k,r in n["kingdoms"].items()}
    forts={}
    for k,r in n["settlements"].items():
        c=r["component"]
        if not isinstance(c,str) or c not in n["fortifications"]:continue
        t=n["fortifications"][c];d=sw[r["id"]];require(d["kind"] in ("town","castle"),"fort kind")
        forts[k]={"id":r["id"],"name":d["name"],"kind":d["kind"],"owner":t["owner"],"gold":t["gold"],"security":t["security"],"prosperity":t["prosperity"]}
    parties={}
    for k,r in n["parties"].items():
        i=int(k.split(":")[1]);require(r["personnel"] is not None,"unresolved roster")
        parties[k]={"id":r["id"],"clan":r["clan"],"settlement":r["settlement"],"active":g.get(i,330074,3,4,1023,6),"at_sea":g.get(i,330074,2,4,1110,6),"healthy":r["personnel"]["healthy"]}
    g.finish();require(digest(db)==DB_HASH,"database changed")
    projection_hash=hashlib.sha256(json.dumps(w,sort_keys=True,allow_nan=False).encode()).hexdigest()
    return {"version":1,"source_sha":sha,"projection_sha":projection_hash,"clocks":[{"name":save.stem,"ticks":ticks,"mode":mode}],"ticks_per_hour":36000000,"days_per_year":84,"days_per_season":21,"heroes":heroes,"clans":clans,"kingdoms":kingdoms,"forts":forts,"parties":parties,"relations":w["relations"],"wars":[x for x in w["stances"] if x["stance"]=="War"]}
def main():
    p=argparse.ArgumentParser();p.add_argument("save",type=Path);p.add_argument("sha");p.add_argument("reference",type=Path);p.add_argument("db",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    w=build(a.save,a.sha,a.reference,a.db)
    with a.out.open("x",encoding="utf8") as f:json.dump(w,f,sort_keys=True,allow_nan=False)
    print("IMPORTED",w["clocks"][0]["ticks"],{k:len(w[k]) for k in ("heroes","clans","kingdoms","forts","parties","relations","wars")},"SHA",digest(a.out),flush=True)
if __name__=="__main__":main()
