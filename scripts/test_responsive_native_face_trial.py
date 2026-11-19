#!/usr/bin/env python3
"""v0.38 source contracts for responsive full-screen CU and F9-restorable
native FaceGen-only right viewport. Does not certify game rendering or
single-screen native stage finalization.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
prefabs=root/"Modules/CalradiaUnified/GUI/Prefabs"
profile=(root/"Source/CalradiaUnified/SandboxOriginProfile.cs").read_text(encoding="utf-8")
creator=(root/"Source/CalradiaUnified/SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
shell=(root/"Source/CalradiaUnified/CUNativeStateShell.cs").read_text(encoding="utf-8")
face=(root/"Source/CalradiaUnified/CUNativeFaceDockTrial.cs").read_text(encoding="utf-8")
policy=(root/"Source/CalradiaUnified/CUNativeChapterPolicy.cs").read_text(encoding="utf-8")
shellxml=ET.parse(prefabs/"CUNativeStateShell.xml").getroot()
for name in ("CUSandboxCreator.xml","CUNativeCultureChooser.xml"):
    prefab=ET.parse(prefabs/name).getroot()
    resizable=[el for el in prefab.iter("Widget") if
        el.get("WidthSizePolicy")=="StretchToParent" and
        el.get("HeightSizePolicy")=="StretchToParent" and
        el.get("MaxWidth")=="1820" and el.get("MaxHeight")=="960"]
    assert len(resizable)==1,(name,"responsive centered max-width layout")
    assert resizable[0].get("MarginLeft")=="24"
    assert resizable[0].get("MarginRight")=="24"
    assert resizable[0].get("MarginTop")=="20"
    assert resizable[0].get("MarginBottom")=="20"
    stretch=[el for el in prefab.iter() if
        el.get("WidthSizePolicy")=="StretchToParent" and
        el.get("MarginLeft") is not None and
        el.get("MarginRight") is not None]
    assert len(stretch)>=7,(name,"central panel expansion")
    right=[el for el in prefab.iter() if
        el.get("HorizontalAlignment")=="Right" and
        el.get("PositionXOffset","").startswith("-")]
    assert len(right)>=(2 if name=="CUSandboxCreator.xml" else 1),(name,"right aligned actions or summary")
    bottom=[el for el in prefab.iter() if
        el.get("VerticalAlignment")=="Bottom" and
        el.get("PositionYOffset","").startswith("-")]
    assert len(bottom)>=2,(name,"bottom anchored actions")
    assert not any(el.get("WidthSizePolicy")=="Fixed" and
        el.get("SuggestedWidth") in ("1280","1260") and
        el.get("HeightSizePolicy")=="Fixed" and
        el.get("SuggestedHeight") in ("700","696")
        for el in prefab.iter("Widget")),(name,"old fixed modal")

assert "NativeFaceDockTrialEnabled=false;" in profile
assert 'case "NativeFaceDockTrialEnabled":' in profile
assert '"NativeFaceDockTrialEnabled=" +' in profile
assert 'new Field("Identity","NativeFaceDockTrialEnabled",' in creator
assert 'key == "NativeFaceDockTrialEnabled")' in creator
assert 'candidate.NativeFaceDockTrialEnabled &&' in creator
assert '!candidate.NativeStateShellEnabled' in creator
assert 'F9' in creator
assert "NativeFaceDockTrialEnabled" in shell
assert "CUNativeFaceDockTrial.Tick(screen," in shell
assert "CUNativeFaceDockTrial.Clear();" in shell
assert 'public bool IsFaceDockTrialStage' in shell
assert 'public bool IsRailVisible' in shell
assert 'public string FaceDockStatus' in shell
assert any(el.get("IsVisible")=="@IsRailVisible" for el in shellxml.iter("Widget"))
assert any(el.get("Text")=="@FaceDockStatus" and
    el.get("IsVisible")=="@IsFaceDockTrialStage"
    for el in shellxml.iter("TextWidget"))

# Uses the SAME SceneLayer.SceneView and native game-owned UI widgets.
for mark in ("_currentStageView","CharacterCreationFaceGeneratorView",
             "_faceGeneratorView","BodyGeneratorView",
             'GetMovieIdentifier("FaceGen")','GetAllChildrenRecursive()',
             "nativeScene.SceneView.SetScale(",
             "nativeScene.SceneView.SetOffset(",
             "nativePanel.HorizontalAlignment=HorizontalAlignment.Left",
             "nativeExtensions.HorizontalAlignment=HorizontalAlignment.Left",
             "Input.IsKeyReleased(InputKey.F9)",
             "oldPanelAlignment","oldExtensionsAlignment",
             "nativeScene.SceneView.SetScale(new Vec2(1f,1f))",
             "nativeScene.SceneView.SetOffset(new Vec2(0f,0f))",
             "cu_real_face_dock_trial_applied",
             "cu_real_face_dock_toggle"):
    assert mark in face,mark
assert "new SceneLayer(" not in face
assert "new BodyGeneratorView(" not in face
assert "new CharacterCreationScreen(" not in face
assert "CharacterCreationStageView(" not in face
assert "GameStateScreen(" not in face
assert "NativeFacePanelPixels = 694" in policy
assert "CanTryNativeFaceDock(int width,int height)" in policy
assert "HasVerifiedRectangularSceneDock" in policy
assert "get { return false; }" in policy
assert 'NEXT: NATIVE CREATOR' in (prefabs/"CUSandboxCreator.xml").read_text(encoding="utf-8")
print("PASS: expandable 1366/1920 native-style CU controls and reversible original FaceGen-only right dock, no false persistent one-screen claim (STATIC)")
