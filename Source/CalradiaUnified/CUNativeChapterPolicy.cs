using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Noninteractive native-stage shell chapter sequence. The native engine
    /// still owns each stage, so a chapter label never rewrites stage history.
    /// Do not use this as permission to change ASO after SandBoxGameManager
    /// has constructed its campaign options.
    /// </summary>
    public static class CUNativeChapterPolicy
    {
        public static readonly string[] Chapters =
        {
            "01  YOUR WORLD",
            "02  YOUR CHARACTER",
            "03  YOUR HOUSE",
            "04  YOUR FORTUNE",
            "05  BEGIN YOUR LEGACY"
        };

        public static int ActiveChapter(string nativeStage)
        {
            switch (nativeStage)
            {
                case "CharacterCreationCultureStage":
                case "CharacterCreationFaceGeneratorStage":
                case "CharacterCreationNarrativeStage":
                    return 1;
                case "CharacterCreationBannerEditorStage":
                case "CharacterCreationClanNamingStage":
                    return 2;
                case "CharacterCreationReviewStage":
                case "CharacterCreationOptionsStage":
                    return 4;
                default:
                    return -1;
            }
        }

        public static bool IsLiveNativeChoice(int chapterIndex)
        {
            // World and role are committed before SandBoxGameManager starts.
            // Fortune/family are edited by a dedicated CU layer after clan,
            // never by jumping the game's native stage history.
            return chapterIndex == 1 || chapterIndex == 2 ||
                chapterIndex == 4;
        }

        public static string ChapterStatus(int chapterIndex,
            int activeChapter, bool visited)
        {
            if(chapterIndex==0) return "WORLD CONFIGURED";
            if(chapterIndex==3) return "AFTER NATIVE CLAN";
            if(chapterIndex==activeChapter) return "NATIVE STEP ACTIVE";
            return visited ? "REACHED" : "UPCOMING";
        }
    }

    /// <summary>
    /// Layout target only. SceneLayer v1.5.7 accepts full-screen HitTest
    /// and has no verified per-pixel viewport rectangle API. Returning
    /// a rectangle never claims the native scene was cropped.
    /// </summary>
    public static class CUNativeViewportPolicy
    {
        public static bool HasVerifiedRectangularSceneDock
        {
            // The experimental native FaceGen-only dock is NOT a proven
            // persistent five-chapter viewport.
            get { return false; }
        }

        public const int NativeFacePanelPixels = 694;

        public static bool CanTryNativeFaceDock(int width,int height)
        {
            return width >= 1366 && height >= 768 &&
                width > NativeFacePanelPixels + 500;
        }

        public static float NativeFaceDockLeftFraction(int width)
        {
            if(width <= 0) return 0f;
            return (float)NativeFacePanelPixels / width;
        }

        public static float NativeFaceDockRightFraction(int width)
        {
            return 1f - NativeFaceDockLeftFraction(width);
        }

        public static int NavigationWidth(int screenWidth)
        {
            return screenWidth < 1600 ? 172 : 240;
        }

        public static int ControlsWidth(int screenWidth)
        {
            return screenWidth < 1600 ? 395 : 520;
        }

        public static int ProposedViewportWidth(int screenWidth)
        {
            return Math.Max(0,
                screenWidth - NavigationWidth(screenWidth) -
                ControlsWidth(screenWidth) - 28);
        }

        public static bool IsUsableLayout(int screenWidth, int screenHeight)
        {
            return screenWidth >= 1366 && screenHeight >= 768 &&
                   ProposedViewportWidth(screenWidth) >= 740;
        }

        public static string CapabilityStatus()
        {
            return HasVerifiedRectangularSceneDock ?
                "NATIVE 3D DOCK VERIFIED" :
                "NATIVE 3D SCENE: FULL SCREEN; DOCK NOT YET VERIFIED";
        }
    }
}
