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
            int decisionDay=(int)Math.Floor(CampaignTime.Now.ToDays);
            bool socialRecorded=socialBonds.RecordPetition(
                person.StringId,accepted,relationDelta,decisionDay);
            DiagnosticJournal.Event("cu_social_native_decision",
                "hero="+person.StringId+
                "|verified_relation_delta="+relationDelta+
                "|accepted="+accepted+
                "|social_receipt="+socialRecorded+
                "|native_relation_not_overridden=true");
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            var story=petitionStories.For(person.StringId);
            if(story==null || story.Kind!=kind ||
                (long)day-story.StartedDay>=SandboxAgencyDecisionPolicy.ExpiryDays)return;
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
                story.ResolvedDay>=0 &&
                (long)CurrentHouseholdDay()-story.ResolvedDay>=SandboxPetitionStoryState.FollowUpDays;
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
            socialBonds.RecordFollowUp(person.StringId,
                after-before,day);
            DiagnosticJournal.Event("cu_petition_story_personal_followup",
                "person="+person.StringId+
                "|story="+story.Kind+
                "|original_choice="+story.Outcome+
                "|before="+before+"|after="+after+
                "|native_relation_observed="+(after-before)+
                "|repeated_reward_blocked=true");
        }
        private bool PetitionStoryKind(string kind)
        {
            var speaker=Hero.OneToOneConversationHero;
            if(speaker==null)return false;
            var story=petitionStories.For(speaker.StringId);
            return story!=null && story.Kind==kind &&
                story.ResolvedDay>=0;
        }

        private void RegisterPetitionStoryConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_petition_story_return_supported",
                "cu_personal_matters_topics",
                "cu_petition_story_supported_reply",
                "How have your affairs fared since I helped you?",
                ()=>IsPetitionStoryFollowUp(true),
                ()=>CompletePetitionStoryFollowUp(),115);
            starter.AddDialogLine(
                "cu_petition_story_recognition_reply",
                "cu_petition_story_supported_reply",
                "cu_personal_matters_topics",
                "You recognized the work I had done. "+
                "I remember it. My duties and hopes are still my own.",
                ()=>PetitionStoryKind("Recognition"),null,115);
            starter.AddDialogLine(
                "cu_petition_story_household_reply",
                "cu_petition_story_supported_reply",
                "cu_personal_matters_topics",
                "I remember the help you gave my household. "+
                "Our affairs remain our own to manage, "+
                "but I value your trust.",
                ()=>PetitionStoryKind("HouseholdAid"),null,115);
            starter.AddDialogLine(
                "cu_petition_story_livelihood_reply",
                "cu_petition_story_supported_reply",
                "cu_personal_matters_topics",
                "Your support gave me a chance to keep working "+
                "toward my livelihood. I have not forgotten it.",
                ()=>PetitionStoryKind("LivelihoodAid"),null,115);
            starter.AddPlayerLine(
                "cu_petition_story_return_refused",
                "cu_personal_matters_topics",
                "cu_petition_story_refused_reply",
                "Have you found another way forward?",
                ()=>IsPetitionStoryFollowUp(false),
                ()=>CompletePetitionStoryFollowUp(),115);
            starter.AddDialogLine(
                "cu_petition_story_refused_guarded",
                "cu_petition_story_refused_reply",
                "cu_personal_matters_topics",
                "Your answer was plain enough. I have kept "+
                "my own counsel and made what plans I can.",
                ()=>PetitionerStyle()=="Guarded",null,110);
            starter.AddDialogLine(
                "cu_petition_story_refused_reply",
                "cu_petition_story_refused_reply",
                "cu_personal_matters_topics",
                "I have made what plans I can. "+
                "We may still speak, even after a refusal.",
                ()=>PetitionerStyle()!="Guarded",null,110);
        }
    }
}
