namespace CalradiaUnified
{
    /// <summary>Pure preflight rules run before any real Hero is registered.</summary>
    public static class SandboxFamilyPolicy
    {
        public static string Guard(bool spouseRequested, bool alreadyMarried,
            int childrenRequested, bool hasExistingChildren, int siblingsRequested,
            bool hasNativeParents, bool matchingMarriageSex,
            bool spouseGenderSupported)
        {
            if (spouseRequested && alreadyMarried) return "existing_spouse";
            if (childrenRequested > 0 && hasExistingChildren)
                return "existing_children";
            if (siblingsRequested > 0 && !hasNativeParents)
                return "native_parents_missing";
            if (spouseRequested && !spouseGenderSupported)
                return "native_marriage_gender_untested";
            if (spouseRequested && matchingMarriageSex)
                return "native_marriage_same_gender_untested";
            return "";
        }
        public static bool ValidChildAge(int childAge, int playerAge,
            bool hasSpouse, int spouseAge)
        {
            return childAge >= 0 && playerAge - childAge >= 16 &&
                (!hasSpouse || spouseAge - childAge >= 16);
        }
    }
}
