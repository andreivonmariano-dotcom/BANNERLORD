namespace CalradiaUnified
{
    /// <summary>
    /// Rejoin the native Review stage after its original Back action
    /// skipped the CU House/Fortune interstitial. No new native stages
    /// or managers are allocated. An explicit Back from the CU overlay
    /// is deliberately not intercepted.
    /// </summary>
    internal static class CUNativeHouseNavigationPolicy
    {
        public const string BannerStage=
            "CharacterCreationBannerEditorStage";
        public const string ClanStage=
            "CharacterCreationClanNamingStage";
        public const string ReviewStage=
            "CharacterCreationReviewStage";
        public const string OptionsStage=
            "CharacterCreationOptionsStage";

        public static bool ReviewBackNeedsHouse(string previousStage,
            string currentStage,bool houseWasShown,bool houseCompleted)
        {
            return houseWasShown && houseCompleted &&
                previousStage==ReviewStage &&
                (currentStage==ClanStage || currentStage==BannerStage);
        }

        public static bool CanReplayNativeNext(string stage)
        {
            return stage==ClanStage || stage==BannerStage;
        }
    }
}
