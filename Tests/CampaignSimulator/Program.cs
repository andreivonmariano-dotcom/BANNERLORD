using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CalradiaUnified;

namespace CalradiaUnified.CampaignSimulator
{
    // A test double for actual engine-created MobileParty actors.
    // This does not instantiate or emulate TaleWorlds classes.
    internal sealed class SimulatedConvoy
    {
        public string Id;
        public string Destination;
        public int ActualCargo;
        public int TravelDays;
        public bool AtDestination;
    }

    internal sealed class SimulatedWorld
    {
        private readonly Random random;
        private readonly int seed;
        private readonly string[] settlements =
            { "town_a", "town_b", "castle_c", "town_d" };
        private readonly Dictionary<string, SimulatedConvoy> parties =
            new Dictionary<string, SimulatedConvoy>();

        private const int DepotCapacity = 100;
        private const int MaxPhysicalConvoys = 3;
        private CivicState state = new CivicState();
        private readonly int startingGrain;
        private readonly int startingGold = 50000;
        private readonly int startingInfluence = 500;
        private int playerGrain = 30;
        private int gold = 50000;
        private int influence = 500;
        private int troops = 20;
        private int produced;
        private int lost;
        private int spentGold;
        private int spentInfluence;
        private int nextPartyId;
        private int day;
        private int checks;
        private int physicalDispatched;
        private int abstractDispatched;
        private int delivered;
        private int convoyLosses;
        private int partialUnloads;
        private int reloads;
        private int npcDecisions;
        private int recruitmentEvents;
        private int tradeEvents;
        private int investments;

        public SimulatedWorld(int seed)
        {
            this.seed = seed;
            random = new Random(seed);
            state.Get("town_a").Grain = 70;
            state.Get("town_a").Investment = 1;
            state.Get("town_b").Grain = 35;
            state.Get("castle_c").Grain = 88;
            state.Get("town_d").Grain = 25;
            startingGrain = playerGrain + state.Depots.Sum(x => x.Grain);
            AssertInvariant();
        }

        private void Require(bool success, string detail)
        {
            checks++;
            if (!success)
                throw new InvalidOperationException("seed=" + seed + " day=" + day +
                    " check=" + detail);
        }

        private int TotalGrain()
        {
            return playerGrain + state.Depots.Sum(x => x.Grain) +
                state.Shipments.Where(x => !x.Physical).Sum(x => x.Grain) +
                parties.Values.Sum(x => x.ActualCargo) + lost;
        }

        private void AssertInvariant()
        {
            Require(TotalGrain() == startingGrain + produced,
                "grain conservation: counted=" + TotalGrain() + " expected=" +
                (startingGrain + produced));
            Require(state.Depots.All(x => x.Grain >= 0 && x.Grain <= DepotCapacity),
                "depot stock capacity");
            Require(playerGrain >= 0 && gold >= 0 && influence >= 0 && troops >= 0,
                "nonnegative player resources");
            Require(gold + spentGold == startingGold, "gold balance and expenditures");
            Require(influence + spentInfluence == startingInfluence,
                "influence balance and expenditures");
            Require(state.Shipments.Count(x => x.Physical) <= MaxPhysicalConvoys,
                "active physical convoy limit");
            Require(state.Shipments.Select(x => x.From).Distinct().Count() ==
                state.Shipments.Count, "only one active shipment per source");
            foreach (var shipment in state.Shipments.Where(x => x.Physical))
            {
                SimulatedConvoy p;
                Require(parties.TryGetValue(shipment.PartyId, out p),
                    "physical convoy has a corresponding simulated party");
                Require(p.ActualCargo >= 0 && p.ActualCargo <= shipment.Grain,
                    "party cargo never exceeds tracked manifest");
                Require(shipment.Grain > 0, "no empty tracked convoy");
            }
            Require(parties.Count == state.Shipments.Count(x => x.Physical),
                "no orphan simulated parties");
        }

        private void MaybeMoveInventory()
        {
            var d = state.Get(settlements[random.Next(settlements.Length)]);
            if (random.Next(2) == 0)
            {
                if (DepotLedgerPolicy.CanDeposit(true, true, playerGrain, 5,
                    d.Grain, DepotCapacity))
                {
                    playerGrain -= 5;
                    d.Grain += 5;
                }
            }
            else if (DepotLedgerPolicy.CanWithdraw(true, true, d.Grain, 5))
            {
                playerGrain += 5;
                d.Grain -= 5;
            }
        }

