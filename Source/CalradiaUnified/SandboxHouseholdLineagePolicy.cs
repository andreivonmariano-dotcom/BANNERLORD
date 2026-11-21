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
        public static string ValidateHousePlan(
            SandboxOriginProfile p,int founderAge)
        {
            if(p==null)return "missing_profile";
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
