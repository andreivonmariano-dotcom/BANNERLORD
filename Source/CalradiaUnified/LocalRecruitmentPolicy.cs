namespace CalradiaUnified
{
    /// <summary>Bounded daily local recruitment, independent of kingdom levies.</summary>
    public static class LocalRecruitmentPolicy
    {
        public static bool CanRecruit(bool enabled, bool playerOwned, bool hasBasicTroop,
            int playerGold, int goldCost, int partyTroops, int partyCap,
            int recruitedToday, int dailyCap)
        {
            return enabled && playerOwned && hasBasicTroop && goldCost > 0
                && playerGold >= goldCost && partyTroops >= 0 && partyCap > 0
                && partyTroops < partyCap && dailyCap > 0 && recruitedToday >= 0
                && recruitedToday < dailyCap;
        }
    }
}