        private void MaybeTrade()
        {
            var d = state.Get(settlements[random.Next(settlements.Length)]);
            int price = TradePolicy.UnitPrice(d.Grain, DepotCapacity);
            if (!TradePolicy.CanPurchase(true, true, true, d.Grain, 5, gold, price))
                return;
            int amount = TradePolicy.BuyCost(5, price);
            Require(amount > 0 && amount <= gold, "trade price bound");
            gold -= amount;
            spentGold += amount;
            d.Grain -= 5;
            playerGrain += 5;
            tradeEvents++;
        }

        private void MaybeInvest()
        {
            var d = state.Get(settlements[random.Next(settlements.Length)]);
            if (!CivicPolicy.CanInvest(true, true, gold, 300, d.Investment, 3))
                return;
            gold -= 300;
            spentGold += 300;
            d.Investment++;
            investments++;
        }

        private void MaybeRecruit()
        {
            var record = state.CouncilFor("player_clan");
            if (record == null) throw new InvalidOperationException("missing clan record");
            if (KingdomPolicy.CanBatchLevy(true, true, true, true, gold, influence,
                troops, 100, record.LevyToday, 2, 250, 5, 2))
            {
                gold -= 500;
                spentGold += 500;
                influence -= 10;
                spentInfluence += 10;
                troops += 2;
                record.LevyToday += 2;
                recruitmentEvents++;
            }
            Require(troops <= 100 && record.LevyToday <= 2,
                "troop count and daily levy cap");
        }

        private void MaybeDispatch(bool physical)
        {
            string from = settlements[random.Next(settlements.Length)];
            string to = settlements[random.Next(settlements.Length)];
            int cargo = physical ? 10 : 5;
            var src = state.Get(from);
            var dst = state.Get(to);
            bool pending = state.HasShipment(from);
            if (physical)
            {
                bool allowed = ConvoyPolicy.CanDispatchToDepot(true, true, true,
                    from != to, false, src.Grain, cargo, dst.Grain, DepotCapacity,
                    state.Shipments.Count(s => s.Physical), MaxPhysicalConvoys, pending);
                if (!allowed) return;
                var party = new SimulatedConvoy
                {
                    Id = "convoy_" + seed + "_" + (++nextPartyId),
                    Destination = to,
                    ActualCargo = cargo,
                    TravelDays = random.Next(1, 5)
                };
                src.Grain -= cargo;
                parties.Add(party.Id, party);
                state.Shipments.Add(new CivicState.Shipment
                {
                    From = from, To = to, Grain = cargo, Physical = true,
                    PartyId = party.Id, DaysRemaining = 6
                });
                physicalDispatched++;
            }
            else
            {
                bool allowed = from != to && CivicPolicy.CanSendShipment(true, true,
                    src.Grain, cargo,
                    DepotLedgerPolicy.FreeCapacity(dst.Grain, DepotCapacity) >= cargo,
                    pending);
                if (!allowed) return;
                src.Grain -= cargo;
                state.Shipments.Add(new CivicState.Shipment
                {
                    From = from, To = to, Grain = cargo, Physical = false,
                    DaysRemaining = 3
                });
                abstractDispatched++;
            }
        }

        private void RemovePhysical(CivicState.Shipment shipment, SimulatedConvoy party,
            bool cargoLost)
        {
            if (cargoLost)
            {
                lost += party.ActualCargo;
                convoyLosses++;
            }
            parties.Remove(party.Id);
            state.Shipments.Remove(shipment);
        }

        private void SettleShipments()
        {
            foreach (var shipment in state.Shipments.ToArray())
            {
                shipment.DaysRemaining--;
                var destination = state.Get(shipment.To);
                if (!shipment.Physical)
                {
                    if (shipment.DaysRemaining > 0) continue;
                    int credited = DepotLedgerPolicy.Creditable(shipment.Grain,
                        destination.Grain, DepotCapacity);
                    destination.Grain += credited;
                    int remainder = DepotLedgerPolicy.Remainder(shipment.Grain, credited);
                    var origin = state.Get(shipment.From);
                    int refunded = DepotLedgerPolicy.Creditable(remainder, origin.Grain,
                        DepotCapacity);
                    origin.Grain += refunded;
                    lost += DepotLedgerPolicy.Remainder(remainder, refunded);
                    delivered += credited;
                    state.Shipments.Remove(shipment);
                    continue;
                }
                SimulatedConvoy party;
                Require(parties.TryGetValue(shipment.PartyId, out party),
                    "shipment has live simulated party");
                if (!party.AtDestination)
                {
                    // Models time until arrival, never TaleWorlds pathfinding.
                    party.TravelDays--;
                    if (party.TravelDays <= 0) party.AtDestination = true;
                    if (random.Next(50) == 0)
                    {
                        RemovePhysical(shipment, party, true);
                        continue;
                    }
                }
                if (party.AtDestination)
                {
                    int credited = ConvoyPolicy.DeliverableGrain(shipment.Grain,
                        party.ActualCargo, DepotLedgerPolicy.FreeCapacity(
                            destination.Grain, DepotCapacity));
                    if (credited > 0)
                    {
                        destination.Grain += credited;
                        party.ActualCargo -= credited;
                        shipment.Grain -= credited;
                        delivered += credited;
                    }
                    if (shipment.Grain <= 0 || party.ActualCargo <= 0)
                    {
                        RemovePhysical(shipment, party, false);
                        continue;
                    }
                    partialUnloads++;
                    // Mirrors the fixed runtime rule: a convoy parked at a full
                    // depot still expires, releasing its cargo accounting.
                    if (ConvoyPolicy.TimedOut(shipment.DaysRemaining))
                        RemovePhysical(shipment, party, true);
                    continue;
                }
                if (ConvoyPolicy.TimedOut(shipment.DaysRemaining))
                    RemovePhysical(shipment, party, true);
            }
        }

