using System;
namespace CalradiaUnified
{
    /// <summary>
    /// New-game Knight origins are not the legacy landed-vassal start.
    /// Real younger-sibling clan transfer has not been established.
    /// </summary>
    public static class SandboxKnightStartPolicy
    {
        public static readonly string[] Origins={
            "SwornHouse","YoungerSibling"
        };
        public static bool IsOrigin(string value)
        {
            return Array.IndexOf(Origins,value)>=0;
        }
        public static bool CanCreateSiblings(string origin)
        {
            return origin=="SwornHouse";
        }
        public static string ValidateRequest(string origin,
            string kingdomId,int siblingCount)
        {
            if(!IsOrigin(origin))return "invalid_knight_origin";
            if(String.IsNullOrEmpty(kingdomId))
                return "patron_kingdom_required";
            if(siblingCount<0 ||
                (!CanCreateSiblings(origin) && siblingCount!=0))
                return "generated_siblings_not_allowed";
            if(origin=="YoungerSibling")
                return "native_same_clan_kinship_not_implemented";
            return "";
        }
        public static string ValidateNativeSwornHouse(string actualType,
            bool kingdomAffiliation,int fiefs,bool mercenary,
            bool playerClanLeader)
        {
            if(!SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default",actualType,kingdomAffiliation,false,fiefs))
                return "wrong_native_start_type";
            if(kingdomAffiliation || fiefs!=0 || mercenary)
                return "sworn_house_must_be_landless_and_independent";
            if(!playerClanLeader)
                return "player_must_lead_separate_house";
            return "";
        }
    }
}
