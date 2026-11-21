using System;

namespace CalradiaUnified
{
    /// <summary>
    /// New guided King only. Native game Advanced Start, not CU, owns
    /// the kingdom, ruling clan, settlement title and sovereign rights.
    /// Old Role=King profiles are parsed with their old meanings.
    /// </summary>
    public static class SandboxKingStartPolicy
    {
        public const string NativeType="king";
        public const string Retinue="RoleBased";
        public static readonly string[] CourtOffices={
            "Marshal","Kingsguard","RoyalMinister"
        };
        public static string ValidateRequest(string role,
            string kingdomId,string birth,string elderId,string patronId,
            bool roleAffiliations,string retinue,string fiefMode,
            string fiefId,bool fiefTransfer,int gold,int influence,
            int troops,string heirRule)
        {
            if(role!="King")return "not_king_start";
            if(String.IsNullOrEmpty(kingdomId))
                return "royal_kingdom_required";
            if(birth!="Independent" || !String.IsNullOrEmpty(elderId))
                return "native_founder_ruling_house_required";
            if(!String.IsNullOrEmpty(patronId) || roleAffiliations)
                return "native_sovereignty_is_authoritative";
            if(retinue!=Retinue)return "three_native_court_officers_required";
            if(fiefMode!="Native" ||
                !String.IsNullOrEmpty(fiefId) || fiefTransfer)
                return "royal_native_estate_only";
            if(gold!=-1 || influence!=-1 || troops!=0)
                return "royal_native_treasury_and_army_only";
            if(!SandboxLordSuccessionPolicy.IsRule(heirRule))
                return "unknown_royal_succession_custom";
            return "";
        }

        public static string ValidateNativeCrown(string actualType,
            string chosenKingdom,string actualKingdom,
            bool playerIsClanHead,bool leadsMainParty,
            bool rulingClan,bool mercenary,bool nobleHouse,
            bool eliminated,int nativeFiefs)
        {
            if(actualType!=NativeType)return "not_native_king_start";
            if(String.IsNullOrEmpty(chosenKingdom) ||
                chosenKingdom!=actualKingdom)
                return "royal_kingdom_mismatch";
            if(!playerIsClanHead)return "not_native_ruling_clan_head";
            if(!leadsMainParty)return "royal_main_party_not_commanded";
            if(!rulingClan)return "not_real_kingdom_ruling_clan";
            if(mercenary)return "royal_clan_marked_mercenary";
            if(!nobleHouse || eliminated)
                return "royal_house_ineligible";
            if(nativeFiefs<1)return "royal_native_fief_missing";
            return "";
        }

        public static string ValidateRequiredRoyalFamily(
            bool spouseRequested,int children)
        {
            if(!spouseRequested)return "royal_spouse_required";
            if(children<1)return "royal_child_required";
            if(children>SandboxOriginProfile.MaxChildren)
                return "royal_children_exceed_limit";
            return "";
        }

        public static string ValidateNativeMinistry(bool ownNativeFief,
            bool unoccupiedGovernorSeat,bool nativeActionVerified,
            bool sameNativeClan)
        {
            if(!ownNativeFief)return "royal_owned_fief_required";
            if(!unoccupiedGovernorSeat)
                return "royal_governor_seat_occupied_or_missing";
            if(!nativeActionVerified || !sameNativeClan)
                return "native_royal_governor_not_verified";
            return "";
        }

        public static string ValidateCourt(string receipt,int count,
            bool sameCulture,bool validNativeMembership,
            bool realNativeHeroes,bool marshalInParty,
            bool ministerNativeGovernor,bool kingsguardInParty,
            bool marshalVerified,bool ministerVerified,
            bool kingsguardVerified)
        {
            if(String.IsNullOrEmpty(receipt) ||
                !receipt.StartsWith("officers=3|",
                    StringComparison.Ordinal) ||
                !receipt.EndsWith("|same_culture=true",
                    StringComparison.Ordinal))
                return "native_royal_court_receipt_missing";
            if(count!=3 || !sameCulture || !validNativeMembership ||
                !realNativeHeroes || !marshalInParty ||
                !ministerNativeGovernor || !kingsguardInParty ||
                !marshalVerified || !ministerVerified ||
                !kingsguardVerified)
                return "native_royal_court_unverified";
            return "";
        }
        // This ranked claim is never a king-electing vote or an action.
        public static bool CanAppointNativeRuler(){return false;}
        public static bool CanGrantNativeFief(){return false;}
        public static bool CanInstallRegent(){return false;}
    }
}
