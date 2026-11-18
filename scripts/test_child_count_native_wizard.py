#!/usr/bin/env python3
"""v0.27 static regression: the displayed count must equal saved child count.
Source checks cannot establish click behavior or in-game family/save results.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parent.parent
source=root/"Source/CalradiaUnified"
profile=(source/"SandboxOriginProfile.cs").read_text()
ui=(source/"SandboxCreatorOverlay.cs").read_text()
family=(source/"SandboxFamilyCreator.cs").read_text()
behavior=(source/"SandboxOriginBehavior.cs").read_text()
xml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").getroot()

assert "public void ResetForNewCampaign()" in profile
assert "SpouseEnabled=false;" in profile
assert "ChildCount=0;" in profile
assert 'StartingFiefMode="Native";' in profile
assert "profile.ResetForNewCampaign();" in ui

# The old code changed the displayed slider but required a separate
# Confirm amount click. Slider setter and +/- must write the profile now.
assert "int next=Math.Max(NumberMinimum" in ui
assert "if (!Write(Current.Key,next.ToString(" in ui
assert "if (next==pendingNumber) return;" in ui
assert "public void ExecuteIncrease()" in ui and "PendingNumber=" in ui
assert "public void ExecuteDecrease()" in ui
assert 'SetStatus(key=="ChildCount" ?' in ui
assert 'OnPropertyChangedWithValue(ChildPreview,"ChildPreview")' in ui
assert "public string ChildPreview" in ui
assert 'DiagnosticJournal.Event("sandbox_creator_family_plan"' in ui
assert '"|child_count="+profile.ChildCount' in ui
assert 'candidate.ChildCount > 0 && !candidate.SpouseEnabled' in ui
assert "for (int i = 0; i < profile.ChildCount; i++)" in family

# Only mod-specific start choices here. Native culture, character body,
# origins/background, age and difficulty are Bannerlord's subsequent screens.
buttons=[w.get("Command.Click") for w in xml.iter("ButtonWidget")]
assert "ExecuteAppearance" not in buttons
for step in ("ExecuteIdentity","ExecuteHousehold","ExecuteResources",
             "ExecuteLocation","ExecuteReview"):
    assert buttons.count(step)==1,step
assert "ExecuteApplyAndContinue" in buttons
assert len(buttons)>=18
assert any(w.get("Text")=="@ChildPreview" for w in xml.iter("TextWidget"))
assert any(w.get("Text")=="PART 1 OF 2: STARTING CONDITIONS"
           for w in xml.iter("TextWidget"))
assert any(w.get("Text")=="NEXT: NATIVE CREATOR"
           for w in xml.iter("TextWidget"))
assert 'candidate.NativeStageEnabled=false;' in ui
stage=(source/"CUNativeOriginReviewStage.cs").read_text()
assert '[CharacterCreationStageView(typeof(CUNativeOriginReviewStage))]' in stage
assert sum(p.read_text().count("[CharacterCreationStageView(")
           for p in source.glob("*.cs")) == 1
assert "CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))" not in stage
assert 'DiagnosticJournal.Event("sandbox_start_contract"' in behavior
print("PASS: saved numeric values, fresh child count, five nonduplicated CU steps and explicit native wizard transition (STATIC ONLY)")
