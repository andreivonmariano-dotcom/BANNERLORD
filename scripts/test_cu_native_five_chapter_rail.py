#!/usr/bin/env python3
"""v0.36: five native-state status chapters and honest unrelocated FaceGen.
Source/XML checks only, not visual or in-game certification.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
import re

root=Path(__file__).resolve().parent.parent
p=(root/"Source/CalradiaUnified/CUNativeChapterPolicy.cs").read_text(encoding="utf-8")
shell=(root/"Source/CalradiaUnified/CUNativeStateShell.cs").read_text(encoding="utf-8")
creator=(root/"Source/CalradiaUnified/SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
face=(root/"Source/CalradiaUnified/CUNativeFaceInspector.cs").read_text(encoding="utf-8")
xml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStateShell.xml").getroot()
smoke=(root/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
csproj=(root/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")
doc=(root/"docs/V036_NATIVE_FIVE_CHAPTER_SHELL_VIEWPORT_CONTRACT.md").read_text(encoding="utf-8")

pages=["01  YOUR WORLD","02  YOUR CHARACTER","03  YOUR HOUSE",
       "04  YOUR FORTUNE","05  BEGIN YOUR LEGACY"]
assert re.findall(r'"(0[1-5]  [A-Z &]+)"',p)[:5]==pages
for stage,idx in [("CharacterCreationCultureStage",1),
                  ("CharacterCreationFaceGeneratorStage",1),
                  ("CharacterCreationBannerEditorStage",2),
                  ("CharacterCreationClanNamingStage",2),
                  ("CharacterCreationNarrativeStage",1),
                  ("CharacterCreationOptionsStage",4)]:
    assert stage in p,stage
assert 'return "WORLD CONFIGURED";' in p
assert 'return "AFTER NATIVE CLAN";' in p
assert "chapterIndex == 1 || chapterIndex == 2" in p
assert "HasVerifiedRectangularSceneDock" in p
assert "get { return false; }" in p
assert "NavigationWidth(1366)" not in p or "172" in p
assert "screenWidth < 1600 ? 172 : 240" in p
assert "screenWidth < 1600 ? 395 : 520" in p
assert "DOCK NOT YET VERIFIED" in p
assert "new BodyGeneratorView(" not in shell
assert "new BodyGeneratorView(" not in p
assert "new CharacterCreationFaceGeneratorView(" not in shell
assert "GameStateScreen(" not in shell
assert "CharacterCreationStageView(" not in shell

assert 'new MBBindingList<CUNativeChapterBadgeVM>()' in shell
assert 'CUNativeChapterPolicy.Chapters[i]' in shell
assert 'CUNativeChapterPolicy.ActiveChapter(stageName)' in shell
assert 'CUNativeChapterPolicy.ChapterStatus(' in shell
assert 'public MBBindingList<CUNativeChapterBadgeVM> Chapters' in shell
assert 'public string ViewportCapability' in shell
assert "public sealed class CUNativeChapterBadgeVM : ViewModel" in shell
assert 'public void ExecuteSelect' not in shell
assert 'CUNativeFaceInspector.TryInspect(screen,out observed)' in shell
assert '"_faceGeneratorView"' in face
assert '"BodyGeneratorView"' in face
assert "CUNativeStateShell.Tick();" in (root/"Source/CalradiaUnified/SubModule.cs").read_text(encoding="utf-8")

chapter_lists=[el for el in xml.iter("ListPanel")
               if el.get("DataSource")=="{Chapters}"]
assert len(chapter_lists)==1, "chapter list must be singular"
assert len(list(chapter_lists[0].iter("ItemTemplate")))==1
for text in ("@Label","@Status"):
    assert any(el.get("Text")==text for el in chapter_lists[0].iter("TextWidget"))
assert any(el.get("IsVisible")=="@IsSelected" for el in chapter_lists[0].iter())
assert any(el.get("Text")=="@ViewportCapability" and
           el.get("IsVisible")=="@IsFaceDebugVisible"
           for el in xml.iter("TextWidget"))
assert not any(True for el in xml.iter("ButtonWidget")),"stage tracker cannot skip effects"
assert not any(True for el in xml.iter("InputTextWidget"))
assert not any(True for el in xml.iter("SliderWidget"))

# The game's ASO plan is constructed BEFORE CharacterCreationState;
# current pregame choices cannot be retroactively applied.
for token in ("SandboxNativeStartOptions.TryPrepare(",
              "options.GetChangedOptions()",
              "new SandBoxGameManager(",
              "new Campaign(CampaignGameMode.Campaign,gameOptions)"):
    assert token in creator,token
assert 'new CUGuidedCreatorVM(true,' in creator
assert 'layer.LoadMovie("CUGuidedCreator", viewModel);' in creator
assert 'NEXT: NATIVE CREATOR' in (root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").read_text(encoding="utf-8")
assert 'CUNativeChapterPolicy.cs' in csproj
assert "native 3D scene crop is unverified" in smoke
assert "single-screen" in doc and "not" in doc.lower()
print("PASS: reordered five-chapter native-state rail, World before Hero, House/Fortune after identity, real FaceGen kept (STATIC ONLY)")
