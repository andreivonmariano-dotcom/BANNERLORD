#!/usr/bin/env python3
"""Source-only audit of actual royal council and Kingsguard troop changes."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/"Source"/"CalradiaUnified"
read=lambda name:(SRC/name).read_text(encoding="utf-8")
ministry=read("SandboxKingNativeMinistry.cs")
council=read("SandboxOriginBehavior.KingCouncil.cs")
state=read("SandboxKingsguardState.cs")
behavior=read("SandboxOriginBehavior.cs")
policy=read("SandboxKingStartPolicy.cs")
preflight=read("SandboxStartingLifePreflight.cs")
court=read("SandboxCulturalRetinue.cs")
assert 'VerifiedRoyalCourtSeat()' in ministry
assert '"InitialHomeSettlement","InitialHomeLand"' in ministry
assert 'nativeHome!=null && owned.Contains(nativeHome)' in ministry
assert 's.OwnerClan==clan' in ministry
assert 's.IsFortification && s.Town!=null' in ministry
assert 'ChangeGovernorAction.Apply(seat.Town,minister);' in ministry
assert 'minister.GovernorOf==seat.Town' in ministry
assert 'seat.Town.Governor==minister' in ministry
assert 'VerifiedRoyalCourtSeat()' in ministry
assert 'RoyalMinister' in court
assert 'KingGuidedStart' in preflight
assert 'VacantOwnedFief()' in preflight
for phrase in (
    'NativeRoyalCouncilPresent(true)',
    'Settlement.CurrentSettlement==seat',
    'kingdom.RulingClan',
    'native_kingdom_politics_unchanged=true',
    'minister.GovernorOf.Settlement!=seat',
    'SandboxKingNativeMinistry.VerifiedGovernor(minister)',
    'RegisterNativeRoyalCouncilMenus(',
    'cu_king_native_council_meeting',
    'RoyalCourtSeat',
):
    assert phrase.lower() in (council+ministry).lower(),phrase
for phrase in (
    'SandboxKingsguardState.CanRecruit(',
    'garrison.MemberRoster.RemoveTroop(troop,count);',
    'party.MemberRoster.AddToCounts(troop,count);',
    'GetTroopCount(troop)',
    'HasNativeGuardGear(',
    'c.Culture==king.Culture',
    'troop.BattleEquipments',
    'party.Party.PartySizeLimit',
    'native_guard_recruitment_verified',
    'native_guard_transfer_partial_review_required',
    'native_kit_armor_shield_melee=true',
):
    assert phrase in council+state,phrase
for phrase in (
    'LastObservedNativeCount',
    'RecordNativeTransfer(',
    'MaxGuard=12',
    'RecruitBatch=2',
    'MinNativeTier=3',
    'public static SandboxKingsguardState Parse',
    'native_guard_casualties_observed',
):
    assert phrase in state,phrase
for phrase in (
    'ObserveKingsguardDaily);',
    'RegisterNativeRoyalCouncilMenus(starter);',
    'CU_v070n_kingsguard_actual_native_troops',
    'CU_v070n_royal_council_day',
    'CU_v070n_royal_council_meetings',
    'BeginNativeRoyalCouncil(hero)',
):
    assert phrase in behavior,phrase
# No fabricated troop type or ruler, capital setter or noble land grant.
for code in (council,ministry,state):
    for forbidden in (
        'HeroCreator.CreateSpecialHero',
        'ChangeOwnerOfSettlementAction',
        'ChangeRulingClanAction',
        'ChangeKingdomAction',
        'TroopObjectManager.RegisterObject',
        'new CharacterObject',
        '.RulingClan =',
        '.Governor =',
    ):
        assert forbidden not in code,forbidden
assert '"Marshal","Kingsguard","RoyalMinister"' in policy
print("PASS v0.70n SOURCE: real royal court, owned royal seat, "
      "governor link, garrison-conserved armored Kingsguard soldiers, "
      "save-backed casualties and no fabricated native authority. "
      "LIVE IN-GAME ACCEPTANCE NOT TESTED.")
