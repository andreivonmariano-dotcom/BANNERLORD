#!/usr/bin/env python3
"""v0.43 strict STATIC native-VM-safe in-place widget styling audit.
Cannot prove visible native button colors, hit-testing or hover states in
Bannerlord. Live creator diagnostics must show actual nonzero widget counts.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

root=Path(__file__).resolve().parent.parent
s=root/"Source/CalradiaUnified"
skin=(s/"CUNativeControlSkin.cs").read_text(encoding="utf-8")
policy=(s/"CUNativeWidgetSkinPolicy.cs").read_text(encoding="utf-8")
shell=(s/"CUNativeStateShell.cs").read_text(encoding="utf-8")
face=(s/"CUNativeFaceDockTrial.cs").read_text(encoding="utf-8")
guide=(s/"CUGuidedCreatorVM.cs").read_text(encoding="utf-8")
xml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml").getroot()
proj=(root/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")
test=(root/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")

assert "CUNativeControlSkin.Tick(screen," in shell
assert "CUNativeControlSkin.Clear();" in shell
assert 'CUNativeWidgetSkinPolicy.cs' in proj
assert 'native skin targets game-owned story/house/legacy stages' in test
assert 'native skin never duplicates culture or modifies FaceGen' in test

for stage,view in [
("CharacterCreationNarrativeStage","CharacterCreationNarrativeStageView"),
("CharacterCreationBannerEditorStage","CharacterCreationBannerEditorView"),
("CharacterCreationClanNamingStage","CharacterCreationClanNamingStageView"),
("CharacterCreationReviewStage","CharacterCreationReviewStageView"),
("CharacterCreationOptionsStage","CharacterCreationOptionsStageView")
]:
    assert '"'+stage+'"' in policy
    assert '"'+view+'"' in policy
assert '"CharacterCreationCultureStage"' not in policy.split(
    'public static bool CanSkin')[1].split('public static string ExpectedView')[0]
assert '"CharacterCreationFaceGeneratorStage"' not in policy.split(
    'public static bool CanSkin')[1].split('public static string ExpectedView')[0]

# Verify exact original native root. Never select a different module's
# GauntletLayer, clone game state or invoke an editor transition.
for token in ('typeof(CharacterCreationScreen).GetField(',
              '"_currentStageView",InstanceFlags',
              '"_bannerEditorView",InstanceFlags',
              'typeof(GauntletMovieIdentifier)',
              'movie.Movie.RootWidget',
              'GetAllChildrenRecursive()'):
    # This result only checks source intent. Does not certify runtime fields.
    if token=="movie.Movie.RootWidget":
        assert 'found.Movie.RootWidget' in skin
    else:assert token in skin,token
assert "CUNativeWidgetSkinPolicy.ExpectedView(stage)" in skin
assert "field.Name.IndexOf(\"escape\"" in skin
assert "if(count==1)" in skin or "count==1 ?" in skin
assert "MaxWidgetsPerStage" in skin
assert "CUNativeWidgetSkinPolicy.IsProtectedWidget(type)" in skin
assert "new BodyGeneratorView(" not in skin
assert "new CharacterCreationScreen(" not in skin
assert "RemoveStage<" not in skin
assert ".NextStage(" not in skin
assert ".PreviousStage(" not in skin
assert ".SelectCulture(" not in skin
assert ".SetTraitLevel(" not in skin
assert ".SetSkillValue(" not in skin
assert ".SetFocus(" not in skin
assert ".SetAttributeValue(" not in skin

# Brush clones must be private, and all original data restored on stage
# changes, new campaigns, screen teardown and the visible F10 escape hatch.
assert 'Brush own=original.Clone();' in skin
assert 'Object.ReferenceEquals(own,original)' in skin
assert 'own.Color=Hex(' in skin
assert 'own.FontColor=Hex(' in skin
assert 'property.SetValue(widget,own,null);' in skin
assert 'changes.Add(new Change(widget,property,original));' in skin
assert 'item.Property.SetValue(item.Target,item.Original,null);' in skin
assert 'Input.IsKeyReleased(InputKey.F10)' in skin
assert 'if(++refreshTicks>=18)' in skin
assert 'HashSet<Widget> processedWidgets' in skin
assert '"cu_native_widget_skin_refreshed"' in skin
assert 'if(!HasTextChild(widget))return false;' in skin
assert 'foreach(Widget child in widget.GetAllChildrenRecursive())' in skin
assert 'CUNativeWidgetSkinPolicy.MaxWidgetsPerStage' in skin
assert 'Brush local=original.Clone();' in skin
assert 'local.FontColor=next;' in skin
assert "cu_native_widget_skin_applied" in skin
assert "cu_native_widget_skin_restored" in skin
assert "cu_native_widget_skin_unavailable" in skin
assert "if(!enabled) return;" in skin
assert "SandboxCreatorOverlay.HasAcceptedCustomStart" in skin
assert 'native_click_bindings_retained=true' in skin
assert 'CUNativeControlSkin.Clear();' in shell.split(
    'private static void Detach()')[1]

# UI retains genuine native buttons and custom FaceGen F9 scene rollback.
assert not list(xml.iter("ButtonWidget"))
assert "Restore();" in face and "InputKey.F9" in face
assert "HouseRevision++;" in guide
print("PASS: five game-owned native stages targeted for reversible private-button-brush and real-text styling, F10 and native fallback (STATIC ONLY)")
