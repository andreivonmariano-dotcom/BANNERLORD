#!/usr/bin/env python3
"""Static graph regression: all registered CU conversation states must
have a safe unconditional exit/response, not a native menu re-entry.
This is not an actual engine conversation test."""
from pathlib import Path
import re
from collections import defaultdict
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
def calls(s):
    # Consume nested callback bodies and string concatenations without
    # confusing commas within lambdas or quoted dialogue with arguments.
    for m in re.finditer(r'\bstarter\.(AddPlayerLine|AddDialogLine)\s*\(',s):
        start=m.end()
        depth=0
        quote=None
        args=[]
        i=start
        while i<len(s):
            c=s[i]
            if quote:
                if c=="\\":i+=2;continue
                if c==quote:quote=None
            elif c in ('"',"'"):quote=c
            elif c in "([{":depth+=1
            elif c in ")]}":
                if depth==0:
                    args.append(s[start:i].strip())
                    break
                depth-=1
            elif c=="," and depth==0:
                args.append(s[start:i].strip())
                start=i+1
            i+=1
        assert len(args)>=7,(m.group(1),args)
        yield m.group(1),args
def norm(s):return re.sub(r'\s+','',s)
states=defaultdict(list)
ids=set()
all_lines=[]
for path in sorted(src.glob("*.cs")):
    text=path.read_text(encoding="utf-8-sig")
    for kind,args in calls(text):
        if not args[0].startswith('"cu_'):continue
        id,source,target=map(norm,args[:3])
        assert id not in ids,("duplicate dialogue registration",path.name,id)
        ids.add(id)
        assert target!='"hero_main_options"',("native re-entry",id)
        states[source].append((kind,target,args[4]=="null",args[5]=="null",id))
        all_lines.append((source,target))
assert len(ids)>=90,("dialogue inventory unexpectedly small",len(ids))
for source,edges in states.items():
    if source=='"hero_main_options"':continue
    kinds={e[0] for e in edges}
    assert len(kinds)==1,("mixed speaker state",source,kinds)
    if kinds=={"AddPlayerLine"}:
        assert any(e[1]=='"close_window"' and e[2] and e[3] for e in edges),("missing unconditional farewell",source)
    else:
        assert any(e[2] and e[3] for e in edges),("missing unconditional NPC reply",source)
for source,target in all_lines:
    assert target=='"close_window"' or target in states,("unresolved conversation target",source,target)
for source,edges in states.items():
    if source=='"hero_main_options"':continue
    for e in edges:
        if e[1]=='"close_window"':continue
        assert states[e[1]][0][0]!=e[0],("same speaker transition",source,e[1])
living=(src/"SandboxOriginBehavior.LivingMenus.cs").read_text()
entry=living[living.index('"Attend to local affairs"'):living.index('affairsReturnMenu=from;')]
assert 'args.optionLeaveType=GameMenuOption.LeaveType.Submenu' in entry
assert 'Settlement.CurrentSettlement.OwnerClan==' in entry
diag=(src/"DiagnosticBehavior.cs").read_text()
assert '"cu_export_diagnostics"' not in diag
assert "CaptureDay()" in diag and "campaign_session_launched" in diag
assert (root/"scripts"/"Export-CU-Diagnostics.ps1").is_file()
print(f"PASS v0.74 conversation safety: {len(ids)} CU registrations, {len(states)-1} custom input states; all player states have unconditional native close, NPC states unconditional reply, resolved alternating edges. SOURCE ONLY.")

# Private topic follow-up is read-only and must never leak another speaker's context.
depth=(src/"SandboxOriginBehavior.LivingDialogueDepth.cs").read_text()
follow=depth[depth.index("private string PersonalFollowUpText"):depth.index("private bool PreparePersonalFollowUp")]
assert 'personalTopicActorId==actor.StringId' in depth
assert 'CanDiscussPersonalTopic(personalTopicName)' in depth
assert 'actor==player.Spouse && actor.Spouse==player' in depth
assert 'h.Father==actor || h.Mother==actor' in depth
assert '()=>SelectPersonalTopic(topic)' in depth
assert '"cu_personal_followup_choices","{CU_PERSONAL_TOPIC_REPLY}"' in depth
assert 'PersonalFollowUpText(false)' in depth and 'PersonalFollowUpText(true)' in depth
for forbidden in ('GiveGoldAction','ChangeRelationAction','AddToCounts','Remember(','RecordGift','careerCharge.Start','careerCharge.Close','familyGrowth.Revise'):
    assert forbidden not in follow,("social follow-up mutation",forbidden)
