using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Native minors have a recorded home but are NotSpawned and cannot
    /// join parties or be recalled like adult clan heroes. Residency and
    /// physical presence must never be reported as the same state.
    /// </summary>
    public static class SandboxFamilyResidencePolicy
    {
        public const string MinorResident="underage_home_recorded";
        public const string AdultPresent="adult_hero_present";
        public const string AdultUnplaced="adult_hero_not_present";
        public const string WrongResidence="different_residence";

        public static bool EligibleForAdultRecall(bool isMinor,bool isAlive,
            bool isActive,bool disabled,bool prisoner)
        {
            return !isMinor && isAlive && isActive &&
                !disabled && !prisoner;
        }

        public static string Classify(bool isMinor,bool correctHome,
            bool physicallyRegistered)
        {
            if(!correctHome)return WrongResidence;
            if(isMinor)return MinorResident;
            return physicallyRegistered?AdultPresent:AdultUnplaced;
        }

        public static string HumanStatus(string code)
        {
            switch(code)
            {
                case MinorResident:
                    return "At family home (underage; cannot join party)";
                case AdultPresent:
                    return "Present at family home (adult)";
                case AdultUnplaced:
                    return "Adult residence recorded; scene entry unverified";
                default:
                    return "Away from family home";
            }
        }
    }
}
