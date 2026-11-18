#!/usr/bin/env python3
"""Source-only v0.31 opt-in native face workshop checks, not a live game test."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
src = root / "Source/CalradiaUnified"
workshop = (src / "CUFaceWorkshopStage.cs").read_text()
profile = (src / "SandboxOriginProfile.cs").read_text()
creator = (src / "SandboxCreatorOverlay.cs").read_text()
behavior = (src / "SandboxOriginBehavior.cs").read_text()
policy = (src / "NativeCreatorParityPolicy.cs").read_text()
csproj = (root / "Tests/PolicySmoke/PolicySmoke.csproj").read_text()
tests = (root / "Tests/PolicySmoke/Program.cs").read_text()
xml = ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUFaceWorkshop.xml").getroot()
assert xml.tag == "Prefab"
assert not list(xml.iter("ButtonWidget")), "experimental CU ribbon must not steal face input"
assert "[CharacterCreationStageView(typeof(CUFaceWorkshopStage))]" in workshop
assert "CUFaceWorkshopStageView : CharacterCreationStageViewBase" in workshop
assert "new CharacterCreationFaceGeneratorView(" in workshop
assert "faceEditor.GetLayers()" in workshop
assert "faceEditor.Tick(dt);" in workshop
assert "faceEditor.SetGenericScene(scene)" in workshop
assert "((ICharacterCreationStageListener)faceEditor)" in workshop
assert "faceEditor.LoadEscapeMenuMovie()" in workshop
assert "faceEditor.ReleaseEscapeMenuMovie()" in workshop
assert "summaryLayer.IsFocusLayer = false;" in workshop
assert 'summaryLayer.LoadMovie("CUFaceWorkshop", summaryVM)' in workshop
assert "new BodyGeneratorView(" not in workshop
assert "HeroCreator" not in workshop
assert "RemoveStage" not in behavior
assert "NativeFaceWorkshopEnabled=false;" in profile
assert 'case "NativeFaceWorkshopEnabled":' in profile
assert '"NativeFaceWorkshopEnabled=" +' in profile
assert '"NativeFaceWorkshopEnabled",' in creator
assert "candidate.NativeFaceWorkshopEnabled &&" in creator
assert "candidate.NativeIntegrationReviewEnabled" in creator
assert "CanAddFaceWorkshop(" in behavior
assert "manager.AddStage(new CUFaceWorkshopStage())" in behavior
assert "cu_face_workshop_attached" in behavior
assert "cu_face_workshop_skipped" in behavior
assert "cu_face_workshop_native_parity" in behavior
assert "NativeCreatorParityPolicy.cs" in csproj
assert "native creator requires all seven game-owned stages" in tests
assert len(re.findall(r'"CharacterCreation\w+Stage"', policy)) >= 7
assert sum(p.read_text().count("[CharacterCreationStageView(")
           for p in src.glob("*.cs")) == 2
assert all("CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))"
           not in p.read_text() for p in src.glob("*.cs"))
names=[n.get("Text","")[1:] for n in xml.iter("TextWidget")
       if n.get("Text","").startswith("@")]
assert set(names) == {"Title", "Role", "Culture", "Household", "Notice"}
for key in names:
    assert re.search(r"public string\s+"+key+r"\b",workshop),key
print("PASS: native editable face view composition, opt-in exclusivity, seven-stage parity, nonfocus XML and no native-view collision (STATIC)")
