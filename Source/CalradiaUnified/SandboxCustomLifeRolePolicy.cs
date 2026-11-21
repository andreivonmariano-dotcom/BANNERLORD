using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Three new honest starts use native independent character creation.
    /// Recruit is an applicant until a verified external enlistment occurs.
    /// HouseholdKnight has a named lord but is NOT a vassal clan leader.
    /// NobleCadet is a landless independent clan with verified native kin.
    /// </summary>
    public static class SandboxCustomLifeRolePolicy
    {
        public static readonly string[] AdditionalRoles={
            "Recruit","HouseholdKnight","NobleCadet"
        };
        public static bool IsLandless(string role)
        {
            return role=="Recruit" || role=="HouseholdKnight" ||
                role=="NobleCadet";
        }
        public static bool IsNobleFamilyRole(string role)
        {
            return role=="Lord" || role=="NobleCadet";
        }
        public static bool WantsPersonalPatron(string role)
        {
            return role=="Recruit" || role=="HouseholdKnight";
        }
        public static bool NeedsKingdomContext(string role)
        {
            return IsLandless(role);
        }
        public static bool ValidBirth(string role,string birth)
        {
            if(role=="NobleCadet")
                return SandboxNobleStartPolicy.RequiresNobleElder(birth);
            if(role=="HouseholdKnight" || role=="Recruit")
                return birth=="Independent";
            return true;
        }
        public static bool NativeLandlessContract(bool kingdomAffiliation,
            int actualFiefs)
        {
            return !kingdomAffiliation && actualFiefs==0;
        }
        public static string EmploymentMeaning(string role)
        {
            if(role=="Recruit")return "EnlistmentApplicant";
            if(role=="HouseholdKnight")return "PersonalKnightInService";
            if(role=="NobleCadet")return "LandlessDynasticCadet";
            return "NativeStart";
        }
    }
}
