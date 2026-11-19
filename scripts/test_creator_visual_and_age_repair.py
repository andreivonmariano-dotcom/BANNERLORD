#!/usr/bin/env python3
"""v045: reported screenshot defects and real Hero age edits.
Source/XML validation only. Dragging the native SliderWidget,
actual camera visibility and pixel-perfect layout need a game session.
"""
from pathlib import Path
from xml.etree import ElementTree as ET
root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
ui=root/"Modules/CalradiaUnified/GUI/Prefabs"
files={}
for name in ("CUGuidedCreator","CUNativeStatsEditor",
             "CUNativeCultureChooser"):
    p=ui/(name+".xml")
    tree=ET.parse(p)
    assert tree.getroot().tag=="Prefab",name
    files[name]=tree.getroot()
guided=files["CUGuidedCreator"]
stats=files["CUNativeStatsEditor"]
safe_stats=ET.parse(ui/"CUNativeStatsEditorSafe.xml").getroot()
culture=files["CUNativeCultureChooser"]
text_stats=(src/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
text_guided=(src/"CUGuidedCreatorVM.cs").read_text(encoding="utf-8")
visual=(src/"CUNativeVisualPolicy.cs").read_text(encoding="utf-8")
shell=(src/"CUNativeStateShell.cs").read_text(encoding="utf-8")
layout=(src/"CUNativeLayoutAlignment.cs").read_text(encoding="utf-8")
family=(src/"SandboxFamilyAgePolicy.cs").read_text(encoding="utf-8")

# Actual screenshot showed blue/slate panels clashing with native wood,
# heavy oversized type and redundant technical headings. All the active
# custom creator movies must use warm native parchment/bronze.
retired_blue=(
    "#08121EFF","#172536F0","#0C1928E8","#152432EF",
    "#203246EE","#203043FF","#09121DFF","#111F2BF0",
    "#0F1F2EEC","#1C2A38FF",
)
for name,root_node in files.items():
    x=ET.tostring(root_node,encoding="unicode")
    for old in retired_blue:
        assert old not in x,(name,old)
    assert "#BC9153FF" in x or "#BD9155FF" in x
    labels=list(root_node.iter("TextWidget"))
    assert labels and all("Brush.FontSize" in label.attrib
                          for label in labels),name
    assert max(int(label.attrib["Brush.FontSize"])
               for label in labels)<=29
assert not any(x.get("Text")=="OPTIONAL CHOICES"
               for x in guided.iter("TextWidget"))
assert not any(x.get("Text")=="@CultureId"
               for x in culture.iter("TextWidget"))
assert not any("The campaign engine owns all native"
               in x.get("Text","")
               for x in guided.iter("TextWidget"))
# The compact center frame is measured separately from responsive
# sidebars, with original native ButtonBrush command bindings inside.
assert any(w.get("SuggestedWidth")=="610" and
           w.get("SuggestedHeight")=="369"
           for w in guided.iter("Widget"))
assert any(w.get("Sprite")==r"SPGeneral\EscapeMenu\escape_panel"
           for w in guided.iter("Widget"))
assert any(w.get("Sprite")==
    r"General\CharacterCreation\character_creation_background_gradient"
    for w in guided.iter("Widget"))
assert any(w.get("Text")=="@SelectedValue" and
           w.get("Brush.TextHorizontalAlignment")=="Center"
           for w in guided.iter("TextWidget"))
assert '"Create Character"' in text_guided
assert '"Confirm & Continue"' in text_guided

# Original Bannerlord Family, Banner, Review and Difficulty titles were
# visibly overlapped by CU's extra 91px header. Game-owned native
# controls must now draw without a duplicate overlay or layout shoves.
assert "return false;" in visual.split(
    "public static bool IsStandaloneNativeChrome",1)[1].split(
    "public static bool ShouldShowChapterRail",1)[0]
assert 'if(!CUNativeVisualPolicy.IsStandaloneNativeChrome(nextStage))' in layout
assert 'public bool IsFaceDebugVisible { get { return false; } }' in shell
assert 'public bool IsFaceDockTrialStage { get { return false; } }' in shell
assert 'candidate.NativeFaceDockTrialEnabled=false;' in text_guided

# Distinguish skill LEVELS from focus-point allocations. Five is the
# native FOCUS maximum, not the skill maximum (330).
assert '"Skill: "+originalName' in text_stats
assert '"Focus: "+originalName' in text_stats
assert 'SKILL LEVEL | 0–330' in text_stats
assert 'FOCUS POINTS | Native cap: 5' in text_stats
assert 'SandboxStatBudgetPolicy.Step(key)' in text_stats

# One-year slider is a real native Gauntlet SliderWidget bound to a
# writeable VM property, not a cosmetic bar with a guessed percentage.
sliders=list(stats.iter("SliderWidget"))
assert len(sliders)==1
sl=sliders[0]
assert sl.get("ValueInt")=="@PlayerAge"
assert sl.get("MinValueInt")=="18" and sl.get("MaxValueInt")=="80"
assert sl.get("DiscreteIncrementInterval")=="1"
assert sl.get("IsDiscrete")=="true"
assert sl.get("Handle")=="CUPlayerAgeHandle"
assert sl.get("SliderArea")=="CUPlayerAgeTrack"
ids={w.get("Id") for w in sl.iter()}
assert {"CUPlayerAgeHandle","CUPlayerAgeTrack"} <= ids
assert any(w.get("IsVisible")=="@IsAgePage"
           for w in stats.iter("Widget"))
assert any(w.get("IsVisible")=="@IsStatsPage"
           for w in stats.iter("ListPanel"))
commands={b.get("Command.Click") for b in stats.iter("ButtonWidget")}
assert {"ExecuteAgeIncrease","ExecuteAgeDecrease",
        "ExecuteKeepNativeAge"}<=commands
for command in ("ExecuteAgeIncrease","ExecuteAgeDecrease",
                "ExecuteKeepNativeAge"):
    assert "public void "+command+"()" in text_stats
assert 'private int pageIndex=CUNativeStatsNavigationPolicy.Age;' in text_stats
assert "hero.SetBirthDay(CampaignTime.YearsFromNow(" in text_stats
assert "hero.SetBirthDay(oldBirthday)" in text_stats
assert "cu_native_player_age_committed" in text_stats
assert "cu_native_age_slider_fallback" in text_stats
assert "cu_native_age_slider_drag" in text_stats
assert "ReadNativeSliderValue();" in text_stats
assert any(w.get("Id")=="CUPlayerAgeHandle"
           for w in sl.iter("ButtonWidget"))
assert '"CUNativeStatsEditorSafe",vm' in text_stats
assert '"CUNativeStatsEditorPlainFallback",vm' in text_stats
assert len(list(safe_stats.iter("SliderWidget")))==0
assert any(x.get("Command.Click")=="ExecuteAgeIncrease"
           for x in safe_stats.iter("ButtonWidget"))
assert any(x.get("Command.Click")=="ExecuteAgeDecrease"
           for x in safe_stats.iter("ButtonWidget"))
assert "native_story_bonuses_preserved=true" in text_stats
assert "profile.PlayerAge=age;" in text_stats

# Child age never forces guessing. The younger actual parent determines
# the maximum, and the current input rejects ages beyond that limit.
assert "Math.Min(playerAge,spouseAge)" in family
assert "youngestParent-MinimumParentChildGap" in family
assert "ChildAgeMaximum(" in text_guided
assert "ClampChildrenToParents();" in text_guided
assert "CanChooseChildAge(" in text_guided
assert 'EffectiveMaximum(field)' in text_guided
assert 'if(IsChildAge)return "Allowed age:' in text_guided
assert "actualAge" in text_guided and "SaveForPlayer(" in text_guided

# Known screenshot collision: stat row content has two non-overlapping
# lines within its item, not 29px description underneath 28px title.
row=next(x for x in stats.iter("ListPanel")
         if x.get("DataSource")=="{Rows}")
item=row.find("./ItemTemplate/Widget")
assert item is not None and item.get("SuggestedHeight")=="56"
name=next(x for x in item.iter("TextWidget")
          if x.get("Text")=="@Name")
desc=next(x for x in item.iter("TextWidget")
          if x.get("Text")=="@Description")
assert int(name.get("PositionYOffset"))+int(name.get("SuggestedHeight")) <= (
    int(desc.get("PositionYOffset")))
assert int(desc.get("PositionYOffset"))+int(desc.get("SuggestedHeight")) <= 56

print("PASS: warm native-style custom creator, compact text, native original stages unobscured, age slider, child-age cap and skill-vs-focus labels (STATIC ONLY)")
