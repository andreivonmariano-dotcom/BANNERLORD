using System;

namespace CalradiaUnified
{
    /// <summary>
    /// v0.70e PURE budget / offer math. It grants no money, creates no
    /// troops, does not intercept a mercenary offer, and does not change
    /// Bannerlord contracts. Native gameplay integration is a separate
    /// implementation and requires actual v1.5.7 acceptance logs.
    /// </summary>
    public static class SandboxMercenarySolvencyPolicy
    {
        public const int StartingRunwayDays=30;
        public const int StartingContingencyPercent=10;
        public const int InitialFoodDays=14;
        public const int PayrollAdvanceDays=7;
        public const int ExitBondDays=21;
        public const int DefaultMarginPercent=25;
        public const int MaximumMarginPercent=100;

        public static int DailyOperatingCost(int nativePartyWage,
            int estimatedFoodReplenishment,int upkeepAndRecovery)
        {
            if(nativePartyWage<0 || estimatedFoodReplenishment<0 ||
                upkeepAndRecovery<0)return -1;
            long cost=(long)nativePartyWage+
                estimatedFoodReplenishment+upkeepAndRecovery;
            return cost>Int32.MaxValue?-1:(int)cost;
        }

        public static int RequiredGuaranteedDailyPay(int dailyCost,
            int marginPercent)
        {
            if(dailyCost<0 || marginPercent<0 ||
                marginPercent>MaximumMarginPercent)return -1;
            long gross=(long)dailyCost*(100+marginPercent);
            long rounded=(gross+99L)/100L;
            return rounded>Int32.MaxValue?-1:(int)rounded;
        }

        public static int StartingTreasuryTarget(int dailyCost)
        {
            if(dailyCost<0)return -1;
            long gross=(long)dailyCost*StartingRunwayDays*
                (100+StartingContingencyPercent);
            long rounded=(gross+99L)/100L;
            return rounded>Int32.MaxValue?-1:(int)rounded;
        }

        public static int StartingTopUp(int nativeGold,int dailyCost)
        {
            if(nativeGold<0)return -1;
            int target=StartingTreasuryTarget(dailyCost);
            if(target<0)return -1;
            return Math.Max(0,target-nativeGold);
        }

        public static int PayrollAdvance(int dailyCost)
        {
            if(dailyCost<0)return -1;
            long amount=(long)dailyCost*PayrollAdvanceDays;
            return amount>Int32.MaxValue?-1:(int)amount;
        }

        public static int ExitBond(int dailyCost)
        {
            if(dailyCost<0)return -1;
            long amount=(long)dailyCost*ExitBondDays;
            return amount>Int32.MaxValue?-1:(int)amount;
        }

        public static string OfferStatus(int dailyCost,
            int guaranteedDailyPay,int fundedPayrollAdvance,
            int fundedExitBond,int requestedMarginPercent)
        {
            int minPay=RequiredGuaranteedDailyPay(
                dailyCost,requestedMarginPercent);
            int advance=PayrollAdvance(dailyCost);
            int exit=ExitBond(dailyCost);
            if(dailyCost<=0 || guaranteedDailyPay<0 ||
                fundedPayrollAdvance<0 || fundedExitBond<0 ||
                minPay<0 || advance<0 || exit<0)
                return "invalid_financial_quote";
            if(guaranteedDailyPay<dailyCost)
                return "loss_making";
            if(guaranteedDailyPay<minPay)
                return "below_requested_margin";
            if(fundedPayrollAdvance<advance)
                return "payroll_advance_not_funded";
            if(fundedExitBond<exit)
                return "exit_bond_not_funded";
            return "contract_financially_eligible";
        }

        // A ratio from guaranteed contract cash alone. Loot, influence
        // and unverified native mercenary payment never count as guaranteed.
        public static int GuaranteedCoveragePercent(int dailyCost,
            int guaranteedDailyPay)
        {
            if(dailyCost<=0 || guaranteedDailyPay<0)return -1;
            long result=(long)guaranteedDailyPay*100L/dailyCost;
            return result>Int32.MaxValue?Int32.MaxValue:(int)result;
        }
    }
}
