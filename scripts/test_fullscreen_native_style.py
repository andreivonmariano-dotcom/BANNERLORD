#!/usr/bin/env python3
"""v0.37 full-bleed CU/native visual unification, static layout safety gate.

This checks that both in-game Gauntlet screens use a true screen-sized
background and native-style bands, without losing the five real settings
chapters, cultural callbacks, or the native character-creation handoff.
Static XML checks do not certify game rendering on a given resolution.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
prefabs=root/"Modules/CalradiaUnified/GUI/Prefabs"
creator=ET.parse(prefabs/"CUSandboxCreator.xml").getroot()
culture=ET.parse(prefabs/"CUNativeCultureChooser.xml").getroot()
overlay=(root/"Source/CalradiaUnified/SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
native_controller=(root/"Source/CalradiaUnified/CUNativeCultureController.cs").read_text(encoding="utf-8")
native_layer=(root/"Source/CalradiaUnified/CUNativeCultureLayer.cs").read_text(encoding="utf-8")
native_start=(root/"Source/CalradiaUnified/SandboxNativeStartOptions.cs").read_text(encoding="utf-8")

for name,xml in (("pregame",creator),("native_culture",culture)):
    assert xml.tag=="Prefab" and len(list(xml.iter("Window")))==1,name
    window=next(xml.iter("Window"))
    root_widget=next(window.iter("Widget"))
    assert root_widget.get("WidthSizePolicy")=="StretchToParent",name
    assert root_widget.get("HeightSizePolicy")=="StretchToParent",name
    planes=[n for n in root_widget.iter("Widget") if
        n.get("Sprite")=="BlankWhiteSquare_9" and
        n.get("WidthSizePolicy")=="StretchToParent" and
        n.get("HeightSizePolicy")=="StretchToParent"]
    assert planes,name+": missing full-screen native backdrop"
    assert planes[0].get("Color")=="#09121DFF",name
    bands=[n for n in root_widget.iter("Widget") if
        n.get("Sprite")=="BlankWhiteSquare_9" and
        n.get("WidthSizePolicy")=="StretchToParent" and
        n.get("HeightSizePolicy")=="Fixed" and
        n.get("VerticalAlignment") in ("Top","Bottom")]
    assert sum(n.get("VerticalAlignment")=="Top" for n in bands)>=2,name
    assert sum(n.get("VerticalAlignment")=="Bottom" for n in bands)>=2,name
    assert any(n.get("Color")=="#C19B62FF" for n in bands),name
    assert any(n.get("Color")=="#B99361FF" for n in bands),name
    # Responsive centered native-style control region. At 1366 x 768 it
    # fits with margins; at 1920 x 1080 it expands, then caps cleanly.
    safe=[n for n in root_widget.iter("Widget") if
        n.get("HorizontalAlignment")=="Center" and
        n.get("VerticalAlignment")=="Center" and
        n.get("WidthSizePolicy")=="StretchToParent" and
        n.get("HeightSizePolicy")=="StretchToParent" and
        n.get("MaxWidth")=="1820" and n.get("MaxHeight")=="960"]
    assert len(safe)==1,name+": missing responsive centered content region"
    assert int(safe[0].get("MarginLeft"))>=20,name
    assert int(safe[0].get("MarginBottom"))>=20,name

labels=[n.get("Text","") for n in creator.iter("TextWidget")]
commands=[n.get("Command.Click") for n in creator.iter("ButtonWidget")]
for label in ("01   Starting life","02   Bloodline","03   Fortune",
              "04   Land & home","05   Review your house",
              "PART 1 OF 2: STARTING CONDITIONS","NEXT: NATIVE CREATOR"):
    assert label in labels,label
for action in ("ExecuteIdentity","ExecuteHousehold","ExecuteResources",
               "ExecuteLocation","ExecuteReview","ExecuteToggleAdvanced",
               "ExecuteNextField","ExecutePreviousField","ExecuteToggle",
               "ExecuteNextChoice","ExecutePreviousChoice","ExecuteSaveNumber",
               "ExecuteEditText","ExecuteReturnToNative",
               "ExecuteApplyAndContinue"):
    assert commands.count(action)==1,action
assert any(n.get("IsVisible")=="@IsUnavailable" for n in creator.iter("TextWidget"))
assert any(n.get("Text")=="@Status" for n in creator.iter("TextWidget"))
assert any(n.get("Text")=="@ProfileSummary" for n in creator.iter("TextWidget")) or (
    any(n.get("Text")=="@RoleSummary" for n in creator.iter("TextWidget")))

native_commands=[n.get("Command.Click") for n in culture.iter("ButtonWidget")]
assert any(n.get("DataSource")=="{Cultures}" for n in culture.iter("ListPanel"))
for action in ("ExecuteSelect","ExecuteContinue","ExecuteBack",
               "ExecuteUseNativeControls"):
    assert native_commands.count(action)==1,action
assert 'Select.Invoke(option.NativeVM,null)' in native_controller
assert 'advance.Invoke(nativeVM,null)' in native_controller
assert 'controller.Continue();' in native_layer
assert 'SandboxNativeStartOptions.TryPrepare(candidate,' in overlay
assert 'new SandBoxGameManager(' in overlay
assert 'new Campaign(CampaignGameMode.Campaign,gameOptions)' in overlay
assert 'MBGameManager.StartNewGame' in overlay

# The native 3D character screen remains genuinely game-owned.
assert 'new BodyGeneratorView(' not in overlay
assert 'NEXT: NATIVE CREATOR' in ET.tostring(creator,encoding="unicode")
print("PASS: both CU screens fill the full display with native-style bands, keep five pages, live culture callbacks and truthful native 3D handoff (STATIC)")
