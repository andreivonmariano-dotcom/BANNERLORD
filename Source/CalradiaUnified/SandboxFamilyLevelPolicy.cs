namespace CalradiaUnified
{
    /// <summary>
    /// Bound level editing inside Bannerlord's non-sentinel XP range.
    /// The engine uses 2147483647 as an XP threshold past level 62.
    /// </summary>
    public static class SandboxFamilyLevelPolicy
    {
        public const int Minimum=1;
        public const int Maximum=50;
        public const string Key="Level.Character";
        public static bool Valid(int level)
        {
            return level>=Minimum && level<=Maximum;
        }
    }
}
