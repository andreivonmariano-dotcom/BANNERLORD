using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure, game-assembly-independent acceptance rules for new Sandbox starts.
    /// This does not grant anything. It tests the real observed state after
    /// Bannerlord has applied Advanced Start (phase 8) and CU family (phase 9).
    /// Every nonempty result means a visible choice failed in the actual game.
    /// </summary>
    public static class SandboxStartContract
    {
        /// <summary>
        /// The irreversible native phase-8 start cannot be undone here.
        /// Refuse all ADDITIONAL CU grants if family or lineage creation
        /// did not produce its verified game-backed result.
        /// </summary>
        public static string PostFamilyGrantGate(
            bool familyRequested,string familyResult,
            bool exactBirthRankValid,bool nobleLineageRequested,
            string nativeLineageResult)
        {
            if(familyRequested &&
                (familyResult==null ||
                 !familyResult.StartsWith("spouse=",
                     StringComparison.Ordinal)))
                return "family_creation_incomplete";
            if(!exactBirthRankValid)
                return "noble_birth_rank_changed_after_house";
            if(nobleLineageRequested &&
                (nativeLineageResult==null ||
                 !nativeLineageResult.StartsWith("bound:",
                     StringComparison.Ordinal)))
                return "native_noble_lineage_not_bound";
            return "";
        }

        public static string[] Validate(
            string desiredNativeStartType,
            string actualNativeStartType,
            string desiredKingdomId,
            string actualKingdomId,
            bool isRulingClan,
            int actualFiefs,
            bool spouseRequested,
            bool hasSpouse,
            int requestedChildren,
            int actualChildren)
        {
            var errors = new List<string>();
            if (String.IsNullOrEmpty(desiredNativeStartType))
                errors.Add("unknown_requested_start_type");
            else if (!String.Equals(desiredNativeStartType,
                actualNativeStartType, StringComparison.Ordinal))
                errors.Add("native_start_type_mismatch");

            bool requiresKingdom = desiredNativeStartType == "vassal" ||
                desiredNativeStartType == "king" ||
                desiredNativeStartType == "mercenary" ||
                desiredNativeStartType == "fleetadmiral";

            if (requiresKingdom)
            {
                if (String.IsNullOrEmpty(desiredKingdomId))
                    errors.Add("starting_kingdom_not_selected");
                else if (!String.Equals(desiredKingdomId, actualKingdomId,
                    StringComparison.Ordinal))
                    errors.Add("starting_kingdom_mismatch");
            }

            if (desiredNativeStartType == "king" &&
                (!isRulingClan || actualFiefs < 1))
                errors.Add("ruler_status_or_fief_missing");
            if (desiredNativeStartType == "vassal" &&
                (isRulingClan || actualFiefs < 1))
                errors.Add("vassal_status_or_fief_missing");

            if (spouseRequested && !hasSpouse)
                errors.Add("requested_spouse_missing");
            if (requestedChildren > actualChildren)
                errors.Add("requested_children_missing");

            return errors.ToArray();
        }
    }
}
