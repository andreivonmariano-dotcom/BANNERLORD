namespace CalradiaUnified
{
    /// <summary>
    /// Personal sworn-household standing uses witnessed service,
    /// real lord relation, completed field escorts and native clan
    /// renown. It never grants a kingdom title or fief.
    /// </summary>
    public static class SandboxKnightStandingPolicy
    {
        public static string Standing(int nativeRelation,
            float nativeRenown,int completedOrders,
            int completedFieldEscorts)
        {
            if(completedOrders>=6 && completedFieldEscorts>=2 &&
                nativeRelation>=40 && nativeRenown>=125f)
                return "Veteran retainer";
            if(completedOrders>=3 && completedFieldEscorts>=1 &&
                nativeRelation>=25 && nativeRenown>=50f)
                return "Trusted household knight";
            if(completedOrders>=1 && nativeRelation>=10)
                return "Proven sworn sword";
            return "Newly sworn knight";
        }
        public static bool CanRequestRoyalHearing(
            int nativeRelation,float nativeRenown,
            int completedOrders,int completedFieldEscorts,
            bool patronHasRealKingdom,bool playerIndependent)
        {
            return completedOrders>=3 && completedFieldEscorts>=1 &&
                nativeRelation>=30 && nativeRenown>=75f &&
                patronHasRealKingdom && playerIndependent;
        }
    }
}
