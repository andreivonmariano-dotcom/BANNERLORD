using System;

namespace CalradiaUnified
{
    /// <summary>
    /// One pure matrix for all twelve playable social starts.
    /// These are game/native contract constraints, not a claim that
    /// medieval laws were identical across Bannerlord cultures.
    /// </summary>
    public static class SandboxRoleConsistencyPolicy
    {
        public static string NativeType(string role)
        {
            switch(role)
            {
                case "Wanderer":
                case "Recruit":
                case "HouseholdKnight":
                case "NobleCadet":return "default";
                case "Soldier":
                case "Mercenary":return "mercenary";
                case "Knight":
                case "Lord":return "vassal";
                case "King":return "king";
                case "Admiral":return "fleetadmiral";
                case "Trader":return "trader";
                case "Outlaw":return "outlaw";
                case "Beggar":return "beggar";
                default:return "";
            }
        }

        // Native Advanced Start stores changed options only. The default
        // scenario can be represented by an absent StartType in the actual
        // campaign. Treat it as the default ONLY with a verified independent,
        // landless clan. Never infer King/Vassal/Mercenary from a missing ID.
        public static bool NativeStartTypeMatches(string expected,
            string observed,bool hasKingdom,bool rulingClan,int fiefs)
        {
            if(String.IsNullOrEmpty(expected))return false;
            if(String.Equals(expected,observed,StringComparison.Ordinal))
                return true;
            return expected=="default" &&
                String.IsNullOrEmpty(observed) &&
                !hasKingdom && !rulingClan && fiefs==0;
        }

        public static string CheckNativeStation(string role,
            string actualStartType,bool hasKingdom,bool rulingClan,
            int fiefs)
        {
            string expected=NativeType(role);
            if(expected.Length==0)return "unknown_medieval_role";
            if(!NativeStartTypeMatches(expected,actualStartType,
                hasKingdom,rulingClan,fiefs))
                return "native_start_type_mismatch";
            if(fiefs<0)return "invalid_native_fief_count";
            if(role=="King")
                return !hasKingdom || !rulingClan || fiefs<1?
                    "ruler_status_or_fief_missing":"";
            if(role=="Lord" || role=="Knight")
                return !hasKingdom || rulingClan || fiefs<1?
                    "vassal_status_or_fief_missing":"";
            if(role=="Mercenary")
                return !hasKingdom || rulingClan || fiefs!=0 ?
                    "mercenary_company_station_or_land_missing":"";
            if(role=="Soldier" || role=="Admiral")
                return !hasKingdom || rulingClan?
                    "native_military_kingdom_missing":"";
            // Other roles do not start as political vassals and may not
            // claim a native estate under an independent life label.
            return hasKingdom || rulingClan || fiefs>0?
                "independent_start_has_kingdom_or_fief":"";
        }
    }
}
