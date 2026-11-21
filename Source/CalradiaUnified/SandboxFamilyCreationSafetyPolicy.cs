namespace CalradiaUnified
{
    /// <summary>
    /// Restrict cleanup to a brand-new, uncommitted Hero. Native marriage,
    /// children, party service, settlement residence and established
    /// parentage of the player must never be rolled back by a CU retry.
    /// Game-facing code must still inspect the actual Hero before acting.
    /// </summary>
    public static class SandboxFamilyCreationSafetyPolicy
    {
        public static bool CanDisableProvisional(bool createdThisAttempt,
            bool hasNativeSpouse, bool hasNativeChildren,
            bool inNativeParty, bool nativeCompanion,
            bool inNativeSettlement, bool playerReferencesHero)
        {
            return createdThisAttempt && !hasNativeSpouse &&
                !hasNativeChildren && !inNativeParty &&
                !nativeCompanion && !inNativeSettlement &&
                !playerReferencesHero;
        }

        public static bool MayClearUnmarriedSpouse(bool playerMarried,
            int allocatedChildren, bool otherNativeChildren)
        {
            return !playerMarried && allocatedChildren == 0 &&
                !otherNativeChildren;
        }
    }
}
