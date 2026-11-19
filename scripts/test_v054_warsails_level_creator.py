#!/usr/bin/env python3
"""v054 optional War Sails skills and native player/spouse level contract.

Source and XML audit only: a user playthrough must verify visual sliders,
registered NavalDLC skills, real Hero levels and save/reload.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
def read(file):return (src/file).read_text(encoding="utf-8")
catalog=read("SandboxNativeSkillCatalog.cs")
profile=read("SandboxOriginProfile.cs")
budget=read("SandboxStatBudgetPolicy.cs")
snapshot=read("CUNativeStatsSnapshot.cs")
vm=read("CUNativeStatsLayer.cs")
origin=read("SandboxOriginBehavior.cs")
development=read("SandboxFamilyDevelopment.cs")
growth=read("SandboxFamilyDevelopmentPolicy.cs")
levels=read("SandboxFamilyLevelPolicy.cs")

assert '"Mariner", "Boatswain", "Shipmaster"' in catalog
assert "MBObjectManager.Instance" in catalog
assert "GetObjectTypeList<SkillObject>()" in catalog
assert "SkillObject Find(string name)" in catalog
assert "Missing skills hidden" not in catalog
assert "return null;" in catalog
assert '"Mariner", "Boatswain", "Shipmaster"' in profile
assert 'AllPotentialSkills' in profile
assert 'case "SpouseStat."' not in profile
assert 'key.StartsWith("SpouseStat."' in profile
assert 'SandboxFamilyLevelPolicy.Key' in profile
assert 'SandboxFamilyLevelPolicy.Key' in budget
assert 'SandboxOriginProfile.AllPotentialSkills' in budget
assert 'SandboxNativeSkillCatalog.AvailableNames()' in snapshot
assert 'result[SandboxFamilyLevelPolicy.Key]=hero.Level' in snapshot
assert 'spouseBaseline[SandboxFamilyLevelPolicy.Key]' in vm
assert 'SandboxFamilyLevelPolicy.Key' in vm
assert 'Hero level | 1-50' in vm
assert 'SandboxNativeSkillCatalog.AvailableNames()' in vm
assert 'public string NavalStatus' in vm
assert 'SandboxNativeSkillCatalog.Find(parts[1])' in origin
assert 'hero.Level=value;' in origin
assert 'hero.HeroDeveloper.SetInitialLevel(value);' in origin
assert '"cu_player_level_verified"' in origin
assert 'SandboxNativeSkillCatalog.AvailableNames()' in development
assert 'SandboxNativeSkillCatalog.Find(parts[1])' in development
assert 'spouse.Level=value;' in development
assert 'spouse.HeroDeveloper.SetInitialLevel(value);' in development
assert '"cu_family_spouse_level_verified"' in development
assert '"Seafaring"' in growth
assert '"Mariner","Boatswain","Shipmaster"' in growth
assert 'return level>=Minimum && level<=Maximum;' in levels

for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    tree=ET.parse(gui/(name+".xml")).getroot()
    widgets=list(tree.iter("SliderWidget"))
    assert len(widgets)>=1,name
    assert any(w.get("ValueInt")=="@SelectedInt" for w in widgets),name
    assert any(w.get("Text")=="@NavalStatus"
        for w in tree.iter("TextWidget")),name
    commands=[w.get("Command.Click") for w in tree.iter("ButtonWidget")]
    assert "ExecuteEditPlayer" in commands
    assert "ExecuteEditSpouse" in commands

print("PASS: v054 naval native registry, all-stats sliders, level/XP, family inheritance and DLC-off guards (STATIC)")
