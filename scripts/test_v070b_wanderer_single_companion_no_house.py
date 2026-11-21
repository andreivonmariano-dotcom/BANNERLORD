#!/usr/bin/env python3
"""v0.70b new guided Wanderer only: one companion, no House overlay, native player stats.

Structural/source audit. Not a claim of native Bannerlord runtime, wage
charging order, 3D customization, save/reload or campaign pass.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
read=lambda p:(root/p).read_text(encoding="utf-8")
p=read("SandboxOriginProfile.cs")
vm=read("CUGuidedCreatorVM.cs")
house=read("CUNativeHouseLayer.cs")
stats=read("CUNativeStatsLayer.cs")
comp=read("SandboxWandererCompanion.cs")
ret=read("SandboxCulturalRetinue.cs")
behavior=read("SandboxOriginBehavior.cs")
policy=read("SandboxRoleConsistencyPolicy.cs")
assert 'case "Wanderer":' in policy and 'case "NobleCadet":return "default";' in policy
assert 'public bool WandererNoHouseStart;' in p
assert 'public bool WandererCompanionEnabled;' in p
for field in ("Name","Age","Gender","Specialty","Skill"):
    assert f"WandererCompanion{field}" in p
    assert f'"WandererCompanion{field}"' in vm
assert 'case "WandererNoHouseStart":' in p
assert 'WandererNoHouseStart=false;' in p
assert 'WandererCompanionEnabled=false;' in p
assert 'WandererNoHouseStart &&' in p
assert 'Role!="Wanderer"' in p
assert 'SpouseEnabled || ChildCount!=0 || SiblingCount!=0' in p
assert 'RetinueMode!="FamilyOnly"' in p
assert 'candidate.WandererNoHouseStart=candidate.Role=="Wanderer";' in vm
assert 'candidate.SpouseEnabled=false;' in vm
assert 'candidate.ChildCount=0;' in vm
assert 'candidate.SiblingCount=0;' in vm
assert 'candidate.RetinueMode=candidate.WandererNoHouseStart?' in vm
assert '"FamilyOnly":candidate.OutlawGuidedStart?' in vm
assert '"RoleBased":candidate.MercenaryGuidedStart ||' in vm
assert 'candidate.SoldierGuidedStart ?' in vm
assert '"FamilyOnly":SandboxServicePolicy.DefaultRetinue(' in vm
assert 'key=="WandererCompanionEnabled"' in vm
assert 'profile.WandererCompanionEnabled' in vm
assert '"WandererCompanionSkill","Primary specialty skill"' in vm
assert 'profile.WandererCompanionOverrides' in stats
assert 'EditingWandererCompanion' in stats
assert 'IsSecondaryStatsTargetVisible' in stats
assert 'SecondaryStatsTargetLabel' in stats
assert 'ApplyCustomStats(profile,provisional);' in comp
assert 'cu_wanderer_companion_stat_applied' in comp
assert 'WandererCompanionOverrides.Clear();' in p
assert '"WandererCompanionStat."+pair.Key' in p
assert 'case "WandererCompanionSkill":' in p

# This must bypass House at the native Review, not skip the real age,
# custom attributes, skills, focus/traits, FaceGen or Review/Options.
assert 'skipHouseForWanderer=chosen.Enabled &&' in house
assert 'chosen.WandererNoHouseStart' in house
assert 'if(skipHouseForWanderer)return;' in house
assert 'cu_wanderer_house_stage_skipped' in house
assert 'manager.NextStage();' in house
assert 'CUNativeStatsSnapshot.Capture(Hero.MainHero)' in stats
assert 'profile.Overrides' in stats
assert 'SandboxStatBudgetPolicy.TryEvaluate(profile.ProgressionMode,' in stats
assert 'cu_native_stats_committed' in stats

# The companion must be a new single Hero, not the existing native
# wanderers, and the actual native relationships must read 100.
assert 'if(Clan.PlayerClan.Companions.Count!=0)' in comp
assert 'HeroCreator.CreateSpecialHero(' in comp
assert 'AddCompanionAction.Apply(Clan.PlayerClan,provisional);' in comp
assert 'AddHeroToPartyAction.Apply(' in comp
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in comp
assert 'player.GetRelation(provisional)!=100' in comp
assert 'provisional.GetRelation(player)!=100' in comp
assert 'Clan.PlayerClan.Companions.Count!=1' in comp
assert 'cu_wanderer_unique_companion' in comp
assert 'SetInitialSkillLevel(skill,' in comp
assert 'provisional.GetSkillValue(skill)!=' in comp
assert 'source.BattleEquipments' in comp
assert 'RemoveCompanionAction.ApplyByFire(' in ret

# One-shot grant. A failed or extra companion cannot produce PASS.
assert 'wandererCompanionResult=' in behavior
assert 'SandboxWandererCompanion.TryCreate(' in behavior
assert 'reason=wanderer_companion_not_verified' in behavior
assert 'cu_wanderer_companion_start_contract' in behavior
assert 'Clan.PlayerClan.Companions.Count!=1' in behavior
assert 'CU_v070b_wanderer_companion' in behavior
assert 'CU_v070b_wanderer_wage_credit_day' in behavior
assert 'RefundWandererCompanionWage' in behavior
assert 'Campaign.Current.Models.PartyWageModel' in behavior
assert '.GetCharacterWage(companion.CharacterObject)' in behavior
assert 'GiveGoldAction.ApplyBetweenCharacters(' in behavior
assert 'wandererWageCreditDay=day;' in behavior
assert 'companion_in_main_party=true' in behavior
print("PASS v0.70b SOURCE: new Wanderer has no House overlay, "+
      "player stats remain, optional single native companion and "+
      "save-backed wage reimbursement. LIVE GAME CHECK REQUIRED.")
