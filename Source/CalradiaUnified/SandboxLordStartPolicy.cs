using System;

namespace CalradiaUnified
{
    /// <summary>
    /// New seven-path Lord only. Existing legacy vassal profiles retain
    /// their historic grants, fief options and noble-birth parsing.
    /// The new Lord is a native player-led VASSAL house, not a king,
    /// independent knight, mercenary or invented title.
    /// </summary>
    public static class SandboxLordStartPolicy
    {
        public const string GuidedNativeType="vassal";
        public const string GuidedRetinue="RoleBased";
        public static readonly string[] Officers={
            "MasterOfArms","Knight"
        };

        public static string ValidateRequest(string role,
            string kingdom,string birth,string retinue,
            string fiefMode,string fiefId,bool transfer,
            int gold,int influence,int troops,
            bool roleAffiliations,string elderId,string patronId)
        {
            if(role!="Lord")return "not_lord_start";
            if(String.IsNullOrEmpty(kingdom))
                return "native_vassal_kingdom_required";
            if(birth!="Independent" || !String.IsNullOrEmpty(elderId))
                return "independent_founder_house_required";
            if(roleAffiliations)
                return "native_vassal_allegiance_is_authoritative";
            if(!String.IsNullOrEmpty(patronId))
                return "lord_is_clan_head_not_personal_retainer";
            if(fiefMode!="Native" ||
               !String.IsNullOrEmpty(fiefId) || transfer)
                return "native_fief_only_no_transfer";
            if(retinue!=GuidedRetinue)
                return "two_verified_cultural_household_officers_required";
            if(gold!=-1 || influence!=-1 || troops!=0)
                return "native_resources_only";
            return "";
        }

        public static string ValidateNativeHouse(string nativeType,
            string expectedKingdom,string actualKingdom,
            bool playerLeadsClan,bool playerLeadsParty,
            bool nativeRulingClan,bool mercenaryService,
            bool nobleClan,bool clanEliminated,
            int nativeFiefs)
        {
            if(nativeType!=GuidedNativeType)
                return "not_native_vassal_start";
            if(String.IsNullOrEmpty(expectedKingdom) ||
               actualKingdom!=expectedKingdom)
                return "wrong_native_kingdom";
            if(!playerLeadsClan)return "player_not_native_clan_head";
            if(!playerLeadsParty)return "player_not_main_party_leader";
            if(nativeRulingClan)return "king_not_lord";
            if(mercenaryService)return "mercenary_not_vassal";
            if(!nobleClan || clanEliminated)
                return "not_active_noble_house";
            // Pinned v1.5.7 native vassal start and existing CU
            // acceptance both require at least one real fief.
            // Landless noble leadership needs a separately proven
            // native-start contract. Do not gift another lord's fief.
            if(nativeFiefs<1)return "native_vassal_holding_missing";
            return "";
        }

        public static string ValidateOfficers(string receipt,int count,
            bool nativeCompanions,bool allSameCulture,
            bool allRealHeroes,bool bothInParty,bool correctOffices)
        {
            if(String.IsNullOrEmpty(receipt) ||
               !receipt.StartsWith("officers=2|",
                   StringComparison.Ordinal) ||
               !receipt.EndsWith("|same_culture=true",
                   StringComparison.Ordinal))
                return "two_native_officers_unverified";
            if(count!=2 || nativeCompanions)
                return "native_retinue_roster_mismatch";
            if(!allSameCulture || !allRealHeroes ||
               !bothInParty || !correctOffices)
                return "native_retinue_identity_or_assignment_mismatch";
            return "";
        }
    }
}
