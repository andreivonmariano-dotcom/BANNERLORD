"""Local source-definition catalogue, not an engine-effective merged world."""
import argparse
import hashlib
import json
import sqlite3
import xml.etree.ElementTree as ET
from pathlib import Path

MODULES=("Native","SandBoxCore","SandBox","NavalDLC")
KINDS={"Items","SPCultures","NPCCharacters","Heroes","Kingdoms","Factions","WorkshopTypes",
       "Settlements","partyTemplates","EquipmentRosters","SkillSets","CraftingPieces",
       "CraftingTemplates","ItemModifiers","ItemModifierGroups","ShipHulls","ShipSlots",
       "ShipUpgradePieces","MissionShips","CoreParameters","WeaponDescriptions"}
def parse(path):
    data=path.read_bytes()
    if len(data)>32*1024*1024 or b"<!DOCTYPE" in data.upper() or b"<!ENTITY" in data.upper():
        raise ValueError("unsupported XML")
    return ET.fromstring(data),hashlib.sha256(data).hexdigest()
def build(root,output):
    root=Path(root).resolve();output=Path(output).resolve()
    if output.is_relative_to(root) or output.exists():raise ValueError("new DB outside game required")
    output.parent.mkdir(parents=True,exist_ok=True)
    db=sqlite3.connect(output)
    try:
        db.executescript("""CREATE TABLE sources(id INTEGER PRIMARY KEY,module TEXT,path TEXT UNIQUE,kind TEXT,sha256 TEXT,status TEXT);
        CREATE TABLE definitions(source INTEGER,ordinal INTEGER,tag TEXT,object_id TEXT,attributes TEXT,xml TEXT,PRIMARY KEY(source,ordinal));
        CREATE TABLE registrations(module TEXT,kind TEXT,path TEXT,status TEXT);
        CREATE INDEX by_object ON definitions(tag,object_id);
        CREATE INDEX by_kind ON sources(kind);""")
        for module in MODULES:
            base=root/"Modules"/module
            manifest,_=parse(base/"SubModule.xml")
            data=base/"ModuleData"
            entries=[(n.get("id"),n.get("path")) for n in manifest.findall(".//XmlNode/XmlName") if n.get("id") in KINDS]
            if module=="SandBox":entries.append(("Projects","spprojects"))
            for kind,relative in dict.fromkeys(entries):
                if not relative:continue
                path=(data/relative).resolve()
                if not path.is_relative_to(data.resolve()):raise ValueError("path escapes ModuleData")
                if relative.startswith("XSLT/"):
                    db.execute("INSERT INTO registrations VALUES(?,?,?,?)",(module,kind,relative,"transform_not_applied"));continue
                files=sorted(path.glob("*.xml")) if path.is_dir() else [path.with_suffix(".xml")]
                if not files or not all(p.is_file() for p in files):
                    db.execute("INSERT INTO registrations VALUES(?,?,?,?)",(module,kind,relative,"missing"));continue
                db.execute("INSERT INTO registrations VALUES(?,?,?,?)",(module,kind,relative,"indexed_source"))
                for file in files:
                    if not file.resolve().is_relative_to(data.resolve()):raise ValueError("source escapes ModuleData")
                    rel=file.relative_to(root).as_posix()
                    if db.execute("SELECT 1 FROM sources WHERE path=?",(rel,)).fetchone():continue
                    try:element,sha=parse(file)
                    except (ET.ParseError,ValueError,OSError) as exc:
                        db.execute("INSERT INTO sources(module,path,kind,sha256,status) VALUES(?,?,?,?,?)",(module,rel,kind,"","error:"+type(exc).__name__));continue
                    source=db.execute("INSERT INTO sources(module,path,kind,sha256,status) VALUES(?,?,?,?,?)",(module,rel,kind,sha,"parsed")).lastrowid
                    db.executemany("INSERT INTO definitions VALUES(?,?,?,?,?,?)",((source,i,e.tag,e.get("id"),json.dumps(e.attrib,ensure_ascii=False),ET.tostring(e,encoding="unicode")) for i,e in enumerate(element)))
        db.commit()
        return {"sources":db.execute("SELECT count(*) FROM sources").fetchone()[0],
                "source_status":db.execute("SELECT status,count(*) FROM sources GROUP BY status").fetchall(),
                "registrations":db.execute("SELECT status,count(*) FROM registrations GROUP BY status").fetchall(),
                "definitions":db.execute("SELECT s.kind,count(*),count(DISTINCT d.object_id) FROM definitions d JOIN sources s ON s.id=d.source GROUP BY s.kind ORDER BY s.kind").fetchall(),
                "duplicate_settlement_ids":db.execute("SELECT count(*) FROM (SELECT object_id FROM definitions d JOIN sources s ON s.id=d.source WHERE s.kind='Settlements' AND object_id IS NOT NULL GROUP BY object_id HAVING count(*)>1)").fetchone()[0],
                "integrity":db.execute("PRAGMA integrity_check").fetchone()[0]}
    finally:db.close()
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--game-root",required=True);p.add_argument("--output",required=True);a=p.parse_args()
    print(json.dumps(build(a.game_root,a.output),indent=2))
