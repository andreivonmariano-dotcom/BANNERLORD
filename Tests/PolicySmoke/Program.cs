using System;
using CalradiaUnified;

internal static class Program
{
    private static int passed;
    private static void Check(string name, bool ok)
    {
        if (!ok) throw new Exception("FAILED: " + name);
        passed++;
        Console.WriteLine("PASS " + name);
    }
    private static void Main()
    {
        Check("exact shortage threshold", SupplyPolicy.IsLowFood(3, 3));
        Check("unknown supply not shortage", !SupplyPolicy.IsLowFood(-1, 3));
        Check("ample food not shortage", !SupplyPolicy.IsLowFood(4, 3));
        Check("player foraging gated off", !SupplyPolicy.ShouldForage(false, 1, 2, false, false, false, false, 9, 5));
        Check("eligible forage", SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, false, 5, 5));
        Check("too soon to forage", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, false, 4, 5));
        Check("never forage during battle", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, true, false, 10, 5));
        Check("never forage inside settlement", !SupplyPolicy.ShouldForage(true, 1, 2, true, false, false, false, 10, 5));
        Check("never forage within army", !SupplyPolicy.ShouldForage(true, 1, 2, false, true, false, false, 10, 5));
        Check("never forage under siege", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, true, 10, 5));
        Check("unknown army status", SupplyPolicy.ArmyRisk(0, 0) == "unknown");
        Check("mixed army", SupplyPolicy.ArmyRisk(1, 3) == "mixed");
        Check("all army parties critical", SupplyPolicy.ArmyRisk(2, 2) == "critical");

        Check("deposit requires stock", CivicPolicy.CanDeposit(true, true, 5, 5));
        Check("deposit forbids unrelated fiefs", !CivicPolicy.CanDeposit(true, false, 5, 5));
        Check("withdraw prevents negative ledger", !CivicPolicy.CanWithdraw(true, true, 4, 5));
        Check("withdraw exact balance", CivicPolicy.CanWithdraw(true, true, 5, 5));
        Check("investment gold guard", !CivicPolicy.CanInvest(true, true, 299, 300, 0, 3));
        Check("investment cap", !CivicPolicy.CanInvest(true, true, 1000, 300, 3, 3));
        Check("investment allowed", CivicPolicy.CanInvest(true, true, 300, 300, 2, 3));
        Check("recruitment cap", !CivicPolicy.CanRecruit(true, true, 200, 200, 100, 100));
        Check("recruitment allowed", CivicPolicy.CanRecruit(true, true, 200, 200, 99, 100));
        Check("aid requires notable", !CivicPolicy.CanAid(true, true, 5, 5, false));
        Check("shipment requires recipient", !CivicPolicy.CanSendShipment(true, true, 5, 5, false, false));
        Check("shipment cannot stack", !CivicPolicy.CanSendShipment(true, true, 5, 5, true, true));
        Check("shipment allowed", CivicPolicy.CanSendShipment(true, true, 5, 5, true, false));
        Check("production respects depot cap", CivicPolicy.DailyDepotOutput(3, 99, 100) == 1);
        Check("no unpaid production", CivicPolicy.DailyDepotOutput(0, 2, 100) == 0);
        Check("npc advisor restricted to own clan", !CivicPolicy.SafeToAdviseNpc(true, false, true, false, false, false, 1, 2));
        Check("npc advisor skips army parties", !CivicPolicy.SafeToAdviseNpc(true, true, true, false, true, false, 1, 2));
        Check("npc advisor recommends low-food action", CivicPolicy.SafeToAdviseNpc(true, true, true, false, false, false, 1, 2));
        var state = new CivicState();
        state.Get("town_A").Grain = 9;
        state.Get("town_A").Investment = 2;
        state.Shipments.Add(new CivicState.Shipment { From="town_A", To="town_B", Grain=5, DaysRemaining=3 });
        var recovered = CivicState.Parse(state.Serialize());
        Check("save round trip depot stock", recovered.Stock("town_A") == 9);
        Check("save round trip investment", recovered.Get("town_A").Investment == 2);
        Check("save round trip shipment", recovered.Shipments.Count == 1 && recovered.Shipments[0].To == "town_B");
        Check("malformed save fails closed", CivicState.Parse("INVALID").Depots.Count == 0);
        Check("invalid settlement id blocked", !CivicState.Valid("bad;id"));
        Check("empty save state safe", CivicState.Parse("").Serialize() == "CU4");
        Check("legacy CU3 depot loads", CivicState.Parse("CU3;D,old_town,6,1").Stock("old_town") == 6);
        Check("legacy CU3 shipment loads", CivicState.Parse("CU3;S,town_A,town_B,5,3").Shipments.Count == 1);
        Check("convoy disabled by default", !ConvoyPolicy.CanDispatch(false, true, true, 20, 10, 0, 2, false));
        Check("convoy requires both owned", !ConvoyPolicy.CanDispatch(true, true, false, 20, 10, 0, 2, false));
        Check("convoy allowed", ConvoyPolicy.CanDispatch(true, true, true, 20, 10, 1, 2, false));
        Check("convoy active cap", !ConvoyPolicy.CanDispatch(true, true, true, 20, 10, 2, 2, false));
        Check("convoy stock guard", !ConvoyPolicy.CanDispatch(true, true, true, 9, 10, 0, 2, false));
        Check("convoy not delivered before arrival", !ConvoyPolicy.Delivered(true, true, false, true));
        Check("convoy can be lost", ConvoyPolicy.Lost(false, false, false));
        Check("convoy cargo limited by actual food", ConvoyPolicy.DeliverableGrain(10, 3, 15) == 3);
        Check("convoy target cap", ConvoyPolicy.DeliverableGrain(10, 10, 2) == 2);
        Check("convoy timer timeout", ConvoyPolicy.TimedOut(0));
        Check("levy opt-in", !KingdomPolicy.CanLevy(false, true, true, true, 500, 10, 40, 100, 0, 2, 250, 5));
        Check("levy requires influence", !KingdomPolicy.CanLevy(true, true, true, true, 500, 4, 40, 100, 0, 2, 250, 5));
        Check("levy requires kingdom", !KingdomPolicy.CanLevy(true, true, false, true, 500, 10, 40, 100, 0, 2, 250, 5));
        Check("levy daily cap", !KingdomPolicy.CanLevy(true, true, true, true, 500, 10, 40, 100, 2, 2, 250, 5));
        Check("kingdom levy allowed", KingdomPolicy.CanLevy(true, true, true, true, 500, 10, 40, 100, 1, 2, 250, 5));
        Check("diplomatic cooldown", !KingdomPolicy.CanMission(true, true, true, true, 500, 15, 2, 300, 10));
        Check("diplomatic external kingdom blocked", !KingdomPolicy.CanMission(true, false, true, true, 500, 15, 0, 300, 10));
        Check("diplomatic mission allowed", KingdomPolicy.CanMission(true, true, true, true, 500, 15, 0, 300, 10));
        recovered.Shipments[0].Physical = true;
        recovered.Shipments[0].PartyId = "cu_convoy_1";
        var king = recovered.CouncilFor("clan_1"); king.MissionCooldown = 7;
        var more = CivicState.Parse(recovered.Serialize());
        Check("convoy save roundtrip", more.Shipments.Count == 1 && more.Shipments[0].Physical
            && more.Shipments[0].PartyId == "cu_convoy_1");
        Check("council save roundtrip", more.CouncilFor("clan_1").MissionCooldown == 7);
        Check("refuse physical missing id", CivicState.Parse("CU4;S,a,b,5,20,P,").Shipments.Count == 0);
        Check("depot full stock price", TradePolicy.UnitPrice(100, 100) == 18);
        Check("depot scarce price", TradePolicy.UnitPrice(2, 100) == 30);
        Check("depot medium price", TradePolicy.UnitPrice(40, 100) == 24);
        Check("depot trade disabled", !TradePolicy.CanPurchase(false,true,true,20,5,900,18));
        Check("depot trade blocked by ownership", !TradePolicy.CanPurchase(true,false,true,20,5,900,18));
        Check("depot trade blocked by empty stock", !TradePolicy.CanPurchase(true,true,true,4,5,900,18));
        Check("depot trade blocked by gold", !TradePolicy.CanPurchase(true,true,true,20,5,89,18));
        Check("depot trade valid", TradePolicy.CanPurchase(true,true,true,20,5,90,18));
        Check("depot trade rejects invalid amount", TradePolicy.BuyCost(0,18) == -1);
        Check("kingdom batch blocks capacity overflow", !KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,99,100,0,4,250,5,2));
        Check("kingdom batch blocks daily cap", !KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,50,100,1,2,250,5,2));
        Check("kingdom batch recruits pair", KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,50,100,0,2,250,5,2));
        Check("strategy battle holds", KingdomAdvisorPolicy.Decide(true,false,false,false,1,20,45,false)
            == KingdomAdvisorPolicy.Priority.Hold);
        Check("strategy defends against threat", KingdomAdvisorPolicy.Decide(false,false,false,false,4,20,45,true)
            == KingdomAdvisorPolicy.Priority.Defend);
        Check("strategy resupplies low food", KingdomAdvisorPolicy.Decide(false,false,false,false,1,20,45,false)
            == KingdomAdvisorPolicy.Priority.Resupply);
        Check("strategy regroups armies", KingdomAdvisorPolicy.Decide(false,false,true,false,7,20,45,false)
            == KingdomAdvisorPolicy.Priority.Regroup);
        Check("strategy recruits in settlement", KingdomAdvisorPolicy.Decide(false,false,false,true,7,20,45,false)
            == KingdomAdvisorPolicy.Priority.Recruit);
        Check("strategy patrols when ready", KingdomAdvisorPolicy.Decide(false,false,false,false,7,50,45,false)
            == KingdomAdvisorPolicy.Priority.Patrol);
        Check("strategy advisory limit", !KingdomAdvisorPolicy.ShouldReport(true,true,true,3,3));
        Check("autonomy disabled by default", KingdomAutonomyPolicy.Decide(false,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy player party blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,true,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy other clan blocked", KingdomAutonomyPolicy.Decide(true,false,true,true,false,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy battle blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,true,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy army blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,true,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy siege blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,true,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy settlement departure blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,true,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy cooldown blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,11) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy resupply allowed", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,9) == KingdomAutonomyPolicy.Order.Resupply);
        Check("autonomy recruitment trip", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,5,2,20,35,10,9) == KingdomAutonomyPolicy.Order.Recruit);
        Check("autonomy unknown food does not resupply", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,-1,2,50,35,10,9) == KingdomAutonomyPolicy.Order.None);
        Check("autonomy unknown depot food blocked", !KingdomAutonomyPolicy.SafeDepot(true,true,false,false,0,40,true));
        Check("autonomy enemy depot blocked", !KingdomAutonomyPolicy.SafeDepot(false,true,false,true,200,40,true));
        Check("autonomy sieged depot blocked", !KingdomAutonomyPolicy.SafeDepot(true,true,true,true,200,40,true));
        Check("autonomy valid depot", KingdomAutonomyPolicy.SafeDepot(true,true,false,true,200,40,true));
        Check("autonomy cooldown lower bound", KingdomAutonomyPolicy.NextOrderDay(10,0) == 12);
        Check("autonomy cooldown overflow", KingdomAutonomyPolicy.NextOrderDay(int.MaxValue,8) == int.MaxValue);

        Check("kingdom strategy refuses inactive lord", KingdomStrategyPolicy.Choose(false,false,false,false,false,true,5,2,60,35,false,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves battle", KingdomStrategyPolicy.Choose(true,true,false,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves existing army order", KingdomStrategyPolicy.Choose(true,false,true,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves siege order", KingdomStrategyPolicy.Choose(true,false,false,true,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy prioritizes defence", KingdomStrategyPolicy.Choose(true,false,false,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Defend);
        Check("kingdom strategy resupplies before recruitment", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,1,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Resupply);
        Check("kingdom strategy wartime recovery", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,5,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Recover);
        Check("kingdom strategy peacetime recruitment", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Recruit);
        Check("kingdom strategy siege support advisory", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,5,2,80,35,true,true) == KingdomStrategyPolicy.Priority.SiegeSupport);
        Check("kingdom strategy patrols safe areas", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,80,35,false,true) == KingdomStrategyPolicy.Priority.Patrol);
        Check("kingdom strategy no unsafe patrol", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,80,35,false,false) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy blocks besieged defence target", !KingdomStrategyPolicy.EligibleDefenseTarget(true,true,true,true,false));
        Check("kingdom strategy blocks enemy defence target", !KingdomStrategyPolicy.EligibleDefenseTarget(false,false,true,true,false));
        Check("kingdom strategy accepts friendly safe target", KingdomStrategyPolicy.EligibleDefenseTarget(true,false,true,true,false));

        Check("strategy command rejects battle override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,true,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects army override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,true,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects siege override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,true,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects settlement departure", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,true,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects foreign clan", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,false,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects cooldown", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,false,
            false,false,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command resupply opt in", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,false,
            false,true,true,false,true,true,true,false) == KingdomCommandPolicy.TravelOrder.Resupply);
        Check("strategy command recovery permits recruiting", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Recover,true,true,true,false,false,false,false,false,
            false,true,true,false,true,false,true,false) == KingdomCommandPolicy.TravelOrder.Recruit);
        Check("strategy command rejects siege support", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.SiegeSupport,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects direct defense", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Defend,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol requires confirmed peace", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,false,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol rejects moving parties", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            true,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol requires confirmed safe settlement", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,false,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol off by default", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,false) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol allowed in safe peacetime", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Patrol);
        Console.WriteLine("PASS: " + passed + " gameplay policy checks");
    }
}
