namespace CalradiaUnified
{
    /// <summary>
    /// Soldiers and sworn knights have a named native superior. Soldier
    /// remains a native mercenary-clan start, not a fabricated enlisted troop.
    /// </summary>
    public static class SandboxServicePolicy
    {
        public static bool NeedsPatron(string role)
        {
            return role=="Soldier" || role=="Knight" ||
                role=="Recruit" || role=="HouseholdKnight";
        }
        public static string DefaultRetinue(string role)
        {
            if(role=="Soldier" || role=="Mercenary" ||
                role=="Recruit" ||
                role=="HouseholdKnight" || role=="NobleCadet")
                return "FamilyOnly";
            return SandboxNobleStartPolicy.EligibleForRetinue(
                "RoleBased",role)?"RoleBased":"Native";
        }
        public static bool CanSelectPatron(string role,string kingdomId)
        {
            return NeedsPatron(role) && !string.IsNullOrEmpty(kingdomId);
        }
    }
}
