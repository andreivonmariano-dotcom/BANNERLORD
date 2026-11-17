using System;
namespace CalradiaUnified
{
    /// <summary>Pure opt-in political and military eligibility rules.</summary>
    public static class KingdomPolicy
    {
        public static bool CanLevy(bool enabled, bool sameKingdom, bool playerHasKingdom,
            bool hasTroop, int gold, int influence, int troopCount, int partyCap,
            int recruitedToday, int dailyCap, int goldCost, int influenceCost)
        {
            return enabled && playerHasKingdom && sameKingdom && hasTroop
                && gold >= goldCost && influence >= influenceCost
                && troopCount < partyCap && recruitedToday < dailyCap
                && goldCost > 0 && influenceCost > 0;
        }
        public static bool CanBatchLevy(bool enabled, bool sameKingdom, bool playerHasKingdom,
            bool hasTroop, int gold, int influence, int troopCount, int partyCap,
            int recruitedToday, int dailyCap, int goldCost, int influenceCost, int amount)
        {
            if (amount <= 0 || amount > 10 || goldCost <= 0 || influenceCost <= 0) return false;
            if ((long)goldCost * amount > Int32.MaxValue || (long)influenceCost * amount > Int32.MaxValue)
                return false;
            return CanLevy(enabled, sameKingdom, playerHasKingdom, hasTroop, gold, influence,
                troopCount, partyCap, recruitedToday, dailyCap, goldCost, influenceCost)
                && (long)gold >= (long)goldCost * amount
                && (long)influence >= (long)influenceCost * amount
                && (long)troopCount + amount <= partyCap
                && (long)recruitedToday + amount <= dailyCap;
        }
        public static bool CanMission(bool enabled, bool sameKingdom, bool playerHasKingdom,
            bool hasOtherLeader, int gold, int influence, int cooldownDays,
            int goldCost, int influenceCost)
        {
            return enabled && sameKingdom && playerHasKingdom && hasOtherLeader
                && gold >= goldCost && influence >= influenceCost && cooldownDays <= 0
                && goldCost > 0 && influenceCost > 0;
        }
    }
}
