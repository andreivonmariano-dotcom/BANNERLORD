using System;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxBattleHistoryState battleHistory =
            new SandboxBattleHistoryState();

        private void ObserveCompletedBattle(MapEvent battle)
        {
            // No mission or economy patching. A campaign map event is the
            // authoritative outcome. SaS's public API establishes a soldier's
            // current host; a private SaS field or a guessed rank never does.
            if (!applied || !IsStandaloneSandbox() ||
                Campaign.Current == null || battle == null ||
                Hero.MainHero == null)
                return;
            try
            {
                var parties = battle.InvolvedParties;
                if (parties == null) return;

                var service = SandboxSaSBridge.Read();
                bool enrolledHost = service.Available &&
                    service.Enlisted && service.InsideHostParty &&
                    service.LordIsNativeLord &&
                    service.NativeHostPartyLedByLord &&
                    !String.IsNullOrEmpty(service.LordId) &&
                    service.HostParty != null &&
                    parties.Any(p => p != null &&
                        p.MobileParty == service.HostParty);
                bool playerParty = battle.IsPlayerMapEvent;
                if (!enrolledHost && !playerParty) return;

                BattleSideEnum side = battle.PlayerSide;
                string source = "PlayerParty";
                string lordId = "";
                if (enrolledHost)
                {
                    bool attacker = battle.PartiesOnSide(
                        BattleSideEnum.Attacker).Any(p => p != null &&
                            p.Party != null &&
                            p.Party.MobileParty == service.HostParty);
                    bool defender = battle.PartiesOnSide(
                        BattleSideEnum.Defender).Any(p => p != null &&
                            p.Party != null &&
                            p.Party.MobileParty == service.HostParty);
                    if (attacker == defender) return;
                    side = attacker ? BattleSideEnum.Attacker :
                        BattleSideEnum.Defender;
                    source = "ServeAsSoldier";
                    lordId = service.LordId ?? "";
                }
                else if (side != BattleSideEnum.Attacker &&
                         side != BattleSideEnum.Defender)
                    return;

                string kind = battle.IsSiegeAssault ? "Siege" :
                    battle.IsRaid ? "Raid" :
                    battle.IsHideoutBattle ? "Hideout" :
                    battle.IsFieldBattle ? "Field" : "Other";
                string outcome = battle.HasWinner ?
                    (battle.WinningSide == side ? "Victory" : "Defeat") :
                    "Unresolved";
                string settlement = battle.MapEventSettlement == null ?
                    "" : battle.MapEventSettlement.StringId ?? "";
                int day = (int)Math.Floor(CampaignTime.Now.ToDays);
                // Stable canonical key avoids duplicate receipts after
                // event callback reentry or campaign save/reload.
                string participants = String.Join(",",
                    parties.Where(p => p != null)
                    .Select(p => p.Id ?? "unknown")
                    .OrderBy(s => s, StringComparer.Ordinal));
                string canonical = battle.BattleStartTime.ToHours
                    .ToString("R", CultureInfo.InvariantCulture) +
                    "|" + kind + "|" + settlement + "|" +
                    participants + "|" + source + "|" + lordId;
                string key = SandboxBattleHistoryState.EventKey(canonical);
                if (!battleHistory.Record(key, day, kind, outcome,
                    source, settlement, lordId)) return;
                DiagnosticJournal.Event("cu_native_battle_witness",
                    "day=" + day + "|kind=" + kind +
                    "|result=" + outcome + "|source=" + source +
                    "|settlement=" + settlement + "|lord=" + lordId +
                    "|personal_kills_not_claimed=true" +
                    "|casualty_counts_not_claimed=true");
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_battle_history_observer_failed", error);
            }
        }

        private string BattleHistoryDescription()
        {
            return "Witnessed completed battles: " +
                battleHistory.Entries.Count +
                " (last " + SandboxBattleHistoryState.MaxEntries +
                " retained)\n" +
                battleHistory.DescribeRecent(5) +
                "\nThese are party-level outcomes, not proof of " +
                "personal kills or named casualties. Only the native " +
                "game and Serve as Soldier grant actual military status.";
        }
    }
}
