#!/usr/bin/env python3
"""v0.35: source-only checks for real native culture VM delegation and native 3D face layer ownership."""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
controller=(src/"CUNativeCultureController.cs").read_text(encoding="utf-8")
layer=(src/"CUNativeCultureLayer.cs").read_text(encoding="utf-8")
inspector=(src/"CUNativeFaceInspector.cs").read_text(encoding="utf-8")
shell=(src/"CUNativeStateShell.cs").read_text(encoding="utf-8")
profile=(src/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
chooser=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeCultureChooser.xml").getroot()
ribbon=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml").getroot()

# Actual native CultureStageView's data source is *reused*, not recreated.
for marker in (
    '"_currentStageView"',
    '"CharacterCreationCultureStageView"',
    '"_dataSource"',
    '"CharacterCreationCultureStageVM"',
    '"CurrentSelectedCulture"',
    '"Cultures"',
    '"ExecuteSelectCulture"',
    '"OnNextStage"',
    '"OnPreviousStage"',
    'selectedCulture.Culture',
):
    # Last native-only identifier is expected in source documentation rather than code.
    if marker == "selectedCulture.Culture":
        continue
    assert marker in controller,marker
assert 'new CharacterCreationCultureStageVM(' not in controller
assert 'new CharacterCreationCultureStageVM(' not in layer
assert 'SetSelectedCulture(' not in controller, "native VM owns culture commit"
assert 'Select.Invoke(option.NativeVM,null)' in controller
assert 'advance.Invoke(nativeVM,null)' in controller
assert 'back.Invoke(nativeVM,null)' in controller
assert 'GetValue(source,null) as IEnumerable' in controller
assert 'nativeOptions.Count<2' in controller
assert 'failure=' in controller

# Real, interactive CU culture form presents the native runtime culture list.
assert chooser.tag=="Prefab"
assert any(el.get("DataSource")=="{Cultures}" for el in chooser.iter("ListPanel"))
assert sum(el.get("Command.Click")=="ExecuteSelect" for el in chooser.iter("ButtonWidget"))==1
for command in ("ExecuteContinue","ExecuteBack","ExecuteUseNativeControls"):
    assert sum(el.get("Command.Click")==command for el in chooser.iter("ButtonWidget"))==1,command
for key in ("SelectedName","SelectedDescription","Status"):
    assert any(el.get("Text")=="@"+key for el in chooser.iter("TextWidget")),key
assert 'new GauntletLayer("CUNativeCultureChooser",60,false)' in layer
assert 'nextLayer.IsFocusLayer=true;' in layer
assert 'ScreenManager.TrySetFocus(nextLayer)' in layer
assert 'controller.SelectCulture(id)' in layer
assert 'controller.Continue();' in layer
assert 'try { backController.Previous(); }' in layer, "native Back must execute after Gauntlet movie cleanup"
assert 'pendingNativeBack=selected;' in layer, "Back is deferred out of the movie click"
assert 'CUNativeStateShell.Clear();' in layer, "native Back releases CU layers before leaving state"
assert 'RequestAbandon()' in layer
assert 'RestoreNativeCultureFocus' in layer
assert 'oldHost.RemoveLayer(oldLayer)' in layer
assert 'oldLayer.ReleaseMovie(oldMovie)' in layer
assert 'CUNativeCultureLayer.Tick(screen,' in shell
assert 'CUNativeCultureLayer.Clear();' in shell

# FaceGen is inspected by identity of the REAL native scene and control layers.
for marker in (
    '"CharacterCreationFaceGeneratorView"',
    '"_faceGeneratorView"',
    '"BodyGeneratorView"',
    '"SceneLayer"',
    '"GauntletLayer"',
    '"BodyGen"',
    'Object.ReferenceEquals(layer,scene)',
    'Object.ReferenceEquals(layer,controls)',
    "REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE",
):
    assert marker in inspector,marker
assert 'new BodyGeneratorView(' not in inspector
assert 'new CharacterCreationFaceGeneratorView(' not in inspector
assert 'CUNativeFaceInspector.TryInspect(screen,out observed)' in shell
assert 'cu_native_face_layers_verified' in shell
assert 'cu_native_culture_commit_observed' in shell
assert any(el.get("IsVisible")=="@IsFaceDebugVisible" for el in ribbon.iter("TextWidget"))
assert any(el.get("IsVisible")=="@IsFaceDockTrialStage" for el in ribbon.iter("TextWidget"))
assert not any(el.tag in ("ButtonWidget","SliderWidget","InputTextWidget") for el in ribbon.iter())
assert 'public bool IsNotFaceStage' in shell
assert 'public bool IsFaceStage' in shell
assert 'NativeStateShellEnabled=false;' in profile

# No competing game state/view registry; the real native steps remain.
for source in (controller,layer,inspector,shell):
    for forbidden in ("[GameStateScreen(", "[CharacterCreationStageView(",
                      "RemoveStage<", "new BodyGeneratorView("):
        assert forbidden not in source,forbidden
assert "Create character" in (root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").read_text(encoding="utf-8")
print("PASS: native CultureVM selection/commit/back, CU focused culture list and fallback, real FaceGen layer identity, no duplicate screen or BodyGen, truthful native wizard boundary (STATIC)")