request=living[living.index("private void AnswerCharacterRequestCore"):living.index("private Clan RecordedBirthClan")]
assert '!SpeakingWithPendingPetitioner()' in request
assert request.index('agencyDecisions.Dismiss(relationProof?')<request.index('livingPersons.Remember(\n                relationProof?')
civic=(src/"CivicBehavior.cs").read_text()
for key in ("cu_civic_to_stores","cu_civic_to_garrison","cu_civic_to_correspondence","cu_civic_admin_entry_local"):
    block=civic[civic.index('"'+key+'"'):][:400]
    assert 'GameMenuOption.LeaveType.Submenu' in block,key
officer=(src/"SandboxOriginBehavior.NobleOfficerDialogues.cs").read_text()
assert officer.count('"{CU_OFFICER_TRANSFER_REPLY}"')==2
assert 'confirmed?"Five of your recruits' in officer
print("PASS read-only follow-up, speaker binding, petition consumption ordering, civic icon source and officer result reply contracts. SOURCE ONLY.")

# Living dialogue: exact pair, no reward, no forced native conversation.
history=(src/"SandboxLifeHistoryState.cs").read_text()
assert 'SharedConversationMemory(string player,string speaker)' in history
assert 'e.Actor==player && e.Target==speaker' in history
assert 'e.Actor==speaker && e.Target==player' in history
assert 'RecordConversationChoice(string player,string speaker,string topic' in history
assert 'HasRecentConversationChoice(string player,string speaker,string topic,int day)' in history
assert '(long)day-recorded<7' in history
assert 'CU_LIFE_1' in history
initiative=depth[depth.index("private string PersonalInitiativeTopic"):depth.index("private void RecordPersonalFollowUp")]
for mutation in ("GiveGoldAction","ChangeRelationAction","AddToCounts","Remember(","RecordConversationChoice(","OpenConversation","StartConversation"):
    assert mutation not in initiative,mutation
assert 'SpeakingWithPendingPetitioner()' in initiative
assert 'HasRecentConversationChoice(player.StringId,actor.StringId,topic,day)' in initiative
assert 'e.Actor==actor.StringId && e.Target==player.StringId' in initiative
assert 'b.WoundedHeroIds.Split' in initiative
assert 'RecordPersonalFollowUp(false)' in depth and 'RecordPersonalFollowUp(true)' in depth
assert 'speaker==player.Father || speaker==player.Mother' in (src/"SandboxOriginBehavior.HouseholdDuties.cs").read_text()
assert 'if(proof=="confirmed")livingPersons.Remember("FamilyGiftConfirmed"' in (src/"SandboxOriginBehavior.HouseholdDuties.cs").read_text()
print("PASS exact speaker memory, no reward or forced native dialogue, native parents and confirmed family gifts. SOURCE ONLY.")

# Group conversations are a separate native menu graph, not one-to-one dialogue.
group=(src/"SandboxOriginBehavior.GroupDiscussions.cs").read_text()
origin=(src/"SandboxOriginBehavior.cs").read_text()
assert "RegisterGroupDiscussionMenus(starter);" in origin
for parent in ("cu_people_lineage","cu_household_camp","cu_native_royal_council"):
    assert '"'+parent+'"' in group,parent
for menu in ("cu_group_topics","cu_group_exchange","cu_group_reply"):
    assert 'starter.AddGameMenu("'+menu+'"' in group,menu
