using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure configuration contract for freshly generated Sandbox relatives.
    /// A generated spouse is never an existing marriageable campaign hero.
    /// Noble birth means the generated hero starts in a real native clan and,
    /// when valid parents exist, joins that family's ancestry before marriage.
    /// </summary>
    public static class SandboxFamilyOriginPolicy
    {
        public static readonly string[] Backgrounds =
            { "Commoner", "NobleRandom", "NobleChosen" };

        public static string NormalizeCultureId(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "";
            switch (value.Trim().ToLowerInvariant())
            {
                case "vlandian":
                case "vlandia": return "vlandia";
                case "battanian":
                case "battania": return "battania";
                case "sturgian":
                case "sturgia": return "sturgia";
                case "khuzait": return "khuzait";
                case "aserai": return "aserai";
                case "imperial":
                case "empire": return "empire";
                case "empire_s":
                case "empire_w": return "empire";
                default: return value.Trim().ToLowerInvariant();
            }
        }

        public static string Validate(string background, string birthClanId,
            bool spouseRequested)
        {
            if (!spouseRequested) return "";
            if (Array.IndexOf(Backgrounds,background) < 0)
                return "unknown_spouse_background";
            if (background == "NobleChosen" &&
                String.IsNullOrEmpty(birthClanId))
                return "spouse_noble_clan_required";
            return "";
        }

        public static bool CanBeNobleParents(int fatherAge,int motherAge,
            int spouseAge, bool married, bool sameClan)
        {
            return married && sameClan &&
                fatherAge >= spouseAge + 16 &&
                motherAge >= spouseAge + 16;
        }
    }
}
