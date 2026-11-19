using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure, game-independent mapping of native v1.5.7 stages into the
    /// five frozen CU chapters. Never changes character creation progress.
    /// </summary>
    public static class NativeStateShellPolicy
    {
        public static string ChapterForStage(string nativeType)
        {
            switch (nativeType)
            {
                case "CharacterCreationCultureStage":
                case "CharacterCreationFaceGeneratorStage":
                case "CUFaceWorkshopStage":
                    return "01  STARTING LIFE";
                case "CharacterCreationBannerEditorStage":
                case "CharacterCreationClanNamingStage":
                    return "02  BLOODLINE";
                case "CharacterCreationNarrativeStage":
                    return "03  FORTUNE";
                case "CharacterCreationReviewStage":
                case "CharacterCreationOptionsStage":
                case "CUNativeOriginReviewStage":
                    return "05  REVIEW YOUR HOUSE";
                default:
                    return "CALRADIA UNIFIED";
            }
        }

        public static string DetailForStage(string nativeType)
        {
            switch (nativeType)
            {
                case "CharacterCreationCultureStage":
                    return "NATIVE CULTURE | LIVE GAME CHOICE";
                case "CharacterCreationFaceGeneratorStage":
                case "CUFaceWorkshopStage":
                    return "NATIVE 3D FACE | REAL PLAYER";
                case "CharacterCreationNarrativeStage":
                    return "NATIVE ORIGINS | REAL BONUSES";
                case "CharacterCreationBannerEditorStage":
                    return "NATIVE BANNER | REAL CLAN COLORS";
                case "CharacterCreationClanNamingStage":
                    return "NATIVE CLAN NAME | LIVE GAME VALUE";
                case "CharacterCreationReviewStage":
                case "CUNativeOriginReviewStage":
                    return "NATIVE CHARACTER REVIEW";
                case "CharacterCreationOptionsStage":
                    return "NATIVE DIFFICULTY | FINALIZATION";
                default:
                    return "NATIVE CHARACTER CREATION";
            }
        }

        public static bool CanAttach(bool explicitOptIn, bool profileEnabled,
            bool nativeStateActive, bool onNativeScreen, bool conflictingWorkshop,
            bool conflictingReview)
        {
            return explicitOptIn && profileEnabled && nativeStateActive &&
                onNativeScreen && !conflictingWorkshop && !conflictingReview;
        }

        public static bool UsesNativeLiveFace(string nativeType)
        {
            return nativeType == "CharacterCreationFaceGeneratorStage" ||
                   nativeType == "CUFaceWorkshopStage";
        }
    }
}
