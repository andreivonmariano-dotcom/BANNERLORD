using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxPetitionStoryState petitionStories=
            new SandboxPetitionStoryState();

        private void RememberPetitionStory(Hero person,string kind)
        {
            if(person==null || kind==null)return;
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            if(petitionStories.Begin(person.StringId,kind,day))
                DiagnosticJournal.Event("cu_petition_personal_story_opened",
                    "person="+person.StringId+"|kind="+kind+
                    "|native_hero=true|prior_promise_fabricated=false");
        }

        private void ResolvePetitionStory(Hero person,bool accepted,
            string kind,bool paymentVerified,
            int relationDelta,bool relationVerified)
        {
            if(person==null || !relationVerified)return;
            RememberPetitionStory(person,kind);
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            if(petitionStories.Resolve(person.StringId,accepted,
                day,paymentVerified,relationDelta))
                DiagnosticJournal.Event("cu_petition_personal_story_decision",
                    "person="+person.StringId+
                    "|choice="+(accepted?"support":"refuse")+
                    "|relation_actual_delta="+relationDelta+
                    "|native_payment="+paymentVerified+
                    "|next_face_to_face_day="+
                    (day+SandboxPetitionStoryState.FollowUpDays));
        }
        private bool IsPetitionStoryFollowUp(bool supported)
        {
            var person=Hero.OneToOneConversationHero;
            if(!applied || person==null || !person.IsActive ||
                !person.IsAlive || person.IsPrisoner ||
                !SamePlace(Hero.MainHero,person))
                return false;
            var story=petitionStories.For(person.StringId);
            return story!=null &&
                story.Outcome==(supported?"Supported":"Declined") &&
                petitionStories.CanFollowUp(person.StringId,
                    (int)Math.Floor(CampaignTime.Now.ToDays));
        }
        private void CompletePetitionStoryFollowUp()
        {
            var person=Hero.OneToOneConversationHero;
            if(person==null ||
                (!IsPetitionStoryFollowUp(true) &&
                 !IsPetitionStoryFollowUp(false)))
                return;
            var story=petitionStories.For(person.StringId);
            bool helped=story.Outcome=="Supported";
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            if(petitionStories.RecordFollowUp(person.StringId,day)!=
                "face_to_face_story_followup_recorded")return;
            int before=Hero.MainHero.GetRelation(person);
            if(helped)
            {
                try
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        Hero.MainHero,person,1,false);
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_petition_story_native_followup_failed",error);
                }
            }
            int after=Hero.MainHero.GetRelation(person);
            DiagnosticJournal.Event("cu_petition_story_personal_followup",
                "person="+person.StringId+
                "|story="+story.Kind+
                "|original_choice="+story.Outcome+
                "|before="+before+"|after="+after+
                "|native_relation_observed="+(after-before)+
                "|repeated_reward_blocked=true");
        }
        private void RegisterPetitionStoryConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_petition_story_return_supported",
                "hero_main_options",
                "cu_petition_story_supported_reply",
                "How have your affairs fared since I helped you?",
                ()=>IsPetitionStoryFollowUp(true),
                ()=>CompletePetitionStoryFollowUp(),115);
            starter.AddDialogLine(
                "cu_petition_story_supported_reply",
                "cu_petition_story_supported_reply",
                "hero_main_options",
                "Your help gave me room to act. The work is "+
                "still mine to finish, but I shall remember "+
                "that you stood beside me.",
                null,null,110);
            starter.AddPlayerLine(
                "cu_petition_story_return_refused",
                "hero_main_options",
                "cu_petition_story_refused_reply",
                "Have you found another way forward?",
                ()=>IsPetitionStoryFollowUp(false),
                ()=>CompletePetitionStoryFollowUp(),115);
            starter.AddDialogLine(
                "cu_petition_story_refused_reply",
                "cu_petition_story_refused_reply",
                "hero_main_options",
                "I have made what plans I can. Your answer "+
                "was plain, and I have not forgotten it.",
                null,null,110);
        }
    }
}
