namespace CalradiaUnified
{
    /// <summary>
    /// Economic station is a description of REAL Bannerlord systems,
    /// never a scheduled invented income, salary, upkeep or promotion.
    /// Player denar changes must be sampled from native Hero.Gold.
    /// </summary>
    public static class SandboxRoleEconomyPolicy
    {
        public static string IncomeAuthority(string role)
        {
            switch(role)
            {
                case "King":return "NativeKingdomAndFiefEconomy";
                case "Lord":
                case "Knight":return "NativeFiefAndClanEconomy";
                case "Soldier":return "NativeMercenaryContract";
                case "Admiral":return "NativeFleetAndKingdomEconomy";
                case "Trader":return "NativeTradeTransactions";
                case "Outlaw":return "NativeLootAndCommerce";
                case "Recruit":return "NoWageUntilVerifiedEnlistment";
                case "HouseholdKnight":return "PatronContactNoNativeStipend";
                case "NobleCadet":return "BirthKinNoAutomaticEstateIncome";
                case "Beggar":return "NoGuaranteedIncome";
                case "Wanderer":return "NativeIndependentActivity";
                default:return "UnrecognizedRole";
            }
        }
        public static string HouseholdDuty(string role)
        {
            switch(role)
            {
                case "King":return "RuleAndDefendNativeRealm";
                case "Lord":return "MaintainClanAndNativeFiefs";
                case "Knight":return "NativeVassalObligations";
                case "Soldier":return "NativeMercenaryService";
                case "Admiral":return "NativeNavalCommand";
                case "Trader":return "NativeCommerceAndClanUpkeep";
                case "Outlaw":return "NativeBandSurvival";
                case "Recruit":return "SeekVerifiedEnlistment";
                case "HouseholdKnight":return "SeekRealPatronService";
                case "NobleCadet":return "PreserveRecordedBirthRelations";
                case "Beggar":return "MeetDailyNeedsNoGuaranteedPatron";
                case "Wanderer":return "IndependentClanSurvival";
                default:return "UnrecognizedRole";
            }
        }
        public static string CareerEvidence(string role,
            bool verifiedEnlisted,bool nativeVassal,
            bool nativeMercenary,bool nativeKingdom)
        {
            switch(role)
            {
                case "Recruit":
                    return verifiedEnlisted?"ExternalEnlistmentVerified":
                        "EnlistmentNotVerified";
                case "HouseholdKnight":
                    return verifiedEnlisted?"ExternalServiceVerified":
                        "PersonalPatronNotFormalEmployment";
                case "Soldier":
                    return nativeMercenary && nativeKingdom?
                        "NativeMercenaryVerified":"MercenaryUnverified";
                case "King":
                case "Lord":
                case "Knight":
                    return nativeVassal && nativeKingdom?
                        "NativeNobleServiceVerified":"NobleServiceUnverified";
                case "Admiral":
                    return nativeKingdom?"NativeFleetContextVerified":
                        "FleetAuthorityUnverified";
                case "NobleCadet":
                    return "BirthRankNotAutomaticEmployment";
                case "Trader":
                case "Outlaw":
                case "Beggar":
                case "Wanderer":
                    return "ActivityNotAnAutomaticWage";
                default:return "UnrecognizedRole";
            }
        }
    }
}
