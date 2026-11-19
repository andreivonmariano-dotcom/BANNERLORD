#!/usr/bin/env python3
"""v0.34 native-state compositor prototype contract, source-only.
Never interpret a green result as proof that a game renders a CU shell.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
src = root / "Source/CalradiaUnified"
shell = (src / "CUNativeStateShell.cs").read_text(encoding="utf-8")
policy = (src / "NativeStateShellPolicy.cs").read_text(encoding="utf-8")
profile = (src / "SandboxOriginProfile.cs").read_text(encoding="utf-8")
creator = (src / "SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
submodule = (src / "SubModule.cs").read_text(encoding="utf-8")
csproj = (root/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")
tests = (root/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
prefab = ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml").getroot()
assert prefab.tag == "Prefab"

# This layer lives on the REAL game-owned CharacterCreationScreen.
assert "ScreenManager.TopScreen as CharacterCreationScreen" in shell
assert "Game.Current.GameStateManager.ActiveState as CharacterCreationState" in shell
assert "screen.AddLayer(nextLayer)" in shell
assert "new GauntletLayer(" in shell
assert 'nextLayer.LoadMovie(\n                        "CUNativeStateShell", nextVM)' in shell
assert "nextLayer.IsFocusLayer = false;" in shell
assert "screen.HasLayer(nextLayer)" in shell
assert "oldHost.IsFinalized" in shell
assert "oldHost.RemoveLayer(oldLayer)" in shell
assert "oldLayer.ReleaseMovie(oldMovie)" in shell
assert "CUNativeStateShell.Tick();" in submodule
assert "CUNativeStateShell.Clear();" in submodule
assert "CUNativeStateShell : " not in shell, "must not replace native screen subclass"
assert "GameStateScreen(" not in shell, "do not register second CharacterCreationScreen"
assert "CharacterCreationStageView(" not in shell, "do not register native stage view overrides"
assert "new BodyGeneratorView(" not in shell, "must not fabricate second face editor"
assert "HeroCreator" not in shell, "no custom hero creation in stage shell"

# The native scene, selected culture and game stage own actual state.
assert "manager.CharacterCreationContent.SelectedCulture" in shell
assert "manager.CurrentStage" in shell
for s in ("CharacterCreationCultureStage","CharacterCreationFaceGeneratorStage",
          "CharacterCreationNarrativeStage","CharacterCreationBannerEditorStage",
          "CharacterCreationClanNamingStage","CharacterCreationReviewStage",
          "CharacterCreationOptionsStage"):
    assert s in policy,s
assert "ChapterForStage(stageName)" in shell
assert "DetailForStage(stageName)" in shell
assert "UsesNativeLiveFace(" in policy
assert 'NativeStateShellPolicy.CanAttach(' in shell

# The extra layer should not intercept native 3D camera or culture input.
assert not list(prefab.iter("ButtonWidget"))
assert not list(prefab.iter("SliderWidget"))
assert not list(prefab.iter("InputTextWidget"))
labels = [node.get("Text","")[1:] for node in prefab.iter("TextWidget")
          if node.get("Text","").startswith("@")]
assert set(labels)=={"Title","Chapter","NativeStep","Culture","Role","Household","Notice","FaceStatus","Label","Status","ViewportCapability","FaceDockStatus"}
for key in labels:
    assert re.search(r"public string\s+"+key+r"\b",shell),key

# Explicit off-by-default profile gate, and incompatible add-on stages blocked.
assert "public bool NativeStateShellEnabled;" in profile
assert "NativeStateShellEnabled=false;" in profile
assert 'case "NativeStateShellEnabled":' in profile
assert '"NativeStateShellEnabled=" +' in profile
assert 'new Field("Identity","NativeStateShellEnabled",' in creator
assert 'key == "NativeStateShellEnabled" ||' in creator
assert 'key == "NativeFaceDockTrialEnabled")' in creator
assert 'if (candidate.NativeStateShellEnabled &&' in creator
assert 'candidate.NativeFaceWorkshopEnabled ||' in creator
assert 'candidate.NativeIntegrationReviewEnabled)' in creator
assert "NativeStateShellPolicy.cs" in csproj
assert "native CU shell maps culture to Your Character" in tests
assert "native CU shell permits only explicit native-state opt-in" in tests
assert 'NEXT: NATIVE CREATOR' in (
    root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").read_text(encoding="utf-8")
print("PASS: opt-in CU ribbon attached to true character-creation screen, native stages untouched, live culture binding, safe lifecycle, 12 policy checks and truthful dual-screen status (STATIC)")
