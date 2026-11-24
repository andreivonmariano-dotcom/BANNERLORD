using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxKnightEscortState knightEscort=
            new SandboxKnightEscortState();
        // Bannerlord's REAL lord conversation, both in settlements and
        // from encounters on the campaign map. This does not create a
        // replica NPC, a menu-only contact or a fictional kingdom office.
        private bool SpeakingWithSwornPatron()
        {
            var speaker=Hero.OneToOneConversationHero;
            var patron=KnightOathPatron();
            return knightOath.HasCurrentOath &&
                Hero.MainHero!=null &&
                Hero.MainHero.StringId==knightOath.PlayerId &&
                speaker!=null && patron!=null &&
                speaker==patron && speaker.IsAlive &&
                speaker.IsActive && !speaker.IsPrisoner &&
                speaker.Clan!=null &&
                speaker.Clan.StringId==knightOath.PatronClanId &&
                speaker.Clan.Kingdom!=null &&
                speaker.Clan.Kingdom.StringId==knightOath.KingdomId;
        }
        private bool KnightConversationMusterAvailable()
        {
            return SpeakingWithSwornPatron() &&
                knightOath.CanMuster(CurrentOathDay(),true);
        }
        private void RecordNativePatronAudience()
        {
            var patron=KnightOathPatron();
            int day=CurrentOathDay();
            if(patron==null || !KnightConversationMusterAvailable())
                return;
            string receipt=knightOath.RecordMuster(day,true);
            if(receipt!="native_meeting_muster_recorded")return;
            bool favor=KnightOathNativeRelation(patron,2,
                "cu_knight_native_muster_reward");
            DiagnosticJournal.Event("cu_knight_patron_conversation_muster",
                "result="+receipt+
                "|patron="+patron.StringId+
                "|native_conversation=true"+
                "|native_favor_action="+favor+
                "|completed="+knightOath.CompletedMusters+
                "|next_review_day="+knightOath.NextMusterDay);
            Notify("Your personal report to "+patron.Name+
                " was recorded. Next review day: "+
                knightOath.NextMusterDay+".");
        }
        private void ReleaseByPatronConversation()
        {
            var patron=KnightOathPatron();
            if(patron==null || !SpeakingWithSwornPatron())return;
            string result=knightOath.Release(true);
            if(result!="consensual_release")return;
            bool favor=KnightOathNativeRelation(patron,1,
                "cu_knight_oath_honorable_release_relation");
            DiagnosticJournal.Event("cu_knight_patron_conversation_release",
                "result="+result+
                "|patron="+patron.StringId+
                "|native_conversation=true"+
                "|native_favor_action="+favor+
                "|independent_clan_retained=true");
            Notify("Your lord has released you from the personal oath.");
        }
        private bool SpeakingWithSuccessor()
        {
            var successor=AvailableKnightSuccessor();
            return successor!=null &&
                Hero.OneToOneConversationHero==successor &&
                Hero.MainHero!=null &&
                Hero.MainHero.StringId==knightOath.PlayerId &&
                !successor.IsPrisoner;
        }
        private void RenewOathInConversation()
        {
            var successor=AvailableKnightSuccessor();
            int day=CurrentOathDay();
            if(successor==null || !SpeakingWithSuccessor())return;
            string receipt=knightOath.AcceptSuccessor(
                successor.StringId,successor.Clan.StringId,
                successor.Clan.Kingdom.StringId,day,true);
            if(receipt!="oath_renewed_to_native_successor")return;
            KnightOathNativeRelation(successor,2,
                "cu_knight_successor_oath_relation");
            DiagnosticJournal.Event("cu_knight_patron_conversation_successor",
                "result="+receipt+
                "|new_patron="+successor.StringId+
                "|actual_native_conversation=true");
        }
        private bool CanAcceptKnightEscort()
        {
            var patron=KnightOathPatron();
            return SpeakingWithSwornPatron() &&
                knightOath.LastMusterDay>=0 &&
                patron!=null &&
                patron.PartyBelongedTo!=null &&
                patron.PartyBelongedTo.LeaderHero==patron &&
                knightEscort.CanBegin(patron.StringId,CurrentOathDay());
        }
        private void AcceptKnightFieldEscort()
        {
            var patron=KnightOathPatron();
            if(patron==null || !CanAcceptKnightEscort())return;
            string receipt=knightEscort.Begin(patron.StringId,
                CurrentOathDay());
            DiagnosticJournal.Event("cu_knight_native_escort_accepted",
                "result="+receipt+
                "|patron="+patron.StringId+
                "|native_party="+patron.PartyBelongedTo.Name+
                "|escort_days="+SandboxKnightEscortState.RequiredDays+
                "|actual_player_party_not_reparented=true");
            if(receipt=="native_patron_field_escort_accepted")
                Notify("Field service: follow "+patron.Name+
                    "'s moving party for THREE consecutive in-game days. "+
                    "Stay close on the campaign map. No reward is given "+
                    "until the days are verified.");
        }
        private void ObserveKnightEscortDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !knightEscort.IsActive)return;
            var patron=KnightOathPatron();
            if(!knightOath.HasCurrentOath ||
                patron==null ||
                knightEscort.PatronId!=knightOath.PatronId)
            {
                knightEscort.Interrupt();
                DiagnosticJournal.Event(
                    "cu_knight_native_escort_interrupted",
                    "result=patron_changed_or_oath_ended");
                return;
            }
            var lordParty=patron.PartyBelongedTo;
            var playerParty=MobileParty.MainParty;
            bool parties=lordParty!=null && playerParty!=null &&
                lordParty.LeaderHero==patron &&
                playerParty.LeaderHero==Hero.MainHero &&
                !patron.IsPrisoner &&
                patron.CurrentSettlement==null &&
                Hero.MainHero!=null &&
                Hero.MainHero.CurrentSettlement==null;
            float distance=parties?
                lordParty.GetPosition2D.Distance(playerParty.GetPosition2D):
                99999f;
            bool near=parties && distance<=6f;
            int day=CurrentOathDay();
            string observed=knightEscort.Observe(day,near);
            if(observed=="native_field_escort_day_verified")
            {
                DiagnosticJournal.Event("cu_knight_native_escort_progress",
                    "result="+observed+
                    "|native_party_hero="+patron.StringId+
                    "|day="+day+
                    "|verified_days="+knightEscort.ConsecutiveDays+
                    "|native_party_distance="+distance);
                Notify("Sworn field escort: "+
                    knightEscort.ConsecutiveDays+" of "+
                    SandboxKnightEscortState.RequiredDays+
                    " consecutive campaign days verified.");
            }
            if(observed=="verified_native_party_escort_completed")
            {
                bool favor=KnightOathNativeRelation(patron,5,
                    "cu_knight_native_escort_reward");
                DiagnosticJournal.Event("cu_knight_native_escort_completed",
                    "result="+observed+
                    "|patron="+patron.StringId+
                    "|native_hero_party="+parties+
                    "|days="+SandboxKnightEscortState.RequiredDays+
                    "|native_favor_action="+favor+
                    "|native_party_distance="+distance+
                    "|independent_player_house=true");
                Notify("You completed real field service alongside "+
                    patron.Name+". Your standing with him "+
                    (favor?"has improved.":"needs verification.")+
                    " Another escort can be accepted after 30 days.");
            }
        }
        private void RegisterKnightPatronConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            // The opening audience is a native meeting with the saved,
            // real lord. Asking questions does not grant relation or mark
            // a muster. Only the spoken pledge receives native credit.
            starter.AddPlayerLine(
                "cu_knight_patron_first_audience","hero_main_options",
                "cu_knight_ceremony_open",
                "I have come to your banner, my lord, to pledge my sword.",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay<0,null,200);
            starter.AddDialogLine(
                "cu_knight_ceremony_open","cu_knight_ceremony_open",
                "cu_knight_ceremony_choices",
                "Your name is written in my household roll, but this "+
                "is our first meeting face to face. You still "+
                "command your own people. What do you promise me?",
                null,null,200);
            starter.AddPlayerLine(
                "cu_knight_ceremony_duties","cu_knight_ceremony_choices",
                "cu_knight_ceremony_duties_reply",
                "Tell me first what service you expect.",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay<0,null,110);
            starter.AddDialogLine(
                "cu_knight_ceremony_duties_reply",
                "cu_knight_ceremony_duties_reply",
                "cu_knight_ceremony_choices",
                "Keep faith with your word, present yourself when "+
                "summoned, and ride with my company when called. "+
                "Your companions remain under your command.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_ceremony_house","cu_knight_ceremony_choices",
                "cu_knight_ceremony_house_reply",
                "I wish to know the people of your house.",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay<0,null,110);
            starter.AddDialogLine(
                "cu_knight_ceremony_house_reply",
                "cu_knight_ceremony_house_reply",
                "cu_knight_ceremony_choices",
                "Learn their names in the clan ledger, and greet "+
                "them as people of this realm, not as your own kin. "+
                "Standing is earned through deeds.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_ceremony_pledge","cu_knight_ceremony_choices",
                "cu_knight_patron_first_audience_reply",
                "On my honor, I pledge my sword and faithful service.",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay<0 &&
                    KnightConversationMusterAvailable(),
                ()=>RecordNativePatronAudience(),180);
            starter.AddDialogLine(
                "cu_knight_patron_first_audience_reply",
                "cu_knight_patron_first_audience_reply",
                "hero_main_options",
                "I have heard your oath. Rise, and keep faith "+
                "with your household and the people who ride "+
                "beside you. We shall speak again at muster.",
                null,null,200);
            starter.AddPlayerLine(
                "cu_knight_ceremony_defer","cu_knight_ceremony_choices",
                "hero_main_options",
                "I must settle my affairs before I speak the pledge.",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay<0,null,80);
            starter.AddPlayerLine(
                "cu_knight_patrontalk_start",
                "hero_main_options","cu_knight_patron_answer",
                "My lord, I wish to speak of my sworn service.",
                ()=>SpeakingWithSwornPatron(),null,110);
            starter.AddDialogLine(
                "cu_knight_patron_answer","cu_knight_patron_answer",
                "cu_knight_patron_options",
                "You are sworn to my house. Speak plainly.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_patron_muster","cu_knight_patron_options",
                "cu_knight_patron_muster_reply",
                "I present myself for my service review.",
                ()=>KnightConversationMusterAvailable(),
                ()=>RecordNativePatronAudience(),100);
            starter.AddDialogLine(
                "cu_knight_patron_muster_reply",
                "cu_knight_patron_muster_reply","hero_main_options",
                "Your attendance is witnessed. Return at the appointed time.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_patron_expectations","cu_knight_patron_options",
                "cu_knight_patron_expectations_reply",
                "What is expected of my house?",
                ()=>SpeakingWithSwornPatron(),null,100);
            starter.AddDialogLine(
                "cu_knight_patron_expectations_reply",
                "cu_knight_patron_expectations_reply",
                "hero_main_options",
                "Keep your household ready and return for service review. "+
                "Your house is not yet a vassal clan of my kingdom.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_patron_escort","cu_knight_patron_options",
                "cu_knight_patron_escort_reply",
                "I will escort your party in the field.",
                ()=>CanAcceptKnightEscort(),
                ()=>AcceptKnightFieldEscort(),100);
            starter.AddDialogLine(
                "cu_knight_patron_escort_reply",
                "cu_knight_patron_escort_reply","hero_main_options",
                "Then ride near my banner for three days. Your "+
                "household remains under your own command.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_patron_release","cu_knight_patron_options",
                "cu_knight_patron_release_reply",
                "I ask honorable release from my personal oath.",
                ()=>SpeakingWithSwornPatron(),
                ()=>ReleaseByPatronConversation(),100);
            starter.AddDialogLine(
                "cu_knight_patron_release_reply",
                "cu_knight_patron_release_reply","hero_main_options",
                "You have my leave. Your house remains your own.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_patron_nevermind","cu_knight_patron_options",
                "hero_main_options","I have nothing further to report.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_successor_talk","hero_main_options",
                "cu_knight_successor_answer",
                "I seek to renew my oath to the head of your house.",
                ()=>SpeakingWithSuccessor(),null,110);
            starter.AddDialogLine(
                "cu_knight_successor_answer","cu_knight_successor_answer",
                "cu_knight_successor_options",
                "Will you pledge service to me, as you did my predecessor?",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_successor_renew","cu_knight_successor_options",
                "cu_knight_successor_reply","I renew my sworn service.",
                ()=>SpeakingWithSuccessor(),
                ()=>RenewOathInConversation(),100);
            starter.AddDialogLine(
                "cu_knight_successor_reply","cu_knight_successor_reply",
                "hero_main_options",
                "Your oath is accepted. The household remains yours.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_successor_decline","cu_knight_successor_options",
                "hero_main_options","Not today.",null,null,100);
        }
    }
}
