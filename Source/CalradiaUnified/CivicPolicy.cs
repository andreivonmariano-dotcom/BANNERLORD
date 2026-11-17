using System;

namespace CalradiaUnified
{
    /// <summary>Pure rules for player-owned settlements, deposits, and scheduled shipments.</summary>
    public static class CivicPolicy
    {
        public static bool CanDeposit(bool enabled, bool owned, int grainOnHand, int batch)
        {
            return enabled && owned && batch > 0 && grainOnHand >= batch;
        }
        public static bool CanWithdraw(bool enabled, bool owned, int stock, int batch)
        {
            return enabled && owned && batch > 0 && stock >= batch;
        }
        public static bool CanInvest(bool enabled, bool owned, int gold, int cost, int investmentLevel, int cap)
        {
            return enabled && owned && gold >= cost && cost > 0 && investmentLevel < cap;
        }
        public static bool CanRecruit(bool enabled, bool owned, int gold, int cost, int troopCount, int partyLimit)
        {
            return enabled && owned && gold >= cost && cost > 0 && troopCount < partyLimit;
        }
        public static bool CanAid(bool enabled, bool owned, int grainOnHand, int grainCost, bool hasNotable)
        {
            return enabled && owned && hasNotable && grainCost > 0 && grainOnHand >= grainCost;
        }
        public static bool CanSendShipment(bool enabled, bool owned, int stock, int cargo, bool hasDestination,
            bool shipmentPending)
        {
            return enabled && owned && cargo > 0 && stock >= cargo && hasDestination && !shipmentPending;
        }
        public static int DailyDepotOutput(int investmentLevel, int currentStock, int capacity)
        {
            if (investmentLevel <= 0 || currentStock >= capacity) return 0;
            return Math.Min(investmentLevel, Math.Max(0, capacity - currentStock));
        }
        public static bool SafeToAdviseNpc(bool enabled, bool playerClan, bool lordParty,
            bool mapEvent, bool inArmy, bool inSettlement, int foodDays, int threshold)
        {
            return enabled && playerClan && lordParty && !mapEvent && !inArmy && !inSettlement
                && SupplyPolicy.IsLowFood(foodDays, threshold);
        }
    }
}
