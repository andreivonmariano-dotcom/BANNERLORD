namespace CalradiaUnified
{
    /// <summary>Pure role policies. Narrative labels never grant political ranks.</summary>
    public static class SandboxRolePolicy
    {
        public static bool MatchesNativeStatus(string role, bool inKingdom,
            bool rulingClan)
        {
            if (role == "King") return inKingdom && rulingClan;
            if (role == "Lord") return inKingdom && !rulingClan;
            return role == "Wanderer" || role == "Soldier" || role == "Knight";
        }
        public static int BonusRecruits(string role)
        {
            switch (role)
            {
                case "Soldier": return 4;
                case "Knight": return 2;
                case "Lord": return 5;
                case "King": return 8;
                default: return 0;
            }
        }
        public static int StarterRenown(string role)
        {
            switch (role)
            {
                case "Soldier": return 10;
                case "Knight": return 40;
                case "Lord": return 80;
                case "King": return 120;
                default: return 0;
            }
        }
        public static bool CanRequestAffiliation(string role, bool inKingdom,
            bool optedIn)
        {
            return optedIn && !inKingdom &&
                (role == "Soldier" || role == "Knight" || role == "Lord");
        }
    }
}
