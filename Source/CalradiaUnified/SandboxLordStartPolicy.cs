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

        /// <summary>
        /// A new guided Lord must establish a native married household
        /// with one or more genuine children. Siblings remain optional.
        /// Historical Lord profiles do not enter this new gate.
        /// </summary>
        public static string ValidateRequiredFamily(bool spouseRequested,
            int requestedChildren)
        {
            if(!spouseRequested)return "lord_spouse_required";
            if(requestedChildren<1)return "lord_child_required";
            if(requestedChildren>SandboxOriginProfile.MaxChildren)
                return "lord_children_exceed_limit";
            return "";
        }

        public static string ValidateObservedFamily(bool marriageReciprocal,
            bool spouseInPlayerClan,int expectedChildren,
            int verifiedNativeChildren)
        {
            if(!marriageReciprocal || !spouseInPlayerClan)
                return "native_lord_marriage_missing";
            if(expectedChildren<1 || verifiedNativeChildren<
                expectedChildren)
                return "native_lord_children_or_parentage_missing";
            return "";
        }

        public static string ValidateNobleSpouseRequest(
            string background,string chosenClanId)
        {
            if(background!="NobleRandom" && background!="NobleChosen")
                return "lord_spouse_requires_native_noble_house";
            if(background=="NobleChosen" &&
                String.IsNullOrWhiteSpace(chosenClanId))
                return "lord_named_noble_house_required";
            return "";
        }

        public static string ValidateNativeNobleSpouse(
            bool foreignNativeNobleBirthClan,
            bool verifiedNativeMarriedParents,
            bool reciprocalNativeChildReferences,
            bool spouseNativeOriginClanMatches,
            bool chosenClanMatches)
        {
            if(!foreignNativeNobleBirthClan)
                return "native_noble_birth_house_missing";
            if(!verifiedNativeMarriedParents)
                return "native_noble_married_parents_missing";
            if(!reciprocalNativeChildReferences)
                return "native_noble_parentage_unverified";
            if(!spouseNativeOriginClanMatches)
                return "native_noble_origin_clan_mismatch";
            if(!chosenClanMatches)
                return "native_chosen_noble_house_mismatch";
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
