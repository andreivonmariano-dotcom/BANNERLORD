namespace CalradiaUnified
{
    /// <summary>Pure role policies. Narrative labels never grant political ranks.</summary>
    public static class SandboxRolePolicy
    {
        public static bool MatchesNativeStatus(string role, bool inKingdom,
            bool rulingClan)
        {
            if (role == "King") return inKingdom && rulingClan;
            if (role == "Lord" || role == "Knight" || role == "Soldier")
                return inKingdom && !rulingClan;
            return role == "Wanderer" && !inKingdom;
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
        /// <summary>Only the exact native action outcome grants a bonus.</summary>
        public static bool ConfirmedNativeStanding(string role, string result)
        {
            if (role == "King") return result == "native_king_confirmed";
            if (role == "Soldier") return result == "native_mercenary_contract";
            if (role == "Knight" || role == "Lord")
                return result == "native_vassal_contract";
            return role == "Wanderer" &&
                result == "independent_wanderer";
        }
        public static bool CanRequestAffiliation(string role, bool inKingdom,
            bool optedIn)
        {
            return optedIn && !inKingdom &&
                (role == "Soldier" || role == "Knight" || role == "Lord");
        }
    }
}
