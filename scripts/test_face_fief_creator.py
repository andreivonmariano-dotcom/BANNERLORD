#!/usr/bin/env python3
"""v0.22 SOURCE-ONLY audit. Does not prove 3D face controls or fief results in-game."""
from pathlib import Path
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parent.parent
src=ROOT/"Source/CalradiaUnified"
overlay=(src/"SandboxCreatorOverlay.cs").read_text()
profile=(src/"SandboxOriginProfile.cs").read_text()
face=(src/"CUFaceGeneratorStageView.cs").read_text()
fief=(src/"SandboxStartingFief.cs").read_text()
behavior=(src/"SandboxOriginBehavior.cs").read_text()
native=(ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml")
header=(ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUFaceStageHeader.xml")
all_buttons=[w.get("Command.Click") for w in ET.parse(native).getroot().iter("ButtonWidget")]
for action in ("ExecuteIdentity","ExecuteAppearance","ExecuteHousehold","ExecuteProgression",
               "ExecuteEquipment","ExecuteResources","ExecuteLocation","ExecuteReview"):
    assert action in all_buttons,action
assert len(all_buttons)>=20
xmlRoot=ET.parse(native).getroot()
for action, page in (("ExecuteIdentity","Identity"),
                     ("ExecuteAppearance","Appearance"),
                     ("ExecuteHousehold","Household"),
                     ("ExecuteProgression","Progression"),
                     ("ExecuteEquipment","Equipment"),
                     ("ExecuteResources","Resources"),
                     ("ExecuteLocation","Location"),
                     ("ExecuteReview","Review")):
    controls=[w for w in xmlRoot.iter("ButtonWidget")
              if w.get("Command.Click")==action]
    assert len(controls)==1,action
    assert controls[0].get("IsSelected")=="@Is"+page+"Selected",action
    assert "Is"+page+"Selected" in overlay
assert any(w.get("Text")=="@PageProgress" for w in xmlRoot.iter("TextWidget"))

assert "NativeStageEnabled" in overlay and "candidate.NativeStageEnabled=false;" not in overlay
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
assert "SandboxOriginProfile.UserPath" in face
assert "CharacterCreationFaceGeneratorView" in face
assert "CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))" in face
assert "base.GetLayers()" in face and "base.OnFinalize()" in face
assert "cu_face_stage_native_wrapped" in face
assert ET.parse(header).getroot().tag=="Prefab"
assert any(w.get("Text")=="@Title" for w in ET.parse(header).getroot().iter("TextWidget"))
assert "SandBox.View" in (src/"CalradiaUnified.csproj").read_text()
assert "SandBox.GauntletUI" in (src/"CalradiaUnified.csproj").read_text()
print("PASS: native FaceGen wrapper, eight tabs, explicit fief validation and ownership event guards (STATIC ONLY)")
