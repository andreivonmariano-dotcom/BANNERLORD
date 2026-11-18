using System;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Independent read-only daily campaign telemetry for real-world comparisons
    /// against the budget simulator. No native taxes or save-state claims.
    /// </summary>
    public sealed class DiagnosticBehavior : CampaignBehaviorBase
    {
        private readonly Settings settings;
        private int sessionDay;
        private int? previousGold;

        public DiagnosticBehavior(Settings settings) { this.settings = settings; }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDay);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnLaunch);
        }

        public override void SyncData(IDataStore store)
        {
            // Logging session counters are intentionally not save-game data.
            // This event proves SyncData was called, not a successful save/reload.
            if (settings.EnableDiagnostics)
                DiagnosticJournal.Event("sync_data_invoked", "session_day=" + sessionDay);
        }

        private void OnLaunch(CampaignGameStarter starter)
        {
            if (settings.EnableDiagnostics)
                DiagnosticJournal.Event("campaign_session_launched", "version=0.18.0");
            if (starter == null) return;
            foreach (var menu in new[] { "town", "castle" })
                starter.AddGameMenuOption(menu, "cu_export_diagnostics",
                    "CU: Export campaign economy and crash diagnostics",
                    args => settings.EnableDiagnostics,
                    args => ExportFromMenu(), false);
        }

        private static string N(int n)
        {
            return n.ToString(CultureInfo.InvariantCulture);
        }

        private void OnDay()
        {
            if (!settings.EnableDiagnostics) return;
            try { CaptureDay(); }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("daily_finance_capture", ex);
            }
        }

        private void CaptureDay()
        {
            var hero = Hero.MainHero;
            var clan = Clan.PlayerClan;
            if (hero == null || clan == null) return;
            if (sessionDay < Int32.MaxValue) sessionDay++;
            int playerGold = Math.Max(0, hero.Gold);
            string goldDelta = previousGold.HasValue
                ? N(playerGold - previousGold.Value) : "";
            previousGold = playerGold;

            long clanLordWages = 0;
            foreach (var party in MobileParty.AllLordParties)
            {
                if (party == null || party.ActualClan != clan) continue;
                clanLordWages += Math.Max(0, party.TotalWage);
            }
            int fieldWages = (int)Math.Min(Int32.MaxValue, clanLordWages);
            int playerTroops = MobileParty.MainParty == null ||
                MobileParty.MainParty.MemberRoster == null ? 0 :
                MobileParty.MainParty.MemberRoster.TotalManCount;

            int castles = Settlement.All.Count(s => s != null &&
                s.OwnerClan == clan && s.IsCastle);
            int share = castles == 0 ? 0 :
                (fieldWages + castles - 1) / castles;
            int owned = 0;
            foreach (var castle in Settlement.All)
            {
                if (castle == null || castle.OwnerClan != clan ||
                    !castle.IsCastle || castle.Town == null) continue;
                owned++;
                int healthy = 0, raided = 0;
                if (castle.BoundVillages != null)
                    foreach (var village in castle.BoundVillages)
                    {
                        if (village == null) continue;
                        if (village.VillageState == Village.VillageStates.Normal)
                            healthy += Math.Max(0, (int)village.Hearth);
                        else raided++;
                    }
                int prosperity = Math.Max(0, (int)castle.Town.Prosperity);
                var garrison = castle.Town.GarrisonParty;
                int garrisonWages = garrison == null ? 0 :
                    Math.Max(0, garrison.TotalWage);
                int garrisonTroops = garrison == null ||
                    garrison.MemberRoster == null ? 0 :
                    garrison.MemberRoster.TotalManCount;
                int estimate = FeudalBudgetPolicy.EstimateEstateIncome(
                    prosperity, healthy);
                int suggested = FeudalBudgetPolicy.WageBudget(estimate,
                    share, settings.EstateOtherCostsReserve,
                    settings.EstateTargetDailySurplus,
                    settings.EstateMaxDailyGarrisonWage);
                int modelNet = FeudalBudgetPolicy.ProjectedBalance(estimate,
                    share, garrisonWages, settings.EstateOtherCostsReserve, 0);
                DiagnosticJournal.Finance("castle", N(sessionDay),
                    castle.StringId ?? "", N(prosperity), N(healthy), N(raided),
                    N(garrisonTroops), N(garrisonWages), N(fieldWages),
                    N(playerTroops), N(playerGold), goldDelta, N(estimate),
                    N(suggested), N(modelNet), "", "", "",
                    "garrison_limit=" + castle.GarrisonWagePaymentLimit +
                    "|modeled_required_income=" +
                    CastleManagementPolicy.RequiredIncome(share, garrisonWages,
                        settings.EstateOtherCostsReserve,
                        settings.EstateTargetDailySurplus) +
                    "|modeled_shortfall=" + CastleManagementPolicy.ModeledShortfall(
                        estimate, CastleManagementPolicy.RequiredIncome(share,
                        garrisonWages, settings.EstateOtherCostsReserve,
                        settings.EstateTargetDailySurplus)) +
                    "|forecast_not_native_tax");
            }
            // Summary row is logged even without owned castles.
            DiagnosticJournal.Finance("clan_summary", N(sessionDay), "",
                "", "", "", "", "", N(fieldWages), N(playerTroops),
                N(playerGold), goldDelta, "", "", "", "", "", "",
                "owned_castles=" + owned +
                "|gold_delta_includes_all_transactions_not_tax");
        }

        private static void ExportFromMenu()
        {
            try
            {
                var dir = DiagnosticJournal.ExportSnapshot();
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Calradia Unified] Diagnostics exported to " + dir +
                    ". The Windows exporter adds engine crash logs."));
                DiagnosticJournal.Event("manual_export", "snapshot_created");
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("manual_export", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Calradia Unified] Diagnostic export failed. Check mod log."));
            }
        }
    }
}
