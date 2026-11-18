using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Bannerlord native IsCoupleSuitableForMarriage prerequisites checked
    /// before passing a fresh hero to the native marriage model.
    /// The native model retains final authority over courtship and kinship.
    /// </summary>
    public static class SandboxGeneratedMarriagePolicy
    {
        public static string Check(bool playerActive,bool spouseActive,
            bool playerCanMarry,bool spouseCanMarry,
            bool playerClanSuitable,bool spouseClanSuitable,
            bool oppositeSex)
        {
            if (!playerActive) return "player_inactive";
            if (!spouseActive) return "generated_spouse_not_active";
            if (!playerClanSuitable) return "player_clan_invalid";
            if (!spouseClanSuitable) return "spouse_birth_clan_invalid";
            if (!oppositeSex) return "sex_pair_rejected";
            if (!playerCanMarry) return "player_not_eligible";
            if (!spouseCanMarry) return "generated_spouse_not_eligible";
            return "";
        }
    }
}
