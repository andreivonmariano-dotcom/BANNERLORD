using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// Experimental, narrowly scoped NPC order coordinator.
    /// Only non-player lord parties of the player's own clan, never army members.
    /// No Harmony, no siege/departure patches, no inventory or roster mutations.
    /// Disabled by default pending a real v1.5.7 build and campaign regression test.
    /// </summary>
    public sealed class KingdomAutonomyBehavior : CampaignBehaviorBase
    {
        private readonly Settings settings;
        private int campaignDay;
        private readonly Dictionary<string, int> nextOrderDay = new Dictionary<string, int>(StringComparer.Ordinal);
        private int ordersToday;
        private bool warnedMissingMovementApi;

        public KingdomAutonomyBehavior(Settings settings) { this.settings = settings; }

        public override void RegisterEvents()
        {
            // Compatibility serialization only. Do not compete with native or external party AI.
        }

        public override void SyncData(IDataStore store)
        {
            // A primitive string prevents custom SaveableTypeDefiner dependencies.
            string encoded = Serialize();
            store.SyncData("CU_v060_kingdom_orders", ref encoded);
            store.SyncData("CU_v060_kingdom_day", ref campaignDay);
            Restore(encoded);
            if (campaignDay < 0) campaignDay = 0;
        }

        private void OnDay()
        {
            if (campaignDay < Int32.MaxValue) campaignDay++;
            ordersToday = 0;
            if (nextOrderDay.Count > 1024)
            {
                foreach (var key in nextOrderDay.Where(x => x.Value < campaignDay)
                    .Select(x => x.Key).ToList()) nextOrderDay.Remove(key);
            }
        }

        private void OnParty(MobileParty party)
        {
            if (!settings.EnableAutonomousClanResupply &&
                !settings.EnableAutonomousClanRecruitTrips &&
                !settings.EnableAutonomousClanPatrol) return;
            if (party == null || ordersToday >= settings.AutonomousMaxDailyOrders) return;
            try
            {
                Clan clan = Clan.PlayerClan;
                bool ownLord = clan != null && party.ActualClan == clan && party.IsLordParty;
                bool ownerAlive = party.LeaderHero != null && party.LeaderHero.IsAlive;
                int troopCount = party.MemberRoster == null ? 0 : party.MemberRoster.TotalManCount;
                string id = party.StringId;
                if (!SafeId(id)) return;
                int nextDay;
                if (!nextOrderDay.TryGetValue(id, out nextDay)) nextDay = 0;

                bool inBattle = party.MapEvent != null;
                bool inArmy = party.Army != null;
                bool inSiege = Read(party, "BesiegedSettlement") != null ||
                               Read(party, "SiegeEvent") != null || Read(party, "BesiegerCamp") != null;
                bool inSettlement = party.CurrentSettlement != null;
                bool moving = Object.Equals(Read(party, "IsMoving"), true);
                bool player = Object.ReferenceEquals(party, MobileParty.MainParty);
                // No strategy command during a sea voyage or an existing settlement visit.
                bool seaMovement = Object.Equals(Read(party, "IsInRaftState"), true) ||
                    Object.Equals(Read(party, "IsCurrentlyAtSea"), true) ||
                    Object.Equals(Read(party, "IsTransitionInProgress"), true);
                if (seaMovement || inSettlement) return;

                bool atWar;
                bool warKnown = TryReadWarStatus(clan == null ? null : clan.Kingdom, out atWar);
                // Absence of war information never authorizes a patrol.
                bool peaceConfirmed = warKnown && !atWar;
                bool threatened = HasKnownFriendlySiege(clan);
                int days = party.GetNumDaysForFoodToLast();
                var priority = KingdomStrategyPolicy.Choose(
                    ownLord && ownerAlive && party.IsActive && troopCount > 0 && !player,
                    inBattle, inArmy, inSiege, threatened, atWar, days,
                    settings.AutonomousLowFoodDays, troopCount, settings.AutonomousRecruitBelow,
                    false, peaceConfirmed && !threatened);

                bool allowPatrol = settings.EnableAutonomousClanPatrol &&
                    troopCount >= settings.AutonomousPatrolMinimumTroops;
                var command = KingdomCommandPolicy.Choose(priority, ownLord, ownerAlive,
                    party.IsActive, player, inBattle, inArmy, inSiege, inSettlement,
                    moving, campaignDay >= nextDay, true, peaceConfirmed, !threatened,
                    settings.EnableAutonomousClanResupply,
                    settings.EnableAutonomousClanRecruitTrips, allowPatrol);
                if (command == KingdomCommandPolicy.TravelOrder.Hold) return;

                Settlement destination = PickDestination(party, command);
                if (destination == null) return;
                // Avoid overwriting a party already travelling to the exact same destination.
                if (Object.ReferenceEquals(Read(party, "TargetSettlement"), destination)) return;
                if (!TryOrderToSettlement(party, destination,
                    command == KingdomCommandPolicy.TravelOrder.Patrol)) return;

                nextOrderDay[id] = KingdomAutonomyPolicy.NextOrderDay(campaignDay,
                    settings.AutonomousCooldownDays);
                ordersToday++;
                Log.Write("NPC " + command + " order: party=" + id +
                    " settlement=" + destination.StringId + " day=" + campaignDay);
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("npc_movement_order", ex);
                Log.Write("NPC order not issued: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool TryReadWarStatus(object kingdom, out bool atWar)
        {
            atWar = false;
            object value = Read(kingdom, "Enemies");
            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable == null) return false;
            foreach (object enemy in enumerable) { if (enemy != null) { atWar = true; break; } }
            return true;
        }

        private static bool HasKnownFriendlySiege(Clan ownClan)
        {
            if (ownClan == null) return false;
            foreach (var place in Settlement.All)
            {
                if (place == null || place.OwnerClan == null || place.SiegeEvent == null) continue;
                if (place.OwnerClan == ownClan ||
                    (ownClan.Kingdom != null && place.OwnerClan.Kingdom == ownClan.Kingdom))
                    return true;
            }
            return false;
        }

        private Settlement PickDestination(MobileParty party,
            KingdomCommandPolicy.TravelOrder command)
        {
            if (party == null || Clan.PlayerClan == null) return null;
            var ownClan = Clan.PlayerClan;
            var kingdom = ownClan.Kingdom;
            var options = new List<Settlement>();
            foreach (var place in Settlement.All)
            {
                if (place == null || !SafeId(place.StringId) ||
                    (!place.IsTown && !place.IsCastle) || place.OwnerClan == null ||
                    place.SiegeEvent != null) continue;
                // Patrols remain near the player's own holdings. Other commands
                // can visit safe allied holdings belonging to the same kingdom.
                bool ownerValid = command == KingdomCommandPolicy.TravelOrder.Patrol
                    ? place.OwnerClan == ownClan
                    : kingdom == null ? place.OwnerClan == ownClan
                    : place.OwnerClan.Kingdom == kingdom;
                if (!ownerValid) continue;
                float food;
                bool known = TryNumber(Read(Read(place, "Town"), "FoodStocks"), out food);
                if (!KingdomAutonomyPolicy.SafeDepot(ownerValid, true, false,
                    known, food, command == KingdomCommandPolicy.TravelOrder.Resupply
                    ? settings.AutonomousMinimumSettlementFood : 1, true)) continue;
                options.Add(place);
            }
            return options.Select(s => new { Place=s, Distance=DistanceSquared(party,s) })
                .Where(x => x.Distance >= 0 && x.Distance < float.MaxValue)
                .OrderBy(x => x.Distance).ThenBy(x => x.Place.StringId, StringComparer.Ordinal)
                .Select(x => x.Place).FirstOrDefault();
        }

        private bool TryOrderToSettlement(MobileParty party, Settlement destination,
            bool patrol)
        {
            if (party == null || destination == null) return false;
            string method = patrol ? "SetMovePatrolAroundSettlement" : "SetMoveGoToSettlement";
            // The allow-listed game method must exist with a compatible signature.
            // No fallback to direct target assignment or settlement-departure actions.
            if (InvokeMovement(party, destination, method)) return true;
            if (InvokeMovement(Read(party, "Ai"), destination, method)) return true;
            if (!warnedMissingMovementApi)
            {
                Log.Write("Unsupported " + method + " signature: autonomous movement not issued.");
                warnedMissingMovementApi = true;
            }
            return false;
        }

        private static bool InvokeMovement(object target, Settlement destination, string name)
        {
            if (target == null || destination == null ||
                (name != "SetMoveGoToSettlement" && name != "SetMovePatrolAroundSettlement"))
                return false;
            var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == name && m.ReturnType == typeof(void))
                .OrderBy(m => m.GetParameters().Length);
            foreach (var method in methods)
            {
                var pars = method.GetParameters();
                if (pars.Length < 1 || pars.Length > 4 || pars[0].ParameterType != typeof(Settlement)) continue;
                var args = new object[pars.Length];
                args[0] = destination;
                bool supported = true;
                for (int i=1;i<pars.Length;i++)
                {
                    if (pars[i].IsOptional) args[i] = pars[i].DefaultValue;
                    else if (pars[i].ParameterType == typeof(bool)) args[i] = false;
                    else if (pars[i].ParameterType.IsEnum &&
                             Enum.GetNames(pars[i].ParameterType).Contains("Default"))
                        args[i] = Enum.Parse(pars[i].ParameterType, "Default");
                    else { supported = false; break; }
                }
                if (!supported) continue;
                method.Invoke(target, args);
                return true;
            }
            return false;
        }


        private static float DistanceSquared(object a, object b)
        {
            object aPos = Read(a, "GetPosition2D") ?? Read(a, "Position2D");
            object bPos = Read(b, "GetPosition2D") ?? Read(b, "Position2D");
            float ax, ay, bx, by;
            if (!TryNumber(Read(aPos, "X"), out ax) || !TryNumber(Read(aPos, "Y"), out ay) ||
                !TryNumber(Read(bPos, "X"), out bx) || !TryNumber(Read(bPos, "Y"), out by))
                return float.MaxValue;
            float dx = ax - bx, dy = ay - by;
            float result = dx * dx + dy * dy;
            return float.IsNaN(result) || float.IsInfinity(result) ? float.MaxValue : result;
        }

        private static object Read(object value, string name)
        {
            if (value == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            var p = value.GetType().GetProperty(name, flags);
            if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(value, null);
            var f = value.GetType().GetField(name, flags);
            return f == null ? null : f.GetValue(value);
        }
        private static bool TryNumber(object value, out float result)
        {
            result = 0;
            if (value == null) return false;
            try { result = Convert.ToSingle(value, CultureInfo.InvariantCulture); }
            catch { return false; }
            return !float.IsNaN(result) && !float.IsInfinity(result);
        }
        private static bool SafeId(string id)
        {
            return !String.IsNullOrEmpty(id) && id.Length <= 100 &&
                   id.All(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }
        private string Serialize()
        {
            var b = new StringBuilder("CU6");
            foreach (var record in nextOrderDay.OrderBy(x => x.Key, StringComparer.Ordinal).Take(1024))
                if (SafeId(record.Key)) b.Append(';').Append(record.Key).Append(',')
                    .Append(Math.Max(0, record.Value).ToString(CultureInfo.InvariantCulture));
            return b.ToString();
        }
        private void Restore(string packed)
        {
            nextOrderDay.Clear();
            if (String.IsNullOrEmpty(packed) || packed.Length > 64000 || !packed.StartsWith("CU6;")) return;
            foreach (string item in packed.Split(';').Skip(1).Take(1024))
            {
                string[] parts = item.Split(',');
                int day;
                if (parts.Length == 2 && SafeId(parts[0]) &&
                    Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out day) && day >= 0)
                    nextOrderDay[parts[0]] = day;
            }
        }
    }
}
