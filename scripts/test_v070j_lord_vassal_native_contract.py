#!/usr/bin/env python3
"""v0.70j source-only Lord gate. Not an acceptance test of a running game."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
profile=read("SandboxOriginProfile.cs")
policy=read("SandboxLordStartPolicy.cs")
preflight=read("SandboxStartingLifePreflight.cs")
native=read("SandboxNativeStartOptions.cs")
creator=read("CUGuidedCreatorVM.cs")
behavior=read("SandboxOriginBehavior.cs")
retinue=read("SandboxCulturalRetinue.cs")
assert '"Lord","King"' in read("SandboxStartingLifePolicy.cs")
assert 'public bool LordGuidedStart;' in profile
assert 'LordGuidedStart=false;' in profile
assert 'case "LordGuidedStart":' in profile
assert '"LordGuidedStart=" +' in profile
assert 'if(LordGuidedStart)' in profile
assert 'SandboxLordStartPolicy.ValidateRequiredFamily(' in profile
assert 'lord_spouse_required' in policy
assert 'lord_child_required' in policy
assert 'native_lord_marriage_missing' in policy
assert 'native_lord_children_or_parentage_missing' in policy
assert 'candidate.SpouseEnabled=true;' in creator
assert 'candidate.ChildCount=Math.Max(1,candidate.ChildCount);' in creator
assert 'if(!preCampaign && profile.LordGuidedStart &&' in creator
assert 'VerifyGuidedLordNativeFamily(' in behavior
nativeFamily=read("SandboxOriginBehavior.LordFamily.cs")
assert 'spouse.Spouse==player' in nativeFamily
assert 'child.Mother==mother && child.Father==father' in nativeFamily
assert 'mother.Children.Contains(child)' in nativeFamily
assert 'father.Children.Contains(child)' in nativeFamily
assert 'matchingChildren.Remove(actual);' in nativeFamily
assert 'SandboxFamilyAgePolicy.MatchesRequestedAge(' in nativeFamily
assert 'SandboxLordStartPolicy.ValidateRequest(' in profile
assert 'GuidedNativeType="vassal"' in policy
assert 'GuidedRetinue="RoleBased"' in policy
assert '"MasterOfArms","Knight"' in policy
for phrase in (
    'player_not_native_clan_head','player_not_main_party_leader',
    'wrong_native_kingdom','king_not_lord','mercenary_not_vassal',
    'native_vassal_holding_missing','native_fief_only_no_transfer',
    'native_resources_only','two_native_officers_unverified',
    'native_retinue_identity_or_assignment_mismatch'):
    assert phrase in policy,phrase
assert 'StartTypeForProfile(' in native
assert 'if(p.LordGuidedStart)' in preflight
assert 'MobileParty.MainParty.LeaderHero==player' in preflight
assert 'clan.Leader==player' in preflight
assert '"cu_lord_native_preflight"' in preflight
assert 'candidate.LordGuidedStart=candidate.Role=="Lord";' in creator
assert 'candidate.NobleBirthPosition="Independent";' in creator
assert 'candidate.StartingFiefMode="Native";' in creator
assert 'candidate.AllowStartingFiefTransfer=false;' in creator
assert 'candidate.Gold=-1;' in creator
assert 'candidate.Influence=-1;' in creator
assert 'candidate.Troops=0;' in creator
assert 'candidate.RetinueMode=SandboxLordStartPolicy.GuidedRetinue;' in creator
assert 'profile.LordGuidedStart' in creator
assert 'if(profile.LordGuidedStart)' in behavior
assert '"cu_lord_native_household_officers"' in behavior
assert '"cu_lord_native_final_verification"' in behavior
assert '.ValidateNativeHouse(actualStart,' in behavior
assert 'SandboxLordStartPolicy' in behavior
assert '.ValidateOfficers(retinue,lordOfficers.Length,' in behavior
assert 'officer.SetNewOccupation(nobleFieldOffice?' in retinue
assert 'AddCompanionAction.Apply(' in retinue
assert 'AddHeroToPartyAction.Apply(' in retinue
assert 'RemoveCompanionAction.ApplyByFire(' in retinue
assert 'ChangeOwnerOfSettlementAction' not in policy
assert 'ChangeKingdomAction' not in policy
print("PASS v0.70j SOURCE: guided Lord requires native vassal house, "
      "player leadership, real native fief, two culture-trained Heroes, "
      "no extra fief gift or resource grants. LIVE GAME NOT TESTED.")
