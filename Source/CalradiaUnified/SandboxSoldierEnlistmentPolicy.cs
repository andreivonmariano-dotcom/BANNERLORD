using System;

namespace CalradiaUnified
{
    /// <summary>
    /// New guided Soldier only. Real rank, lord, army, pay and promotion
    /// are owned by a supported Serve as Soldier integration, not CU.
    /// Never convert a native mercenary-clan captain into an enlisted man.
    /// This PURE policy validates observed public SaS API fields.
    /// </summary>
    public static class SandboxSoldierEnlistmentPolicy
    {
        public static readonly string[] EntryChoices=
            {"Recruit","Sergeant","Captain"};
        public static readonly string[] NativeRankIds=
            {"private","soldier","corporal","sergeant","veteran",
             "captain","champion"};

        public static bool IsEntryRank(string entry)
        {
            return Array.IndexOf(EntryChoices,entry)>=0;
        }

        public static string RequestedNativeRank(string entry)
        {
            if(entry=="Recruit")return "private";
            if(entry=="Sergeant")return "sergeant";
            if(entry=="Captain")return "captain";
            return "";
        }

        public static int NativeRankNumber(string rankId)
        {
            int i=Array.IndexOf(NativeRankIds,rankId);
            return i<0?0:i+1;
        }

        // A broader career summary is useful after the player is
        // promoted. Exact native rank ID is always retained unchanged.
        public static string CareerStage(string nativeRankId)
        {
            int native=NativeRankNumber(nativeRankId);
            if(native==0)return "Unverified";
            if(native<4)return "Recruit";
            if(native<6)return "Sergeant";
            return "Captain";
        }

        public static string ValidateIndependentStart(string nativeType,
            bool playerClanInKingdom,bool hasNativeFief,
            bool nativeMercenaryContract)
        {
            if(nativeType!="default" && nativeType!="")
                return "not_independent_native_start";
            if(playerClanInKingdom || hasNativeFief ||
                nativeMercenaryContract)
                return "native_clan_not_independent";
            return "";
        }

        public static string ValidateEnlisted(
            bool providerAvailable,bool actuallyEnlisted,
            string expectedKingdomId,string requestedLordId,
            string actualLordId,string actualLordKingdomId,
            string actualRankId,bool hostPartyLedByActualLord,
            bool playerClanIsMercenary,bool playerClanOwnsFief)
        {
            if(!providerAvailable)return "service_provider_unavailable";
            if(!actuallyEnlisted)return "native_enlistment_not_observed";
            if(String.IsNullOrEmpty(expectedKingdomId))
                return "employer_kingdom_not_requested";
            if(String.IsNullOrEmpty(actualLordId) ||
                !hostPartyLedByActualLord)
                return "native_commanding_lord_or_party_missing";
            if(!String.Equals(expectedKingdomId,actualLordKingdomId,
                StringComparison.Ordinal))
                return "native_commander_kingdom_mismatch";
            if(!String.IsNullOrEmpty(requestedLordId) &&
                !String.Equals(requestedLordId,actualLordId,
                    StringComparison.Ordinal))
                return "different_native_commanding_lord";
            if(NativeRankNumber(actualRankId)==0)
                return "native_rank_not_recognized";
            if(playerClanIsMercenary || playerClanOwnsFief)
                return "enlisted_person_misclassified_as_landed_company";
            return "";
        }

        public static string ValidateRequestedEntryRank(
            string desiredEntry,string actualRankId)
        {
            if(!IsEntryRank(desiredEntry))
                return "unrecognized_requested_entry_rank";
            if(NativeRankNumber(actualRankId)==0)
                return "native_rank_not_recognized";
            return String.Equals(RequestedNativeRank(desiredEntry),
                actualRankId,StringComparison.Ordinal)?""
                :"actual_rank_differs_from_selected_entry";
        }

        // SaS owns experience, equipment, duty, rank and salary. This
        // policy does not use days or kills to fake promotion.
        public static string PromotionReceipt(string oldRankId,
            string observedRankId)
        {
            int old=NativeRankNumber(oldRankId);
            int current=NativeRankNumber(observedRankId);
            if(current==0)return "rank_not_verified";
            if(old==0)return "initial_native_rank_observed";
            if(current>old)return "native_promotion_observed";
            if(current<old)return "native_demotion_observed";
            return "native_rank_unchanged";
        }
    }
}
