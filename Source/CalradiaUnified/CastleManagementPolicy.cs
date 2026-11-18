using System;
namespace CalradiaUnified
{
    /// <summary>Read-only modeled affordability and safe manual roster rules.</summary>
    public static class CastleManagementPolicy
    {
        public const int TransferBatch = 5;
        public static int RequiredIncome(int fieldWages, int garrisonWages,
            int otherReserve, int surplus)
        {
            if (fieldWages < 0 || garrisonWages < 0 ||
                otherReserve < 0 || surplus < 0) return 0;
            return (int)Math.Min(Int32.MaxValue,
                (long)fieldWages + garrisonWages + otherReserve + surplus);
        }
        public static int ModeledShortfall(int forecast, int required)
        {
            if (forecast < 0 || required < 0) return 0;
            return (int)Math.Min(Int32.MaxValue,
                Math.Max(0L, (long)required - forecast));
        }
        public static bool CanMove(bool enabled, bool owned, bool castle,
            bool siege, bool rostersExist, bool troopExists, int source,
            int destination, int destinationCap, int batch)
        {
            return enabled && owned && castle && !siege && rostersExist &&
                troopExists && batch > 0 && source >= batch &&
                destination >= 0 && destinationCap >= batch &&
                destination <= destinationCap - batch;
        }
        public static bool CanApplyCap(bool enabled, bool owned, bool castle,
            bool siege, int oldCap, int proposed, int defensiveFloor,
            int actualWages)
        {
            return FeudalBudgetPolicy.MayLowerWageCap(enabled, owned, castle,
                siege, oldCap, proposed, defensiveFloor) &&
                actualWages >= 0 && actualWages <= proposed;
        }
    }
}
