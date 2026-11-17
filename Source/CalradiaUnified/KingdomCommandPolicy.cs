using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Maps strategic intentions to a small allow-list of settlement commands.
    /// It does not interact with the game API. Defense and siege priorities stay
    /// advisory-only: moving a solo party toward combat is deliberately excluded.
    /// </summary>
    public static class KingdomCommandPolicy
    {
        public enum TravelOrder { Hold, Resupply, Recruit, Patrol }

        public static TravelOrder Choose(KingdomStrategyPolicy.Priority priority,
            bool ownClanLord, bool ownerAlive, bool active, bool playerControlled,
            bool inBattle, bool inArmy, bool inSiege, bool inSettlement,
            bool moving, bool cooldownComplete, bool hasFriendlyDestination,
            bool peaceConfirmed, bool safePatrolTarget,
            bool allowResupply, bool allowRecruit, bool allowPatrol)
        {
            if (!ownClanLord || !ownerAlive || !active || playerControlled ||
                inBattle || inArmy || inSiege || inSettlement || !cooldownComplete ||
                !hasFriendlyDestination) return TravelOrder.Hold;

            switch (priority)
            {
                case KingdomStrategyPolicy.Priority.Resupply:
                    return allowResupply ? TravelOrder.Resupply : TravelOrder.Hold;
                case KingdomStrategyPolicy.Priority.Recruit:
                case KingdomStrategyPolicy.Priority.Recover:
                    return allowRecruit ? TravelOrder.Recruit : TravelOrder.Hold;
                case KingdomStrategyPolicy.Priority.Patrol:
                    return allowPatrol && peaceConfirmed && safePatrolTarget && !moving
                        ? TravelOrder.Patrol : TravelOrder.Hold;
                default:
                    // No unverified combat/defense/siege AI orders.
                    return TravelOrder.Hold;
            }
        }
    }
}
