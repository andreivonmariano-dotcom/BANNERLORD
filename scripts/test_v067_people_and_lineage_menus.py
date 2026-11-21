#!/usr/bin/env python3
"""v0.67: native menu composition, autonomous requests, guarded grants.
Source/static checks only, C# simulated cases run through PolicySmoke."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
def read(name):
    return (src/name).read_text(encoding="utf-8")
behavior=read("SandboxOriginBehavior.cs")
menu=read("SandboxOriginBehavior.LivingMenus.cs")
policy=read("SandboxAgencyDecisionPolicy.cs")
agency=read("SandboxAgencyDecisionState.cs")
inherit=read("SandboxInheritanceNegotiationPolicy.cs")
state=read("SandboxInheritanceNegotiationState.cs")
sim=read("SandboxLivingAffairsSimulation.cs")
assert "public sealed partial class SandboxOriginBehavior" in behavior
assert "public sealed partial class SandboxOriginBehavior" in menu
assert "RegisterPeopleAndLineageMenus(starter);" in behavior
assert "CU_v067_character_requests" in behavior
assert "CU_v067_family_negotiation" in behavior
assert "ObserveAgencyDaily(player);" in behavior
assert '"cu_people_lineage"' in menu
assert '"cu_people_household"' in menu
assert '"cu_people_requests"' in menu
assert '"cu_people_claim"' in menu
assert 'new[]{"town","castle"}' in menu
assert '"CU: People & Lineage"' in menu
assert '"Return to vanilla settlement menu"' in menu
assert '"cu_household_view"' in menu
assert '"cu_household_next"' in menu
assert '"cu_people_request"' in menu
assert '"cu_people_accept"' in menu
assert '"cu_people_decline"' in menu
assert '"cu_family_claim_review"' in menu
assert '"cu_family_petition"' in menu
assert '"cu_family_grant"' in menu
assert '"cu_family_mediation"' in menu
assert '"cu_family_withdraw"' in menu
assert 'OfferFamilyMediation()' in menu
assert 'WithdrawFamilyClaim()' in menu
assert 'SandboxInheritanceNegotiationPolicy.MediationGate(' in menu
assert 'SandboxInheritanceNegotiationPolicy.MayWithdraw(' in menu
assert 'GiveGoldAction.ApplyBetweenCharacters(' in menu
assert 'inheritanceNegotiation.RecordMediation()' in menu
assert 'CU_NEGOTIATION_2' in state
assert 'CU_NEGOTIATION_1' in state
assert 'GameMenu.SwitchToMenu(affairsReturnMenu)' in menu
assert 'InformationManager.ShowInquiry(new InquiryData(' in menu
assert 'ConfirmCharacterSupport()' in menu
assert 'ConfirmEstateInNativeDialog()' in menu
assert 'args=>ConfirmCharacterSupport(),false' in menu
assert 'args=>ConfirmEstateInNativeDialog(),false' in menu
assert '"Confirm grant","Cancel"' in menu
assert '"Confirm","Cancel"' in menu
assert 'ScreenManager.PushScreen' not in menu
assert 'Gauntlet' not in menu
assert '"CU: Review my service, relatives and recorded history"' not in behavior
assert 'SandboxAgencyDecisionPolicy.ChooseInitiative(' in menu
assert 'public static string Choose(' in policy
assert 'SamePlace(player,hero)' in menu
assert 'other.StayingInSettlement==settlement' in menu
assert "other.HomeSettlement==settlement" not in menu
assert 'SandboxAgencyDecisionPolicy.NativeTransferPermitted(' in menu
assert 'GiveGoldAction.ApplyBetweenCharacters(' in menu
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in menu
assert 'NativeResidenceChangedObserved' in read("SandboxLivingPersons.cs")
assert 'SandboxAgencyDecisionPolicy.ExpiryDays' in agency
assert 'SandboxAgencyDecisionPolicy' in agency and 'MaxRequestsPerHero' in agency
assert 'CU_AGENCY_1' in agency
assert 'CU_NEGOTIATION_1' in state
assert 'SandboxInheritanceNegotiationPolicy.Evaluate(' in menu
assert 'SandboxInheritanceNegotiationPolicy.TransferGate(' in menu
assert 'ChangeOwnerOfSettlementAction.ApplyByDefault(' in menu
assert 'bool confirmed=estate.OwnerClan==Clan.PlayerClan' in menu
assert 'inheritanceNegotiation.Complete(confirmed)' in menu
assert '!inheritanceNegotiation.NativeHeadConsentObserved' in menu
assert 'if(!CanConfirmFamilyGrant())' in menu
assert 'current==primary?null:current' in menu
assert 'Clan.PlayerClan.Kingdom==clan.Kingdom' in menu
assert 'player.Father!=null &&' in menu and '!player.Father.IsAlive' in menu
assert 'currentBirthHoldings>=2' in inherit
assert 'if(!fatherDeceased)return "ParentStillLiving"' in inherit
assert 'if(!sameNativeKingdom)return "NativeAllegianceRequired"' in inherit
assert 'if(nativeBirthClanFiefs<2)return "NoSecondaryEstate"' in inherit
assert 'ConsentThreshold' in inherit
assert 'Sim' in sim and 'Run(int seeds,int days)' in sim
assert '120 campaign seeds for two years' in (
    root/"Tests"/"PolicySmoke"/"Program.cs").read_text(encoding="utf-8")
print("PASS: native complementary People/Lineage menu, independent NPC requests, two-stage cultural council grants and simulated estate safeguards (SOURCE ONLY)")
