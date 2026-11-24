using System;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxBattleHistoryState battleHistory =
            new SandboxBattleHistoryState();

        // Legacy acknowledgement history, not a second reward engine.
        private SandboxKnightBattleRecognitionState knightBattleRecognition =
            new SandboxKnightBattleRecognitionState();

        private static string NativeOwnClanHeroIds(TroopRoster roster)
        {
            var clan = Clan.PlayerClan;
            if (roster == null || clan == null) return "";
            // A death can clear PartyBelongedTo. The own-clan identity
            // comes from the Hero object, not a post-battle party slot.
            return String.Join(",", roster.GetTroopRoster()
                .Where(e => e.Number > 0 && e.Character != null &&
                    e.Character.IsHero && e.Character.HeroObject != null)
                .Select(e => e.Character.HeroObject)
                .Where(h => h.StringId != null &&
                    (h == Hero.MainHero || h.Clan == clan ||
                     h.CompanionOf == clan))
                .Select(h => h.StringId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .Take(4));
        }

        private void ObserveCompletedBattle(MapEvent battle)
        {
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
                // Unknown or incomplete external service is not an independent battle.
                if(!enrolledHost && (service.Enlisted || (!service.Available &&
                    (serviceCareer.VerifiedExternalEnlistment ||
                     serviceCareer.Status=="ExternalStatusUnverified"))))return;
                bool playerParty = battle.IsPlayerMapEvent;
                if (!enrolledHost && !playerParty) return;

                BattleSideEnum side = battle.PlayerSide;
                string source = "PlayerParty", lordId = "";
                MapEventParty receiptParty = null;

                if (enrolledHost)
                {
                    receiptParty = battle.PartiesOnSide(
                        BattleSideEnum.Attacker).FirstOrDefault(p =>
                            p != null && p.Party != null &&
                            p.Party.MobileParty == service.HostParty);
                    if (receiptParty != null)
                        side = BattleSideEnum.Attacker;
                    else
                    {
                        receiptParty = battle.PartiesOnSide(
                            BattleSideEnum.Defender).FirstOrDefault(p =>
                                p != null && p.Party != null &&
                                p.Party.MobileParty == service.HostParty);
                        if (receiptParty == null) return;
                        side = BattleSideEnum.Defender;
                    }
                    source = "ServeAsSoldier";
                    lordId = service.LordId;
                }
                else
                {
                    if (side != BattleSideEnum.Attacker &&
                        side != BattleSideEnum.Defender) return;
                    receiptParty = battle.PartiesOnSide(side)
                        .FirstOrDefault(p => p != null &&
                            p.Party == PartyBase.MainParty);
                    // A campaign "player event" need not mean our
                    // controlled party actually suffered casualties.
                    if (receiptParty == null) return;
                }

                string patronWitnessId = "";
                if (source == "PlayerParty" &&
                    knightOath.HasCurrentOath &&
                    knightOath.PlayerId==Hero.MainHero.StringId)
                {
                    var patron = KnightOathPatron();
                    if (patron != null && patron.IsAlive &&
                        patron.IsActive && !patron.IsPrisoner &&
                        battle.PartiesOnSide(side).Any(p =>
                            p != null && p.Party != null &&
                            p.Party != PartyBase.MainParty &&
                            p.Party.MobileParty != null &&
                            p.Party.MobileParty.LeaderHero == patron))
                        patronWitnessId = patron.StringId;
                }

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

                // Native MapEventParty rosters count deaths and wounds in
                // this single party, not the entire army or personal kills.
                // Refuse estimated numbers when the rosters are absent.
                int fallen = receiptParty.DiedInBattle == null ? -1 :
                    receiptParty.DiedInBattle.TotalManCount;
                int wounded = receiptParty.WoundedInBattle == null ? -1 :
                    receiptParty.WoundedInBattle.TotalManCount;
                if ((fallen < 0) != (wounded < 0))
                {
                    fallen = -1;
                    wounded = -1;
                }
                string woundedHeroes = fallen < 0 ? "" :
                    NativeOwnClanHeroIds(receiptParty.WoundedInBattle);
                string fallenHeroes = fallen < 0 ? "" :
                    NativeOwnClanHeroIds(receiptParty.DiedInBattle);

                string participants = String.Join(",",
                    parties.Where(p => p != null)
                    .Select(p => p.Id ?? "unknown")
                    .OrderBy(s => s, StringComparer.Ordinal));
                string canonical = battle.BattleStartTime.ToHours
                    .ToString("R", CultureInfo.InvariantCulture) +
                    "|" + kind + "|" + settlement + "|" +
                    participants + "|" + source + "|" + lordId;
                string key = SandboxBattleHistoryState.EventKey(canonical);
                // Evidence at the native completed-battle callback, not
                // the player's employer at the later conversation.
                var clan=Clan.PlayerClan;
                bool mercenary=source=="PlayerParty" && clan!=null &&
                    clan.IsUnderMercenaryService && clan.Kingdom!=null;
                string mercenaryHeroId=mercenary?Hero.MainHero.StringId:"";
                string mercenaryKingdomId=mercenary?clan.Kingdom.StringId:"";
                if (!battleHistory.RecordObserved(key, day, kind,
                    outcome, source, settlement, lordId,
                    patronWitnessId, fallen, wounded,
                    woundedHeroes, fallenHeroes,mercenaryHeroId,mercenaryKingdomId))
                    return;
                DiagnosticJournal.Event("cu_native_battle_witness",
                    "day=" + day + "|kind=" + kind +
                    "|result=" + outcome + "|source=" + source +
                    "|settlement=" + settlement + "|lord=" + lordId +
                    "|party_fallen=" + fallen +
                    "|party_wounded=" + wounded +
                    "|clan_wounded_heroes=" + woundedHeroes +
                    "|clan_fallen_heroes=" + fallenHeroes +
                    "|same_side_patron=" + patronWitnessId +
                    "|mercenary_kingdom=" + mercenaryKingdomId +
                    "|personal_kills_not_claimed=true");
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_battle_history_observer_failed", error);
            }
        }

        private string NativeBattleHeroName(string nativeId)
        {
            var person = ResolveCampaignHero(nativeId);
            return person == null ? nativeId :
                person.Name.ToString();
        }

        private string BattleHistoryDescription()
        {
            return "Witnessed completed battles: " +
                battleHistory.Entries.Count +
                " (last " + SandboxBattleHistoryState.MaxEntries +
                " retained)\n" +
                battleHistory.DescribeRecent(5, NativeBattleHeroName) +
                "\nCasualties are native rosters of the participating " +
                "player or enlisted host party, not the whole army. " +
                "Named injuries are verified own-clan Heroes only. " +
                "No personal kills, cause of injury or political " +
                "promotion is inferred. Campaign history, not a personal lifetime ledger. "+
                "Legacy knight battle acknowledgements: "+knightBattleRecognition.Stage+
                " (historical only; no new relation or reward).";
        }
    }
}
