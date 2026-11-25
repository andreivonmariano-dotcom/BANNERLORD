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
active={"CUFamilyPortraits.xml","CUNobleDossier.xml",
        "CUHouseholdCamp.xml","CUAffairs.xml"}
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
assert len(fragments)==4,len(fragments)
for movie,fragment in zip(("Clan","Party","Kingdom","PartyCamp"),fragments):
    widget=ET.fromstring(ast.literal_eval(fragment))
    assert widget.tag=="ButtonWidget"
    label=widget.find("./Children/TextWidget")
    assert label is not None
    if movie=="PartyCamp":
        assert widget.get("Command.Click")=="ExecuteCUCamp"
        assert label.get("Text")=="Household camp"
        bindings=[]
        commands=["ExecuteCUCamp"]
    else:
        assert widget.get("Command.Click")=="ExecuteCUDossier"
        assert label.get("Text")=="@CU"+movie+"Summary"
        bindings=["CU"+movie+"Summary"]
        commands=["ExecuteCUDossier"]
    results.append(dict(movie=movie+"_BUTR",category="native_patch_live_unverified",
      max_fixed_width=int(widget.get("SuggestedWidth")),
      max_fixed_height=int(widget.get("SuggestedHeight")),
      bindings=bindings,commands=commands,
      duplicate_widget_ids=[],flags=[]))
affairs=(root/"Source"/"CalradiaUnified"/"CUMapAffairsNative.cs").read_text()
map_fragments=re.findall(r'public string Content\s*=>\s*("(?:[^"\\]|\\.)*");',affairs)
assert len(map_fragments)==2
for movie,fragment,command in zip(("MapBar","MapNotificationUI"),map_fragments,
        ("ExecuteCUAffairs","ExecuteCUAffairsNotice")):
    widget=ET.fromstring(ast.literal_eval(fragment))
    assert widget.tag=="ButtonWidget" and widget.get("Command.Click")==command
    results.append(dict(movie=movie+"_BUTR",category="native_patch_live_unverified",
      max_fixed_width=int(widget.get("SuggestedWidth")),max_fixed_height=int(widget.get("SuggestedHeight")),
      bindings=[v[1:] for e in widget.iter() for v in e.attrib.values() if v.startswith("@")],
      commands=[command],duplicate_widget_ids=[],flags=[]))
journal=ET.parse(ui/"CUAffairs.xml").getroot()
journal_vm=(root/"Source"/"CalradiaUnified"/"CUAffairsScreen.cs").read_text()
for e in journal.iter():
    for k,v in e.attrib.items():
        if k.startswith("Command."):assert "void "+v+"(" in journal_vm,v
        if v.startswith("@") and e.tag!="ImageIdentifierWidget":assert re.search(r"\b"+re.escape(v[1:])+r"\s*\{",journal_vm),v
assert any(e.tag=="ImageIdentifierWidget" and e.get("DataSource")=="{Portrait}" for e in journal.iter())
assert any(e.tag=="ImageIdentifierWidget" and e.get("DataSource")=="{SenderBanner}" for e in journal.iter())
assert "new BannerImageIdentifierVM(HasSenderBanner?" in journal_vm
assert "senderClan.Banner:null" in journal_vm
for name in active:
    movie=ET.parse(ui/name).getroot()
    sprites={e.get("Sprite") for e in movie.iter()}
    assert "StdAssets\\Popup\\canvas" in sprites,name
    assert "frame_9" in sprites,name
    assert "Popup.Done.Button.NineGrid" in (ui/name).read_text(),name
assert any(e.get("Sprite")=="Quest\\header left" for e in journal.iter())
assert any(e.get("Sprite")=="SPGeneral\\MapNotification\\kingdom_banner_frame_2" for e in journal.iter())
assert "Map.Notification.More.Panel" in affairs
assert any(e.get("Brush")=="Popup.SelectionElement.Tuple" and e.get("IsSelected")=="@Selected" for e in journal.iter())
assert any(e.get("Brush")=="Quest.Tuple.Notification" and e.get("IsVisible")=="@Pending" for e in journal.iter())
assert any(e.get("Sprite")=="General\\Icons\\icon_quest_done_checkmark" and e.get("IsVisible")=="@Completed" for e in journal.iter())
output=root/"BuildResults"/"v070y-all-gauntlet-layouts.json"
output.parent.mkdir(exist_ok=True)
output.write_text(json.dumps(dict(target="Bannerlord v1.5.7.125802",
    prefab_count=len(files),butr_patches=6,game_rendered=False,
    acceptance="BLOCKED_PENDING_GAME",findings=results),indent=2)+"\n")
print("PASS v0.70y PREFABS: {} custom XML movies and 6 BUTR patches parsed. "
      "Legacy layout problems recorded, live visual acceptance BLOCKED."
      .format(len(files)))
print("Source flags: "+", ".join(x["movie"] for x in results if x["flags"]))
