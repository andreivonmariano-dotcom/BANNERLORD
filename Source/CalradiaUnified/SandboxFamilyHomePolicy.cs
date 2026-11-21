using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure policy for home residence of newly generated Sandbox relatives.
    /// Keep infant heroes out of parties and preserve their minor state.
    /// </summary>
    public static class SandboxFamilyHomePolicy
    {
        public static readonly string[] Modes =
            { "None", "OwnedFief", "ClanHome", "SpecificOwned" };

        public static string ValidateRequest(string mode, string settlementId,
            bool spouseRequested)
        {
            if (!spouseRequested) return "";
            if (Array.IndexOf(Modes, mode) < 0)
                return "family_home_mode_invalid";
            if (mode == "SpecificOwned" &&
                String.IsNullOrEmpty(settlementId))
                return "family_home_id_required";
            return "";
        }

        // A landless independent clan cannot promise an owned estate at start.
        // A non-landed role may opt out of fixed residence without inventing a fief.
        public static string ValidateRoleRequest(string role, string mode,
            bool spouseRequested)
        {
            return spouseRequested &&
                SandboxCustomLifeRolePolicy.IsLandless(role) &&
                mode!="None" ? "landless_cannot_start_with_owned_home" : "";
        }

        public static string ValidateObservedPlacement(string mode,
            bool spouseRequested, string outcome)
        {
            if(!spouseRequested || mode=="None")return "";
            return outcome!=null && outcome.StartsWith("placed:",
                StringComparison.Ordinal) ? "" :
                "requested_family_home_unfulfilled";
        }

        public static string ValidateSettlement(bool playerClanOwns,
            bool fortification, bool underSiege, bool battle)
        {
            if (!playerClanOwns) return "not_player_clan_fief";
            if (!fortification) return "not_castle_or_town";
            if (underSiege) return "settlement_under_siege";
            if (battle) return "settlement_in_battle";
            return "";
        }

        public static bool CanPlaceMinor(bool sameClan, bool hasNativeParents,
            bool noParty, bool underage)
        {
            return sameClan && hasNativeParents && noParty && underage;
        }
    }
}
