using System;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A "second son" has EXACTLY one older living son of both parents.
    /// Selected older House siblings count toward the same birth rank.
    /// </summary>
    public static class SandboxHouseholdLineagePolicy
    {
        public static int PlannedOlderBrothers(
            SandboxOriginProfile p,int founderAge)
        {
            if(p==null)return 0;
            return p.Siblings.Take(p.SiblingCount).Count(s=>
                s.Gender=="Male" && s.Age>founderAge);
        }
        public static int OldestPlannedSiblingAge(
            SandboxOriginProfile p,int founderAge)
        {
            if(p==null)return founderAge;
            return Math.Max(founderAge,
                p.Siblings.Take(p.SiblingCount)
                    .Select(s=>s.Age)
                    .DefaultIfEmpty(founderAge).Max());
        }
        public static bool ValidProjectedRank(int required,
            int existingOlderBrothers,int plannedOlderBrothers)
        {
            return required>=0 && existingOlderBrothers>=0 &&
                plannedOlderBrothers>=0 &&
                existingOlderBrothers+plannedOlderBrothers<=required;
        }
        public static int AdditionalOlderBrothers(int required,
            int existingOlderBrothers,int plannedOlderBrothers)
        {
            if(!ValidProjectedRank(required,existingOlderBrothers,
                plannedOlderBrothers))return -1;
            return required-existingOlderBrothers-plannedOlderBrothers;
        }
        // Independent Lords keep their player-character position as
        // the eldest brother. This is a creation rule, not a native
        // Clan.Leader transfer or a blanket ban on older sisters.
        // Return a 1-based index so House can focus the exact age field.
        public static int FirstOlderBrotherInIndependentLordHouse(
            SandboxOriginProfile p,int founderAge)
        {
            if(p==null || p.Role!="Lord" ||
                p.NobleBirthPosition!="Independent")return 0;
            int count=Math.Min(p.SiblingCount,p.Siblings.Count);
            for(int i=0;i<count;i++)
            {
                var sibling=p.Siblings[i];
                if(sibling!=null && sibling.Gender=="Male" &&
                    sibling.Age>founderAge)return i+1;
            }
            return 0;
        }
        public static string ValidateHousePlan(
            SandboxOriginProfile p,int founderAge)
        {
            if(p==null)return "missing_profile";
            if(FirstOlderBrotherInIndependentLordHouse(p,founderAge)>0)
                return "independent_lord_older_brother";
            if(!SandboxNobleStartPolicy.IsSonOrigin(
                p.NobleBirthPosition))return "";
            int required=SandboxNobleStartPolicy.RequiredOlderSons(
                p.NobleBirthPosition);
            int planned=PlannedOlderBrothers(p,founderAge);
            if(planned>required)return "too_many_older_male_siblings";
            return "";
        }
        public static bool ExactFinalRank(int required,int olderBrothers)
        {
            return olderBrothers==required;
        }
    }
}
