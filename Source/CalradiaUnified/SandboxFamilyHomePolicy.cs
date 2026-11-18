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
            { "OwnedFief", "ClanHome", "SpecificOwned" };

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
