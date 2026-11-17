using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class CivicBehavior
    {
        private void RegisterDepotTradeMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_purchase_depot_grain", "CU: Purchase 5 grain from your depot",
                x => CanVisit() && CanPurchaseDepotGrain(), x => Safe(PurchaseDepotGrain), false);
        }

        private bool CanPurchaseDepotGrain()
        {
            var depot = CurrentDepot();
            var party = MobileParty.MainParty;
            return depot != null && TradePolicy.CanPurchase(settings.EnableDepotTrade,
                Owned(), party != null && party.ItemRoster != null && DefaultItems.Grain != null,
                depot.Grain, 5, PlayerGold(), TradePolicy.UnitPrice(depot.Grain, settings.DepotCapacity));
        }

        private void PurchaseDepotGrain()
        {
            if (!CanPurchaseDepotGrain()) return;
            var depot = CurrentDepot();
            if (depot == null || Hero.MainHero == null || MobileParty.MainParty == null) return;
            int price = TradePolicy.UnitPrice(depot.Grain, settings.DepotCapacity);
            int total = TradePolicy.BuyCost(5, price);
            if (total <= 0 || PlayerGold() < total || depot.Grain < 5) return;
            // Gold is spent, not minted by a mock merchant. Source stock is deducted exactly once.
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -total, true);
            depot.Grain -= 5;
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 5);
            Notice("Purchased 5 depot grain for " + total + " gold. Stock: " + depot.Grain);
        }
    }
}
