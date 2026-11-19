using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure visual decisions for the EXISTING native CharacterCreationScreen.
    /// Never changes the native stage graph, Hero, banner, FaceGen renderer
    /// or difficulty ViewModels. The real native movie remains interactive.
    /// </summary>
    public static class CUNativeVisualPolicy
    {
        public static bool IsNativeChapterStage(string name)
        {
            switch(name)
            {
                case "CharacterCreationCultureStage":
                case "CharacterCreationFaceGeneratorStage":
                case "CharacterCreationNarrativeStage":
                case "CharacterCreationBannerEditorStage":
                case "CharacterCreationClanNamingStage":
                case "CharacterCreationReviewStage":
                case "CharacterCreationOptionsStage":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsStandaloneNativeChrome(string name)
        {
            // Actual screenshots show the top overlay obscuring the real
            // native Family/Banner/Review/Difficulty titles and menus.
            // Native Gauntlet already owns those stone/wood layouts.
            // Never draw a competing CU header on a game-owned stage.
            return false;
        }

        public static bool ShouldShowChapterRail(string stage,int width)
        {
            // At 1366 the original native text and controls require
            // space. Full rail only on large displays and non-3D stages.
            return IsStandaloneNativeChrome(stage) && width>=1600;
        }

        public static string StageTitle(string name)
        {
            switch(name)
            {
                case "CharacterCreationCultureStage":
                    return "CULTURE";
                case "CharacterCreationFaceGeneratorStage":
                    return "APPEARANCE";
                case "CharacterCreationNarrativeStage":
                    return "YOUR ORIGINS";
                case "CharacterCreationBannerEditorStage":
                    return "BANNER & HERALDRY";
                case "CharacterCreationClanNamingStage":
                    return "YOUR CLAN";
                case "CharacterCreationReviewStage":
                    return "REVIEW YOUR CHARACTER";
                case "CharacterCreationOptionsStage":
                    return "DIFFICULTY & CAMPAIGN RULES";
                default:
                    return "CHARACTER CREATION";
            }
        }

        public static string StageGuidance(string name)
        {
            switch(name)
            {
                case "CharacterCreationCultureStage":
                    return "Select your people. The original culture " +
                        "effects are applied by Bannerlord.";
                case "CharacterCreationFaceGeneratorStage":
                    return "Edit the actual player model with Bannerlord's " +
                        "face, body and age tools.";
                case "CharacterCreationNarrativeStage":
                    return "Choose your background, childhood and " +
                        "formative experiences. Bannerlord keeps the " +
                        "real skill and attribute consequences.";
                case "CharacterCreationBannerEditorStage":
                    return "Design your clan's colors and banner through " +
                        "the original heraldry editor.";
                case "CharacterCreationClanNamingStage":
                    return "Name your native clan. The household and " +
                        "estate questions follow after this stage.";
                case "CharacterCreationReviewStage":
                    return "Review your game-owned character and " +
                        "continue with the original native controls.";
                case "CharacterCreationOptionsStage":
                    return "Set native difficulty and campaign rules. " +
                        "The game's original finalization handles your start.";
                default:
                    return "Bannerlord owns this character-creation step.";
            }
        }

        public static string StepProgress(string name)
        {
            switch(name)
            {
                case "CharacterCreationCultureStage":
                    return "YOUR CHARACTER | CULTURE";
                case "CharacterCreationFaceGeneratorStage":
                    return "YOUR CHARACTER | APPEARANCE";
                case "CharacterCreationNarrativeStage":
                    return "YOUR CHARACTER | LIFE STORY";
                case "CharacterCreationBannerEditorStage":
                    return "YOUR HOUSE | BANNER";
                case "CharacterCreationClanNamingStage":
                    return "YOUR HOUSE | CLAN NAME";
                case "CharacterCreationReviewStage":
                    return "BEGIN YOUR LEGACY | REVIEW";
                case "CharacterCreationOptionsStage":
                    return "BEGIN YOUR LEGACY | DIFFICULTY";
                default:
                    return "CHARACTER CREATION";
            }
        }

        public static string ChapterPosition(string name)
        {
            int index=CUNativeChapterPolicy.ActiveChapter(name);
            return index<0 ? "NATIVE CHARACTER CREATION" :
                "CHAPTER "+(index+1)+" OF 5";
        }

        public static string NextHint(string name)
        {
            switch(name)
            {
                case "CharacterCreationCultureStage":
                    return "NEXT | APPEARANCE";
                case "CharacterCreationFaceGeneratorStage":
                    return "NEXT | LIFE STORY";
                case "CharacterCreationNarrativeStage":
                    return "NEXT | STARTING STATS";
                case "CharacterCreationBannerEditorStage":
                    return "NEXT | CLAN NAME";
                case "CharacterCreationClanNamingStage":
                    return "NEXT | HOUSEHOLD & FORTUNE";
                case "CharacterCreationReviewStage":
                    return "CONTINUE | NATIVE REVIEW";
                case "CharacterCreationOptionsStage":
                    return "CONTINUE | NATIVE FINALIZATION";
                default:
                    return "CONTINUE IN THE NATIVE EDITOR";
            }
        }
    }
}
