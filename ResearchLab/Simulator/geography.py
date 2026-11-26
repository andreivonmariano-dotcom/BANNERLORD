"""Pinned native-save geography enrichment. Source XML coordinates are not runtime routes."""
import argparse,collections,hashlib,json,math,sqlite3,xml.etree.ElementTree as ET
from pathlib import Path
from campaign_import import Graph
from typed_save import Reader,field,save_id
from compare_native import SAVE_HASHES,read_saves
from native_rules import f32
NAMES=("LABD001BASE","LABD001ACTIVE","LABD001RETURN")
WORLD_HASHES=("56f100d81665894a6dcb6a93ed0a7d713606f2de0c3006047e1af4bedbf60aba","dd2f9183582bfc95b3ef6e136230d73416c687c2dd217938e616af96658f21f7","b328c94014d4015c9f48273deb586d06722bdc899679b1d835ffc1f807f0de63")
DB_HASH="213f3ca97f3316dd98c004ef4a8dd53b4b0f50075de9c8d13103c3f4d8425404"
def digest(path):
    with path.open("rb") as f:return hashlib.file_digest(f,"sha256").hexdigest()
def pinned_json(path,sha):
    if digest(path)!=sha:raise ValueError("projection digest mismatch")
    return json.loads(path.read_text(encoding="utf8"))
def base_speed(n):
    if type(n) is not int or n<0:raise ValueError("party count")
    return f32(f32(4.6)*f32(math.pow(f32(200/f32(200+n)),f32(.4))))
def point(g,i,member):
    fs,es,r=g.obj(i,330074);v=g.value(es,r,2,4,member,4)
    kids=[k for k,(p,l,t) in fs.items() if p==r and l==v and t==2]
    if len(kids)!=1:raise ValueError("position struct missing")
    k=kids[0];h=Reader(es[k,-1,8])
    if save_id(h)!=331022:raise ValueError("position struct type")
    h.s();h.s();h.end()
    values=[]
    for (p,j,t),raw in es.items():
        if p!=k or t!=2:continue
        h=Reader(raw);kind,level,local=h.b(),h.b(),h.s()
        if (kind,level,local)==(6,1,1):
            if save_id(h)!=30011:raise ValueError("Vec2 type")
            values.append([h.num("<f"),h.num("<f")]);h.end()
    if len(values)!=1 or not all(math.isfinite(x) for x in values[0]):raise ValueError("coordinate")
    return {"xy":values[0],"on_land":g.value(es,k,2,1,2,6)}
def source_data(path):
    if digest(path)!=DB_HASH:raise ValueError("database digest")
    db=sqlite3.connect(path.resolve().as_uri()+"?mode=ro",uri=True)
    settlements={};levels=collections.defaultdict(list)
    try:
        for oid,xml,src in db.execute("select d.object_id,d.xml,s.path from definitions d join sources s on s.id=d.source where s.kind='Settlements' and s.path=?",("Modules/NavalDLC/ModuleData/settlements.xml",)):
            e=ET.fromstring(xml)
            def xy(prefix):
                x=e.get(prefix+"posX");y=e.get(prefix+"posY")
                if x is None or y is None:return None
                a=[float(x),float(y)]
                if not all(map(math.isfinite,a)):raise ValueError("source coordinate")
                return a
            if oid in settlements:raise ValueError("duplicate settlement")
            settlements[oid]={"name":e.get("name"),"xy":xy(""),"gate":xy("gate_"),"port":xy("port_"),"source":src}
        for oid,attrs,src in db.execute("select d.object_id,d.attributes,s.path from definitions d join sources s on s.id=d.source where s.kind='NPCCharacters'"):
            a=json.loads(attrs)
            if a.get("level") is not None:levels[oid].append({"level":int(a["level"]),"name":a.get("name"),"source":src})
    finally:db.close()
    if digest(path)!=DB_HASH:raise ValueError("database changed")
    return settlements,dict(levels)
def enrich(path,sha,w,settlements,levels):
    if w["source_sha"]!=sha:raise ValueError("world source")
    g=Graph(path,sha);n=w["nodes"];ships={}
    for key,r in n["heroes"].items():
        i=int(key.split(":")[1]);r["level"]=g.get(i,330048,2,3,370,6)
        name=g.get(i,330048,2,3,180,0)
        r["first_name"]=None if name==-1 else g.strings[g.get(name,20001,2,2,1,2)]
    for r in n["settlements"].values():r["geography"]=settlements.get(r["id"])
    for key,r in n["parties"].items():
        i=int(key.split(":")[1]);t=330074
        r["position"]=point(g,i,225);r["target_position"]=point(g,i,224)
        r["active"]=g.get(i,t,3,4,1023,6);r["at_sea"]=g.get(i,t,2,4,1110,6)
        r["navigation"]=g.get(i,t,2,4,1093,5)
        r["land_capable"]=g.get(i,t,3,4,1099,6)
        r["target_party"]=g.sid(g.get(i,t,2,4,1048,0),330074)
        r["target_settlement"]=g.sid(g.get(i,t,2,4,1049,0),330095)
        p=g.get(i,t,3,4,1020,0);a=g.get(p,330077,2,2,20,1)
        r["ships"]=[]
        if a!=-1:
            f,e,root=g.container(a,("container",5,(330316,)))
            for j in range(g.cn[a]):
                kind,_,_,s=field(e[root,j,5])
                if kind!=0 or g.types.get(s)!=330316:raise ValueError("ship reference")
                if g.get(s,330316,2,2,4,0)!=p:raise ValueError("ship owner mismatch")
                if str(s) in ships:raise ValueError("duplicate ship")
                h=g.get(s,330316,2,2,2,0)
                ships[str(s)]={"hull":g.sid(h,10054),"hp":g.get(s,330316,2,2,3,6),"sail_hp":g.get(s,330316,2,2,12,6),"party":key}
                r["ships"].append(str(s))
    # Only levels actually present in the save are authoritative for heroes.
    w["character_sources"]=levels;w["party_ships"]=ships;w["geography_scope"]="saved party positions; selected DLC XML settlement coordinates; no route mesh"
    g.finish()
    return w
def summary(w):
    n=w["nodes"];ps=list(n["parties"].values());p=next(r for r in ps if r["id"]=="player_party")
    return {"positions":len(ps),"active":sum(r["active"] for r in ps),"at_sea":sum(r["at_sea"] for r in ps),"ships_in_party_lists":len(w["party_ships"]),"settlement_coordinates":sum(r["geography"] is not None and r["geography"]["xy"] is not None for r in n["settlements"].values()),"ports_with_source_coordinates":sum(r["geography"] is not None and r["geography"]["port"] is not None for r in n["settlements"].values()),"player_position":p["position"],"player_ship_count":len(p["ships"]),"player_base_speed":base_speed(p["personnel"]["healthy"]+p["personnel"]["wounded"])}
def main():
    p=argparse.ArgumentParser();p.add_argument("saves",type=Path);p.add_argument("world",type=Path);p.add_argument("db",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    read_saves(a.saves);ss,ls=source_data(a.db)
    for name,sha,wh in zip(NAMES,SAVE_HASHES,WORLD_HASHES):
        w=enrich(a.saves/(name+".sav"),sha,pinned_json(a.world/(name+".json"),wh),ss,ls)
        dst=a.out/(name+".json")
        with dst.open("x",encoding="utf8") as f:json.dump(w,f,sort_keys=True,allow_nan=False)
        print(name,json.dumps(summary(w),sort_keys=True),"SHA",digest(dst),flush=True)
if __name__=="__main__":main()
