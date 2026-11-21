#!/usr/bin/env python3
"""Inventory every CU Gauntlet prefab and required BUTR patch.

Source geometry and XML structure are not actual gameplay screenshots.
"""
from pathlib import Path
import json, re, ast
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
ui=root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"
files=sorted(ui.glob("*.xml"))
assert len(files)>=18
results=[]
active={"CUFamilyPortraits.xml","CUNobleDossier.xml"}
for path in files:
    x=ET.parse(path).getroot()
    assert x.tag=="Prefab",path
    bindings=set()
    commands=set()
    ids=set()
    duplicates=[]
    maxw=maxh=0
    for e in x.iter():
        a=e.attrib
        if "Id" in a:
            if a["Id"] in ids: duplicates.append(a["Id"])
            ids.add(a["Id"])
        for k,v in a.items():
            if v.startswith("@"): bindings.add(v[1:])
            if k.startswith("Command."): commands.add(v)
        if a.get("WidthSizePolicy")=="Fixed":
            maxw=max(maxw,int(a.get("SuggestedWidth","0")))
        if a.get("HeightSizePolicy")=="Fixed":
            maxh=max(maxh,int(a.get("SuggestedHeight","0")))
    flags=[]
    if maxw>1216: flags.append("large_720p_width")
    if maxh>656: flags.append("large_720p_height")
    if duplicates:flags.append("duplicate_widget_ids")
    if path.name in active:
        assert not flags,(path.name,flags)
        assert "ExecuteClose" in commands,path.name
    results.append(dict(movie=path.stem,
      category="current_strict" if path.name in active else "stage_or_legacy_review",
      max_fixed_width=maxw,max_fixed_height=maxh,
      bindings=sorted(bindings),commands=sorted(commands),
      duplicate_widget_ids=duplicates,flags=flags))
source=(root/"Source"/"CalradiaUnified"/"CUBUTRNativeScreens.cs").read_text()
fragments=re.findall(
    r'public string Content\s*=>\s*("(?:[^"\\]|\\.)*");',source)
assert len(fragments)==3,len(fragments)
for movie,fragment in zip(("Clan","Party","Kingdom"),fragments):
    widget=ET.fromstring(ast.literal_eval(fragment))
    assert widget.tag=="ButtonWidget"
    assert widget.get("Command.Click")=="ExecuteCUDossier"
    label=widget.find("./Children/TextWidget")
    assert label is not None and label.get("Text")=="@CU"+movie+"Summary"
    results.append(dict(movie=movie+"_BUTR",category="native_patch_live_unverified",
      max_fixed_width=int(widget.get("SuggestedWidth")),
      max_fixed_height=int(widget.get("SuggestedHeight")),
      bindings=["CU"+movie+"Summary"],commands=["ExecuteCUDossier"],
      duplicate_widget_ids=[],flags=[]))
output=root/"BuildResults"/"v070y-all-gauntlet-layouts.json"
output.parent.mkdir(exist_ok=True)
output.write_text(json.dumps(dict(target="Bannerlord v1.5.7.125802",
    prefab_count=len(files),butr_patches=3,game_rendered=False,
    acceptance="BLOCKED_PENDING_GAME",findings=results),indent=2)+"\n")
print("PASS v0.70y PREFABS: {} custom XML movies and 3 BUTR patches parsed. "
      "Legacy layout problems recorded, live visual acceptance BLOCKED."
      .format(len(files)))
print("Source flags: "+", ".join(x["movie"] for x in results if x["flags"]))
