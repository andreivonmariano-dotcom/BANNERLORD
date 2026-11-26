"""Read-only source-world projection and isolated market correction experiment.
This deliberately selects exactly one official settlement source. It does not
claim to reproduce MBObjectManager schema merges, runtime ownership or saves.
"""
import argparse
import collections
import hashlib
import json
import sqlite3
import xml.etree.ElementTree as ET
from pathlib import Path
from native_rules import market_change,calendar,age_after_days,age_stage

SOURCES={"base":"Modules/SandBox/ModuleData/settlements.xml",
         "war_sails":"Modules/NavalDLC/ModuleData/settlements.xml"}

def source_world(database,profile):
    database=Path(database).resolve()
    with database.open("rb") as f:before=hashlib.file_digest(f,"sha256").hexdigest()
    db=sqlite3.connect(database.as_uri()+"?mode=ro",uri=True)
    records={}
    try:
        source=db.execute("SELECT id,sha256 FROM sources WHERE path=? AND kind='Settlements' AND status='parsed'",(SOURCES[profile],)).fetchone()
        if not source:raise ValueError("source not indexed")
        for oid,xml in db.execute("SELECT object_id,xml FROM definitions WHERE source=? ORDER BY ordinal",(source[0],)):
            e=ET.fromstring(xml)
            container=e.find("Components")
            components=list(container) if container is not None else []
            if not oid or oid in records or len(components)!=1:raise ValueError("unexpected source structure")
            c=components[0];kind=c.tag
            if kind=="Town":kind="castle" if c.get("is_castle","false").lower()=="true" else "town"
            records[oid]={"id":oid,"kind":kind,"name":e.get("name"),"culture_ref":e.get("culture"),"owner_ref":e.get("owner"),
                          "bound_ref":c.get("bound"),"prosperity":float(c.get("prosperity")) if c.get("prosperity") is not None else None,
                          "hearth":float(c.get("hearth")) if c.get("hearth") is not None else None}
        if not records:raise ValueError("empty source")
    finally:db.close()
    with database.open("rb") as f:after=hashlib.file_digest(f,"sha256").hexdigest()
    if before!=after:raise ValueError("database changed during read")
    return {"profile":profile,"source":SOURCES[profile],"source_sha256":source[1],"database_sha256":before,"settlements":records}

def run(world,days,initial_gold=10000):
    if type(days) is not int or not 0<=days<=365:raise ValueError("invalid duration")
    if type(initial_gold) is not int or initial_gold<0:raise ValueError("invalid gold")
    # Town-only experiment, deliberately excludes castles and villages.
    towns={k:{"prosperity":v["prosperity"],"gold":initial_gold} for k,v in world["settlements"].items() if v["kind"]=="town"}
    if any(t["prosperity"] is None for t in towns.values()):raise ValueError("missing prosperity")
    history=[sum(t["gold"] for t in towns.values())]
    for _ in range(days):
        for t in towns.values():t["gold"]+=market_change(t["prosperity"],t["gold"])
        history.append(sum(t["gold"] for t in towns.values()))
    return {"source":world["source"],"source_sha256":world["source_sha256"],"database_sha256":world["database_sha256"],
            "day":days,"settlements":world["settlements"],"town_market_scenario":towns,
            "history":history,"initial_gold_per_town":initial_gold,"scope":"source projection; isolated fixed prosperity market correction only"}

def main():
    p=argparse.ArgumentParser();p.add_argument("database",type=Path);p.add_argument("profile",choices=SOURCES);p.add_argument("output",type=Path);p.add_argument("--days",type=int,default=21)
    a=p.parse_args()
    if a.output.suffix.lower()!=".json":raise ValueError("JSON only")
    w=source_world(a.database,a.profile);r=run(w,a.days)
    with a.output.open("x",encoding="utf-8") as f:json.dump(r,f,sort_keys=True)
    count=collections.Counter(v["kind"] for v in w["settlements"].values())
    print(json.dumps({"profile":a.profile,"source_sha256":w["source_sha256"],"database_sha256":w["database_sha256"],"counts":count,
        "towns":len(r["town_market_scenario"]),"days":r["day"],"market_gold_initial":r["history"][0],"market_gold_final":r["history"][-1],
        "default_calendar":calendar(),"fast_calendar":calendar(True),"age_17_after_default_year":age_after_days(17,84),"stage":age_stage(age_after_days(17,84))}))
if __name__=="__main__":main()
