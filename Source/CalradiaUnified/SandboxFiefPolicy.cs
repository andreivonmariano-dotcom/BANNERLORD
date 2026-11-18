using System;
namespace CalradiaUnified
{
    /// <summary>Pure, testable checks for a one-time startup fief request.</summary>
    public static class SandboxFiefPolicy
    {
        public static readonly string[] Modes =
            { "Native", "Castle", "Town", "Specific" };

        public static bool IsEligibleRole(string role)
        {
            return role == "Lord" || role == "Knight" || role == "King";
        }

        public static string ValidateRequest(string role, string mode,
            string selectedId, bool explicitTransferConsent)
        {
            if (mode == "Native") return "";
            if (!IsEligibleRole(role)) return "fief_role_ineligible";
            if (mode != "Castle" && mode != "Town" &&
                mode != "Specific") return "fief_mode_unknown";
            if (!explicitTransferConsent)
                return "fief_transfer_not_authorized";
            if (mode == "Specific" && String.IsNullOrEmpty(selectedId))
                return "fief_id_required";
            return "";
        }

        public static string ValidateTarget(bool isFortification,
            bool matchingKingdom, bool underSiege, bool inBattle,
            bool alreadyPlayerOwned)
        {
            if (!isFortification) return "not_a_fortification";
            if (!matchingKingdom) return "fief_outside_starting_kingdom";
            if (underSiege || inBattle) return "fief_contested";
            // Reusing an already owned native-start fief is idempotent.
            return "";
        }
    }
}
