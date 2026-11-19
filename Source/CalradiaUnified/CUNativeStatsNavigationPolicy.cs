namespace CalradiaUnified
{
    /// <summary>
    /// Two-screen native boundary: the verified age controls first,
    /// then ONE scrollable page of all attributes, skills, focus and
    /// traits with a player/spouse selection. Native banner follows.
    /// The old individual category pages are deliberately not used.
    /// </summary>
    public static class CUNativeStatsNavigationPolicy
    {
        public const int Age=-1;
        public const int Attributes=0;
        // Retain legacy indices as internal identifiers only; no
        // caller is permitted to navigate to these separate pages.
        public const int Skills=1;
        public const int Traits=2;
        public const int PageCount=2;
        public static bool IsValidPage(int page)
            {return page==Age || page==Attributes;}
        public static int StepNumber(int page)
            {return page==Age?1:page==Attributes?2:0;}
        public static int BackPage(int page)
            {return page==Attributes?Age:Age-1;}
        public static int NextPage(int page)
            {return page==Age?Attributes:Attributes+1;}
        public static bool BackToNativeNarrative(int page)
            {return page==Age;}
        public static bool IsFinal(int page)
            {return page==Attributes;}
        public static bool IsDraftPage(int page)
            {return page==Attributes;}
        public static string NextLabel(int page)
            {return page==Age?"Next: All Stats":
                page==Attributes?"Confirm & Continue":"Continue";}
    }
}
