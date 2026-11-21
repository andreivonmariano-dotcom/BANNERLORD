#!/usr/bin/env python3
"""Native Gauntlet layout contract for Calradia Unified personal surfaces.

Validates XML geometry, bindings, closures, and historical state boundaries.
Source geometry does not replace 720p/927p/1080p rendered Bannerlord proof.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/"Source"/"CalradiaUnified"
UI=ROOT/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"

def read(path):
    return path.read_text(encoding="utf-8")

def movie(name):
    root=ET.parse(UI/(name+".xml")).getroot()
    assert root.tag=="Prefab",name
    return root

def center_panel(root):
    hits=[e for e in root.iter("Widget") if
          e.get("HorizontalAlignment")=="Center" and
          e.get("VerticalAlignment")=="Center" and
          e.get("WidthSizePolicy")=="Fixed" and
          e.get("HeightSizePolicy")=="Fixed"]
    assert len(hits)==1,"exactly one centered panel required"
    return hits[0]

def checked_bounds(panel):
    width=int(panel.get("SuggestedWidth"))
    height=int(panel.get("SuggestedHeight"))
    assert width<=1280-64 and height<=720-64,(
        "panel clips minimum supported 1280x720",width,height)
    children=panel.find("Children")
    assert children is not None
    for element in children:
        x=int(element.get("PositionXOffset","0"))
        y=int(element.get("PositionYOffset","0"))
        if element.get("WidthSizePolicy")!="Fixed" or (
                element.get("HeightSizePolicy")!="Fixed"):
            continue
        w=int(element.get("SuggestedWidth","0"))
        h=int(element.get("SuggestedHeight","0"))
        assert w>0 and h>0
        assert 0<=x and 0<=y
        assert x+w<=width and y+h<=height,(
            "child is outside its centered panel",
            element.tag,x,y,w,h,width,height)
    return width,height

def bound_names(root):
    return set(attr[1:] for e in root.iter()
        for attr in e.attrib.values()
        if attr.startswith("@"))

def command_names(root):
    return set(e.get("Command.Click") for e in root.iter()
        if e.get("Command.Click"))

dossier=movie("CUNobleDossier")
family=movie("CUFamilyPortraits")
for name,panel in (("CUNobleDossier",dossier),
                   ("CUFamilyPortraits",family)):
    checked_bounds(center_panel(panel))
    assert "ExecuteClose" in command_names(panel),name

dossier_source=read(SRC/"CUNobleDossierScreen.cs")
origin=read(SRC/"SandboxOriginBehavior.NativeHouseholdUI.cs")
gallery_source=read(SRC/"CUFamilyPortraitGallery.cs")
settings=read(SRC/"NativeSettingsPanel.cs")
assert bound_names(dossier)=={
    "Title","Clan","Family","Officers","Duty","Footer",
    "SectionFamilyTitle","SectionOfficersTitle","SectionDutyTitle"}
for name in bound_names(dossier):
    assert "public string "+name in dossier_source,name
assert 'Command.Click="ExecuteClose"' in read(UI/"CUNobleDossier.xml")
assert "public void ExecuteClose()" in dossier_source
assert 'layer.LoadMovie("CUNobleDossier",viewModel)' in dossier_source
assert "ScreenManager.TrySetFocus(layer)" in dossier_source
assert "Input.IsKeyReleased(InputKey.Escape)" in dossier_source
assert 'ScreenManager.PushScreen(new CUNobleDossierScreen(' in origin
assert 'ShowNativeDetail("Starting household | "+type,' in origin
assert '"cu_native_household_dossier_fallback"' in origin
assert '"cu_native_household_dossier_attached"' in dossier_source
for role_name in ("Wanderer","Outlaw","Mercenary","Soldier",
                  "Knight","Lord","King"):
    assert 'case "'+role_name+'":' in dossier_source
    assert 'case "'+role_name+'":return' in origin
assert 'NativeDossierTitle(type)' in origin
assert 'native_read_only=true' in dossier_source
assert 'HeroCreator.Create' not in dossier_source
assert 'ChangeKingdomAction' not in dossier_source
assert 'ChangeOwnerOfSettlementAction' not in dossier_source

# Roster capacity includes the player, one eligible spouse and six
# children, all native Hero portraits rather than fantasy placeholders.
items=family.find(".//ListPanel[@DataSource='{Members}']")
assert items is not None
row=items.find("./ItemTemplate/ListPanel")
assert row is not None
children=row.find("Children")
assert children is not None
col_widths=[int(e.get("SuggestedWidth","0")) for e in children]
assert sum(col_widths)<=int(items.get("SuggestedWidth"))
assert int(row.get("SuggestedHeight"))*8<=int(
    items.get("SuggestedHeight"))
assert '"CHILD"' in gallery_source
assert 'CharacterCode.CreateFrom(hero.CharacterObject)' in gallery_source
assert 'player.Spouse.Spouse == player' in gallery_source
assert 'ScreenManager.TrySetFocus(layer)' in gallery_source
assert 'Input.IsKeyReleased(InputKey.Escape)' in gallery_source
assert 'Input.IsKeyReleased(InputKey.Escape)' in settings
assert 'CloseSafely()' in settings
# The required BUTR modules are launcher-enforced. UIExtenderEx
# patches native screens and must never ship copied third-party DLLs.
module=read(ROOT/"Modules"/"CalradiaUnified"/"SubModule.xml")
project=read(SRC/"CalradiaUnified.csproj")
for dep in ("Bannerlord.Harmony","Bannerlord.ButterLib",
            "Bannerlord.UIExtenderEx","Bannerlord.MBOptionScreen"):
    assert '<DependedModule Id="'+dep+'"' in module,dep
assert 'Include="Bannerlord.UIExtenderEx" Version="3.0.3"' in project
assert 'ExcludeAssets="runtime"' in project
for path in UI.glob("*.xml"):
    ET.parse(path)
print("PASS v0.70x STATIC: structured read-only Gauntlet dossier, "+
      "720p-safe family gallery bounds and eight-slot roster, "+
      "bound ViewModel fields, Escape/Back, fallback and required BUTR "+
      "framework stack. Live visual acceptance is separate.")
