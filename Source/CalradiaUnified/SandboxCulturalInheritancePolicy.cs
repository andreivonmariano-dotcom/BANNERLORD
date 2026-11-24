using System;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Regional PROPOSAL priorities, never fictitious native legal grants.
    /// All cultures require native living family and actual clan holdings.
    /// These are gameplay conventions, not universal historical law.
    /// </summary>
    public static class SandboxCulturalInheritancePolicy
    {
        public static string Model(string culture)
        {
            switch(culture)
            {
                case "vlandia":return "HouseholdSeniority";
                case "sturgia":return "ClanConsensus";
                case "battania":return "KindredCouncil";
                case "empire":return "CivilAppointment";
                case "aserai":return "FamilyNegotiation";
                case "khuzait":return "ClanCoalition";
                default:return "Unestablished";
            }
        }
        // Cultural conventions are guidance, not legal rights, trait
        // assignments, generated names for existing Heroes or native grants.
        public static string EducationConvention(string culture)
        {
            switch(culture)
            {
                case "vlandia":return "Martial";
                case "sturgia":return "Martial";
                case "battania":return "Independent";
                case "empire":return "Scholarly";
                case "aserai":return "Mercantile";
                case "khuzait":return "Martial";
                case "nord":return "Seafaring";
                default:return "";
            }
        }
        public static string CustomGuidance(string culture)
        {
            switch(culture)
            {
                case "vlandia":return "Household seniority, sworn service and knightly patronage";
                case "sturgia":return "Clan consensus, kin support and household defense";
                case "battania":return "Kindred council, local knowledge and independent upbringing";
                case "empire":return "Civil appointment, formal service and scholarly education";
                case "aserai":return "Family negotiation, hospitality and commercial education";
                case "khuzait":return "Clan coalition, mounted service and kin alliances";
                case "nord":return "Proposed maritime household tradition; inheritance model unestablished";
                default:return "";
            }
        }
        public static int ClaimPriority(string culture,int olderSons,
            bool aliveParent,bool claimantInBirthClan,
            int clanSupport,int relationshipToParent)
        {
            if(olderSons<0 || clanSupport<0 ||
                clanSupport>100 || relationshipToParent< -100 ||
                relationshipToParent>100)return -1;
            if(!aliveParent)return 0;
            int seniority=(int)Math.Max(0L,100L-(long)olderSons*22L);
            int kinship=claimantInBirthClan?15:0;
            int standing=Math.Max(0,Math.Min(100,
                (relationshipToParent+100)/2));
            switch(Model(culture))
            {
                case "HouseholdSeniority":
                    return seniority*3+standing/2+kinship;
                case "ClanConsensus":
                    return seniority+clanSupport*2+standing+kinship;
                case "KindredCouncil":
                    return seniority+clanSupport*2+standing*2;
                case "CivilAppointment":
                    return seniority+clanSupport+standing*2;
                case "FamilyNegotiation":
                    return seniority+clanSupport+standing*2+kinship;
                case "ClanCoalition":
                    return seniority+clanSupport*3+standing+kinship;
                default:return -1;
            }
        }
        public static string Review(int score,bool fatherAlive,
            int actualBirthClanFiefs)
        {
            if(score<0)return "Unassessed";
            if(actualBirthClanFiefs<=0)return "NoNativeEstate";
            if(fatherAlive)return "LivingParentNoInheritance";
            return "NativeSuccessionOnly";
        }
        public static bool CanTransferEstateWithoutNativeAction()
        {
            return false;
        }
    }
}
