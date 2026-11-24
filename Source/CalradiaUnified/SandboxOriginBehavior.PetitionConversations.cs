using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // Private petitions belong to the actual speaking character.
        // A remote Clan/Party screen is only a ledger, never a way to
        // conjure a character or carry out a payment at a distance.
        private bool SpeakingWithPendingPetitioner()
        {
            var actor=Hero.OneToOneConversationHero;
            return applied && actor!=null && actor.IsAlive &&
                actor.IsActive && !actor.IsPrisoner &&
                agencyDecisions.HasPending &&
                actor.StringId==agencyDecisions.PendingHeroId &&
                RequestedCharacter()==actor;
        }

        private string AvailablePetitionForSpeaker()
        {
            var player=Hero.MainHero;
            var actor=Hero.OneToOneConversationHero;
            if(!applied || player==null || actor==null ||
                agencyDecisions.HasPending || !actor.IsAlive ||
                !actor.IsActive || actor.IsPrisoner ||
                actor.Age<18f || !SamePlace(player,actor) ||
                !livingPersons.People.Any(p=>p.Id==actor.StringId))
                return "";
            var agenda=characterAgendas.Get(actor.StringId);
            if(agenda==null)return "";
            var history=agencyDecisions.For(actor.StringId);
            if(!SandboxAgencyDecisionPolicy.CanReapproach(
                socialBonds.For(actor.StringId),player.GetRelation(actor)))return "";
            return SandboxAgencyDecisionPolicy.ChooseInitiative(
                agenda.Goal,true,true,player.GetRelation(actor),
                history==null?0:history.RequestsMade,
                history==null?1000:
                    agencyDecisions.Day-history.LastDay,
                actor.GetTraitLevel(DefaultTraits.Honor),
                actor.GetTraitLevel(DefaultTraits.Generosity),
                actor.GetTraitLevel(DefaultTraits.Calculating),
                actor.Gold);
        }

        private string PetitionerStyle()
        {
            var speaker=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(speaker==null || player==null)return "Direct";
            var agenda=characterAgendas.Get(speaker.StringId);
            return SandboxPetitionVoicePolicy.Style(
                agenda==null?"":agenda.Goal,
                speaker.GetTraitLevel(DefaultTraits.Honor),
                speaker.GetTraitLevel(DefaultTraits.Generosity),
                speaker.GetTraitLevel(DefaultTraits.Calculating),
                player.GetRelation(speaker));
        }

        private void InvitePetitionFromSpeakingHero()
        {
            var actor=Hero.OneToOneConversationHero;
            string kind=AvailablePetitionForSpeaker();
            if(actor==null || kind.Length==0)return;
            if(agencyDecisions.Propose(actor.StringId,kind))
            {
                RememberPetitionStory(actor,kind);
                livingPersons.Remember("IndependentCharacterRequest",
                    actor.StringId,Hero.MainHero.StringId,
                    "kind="+kind+
                    "|source=face_to_face_private_petition"+
                    "|actor_physically_present=true"+
                    "|no_fabricated_prior_promise=true");
                DiagnosticJournal.Event("cu_character_petition_conversation",
                    "result=OFFERED|hero="+actor.StringId+
                    "|kind="+kind+
                    "|actual_dialogue=true|no_remote_choice=true"+
                    "|voice="+PetitionerStyle());
            }
        }

        private bool CanAcceptPetitionInConversation()
        {
            if(!SpeakingWithPendingPetitioner())return false;
            int cost=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            return cost>=0 && Hero.MainHero.Gold>=cost;
        }

        private bool CannotAffordPetitionInConversation()
        {
            if(!SpeakingWithPendingPetitioner())return false;
            int cost=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            return cost>0 && Hero.MainHero.Gold<cost;
        }

        private string petitionDecisionReply="";
        private bool PreparePetitionDecisionReply()
        {
            MBTextManager.SetTextVariable("CU_PETITION_DECISION_REPLY",
                new TextObject(petitionDecisionReply));
            return true;
        }
        private void RegisterPrivatePetitionConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_petition_hear_pending","cu_personal_matters_topics",
                "cu_petition_open",
                "You wished to speak privately with me?",
                ()=>SpeakingWithPendingPetitioner(),null,115);
            starter.AddPlayerLine(
                "cu_petition_invite","cu_personal_matters_topics",
                "cu_petition_open",
                "Is there a particular matter you wish to put before me?",
                ()=>AvailablePetitionForSpeaker().Length>0,
                ()=>InvitePetitionFromSpeakingHero(),90);
            starter.AddDialogLine(
                "cu_petition_no_longer_available","cu_petition_open",
                "cu_personal_matters_topics","There is nothing I need to ask of you just now.",
                ()=>!SpeakingWithPendingPetitioner(),null,80);
            starter.AddDialogLine(
                "cu_petition_audience","cu_petition_open",
                "cu_petition_answers",
                "I wished to speak with you about our houses. Let us part with a clear understanding.",
                ()=>SpeakingWithPendingPetitioner() &&
                    agencyDecisions.PendingKind=="Audience",null,125);
            starter.AddDialogLine(
                "cu_petition_recognition","cu_petition_open",
                "cu_petition_answers",
                "I have served as best I can. I ask that "+
                "my work not go unnoticed.",
                ()=>SpeakingWithPendingPetitioner() &&
                    agencyDecisions.PendingKind=="Recognition",
                null,120);
            starter.AddDialogLine(
                "cu_petition_household","cu_petition_open",
                "cu_petition_answers",
                "My household is short of means. "+
                "A little silver would bring us some relief.",
                ()=>SpeakingWithPendingPetitioner() &&
                    agencyDecisions.PendingKind=="HouseholdAid",
                null,120);
            starter.AddDialogLine(
                "cu_petition_livelihood","cu_petition_open",
                "cu_petition_answers",
                "I have a chance to establish myself. "+
                "I need help to make a beginning.",
                ()=>SpeakingWithPendingPetitioner() &&
                    agencyDecisions.PendingKind=="LivelihoodAid",
                null,120);
            // Asking for a reason changes no gold or relationship.
            // The reply comes from recorded ambition, current
            // relation and real native Hero traits.
            starter.AddPlayerLine(
                "cu_petition_personal_reason","cu_petition_answers",
                "cu_petition_personal_reason_reply",
                "What is most important to you in this matter?",
                ()=>SpeakingWithPendingPetitioner() && agencyDecisions.PendingKind!="Audience",null,90);
            starter.AddDialogLine(
                "cu_petition_reason_guarded",
                "cu_petition_personal_reason_reply",
                "cu_petition_answers",
                "I do not ask you to trust me blindly. "+
                "Give me a plain answer, so I can plan my own way.",
                ()=>SpeakingWithPendingPetitioner() &&
                    PetitionerStyle()=="Guarded",null,120);
            starter.AddDialogLine(
                "cu_petition_reason_duty",
                "cu_petition_personal_reason_reply",
                "cu_petition_answers",
                "My first concern is the household that "+
                "depends upon my work. I mean to carry my share.",
                ()=>SpeakingWithPendingPetitioner() &&
                    PetitionerStyle()=="Duty",null,120);
            starter.AddDialogLine(
                "cu_petition_reason_ambition",
                "cu_petition_personal_reason_reply",
                "cu_petition_answers",
                "I want the chance to build a livelihood "+
                "through my own effort. This request is one step.",
                ()=>SpeakingWithPendingPetitioner() &&
                    PetitionerStyle()=="Ambitious",null,120);
            starter.AddDialogLine(
                "cu_petition_reason_warm",
                "cu_petition_personal_reason_reply",
                "cu_petition_answers",
                "I came to you in trust, and I hope "+
                "we shall still speak freely after your answer.",
                ()=>SpeakingWithPendingPetitioner() &&
                    PetitionerStyle()=="Warm",null,120);
            starter.AddDialogLine(
                "cu_petition_reason_direct",
                "cu_petition_personal_reason_reply",
                "cu_petition_answers",
                "I ask for your answer. Whatever you decide, "+
                "the work ahead will be mine to do.",
                ()=>SpeakingWithPendingPetitioner() &&
                    PetitionerStyle()=="Direct",null,120);
            starter.AddPlayerLine(
                "cu_petition_accept_audience","cu_petition_answers",
                "cu_petition_accept_reply",
                "You have my ear. Let us remain on good terms.",
                ()=>SpeakingWithPendingPetitioner() && agencyDecisions.PendingKind=="Audience",
                ()=>AnswerCharacterRequest(true),125);
            starter.AddPlayerLine(
                "cu_petition_accept","cu_petition_answers",
                "cu_petition_accept_reply",
                "You shall have my support.",
                ()=>CanAcceptPetitionInConversation() && agencyDecisions.PendingKind!="Audience",
                ()=>AnswerCharacterRequest(true),120);
            starter.AddDialogLine(
                "cu_petition_accept_reply","cu_petition_accept_reply",
                "cu_personal_matters_topics",
                "{CU_PETITION_DECISION_REPLY}",
                ()=>PreparePetitionDecisionReply(),null,120);
            starter.AddPlayerLine(
                "cu_petition_refuse","cu_petition_answers",
                "cu_petition_refuse_reply",
                "I cannot agree to your request.",
                ()=>SpeakingWithPendingPetitioner(),
                ()=>AnswerCharacterRequest(false),110);
            starter.AddDialogLine(
                "cu_petition_refuse_reply","cu_petition_refuse_reply",
                "cu_personal_matters_topics",
                "{CU_PETITION_DECISION_REPLY}",
                ()=>PreparePetitionDecisionReply(),null,110);
            starter.AddPlayerLine(
                "cu_petition_no_coin","cu_petition_answers",
                "cu_personal_matters_topics",
                "I have not the silver to offer you.",
                ()=>CannotAffordPetitionInConversation(),
                null,80);
            starter.AddPlayerLine(
                "cu_petition_defer","cu_petition_answers",
                "cu_personal_matters_topics",
                "Let us speak of this another time.",
                ()=>SpeakingWithPendingPetitioner(),null,70);
        }
    }
}
