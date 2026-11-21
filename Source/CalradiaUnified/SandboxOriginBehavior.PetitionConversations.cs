using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

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

        private void InvitePetitionFromSpeakingHero()
        {
            var actor=Hero.OneToOneConversationHero;
            string kind=AvailablePetitionForSpeaker();
            if(actor==null || kind.Length==0)return;
            if(agencyDecisions.Propose(actor.StringId,kind))
            {
                livingPersons.Remember("IndependentCharacterRequest",
                    actor.StringId,Hero.MainHero.StringId,
                    "kind="+kind+
                    "|source=face_to_face_private_petition"+
                    "|actor_physically_present=true"+
                    "|no_fabricated_prior_promise=true");
                DiagnosticJournal.Event("cu_character_petition_conversation",
                    "result=OFFERED|hero="+actor.StringId+
                    "|kind="+kind+
                    "|actual_dialogue=true|no_remote_choice=true");
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

        private void RegisterPrivatePetitionConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_petition_hear_pending","hero_main_options",
                "cu_petition_open",
                "You wished to speak privately with me?",
                ()=>SpeakingWithPendingPetitioner(),null,115);
            starter.AddPlayerLine(
                "cu_petition_invite","hero_main_options",
                "cu_petition_open",
                "How fares your work and your household?",
                ()=>AvailablePetitionForSpeaker().Length>0,
                ()=>InvitePetitionFromSpeakingHero(),90);
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
            starter.AddPlayerLine(
                "cu_petition_accept","cu_petition_answers",
                "cu_petition_accept_reply",
                "You shall have my support.",
                ()=>CanAcceptPetitionInConversation(),
                ()=>AnswerCharacterRequest(true),120);
            starter.AddDialogLine(
                "cu_petition_accept_reply","cu_petition_accept_reply",
                "hero_main_options",
                "I have heard your answer. Thank you for "+
                "taking my affairs seriously.",
                null,null,120);
            starter.AddPlayerLine(
                "cu_petition_refuse","cu_petition_answers",
                "cu_petition_refuse_reply",
                "I cannot agree to your request.",
                ()=>SpeakingWithPendingPetitioner(),
                ()=>AnswerCharacterRequest(false),110);
            starter.AddDialogLine(
                "cu_petition_refuse_reply","cu_petition_refuse_reply",
                "hero_main_options",
                "I understand. I shall make other plans.",
                null,null,110);
            starter.AddPlayerLine(
                "cu_petition_no_coin","cu_petition_answers",
                "cu_petition_answers",
                "I have not the silver to offer you.",
                ()=>CannotAffordPetitionInConversation(),
                null,80);
            starter.AddPlayerLine(
                "cu_petition_defer","cu_petition_answers",
                "hero_main_options",
                "Let us speak of this another time.",
                ()=>SpeakingWithPendingPetitioner(),null,70);
        }
    }
}
