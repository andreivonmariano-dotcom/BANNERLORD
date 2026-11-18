#!/usr/bin/env python3
"""Static audit of the NEW visually independent Sandbox creator.
The test cannot prove real Gauntlet rendering, mouse hits or saved campaigns."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parent.parent
p=ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml"
src=(ROOT/"Source/CalradiaUnified/SandboxCreatorOverlay.cs").read_text()
origin=(ROOT/"Source/CalradiaUnified/SandboxOriginBehavior.cs").read_text()
entry=(ROOT/"Source/CalradiaUnified/SubModule.cs").read_text()
root=ET.parse(p).getroot()
assert root.tag=="Prefab"
clicks=[]
binding=set()
sliders=0
for element in root.iter():
    if element.tag=="SliderWidget": sliders+=1
    if element.tag=="ButtonWidget":
        command=element.get("Command.Click")
        assert command
        clicks.append(command)
        texts=[x for x in element.iter() if x.tag=="TextWidget"]
        assert len(texts)==1,(command,len(texts))
        # The old creator's missing text widths produced vertical letters.
        assert texts[0].get("WidthSizePolicy")=="StretchToParent",command
        assert texts[0].get("HeightSizePolicy")=="StretchToParent",command
        assert int(element.get("SuggestedWidth"))>=130,command
    for key,value in element.attrib.items():
        if value.startswith("@"): binding.add(value[1:])
assert sliders==1
assert len(clicks)>=19
for c in set(clicks):
    assert re.search(r"\bpublic\s+void\s+"+re.escape(c)+r"\s*\(",src),c
for b in binding:
    assert re.search(r"\bpublic\s+(?:bool|int|string)\s+"+
                     re.escape(b)+r"\b",src),b
assert set(["ExecuteIdentity","ExecuteHousehold","ExecuteProgression",
            "ExecuteEquipment","ExecuteResources","ExecuteLocation",
            "ExecuteReview","ExecuteReturnToNative",
            "ExecuteApplyAndContinue"]).issubset(clicks)
assert "Input.IsKeyReleased(InputKey.Escape)" in src
assert "game.GameStateManager.ActiveState is CharacterCreationState" in src
assert "ScreenManager.PushScreen(new SandboxCreatorScreen())" in src
assert "SandboxCreatorOverlay.Arm(manager)" in origin
assert "SandboxCreatorOverlay.Tick()" in entry
assert "new InitialStateOption(" not in entry
assert "NativeStartingLifeMenus.TryAttach(manager" not in origin
print("PASS: native Sandbox entry, seven custom pages, safe text width, keyboard exit, profile commit and native fallback")
