using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Forecast and budget rules, NOT the Bannerlord clan finance calculation.
    /// Estate prosperity and village hearths are deliberately conservative
    /// proxies. No forecast is an observed game tax receipt.
    /// </summary>
    public static class FeudalBudgetPolicy
    {
        public static int EstimateEstateIncome(int castleProsperity,
            int healthyVillageHearths)
        {
            if (castleProsperity < 0 || healthyVillageHearths < 0) return 0;
            long prosperity = Math.Min(10000, castleProsperity);
            long hearths = Math.Min(20000, healthyVillageHearths);
            // Model coefficients are user-tunable balance assumptions,
            // not duplicated native taxes or the exact TaleWorlds tax formula.
            return (int)Math.Min(20000, 100L + 45L * prosperity / 100L
                + 75L * hearths / 100L);
        }

        public static int WageBudget(int forecastIncome, int fieldPartyWages,
            int otherCosts, int targetSurplus, int hardWageCap)
        {
            if (forecastIncome < 0 || fieldPartyWages < 0 ||
                otherCosts < 0 || targetSurplus < 0 || hardWageCap < 0)
                return 0;
            long budget = (long)forecastIncome - fieldPartyWages -
                otherCosts - targetSurplus;
            return (int)Math.Max(0L, Math.Min((long)hardWageCap, budget));
        }

        public static int ProjectedBalance(int income, int fieldWages,
            int garrisonWages, int otherCosts, int tradeProceeds)
        {
            long net = (long)income - fieldWages - garrisonWages -
                otherCosts + tradeProceeds;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, net));
        }

        public static bool MayLowerWageCap(bool enabled, bool owned, bool castle,
            bool siege, int currentLimit, int recommendedLimit,
            int minimumDefensiveWage)
        {
            // This never raises a manually lower wage cap.
            // A current limit of zero is treated as special/unknown and untouched.
            return enabled && owned && castle && !siege && currentLimit > 0 &&
                recommendedLimit >= minimumDefensiveWage &&
                recommendedLimit < currentLimit;
        }

        public static int VillageHarvest(int healthyHearths,
            int freeDepotSpace, int hearthsPerGrain, int maxDaily)
        {
            if (healthyHearths <= 0 || freeDepotSpace <= 0 ||
                hearthsPerGrain <= 0 || maxDaily <= 0) return 0;
            return Math.Max(0, Math.Min(freeDepotSpace,
                Math.Min(maxDaily, healthyHearths / hearthsPerGrain)));
        }

        public static int SurplusSale(int depotStock, int reserveGrain,
            int maxDailySale)
        {
            if (depotStock <= reserveGrain || reserveGrain < 0 ||
                maxDailySale <= 0) return 0;
            return Math.Min(maxDailySale, depotStock - reserveGrain);
        }

        public static bool AffordableTraining(bool enabled, bool owned,
            bool siege, int estimatedIncome, int fieldWages,
            int currentGarrisonWage, int wageLimit, int otherCosts,
            int targetSurplus, int estimatedAddedWage,
            int playerGold, int minimumGoldReserve, int currentTroops,
            int targetTroops)
        {
            return enabled && owned && !siege && wageLimit > 0 &&
                currentTroops >= 0 && targetTroops > currentTroops &&
                estimatedAddedWage > 0 && playerGold >= minimumGoldReserve &&
                currentGarrisonWage >= 0 &&
                (long)currentGarrisonWage + estimatedAddedWage <= wageLimit &&
                ProjectedBalance(estimatedIncome, fieldWages,
                    currentGarrisonWage + estimatedAddedWage, otherCosts, 0)
                    >= targetSurplus;
        }
    }
}
