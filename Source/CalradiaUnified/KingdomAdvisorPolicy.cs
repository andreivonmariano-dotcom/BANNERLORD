namespace CalradiaUnified
{
    /// <summary>Pure NPC kingdom-planning recommendations. Never directly issues movement or diplomacy orders.</summary>
    public static class KingdomAdvisorPolicy
    {
        public enum Priority { Hold, Defend, Resupply, Recruit, Regroup, Patrol }

        public static Priority Decide(bool activeCombat, bool besieged, bool inArmy,
            bool atSettlement, int daysOfFood, int members, int preferredMembers, bool hasEnemyThreat)
        {
            if (activeCombat) return Priority.Hold;
            if (besieged || hasEnemyThreat) return Priority.Defend;
            if (daysOfFood >= 0 && daysOfFood <= 2) return Priority.Resupply;
            if (inArmy) return Priority.Regroup;
            if (members >= 0 && members < preferredMembers && atSettlement) return Priority.Recruit;
            return Priority.Patrol;
        }

        public static bool ShouldReport(bool enabled, bool playerClan, bool lordParty, int alreadyReported, int dailyLimit)
        {
            return enabled && playerClan && lordParty && alreadyReported < dailyLimit && dailyLimit > 0;
        }
    }
}
