namespace CalradiaUnified
{
    /// <summary>
    /// Policy for creating native-culture recruits in an existing player-owned
    /// garrison. The campaign adapter consumes actual gold and recorded depot grain.
    /// </summary>
    public static class GarrisonTrainingPolicy
    {
        public static bool CanTrain(bool enabled, bool playerOwned, bool underSiege,
            bool garrisonExists, bool basicTroopExists, int playerGold, int goldCost,
            int depotGrain, int grainCost, int garrisonTroops, int garrisonCap,
            int trainedToday, int dailyCap, int batch)
        {
            return enabled && playerOwned && !underSiege && garrisonExists &&
                basicTroopExists && goldCost > 0 && grainCost > 0 && batch > 0 &&
                playerGold >= goldCost && depotGrain >= grainCost &&
                garrisonTroops >= 0 && garrisonCap >= batch &&
                garrisonTroops <= garrisonCap - batch && trainedToday >= 0 &&
                dailyCap >= batch && trainedToday <= dailyCap - batch;
        }
    }
}
