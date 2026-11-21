using System;

namespace CalradiaUnified
{
    /// <summary>
    /// A negotiated SECONDARY family holding grant is possible only
    /// after a death and actual clan head consent, within one kingdom.
    /// It never stands in for the kingdom's native succession election.
    /// </summary>
    public static class SandboxInheritanceNegotiationPolicy
    {
        public static int ConsentThreshold(string culture,
            int precedingOlderSons)
        {
            int basis;
            switch(SandboxCulturalInheritancePolicy.Model(culture))
            {
                case "HouseholdSeniority":basis=65;break;
                case "ClanConsensus":basis=55;break;
                case "KindredCouncil":basis=53;break;
                case "CivilAppointment":basis=72;break;
                case "FamilyNegotiation":basis=50;break;
                case "ClanCoalition":basis=56;break;
                default:return 101;
            }
            return Math.Min(100,basis+
                Math.Max(0,precedingOlderSons-1)*8);
        }
        public static string Evaluate(bool verifiedClaimant,
            bool fatherDeceased,bool leaderAlive,
            bool atBirthClanCourt,bool sameNativeKingdom,
            int nativeBirthClanFiefs,int precedingOlderSons,
            int leaderRelation,string culture)
        {
            if(!verifiedClaimant)return "UnverifiedFamily";
            if(!fatherDeceased)return "ParentStillLiving";
            if(!leaderAlive)return "NoConsentingClanHead";
            if(!atBirthClanCourt)return "NotAtFamilyCourt";
            if(!sameNativeKingdom)return "NativeAllegianceRequired";
            if(nativeBirthClanFiefs<2)return "NoSecondaryEstate";
            if(precedingOlderSons<0 || leaderRelation< -100 ||
                leaderRelation>100 ||
                SandboxCulturalInheritancePolicy.Model(culture)==
                    "Unestablished")return "Unassessed";
            if(leaderRelation<0)return "ClaimRejected";
            if(leaderRelation<ConsentThreshold(culture,
                precedingOlderSons))return "ClaimContested";
            return "ClanHeadConsents";
        }
        public static bool TransferGate(bool explicitPlayerConfirmed,
            bool priorConsent,string expectedBirthClanId,
            string currentOwnerClanId,bool sameKingdom,
            bool fatherDeceased,int currentBirthHoldings)
        {
            return explicitPlayerConfirmed && priorConsent &&
                !String.IsNullOrEmpty(expectedBirthClanId) &&
                expectedBirthClanId==currentOwnerClanId &&
                sameKingdom && fatherDeceased &&
                currentBirthHoldings>=2;
        }
    }
}
