using System;

namespace CalradiaUnified
{
    /// <summary>Deterministic depot grain purchasing. Only spent player gold, no invented treasury payout.</summary>
    public static class TradePolicy
    {
        public static int UnitPrice(int availableGrain, int depotCapacity)
        {
            if (depotCapacity <= 0) return 30;
            int stock = Math.Max(0, Math.Min(availableGrain, depotCapacity));
            if ((long)stock * 4 <= depotCapacity) return 30;
            if ((long)stock * 2 <= depotCapacity) return 24;
            return 18;
        }

        public static int BuyCost(int amount, int pricePerUnit)
        {
            if (amount <= 0 || amount > 1000 || pricePerUnit <= 0 || pricePerUnit > 1000)
                return -1;
            long total = (long)amount * pricePerUnit;
            return total > Int32.MaxValue ? -1 : (int)total;
        }

        public static bool CanPurchase(bool enabled, bool playerOwnsDepot, bool hasInventory,
            int stock, int amount, int availableGold, int unitPrice)
        {
            int cost = BuyCost(amount, unitPrice);
            return enabled && playerOwnsDepot && hasInventory && stock >= amount
                && amount > 0 && cost > 0 && availableGold >= cost;
        }
    }
}
