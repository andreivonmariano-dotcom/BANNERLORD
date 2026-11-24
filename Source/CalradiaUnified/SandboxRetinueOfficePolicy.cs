namespace CalradiaUnified
{
    /// <summary>
    /// Military and court specialties. Only native party offices are
    /// assigned official gameplay duties.
    /// </summary>
    public static class SandboxRetinueOfficePolicy
    {
        public static string[] Skills(string office)
        {
            switch(office)
            {
                case "Knight":return new[]{"Riding","Polearm","OneHanded"};
                case "MasterOfArms":return new[]{"OneHanded","Leadership","Tactics"};
                case "Marshal":return new[]{"Leadership","Tactics","Riding"};
                case "HouseholdSteward":return new[]{"Steward","Trade","Charm"};
                case "Chamberlain":return new[]{"Steward","Charm","Trade"};
                case "RoyalMinister":return new[]{"Steward","Charm","Trade"};
                case "Kingsguard":return new[]{"OneHanded","Athletics","Riding"};
                case "Squire":return new[]{"Riding","Athletics","OneHanded"};
                case "FirstMate":return new[]{"Leadership","Tactics","Steward"};
                case "Navigator":return new[]{"Scouting","Tactics","Riding"};
                case "Sergeant":return new[]{"Leadership","Athletics","OneHanded"};
                case "Factor":return new[]{"Trade","Steward","Charm"};
                case "SecondInCommand":return new[]{"Tactics","Roguery","Leadership"};
                default:return new string[0];
            }
        }
        public static bool IsFieldOfficer(string office)
        {
            return office=="Knight" || office=="MasterOfArms" ||
                office=="Marshal" || office=="Kingsguard" ||
                office=="Squire" ||
                office=="FirstMate" || office=="Sergeant" ||
                office=="SecondInCommand";
        }
        public static string PartyDuty(string office)
        {
            if(office=="HouseholdSteward" || office=="Chamberlain" ||
                office=="Factor")return "Quartermaster";
            if(office=="Navigator")return "Scout";
            return "None";
        }
    }
}