assert 'groupParticipants.Count<2 || groupParticipants.Count>4' in group
assert 'groupParticipants.All(person=>available.Contains(person))' in group
assert 'Hero.MainHero.StringId!=groupPlayerId' in group
assert 'person.IsPrisoner || person.Age<18f' in group
assert 'party.MemberRoster.GetTroopCount(person.CharacterObject)==1' in group
assert 'person.StayingInSettlement==seat && person.PartyBelongedTo==null' in group
assert 'person.PartyBelongedTo==null && person.CurrentSettlement==seat' in group
assert 'seat.OwnerClan!=clan' in group and 'seat.IsUnderSiege' in group
assert 'speaker.GetRelation(previous)' in group
assert 'livingPersons.RecordGroupDiscussion(groupPlayerId,speaker.StringId' in group
assert 'LatestGroupDiscussion(player.StringId,actor.StringId)' in depth
assert 'foreach(var source in new[]{"cu_group_topics","cu_group_exchange","cu_group_reply"})' in group
assert 'args=>true,args=>LeaveGroupDiscussion(),false' in group
assert 'if(valid)GameMenu.SwitchToMenu(parent);' in group and 'else GameMenu.ExitToLast();' in group
for forbidden in ("GiveGoldAction","ChangeRelationAction","AddToCounts","OpenConversation","StartConversation","AddPlayerLine","AddDialogLine","SyncData(","ChangeOwnerOfSettlementAction","SetParty"):
    assert forbidden not in group,forbidden
print("PASS group discussion attendance, peer response, bounded memory and unconditional menu exits. SOURCE ONLY.")

# Personal repair and care are stated intentions, not free native relation.
intention=depth[depth.index("private void ExpressRelationshipIntention"):depth.index("private void SelectPersonalTopic")]
assert 'CanExpressRelationshipIntention(repair)' in intention
assert 'livingPersons.RecordRelationshipIntention(player.StringId,actor.StringId' in intention
assert 'LatestRelationshipIntention(player.StringId,actor.StringId)' in depth
assert 'ObservedGoalReassessment' in depth
for forbidden in ("GiveGoldAction","ChangeRelationAction","AddToCounts","SetRelation","ChangeOwnerOfSettlementAction"):
    assert forbidden not in intention,forbidden
print("PASS personal intention and evolving goal references. SOURCE ONLY.")

# Occasion and peer social model: no synthetic marriages, funerals or native changes.
peer=(src/"SandboxOriginBehavior.PeerSocial.cs").read_text()
peerstate=(src/"SandboxPeerSocialState.cs").read_text()
assert 'ObservePeerSocialDaily(player);' in origin
assert 'store.SyncData("CU_peer_social_1",ref peerRecords);' in origin
assert 'ProbeNativeGroupConversationApi();' in origin
assert 'SetupAndStartMissionConversationWithMultipleAgents' in peer and 'AddDialogLineMultiAgent' in peer
assert '|invoked=false|scene_verified=false' in peer
assert 'h!=Hero.MainHero && h.IsAlive && h.IsActive' in peer
assert 'party==b.PartyBelongedTo' in peer and 'seat==b.StayingInSettlement' in peer
assert 'party.MemberRoster.GetTroopCount(a.CharacterObject)==1' in peer
assert 'party.MemberRoster.GetTroopCount(b.CharacterObject)==1' in peer
assert 'Take(12)' in peer and 'MaxPairs=48, Interval=7' in peerstate
assert 'pair.A.GetRelation(pair.B)' in peer
assert 'PeerVoice(pair.A,relation)' in peer and 'PeerVoice(pair.B,relation)' in peer
assert 'Pairs.Any(p=>p.LastDay>=day)' in peerstate
assert 'player.Spouse.Spouse==player' in group and 'groupParticipants.Contains(player.Spouse)' in group
assert 'RememberedFamilyMember()!=null' in group
assert 'occasion=="CourtAssembly")return groupParent=="cu_native_royal_council"' in group
assert 'GroupOccasionArgument(preference)' in group
assert 'ShowGroupPeerStanding()' in group
for forbidden in ("GiveGoldAction","ChangeRelationAction","MarriageAction","KillCharacterAction","DeclareWarAction","MakePeaceAction","AddToCounts","OpenConversation","StartConversation(",".Invoke("):
    assert forbidden not in peer and forbidden not in group,forbidden
print("PASS bounded independent peer observations, real gathering eligibility and non-invoked native scene probe. SOURCE ONLY.")
