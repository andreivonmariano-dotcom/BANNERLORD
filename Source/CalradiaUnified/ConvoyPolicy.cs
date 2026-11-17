using System;

namespace CalradiaUnified
{
    /// <summary>Pure rules tested without the Bannerlord runtime.</summary>
    public static class ConvoyPolicy
    {
        public static bool CanDispatch(bool enabled, bool playerOwnedSource, bool playerOwnedTarget,
            int grain, int cargo, int active, int maxActive, bool alreadyDispatched)
        {
            return enabled && playerOwnedSource && playerOwnedTarget && cargo > 0 && grain >= cargo
                && active >= 0 && active < maxActive && !alreadyDispatched;
        }
        public static bool CanDispatchToDepot(bool enabled, bool playerOwnedSource,
            bool playerOwnedTarget, bool distinctSettlements, bool targetUnderSiege,
            int grain, int cargo, int targetGrain, int targetCapacity,
            int active, int maxActive, bool alreadyDispatched)
        {
            return distinctSettlements && !targetUnderSiege &&
                CanDispatch(enabled, playerOwnedSource, playerOwnedTarget, grain, cargo,
                    active, maxActive, alreadyDispatched)
                && DepotLedgerPolicy.FreeCapacity(targetGrain, targetCapacity) >= cargo;
        }
        public static bool Delivered(bool partyExists, bool active, bool atDestination,
            bool destinationStillOwned)
        {
            return partyExists && active && atDestination && destinationStillOwned;
        }
        public static bool Lost(bool partyExists, bool active, bool hasTroops)
        {
            return !partyExists || !active || !hasTroops;
        }
        public static int DeliverableGrain(int expected, int actualCargo, int freeCapacity)
        {
            return Math.Max(0, Math.Min(expected, Math.Min(actualCargo, freeCapacity)));
        }
        public static bool TimedOut(int daysRemaining) { return daysRemaining <= 0; }
    }
}
