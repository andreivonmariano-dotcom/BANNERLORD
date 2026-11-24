using System;

namespace CalradiaUnified
{
    /// <summary>Generated motivation, not a claim about a real hero's past.</summary>
    public static class SandboxGeneratedCharacterGoalPolicy
    {
        public static string Goal(string culture,string station,
            string occupation,bool sibling,bool inPlayerParty)
        {
            if(sibling)
                return inPlayerParty?"ProtectSharedHousehold":
                    "PreserveFamilyPosition";
            if(occupation=="Factor")return "BuildTradingContacts";
            if(occupation=="Navigator")return "MapSafePassages";
            if(occupation=="Squire" || occupation=="MasterOfArms")return "GainMilitaryTraining";
            if(occupation=="Knight" || occupation=="Kingsguard" || occupation=="Marshal" ||
                occupation=="FirstMate" || occupation=="SecondInCommand")return "KeepCompanyTogether";
            if(occupation=="RoyalMinister")return "MaintainHousehold";
            if(occupation=="Sergeant")return "KeepCompanyTogether";
            if(occupation=="HouseholdSteward" ||
                occupation=="Chamberlain")return "MaintainHousehold";
            if(station=="RecordedParent")return "ProtectDescendants";
            if(station=="RecordedOffspring")return "CompleteUpbringing";
            if(station=="SpouseInPlayerClan")return "SupportHousehold";
            if(station=="KingdomClanLeader")return "DefendClanStanding";
            return "EstablishLivelihood";
        }
        public static int ComradeshipIncrement(int consecutiveDays,
            int previousMilestones,int currentNativeRelation)
        {
            if(consecutiveDays<=0 || consecutiveDays%30!=0 ||
                previousMilestones>=5 || currentNativeRelation>=80)
                return 0;
            return 1;
        }
        public static bool CanShareMilitaryService(bool adult,
            bool hasSameMotherAndFather,bool sameRealParty)
        {
            return adult && hasSameMotherAndFather && sameRealParty;
        }
    }
}
