#!/usr/bin/env python3
"""Static regression for real patron talks; runtime screenshot still needed."""
from pathlib import Path
import importlib.util
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
dialogue=read("SandboxOriginBehavior.KnightDialogue.cs")
oath=read("SandboxOriginBehavior.KnightOath.cs")
state=read("SandboxKnightOathState.cs")
escort=read("SandboxKnightEscortState.cs")
household=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
behavior=read("SandboxOriginBehavior.cs")
house=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
for key in ('starter.AddPlayerLine(', 'starter.AddDialogLine(',
    '"hero_main_options"', 'Hero.OneToOneConversationHero',
    'speaker==patron', 'speaker.IsAlive',
    'speaker.Clan.Kingdom.StringId==knightOath.KingdomId',
    'knightOath.RecordMuster(day,true)',
    'knightOath.Release(true)',
    'KnightOathNativeRelation(patron,2,',
    '"cu_knight_patron_conversation_muster"',
    'native_conversation=true',
    '"cu_knight_successor_talk"',
    'RenewOathInConversation()'):
    assert key in dialogue,key
assert 'RegisterKnightPatronConversations(starter);' in behavior
assert 'Hero.AllAliveHeroes.FirstOrDefault(' in house
assert 'ResolveCampaignHero(knightOath.PatronId)' in oath
assert 'KnightPatronWhereabouts' in oath
assert 'Speak to the lord through Bannerlord' in oath
assert 'LastMusterDay<0 ||' in state
assert 'day>=NextMusterDay-MusterWindowDays' in state
assert '"cu_knight_oath_muster"' not in oath
assert '"cu_knight_oath_release"' not in oath
assert '"cu_knight_oath_renew"' not in oath
assert 'ChangeKingdomAction' not in dialogue+oath
assert 'ChangeClanLeaderAction' not in dialogue+oath
assert '"CU_KNIGHT_OATH_1"' in state
assert '"CU_KNIGHT_ESCORT_1"' in escort
assert 'RequiredDays=3' in escort
assert 'day!=LastObservedDay+1' in escort
assert 'if(!nearNativePatronParty)' in escort
assert 'CompletedEscorts++' in escort
assert 'Serialize()' in escort and 'Parse(string text)' in escort
assert 'private void ObserveKnightEscortDaily()' in dialogue
assert 'lordParty.Position2D.Distance(playerParty.Position2D)' in dialogue
assert 'lordParty.LeaderHero==patron' in dialogue
assert 'native_hero_party=' in dialogue
assert '"cu_knight_native_escort_completed"' in dialogue
assert 'ChangeRelationAction' not in escort
assert 'escort.Status' not in household  # uses actual instance and state instead
spec=importlib.util.spec_from_file_location("native_gate",
    root/"scripts"/"audit_v070_seven_live_start_evidence.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
assert "cu_knight_patron_conversation_muster" in mod.REQUIRED["Knight"]
assert "knight_patron_conversation.png" in mod.ROLE_UI_SCREENSHOTS["Knight"]
assert "knight_escort_map.png" in mod.ROLE_UI_SCREENSHOTS["Knight"]
assert "cu_knight_native_escort_completed" in mod.REQUIRED["Knight"]
assert mod.GOOD["cu_knight_patron_conversation_muster"] == (
    "result=native_meeting_muster_recorded|native_conversation=true|native_favor_action=True",)
print("PASS v0.70u STATIC: native patron conversation and 3 verified "
      "escort map days with save-backed one-shot native relation; "
      "no fake vassalage. Live interaction still unverified.")
