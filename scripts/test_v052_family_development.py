#!/usr/bin/env python3
"""v0.52 new-family integration source contracts (not in-game proof)."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
def read(name):return (src/name).read_text(encoding="utf-8")
profile=read("SandboxOriginProfile.cs")
ui=read("CUGuidedCreatorVM.cs")
new=read("SandboxFamilyDevelopment.cs")
creator=read("SandboxFamilyCreator.cs")
behavior=read("SandboxOriginBehavior.cs")
growth=read("SandboxFamilyGrowthState.cs")
policy=read("SandboxFamilyDevelopmentPolicy.cs")
for item in (
  'public string FamilyScenario = "Natural"',
  'public string Upbringing;',
  'case "FamilyScenario":',
  'case "Upbringing":',
  '"FamilyScenario=" + FamilyScenario',
  '"Upbringing=" + r.Upbringing',
  'SandboxFamilyDevelopmentPolicy.IsScenario(FamilyScenario)',
  'SandboxFamilyDevelopmentPolicy.IsUpbringing('
):
  assert item in profile,item
assert '"Household story"' in ui
assert '" | Upbringing"' in ui
assert 'choices:SandboxFamilyDevelopmentPolicy.Scenarios' in ui
assert 'choices:SandboxFamilyDevelopmentPolicy.Upbringings' in ui
for item in (
  'h.HeroDeveloper.AddAttribute(',
  'h.HeroDeveloper.SetInitialSkillLevel(',
  'h.SetTraitLevel(trait,result)',
  'a.SetPersonalRelation(b,requested)',
  'positive_skill_count=',
  'positive_attribute_count=',
  'cu_family_personality_initialized',
  'cu_family_relation_initialized',
  'cu_family_upbringing_milestone',
  'int positive=0;',
  'if(h.Age>=18f && positive<3)',
  'if(h.Age>=18f && positiveAttributes<6)'
):
  assert item in new,item
assert 'SandboxFamilyDevelopment.Spouse(' in creator
assert 'SandboxFamilyDevelopment.Child(' in creator
assert 'spouseHero.IsActive' in creator
assert 'child.ChangeState(Hero.CharacterStates.Active)' in creator
assert 'RegisterFamilyUpbringings(profile,hero,family)' in behavior
assert 'CampaignEvents.DailyTickEvent.AddNonSerializedListener(' in behavior
assert 'this, AdvanceFamilyUpbringing);' not in behavior # native education owns post-creation progression
assert 'store.SyncData("CU_v052_family_growth",ref packed)' in behavior
assert 'SandboxFamilyDevelopment.Milestone(' in behavior
assert 'familyGrowth.TryAdvance(' in behavior
assert 'CompletedMilestone(age)' in growth
assert 'new SortedDictionary<string,Entry>' in growth
assert 'Upbringing' in growth and 'LastMilestone' in growth
assert 'public static int SkillLevel(' in policy
assert 'return age==6 || age==10 || age==14 || age==18;' in policy
print("PASS: v052 native hero development, family relations, selectable story and save-safe upbringing milestone contracts (STATIC ONLY)")

assert '"cu_family_education_child_"+slot' not in read("SandboxOriginBehavior.LivingDialogueDepth.cs")
assert 'RegisterFamilyUpbringings(profile,hero,family)' in behavior
