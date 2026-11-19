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

        // Native Hero.Age is a float. A newly applied birthday should be
        // within a fraction of a year of the requested integer, not 22
        // when the player typed 48. This checks game state, not just the UI.
        public static bool MatchesRequestedAge(float actual,int requested)
        {
            return !Single.IsNaN(actual) && !Single.IsInfinity(actual) &&
                requested>=0 && Math.Abs(actual-requested)<0.1f;
        }

        // Map actual native UIContext pointer coordinates to the desired
        // one-year age step. Preserve the user's offset from a grabbed
        // handle so the thumb does not jump on the initial mouse press.
        public static int AgeFromSliderPointer(float trackLeft,
            float trackWidth,float handleWidth,float mouseX,
            float grabOffset)
        {
            float travel=trackWidth-handleWidth;
            if(Single.IsNaN(travel) || Single.IsInfinity(travel) ||
                travel<=0f || Single.IsNaN(mouseX) ||
                Single.IsInfinity(mouseX) ||
                Single.IsNaN(trackLeft) || Single.IsInfinity(trackLeft))
                return MinimumPlayerAge;
            float position=(mouseX-grabOffset-trackLeft)/travel;
            position=Math.Max(0f,Math.Min(1f,position));
            return MinimumPlayerAge+(int)Math.Round(
                position*(MaximumPlayerAge-MinimumPlayerAge),
                MidpointRounding.AwayFromZero);
        }

        public static bool ValidPlayerAge(int age)
        {
            return age>=MinimumPlayerAge && age<=MaximumPlayerAge;
        }
    }
}
