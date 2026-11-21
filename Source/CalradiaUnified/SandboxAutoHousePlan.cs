using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// The minimal House flow asks only for family counts. Native identity
    /// supplies culture/age/sex, and Advanced can override these defaults.
    /// </summary>
    public static class SandboxAutoHousePlan
    {
        public static void Prepare(SandboxOriginProfile plan,string culture,
            int playerAge,bool playerFemale,string playerName)
        {
            if(plan==null || plan.AutoHouseNamesPrepared)return;
            int founder=Math.Max(18,Math.Min(80,playerAge));
            plan.SpouseAge=Math.Max(18,Math.Min(70,
                founder>=30?founder-2:founder));
            plan.SpouseGender="Auto";
            plan.SpouseCulture="";
            plan.SpouseBackground=
                (plan.Role=="Lord" || plan.Role=="King")?
                "NobleRandom":"Commoner";
            if(SandboxCustomLifeRolePolicy.IsLandless(plan.Role))
                plan.FamilyHomeMode="None";
            plan.FamilyScenario=plan.Role=="Admiral"?
                "Seafaring":SandboxStartingLifePolicy.FamilyScenario(
                    plan.Role,plan.EarlyHousehold);
            var used=new List<string>();
            if(!String.IsNullOrWhiteSpace(playerName))used.Add(playerName);
            bool spouseFemale=!playerFemale;
            plan.SpouseName=SandboxCulturalNames.Suggest(
                culture,spouseFemale,0,used);
            used.Add(plan.SpouseName);
            int childMax=Math.Max(0,Math.Min(founder,
                plan.SpouseAge)-16);
            for(int i=0;i<plan.Children.Count;i++)
            {
                var child=plan.Children[i];
                bool female=child.Gender=="Female";
                child.Name=SandboxCulturalNames.Suggest(
                    culture,female,i+1,used);
                used.Add(child.Name);
                child.Age=Math.Min(childMax,Math.Max(0,9-2*i));
                child.Upbringing="FamilyTradition";
                child.Culture="";
            }
            for(int i=0;i<plan.Siblings.Count;i++)
            {
                var sibling=plan.Siblings[i];
                bool female=sibling.Gender=="Female";
                sibling.Name=SandboxCulturalNames.Suggest(
                    culture,female,i+4,used);
                used.Add(sibling.Name);
                int delta=(i+1)*2;
                sibling.Age=Math.Max(18,Math.Min(80,
                    founder+(i%2==0?-delta:delta)));
                sibling.Culture="";
            }
            plan.AutoHouseNamesPrepared=true;
        }
    }
}
