using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxKnightServiceOrderState knightServiceOrders=
            new SandboxKnightServiceOrderState();
        private SandboxKnightBattleRecognitionState knightBattleRecognition=
            new SandboxKnightBattleRecognitionState();

        private int WitnessedKnightVictories()
        {
            return knightOath.HasCurrentOath ?
                battleHistory.WitnessedVictoriesWith(
                    knightOath.PatronId) : 0;
        }
        private string KnightBattleServiceSummary()
        {
            int victories=WitnessedKnightVictories();
            return "Confirmed victories on the same side as your sworn lord: "+
                victories+" | acknowledged stages: "+
                knightBattleRecognition.Stage+"/2"+
                "\\nBattle receipts prove shared participation only. "+
                "Native relations change solely after reporting in person.";
        }
        private bool CanReportKnightBattleService()
        {
            var patron=KnightOathPatron();
            return applied && knightOath.HasCurrentOath &&
                patron!=null && patron.IsAlive && patron.IsActive &&
                !patron.IsPrisoner && SpeakingWithSwornPatron() &&
                knightBattleRecognition.CanAcknowledge(
                    patron.StringId,WitnessedKnightVictories(),
                    true,CurrentOathDay());
        }
        private void ReportKnightBattleService()
        {
            if(!CanReportKnightBattleService())return;
            var patron=KnightOathPatron();
            int victories=WitnessedKnightVictories();
            int day=CurrentOathDay();
            // Mark once before attempting the native action. A failed
            // transaction can never be replayed as free relationship XP.
            if(!knightBattleRecognition.MarkAcknowledged(
                patron.StringId,victories,true,day))return;
            bool nativeFavor=KnightOathNativeRelation(patron,1,
                "cu_knight_joint_battle_acknowledgement");
            DiagnosticJournal.Event("cu_knight_battle_acknowledgement",
                "patron="+patron.StringId+
                "|joint_native_victories="+victories+
                "|stage="+knightBattleRecognition.Stage+
                "|relation_effect_verified="+nativeFavor+
                "|rank_title_and_renown_unchanged=true");
            Notify(nativeFavor?
                "Your lord acknowledges your shared battles. "+
                "Your native personal relation increased by one.":
                "Your shared battle report was heard, but no native "+
                "relationship change was confirmed.");
        }

        private string KnightVerifiedStanding()
        {
            var patron=KnightOathPatron();
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(patron==null || player==null || clan==null)
                return "Unverified sworn service";
            return SandboxKnightStandingPolicy.Standing(
                player.GetRelation(patron),clan.Renown,
                knightServiceOrders.CompletedOrders,
                knightEscort.CompletedEscorts);
        }
        private bool KnightCanAskRecommendation()
        {
            var player=Hero.MainHero;
            var patron=KnightOathPatron();
            var clan=Clan.PlayerClan;
            return SpeakingWithSwornPatron() &&
                knightOath.LastMusterDay>=0 &&
                player!=null && patron!=null && clan!=null &&
                patron.Clan!=null &&
                knightServiceOrders.CanPresentRecommendation(
                    patron.StringId,CurrentOathDay()) &&
                SandboxKnightStandingPolicy.CanRequestRoyalHearing(
                    player.GetRelation(patron),clan.Renown,
                    knightServiceOrders.CompletedOrders,
                    knightEscort.CompletedEscorts,
                    patron.Clan.Kingdom!=null,
                    clan.Kingdom==null);
        }
        private void RequestKnightRecommendation()
        {
            if(!KnightCanAskRecommendation())return;
            var patron=KnightOathPatron();
            if(!knightServiceOrders.RecordRecommendation(
                patron.StringId,CurrentOathDay()))return;
            bool favored=KnightOathNativeRelation(patron,1,
                "cu_knight_service_recommendation_favor");
            DiagnosticJournal.Event("cu_knight_recommendation",
                "patron="+patron.StringId+
                "|native_relations_verified="+favored+
                "|actual_kingdom_membership_unchanged=true"+
                "|king_audience_not_yet_held=true");
        }
        private bool KnightCanSeekRoyalAudience()
        {
            var patron=KnightOathPatron();
            var clan=Clan.PlayerClan;
            var lord=Hero.OneToOneConversationHero;
            if(!applied || !knightOath.HasCurrentOath ||
                patron==null || patron.Clan==null ||
                patron.Clan.Kingdom==null || clan==null ||
                clan.Kingdom!=null ||
                knightOath.PatronId!=knightServiceOrders.PatronId ||
                !knightServiceOrders.CanHearKing(CurrentOathDay()))
                return false;
            var rulingClan=patron.Clan.Kingdom.RulingClan;
            return rulingClan!=null && lord!=null &&
                lord==rulingClan.Leader && lord.IsAlive &&
                lord.IsActive && !lord.IsPrisoner &&
                Hero.MainHero!=null;
        }
        private void CompleteKnightRoyalAudience()
        {
            if(!KnightCanSeekRoyalAudience())return;
            var king=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(!knightServiceOrders.RecordRoyalHearing(
                CurrentOathDay()))return;
            int before=player.GetRelation(king);
            try
            {
                TaleWorlds.CampaignSystem.Actions.ChangeRelationAction
                    .ApplyRelationChangeBetweenHeroes(
                        player,king,1,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_knight_royal_audience_relation_failed",error);
            }
            int after=player.GetRelation(king);
            DiagnosticJournal.Event("cu_knight_royal_audience",
                "ruler="+king.StringId+
                "|relation_before="+before+
                "|relation_after="+after+
                "|native_favor_observed="+
                SandboxNativeEffectProof.RelationDirectionObserved(
                    before,after,1)+
                "|kingdom_membership_unchanged=true"+
                "|no_fief_or_royal_promotion_granted=true");
        }

        private bool KnightCanDiscussNativeVassalage()
        {
            var clan=Clan.PlayerClan;
            var patron=ResolveCampaignHero(
                knightServiceOrders.PatronId);
            var speaker=Hero.OneToOneConversationHero;
            if(!applied || clan==null || clan.Kingdom!=null ||
                patron==null || patron.Clan==null ||
                patron.Clan.Kingdom==null ||
                knightServiceOrders.RoyalHearingDay<0 ||
                speaker==null || !speaker.IsAlive ||
                !speaker.IsActive || speaker.IsPrisoner)
                return false;
            var ruler=patron.Clan.Kingdom.RulingClan;
            return ruler!=null && ruler.Leader==speaker;
        }

        private bool KnightCanRequestOrder()
        {
            var patron=KnightOathPatron();
            return SpeakingWithSwornPatron() &&
                knightOath.LastMusterDay>=0 &&
                patron!=null && knightServiceOrders.CanBegin(
                    patron.StringId,CurrentOathDay());
        }
        private void AcceptKnightPatronOrder(string task)
        {
            if(!KnightCanRequestOrder())return;
            var patron=KnightOathPatron();
            string receipt=knightServiceOrders.Begin(patron.StringId,
                task,CurrentOathDay());
            DiagnosticJournal.Event("cu_knight_service_order_accepted",
                "result="+receipt+"|type="+task+
                "|patron="+patron.StringId+
                "|native_conversation=true|independent_clan=true");
            if(receipt=="native_patron_order_accepted")
                Notify(task=="Scout"?
                    "Your lord asks you to survey two distinct "+
                    "towns in his kingdom within fourteen days. "+
                    "Return and report to him in person.":
                    "Your lord asks you to command at least "+
                    "eight real party members and report "+
                    "after three days, before day fourteen.");
        }
        private bool KnightServiceReportAvailable()
        {
            var patron=KnightOathPatron();
            var party=MobileParty.MainParty;
            return SpeakingWithSwornPatron() &&
                patron!=null && party!=null &&
                knightServiceOrders.CanReport(patron.StringId,
                    CurrentOathDay(),
                    party.MemberRoster!=null &&
                    party.MemberRoster.TotalManCount>=8);
        }
        private void ReportKnightServiceOrder()
        {
            if(!KnightServiceReportAvailable())return;
            var patron=KnightOathPatron();
            bool troopProof=MobileParty.MainParty.MemberRoster
                .TotalManCount>=8;
            string kind=knightServiceOrders.Order;
            string receipt=knightServiceOrders.Report(
                patron.StringId,CurrentOathDay(),troopProof);
            if(receipt!="native_patron_service_report_completed")return;
            // The real lord hears the report, then the game handles
            // whatever bounded native favor it actually applies.
            bool favor=KnightOathNativeRelation(patron,2,
                "cu_knight_native_service_order_favor");
            DiagnosticJournal.Event("cu_knight_service_order_report",
                "result="+receipt+
                "|type="+kind+
                "|patron="+patron.StringId+
                "|total_completed="+knightServiceOrders.CompletedOrders+
                "|native_favor_verified="+favor+
                "|personal_standing="+KnightVerifiedStanding()+
                "|no_native_kingdom_rank_or_troops=true");
            Notify("Your lord has witnessed this service. "+
                "Your household standing is now "+
                KnightVerifiedStanding()+".");
        }
        private bool KnightCanScoutCurrentTown()
        {
            if(!applied || !knightServiceOrders.Active ||
                knightServiceOrders.Order!="Scout" ||
                !knightOath.HasCurrentOath ||
                knightOath.PatronId!=knightServiceOrders.PatronId)
                return false;
            var town=Settlement.CurrentSettlement;
            var patron=KnightOathPatron();
            return town!=null && town.IsTown &&
                town.OwnerClan!=null && patron!=null &&
                patron.Clan!=null &&
                town.OwnerClan.Kingdom!=null &&
                town.OwnerClan.Kingdom==patron.Clan.Kingdom &&
                Hero.MainHero!=null &&
                Hero.MainHero.CurrentSettlement==town;
        }
        private void SurveyKnightTown()
        {
            if(!KnightCanScoutCurrentTown())return;
            var town=Settlement.CurrentSettlement;
            string result=knightServiceOrders.Observe(
                CurrentOathDay(),true,town.StringId,true);
            DiagnosticJournal.Event("cu_knight_native_town_scout",
                "town="+town.StringId+
                "|result="+result+
                "|patron="+knightServiceOrders.PatronId+
                "|same_native_kingdom=true");
            Notify(result=="native_first_town_scouted"?
                "You have observed your first town. "+
                "Continue to another town in your lord's realm.":
                result=="native_second_town_scouted"?
                "Your second town is witnessed. Return "+
                "to your sworn lord to give the report.":
                "This town adds no new service evidence today.");
        }
        private void ObserveKnightServiceOrderDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !knightServiceOrders.Active)return;
            if(!knightOath.HasCurrentOath ||
                knightOath.PatronId!=knightServiceOrders.PatronId)
            {
                // Never convert another lord's duties into this
                // lord's credit after oath release or succession.
                knightServiceOrders.Expire(
                    knightServiceOrders.DueDay+1);
                DiagnosticJournal.Event("cu_knight_order_interrupted",
                    "reason=patron_changed_or_oath_released");
                return;
            }
            string outcome=knightServiceOrders.Expire(
                CurrentOathDay());
            if(outcome=="native_order_expired")
            {
                DiagnosticJournal.Event("cu_knight_service_order_expired",
                    "patron="+knightServiceOrders.PatronId+
                    "|order="+knightServiceOrders.Order+
                    "|no_unearned_reward=true");
                Notify("Your sworn service order has passed "+
                    "its deadline. Speak with your lord for new duties.");
            }
        }
        private void RegisterKnightServiceOrders(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_knight_request_service_order",
                "cu_knight_patron_options",
                "cu_knight_order_choices",
                "Have you a duty for my household?",
                ()=>KnightCanRequestOrder(),null,112);
            starter.AddDialogLine(
                "cu_knight_order_choices",
                "cu_knight_order_choices",
                "cu_knight_order_select",
                "A sworn sword must learn both the roads "+
                "of this realm and the strength of a household.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_order_scout",
                "cu_knight_order_select",
                "cu_knight_order_scout_reply",
                "I shall survey two towns and return.",
                ()=>KnightCanRequestOrder(),
                ()=>AcceptKnightPatronOrder("Scout"),100);
            starter.AddDialogLine(
                "cu_knight_order_scout_reply",
                "cu_knight_order_scout_reply",
                "hero_main_options",
                "Report what you actually find. I have "+
                "no need of tales you cannot witness.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_order_muster",
                "cu_knight_order_select",
                "cu_knight_order_muster_reply",
                "I shall ready eight sworn fighters.",
                ()=>KnightCanRequestOrder(),
                ()=>AcceptKnightPatronOrder("Muster"),100);
            starter.AddDialogLine(
                "cu_knight_order_muster_reply",
                "cu_knight_order_muster_reply",
                "hero_main_options",
                "Keep your company under your own "+
                "banner. Return in three days and "+
                "let me inspect your strength.",
                null,null,100);
            starter.AddPlayerLine(
                "cu_knight_order_defer",
                "cu_knight_order_select",
                "hero_main_options",
                "I must first settle my household affairs.",
                null,null,75);
            starter.AddPlayerLine(
                "cu_knight_order_report",
                "cu_knight_patron_options",
                "cu_knight_order_report_reply",
                "My lord, I return with my service report.",
                ()=>KnightServiceReportAvailable(),
                ()=>ReportKnightServiceOrder(),120);
            starter.AddDialogLine(
                "cu_knight_order_report_reply",
                "cu_knight_order_report_reply",
                "hero_main_options",
                "Your report is witnessed. You have "+
                "earned my greater trust. Keep your "+
                "household fit for the next duty.",
                null,null,100);

            starter.AddPlayerLine(
                "cu_knight_service_review",
                "cu_knight_patron_options",
                "cu_knight_service_review_reply",
                "How stands my household's service in your eyes?",
                ()=>SpeakingWithSwornPatron() &&
                    knightOath.LastMusterDay>=0,null,100);
            starter.AddDialogLine(
                "cu_knight_review_new",
                "cu_knight_service_review_reply",
                "hero_main_options",
                "You have sworn your sword. Keep faith and "+
                "show what your household can do.",
                ()=>KnightVerifiedStanding()=="Newly sworn knight",
                null,100);
            starter.AddDialogLine(
                "cu_knight_review_proven",
                "cu_knight_service_review_reply",
                "hero_main_options",
                "Your proven work has reached my notice. "+
                "I expect your banner to answer when called.",
                ()=>KnightVerifiedStanding()=="Proven sworn sword",
                null,100);
            starter.AddDialogLine(
                "cu_knight_review_trusted",
                "cu_knight_service_review_reply",
                "hero_main_options",
                "Your service, conduct and reputation "+
                "have earned my trust. Continue to serve well.",
                ()=>KnightVerifiedStanding()=="Trusted household knight",
                null,100);
            starter.AddDialogLine(
                "cu_knight_review_veteran",
                "cu_knight_service_review_reply",
                "hero_main_options",
                "Few have a longer record of dependable "+
                "service to this house. Your honor is your own.",
                ()=>KnightVerifiedStanding()=="Veteran retainer",
                null,100);
            starter.AddPlayerLine(
                "cu_knight_joint_battle_report",
                "cu_knight_patron_options",
                "cu_knight_joint_battle_report_reply",
                "My lord, I fought in your company. Hear my account?",
                ()=>CanReportKnightBattleService(),
                ()=>ReportKnightBattleService(),114);
            starter.AddDialogLine(
                "cu_knight_joint_battle_report_reply",
                "cu_knight_joint_battle_report_reply",
                "hero_main_options",
                "Your presence in those battles has been witnessed. "+
                "Keep your house ready for further service.",
                null,null,110);
            starter.AddPlayerLine(
                "cu_knight_request_recommendation",
                "cu_knight_patron_options",
                "cu_knight_recommendation_reply",
                "Would you acknowledge my service before the crown?",
                ()=>KnightCanAskRecommendation(),
                ()=>RequestKnightRecommendation(),115);
            starter.AddDialogLine(
                "cu_knight_recommendation_reply",
                "cu_knight_recommendation_reply",
                "hero_main_options",
                "Your household has earned a hearing. "+
                "Seek our ruler in person. I will acknowledge "+
                "the service you have completed under my banner.",
                null,null,110);
            starter.AddPlayerLine(
                "cu_knight_royal_hearing",
                "hero_main_options",
                "cu_knight_royal_hearing_reply",
                "I seek an audience on the service of my household.",
                ()=>KnightCanSeekRoyalAudience(),
                ()=>CompleteKnightRoyalAudience(),110);
            starter.AddDialogLine(
                "cu_knight_royal_hearing_reply",
                "cu_knight_royal_hearing_reply",
                "hero_main_options",
                "Your service under your sworn lord has been heard. "+
                "Any formal entry into this kingdom must follow "+
                "the law of the crown and its existing clans.",
                null,null,110);

            starter.AddPlayerLine(
                "cu_knight_lawful_vassalage_inquiry",
                "hero_main_options",
                "cu_knight_lawful_vassalage_reply",
                "I would petition for formal entry into your kingdom.",
                ()=>KnightCanDiscussNativeVassalage(),null,80);
            starter.AddDialogLine(
                "cu_knight_lawful_vassalage_reply",
                "cu_knight_lawful_vassalage_reply",
                "hero_main_options",
                "Your personal service is acknowledged, but "+
                "the crown's ordinary terms for joining the realm "+
                "remain in force. A hearing grants no land or title.",
                null,null,80);

            foreach(var source in new[]{"town"})
                starter.AddGameMenuOption(source,
                    "cu_knight_scout_native_town",
                    "Survey the town and roads for my sworn lord",
                    args=>KnightCanScoutCurrentTown(),
                    args=>SurveyKnightTown(),false);
        }
    }
}
