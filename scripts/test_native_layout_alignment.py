#!/usr/bin/env python3
"""v0.44 static gate: measured and reversible CU-aligned native geometry.

The existence of widget bounds is not established by CI source inspection.
Actual in-game layout moves, click locations and responsive transitions
require diagnostics + screenshot-based acceptance on Bannerlord 1.5.7.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

r=Path(__file__).resolve().parent.parent
src=r/"Source/CalradiaUnified"
skin=(src/"CUNativeControlSkin.cs").read_text(encoding="utf-8")
aligned=(src/"CUNativeLayoutAlignment.cs").read_text(encoding="utf-8")
policy=(src/"CUNativeLayoutPolicy.cs").read_text(encoding="utf-8")
shell=(src/"CUNativeStateShell.cs").read_text(encoding="utf-8")
test=(r/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
csproj=(r/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")
xml=ET.parse(r/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml")

assert "CUNativeLayoutAlignment.Tick(screen," in shell
assert "CUNativeLayoutAlignment.Clear();" in shell
assert "CUNativeLayoutAlignment.IsWideRailSafe(stageName)" in shell
assert 'internal static Widget GetGameOwnedRoot(object view,string stage)' in skin
assert 'root=CUNativeControlSkin.GetGameOwnedRoot(view,nextStage)' in aligned
assert '"_currentStageView"' in aligned
assert 'CUNativeWidgetSkinPolicy.ExpectedView(nextStage)' in aligned
assert "SandboxCreatorOverlay.HasAcceptedCustomStart" in aligned
assert 'CUNativeLayoutPolicy.IsViewportSupported(width,height)' in aligned
assert 'RootMatchesResolution(' in aligned
assert 'ReadProperty(widget,"GlobalPosition")' in aligned
assert 'ReadProperty(widget,"Size")' in aligned
assert 'ReadProperty(widget,"MeasuredSize")' in aligned
assert 'TryReadFloat(size,"X",out w)' in aligned
assert 'TryReadFloat(size,"Y",out h)' in aligned
assert 'ActualWidth' not in aligned.split('private static bool TryMeasure(')[1] or 'NOT WPF-style ActualWidth' in aligned
assert 'GetAllChildrenRecursive()' in aligned
assert 'DepthUnder(widget,gameRoot)' in aligned
assert 'if(depth<1 || depth>2)continue;' in aligned
assert 'ContentScore(widget,nativeStage)' in aligned
assert 'CUNativeLayoutPolicy.TryPlanPrimary(' in aligned
assert 'CUNativeLayoutPolicy.FitsPrimary(' in aligned
assert 'CUNativeLayoutPolicy.TryPlanFooter(' in aligned
assert 'CUNativeLayoutPolicy.CanSkin' not in aligned
assert 'CUNativeWidgetSkinPolicy.CanSkinClanTextInput(' in aligned
assert 'CUNativeWidgetSkinPolicy.CanSkinDifficultySlider(' in aligned
assert 'IsProtected(type)' in aligned
assert 'IsWideRailSafe(string stageName)' in aligned
assert 'wide_chapter_rail_hidden=true' in aligned
assert 'wide_chapter_rail_permitted=true' in aligned

# No native screen, stage or callback is replaced to change the look.
for forbidden in (
    "new CharacterCreationScreen(", "new BodyGeneratorView(",
    "new SceneView(", "RemoveStage<", ".NextStage(", ".PreviousStage(",
    ".SetSkillValue(", ".SetFocus(", ".SetTraitLevel(",
    ".SetAttributeValue(", ".SelectCulture(", ".SetBanner(",
    "ParentWidget=", ".AddChild(", ".RemoveChild(",
    "Command.Click=", ".LoadMovie(", ".LoadScene(",
):
    assert forbidden not in aligned,forbidden

# Native views keep their original positions when unmeasurable, too
# crowded, outside bounds or on compact/unsupported screen modes.
assert "MaxContentShift = 256" in policy
assert "MaxFooterShift = 64" in policy
assert "HeaderBottom = 96" in policy
assert "ContentTop = 116" in policy
assert "FooterReserve = 93" in policy
assert "WideLeftInset = 260" in policy
assert "WideRightInset = 358" in policy
assert "WideThreshold = 1600" in policy
assert "MinimumWidth = 1280" in policy
assert "MinimumHeight = 720" in policy
assert 'stage=="CharacterCreationNarrativeStage"' in policy
assert 'stage=="CharacterCreationClanNamingStage"' in policy
assert 'stage=="CharacterCreationReviewStage"' in policy
assert 'stage=="CharacterCreationOptionsStage"' in policy
p=policy.split("public static bool CanMovePrimaryPanel",1)[1].split(
    "public static bool CanAlignNativeFooter",1)[0]
assert 'CharacterCreationBannerEditorStage' not in p
assert 'CharacterCreationFaceGeneratorStage' not in p
assert 'CharacterCreationCultureStage' not in p

# Every actual widget move is atomic, recorded, fully reversible.
assert 'x.SetValue(widget,ConvertNumber(originalX+dx,' in aligned
assert 'y.SetValue(widget,ConvertNumber(originalY+dy,' in aligned
assert 'saved.Add(new SavedOffset(widget,x,y,oldX,oldY));' in aligned
assert 'item.XProperty.SetValue(item.Widget,' in aligned
assert 'item.YProperty.SetValue(item.Widget,' in aligned
assert 'Input.IsKeyReleased(InputKey.F10)' in aligned
assert 'CUNativeLayoutAlignment.Clear();' in shell.split(
    "private static void Detach()",1)[1]
assert "cu_native_layout_content_aligned" in aligned
assert "cu_native_layout_content_verified" in aligned
assert "cu_native_layout_native_preserved" in aligned
assert "cu_native_layout_footer_aligned" in aligned
assert "cu_native_layout_restored" in aligned
assert "cu_native_layout_alignment_failed" in aligned

# Original CU chrome is read-only. The measured-safe rail must not
# insert a duplicate native Back/Continue widget on top of game controls.
assert len(list(xml.iter("ButtonWidget")))==0
assert "CUNativeLayoutPolicy.cs" in csproj
assert "native narrative content clears CU wide sidebar/header" in test
assert "native banner, FaceGen and culture panels are never moved" in test
assert "native root must match the physical unscaled viewport" in test
assert "native 1366 content receives only measured small corrections" in test
assert "native Back footer matches CU left 29px and bottom 24px" in test
print("PASS: guarded measured native content and footer layout, safe wide rail, reversible F10 and real game-owned callbacks (STATIC ONLY)")
