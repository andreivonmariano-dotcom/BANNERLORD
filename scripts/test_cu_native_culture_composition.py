#!/usr/bin/env python3
"""v0.35 culture replacement and real FaceGen inspection, SOURCE-ONLY.

This verifies native-delegate intent and the specific Gauntlet widgets.
It cannot prove that the real game loads our movie, accepts mouse input,
generates faces, or positions the 3D model in a right-hand viewport.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
S = ROOT/"Source/CalradiaUnified"
controller=(S/"CUNativeCultureController.cs").read_text(encoding="utf-8")
layer=(S/"CUNativeCultureLayer.cs").read_text(encoding="utf-8")
inspector=(S/"CUNativeFaceInspector.cs").read_text(encoding="utf-8")
shell=(S/"CUNativeStateShell.cs").read_text(encoding="utf-8")
behavior=(S/"SandboxOriginBehavior.cs").read_text(encoding="utf-8")
menu=(S/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
profile=(S/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
ui=ET.parse(ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeCultureChooser.xml").getroot()
ribbon=ET.parse(ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml").getroot()

# Do not instantiate a second native VM. Access the ONE owned by the
# CharacterCreationCultureStageView using exact private pinned names.
for key in ('"_currentStageView"','"CharacterCreationCultureStageView"',
            '"_dataSource"','"CharacterCreationCultureStageVM"',
            '"Cultures"','"CurrentSelectedCulture"','"CultureID"',
            '"NameText"','"DescriptionText"','"ExecuteSelectCulture"',
            '"OnNextStage"','"OnPreviousStage"'):
    assert key in controller,key
assert "selectMethod=itemType.GetMethod" in controller
assert "option.Select.Invoke(option.NativeVM,null)" in controller
assert "advance.Invoke(nativeVM,null)" in controller
assert "back.Invoke(nativeVM,null)" in controller
assert "SetSelectedCulture(" not in controller, "must not bypass native VM finalize path"
assert "UpdatePlayerCharacterBodyProperties(" not in controller, "native choice owns body seed"
assert "new CharacterCreationCultureStageVM(" not in controller
assert "try" in controller and "native_culture_reflection:" in controller
assert "nativeOptions.Count<2" in controller
assert "native_culture_item_incomplete" in controller

# Gauntlet actually presents native-derived choices and routes clicks to VM.
lists=[x for x in ui.iter("ListPanel") if x.get("DataSource")=="{Cultures}"]
assert len(lists)==1
item_buttons=[x for x in lists[0].iter("ButtonWidget")
              if x.get("Command.Click")=="ExecuteSelect"]
assert len(item_buttons)==1
assert item_buttons[0].get("IsSelected")=="@IsSelected"
btns=[x.get("Command.Click") for x in ui.iter("ButtonWidget")]
assert set(btns)=={"ExecuteSelect","ExecuteBack","ExecuteContinue",
                   "ExecuteUseNativeControls"},btns
labels=[x.get("Text","") for x in ui.iter("TextWidget")]
for field in ("@Name","@SelectedName",
              "@SelectedDescription","@Status","@Notice"):
    # Native culture IDs remain internal, avoiding duplicate raw text
    # that visibly overlapped culture labels in screenshots.
    assert field in labels,field
assert "new MBBindingList<CUNativeCultureChoiceVM>()" in layer
assert "controller.SelectCulture(id)" in layer
assert "controller.Continue()" in layer
assert "requestBack(controller)" in layer
assert "backController.Previous()" in layer
assert "CUNativeStateShell.Clear();" in layer
assert "pendingNativeBack=selected" in layer
assert "Input.IsKeyReleased(InputKey.Escape)" in layer
assert "failedHost=nativeScreen" in layer
assert "RestoreNativeCultureFocus(nativeScreen)" in layer
assert "CUNativeStateShell.UseNativeFallback(nativeScreen);" in layer
assert "skippedHost=nativeScreen;" in shell
assert "cu_native_culture_full_fallback" in shell
assert 'candidate.Name=="CharacterCreationCulture"' in layer
assert 'nextLayer.LoadMovie("CUNativeCultureChooser",nextVM)' in layer
assert "screen.HasLayer" not in layer or "nativeScreen.HasLayer(nextLayer)" in layer
assert "CUNativeCultureLayer.Tick(screen," in shell
assert "CUNativeCultureLayer.Clear();" in shell
assert "CUNativeStateShell.Clear();" in behavior
assert "cu_native_shell_released_before_finalization" in behavior

# FaceGen remains native, one live FaceGen SceneLayer and UI layer;
# no fake new BodyGeneratorView or second CharacterCreationScreen.
for key in ('"_currentStageView"','"CharacterCreationFaceGeneratorView"',
            '"_faceGeneratorView"','"BodyGeneratorView"',
            '"SceneLayer"','"GauntletLayer"','"BodyGen"'):
    assert key in inspector,key
assert "Object.ReferenceEquals(layer,scene)" in inspector
assert "Object.ReferenceEquals(layer,controls)" in inspector
assert "REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE" in inspector
assert "CUNativeFaceInspector.TryInspect(screen,out observed)" in shell
assert "cu_native_face_layers_verified" in shell
assert 'relocated=false' in shell
assert not list(ribbon.iter("ButtonWidget")), "face-state ribbon must not intercept input"
assert any(x.get("Text")=="@FaceStatus" and x.get("IsVisible")=="@IsFaceDebugVisible"
           for x in ribbon.iter("TextWidget"))
assert any(x.get("Text")=="@FaceDockStatus" and x.get("IsVisible")=="@IsFaceDockTrialStage"
           for x in ribbon.iter("TextWidget"))
assert all(not re.search(r'new\s+(?:BodyGeneratorView|CharacterCreationFaceGeneratorView)\s*\(',src.read_text(encoding="utf-8")) for src in
           (S/"CUNativeCultureLayer.cs",S/"CUNativeCultureController.cs",S/"CUNativeFaceInspector.cs",S/"CUNativeStateShell.cs"))

# Explicit opt-in, no hidden default activation, and native-screen fallback.
assert "NativeStateShellEnabled=false;" in profile
assert 'if (candidate.NativeStateShellEnabled &&' in menu
assert "NativeStageEnabled=false;" in menu
assert "Create character" in (
    ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").read_text(encoding="utf-8")
assert not any("CharacterCreationStageView(typeof(CharacterCreationCultureStage))"
               in p.read_text(encoding="utf-8") for p in S.glob("*.cs"))
assert not any("GameStateScreen(typeof(CharacterCreationState))"
               in p.read_text(encoding="utf-8") for p in S.glob("*.cs"))
print("PASS: CU culture UI delegates to live native culture VM, preserves native back/confirm and real FaceGen layers; right-hand 3D relocation remains unimplemented (STATIC)")
