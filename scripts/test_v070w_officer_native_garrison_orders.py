#!/usr/bin/env python3
"""Source-only guard for real Lord and King companion command contracts.

The exact Bannerlord runtime must still confirm the conversation,
five-troop roster transfer, duplicate prevention and save/reload.
"""
from pathlib import Path
import importlib.util
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
s=read("SandboxOriginBehavior.NobleOfficerDialogues.cs")
origin=read("SandboxOriginBehavior.cs")
council=read("SandboxOriginBehavior.KingCouncil.cs")
retinue=read("SandboxCulturalRetinue.cs")
assert 'RegisterNobleOfficerConversations(starter);' in origin
assert 'if(!Settings.NativeOfficerTransfersEnabled)return false;' in s
assert 'NativeOfficerTransfersEnabled { get { return false; } }' in read("Settings.cs")
for phrase in ('Hero.OneToOneConversationHero==officer',
    'RealOfficerSpeaking("Master of Arms")',
    'RealOfficerSpeaking("Marshal")',
    'lordInheritance.IsRecorded',
    'kingSuccession.IsRecorded',
    'NativeRoyalCouncilPresent(true)',
    'royalCouncil.LastMeetingDay==',
    'CanNativeOfficerTransfer(RecordedRoyalSeat())',
    'seat.OwnerClan!=clan',
    'seat.IsUnderSiege',
    'party.MemberRoster.FindIndexOfTroop(troop)',
    'element.WoundedNumber==0 && element.Xp==0',
    'party.AddToCounts(troop,-5)',
    'garrison.AddToCounts(troop,5)',
    'oldParty-5',
    'oldGarrison+5',
    '"cu_lord_master_native_garrison_order"',
    '"cu_king_marshal_native_court_order"',
    '"|troops_spawned=0"',
    'starter.AddPlayerLine(',
    'starter.AddDialogLine('):
    assert phrase in s,phrase
assert 'new[]{"MasterOfArms","Knight"}' in read("SandboxNobleStartPolicy.cs")
assert '"Marshal","Kingsguard","RoyalMinister"' in read("SandboxKingStartPolicy.cs")
assert 'SandboxKingNativeMinistry.VerifiedGovernor(minister)' in council
assert 'officer.SetNewOccupation(' in retinue
assert 'AddCompanionAction.Apply(Clan.PlayerClan,officer);' in retinue
assert 'ChangeOwnerOfSettlementAction' not in s
assert 'ChangeKingdomAction' not in s
assert 'HeroCreator.Create' not in s
spec=importlib.util.spec_from_file_location("gate",
    root/"scripts"/"audit_v070_seven_live_start_evidence.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
for role,receipt in (
    ("Lord","cu_lord_master_native_garrison_order"),
    ("King","cu_king_marshal_native_court_order")):
    assert receipt in mod.REQUIRED[role]
    assert mod.GOOD[receipt] == (
        "result=PASS|troops_transferred=5|troops_spawned=0|real_garrison=True",)
print("PASS v0.70w SOURCE: historical officer contracts remain guarded; "+
      "direct transfers are held during acceptance. Historical live "+
      "transfer receipt requirement remains unfulfilled.")
