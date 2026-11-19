#!/usr/bin/env python3
"""v0.46 regression against observed 2026-11-19 creator screenshots.
Static source/GUI contract only. Native runtime and recall require game.
"""
from pathlib import Path
from xml.etree import ElementTree as ET
r=Path(__file__).resolve().parent.parent
src=r/"Source/CalradiaUnified"
ui=r/"Modules/CalradiaUnified/GUI/Prefabs"
def read(p):return p.read_text(encoding="utf-8")
nav=read(src/"CUNativeStatsNavigationPolicy.cs")
stats=read(src/"CUNativeStatsLayer.cs")
guide=read(src/"CUGuidedCreatorVM.cs")
home=read(src/"SandboxFamilyHome.cs")
residence=read(src/"SandboxFamilyResidencePolicy.cs")
gallery=read(src/"CUFamilyPortraitGallery.cs")
culture_vm=read(src/"CUNativeCultureLayer.cs")
def xml(name):return ET.parse(ui/(name+".xml")).getroot()
guided=xml("CUGuidedCreator")
statui=xml("CUNativeStatsEditor")
safe=xml("CUNativeStatsEditorSafe")
cult=xml("CUNativeCultureChooser")
portraits=xml("CUFamilyPortraits")

# Same four screens, same boundaries, regardless of Native/Balanced/
# Unrestricted. Values are still only committed on the final Traits page.
for code in ("Age=-1","Attributes=0","Skills=1","Traits=2",
             "public const int PageCount=4","BackToNativeNarrative(",
             "NextLabel(int page)"):
    assert code in nav,code
assert "CUNativeStatsNavigationPolicy.BackPage(pageIndex)" in stats
assert "CUNativeStatsNavigationPolicy.NextPage(pageIndex)" in stats
assert "CUNativeStatsNavigationPolicy.IsFinal(pageIndex)" in stats
assert "CUNativeStatsNavigationPolicy.BackToNativeNarrative(pageIndex)" in stats
assert "CUNativeStatsNavigationPolicy.NextLabel(pageIndex)" in stats
assert 'profile.ProgressionMode!="Native" &&' not in stats
assert "cu_stats_navigation" in stats
assert "pageIndex=CUNativeStatsNavigationPolicy.Age" in stats
assert "manager.PreviousStage();" in stats
assert 'hero.SetBirthDay(oldBirthday)' in stats

# The existing native game actions remain intact and are not cloned.
guide_buttons={x.get("Command.Click") for x in guided.iter("ButtonWidget")}
assert {"ExecutePreviousChoice","ExecuteNextChoice",
        "ExecuteDecrease","ExecuteIncrease","ExecuteExact",
        "ExecuteToggle","ExecuteEditText","ExecuteNativeDefault",
        "ExecuteBack","ExecuteContinue"}<=guide_buttons
for kind in ("CUNativeStatsEditor","CUNativeStatsEditorSafe"):
    root=statui if kind=="CUNativeStatsEditor" else safe
    commands={x.get("Command.Click") for x in root.iter("ButtonWidget")}
    assert {"ExecuteBack","ExecuteContinue","ExecuteAgeIncrease",
            "ExecuteAgeDecrease","ExecutePreviousGroup",
            "ExecuteNextGroup","ExecuteEditExact"}<=commands
    assert any(x.get("IsVisible")=="@IsStatsPage" for x in root.iter("ButtonWidget"))
    assert any(x.get("SuggestedHeight")=="56" for x in root.iter("Widget"))

# Compact central card with original button commands and enough width
# for all three simultaneously displayed age/numeric actions. No visible
# debug config fields or redundant culture-controller instructions.
assert any(x.get("SuggestedWidth")=="610" and
           x.get("SuggestedHeight")=="369" for x in guided.iter("Widget"))
assert any(x.get("SuggestedHeight")=="527" for x in guided.iter("Widget"))
assert not any("NATIVE RULES AND CHARACTER CHOICES"
               in x.get("Text","") for x in guided.iter("TextWidget"))
assert 'Report("Selected: "+key+" = "+value)' not in guide
assert "Report(\"\");" in guide
assert '"These are real game culture choices."' not in culture_vm
assert 'SetStatus("Selected in Bannerlord' not in culture_vm
assert any(x.get("Text")=="Original culture screen" for x in cult.iter("TextWidget"))
assert any(x.get("SuggestedHeight")=="226" and
           x.get("Color")=="#1B140FFF" for x in cult.iter("Widget"))

# The missing son is not a generation failure: the existing native
# underage Hero cannot be treated as an adult recruitable party member.
# Log and display BOTH residence and spawn-state, without forcing
# ChangeState, a MobileParty or scene spawn for underage Heroes.
assert "target.HeroesWithoutParty.Contains(child)" in home
minor=home.split("if (child.IsChild)",1)[1].split("else\n",1)[0]
assert "child.StayingInSettlement = target;" in minor
assert "placed = child.StayingInSettlement == target;" in minor
assert "child.ChangeState" not in minor
assert "TeleportHeroAction.ApplyImmediateTeleportToSettlement" not in minor
assert "|scene_registered=" in home
assert "|gender=" in home
assert "|status=" in home
assert "minorsWithResidence" in home
assert "adultsSceneRegistered" in home
assert "EligibleForAdultRecall(" in home
assert '"underage_home_recorded"' in residence
assert '"adult_hero_not_present"' in residence
assert "Every son and daughter is shown here." in gallery
assert "cu_family_gallery_child" in gallery
assert "hero.IsChild" in gallery
assert 'Text="@Presence"' in ET.tostring(portraits,encoding="unicode")
assert not any(x.get("Text")=="@Parentage" for x in portraits.iter("TextWidget"))
assert any(x.get("Brush.FontSize") for x in portraits.iter("TextWidget"))

print("PASS: deterministic age-to-attributes-to-skills-to-traits navigation, compact native-style UI, minor/older-child home and recall diagnostics (STATIC ONLY)")
