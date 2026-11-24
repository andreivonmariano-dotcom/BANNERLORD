using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxStoryDirectorState storyDirector=new SandboxStoryDirectorState();

        // Consume only observations emitted by the existing daily pass.
        // No additional world-wide Hero scan and no native AI orders.
        private void ObserveStoryDirector(Hero player,SandboxLifeHistoryState.Event previous)
        {
            int day=CurrentHouseholdDay();
            storyDirector.Expire(day);
            if(player==null || !storyDirector.CanOpen(day))return;
            int index=previous==null?-1:livingPersons.Events.IndexOf(previous);
            // If the previous tail was evicted, skip ambiguous older evidence.
            if(previous!=null && index<0)return;
            var recent=livingPersons.Events.Skip(index+1).Take(8).ToArray();
            var candidates=new System.Collections.Generic.List<Tuple<string,string,string>>();
            foreach(var observed in recent)
            {
                string actor="",kind="";
                if(observed.Kind=="ObservedGoalReassessment" && observed.Target==player.StringId)
                {actor=observed.Actor;kind="ChangedAmbition";}
                else if(observed.Kind=="NativeClanChangedObserved")
                {actor=observed.Actor;kind="ChangedClan";}
                else if(observed.Kind=="NativeRelationChangedObserved" && observed.Actor==player.StringId)
                {
                    var f=observed.Detail.Split('|');int before,after;
                    if(f.Length<2 || !f[0].StartsWith("before=",StringComparison.Ordinal) ||
                        !f[1].StartsWith("after=",StringComparison.Ordinal) ||
                        !Int32.TryParse(f[0].Substring(7),out before) ||
                        !Int32.TryParse(f[1].Substring(6),out after) ||
                        before< -100 || before>100 || after< -100 || after>100 ||
                        Math.Abs(after-before)<10)continue;
                    actor=observed.Target;kind=after>before?"ImprovedStanding":"StrainedStanding";
                }
                if(actor.Length==0 || actor==player.StringId || characterAgendas.Get(actor)==null)continue;
                string fingerprint=SandboxStoryDirectorState.Fingerprint(player.StringId+"|"+actor,kind,observed.Detail);
                if(storyDirector.Receipts.Contains(fingerprint))continue;
                candidates.Add(Tuple.Create(actor,kind,fingerprint));
            }
            if(candidates.Count==0)return;
            EnsureDialogueSeed();
            int offset=dialogueVariety.Variant(player.StringId,day.ToString(),"Director",candidates.Count);
            foreach(var candidate in candidates.Skip(offset).Concat(candidates.Take(offset)))
            {
                if(storyDirector.Open(player.StringId,candidate.Item1,candidate.Item2,candidate.Item3,day))break;
            }
        }
        private string StoryTopic()
        {
            return storyDirector.Kind=="ChangedAmbition" || storyDirector.Kind=="ChangedClan"?
                "Ambitions":"Standing";
        }
        private bool CanDiscussStory(string topic)
        {
            if(!CanSpeakSocially() || SpeakingWithPendingPetitioner() || topic!=StoryTopic())return false;
            var player=Hero.MainHero;var actor=Hero.OneToOneConversationHero;
            return storyDirector.CanDiscuss(player.StringId,actor.StringId,CurrentHouseholdDay()) &&
                CanDiscussPersonalTopic(topic);
        }
        private string StoryWords()
        {
            var actor=Hero.OneToOneConversationHero;
            if(actor==null)return "";
            if(storyDirector.Stage=="FollowUp")
                return storyDirector.Response=="Listen"?
                    "You gave me a hearing when we last spoke of this. I would like to return to the matter without pretending it has been settled.":
                    "You urged caution when we spoke of this. I have not taken that as a promise or an order.";
            if(storyDirector.Kind=="ChangedAmbition")
                return "My circumstances have changed. I would like to speak about the course ahead, rather than merely repeat what I hoped for before.";
            if(storyDirector.Kind=="ChangedClan")
                return "My place among the houses has changed. I would speak about what that means for my obligations.";
            int relation=Hero.MainHero.GetRelation(actor);
            return relation<0?"Our dealings have not been easy. I would like to speak plainly about where we stand.":
                "Our standing has changed. I would like to understand what we expect of one another now.";
        }
        private bool CanAnswerStory(bool follow)
        {
            return CanFollowPersonalTopic() && CanDiscussStory(personalTopicName) &&
                storyDirector.Stage==(follow?"FollowUp":"Open");
        }
        private void AnswerStory(string response)
        {
            personalReply="We can leave the matter for another time.";
            bool follow=response=="Finish";
            if(!CanAnswerStory(follow))return;
            var actor=Hero.OneToOneConversationHero;var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            bool changed=follow?storyDirector.Finish(player.StringId,actor.StringId,day):
                storyDirector.Answer(player.StringId,actor.StringId,response,day);
            if(!changed)return;
            personalReply=follow?"I am glad we have spoken again. Let our future dealings show what becomes of it.":
                response=="Listen"?"I value the chance to speak. We can return to this after some days have passed.":
                response=="Caution"?"I have heard your caution. I would like to consider the matter further before we speak again.":
                "Then I shall leave the matter here. We need not make a promise today.";
        }
        private void CycleSocialDetail()
        {
            storyDirector.SetDetail((storyDirector.Detail+1)%3);
            string words=storyDirector.Detail==0?"Quiet: no new director threads or peer samples.":
                storyDirector.Detail==1?"Balanced: peer samples at most every three days, new threads after a fourteen-day cooldown.":
                "Detailed: peer samples at most daily, new threads after a seven-day cooldown.";
            ShowNativeDetail("Social simulation",words+" Existing conversations, pending stories and native campaign systems remain available.");
        }
        private void RegisterStoryDetailMenus(CampaignGameStarter starter)
        {
            foreach(var source in new[]{"cu_people_lineage","cu_household_camp"})
            {
                string parent=source;
                starter.AddGameMenuOption(parent,"cu_story_detail_"+parent,"Social simulation detail",
                    args=>{args.optionLeaveType=GameMenuOption.LeaveType.Submenu;return GroupLocationValid(parent);},
                    args=>{if(GroupLocationValid(parent))CycleSocialDetail();},false);
            }
        }
    }
}
