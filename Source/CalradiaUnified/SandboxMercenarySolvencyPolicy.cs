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

        /// <summary>
        /// Accepting below costs is a deliberate player risk. The user
        /// must choose Accept AND confirm the displayed daily loss.
        /// Neither a default selection nor stale toggle is consent.
        /// Employer-funded advance and exit security stay mandatory.
        /// This authorizes a PURE choice, not a native kingdom action.
        /// </summary>
        public static string AcceptanceDecision(int dailyCost,
            int offeredDailyPay,int fundedAdvance,int fundedExitBond,
            int preferredMarginPercent,bool playerClickedAccept,
            bool explicitLossAcknowledgment)
        {
            int minPay=RequiredGuaranteedDailyPay(
                dailyCost,preferredMarginPercent);
            int advance=PayrollAdvance(dailyCost);
            int bond=ExitBond(dailyCost);
            if(dailyCost<=0 || offeredDailyPay<0 ||
                fundedAdvance<0 || fundedExitBond<0 ||
                minPay<0 || advance<0 || bond<0)
                return "invalid_financial_quote";
            if(!playerClickedAccept)return "declined";
            if(fundedAdvance<advance || fundedExitBond<bond)
                return "refused_unfunded_security";
            if(offeredDailyPay<dailyCost)
                return explicitLossAcknowledgment?
                    "player_confirmed_loss_making_offer":
                    "explicit_loss_confirmation_required";
            if(offeredDailyPay<minPay)
                return "accepted_below_preferred_margin";
            return "accepted_financially_eligible_offer";
        }

        // Sum for a warning only. No automatic debit or deficit subsidy.
        public static long ProjectedDeficit(int dailyCost,
            int offeredDailyPay,int proposedDays)
        {
            if(dailyCost<0 || offeredDailyPay<0 || proposedDays<0)
                return -1L;
            return Math.Max(0L,(long)dailyCost-offeredDailyPay)*
                proposedDays;
        }

        /// <summary>
        /// One active contract covers a specified number of regulars and
        /// a native wage ceiling. Upgrades can raise wages without raising
        /// headcount. A revision must be OFFERED when either limit changes.
        /// </summary>
        public static string CoverageReview(int coveredHeadcount,
            int coveredDailyWage,int currentHeadcount,
            int actualNativeDailyWage)
        {
            if(coveredHeadcount<0 || coveredDailyWage<0 ||
                currentHeadcount<0 || actualNativeDailyWage<0)
                return "invalid_coverage";
            bool tooMany=currentHeadcount>coveredHeadcount;
            bool moreWages=actualNativeDailyWage>coveredDailyWage;
            if(tooMany && moreWages)
                return "renegotiate_headcount_and_wages";
            if(tooMany)return "renegotiate_headcount";
            if(moreWages)return "renegotiate_wages";
            return "within_agreed_coverage";
        }

        // No unilateral, silent changes to a signed contract. Native
        // financing and acceptance still require independent evidence.
        public static string RevisionDecision(bool coverageExceeded,
            bool playerAccepted,bool employerAccepted,
            bool additionalEscrowFunded)
        {
            if(!coverageExceeded)return "no_revision_required";
            if(!playerAccepted || !employerAccepted)
                return "renegotiation_pending_both_consents";
            if(!additionalEscrowFunded)
                return "revision_refused_unfunded";
            return "revision_ready_for_native_commit";
        }

        /// <summary>
        /// An exit bond is inaccessible company security until a legitimate
        /// termination. Native escrow ownership and exact once-only gold
        /// movements still require a saved campaign transaction ledger.
        /// </summary>
        public static string ExitBondReleaseDecision(bool realEscrowFunded,
            bool completedNormally,bool employerDismissedWithoutCause,
            bool playerBrokeContract,bool alreadyPaid)
        {
            if(alreadyPaid)return "already_released_no_repeat";
            if(!realEscrowFunded)return "no_verified_escrow";
            if(playerBrokeContract)
                return "forfeit_unearned_bond";
            if(completedNormally && employerDismissedWithoutCause)
                return "invalid_termination_state";
            if(!completedNormally && !employerDismissedWithoutCause)
                return "protected_not_spendable";
            return "eligible_once_on_native_exit";
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
