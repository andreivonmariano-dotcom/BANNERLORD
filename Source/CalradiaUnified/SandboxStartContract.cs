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

        /// <summary>
        /// Only accept the COMPLETE, structured success receipt emitted by
        /// SandboxCulturalRetinue. A prefix match is not proof of native
        /// companion registration and could release later starting grants.
        /// </summary>
        public static bool RetinueReadyForOtherGrants(
            string mode,string observedResult)
        {
            if(mode=="Native")
                return observedResult=="native_companions_preserved";
            if(mode!="FamilyOnly" && mode!="RoleBased" &&
                mode!="ReplaceNative")
                return false;
            if(String.IsNullOrEmpty(observedResult))return false;

            string[] parts=observedResult.Split('|');
            if(parts.Length!=3)return false;
            const string officerPrefix="officers=";
            const string dismissedPrefix="native_companions_dismissed=";
            if(!parts[0].StartsWith(officerPrefix,StringComparison.Ordinal) ||
                !parts[1].StartsWith(dismissedPrefix,
                    StringComparison.Ordinal))
                return false;
            int officers,dismissed;
            if(!Int32.TryParse(parts[0].Substring(officerPrefix.Length),
                    out officers) ||
                !Int32.TryParse(parts[1].Substring(dismissedPrefix.Length),
                    out dismissed) ||
                dismissed<0 || dismissed>16)
                return false;

            if(mode=="FamilyOnly")
                return officers==0 && parts[2]=="family_only=true";

            // The actual native retinue factory returns the full receipt
            // only after all new officers were registered in the party,
            // and the selected native companion was dismissed, if needed.
            return officers>0 && officers<=16 &&
                parts[2]=="same_culture=true";
        }

        // Shared final proof: a matching name/count alone is not family.
        public static string FamilyIdentityGate(bool spouseRequested,bool reciprocalMarriage,
            bool childrenRequested,bool exactChildrenAndBothParentLinks,
            bool siblingsRequested,bool exactSiblingsAndBothParentLinks)
        {
            if(spouseRequested && !reciprocalMarriage)return "native_marriage_not_reciprocal";
            if(childrenRequested && !exactChildrenAndBothParentLinks)return "native_child_identity_or_parentage_missing";
            if(siblingsRequested && !exactSiblingsAndBothParentLinks)return "native_sibling_identity_or_parentage_missing";
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
            else if (!SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                desiredNativeStartType,actualNativeStartType,
                !String.IsNullOrEmpty(actualKingdomId),isRulingClan,
                actualFiefs))
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
