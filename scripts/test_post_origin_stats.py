#!/usr/bin/env python3
"""v0.40 STATIC gate: real native post-origins stat panel, three modes and
point safeguards. Only an in-game session can prove correct stage timing,
actual rendered bonus history, input handling or persistence after reload.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

r = Path(__file__).resolve().parent.parent
s = r / "Source/CalradiaUnified"
profile = (s/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
overlay = (s/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
shell = (s/"CUNativeStateShell.cs").read_text(encoding="utf-8")
layer = (s/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
snapshot = (s/"CUNativeStatsSnapshot.cs").read_text(encoding="utf-8")
policy = (s/"SandboxStatBudgetPolicy.cs").read_text(encoding="utf-8")
behavior = (s/"SandboxOriginBehavior.cs").read_text(encoding="utf-8")
csproj = (r/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")
tests = (r/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
ui = ET.parse(r/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeStatsEditor.xml").getroot()

assert 'new GauntletLayer("CUNativeStatsEditor",85,false)' in layer
assert 'nextMovie=next.LoadMovie("CUNativeStatsEditor",vm);' in layer
assert 'ScreenManager.TrySetFocus(next);' in layer
assert 'next.InputRestrictions.SetInputRestrictions();' in layer
assert 'next.IsFocusLayer=true;' in layer
assert 'if(stage=="CharacterCreationNarrativeStage")' in layer
assert 'if(stage!="CharacterCreationBannerEditorStage")' in layer
assert 'manager.PreviousStage();' in layer, "Back must delegate native stage"
assert 'screen.AddLayer(next);' in layer
assert 'oldHost.RemoveLayer(oldLayer);' in layer
assert 'oldLayer.ReleaseMovie(oldMovie);' in layer
assert 'native.IsFocusLayer' in layer, "restore original input"
assert 'CUNativeStatsLayer.Tick(screen,' in shell
assert 'CUNativeStatsLayer.Clear();' in shell
assert 'new CharacterCreationScreen(' not in layer
assert 'new BodyGeneratorView(' not in layer
assert 'RemoveStage<' not in layer

# The native Hero, not guessed story or class templates, owns the baseline.
for token in ("Hero.MainHero","hero.GetAttributeValue(attr)",
              "hero.GetSkillValue(skill)","hero.HeroDeveloper.GetFocus(skill)",
              "hero.GetTraitLevel(trait)"):
    assert token in (layer+snapshot), token
assert "NativeNarrativeBonusReader.Read(manager)" in layer
assert 'GetField(' in layer and '"SelectedOptions",flags' in layer
assert 'IDictionary<NarrativeMenu,NarrativeMenuOption>' in layer
assert 'foreach(var option in selected.Values)' in layer
assert 'Command.Click="ExecuteEditExact"' in ET.tostring(ui,encoding="unicode")
assert 'option.PositiveEffectText.ToString()' in layer
assert "Detailed selection history is " in layer, "never invent missing bonus history"
assert "CUNativeStatsSnapshot.Capture(hero)" in behavior
assert 'profile.ProgressionMode=="Native"' in behavior
assert "SandboxStatBudgetPolicy.TryEvaluate(" in behavior
assert "profile.Overrides" in behavior
assert '"cu_progression_plan_rejected"' in behavior
assert '"cu_native_stats_committed"' in layer

# No redundant pre-campaign attribute/skill/trait control remains visible.
pre=overlay.split('private bool IsAvailable(Field field)',1)[1].split(
    'private Field[] VisibleFields',1)[0]
for prefix in ("Skill.","Focus.","Attribute.","Trait."):
    assert ('key.StartsWith("'+prefix+'",StringComparison.Ordinal)') in pre
    assert pre.index('return false;',pre.index('key.StartsWith("Skill."')) < (
        pre.index('if (key=="WorldSeed"'))
assert 'ProgressionMode="Native";' in profile
assert 'Overrides.Clear();' in profile
assert 'case "ProgressionMode":' in profile
assert '"ProgressionMode=" + ProgressionMode' in profile
assert '"Native", "Balanced", "Unrestricted"' in policy
assert "StartingBudget = 30;" in policy
assert "Math.Max(0,selectedValue-nativeValue)" in policy
assert "TraitDescription(string trait)" in policy
assert "ModeDescription(string mode)" in policy
assert 'SandboxStatBudgetPolicy.cs' in csproj
assert 'balanced allocations over 30 points rejected' in tests

# The UI has exactly 3 mode actions and no external experiment control.
assert ui.tag=="Prefab"
clicks=[x.get("Command.Click") for x in ui.iter("ButtonWidget")]
for action in ("ExecuteNative","ExecuteBalanced","ExecuteUnrestricted",
               "ExecuteReset","ExecutePreviousGroup","ExecuteNextGroup",
               "ExecuteBack","ExecuteContinue","ExecuteIncrease","ExecuteDecrease",
               "ExecuteEditExact"):
    assert action in clicks,action
    assert re.search(r'public void '+action+r'\s*\(',layer),action
assert clicks.count("ExecuteNative")==1
assert clicks.count("ExecuteBalanced")==1
assert clicks.count("ExecuteUnrestricted")==1
assert not any("Experimental" in (node.get("Text","")) for node in ui.iter())
assert any(n.get("DataSource")=="{Rows}" for n in ui.iter("ListPanel"))
for key in ("Name","NativeValue","SelectedValue","Difference","Description",
            "Title","Subtitle","Page","PageProgress","ModeDescription",
            "BonusSummary","BudgetStatus","NextLabel","Status","SkillGroup"):
    assert any(x.get("Text")=="@"+key for x in ui.iter("TextWidget")),key
assert any(x.get("MaxWidth")=="1820" and
    x.get("MaxHeight")=="960" for x in ui.iter("Widget"))
assert 'Begin Campaign' not in ET.tostring(ui,encoding="unicode")
print("PASS: native post-origins stat interstitial, three budget modes, guarded final Hero overrides, native fallback (STATIC)")
