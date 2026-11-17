using System;

namespace CalradiaUnified
{
    /// <summary>Pure, bounded depot stock and shipment calculations.</summary>
    public static class DepotLedgerPolicy
    {
        public static int FreeCapacity(int currentStock, int capacity)
        {
            if (currentStock < 0 || capacity <= 0) return 0;
            return Math.Max(0, capacity - currentStock);
        }

        public static bool CanDeposit(bool enabled, bool owned, int grainOnHand, int amount,
            int currentStock, int capacity)
        {
            return CivicPolicy.CanDeposit(enabled, owned, grainOnHand, amount)
                && amount <= FreeCapacity(currentStock, capacity);
        }

        public static bool CanWithdraw(bool enabled, bool owned, int currentStock, int amount)
        {
            return CivicPolicy.CanWithdraw(enabled, owned, currentStock, amount);
        }

        public static int Creditable(int amount, int currentStock, int capacity)
        {
            if (amount <= 0) return 0;
            return Math.Min(amount, FreeCapacity(currentStock, capacity));
        }

        /// <summary>Returns the uncredited remainder after a bounded transfer.</summary>
        public static int Remainder(int expected, int credited)
        {
            return Math.Max(0, expected - Math.Max(0, credited));
        }
    }
}
