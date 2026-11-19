using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Limit live Gauntlet widgets per frame. The old single ListPanel
    /// created 54+ sliders at once, paired with a ScrollablePanel that
    /// had never been verified in the actual installed game.
    /// All edits remain on one logical Stats stage, with six visible rows.
    /// </summary>
    public static class SandboxStatsPagePolicy
    {
        public const int VisibleRows=6;
        public static int PageCount(int total)
        {
            return Math.Max(1,(Math.Max(0,total)+VisibleRows-1)/
                VisibleRows);
        }
        public static int ClampPage(int page,int total)
        {
            return Math.Max(0,Math.Min(PageCount(total)-1,page));
        }
        public static int StartIndex(int page,int total)
        {
            return ClampPage(page,total)*VisibleRows;
        }
        public static int PageSize(int page,int total)
        {
            return Math.Max(0,Math.Min(VisibleRows,
                Math.Max(0,total)-StartIndex(page,total)));
        }
    }
}
