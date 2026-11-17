using System;

namespace CalradiaUnified
{
    /// <summary>Deterministic, unit-testable policy. Contains no TaleWorlds calls.</summary>
    public static class SupplyPolicy
    {
        public static bool IsLowFood(int days, int threshold)
        {
            return days >= 0 && threshold >= 0 && days <= threshold;
        }

        public static bool ShouldForage(bool enabled, int days, int threshold, bool inSettlement,
            bool inArmy, bool isInMapEvent, bool isBesieged, int elapsedDays, int cadence)
        {
            return enabled && IsLowFood(days, threshold) && !inSettlement && !inArmy
                && !isInMapEvent && !isBesieged && elapsedDays >= Math.Max(1, cadence);
        }

        public static string ArmyRisk(int criticalParties, int inspectedParties)
        {
            if (inspectedParties <= 0) return "unknown";
            if (criticalParties <= 0) return "adequate";
            return criticalParties == inspectedParties ? "critical" : "mixed";
        }
    }
}
