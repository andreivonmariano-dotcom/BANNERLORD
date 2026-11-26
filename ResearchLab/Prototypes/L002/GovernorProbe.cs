using System;
using System.Globalization;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

[assembly:System.Reflection.AssemblyVersion("0.0.2.0")]
namespace CUGovernorResearch
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            var campaign = starter as CampaignGameStarter;
            if (campaign != null) campaign.AddBehavior(new GovernorProbe());
        }
    }

    internal sealed class GovernorProbe : CampaignBehaviorBase
    {
        private readonly string session = Guid.NewGuid().ToString("N");
        private const int MaxRecords = 1200;
        private int records;
        private bool stopped;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSession);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnLoaded);
            CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernor);
            CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuilding);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDaily);
        }
        public override void SyncData(IDataStore dataStore) { }
        private static string Id(Hero h) { return h == null ? "none" : h.StringId; }
        private static string F(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private bool Active { get { return !stopped && records < MaxRecords && Campaign.Current != null && Clan.PlayerClan != null; } }
        private void Write(string row)
        {
            if (stopped || records >= MaxRecords) return;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BannerlordResearchLab");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "governor-L002-" + session + ".log"),
                    DateTime.UtcNow.ToString("o") + " " + row + Environment.NewLine);
                records++;
            }
            catch { stopped = true; }
        }
        private void Observe(Town town, string reason)
        {
            if (!Active || town == null || town.Settlement == null || town.OwnerClan != Clan.PlayerClan) return;
            try
            {
                var s = town.Settlement;
                var g = town.Governor;
                var building = town.CurrentBuilding;
                Write("snapshot reason=" + reason + " holding=" + s.StringId +
                    " governor=" + Id(g) + " present=" + (g != null && g.CurrentSettlement == s) +
                    " reciprocal=" + (g == null || g.GovernorOf == town) +
                    " siege=" + s.IsUnderSiege + " market_gold=" + town.Gold +
                    " construction_reserve=" + town.BoostBuildingProcess +
                    " prosperity=" + F(town.Prosperity) + " loyalty=" + F(town.Loyalty) +
                    " security=" + F(town.Security) + " food=" + F(town.FoodStocks) +
                    " daily_loyalty=" + F(town.LoyaltyChange) + " daily_security=" + F(town.SecurityChange) +
                    " daily_food=" + F(town.FoodChange) + " daily_construction=" + F(town.Construction) +
                    " building=" + (building == null || building.BuildingType == null ? "none" : building.BuildingType.StringId) +
                    " building_level=" + (building == null ? -1 : building.CurrentLevel) +
                    " building_progress=" + (building == null ? "none" : F(building.BuildingProgress)) +
                    " player_gold=" + (Hero.MainHero == null ? -1 : Hero.MainHero.Gold));
            }
            catch (Exception ex) { Write("observation_error type=" + ex.GetType().Name); }
        }
        private void Snapshot(string reason)
        {
            if (!Active) return;
            try
            {
                int i = 0;
                foreach (var t in Clan.PlayerClan.Fiefs)
                {
                    if (i++ >= 24 || !Active) break;
                    Observe(t, reason);
                }
            }
            catch (Exception ex) { Write("snapshot_error type=" + ex.GetType().Name); }
        }
        private void OnSession(CampaignGameStarter starter) { Snapshot("session"); }
        private void OnLoaded() { Snapshot("load_finished"); }
        private void OnDaily() { Snapshot("daily_event"); }
        private void OnGovernor(Town t, Hero oldGovernor, Hero newGovernor)
        {
            if (!Active || t == null || t.Settlement == null || t.OwnerClan != Clan.PlayerClan) return;
            Write("governor_event holding=" + t.Settlement.StringId + " old=" + Id(oldGovernor) + " new=" + Id(newGovernor));
            Observe(t, "governor_event");
        }
        private void OnBuilding(Town t, Building b, int eventValue)
        {
            if (!Active || t == null || t.Settlement == null || t.OwnerClan != Clan.PlayerClan) return;
            Write("building_event holding=" + t.Settlement.StringId + " event_value=" + eventValue +
                " actual_level=" + (b == null ? -1 : b.CurrentLevel));
            Observe(t, "building_event");
        }
    }
}
