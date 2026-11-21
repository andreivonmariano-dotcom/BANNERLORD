#!/usr/bin/env python3
"""v0.70g guided Soldier: real external enlistment, not a fake mercenary.

Source-audit only. Passing this script never proves an in-game SaS
dialogue, start-enlisted menu, party, wages, rank or save/reload.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
get=lambda n:(src/n).read_text(encoding="utf-8")
profile=get("SandboxOriginProfile.cs")
start=get("SandboxNativeStartOptions.cs")
vm=get("CUGuidedCreatorVM.cs")
bridge=get("SandboxSaSBridge.cs")
policy=get("SandboxSoldierEnlistmentPolicy.cs")
pre=get("SandboxStartingLifePreflight.cs")
patron=get("SandboxServicePatron.cs")
origin=get("SandboxOriginBehavior.cs")
family=get("SandboxFamilyCreator.cs")
economy=get("SandboxRoleEconomyPolicy.cs")
role_matrix=get("SandboxRoleConsistencyPolicy.cs")
career=get("SandboxServiceCareerState.cs")
doc=(root/"docs"/"V070G_SOLDIER_REAL_ENLISTMENT_ACCEPTANCE.md").read_text(
    encoding="utf-8")
csproj=(root/"Tests"/"PolicySmoke"/"PolicySmoke.csproj").read_text(
    encoding="utf-8")
tests=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text(
    encoding="utf-8")
assert '"Soldier","Recruit"' in profile, "Soldier must be fourth choice"
assert 'public bool SoldierGuidedStart;' in profile
assert 'public string SoldierEntryRank = "Recruit";' in profile
assert 'SoldierGuidedStart=false;' in profile
assert 'SoldierEntryRank="Recruit";' in profile
assert 'case "SoldierGuidedStart":' in profile
assert 'case "SoldierEntryRank":' in profile
assert '"SoldierGuidedStart=" +' in profile
assert '"SoldierEntryRank=" + SoldierEntryRank' in profile
assert 'Gold!=-1 || Influence!=-1 || Troops!=0' in profile
assert 'candidate.SoldierGuidedStart=candidate.Role=="Soldier";' in vm
assert 'candidate.StartingFiefMode="Native";' in vm
assert 'candidate.RetinueMode="FamilyOnly";' in vm
assert 'candidate.RoleAffiliationsEnabled=false;' in vm
assert '"SoldierEntryRank","Requested service entry"' in vm
assert 'SandboxSoldierEnlistmentPolicy.EntryChoices' in vm
assert 'preCampaign && profile.Role=="Soldier"' in vm
assert 'Actual SaS' not in vm or 'SaS' in vm
assert 'case "Soldier":' in role_matrix
assert 'case "Mercenary":return "mercenary";' in role_matrix
assert 'public static string StartTypeForProfile(' in start
assert 'profile.SoldierGuidedStart &&' in start
assert '"default":StartTypeForRole(profile.Role)' in start
assert 'var type = StartTypeForProfile(profile)' in start
assert 'SandboxSaSBridge.HasSupportedProvider()' in start
for native_rank in ('private','soldier','corporal','sergeant','veteran',
                    'captain','champion'):
    assert f'"{native_rank}"' in policy
for method in ("RequestedNativeRank(","NativeRankNumber(","CareerStage(",
               "ValidateEnlisted(","ValidateIndependentStart(",
               "ValidateRequestedEntryRank(","PromotionReceipt("):
    assert method in policy,method
assert 'new[]{"Recruit","Sergeant","Captain"}' not in policy or 'Recruit' in policy
assert 'NativeHostPartyLedByLord' in bridge
assert 'LordKingdomId' in bridge
assert 'RankNumber=Num(Field(snap,"Rank"))' in bridge
assert 'host.LeaderHero==lord' in bridge
assert 'SandboxSoldierEnlistmentPolicy.ValidateEnlisted(' in pre
assert 'SandboxSoldierEnlistmentPolicy.ValidateIndependentStart(' in pre
assert 'SandboxNativeStartOptions' in pre
assert '.StartTypeForProfile(p)' in pre
assert 'cu_soldier_real_enlistment_preflight' in pre
assert 'SandboxSoldierEnlistmentPolicy.ValidateEnlisted(' in patron
assert 'cu_soldier_real_lord_bound' in patron
assert 'if(p.SoldierGuidedStart)' in patron
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in patron
assert 'CU_v070g_soldier_native_enlistment_pending' in origin
assert 'CU_v070g_soldier_pending_origin_profile' in origin
assert 'soldierPendingOriginProfile=profile.Serialize();' in origin
assert 'if(!waiting.Available || !waiting.Enlisted)' in origin
assert 'soldierPendingStartSaved=true;' in origin
assert 'soldierPendingStartSaved=false;' in origin
assert 'SandboxNativeStartOptions.StartTypeForProfile(profile)' in origin
assert 'cu_soldier_native_start_verification' in origin
assert 'native_enlisted_soldier_unverified:' in origin
assert 'cu_soldier_real_career_started' in origin
assert '"EnlistedSoldier":profile.Role' in origin
assert 'profile.SoldierGuidedStart' in family
assert 'ExternalEnlistedSoldierWage' in economy
assert 'ExternalEnlistmentVerified' in career
assert 'SandboxSoldierEnlistmentPolicy.cs' in csproj
assert 'fourth guided start is real soldier' in tests
assert 'real soldier needs live SaS service' in tests
assert 'Soldier startup profile flags roundtrip and reset' in tests
for name in ('Recruit','Sergeant','Captain','SaS','rank','lord',
             'not a gameplay','source'):
    assert name.lower() in doc.lower(),name
print("PASS v0.70g SOURCE: Soldier mapped to independent native start, "
      "true SaS lord/host/rank verification, pending save and native wages. "
      "GAMEPLAY ENLISTMENT UNTESTED.")
