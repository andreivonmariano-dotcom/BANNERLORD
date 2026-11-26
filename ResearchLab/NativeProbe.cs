using System;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CUResearchLab
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            var campaignStarter = starter as CampaignGameStarter;
            if (campaignStarter != null) campaignStarter.AddBehavior(new NativeProbe());
        }
    }
    internal sealed class NativeProbe : CampaignBehaviorBase
    {
        private bool attempted;
        private static bool Write(string line)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BannerlordResearchLab");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "native-probe.log"), DateTime.UtcNow.ToString("o") + " " + line + Environment.NewLine);
                return true;
            }
            catch { return false; }
        }
        private static string Id(Hero h) { return h == null ? "none" : h.StringId; }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSession);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnLoad);
            CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernor);
        }
        public override void SyncData(IDataStore dataStore) { }
        private void OnSession(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "cu_lab_treasury_town", "[LAB] Transfer 100 denars to holding treasury", Condition, Confirm, false);
            starter.AddGameMenuOption("castle", "cu_lab_treasury_castle", "[LAB] Transfer 100 denars to holding treasury", Condition, Confirm, false);
            Snapshot("session_launched");
        }
        private void OnLoad() { Snapshot("game_load_finished"); }
        private static void Snapshot(string phase)
        {
            try
            {
                if (Campaign.Current == null || Hero.MainHero == null || Clan.PlayerClan == null) return;
                var player = Hero.MainHero;
                Write("snapshot " + phase + " actor=" + Id(player) + " purse=" + player.Gold);
                int count = 0;
                foreach (var town in Clan.PlayerClan.Fiefs)
                {
                    if (count++ >= 24) break;
                    if (town == null || town.Settlement == null || town.OwnerClan != Clan.PlayerClan) continue;
                    Write("holding=" + town.Settlement.StringId + " treasury=" + town.Gold + " governor=" + Id(town.Governor) +
                        " reciprocal=" + (town.Governor == null || town.Governor.GovernorOf == town));
                }
            }
            catch (Exception e) { Write("snapshot_error " + e.GetType().Name); }
        }
        private void OnGovernor(Town town, Hero oldGovernor, Hero newGovernor)
        {
            try
            {
                if (town == null || town.Settlement == null || Clan.PlayerClan == null || town.OwnerClan != Clan.PlayerClan) return;
                Write("governor_event holding=" + town.Settlement.StringId + " old=" + Id(oldGovernor) + " new=" + Id(newGovernor) +
                    " town_matches=" + (town.Governor == newGovernor) + " new_reciprocal=" + (newGovernor == null || newGovernor.GovernorOf == town) +
                    " old_released=" + (oldGovernor == null || oldGovernor == newGovernor || oldGovernor.GovernorOf != town));
            }
            catch (Exception e) { Write("governor_probe_error " + e.GetType().Name); }
        }
        private bool Eligible(Hero actor, Settlement target)
        {
            return Campaign.Current != null && actor != null && actor == Hero.MainHero &&
                actor.IsAlive && actor.IsActive && !actor.IsPrisoner && target != null &&
                target == Settlement.CurrentSettlement && actor.CurrentSettlement == target &&
                (target.IsTown || target.IsCastle) && !target.IsUnderSiege &&
                Clan.PlayerClan != null && target.OwnerClan == Clan.PlayerClan && actor.Clan == Clan.PlayerClan &&
                target.Town != null && TransferPolicy.CanTransfer(actor.Gold, target.Town.Gold, true, attempted);
        }
        private bool Condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return Campaign.Current != null && Eligible(Hero.MainHero, Settlement.CurrentSettlement);
        }
        private void Confirm(MenuCallbackArgs args)
        {
            var actor = Hero.MainHero;
            var target = Settlement.CurrentSettlement;
            if (!Eligible(actor, target)) return;
            InformationManager.ShowInquiry(new InquiryData("Research test only",
                "Transfer exactly 100 denars to this holding's native treasury? Use only a disposable control campaign. No food or recruitment benefit is promised.",
                true, true, "Transfer 100", "Cancel", () => Execute(actor, target), () => Write("transfer_cancelled")), true);
        }
        private void Execute(Hero actor, Settlement target)
        {
            if (!Eligible(actor, target)) { Write("transfer_refused_precondition"); return; }
            int purse = actor.Gold, treasury = target.Town.Gold;
            if (!Write("transfer_begin actor=" + Id(actor) + " target=" + target.StringId + " purse=" + purse + " treasury=" + treasury)) return;
            attempted = true;
            try
            {
                GiveGoldAction.ApplyForCharacterToSettlement(actor, target, TransferPolicy.Amount, false);
                int p = actor.Gold, t = target.Town.Gold;
                bool ok = TransferPolicy.Confirmed(purse, treasury, p, t);
                Write("transfer_result confirmed=" + ok + " purse_after=" + p + " treasury_after=" + t);
                InformationManager.DisplayMessage(new InformationMessage(ok ?
                    "LAB: native transfer verified. See research log." : "LAB: unexpected balance change. No further attempts this session."));
            }
            catch (Exception e)
            {
                Write("transfer_exception " + e.GetType().Name);
                InformationManager.DisplayMessage(new InformationMessage("LAB: transfer threw; inspect actual balances. No further attempts this session."));
            }
        }
    }
}
