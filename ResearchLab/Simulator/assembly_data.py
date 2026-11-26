"""Convert local reflection JSONL into a searchable SQLite catalogue."""
import argparse
import json
import sqlite3
from pathlib import Path

def build(source,output):
    source=Path(source);output=Path(output)
    if output.exists():raise ValueError("new database required")
    db=sqlite3.connect(output)
    try:
        db.executescript("""CREATE TABLE members(kind TEXT,assembly TEXT,type TEXT,name TEXT,signature TEXT,record TEXT);
        CREATE INDEX member_type ON members(type);
        CREATE INDEX member_name ON members(name);
        CREATE INDEX member_kind ON members(kind,assembly);""")
        with source.open(encoding="utf-8") as stream:
            for line in stream:
                r=json.loads(line)
                db.execute("INSERT INTO members VALUES(?,?,?,?,?,?)",(r["kind"],r["assembly"],r.get("type"),r.get("name"),r.get("signature"),line.strip()))
        db.commit()
        return {"counts":db.execute("SELECT kind,count(*) FROM members GROUP BY kind ORDER BY kind").fetchall(),
                "integrity":db.execute("PRAGMA integrity_check").fetchone()[0],
                "assemblies":db.execute("SELECT count(*) FROM members WHERE kind='assembly'").fetchone()[0],
                "campaign_event_properties":db.execute("SELECT count(*) FROM members WHERE kind='property' AND type='TaleWorlds.CampaignSystem.CampaignEvents'").fetchone()[0],
                "quest_lifecycle_methods":db.execute("SELECT count(*) FROM members WHERE kind='method' AND type='TaleWorlds.CampaignSystem.QuestBase'").fetchone()[0]}
    finally:db.close()
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("source");p.add_argument("output");a=p.parse_args()
    print(json.dumps(build(a.source,a.output),indent=2))
