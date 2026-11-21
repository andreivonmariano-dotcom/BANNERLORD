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
menu=(ROOT/"Source/CalradiaUnified/SandboxMainMenuEntry.cs").read_text()
entry=(ROOT/"Source/CalradiaUnified/SubModule.cs").read_text()
root=ET.parse(p).getroot()
assert root.tag=="Prefab"
assert any(n.tag=="Widget" and n.get("Sprite")=="BlankWhiteSquare_9"
           and n.get("Color","").startswith("#")
           for n in root.iter())
assert any(n.tag=="TextWidget" and n.get("Text")=="@FamilySummary"
           for n in root.iter())
clicks=[]
binding=set()
sliders=0
for element in root.iter():
    if element.tag=="SliderWidget": sliders+=1
    if element.tag=="ButtonWidget":
        command=element.get("Command.Click")
        assert command
        assert element.get("Brush")=="ButtonBrush",command
        assert element.get("DoNotPassEventsToChildren")=="true",command
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
assert len(clicks)>=18
for c in set(clicks):
    assert re.search(r"\bpublic\s+void\s+"+re.escape(c)+r"\s*\(",src),c
for b in binding:
    assert re.search(r"\bpublic\s+(?:bool|int|string)\s+"+
                     re.escape(b)+r"\b",src),b
assert set(["ExecuteIdentity","ExecuteHousehold",
            "ExecuteResources","ExecuteLocation","ExecuteReview",
            "ExecuteToggleAdvanced","ExecuteReturnToNative",
            "ExecuteApplyAndContinue","ExecuteNextField",
            "ExecutePreviousField"]).issubset(clicks)
assert "IsAvailable(Field field)" in src
assert "VisibleFields" in src
assert "AdvancedToggleLabel" in src
assert "IsNotReviewPage" in src
assert "candidate.NativeStageEnabled=false;" in src
assert "profile.ResetForNewCampaign();" in src
assert 'if (!Write(Current.Key,next.ToString(' in src
assert "sandbox_creator_family_plan" in src
assert '"|child_count="+profile.ChildCount' in src
assert 'Text="PART 1 OF 2: STARTING CONDITIONS"' in p.read_text()
assert 'Text="NEXT: NATIVE CREATOR"' in p.read_text()
assert "ExecuteAppearance" not in clicks
assert "Input.IsKeyReleased(InputKey.Escape)" in src
assert "if(viewModel!=null) viewModel.ExecuteBack();" in src, "Escape must navigate guided Back before quitting"
assert "SandboxNativeStartOptions.TryPrepare" in src
aso=(ROOT/"Source/CalradiaUnified/SandboxNativeStartOptions.cs").read_text()
assert "AdvancedStartOptionsManager.CreateCampaignStartOptions()" in aso
assert '"Lord": return "vassal"' not in aso or 'case "Lord": return "vassal"' in aso
assert 'case "Lord": return "vassal"' in aso or 'case "Lord": return "vassal";' in aso or ('case "Lord": return "vassal"' in aso) or ('case "Lord":' in aso and 'case "Knight":' in aso)
assert "new CUSandboxNoIntroGameManager(" in src
assert "public override void OnLoadFinished()" in (
    ROOT/"Source/CalradiaUnified/CUSandboxNoIntroGameManager.cs").read_text(encoding="utf-8")
assert "MBGameManager.StartNewGame(" in src
# A separately pushed ScreenBase deactivates the animated main menu.
# World is now mounted as a GauntletLayer on its existing screen.
assert "ScreenManager.PushScreen(new SandboxCreatorScreen())" not in src
assert "SandboxCreatorMenuLayer.Open(" in src
assert "SandboxCreatorMenuLayer.Tick();" in src
assert "SandboxCreatorOverlay.Arm(manager)" not in origin
assert "new NativeSandboxStageObserver()" in origin
assert "Arm(native.CharacterCreationManager)" not in src
assert "SandboxCreatorOverlay.BlockNewGameProfile" in origin
assert "SandboxCreatorOverlay.Tick()" in entry
assert "OnBeforeInitialModuleScreenSetAsRoot()" in entry
assert "SandboxMainMenuEntry.Install()" in entry
assert "SandBoxNewGame" in menu
assert "OverrideInitialStateOption" in menu
assert "option.DoAction == CustomClick" in menu
assert "new InitialStateOption(" in menu
assert "new InitialStateOption(" not in entry
assert "NativeStartingLifeMenus.TryAttach(manager" not in origin
print("PASS: direct SandBoxNewGame override, default native startup, skinned controls, Escape and opt-out")
