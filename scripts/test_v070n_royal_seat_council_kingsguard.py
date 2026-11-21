#!/usr/bin/env python3
"""Source audit: one real Kingsguard Hero, verified governor and council."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
ministry=read("SandboxKingNativeMinistry.cs")
council=read("SandboxOriginBehavior.KingCouncil.cs")
state=read("SandboxRoyalCouncilState.cs")
behavior=read("SandboxOriginBehavior.cs")
court=read("SandboxCulturalRetinue.cs")
policy=read("SandboxKingStartPolicy.cs")
preflight=read("SandboxStartingLifePreflight.cs")
assert not (src/"SandboxKingsguardState.cs").exists()
assert 'VerifiedRoyalCourtSeat()' in ministry
assert '"InitialHomeSettlement","InitialHomeLand"' in ministry
assert 'nativeHome!=null && owned.Contains(nativeHome)' in ministry
assert 's.OwnerClan==clan' in ministry
assert 'ChangeGovernorAction.Apply(seat.Town,minister);' in ministry
assert 'minister.GovernorOf==seat.Town' in ministry
assert 'seat.Town.Governor==minister' in ministry
assert 'VacantOwnedFief()' in preflight
for token in (
    'NativeRoyalCouncilPresent(true)',
    'Settlement.CurrentSettlement==seat',
    'clan.Kingdom.RulingClan!=clan',
    'SandboxKingNativeMinistry.VerifiedGovernor(minister)',
    'minister.GovernorOf.Settlement!=seat',
    'cu_king_native_council_meeting',
    'native_kingdom_politics_unchanged=true',
    'NativeCourtOfficer("Marshal")',
    'NativeCourtOfficer("Royal Minister")',
    'NativeCourtOfficer("Kingsguard")',
    'party.MemberRoster.GetTroopCount(',
    'kingsguard.CharacterObject',
    'royalCouncil.KingsguardHeroId',
    'hero',
):
    assert token in council,token
for token in (
    'public const string Version="CU_ROYAL_HERO_COUNCIL_1"',
    'public string MarshalId="",MinisterId="",KingsguardHeroId=""',
    'Distinct(StringComparer.Ordinal).Count()==4',
    'public string Convene(',
    'native_royal_hero_council_commissioned',
    'real_royal_hero_council_convened',
    'public static SandboxRoyalCouncilState Parse(',
    'council_day_invalid',
    'native_royal_council_not_present'
):
    assert token in state,token
for token in (
    'RegisterNativeRoyalCouncilMenus(starter);',
    'CU_v070n_single_hero_royal_council',
    'SandboxRoyalCouncilState.Parse(council)',
    'BeginNativeRoyalCouncil(hero)'
):
    assert token in behavior,token
assert '"Marshal","Kingsguard","RoyalMinister"' in policy
assert 'offices=SandboxKingStartPolicy.CourtOffices;' in court
assert 'AddCompanionAction.Apply(Clan.PlayerClan,officer);' in court
assert 'AddHeroToPartyAction.Apply(' in court
assert 'SandboxKingNativeMinistry.TryAppoint(' in court
for token in (
    'RemoveTroop(', 'AddToCounts(', 'native_guard_recruitment',
    'MaxGuard', 'RecruitBatch', 'HasNativeGuardGear(',
    'EligibleNativeGuardTroop(', 'garrison.MemberRoster',
    'ChangeOwnerOfSettlementAction','ChangeKingdomAction',
    'new CharacterObject',
):
    assert token not in council+state+ministry,token
print("PASS v0.70n SOURCE: Kingsguard is ONE native Hero companion "
      "with culturally equipped native kit, not recruitable troops. "
      "Royal Minister uses actual governor action; live native play untested.")