        private void SimulateNpcDecisions()
        {
            bool own = random.Next(4) != 0;
            bool battle = random.Next(8) == 0;
            bool army = random.Next(8) == 0;
            bool siege = random.Next(10) == 0;
            bool player = random.Next(12) == 0;
            bool settlement = random.Next(10) == 0;
            bool war = random.Next(2) == 0;
            bool friendly = random.Next(5) != 0;
            int food = random.Next(-1, 12);
            int soldiers = random.Next(1, 100);
            bool safe = random.Next(4) != 0;
            var priority = KingdomStrategyPolicy.Choose(true, battle, army,
                siege, random.Next(20) == 0, war,
                food, 2, soldiers, 35, random.Next(10) == 0, safe);
            var order = KingdomCommandPolicy.Choose(priority, own, true,
                true, player, battle, army, siege, settlement,
                false, true, friendly, !war, safe, true, true, true);
            if (!own || battle || army || siege || player || settlement || !friendly)
                Require(order == KingdomCommandPolicy.TravelOrder.Hold,
                    "invalid party or destination never receives a travel command");
            if (order == KingdomCommandPolicy.TravelOrder.Patrol)
                Require(!war && safe && priority == KingdomStrategyPolicy.Priority.Patrol,
                    "patrol requires peace and safe area");
            if (order == KingdomCommandPolicy.TravelOrder.Resupply)
                Require(priority == KingdomStrategyPolicy.Priority.Resupply,
                    "resupply command requires resupply strategy");
            if (order == KingdomCommandPolicy.TravelOrder.Recruit)
                Require(priority == KingdomStrategyPolicy.Priority.Recruit ||
                    priority == KingdomStrategyPolicy.Priority.Recover,
                    "recruitment command requires recruiting or recovery strategy");
            var disabled = KingdomAutonomyPolicy.Decide(false, own, true, true,
                player, true, true, battle, army, siege, settlement,
                food, 2, soldiers, 35, day, 0);
            Require(disabled == KingdomAutonomyPolicy.Order.Hold,
                "autonomous NPC movement is blocked by disabled switch");
            npcDecisions++;
        }

        private void SnapshotRoundTrip()
        {
            var packed = state.Serialize();
            var read = CivicState.Parse(packed);
            Require(read.Serialize() == packed, "primitive CU4 state round trip");
            foreach (var shipment in read.Shipments.Where(x => x.Physical))
                Require(parties.ContainsKey(shipment.PartyId),
                    "saved simulated party ID resolves after model reload");
            state = read;
            reloads++;
        }

        private void DailyStep()
        {
            day++;
            var levy = state.CouncilFor("player_clan");
            if (levy != null) levy.LevyToday = 0;
            foreach (var depot in state.Depots)
            {
                int n = CivicPolicy.DailyDepotOutput(depot.Investment, depot.Grain,
                    DepotCapacity);
                depot.Grain += n;
                produced += n;
            }
            if (random.Next(2) == 0) MaybeMoveInventory();
            if (random.Next(4) == 0) MaybeTrade();
            if (random.Next(7) == 0) MaybeInvest();
            if (random.Next(15) == 0) MaybeRecruit();
            if (random.Next(3) == 0) MaybeDispatch(true);
            if (random.Next(4) == 0) MaybeDispatch(false);
            SettleShipments();
            SimulateNpcDecisions();
            if (day % 7 == 0) SnapshotRoundTrip();
            AssertInvariant();
        }

        public object Run(int days)
        {
            for (int d = 0; d < days; d++) DailyStep();
            return new
            {
                seed, days, checks, physicalDispatched, abstractDispatched,
                delivered, convoyLosses, partialUnloads, reloads,
                npcDecisions, recruitmentEvents, tradeEvents, investments,
                generatedGrain = produced, accountedLoss = lost,
                activeShipments = state.Shipments.Count
            };
        }
    }

