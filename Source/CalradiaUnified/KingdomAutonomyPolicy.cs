using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Deterministic, side-effect-free guard for issuing NPC supply or recruitment travel orders.
    /// No battle/siege/army commands and no forced troop or food creation.
    /// </summary>
    public static class KingdomAutonomyPolicy
    {
        public enum Order { Hold, Resupply, Recruit, None }

        public static Order Decide(bool enabled, bool ownClan, bool lord, bool leaderAlive,
            bool playerControlled, bool partyActive, bool hasTroops,
            bool inCombat, bool inArmy, bool besieging, bool inSettlement,
            int daysFood, int foodThreshold, int currentTroops, int recruitBelow,
            int currentDay, int nextEligibleDay)
        {
            if (!enabled || !ownClan || !lord || !leaderAlive || playerControlled ||
                !partyActive || !hasTroops || inCombat || inArmy || besieging || inSettlement)
                return Order.Hold;
            if (currentDay < nextEligibleDay) return Order.Hold;
            if (daysFood >= 0 && daysFood <= foodThreshold) return Order.Resupply;
            if (currentTroops > 0 && currentTroops < recruitBelow && daysFood > foodThreshold)
                return Order.Recruit;
            return Order.None;
        }

        public static bool SafeDepot(bool sameKingdom, bool townOrCastle, bool besieged,
            bool foodKnown, float food, float requiredFood, bool validLocation)
        {
            return sameKingdom && townOrCastle && !besieged && validLocation &&
                foodKnown && !Single.IsNaN(food) && !Single.IsInfinity(food) && food >= requiredFood;
        }

        public static int NextOrderDay(int day, int cooldownDays)
        {
            if (day < 0) day = 0;
            if (cooldownDays < 2) cooldownDays = 2;
            return day >= Int32.MaxValue - cooldownDays ? Int32.MaxValue : day + cooldownDays;
        }
    }
}
