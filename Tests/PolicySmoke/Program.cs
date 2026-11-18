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
        Check("start contract accepts genuine Vlandian Lord with fief and spouse",
            SandboxStartContract.Validate("vassal","vassal","vlandia",
                "vlandia",false,1,true,true,0,0).Length == 0);
        Check("start contract refuses fake vanilla Lord",
            Array.IndexOf(SandboxStartContract.Validate("vassal","default",
                "vlandia","",false,0,false,false,0,0),
                "native_start_type_mismatch") >= 0);
        Check("start contract refuses wrong kingdom",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","sturgia",false,1,false,false,0,0),
                "starting_kingdom_mismatch") >= 0);
        Check("start contract refuses fake king without ruling clan",
            Array.IndexOf(SandboxStartContract.Validate("king","king",
                "vlandia","vlandia",false,2,false,false,0,0),
                "ruler_status_or_fief_missing") >= 0);
        Check("start contract refuses missing vassal fief",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,0,false,false,0,0),
                "vassal_status_or_fief_missing") >= 0);
        Check("start contract refuses spouse fields without real hero",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,1,true,false,0,0),
                "requested_spouse_missing") >= 0);
        Check("start contract refuses missing requested children",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,1,true,true,2,1),
                "requested_children_missing") >= 0);
        Check("start contract accepts independent Wanderer",
            SandboxStartContract.Validate("default","default","","",
                false,0,false,false,0,0).Length == 0);

        Check("fief native default needs no transfer",
            SandboxFiefPolicy.ValidateRequest("Wanderer", "Native",
                "", false) == "");
        Check("fief opt-in requires real native political role",
            SandboxFiefPolicy.ValidateRequest("Wanderer", "Castle",
                "", true) == "fief_role_ineligible");
        Check("fief transfer refuses no consent",
            SandboxFiefPolicy.ValidateRequest("Lord", "Specific",
                "castle_test", false) == "fief_transfer_not_authorized");
        Check("fief specific requires a settlement id",
            SandboxFiefPolicy.ValidateRequest("King", "Specific",
                "", true) == "fief_id_required");
        Check("fief grant accepts kingdom castle request",
            SandboxFiefPolicy.ValidateRequest("Lord", "Castle",
                "", true) == "");
        Check("fief grant blocks enemy fief",
            SandboxFiefPolicy.ValidateTarget(true, false, false,
                false, false) == "fief_outside_starting_kingdom");
        Check("fief grant blocks sieged settlement",
            SandboxFiefPolicy.ValidateTarget(true, true, true,
                false, false) == "fief_contested");
        Check("fief grant blocks villages",
            SandboxFiefPolicy.ValidateTarget(false, true, false,
                false, false) == "not_a_fortification");
        Check("fief round-trip keeps exact settlement choice",
            new SandboxOriginProfile().TrySet("StartingFiefMode","Specific") &&
            SandboxOriginProfile.Parse("StartingFiefMode=Specific\n" +
                "StartingFiefId=castle_x\nAllowStartingFiefTransfer=true")
                .StartingFiefId == "castle_x");
        Check("legacy display culture Vlandian maps to real game ID",
            SandboxFamilyOriginPolicy.NormalizeCultureId("Vlandian") == "vlandia" &&
            SandboxFamilyOriginPolicy.NormalizeCultureId("vlandia") == "vlandia");
        Check("legacy Empire display maps to real empire culture",
            SandboxFamilyOriginPolicy.NormalizeCultureId("Imperial") == "empire");
        Check("generated spouse background defaults commoner",
            new SandboxOriginProfile().SpouseBackground == "Commoner");
        Check("generated noble random and chosen modes validate",
            SandboxFamilyOriginPolicy.Validate("NobleRandom","",true) == "" &&
            SandboxFamilyOriginPolicy.Validate("NobleChosen","clan_vlandia_1",true) == "");
        Check("chosen noble birth clan requires exact id",
            SandboxFamilyOriginPolicy.Validate("NobleChosen","",true) ==
                "spouse_noble_clan_required");
        Check("invalid generated social background rejected",
            SandboxFamilyOriginPolicy.Validate("Invented","",true) ==
                "unknown_spouse_background");
        Check("noble parent ages and marriage required",
            SandboxFamilyOriginPolicy.CanBeNobleParents(55,48,30,true,true) &&
            !SandboxFamilyOriginPolicy.CanBeNobleParents(45,47,30,true,true) &&
            !SandboxFamilyOriginPolicy.CanBeNobleParents(55,48,30,false,true));
        Check("social background survives profile save and reload",
            SandboxOriginProfile.Parse("SpouseEnabled=true\n"+
                "SpouseBackground=NobleChosen\n"+
                "SpouseBirthClanId=clan_x\nSpouseCulture=Vlandian")
                .SpouseCulture == "vlandia" &&
            SandboxOriginProfile.Parse("SpouseEnabled=true\n"+
                "SpouseBackground=NobleChosen\n"+
                "SpouseBirthClanId=clan_x").SpouseBirthClanId == "clan_x");
        Check("active marriageable generated couple passes preflight",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,true,true) == "");
        Check("family home defaults to a safe owned fief",
            new SandboxOriginProfile().FamilyHomeMode=="OwnedFief");
        Check("family home refuses unknown mode",
            SandboxFamilyHomePolicy.ValidateRequest("None","",true) ==
                "family_home_mode_invalid");
        Check("family home exact settlement requires ID",
            SandboxFamilyHomePolicy.ValidateRequest("SpecificOwned","",true) ==
                "family_home_id_required");
        var incompleteFamilyHome = new SandboxOriginProfile();
        incompleteFamilyHome.TrySet("SpouseEnabled","true");
        incompleteFamilyHome.TrySet("FamilyHomeMode","SpecificOwned");
        Check("family residence mode can be saved before ID entry",
            incompleteFamilyHome.IsValid(30,out var incompleteHomeError));
        Check("family residence must be complete at final confirmation",
            SandboxFamilyHomePolicy.ValidateRequest(
                incompleteFamilyHome.FamilyHomeMode,
                incompleteFamilyHome.FamilyHomeId,
                incompleteFamilyHome.SpouseEnabled) ==
                "family_home_id_required");
        Check("family home accepts owned unbesieged fortified destination",
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,false,false)=="");
        Check("family home refuses cross-clan siege or village",
            SandboxFamilyHomePolicy.ValidateSettlement(false,true,false,false) ==
                "not_player_clan_fief" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,true,false) ==
                "settlement_under_siege" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,false,false,false) ==
                "not_castle_or_town" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,false,true) ==
                "settlement_in_battle");
        Check("a baby remains a minor without party after homing",
            SandboxFamilyHomePolicy.CanPlaceMinor(true,true,true,true) &&
            !SandboxFamilyHomePolicy.CanPlaceMinor(true,true,true,false) &&
            !SandboxFamilyHomePolicy.CanPlaceMinor(true,true,false,true));
        var familyHomeProfile = new SandboxOriginProfile();
        familyHomeProfile.TrySet("FamilyHomeMode","SpecificOwned");
        familyHomeProfile.TrySet("FamilyHomeId","castle_V1");
        familyHomeProfile.TrySet("SpouseEnabled","true");
        Check("family residence selected castle survives serialize",
            SandboxOriginProfile.Parse(familyHomeProfile.Serialize())
                .FamilyHomeMode=="SpecificOwned" &&
            SandboxOriginProfile.Parse(familyHomeProfile.Serialize())
                .FamilyHomeId=="castle_V1");
        familyHomeProfile.ResetForNewCampaign();
        Check("new campaign resets family home choice",
            familyHomeProfile.FamilyHomeMode=="OwnedFief" &&
            familyHomeProfile.FamilyHomeId=="");
        Check("native fresh NotSpawned hero is refused",
            SandboxGeneratedMarriagePolicy.Check(
                true,false,true,false,true,true,true) ==
                "generated_spouse_not_active");
        Check("born noble clan must be native-marriage-suitable",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,false,true) ==
                "spouse_birth_clan_invalid");
        Check("native marriage eligibility must be verified after activation",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,false,true,true,true) ==
                "generated_spouse_not_eligible");
        Check("native marriage fails on same-sex pair",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,true,false) ==
                "sex_pair_rejected");
        Check("automatic spouse gender yields opposite of male player",
            SandboxFamilyPolicy.ResolveSpouseFemale("Auto",false));
        Check("automatic spouse gender yields opposite of female player",
            !SandboxFamilyPolicy.ResolveSpouseFemale("Auto",true));
        Check("explicit spouse female and male choices respected",
            SandboxFamilyPolicy.ResolveSpouseFemale("Female",false) &&
            !SandboxFamilyPolicy.ResolveSpouseFemale("Male",true));
        Check("family profile defaults automatic spouse gender",
            new SandboxOriginProfile().SpouseGender == "Auto" &&
            SandboxOriginProfile.Parse("SpouseGender=Auto").SpouseGender == "Auto");
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
        Check("empty save state safe", CivicState.Parse("").Serialize() == "CU5");
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
        Check("depot free capacity exactly", DepotLedgerPolicy.FreeCapacity(94, 100) == 6);
        Check("depot zero capacity rejected", DepotLedgerPolicy.FreeCapacity(1, 0) == 0);
        Check("depot negative stock fails closed", DepotLedgerPolicy.FreeCapacity(-1, 100) == 0);
        Check("depot deposit fits precisely", DepotLedgerPolicy.CanDeposit(true, true, 5, 5, 95, 100));
        Check("depot deposit refuses overflow", !DepotLedgerPolicy.CanDeposit(true, true, 5, 5, 96, 100));
        Check("depot deposit refuses disabled", !DepotLedgerPolicy.CanDeposit(false, true, 5, 5, 95, 100));
        Check("depot withdraw blocks overdraft", !DepotLedgerPolicy.CanWithdraw(true, true, 4, 5));
        Check("depot arrival respects cap", DepotLedgerPolicy.Creditable(10, 98, 100) == 2);
        Check("depot arrival blocks negative amount", DepotLedgerPolicy.Creditable(-2, 0, 100) == 0);
        Check("depot arrival remains bounded when full", DepotLedgerPolicy.Creditable(10, 100, 100) == 0);
        Check("shipment remainder bounded", DepotLedgerPolicy.Remainder(5, 7) == 0);
        Check("shipment remainder exact", DepotLedgerPolicy.Remainder(5, 2) == 3);
        Check("convoy dispatch blocks full recipient", !ConvoyPolicy.CanDispatchToDepot(true,true,true,true,false,50,10,95,100,0,2,false));
        Check("convoy dispatch exact recipient space", ConvoyPolicy.CanDispatchToDepot(true,true,true,true,false,50,10,90,100,0,2,false));
        Check("convoy dispatch blocks besieged recipient", !ConvoyPolicy.CanDispatchToDepot(true,true,true,true,true,50,10,0,100,0,2,false));
        Check("convoy dispatch blocks same settlement", !ConvoyPolicy.CanDispatchToDepot(true,true,true,false,false,50,10,0,100,0,2,false));
        Check("convoy unload tracks remaining manifest", DepotLedgerPolicy.Remainder(10, ConvoyPolicy.DeliverableGrain(10,10,3)) == 7);
        Check("convoy never credits more than cargo", ConvoyPolicy.DeliverableGrain(10,7,100) == 7);

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
        Check("local recruit daily cap", !LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 200, 200, 20, 100, 2, 2));
        Check("local recruit lacks gold", !LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 199, 200, 20, 100, 0, 2));
        Check("local recruit ownership guarded", !LocalRecruitmentPolicy.CanRecruit(
            true, false, true, 200, 200, 20, 100, 0, 2));
        Check("local recruit allowed within cap", LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 200, 200, 20, 100, 1, 2));
        Check("garrison disabled by default", !GarrisonTrainingPolicy.CanTrain(
            false,true,false,true,true,250,250,5,5,90,100,0,4,2));
        Check("garrison under siege rejected", !GarrisonTrainingPolicy.CanTrain(
            true,true,true,true,true,250,250,5,5,90,100,0,4,2));
        Check("garrison requires real party", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,false,true,250,250,5,5,90,100,0,4,2));
        Check("garrison grain shortage rejected", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,4,5,90,100,0,4,2));
        Check("garrison full-party cap", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,99,100,0,4,2));
        Check("garrison daily cap", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,90,100,4,4,2));
        Check("garrison exact capacity allowed", GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,98,100,0,4,2));
        var legacyCouncil = CivicState.Parse("CU4;K,town_a,1,6");
        Check("legacy CU4 council migration",
            legacyCouncil.CouncilFor("town_a").LevyToday == 1 &&
            legacyCouncil.CouncilFor("town_a").MissionCooldown == 6 &&
            legacyCouncil.CouncilFor("town_a").LocalToday == 0 &&
            legacyCouncil.CouncilFor("town_a").GarrisonToday == 0);
        var newCounters = new CivicState();
        newCounters.CouncilFor("town_a").LocalToday = 1;
        newCounters.CouncilFor("town_a").GarrisonToday = 2;
        var reloadedCounters = CivicState.Parse(newCounters.Serialize());
        Check("new daily counters survive CU5 reload",
            reloadedCounters.CouncilFor("town_a").LocalToday == 1 &&
            reloadedCounters.CouncilFor("town_a").GarrisonToday == 2);
        Check("estate forecast castle + two healthy villages",
            FeudalBudgetPolicy.EstimateEstateIncome(1500, 900) == 1450);
        Check("estate forecast ignores negative unknown input",
            FeudalBudgetPolicy.EstimateEstateIncome(-10, 900) == 0);
        Check("wage cap leaves positive target",
            FeudalBudgetPolicy.WageBudget(1450, 500, 150, 250, 1800) == 550);
        Check("wage cap refuses unaffordable field party",
            FeudalBudgetPolicy.WageBudget(1450, 7000, 150, 250, 1800) == 0);
        Check("healthy castle projected net after cap",
            FeudalBudgetPolicy.ProjectedBalance(1450, 500, 550, 150, 0) == 250);
        Check("overstaffed castle baseline loses 10k possible",
            FeudalBudgetPolicy.ProjectedBalance(1450, 500, 10000, 150, 0) < -5000);
        Check("garrison cap may reduce unlimited prior expense",
            FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 8000, 550, 250));
        Check("never lower garrison wages during siege",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, true, 8000, 550, 250));
        Check("do not raise an existing manual cap",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 300, 550, 250));
        Check("zero current cap untouched",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 0, 550, 250));
        Check("reject a garrison cap below defensive threshold",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 8000, 200, 250));
        Check("harvest respects depot free space",
            FeudalBudgetPolicy.VillageHarvest(900, 5, 150, 10) == 5);
        Check("raided villages produce no extra grain",
            FeudalBudgetPolicy.VillageHarvest(0, 100, 150, 10) == 0);
        Check("harvest stops when depot full",
            FeudalBudgetPolicy.VillageHarvest(900, 0, 150, 10) == 0);
        Check("surplus sale respects reserve and daily cap",
            FeudalBudgetPolicy.SurplusSale(65, 30, 8) == 8);
        Check("surplus sale never drains protected grain",
            FeudalBudgetPolicy.SurplusSale(30, 30, 8) == 0);
        Check("automated garrison training only with fiscal room",
            FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 450, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison stops when wages reach cap",
            !FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 525, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison stops during siege",
            !FeudalBudgetPolicy.AffordableTraining(true, true, true,
                1450, 500, 450, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison obeys player gold reserve",
            !FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 450, 550, 150, 250, 50,
                3000, 5000, 40, 55));
        Check("castle required income", CastleManagementPolicy.RequiredIncome(500, 750, 150, 250) == 1650);
        Check("castle modeled shortfall", CastleManagementPolicy.ModeledShortfall(1450, 1650) == 200);
        Check("castle shortfall zero", CastleManagementPolicy.ModeledShortfall(1650, 1650) == 0);
        Check("castle required rejects negative", CastleManagementPolicy.RequiredIncome(-1, 750, 150, 250) == 0);
        Check("castle required saturates", CastleManagementPolicy.RequiredIncome(int.MaxValue, 10, 0, 0) == int.MaxValue);
        Check("manual transfer disabled", !CastleManagementPolicy.CanMove(false, true, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer bounded", CastleManagementPolicy.CanMove(true, true, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer low source rejected", !CastleManagementPolicy.CanMove(true, true, true, false, true, true, 4, 40, 50, 5));
        Check("manual transfer siege rejected", !CastleManagementPolicy.CanMove(true, true, true, true, true, true, 5, 40, 50, 5));
        Check("manual transfer foreign rejected", !CastleManagementPolicy.CanMove(true, false, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer full destination rejected", !CastleManagementPolicy.CanMove(true, true, true, false, true, true, 5, 46, 50, 5));
        Check("manual transfer missing garrison rejected", !CastleManagementPolicy.CanMove(true, true, true, false, false, true, 5, 40, 50, 5));
        Check("manual wage cap allowed", CastleManagementPolicy.CanApplyCap(true, true, true, false, 2000, 1000, 250, 900));
        Check("manual wage cap no layoffs", !CastleManagementPolicy.CanApplyCap(true, true, true, false, 2000, 1000, 250, 1100));
        Check("manual wage cap siege rejected", !CastleManagementPolicy.CanApplyCap(true, true, true, true, 2000, 1000, 250, 900));
        Check("manual wage cap disabled", !CastleManagementPolicy.CanApplyCap(false, true, true, false, 2000, 1000, 250, 900));
        var catalogKeys = new System.Collections.Generic.HashSet<string>();
        foreach (var option in SettingsCatalog.All)
        {
            Check("catalog has unique field " + option.Key,
                catalogKeys.Add(option.Key));
            Check("catalog field matches Settings type " + option.Key,
                typeof(Settings).GetField(option.Key) != null &&
                typeof(Settings).GetField(option.Key).FieldType ==
                (option.Boolean ? typeof(bool) : typeof(int)));
            Check("catalog default value is valid " + option.Key,
                SettingsCatalog.TryNormalize(option.Key,
                    SettingsCatalog.Read(new Settings(), option), out var normalizedDefault));
            if (!option.Boolean)
            {
                Check("catalog numeric upper bound " + option.Key,
                    !SettingsCatalog.TryNormalize(option.Key,
                        ((long)option.Max + 1).ToString(), out var tooBig));
                Check("catalog numeric lower bound " + option.Key,
                    !SettingsCatalog.TryNormalize(option.Key,
                        ((long)option.Min - 1).ToString(), out var tooSmall));
                Check("catalog numeric clamp " + option.Key,
                    SettingsCatalog.Next(option.Max, option, 1, 10) == option.Max &&
                    SettingsCatalog.Next(option.Min, option, -1, 10) == option.Min);
            }
        }
        Check("catalog covers every public configurable value",
            catalogKeys.Count == 64);
        var origin = new SandboxOriginProfile();
        Check("roles cannot forge king or lord titles",
            !SandboxRolePolicy.MatchesNativeStatus("King", false, false) &&
            SandboxRolePolicy.MatchesNativeStatus("King", true, true) &&
            !SandboxRolePolicy.MatchesNativeStatus("Lord", true, true) &&
            !SandboxRolePolicy.MatchesNativeStatus("Knight", false, false) &&
            SandboxRolePolicy.MatchesNativeStatus("Soldier", true, false));
        Check("role packages have distinct troops and renown",
            SandboxRolePolicy.BonusRecruits("Wanderer") == 0 &&
            SandboxRolePolicy.BonusRecruits("Soldier") == 4 &&
            SandboxRolePolicy.BonusRecruits("Knight") == 2 &&
            SandboxRolePolicy.BonusRecruits("King") == 8 &&
            SandboxRolePolicy.StarterRenown("Knight") == 40);
        Check("role bonuses require confirmed native contract result",
            SandboxRolePolicy.ConfirmedNativeStanding("Soldier",
                "native_mercenary_contract") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("Soldier",
                "native_affiliation_retained") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("Knight",
                "native_mercenary_contract") &&
            SandboxRolePolicy.ConfirmedNativeStanding("Knight",
                "native_vassal_contract") &&
            SandboxRolePolicy.ConfirmedNativeStanding("King",
                "native_king_confirmed") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("King",
                "king_requires_official_advanced_start"));
        Check("native affiliations require explicit opt-in",
            SandboxRolePolicy.CanRequestAffiliation("Soldier", false, true) &&
            !SandboxRolePolicy.CanRequestAffiliation("Soldier", true, true) &&
            !SandboxRolePolicy.CanRequestAffiliation("Soldier", false, false) &&
            !SandboxRolePolicy.CanRequestAffiliation("King", false, true));
        Check("family creator refuses duplicate native spouse",
            SandboxFamilyPolicy.Guard(true,true,0,false,0,true,false,true)
                == "existing_spouse");
        Check("family creator refuses duplicate native children",
            SandboxFamilyPolicy.Guard(false,false,2,true,0,true,false,true)
                == "existing_children");
        Check("family creator refuses fake siblings without parents",
            SandboxFamilyPolicy.Guard(false,false,0,false,2,false,false,true)
                == "native_parents_missing");
        Check("family creator rejects untested same-sex native marriage",
            SandboxFamilyPolicy.Guard(true,false,0,false,0,true,true,true)
                == "native_marriage_same_gender_untested");
        Check("single-parent child age validation",
            SandboxFamilyPolicy.ValidChildAge(3,20,false,0) &&
            !SandboxFamilyPolicy.ValidChildAge(7,20,false,0) &&
            !SandboxFamilyPolicy.ValidChildAge(10,30,true,24));
        Check("family defaults use valid hero names",
            new SandboxOriginProfile().TrySet("ChildCount", "2") &&
            new SandboxOriginProfile().Children[0].Name == "Alden");
        var nativeReviewOptIn = new SandboxOriginProfile();
        Check("native CU 3D review is off by default",
            !nativeReviewOptIn.NativeIntegrationReviewEnabled);
        nativeReviewOptIn.TrySet("NativeIntegrationReviewEnabled","true");
        Check("native CU 3D review opt-in survives exact profile serialization",
            SandboxOriginProfile.Parse(nativeReviewOptIn.Serialize())
                .NativeIntegrationReviewEnabled);
        nativeReviewOptIn.ResetForNewCampaign();
        Check("native CU 3D review does not silently carry into new games",
            !nativeReviewOptIn.NativeIntegrationReviewEnabled);
        var priorCampaign = new SandboxOriginProfile();
        priorCampaign.TrySet("Role","Lord");
        priorCampaign.TrySet("SpouseEnabled","true");
        priorCampaign.TrySet("ChildCount","2");
        priorCampaign.TrySet("Child.1.Name","Alina");
        priorCampaign.TrySet("Child.1.Gender","Female");
        priorCampaign.TrySet("StartingFiefMode","Castle");
        priorCampaign.TrySet("AllowStartingFiefTransfer","true");
        priorCampaign.ResetForNewCampaign();
        Check("fresh Sandbox clears previous child count and fief transfer",
            priorCampaign.ChildCount==0 && !priorCampaign.SpouseEnabled &&
            priorCampaign.StartingFiefMode=="Native" &&
            !priorCampaign.AllowStartingFiefTransfer &&
            !priorCampaign.Enabled);
        Check("fresh Sandbox keeps role and child name as optional presets",
            priorCampaign.Role=="Lord" &&
            priorCampaign.Children[0].Name=="Alina" &&
            priorCampaign.Children[0].Gender=="Female");
        priorCampaign.TrySet("SpouseEnabled","true");
        priorCampaign.TrySet("ChildCount","1");
        var oneChildReload=SandboxOriginProfile.Parse(
            priorCampaign.Serialize());
        Check("exactly one child remains after save/reload of a fresh profile",
            oneChildReload.ChildCount==1 &&
            oneChildReload.SpouseEnabled &&
            oneChildReload.Children[0].Name=="Alina" &&
            oneChildReload.Children[1].Name!="Alina" &&
            oneChildReload.IsValid(30,out var oneChildValidity));

        var nativeFamilyPreset = new SandboxOriginProfile();
        nativeFamilyPreset.TrySet("ChildCount", "2");
        nativeFamilyPreset.TrySet("SpouseEnabled", "true");
        nativeFamilyPreset.TrySet("Child.1.Age", "1");
        nativeFamilyPreset.TrySet("Child.2.Age", "2");
        Check("native married-two-children preset validates without text edits",
            nativeFamilyPreset.IsValid(30, out var presetProblem));
        nativeFamilyPreset.TrySet("SpouseEnabled", "false");
        nativeFamilyPreset.TrySet("ChildCount", "1");
        Check("native single-parent preset validates without text edits",
            nativeFamilyPreset.IsValid(20, out var singleParentProblem));
        nativeFamilyPreset.TrySet("ChildCount", "0");
        nativeFamilyPreset.TrySet("SiblingCount", "2");
        Check("native sibling-only preset validates without text edits",
            nativeFamilyPreset.IsValid(20, out var siblingProblem));
        Check("the visual creator defaults on, with an explicit native fallback",
            new SandboxOriginProfile().OverhauledCreatorEnabled &&
            new SandboxOriginProfile().TrySet("OverhauledCreatorEnabled","false") &&
            !SandboxOriginProfile.Parse("OverhauledCreatorEnabled=false").OverhauledCreatorEnabled);
        Check("culture preferences are bounded profile data only",
            origin.TrySet("StartingCulture","empire") &&
            !origin.TrySet("StartingCulture","bad culture id"));
        Check("Admiral is a selectable profile but requires separate naval integration",
            Array.IndexOf(SandboxOriginProfile.Roles,"Admiral")>=0 &&
            SandboxRolePolicy.BonusRecruits("Admiral")==0 &&
            !SandboxRolePolicy.MatchesNativeStatus("Admiral",true,true));
        Check("new sandbox origin disabled by default", !origin.Enabled);
        Check("sandbox all native start roles included", SandboxOriginProfile.Roles.Length >= 9 &&
            Array.IndexOf(SandboxOriginProfile.Roles, "Wanderer") >= 0 &&
            Array.IndexOf(SandboxOriginProfile.Roles, "King") >= 0);
        Check("three risky v0.17 features remain default-off",
            !new SandboxOriginProfile().NativeStageEnabled &&
            !new SandboxOriginProfile().RealFamilyEnabled &&
            !new SandboxOriginProfile().RoleAffiliationsEnabled);
        Check("profile opt-ins are strict booleans and survive roundtrip",
            !origin.TrySet("NativeStageEnabled", "yes") &&
            origin.TrySet("NativeStageEnabled", "true") &&
            origin.TrySet("RealFamilyEnabled", "true") &&
            origin.TrySet("RoleAffiliationsEnabled", "true") &&
            SandboxOriginProfile.Parse(origin.Serialize()).NativeStageEnabled &&
            SandboxOriginProfile.Parse(origin.Serialize()).RealFamilyEnabled &&
            SandboxOriginProfile.Parse(origin.Serialize()).RoleAffiliationsEnabled);
        Check("sandbox default does not replace native treasury",
            origin.Gold == -1 && origin.Influence == -1);
        Check("sandbox reject invalid role", !origin.TrySet("Role", "God"));
        Check("sandbox accept knight role", origin.TrySet("Role", "Knight") &&
            origin.Role == "Knight");
        Check("sandbox reject unbounded gold", !origin.TrySet("Gold", "9000000"));
        Check("sandbox accept explicit wealth", origin.TrySet("Gold", "12000") &&
            origin.Gold == 12000);
        Check("sandbox accept configurable spouse",
            origin.TrySet("SpouseEnabled", "true") &&
            origin.TrySet("SpouseName", "Lady Aria") &&
            origin.TrySet("SpouseAge", "35"));
        Check("sandbox child requires parental ages",
            origin.TrySet("ChildCount", "2") &&
            origin.TrySet("Child.1.Name", "Aelia") &&
            origin.TrySet("Child.1.Age", "8") &&
            origin.TrySet("Child.2.Name", "Cassian") &&
            origin.TrySet("Child.2.Age", "9") &&
            origin.IsValid(34, out var validOrigin));
        Check("sandbox reject child older than parent",
            origin.TrySet("Child.1.Age", "30") &&
            !origin.IsValid(34, out var childAgeError));
        origin.TrySet("Child.1.Age", "8");
        Check("sandbox supports single parent with existing children",
            origin.TrySet("SpouseEnabled", "false") &&
            origin.IsValid(34, out var noSpouseError));
        origin.TrySet("SpouseEnabled", "true");
        Check("sandbox reject bad hero name",
            !origin.TrySet("SpouseName", "Bad\nName"));
        Check("sandbox skill override bounded",
            origin.TrySet("Skill.OneHanded", "220") &&
            !origin.TrySet("Skill.OneHanded", "440"));
        Check("sandbox attributes bounded",
            origin.TrySet("Attribute.Vigor", "8") &&
            !origin.TrySet("Attribute.Vigor", "11"));
        Check("sandbox focus bounded",
            origin.TrySet("Focus.Bow", "4") &&
            !origin.TrySet("Focus.Bow", "9"));
        Check("sandbox traits bounded",
            origin.TrySet("Trait.Mercy", "-2") &&
            !origin.TrySet("Trait.Mercy", "-4"));
        Check("sandbox equipment exact id allow-list",
            origin.TrySet("Item.1", "empire_sword_1_t2") &&
            !origin.TrySet("Item.13", "empire_sword_1_t2") &&
            !origin.TrySet("Item.2", "../../evil"));
        var inactiveChild = new SandboxOriginProfile();
        Check("sandbox remembers child name before count activation",
            inactiveChild.TrySet("Child.1.Name", "Mira") &&
            SandboxOriginProfile.Parse(inactiveChild.Serialize()).Children[0].Name
                == "Mira");
        Check("sandbox roundtrip all family and stats data",
            SandboxOriginProfile.Parse(origin.Serialize()).Children[0].Name == "Aelia" &&
            SandboxOriginProfile.Parse(origin.Serialize()).Overrides["Skill.OneHanded"] == 220 &&
            SandboxOriginProfile.Parse(origin.Serialize()).ItemIds[1] == "empire_sword_1_t2");
        var originTemp = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "CU-Origin-" + Guid.NewGuid().ToString("N"), "origin.ini");
        try
        {
            string saveError;
            Check("sandbox origin persisted atomically",
                origin.Save(originTemp, out saveError) &&
                SandboxOriginProfile.Load(originTemp).SpouseName == "Lady Aria");
            var broken = SandboxOriginProfile.Load(originTemp);
            broken.ChildCount = 1;
            broken.SpouseEnabled = false;
            broken.Children[0].Age = 50;
            string previous = System.IO.File.ReadAllText(originTemp);
            Check("invalid sandbox family cannot overwrite profile",
                !broken.Save(originTemp, out saveError) &&
                System.IO.File.ReadAllText(originTemp) == previous);
            Check("malformed origin profile is safe",
                !SandboxOriginProfile.Parse("Enabled=nope\nRole=wrong\nGold=abc").Enabled);
        }
        finally
        {
            string folder = System.IO.Path.GetDirectoryName(originTemp);
            if (System.IO.Directory.Exists(folder))
                System.IO.Directory.Delete(folder, true);
        }
        Check("settings allow-list accepts reporting toggles",
            SettingsMenuPolicy.TryNormalize("EnableSupplyWarnings", "false", out var normalizedBool)
            && normalizedBool == "false");
        Check("settings allow-list rejects external patch switch",
            !SettingsMenuPolicy.TryNormalize("EnableHarmonyPatching", "true", out var unknown));
        Check("settings catalog exposes risky patrol under explicit experimental state",
            SettingsMenuPolicy.TryNormalize("EnableAutonomousClanPatrol", "true", out var forbidden)
            && SettingsCatalog.Find("EnableAutonomousClanPatrol").Experimental);
        Check("settings rejects malformed bool",
            !SettingsMenuPolicy.TryNormalize("EnableEconomy", "yes", out var badBool));
        Check("settings accepts maximum day threshold",
            SettingsMenuPolicy.TryNormalize("SupplyWarningDays", "15", out var bounded)
            && bounded == "15");
        Check("settings rejects day threshold overflow",
            !SettingsMenuPolicy.TryNormalize("SupplyWarningDays", "16", out var overflow));
        Check("settings rejects negative recruit cap",
            !SettingsMenuPolicy.TryNormalize("LocalRecruitDailyCap", "-1", out var negative));
        Check("settings replace preserves unknown keys and comments",
            SettingsMenuPolicy.UpdateLines("# user note\nUnknownCustomOption=yes\nEnableSupplyWarnings=true\n",
                "EnableSupplyWarnings", "false").Contains("UnknownCustomOption=yes")
            && SettingsMenuPolicy.UpdateLines("# user note\nEnableSupplyWarnings=true\n",
                "EnableSupplyWarnings", "false").Contains("# user note"));
        Check("settings replace prevents duplicate definitions",
            SettingsMenuPolicy.UpdateLines("EnableEconomy=true\nEnableEconomy=true",
                "EnableEconomy", "false").Split(new[] {"EnableEconomy="},
                StringSplitOptions.None).Length == 2);
        var tempSettings = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "CU-Settings-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempSettings);
        try
        {
            string baseline = System.IO.Path.Combine(tempSettings, "baseline.ini");
            string userIni = System.IO.Path.Combine(tempSettings, "user.ini");
            System.IO.File.WriteAllText(baseline,
                "# default configuration\nEnableEconomy=false\nSupplyWarningDays=3\n");
            var settings = Settings.LoadFromPaths(baseline, userIni);
            Check("settings start with safe baseline", !settings.EnableEconomy &&
                settings.SupplyWarningDays == 3);
            string configError;
            Check("settings in-game write succeeds",
                settings.TrySetMenuValue("EnableSupplyWarnings", "false",
                    userIni, out configError) && !settings.EnableSupplyWarnings);
            Check("settings write persists across new object",
                !Settings.LoadFromPaths(baseline, userIni).EnableSupplyWarnings);
            Check("settings experimental stays opt-in on reload",
                !Settings.LoadFromPaths(baseline, userIni).EnableEconomy);
            Check("settings can enable and persist an opt-in feature",
                settings.TrySetMenuValue("EnableEconomy", "true",
                    userIni, out configError) && settings.EnableEconomy &&
                Settings.LoadFromPaths(baseline, userIni).EnableEconomy);
            Check("settings numeric updates apply immediately",
                settings.TrySetMenuValue("SupplyWarningDays", "5",
                    userIni, out configError) && settings.SupplyWarningDays == 5);
            string previousContents = System.IO.File.ReadAllText(userIni);
            Check("settings invalid input does not mutate disk",
                !settings.TrySetMenuValue("SupplyWarningDays", "99",
                    userIni, out configError) &&
                System.IO.File.ReadAllText(userIni) == previousContents &&
                settings.SupplyWarningDays == 5);
            Check("settings malformed path does not change live state",
                !settings.TrySetMenuValue("EnableEconomy", "false",
                    "", out configError) && settings.EnableEconomy);
            Check("settings preserving unrelated user entries",
                SettingsMenuPolicy.UpdateLines("# keep\nExtraKey=hello\n",
                    "EnableCivicSystems", "false").Contains("ExtraKey=hello"));
        }
        finally
        {
            System.IO.Directory.Delete(tempSettings, true);
        }
        Check("diagnostic CSV escapes quotes and comma",
            DiagnosticJournal.CsvField("castle,\"test\"") == "\"castle,\"\"test\"\"\"");
        Check("diagnostic path redactor accepts empty", DiagnosticJournal.StripPersonalPath("") == "");
        var tempDiagnostic = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "CU-Diagnostic-Test-" + Guid.NewGuid().ToString("N"));
        try
        {
            DiagnosticJournal.OverrideRootForTests = tempDiagnostic;
            DiagnosticJournal.Enabled = true;
            var cells = new string[19];
            for (int i = 0; i < cells.Length; i++) cells[i] = "0";
            cells[0] = "castle";
            cells[2] = "castle_test";
            DiagnosticJournal.Finance(cells);
            DiagnosticJournal.Event("test_event", "change=0");
            DiagnosticJournal.Exception("test_crash",
                new InvalidOperationException("Mock crash with comma, and quotes"));
            Check("diagnostic finance CSV writes correct header",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "finance.csv"))
                    .StartsWith(DiagnosticJournal.FinanceHeader));
            Check("diagnostic exception CSV captures managed failure",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "exceptions.csv"))
                    .Contains("InvalidOperationException"));
            Check("diagnostic exception CSV preserves escaped commas",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "exceptions.csv"))
                    .Contains("Mock crash with comma, and quotes"));
            string exported = DiagnosticJournal.ExportSnapshot();
            Check("diagnostic export contains finance and crash evidence",
                System.IO.File.Exists(System.IO.Path.Combine(exported, "finance.csv")) &&
                System.IO.File.Exists(System.IO.Path.Combine(exported, "exceptions.csv")) &&
                System.IO.File.Exists(System.IO.Path.Combine(exported, "events.csv")));
            DiagnosticJournal.Enabled = false;
            DiagnosticJournal.Event("disabled", "should_not_be_written");
            Check("opt-out stops writing diagnostic journal",
                !System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "events.csv"))
                    .Contains("should_not_be_written"));
        }
        finally
        {
            DiagnosticJournal.Enabled = true;
            DiagnosticJournal.OverrideRootForTests = null;
            if (System.IO.Directory.Exists(tempDiagnostic))
                System.IO.Directory.Delete(tempDiagnostic, true);
        }
        Console.WriteLine("PASS: " + passed + " gameplay policy checks");
    }
}
