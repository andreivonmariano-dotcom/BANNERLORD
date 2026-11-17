using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Original player-initiated economic and civic systems. No Harmony, no foreign mod DLLs.
    /// "Shipments" are abstracted timed transfers between depots, not visible MobileParty actors.
    /// </summary>
    public sealed partial class CivicBehavior : CampaignBehaviorBase
    {
        private readonly Settings settings;
        private CivicState state = new CivicState();
        private int advisorCount;
        public CivicBehavior(Settings settings) { this.settings = settings; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnPartyTick);
        }
        public override void SyncData(IDataStore store)
        {
            string packed = state.Serialize();
            store.SyncData("CU_v030_civic_state", ref packed);
            state = CivicState.Parse(packed);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (starter == null) return;
            foreach (var menu in new[] { "town", "castle" })
            {
                starter.AddGameMenuOption(menu, "cu_depot_deposit", "CU: Deposit 5 grain in depot",
                    x => CanVisit() && CivicPolicy.CanDeposit(settings.EnableCivicSystems, Owned(), GrainOnHand(), 5),
                    x => Safe(Deposit), false);
                starter.AddGameMenuOption(menu, "cu_depot_withdraw", "CU: Withdraw 5 grain from depot",
                    x => CanVisit() && CivicPolicy.CanWithdraw(settings.EnableCivicSystems, Owned(), Stock(), 5),
                    x => Safe(Withdraw), false);
                starter.AddGameMenuOption(menu, "cu_depot_invest", "CU: Fund grain production (300 gold)",
                    x => CanVisit() && CivicPolicy.CanInvest(settings.EnableEconomy, Owned(), PlayerGold(), 300,
                        CurrentDepot() == null ? 0 : CurrentDepot().Investment, settings.MaxDepotInvestment),
                    x => Safe(Invest), false);
                starter.AddGameMenuOption(menu, "cu_depot_ship", "CU: Send 5 grain to lowest-stock fief (3 days)",
                    x => CanVisit() && CivicPolicy.CanSendShipment(settings.EnableAbstractShipments, Owned(), Stock(), 5,
                        Destination() != null, state.HasShipment(CurrentId())),
                    x => Safe(Ship), false);
                starter.AddGameMenuOption(menu, "cu_local_levy", "CU: Hire one local levy (200 gold)",
                    x => CanVisit() && CivicPolicy.CanRecruit(settings.EnableRecruitment, Owned(), PlayerGold(), 200,
                        MobileParty.MainParty == null ? 0 : MobileParty.MainParty.MemberRoster.TotalManCount,
                        settings.RecruitmentMaxPartySize) && LocalRecruit() != null,
                    x => Safe(Recruit), false);
                starter.AddGameMenuOption(menu, "cu_grain_aid", "CU: Donate 5 grain for local goodwill",
                    x => CanVisit() && CivicPolicy.CanAid(settings.EnableLocalDiplomacy, Owned(), GrainOnHand(), 5,
                        LocalNotable() != null),
                    x => Safe(Aid), false);
                RegisterPhysicalConvoyMenu(starter, menu);
                RegisterKingdomMenu(starter, menu);
                RegisterDepotTradeMenu(starter, menu);
            }
        }
        private static bool CanVisit()
        {
            return Settlement.CurrentSettlement != null && MobileParty.MainParty != null;
        }
        private static bool Owned()
        {
            return CanVisit() && Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan;
        }
        private static string CurrentId()
        {
            return CanVisit() ? Settlement.CurrentSettlement.StringId : null;
        }
        private CivicState.Depot CurrentDepot()
        {
            return CivicState.Valid(CurrentId()) ? state.Get(CurrentId()) : null;
        }
        private int Stock()
        {
            return CurrentDepot() == null ? 0 : CurrentDepot().Grain;
        }
        private static int GrainOnHand()
        {
            return MobileParty.MainParty != null && MobileParty.MainParty.ItemRoster != null
                && DefaultItems.Grain != null ? MobileParty.MainParty.ItemRoster.GetItemNumber(DefaultItems.Grain) : 0;
        }
        private static int PlayerGold() { return Hero.MainHero == null ? 0 : Hero.MainHero.Gold; }
        private static Hero LocalNotable()
        {
            var s = Settlement.CurrentSettlement;
            return s == null ? null : s.Notables.FirstOrDefault(x => x != null && x.IsAlive);
        }
        private static CharacterObject LocalRecruit()
        {
            var s = Settlement.CurrentSettlement;
            return s == null || s.Culture == null ? null : s.Culture.BasicTroop;
        }
        private void Safe(Action body)
        {
            try { body(); }
            catch (Exception e) { Log.Write("Civic action failed: " + e); }
        }
        private static void Notice(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage("[Calradia Unified] " + message));
        }
        private void Deposit()
        {
            if (!Owned() || GrainOnHand() < 5 || !settings.EnableCivicSystems) return;
            var d = CurrentDepot(); if (d == null || d.Grain > 99995) return;
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, -5);
            d.Grain += 5;
            Notice("Deposited 5 grain. Depot stock: " + d.Grain);
        }
        private void Withdraw()
        {
            var d = CurrentDepot();
            if (!Owned() || d == null || d.Grain < 5 || !settings.EnableCivicSystems) return;
            // Vanilla grain stack, no custom items or textures.
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 5);
            d.Grain -= 5;
            Notice("Withdrew 5 grain. Depot stock: " + d.Grain);
        }
        private void Invest()
        {
            var d = CurrentDepot();
            if (!Owned() || d == null || !CivicPolicy.CanInvest(settings.EnableEconomy, true,
                PlayerGold(), 300, d.Investment, settings.MaxDepotInvestment)) return;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -300, true);
            d.Investment++;
            Notice("Production level " + d.Investment + ". Depot gains grain each day up to stock limit.");
        }
        private Settlement Destination()
        {
            var current = Settlement.CurrentSettlement;
            if (current == null || Clan.PlayerClan == null) return null;
            return Settlement.All.Where(s => s != null && s != current && CivicState.Valid(s.StringId)
                && s.OwnerClan == Clan.PlayerClan && (s.IsTown || s.IsCastle))
                .OrderBy(s => state.Stock(s.StringId)).ThenBy(s => s.StringId).FirstOrDefault();
        }
        private void Ship()
        {
            if (!Owned() || !settings.EnableAbstractShipments) return;
            var source = CurrentDepot(); var target = Destination();
            if (source == null || target == null || source.Grain < 5 || state.HasShipment(source.SettlementId)) return;
            source.Grain -= 5;
            state.Shipments.Add(new CivicState.Shipment { From = source.SettlementId, To = target.StringId,
                Grain = 5, DaysRemaining = 3 });
            Notice("Scheduled 5 grain to " + target.Name + ". Delivery in 3 days (abstract shipment).");
        }
        private void Recruit()
        {
            var troop = LocalRecruit(); var p = MobileParty.MainParty;
            if (!Owned() || troop == null || p == null || !CivicPolicy.CanRecruit(settings.EnableRecruitment, true,
                PlayerGold(), 200, p.MemberRoster.TotalManCount, settings.RecruitmentMaxPartySize)) return;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -200, true);
            p.MemberRoster.AddToCounts(troop, 1);
            Notice("Recruited one local levy.");
        }
        private void Aid()
        {
            var notable = LocalNotable();
            if (!Owned() || !settings.EnableLocalDiplomacy || notable == null || GrainOnHand() < 5) return;
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, -5);
            ChangeRelationAction.ApplyPlayerRelation(notable, 1, true);
            Notice("Gave 5 grain to local families. Relations improved with a notable.");
        }
        private void DailyTick()
        {
            try
            {
                foreach (var d in state.Depots)
                {
                    var s = Settlement.All.FirstOrDefault(x => x != null && x.StringId == d.SettlementId);
                    if (s != null && s.OwnerClan == Clan.PlayerClan)
                        d.Grain += CivicPolicy.DailyDepotOutput(d.Investment, d.Grain, settings.DepotCapacity);
                }
                foreach (var shipment in state.Shipments.ToArray())
                {
                    if (shipment.Physical) continue; // Physical convoys deliver on arrival only.
                    shipment.DaysRemaining--;
                    if (shipment.DaysRemaining > 0) continue;
                    var target = Settlement.All.FirstOrDefault(s => s != null && s.StringId == shipment.To);
                    var source = Settlement.All.FirstOrDefault(s => s != null && s.StringId == shipment.From);
                    // Ownership loss cancels the shipment; refund only if source still belongs to player.
                    if (target != null && target.OwnerClan == Clan.PlayerClan &&
                        CivicState.Valid(shipment.To) && state.Get(shipment.To) != null)
                    {
                        state.Get(shipment.To).Grain += shipment.Grain;
                        Notice("Shipment delivered: " + shipment.Grain + " grain to " + target.Name);
                    }
                    else if (source != null && source.OwnerClan == Clan.PlayerClan && state.Get(shipment.From) != null)
                    {
                        state.Get(shipment.From).Grain += shipment.Grain;
                        Notice("Shipment returned after destination ownership changed.");
                    }
                    state.Shipments.Remove(shipment);
                }
                TickPhysicalConvoys();
                TickKingdomCouncil();
                advisorCount = 0;
            }
            catch (Exception e) { Log.Write("Civic daily tick error: " + e); }
        }
        private void OnPartyTick(MobileParty party)
        {
            if (party == null || !settings.EnableNpcAdvisory || advisorCount >= 3) return;
            try
            {
                bool own = party.ActualClan == Clan.PlayerClan;
                bool eventActive = party.MapEvent != null;
                bool hasArmy = party.Army != null;
                bool settlement = party.CurrentSettlement != null;
                int days = party.GetNumDaysForFoodToLast();
                if (KingdomAdvisorPolicy.ShouldReport(settings.EnableNpcAdvisory, own,
                    party.IsLordParty, advisorCount, settings.MaxDailyCouncilReports))
                {
                    var members = party.MemberRoster == null ? 0 : party.MemberRoster.TotalManCount;
                    // Query siege status without binding to a patch-sensitive property at compile time.
                    bool besieged = false;
                    var current = party.CurrentSettlement;
                    if (current != null)
                    {
                        var flag = current.GetType().GetProperty("IsUnderSiege");
                        if (flag != null && flag.PropertyType == typeof(bool))
                            besieged = (bool)flag.GetValue(current, null);
                    }
                    var priority = KingdomAdvisorPolicy.Decide(eventActive, besieged, hasArmy,
                        settlement, days, members, settings.CouncilPreferredPartySize, false);
                    advisorCount++;
                    Log.Write("Kingdom council advisory: party=" + party.StringId
                        + " foodDays=" + days + " members=" + members + " priority=" + priority
                        + ". Advisory only; no movement, war, peace or recruitment order issued.");
                }
            }
            catch (Exception ex) { Log.Write("NPC advisory skipped: " + ex.GetType().Name); }
        }
    }
}
