using System;
namespace CalradiaUnified
{
    /// <summary>Visible age limits are always computed from the two real
    /// founders, never from a guessed default 30-year-old player.</summary>
    public static class SandboxFamilyAgePolicy
    {
        public const int MinimumParentChildGap = 16;
        public const int MaximumChildAge = 55;
        public const int MinimumPlayerAge = 18;
        public const int MaximumPlayerAge = 80;

        public static int ChildAgeMaximum(int playerAge,int spouseAge)
        {
            int youngestParent=Math.Min(playerAge,spouseAge);
            return Math.Max(0,Math.Min(MaximumChildAge,
                youngestParent-MinimumParentChildGap));
        }

        public static int ClampChildAge(int age,int playerAge,int spouseAge)
        {
            return Math.Max(0,Math.Min(age,
                ChildAgeMaximum(playerAge,spouseAge)));
        }

        public static bool CanChooseChildAge(int age,
            int playerAge,int spouseAge)
        {
            return age>=0 && age<=ChildAgeMaximum(playerAge,spouseAge);
        }

        public static bool ValidPlayerAge(int age)
        {
            return age>=MinimumPlayerAge && age<=MaximumPlayerAge;
        }
    }
}
