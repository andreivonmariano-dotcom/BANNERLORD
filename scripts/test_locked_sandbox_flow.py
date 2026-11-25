#!/usr/bin/env python3
"""v0.32: lock CU Sandbox five-page UX/transition contract.

Source-only regression gate. This does not prove a single-screen Bannerlord
runtime and intentionally rejects dishonest 'Begin Campaign' UI wording in
the still-dual-screen v0.31 baseline.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
overlay=(src/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
menu=(src/"SandboxMainMenuEntry.cs").read_text(encoding="utf-8")
profile=(src/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
behavior=(src/"SandboxOriginBehavior.cs").read_text(encoding="utf-8")
options=(src/"SandboxNativeStartOptions.cs").read_text(encoding="utf-8")
native_review=(src/"CUNativeOriginReviewStage.cs").read_text(encoding="utf-8")
native_face=(src/"CUFaceWorkshopStage.cs").read_text(encoding="utf-8")
prefab=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").getroot()
doc=(root/"docs/V032_LOCKED_SANDBOX_CREATOR_FLOW.md").read_text(encoding="utf-8")

pages=["Identity","Household","Resources","Location","Review"]
m=re.search(r"private static readonly string\[\] Pages\s*=\s*\{([^}]+)\}",overlay)
assert m, "missing fixed five-page order"
assert re.findall(r'"([^"]+)"',m.group(1))==pages, "CU tab sequence changed"

actions=["ExecuteIdentity","ExecuteHousehold","ExecuteResources",
         "ExecuteLocation","ExecuteReview"]
all_buttons=[w.get("Command.Click") for w in prefab.iter("ButtonWidget")]
for index,(page,action) in enumerate(zip(pages,actions)):
    buttons=[w for w in prefab.iter("ButtonWidget")
             if w.get("Command.Click")==action]
    assert len(buttons)==1,(page,"duplicate/missing tab")
    assert buttons[0].get("IsSelected")=="@Is"+page+"Selected",page
    assert "public void "+action+"() {Switch(\""+page+"\");}" in overlay,page
assert [all_buttons.index(action) for action in actions]==sorted(
       all_buttons.index(action) for action in actions),"tab order changed"
assert len(all_buttons)>=18

labels=[n.get("Text","") for n in prefab.iter("TextWidget")]
for label in ("01   Starting life","02   Bloodline","03   Fortune",
              "04   Land & home","05   Review your house"):
    assert label in labels,label
assert "Starting conditions" in labels
assert "Create character" in labels
assert "Begin Campaign" not in labels, "native wizard still follows in this candidate"
assert "ExecuteApplyAndContinue" in all_buttons
assert all_buttons.count("ExecuteApplyAndContinue")==1
assert "ExecuteNextField" in all_buttons
assert "ExecutePreviousField" in all_buttons
assert "ExecuteReturnToNative" in all_buttons
assert "@PageProgress" in labels
assert "if (page==\"Review\") return false;" in overlay
assert "if(selectedIndex+1<count)" in overlay
assert "if(pageIndex>0)" in overlay

# Explicit and safe startup, cancellation, and per-campaign reset.
assert "SandBoxNewGame" in menu
assert "OverrideInitialStateOption" in menu
assert "if (!profile.OverhauledCreatorEnabled)" in overlay
assert "if (nativeStart != null) nativeStart();" in overlay
assert "profile.ResetForNewCampaign();" in overlay
assert "profile.Enabled=false;" in overlay
assert "SandboxCreatorOverlay.Cancel();" in overlay
assert "launchQueued = false;" in overlay
assert "if (ScreenManager.TopScreen == this &&" in overlay
assert "Input.IsKeyReleased(InputKey.Escape)" in overlay
assert "if (!profile.Enabled ||" in overlay
assert "MBGameManager.StartNewGame(new CUSandboxNoIntroGameManager(" in overlay
assert "candidate.Enabled=true;" in overlay
assert "SandboxNativeStartOptions.TryPrepare(candidate," in overlay
assert "SandboxCreatorOverlay.Accept();" in overlay
assert "candidate.Save(SandboxOriginProfile.UserPath,out error)" in overlay
assert "candidate.NativeStageEnabled=false;" in overlay
assert "if (candidate.NativeFaceWorkshopEnabled &&" in overlay
assert "candidate.NativeIntegrationReviewEnabled)" in overlay
assert "if (candidate.ChildCount > 0 && !candidate.SpouseEnabled)" in overlay
assert "if (candidate.SiblingCount > 0)" in overlay
assert "SandboxFamilyHomePolicy.ValidateRequest(" in overlay
assert "SandboxFiefPolicy.ValidateRequest(" in overlay
assert "candidate.ChildCount" in overlay
assert "SetStatus(key==\"ChildCount\" ?" in overlay

# No accidental default activation of the old unsafe, distinct, optional
# native creator views. The stock engine remains owner of the real FaceGen.
for disabled in ("NativeStageEnabled=false;",
                 "NativeIntegrationReviewEnabled=false;",
                 "NativeFaceWorkshopEnabled=false;"):
    assert disabled in profile,disabled
assert "CharacterCreationStageView(typeof(CUFaceWorkshopStage))" in native_face
assert "CharacterCreationStageView(typeof(CUNativeOriginReviewStage))" in native_review
assert not any("CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))"
               in p.read_text(encoding="utf-8") for p in src.glob("*.cs"))

# Explicit real engine options and postcreation real-Hero guardrails.
assert "AdvancedStartOptionsManager.CreateCampaignStartOptions()" in options
assert "types.SetValue(type);" in options
assert "phase != 9" in behavior
assert "applied = true;" in behavior
assert "SandboxFamilyCreator.TryCreate(profile,hero)" in behavior
assert "SandboxFamilyHome.TryPlace(profile,hero,fiefOutcome)" in behavior
assert "SandboxStartContract.Validate(" in behavior
assert "native culture" in doc.lower()
assert "no second" in doc.lower() or "second native" in doc.lower()

print("PASS: frozen CU five-page order, labels, navigation, saved requests, opt-out, one launch, native fallback and truthful v0.31 handoff (STATIC)")
