using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Independent, deterministic strategy advisor. This class NEVER issues orders.
    /// The v0.7 runtime still uses the narrower existing clan-resupply controller.
    /// Decisions cannot be used for commands before game-side threat/reachability checks.
    /// </summary>
    public static class KingdomStrategyPolicy
    {
        public enum Priority
        {
            Hold, Defend, Recover, Resupply, Recruit, SiegeSupport, Patrol
        }

        public static Priority Choose(bool eligible, bool inBattle, bool inArmy,
            bool besieging, bool settlementThreatened, bool kingdomAtWar,
            int foodDays, int lowFoodThreshold, int soldiers, int minimumSoldiers,
            bool requestedSiegeSupport, bool patrolAreaIsSafe)
        {
            if (!eligible || inBattle || inArmy || besieging || soldiers <= 0)
                return Priority.Hold;
            if (settlementThreatened) return Priority.Defend;
            if (foodDays >= 0 && lowFoodThreshold >= 0 && foodDays <= lowFoodThreshold)
                return Priority.Resupply;
            if (soldiers < Math.Max(1, minimumSoldiers))
                return kingdomAtWar ? Priority.Recover : Priority.Recruit;
            if (kingdomAtWar && requestedSiegeSupport) return Priority.SiegeSupport;
            return patrolAreaIsSafe ? Priority.Patrol : Priority.Hold;
        }

        /// <summary>
        /// Fail closed for defended territory. Do not send solo parties into hostile
        /// settlements, active sieges, unknown approach paths, or wounded armies.
        /// </summary>
        public static bool EligibleDefenseTarget(bool friendlyOwner, bool underSiege,
            bool routeKnownSafe, bool troopsReady, bool hasHostileBattle)
        {
            return friendlyOwner && !underSiege && routeKnownSafe && troopsReady
                && !hasHostileBattle;
        }
    }
}
