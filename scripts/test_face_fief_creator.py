#!/usr/bin/env python3
"""v0.23 SOURCE-ONLY audit. Does not prove 3D face controls or fief results in-game."""
from pathlib import Path
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parent.parent
src=ROOT/"Source/CalradiaUnified"
overlay=(src/"SandboxCreatorOverlay.cs").read_text()
profile=(src/"SandboxOriginProfile.cs").read_text()
family=(src/"SandboxFamilyCreator.cs").read_text()
fief=(src/"SandboxStartingFief.cs").read_text()
behavior=(src/"SandboxOriginBehavior.cs").read_text()
native=(ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml")
# The unsafe FaceGen stage must be absent from the next build.
all_buttons=[w.get("Command.Click") for w in ET.parse(native).getroot().iter("ButtonWidget")]
for action in ("ExecuteIdentity","ExecuteHousehold",
               "ExecuteResources","ExecuteLocation","ExecuteReview","ExecuteToggleAdvanced"):
    assert action in all_buttons,action
assert len(all_buttons)>=18
xmlRoot=ET.parse(native).getroot()
for action, page in (("ExecuteIdentity","Identity"),
                     ("ExecuteHousehold","Household"),
                     ("ExecuteResources","Resources"),
                     ("ExecuteLocation","Location"),
                     ("ExecuteReview","Review")):
    controls=[w for w in xmlRoot.iter("ButtonWidget")
              if w.get("Command.Click")==action]
    assert len(controls)==1,action
    assert controls[0].get("IsSelected")=="@Is"+page+"Selected",action
    assert "Is"+page+"Selected" in overlay
assert any(w.get("Text")=="@PageProgress" for w in xmlRoot.iter("TextWidget"))

assert "candidate.NativeStageEnabled=false;" in overlay
assert not (src/"CUFaceGeneratorStageView.cs").exists()
assert not (ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUFaceStageHeader.xml").exists()
stage=(src/"CUNativeOriginReviewStage.cs").read_text()
assert '[CharacterCreationStageView(typeof(CUNativeOriginReviewStage))]' in stage
registered=[p.read_text() for p in src.glob("*.cs")]
assert sum(t.count('[CharacterCreationStageView(') for t in registered)==2
assert '[CharacterCreationStageView(typeof(CUFaceWorkshopStage))]' in (
    src/"CUFaceWorkshopStage.cs").read_text()
assert all('CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))'
           not in t for t in registered)
assert 'CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))' not in stage
assert 'CharacterCreationStageView(typeof(CharacterCreationCultureStage))' not in stage
assert "IsCoupleSuitableForMarriage(player,spouseHero)" in family
assert "SandboxFamilyPolicy.ResolveSpouseFemale(" in family
assert "IsCoupleSuitableForMarriage(player,spouseHero)" in family
assert "cu_family_verification" in family
assert "cu_family_verification" in family
assert 'new Field("Appearance","NativeStageEnabled"' not in overlay
assert "StartingFiefMode" in overlay and "StartingFiefId" in overlay
assert "AllowStartingFiefTransfer" in overlay
for key in ("StartingFiefMode","StartingFiefId","AllowStartingFiefTransfer"):
    assert key in profile
assert "SandboxFiefPolicy.ValidateRequest" in overlay
assert "SandboxStartingFief.TryApply(profile,hero)" in behavior
assert "sandbox_start_fief_result" in behavior
assert "ChangeOwnerOfSettlementAction.ApplyByGift" in fief
assert "target.OwnerClan != Clan.PlayerClan" in fief
assert "fief_outside_starting_kingdom" in (src/"SandboxFiefPolicy.cs").read_text()
refs=(src/"CalradiaUnified.csproj").read_text()
assert 'Reference Include="SandBox.View"' in refs
assert 'Reference Include="SandBox.GauntletUI"' in refs
assert "CharacterCreationReviewStageView" in stage
assert "CharacterCreationFaceGeneratorView" not in stage
assert any(w.get("Text")=="@FieldHelp" for w in xmlRoot.iter("TextWidget"))
assert any(w.get("Text")=="@AdvancedToggleLabel" for w in xmlRoot.iter("TextWidget"))
print("PASS: no FaceGen view hijack, five guided custom-start pages before native wizard, family eligibility and fief guards (STATIC ONLY)")
