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
