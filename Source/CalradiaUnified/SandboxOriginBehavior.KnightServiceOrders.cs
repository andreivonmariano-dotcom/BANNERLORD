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
                "|personal_standing="+knightServiceOrders.PersonalStanding()+
                "|no_native_kingdom_rank_or_troops=true");
            Notify("Your lord has witnessed this service. "+
                "Your household standing is now "+
                knightServiceOrders.PersonalStanding()+".");
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

            foreach(var source in new[]{"town"})
                starter.AddGameMenuOption(source,
                    "cu_knight_scout_native_town",
                    "Survey the town and roads for my sworn lord",
                    args=>KnightCanScoutCurrentTown(),
                    args=>SurveyKnightTown(),false);
        }
    }
}
