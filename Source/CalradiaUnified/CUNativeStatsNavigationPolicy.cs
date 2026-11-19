namespace CalradiaUnified
{
    /// <summary>
    /// Fixed four-page Chapter II creator sequence. A user's choice of
    /// Native/Balanced/Unrestricted controls edits, never navigation.
    /// Stage edge -1 is the original game-owned Narrative Back.
    /// </summary>
    public static class CUNativeStatsNavigationPolicy
    {
        public const int Age=-1;
        public const int Attributes=0;
        public const int Skills=1;
        public const int Traits=2;
        public const int PageCount=4;

        public static bool IsValidPage(int page)
        {
            return page>=Age && page<=Traits;
        }
        public static int StepNumber(int page)
        {
            return IsValidPage(page)?page+2:0;
        }
        public static int BackPage(int page)
        {
            return IsValidPage(page)?page-1:Age-1;
        }
        public static int NextPage(int page)
        {
            return IsValidPage(page)?page+1:Traits+1;
        }
        public static bool BackToNativeNarrative(int page)
        {
            return page==Age;
        }
        public static bool IsFinal(int page)
        {
            return page==Traits;
        }
        public static bool IsDraftPage(int page)
        {
            return page==Attributes || page==Skills || page==Traits;
        }
        public static string NextLabel(int page)
        {
            switch(page)
            {
                case Age: return "Next: Attributes";
                case Attributes: return "Next: Skills";
                case Skills: return "Next: Traits";
                case Traits: return "Confirm & Continue";
                default: return "Continue";
            }
        }
    }
}