    internal static class Program
    {
        private static int Examples()
        {
            int count = 0;
            void Check(bool b, string label)
            {
                if (!b) throw new InvalidOperationException("deterministic example: " + label);
                count++;
            }
            Check(DepotLedgerPolicy.Creditable(10, 98, 100) == 2,
                "partial depot unloading");
            Check(DepotLedgerPolicy.Remainder(10, 2) == 8,
                "unloaded manifest remainder");
            Check(ConvoyPolicy.TimedOut(0), "parked convoy timeout at zero");
            Check(ConvoyPolicy.TimedOut(-1), "parked convoy timeout after zero");
            Check(!ConvoyPolicy.CanDispatchToDepot(true, true, true, true,
                false, 40, 10, 95, 100, 0, 3, false),
                "dispatch rejected when receiving depot is nearly full");
            Check(!KingdomPolicy.CanBatchLevy(true, true, true, true,
                1000, 100, 99, 100, 0, 2, 250, 5, 2),
                "batch levy cannot exceed party capacity");
            Check(!KingdomPolicy.CanBatchLevy(true, true, true, true,
                1000, 100, 20, 100, 1, 2, 250, 5, 2),
                "batch levy cannot exceed daily limit");
            var state = new CivicState();
            state.Get("town_a").Grain = 42;
            state.Shipments.Add(new CivicState.Shipment
            {
                From = "town_a", To = "town_b", Physical = true,
                PartyId = "convoy_example", Grain = 8, DaysRemaining = 0
            });
            Check(CivicState.Parse(state.Serialize()).Serialize() ==
                state.Serialize(), "versioned shipment serialization");
            Check(KingdomCommandPolicy.Choose(KingdomStrategyPolicy.Priority.Defend,
                true, true, true, false, false, false, false, false,
                false, true, true, true, true, true, true, true)
                == KingdomCommandPolicy.TravelOrder.Hold,
                "defense remains advisory");
            Check(KingdomAutonomyPolicy.Decide(false, true, true, true,
                false, true, true, false, false, false, false,
                1, 2, 50, 35, 20, 0) == KingdomAutonomyPolicy.Order.Hold,
                "autonomy remains disabled by default");
            return count;
        }

        private static int ReadInt(string[] args, string key, int fallback,
            int min, int max)
        {
            var i = Array.IndexOf(args, key);
            if (i < 0) return fallback;
            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int n)
                || n < min || n > max)
                throw new ArgumentException("Invalid " + key + " value");
            return n;
        }

        private static string ReadString(string[] args, string key)
        {
            int i = Array.IndexOf(args, key);
            if (i < 0) return null;
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                throw new ArgumentException("Invalid " + key + " value");
            return args[i + 1];
        }

        private static int Main(string[] args)
        {
            try
            {
                int seed = ReadInt(args, "--seed", 157, 0, int.MaxValue - 1000);
                int days = ReadInt(args, "--days", 365, 1, 3650);
                int runs = ReadInt(args, "--runs", 40, 1, 100);
                string json = ReadString(args, "--json");
                int examples = Examples();
                var results = new List<object>();
                long totalChecks = examples;
                for (int run = 0; run < runs; run++)
                {
                    var world = new SimulatedWorld(seed + run);
                    var summary = world.Run(days);
                    results.Add(summary);
                    var count = (int)summary.GetType().GetProperty("checks").GetValue(summary);
                    totalChecks += count;
                }
                var report = new
                {
                    status = "PASS", model = "Calradia Unified policy-level campaign simulator",
                    target = "Bannerlord v1.5.7 C# policy implementation",
                    engineExecuted = false, actualBannerlordStartupTest = false,
                    actualBannerlordSaveLoadTest = false,
                    seed, runs, daysPerRun = days,
                    simulatedDays = (long)runs * days,
                    deterministicExamples = examples,
                    totalChecks, simulatedWorlds = results
                };
                var payload = JsonSerializer.Serialize(report,
                    new JsonSerializerOptions { WriteIndented = true });
                if (!string.IsNullOrWhiteSpace(json))
                {
                    string path = Path.GetFullPath(json);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, payload, new UTF8Encoding(false));
                }
                Console.WriteLine("SIMULATION PASS | seeds=" + runs +
                    " | days=" + ((long)runs * days) + " | checks=" + totalChecks);
                Console.WriteLine("MODEL ONLY: no TaleWorlds game runtime, campaign save, map party or pathfinding executed.");
                if (json != null) Console.WriteLine("Report: " + Path.GetFullPath(json));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SIMULATION FAIL: " + ex);
                return 1;
            }
        }
    }
}
