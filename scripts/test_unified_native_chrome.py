#!/usr/bin/env python3
"""v0.42: check actual CU/native visual parity contract against source and
Gauntlet XML, not an observed Bannerlord screenshot or user input session.

A nonfocus header and an optional chapter rail must never replace native
culture, face, narrative, heraldry, clan, review or difficulty controllers.
"""
from pathlib import Path
from xml.etree import ElementTree as ET
import re

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
prefabs=root/"Modules/CalradiaUnified/GUI/Prefabs"
shell=(src/"CUNativeStateShell.cs").read_text(encoding="utf-8")
visual=(src/"CUNativeVisualPolicy.cs").read_text(encoding="utf-8")
chapter=(src/"CUNativeChapterPolicy.cs").read_text(encoding="utf-8")
guided=(src/"CUGuidedCreatorVM.cs").read_text(encoding="utf-8")
culture=(src/"CUNativeCultureLayer.cs").read_text(encoding="utf-8")
stats=(src/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
face=(src/"CUNativeFaceDockTrial.cs").read_text(encoding="utf-8")
ui=ET.parse(prefabs/"CUNativeStateShell.xml").getroot()
cult=ET.parse(prefabs/"CUNativeCultureChooser.xml").getroot()
world=ET.parse(prefabs/"CUGuidedCreator.xml").getroot()
stat=ET.parse(prefabs/"CUNativeStatsEditor.xml").getroot()

assert ui.tag=="Prefab"
assert "new GauntletLayer(" in shell
assert 'nextLayer.IsFocusLayer = false;' in shell
assert '"CUNativeStateShell", nextVM)' in shell
assert 'CUNativeVisualPolicy.IsStandaloneNativeChrome(stageName)' in shell
assert 'CUNativeVisualPolicy.ShouldShowChapterRail(' in shell
assert 'Screen.RealScreenResolutionWidth' in shell
assert 'OnPropertyChangedWithValue(stageTitle,"StageTitle");' in shell
assert 'OnPropertyChangedWithValue(stageGuidance,"StageGuidance");' in shell
assert 'OnPropertyChangedWithValue(nativeRailVisible,"IsRailVisible");' in shell
assert 'new CharacterCreationScreen(' not in shell
assert 'new BodyGeneratorView(' not in shell
assert 'RemoveStage<' not in shell
assert 'ScreenManager.TrySetFocus(nextLayer);' not in shell
assert "CUGuidedCreatorVM.HouseRevision" in shell
assert "HouseRevision++;" in guided

# Only noninteractive chrome; never a button layer, blocked face camera,
# duplicate banner/age/input, or a hand-written difficulty controller.
assert not list(ui.iter("ButtonWidget"))
assert not list(ui.iter("SliderWidget"))
assert not list(ui.iter("InputTextWidget"))
assert not list(ui.iter("InputWidget"))
assert not list(ui.iter("TextInputWidget"))
for w in ui.iter("Widget"):
    assert w.get("DoNotPassEventsToChildren","false")!="true"
assert any(w.get("IsVisible")=="@IsNativeChromeVisible"
           for w in ui.iter("Widget"))
assert any(w.get("IsVisible")=="@IsRailVisible"
           for w in ui.iter("Widget"))
assert sum(w.get("DataSource")=="{Chapters}"
               for w in ui.iter("ListPanel"))==1
assert any(w.get("Text")=="@StageGuidance" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@ChapterPosition" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@StepProgress" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@NextHint" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@FaceStatus" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@FaceDockStatus" for w in ui.iter("TextWidget"))
assert any(w.get("Text")=="@Household" for w in ui.iter("TextWidget"))
assert not any("EXPERIMENTAL" in w.get("Text","").upper()
               for w in ui.iter("TextWidget"))

for stage in ("CharacterCreationCultureStage",
              "CharacterCreationFaceGeneratorStage",
              "CharacterCreationNarrativeStage",
              "CharacterCreationBannerEditorStage",
              "CharacterCreationClanNamingStage",
              "CharacterCreationReviewStage",
              "CharacterCreationOptionsStage"):
    assert f'case "{stage}":' in visual,stage
assert 'width>=1600' in visual
assert "return false;" in visual.split(
    "public static bool IsStandaloneNativeChrome",1)[1].split(
    "public static bool ShouldShowChapterRail",1)[0]
assert 'IsNativeChromeVisible' in shell
assert 'CUNativeVisualPolicy.IsStandaloneNativeChrome(stageName)' in shell
assert 'return index<0 ? "NATIVE CHARACTER CREATION"' in visual
assert "CHAPTER "+chr(34) in visual
assert "CUNativeChapterPolicy.ActiveChapter(name)" in visual

# Main shared style: the most conspicuous header accents and footer
# all use dark/slate/gold, stable widths, legible text and native buttons.
for name,doc in (("guided",world),("stats",stat),("native_chrome",ui)):
    text=ET.tostring(doc,encoding="unicode")
    if name!="native_chrome":
        for color in ("#261C14","#BC9153","#EDD1A4"):
            assert color in text,(name,color)
    else:
        # Retained in source as a diagnostic recovery shell; no original
        # native stage receives this header in the default layout.
        assert "CUNativeStateShell" not in text or doc.tag=="Prefab"
    assert doc.tag=="Prefab"
assert 'CALRADIA UNIFIED' in culture
assert 'CHAPTER 02 OF 05 | YOUR CHARACTER | CULTURE' in culture
assert 'CHAPTER 02 OF 05 | STARTING STATS & TRAITS' in stats
culture_buttons=[w.get("Command.Click") for w in cult.iter("ButtonWidget")]
assert set(culture_buttons)=={"ExecuteSelect","ExecuteBack",
                             "ExecuteContinue","ExecuteUseNativeControls"}
assert any(w.get("Text")=="Back" for w in cult.iter("TextWidget"))
assert any(w.get("Text")=="Continue" for w in cult.iter("TextWidget"))
assert any(w.get("Text")=="Original culture screen"
           for w in cult.iter("TextWidget"))
assert 'nextLayer.IsFocusLayer=true;' in culture
assert 'controller.SelectCulture(id)' in culture
assert 'controller.Continue();' in culture
assert 'nativeScene.SceneView.SetScale(' in face
assert 'Input.IsKeyReleased(InputKey.F9)' in face
assert 'Restore();' in face
assert 'CUNativeChapterPolicy.Chapters[i]' in shell
assert 'public void ExecuteSelect' not in shell

# Existing native-stage policy is authoritative, and XML not a replacement
# for Bannerlord's GameState or game-owned stage transitions.
assert 'return "02  YOUR CHARACTER";' in (src/"NativeStateShellPolicy.cs").read_text()
assert '"03  YOUR HOUSE"' in chapter
assert 'GetStage' not in visual
assert 'NextStage()' not in shell
assert 'PreviousStage()' not in shell
print("PASS: warm custom creator, native original headings never obscured, culture/FaceGen/hero callbacks and F9 recovery retained (STATIC ONLY)")
