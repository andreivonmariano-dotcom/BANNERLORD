#!/usr/bin/env python3
"""Audit the INSTALLED Bannerlord UI resources before binding CU prefabs.

Usage:
    python scripts/audit_native_creator_art.py "C:/Games/Mount & Blade II Bannerlord"
Output:
    BuildResults/native-gauntlet-asset-audit.json

Read-only. This does not copy TaleWorlds images into the project,
overwrite vanilla files or assume asset IDs from a newer game.
"""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ap=argparse.ArgumentParser()
ap.add_argument("game_root",type=Path)
ap.add_argument("--output",type=Path,
                default=Path("BuildResults/native-gauntlet-asset-audit.json"))
args=ap.parse_args()
base=args.game_root.resolve()
mods=["Native","SandBox","SandBoxCore","StoryMode"]
report={"game_root":str(base),"sources":[],"brushes":[],
        "sprite_references":[],"character_creation_prefabs":[],"errors":[]}
for mod in mods:
    gui=base/"Modules"/mod/"GUI"
    if not gui.exists():
        continue
    for path in (gui/"Brushes").glob("*.xml"):
        try:
            tree=ET.parse(path)
            count=0
            for brush in tree.iter("Brush"):
                name=brush.get("Name","")
                if not name: continue
                if any(x in name.lower() for x in
                       ("character","creation","panel","frame","slider",
                        "scroll","border","title","button","popup","menu",
                        "standard","header","option","background")):
                    report["brushes"].append({"name":name,"source":str(path),
                                               "module":mod})
                    count+=1
            report["sources"].append({"path":str(path),"matching_brushes":count})
        except (ET.ParseError,OSError) as ex:
            report["errors"].append({"path":str(path),"error":str(ex)})
    for path in (gui/"Prefabs").rglob("*.xml"):
        if not any(x in path.as_posix().lower()
                   for x in ("charactercreation","character_creation",
                             "charactercreation", "narrative", "age")):
            continue
        try:
            tree=ET.parse(path)
            refs=[]
            for el in tree.iter():
                for key in ("Sprite","Brush"):
                    val=el.get(key)
                    if val and val not in refs: refs.append(val)
            report["character_creation_prefabs"].append({
                "path":str(path),"module":mod,"resources":refs[:300]})
        except (ET.ParseError,OSError) as ex:
            report["errors"].append({"path":str(path),"error":str(ex)})
    for path in gui.glob("*SpriteData*.xml"):
        try:
            tree=ET.parse(path)
            for el in tree.iter():
                name=el.get("Name","")
                if name and any(k in name.lower()
                      for k in ("charactercreation","frame","panel","slider",
                                "background","wood","stone","parchment")):
                    report["sprite_references"].append({
                        "name":name,"module":mod,"source":str(path)})
        except (ET.ParseError,OSError) as ex:
            report["errors"].append({"path":str(path),"error":str(ex)})
# Keep source names unique, do not invent unavailable matching assets.
report["brushes"].sort(key=lambda x:(x["module"],x["name"],x["source"]))
report["character_creation_prefabs"].sort(key=lambda x:x["path"])
report["sprite_references"].sort(key=lambda x:(x["module"],x["name"]))
args.output.parent.mkdir(parents=True,exist_ok=True)
args.output.write_text(json.dumps(report,indent=2,ensure_ascii=False),
                       encoding="utf-8")
print("Game-native UI audit:",
      len(report["brushes"]),"matching brushes,",
      len(report["sprite_references"]),"matching sprite IDs,",
      len(report["character_creation_prefabs"]),"creator prefabs.")
print("Report:",args.output.resolve())
if not report["brushes"] and not report["character_creation_prefabs"]:
    raise SystemExit("No game UI resources found. Check --game-root and version.")
