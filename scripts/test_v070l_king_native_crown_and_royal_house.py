#!/usr/bin/env python3
"""v0.70l source-only King crown, family and saved succession gates."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda p:(src/p).read_text(encoding="utf-8")
policy=read("SandboxKingStartPolicy.cs")
profile=read("SandboxOriginProfile.cs")
lineage=read("SandboxHouseholdLineagePolicy.cs")
creator=read("CUGuidedCreatorVM.cs")
preflight=read("SandboxStartingLifePreflight.cs")
behavior=read("SandboxOriginBehavior.cs")
court=read("SandboxCulturalRetinue.cs")
claims=read("SandboxKingSuccessionState.cs")
observation=read("SandboxOriginBehavior.KingSuccession.cs")
assert '"Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King"' in read("SandboxStartingLifePolicy.cs")
assert 'public bool KingGuidedStart;' in profile
assert 'KingGuidedStart=false;' in profile
assert 'case "KingGuidedStart":' in profile
assert '"KingGuidedStart=" +' in profile
assert 'public string KingSuccessionRule="EldestChild";' in profile
assert 'case "KingSuccessionRule":' in profile
assert 'SandboxKingStartPolicy.ValidateRequest(' in profile
assert 'NativeType="king"' in policy
assert 'Retinue="RoleBased"' in policy
assert '"Marshal","Chamberlain"' in policy
for v in (
    "not_native_king_start","royal_kingdom_mismatch",
    "not_native_ruling_clan_head","not_real_kingdom_ruling_clan",
    "royal_native_fief_missing","royal_native_estate_only",
    "royal_native_treasury_and_army_only",
    "native_royal_court_receipt_missing",
    "native_royal_court_unverified"):
    assert v in policy,v
for marker in (
    "CanAppointNativeRuler(){return false;}",
    "CanGrantNativeFief(){return false;}",
    "CanInstallRegent(){return false;}"):
    assert marker in policy
assert 'FirstOlderBrotherInFoundingKingHouse(' in lineage
assert 'founding_king_older_brother' in lineage
assert 'candidate.KingGuidedStart=candidate.Role=="King";' in creator
assert 'candidate.RetinueMode=SandboxKingStartPolicy.Retinue;' in creator
assert 'candidate.KingSuccessionRule=' in creator
assert 'profile.KingGuidedStart' in creator
assert 'SandboxLordSuccessionPolicy.Rules' in creator
assert 'candidate.StartingFiefMode="Native";' in creator
assert 'candidate.Gold=-1;' in creator
assert 'if(p.KingGuidedStart)' in preflight
assert '"cu_king_native_preflight"' in preflight
assert 'SandboxKingStartPolicy.ValidateNativeCrown(' in preflight
assert 'if(profile.KingGuidedStart)' in court
assert 'offices=SandboxKingStartPolicy.CourtOffices;' in court
assert '"cu_king_native_court_verification"' in behavior
assert 'SandboxKingStartPolicy.ValidateCourt(' in behavior
assert '"cu_king_native_final_verification"' in behavior
assert '"CU_v070l_native_royal_household"' in behavior
assert 'SandboxKingSuccessionState.Parse(royal)' in behavior
assert 'RegisterKingSuccessionMenus(starter);' in behavior
assert 'ObserveKingSuccessionDaily);' in behavior
assert 'BeginGuidedKingSuccession(' in behavior
assert 'CU_KING_CLAIMS_1' in claims
assert 'public static SandboxKingSuccessionState Parse(' in claims
assert 'SandboxLordSuccessionPolicy.Order(' in claims
assert 'NativeRoyalHeirObserved' in claims
assert 'MinorRoyalClaimNativeDecisionPending' in claims
assert 'FounderDeadNoVerifiedHeir' in claims
assert 'Kingdom.All.FirstOrDefault(' in observation
assert 'kingdom.RulingClan.Leader.StringId' in observation
assert 'VerifiedLordChildCandidates(' in observation
assert 'GameMenu.SwitchToMenu("cu_king_succession")' in observation
for code in (policy,claims,observation):
    for forbidden in ('ChangeOwnerOfSettlementAction',
        'ChangeKingdomAction.Apply','ChangeClanLeaderAction.Apply',
        'Clan.PlayerClan.Leader=','kingdom.RulingClan='):
        # An == native-identity comparison is not a setter.
        assert not re.search(re.escape(forbidden)+r'(?![=])',code),forbidden
print("PASS v0.70l SOURCE: King requires actual native crown, "
      "true clan and estate, real cultural Marshal and Chamberlain, "
      "and saved read-only royal heir ranks. LIVE GAME NOT TESTED.")
