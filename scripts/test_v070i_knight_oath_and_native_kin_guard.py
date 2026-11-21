#!/usr/bin/env python3
"""Source-only guard. Native game clan transfer and oath actions remain untested."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
get=lambda name:(src/name).read_text(encoding="utf-8")
state=get("SandboxKnightOathState.cs")
native=get("SandboxOriginBehavior.KnightOath.cs")
origin=get("SandboxOriginBehavior.cs")
policy=get("SandboxKnightStartPolicy.cs")
profile=get("SandboxOriginProfile.cs")
vm=get("CUGuidedCreatorVM.cs")
assert '"native_same_clan_kinship_not_implemented"' in policy
assert 'if(origin=="YoungerSibling")' in policy
assert 'KnightGuidedStart' in profile
assert 'KnightOrigin' in profile
assert 'RegisterKnightOathMenus(starter);' in origin
assert 'this, ObserveKnightOathDaily);' in origin
assert 'store.SyncData("CU_v070i_knight_personal_oath",ref oath);' in origin
assert 'knightOath=SandboxKnightOathState.Parse(oath);' in origin
assert 'profile.KnightGuidedStart &&' in origin
assert 'knightOath.Begin(' in origin
assert 'if (failures.Length != 0)' in origin
assert '"cu_knight_oath_start"' in origin
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in native
assert 'KnightOathNativeRelation(patron,-3,' in native
assert 'KnightOathNativeRelation(patron,-20,' in native
assert 'AvailableKnightSuccessor()' in native
assert 'successor.Clan.Kingdom.StringId' in native
assert 'knightOath.AcceptSuccessor(' in native
assert 'knightOath.RecordMuster(' in native
assert 'CanKnightMuster()' in native
assert 'KnightOathInPerson(' in native
assert 'knightOath.Release(true);' in native
assert 'patron.IsPrisoner' in native
assert 'patron_captivity_pause' in state
assert 'patron_returned_muster_reset' in state
assert '"cu_knight_oath_confirm_break"' in native
assert 'your personal oath' in origin
assert 'Native relation:' in native
assert 'patron.Clan.Name.ToString()' in native
assert 'KnightOrigin","Your knightly origin"' in vm
assert '30-day muster' in vm
assert 'public const string Version="CU_KNIGHT_OATH_1";' in state
for token in ('MusterDays=30','MusterWindowDays=7','Status="SuccessionPending"',
              'Status="Overdue"','Status="Released"','Status="Broken"',
              'MissedMusters++','CompletedMusters++','Successions++',
              'Serialize()','Parse(string text)'):
    assert token in state,token
for text in (native,origin):
    assert not re.search(r'\bHero\.MainHero\.Clan\s*=(?!=)',text)
    assert not re.search(r'\bClan\.PlayerClan\s*=(?!=)',text)
    assert 'ChangeClanLeaderAction' not in text
print("PASS v0.70i SOURCE: save-backed, one-shot Knight oath with "
      "native relation actions; unsupported clan reparent remains blocked. "
      "REAL GAME AND SAVE/RELOAD NOT TESTED.")
