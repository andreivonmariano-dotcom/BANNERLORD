using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Explicit allowlist for style-only changes to native game-owned
    /// Gauntlet widget trees. No StageBase replacement or VM callback
    /// rewriting. The policy never authorizes culture or FaceGen mutation.
    /// </summary>
    public static class CUNativeWidgetSkinPolicy
    {
        public const int MaxWidgetsPerStage = 600;

        public static bool CanSkin(string stage)
        {
            return stage=="CharacterCreationNarrativeStage" ||
                stage=="CharacterCreationBannerEditorStage" ||
                stage=="CharacterCreationClanNamingStage" ||
                stage=="CharacterCreationReviewStage" ||
                stage=="CharacterCreationOptionsStage";
        }

        public static string ExpectedView(string stage)
        {
            switch(stage)
            {
                case "CharacterCreationNarrativeStage":
                    return "CharacterCreationNarrativeStageView";
                case "CharacterCreationBannerEditorStage":
                    return "CharacterCreationBannerEditorView";
                case "CharacterCreationClanNamingStage":
                    return "CharacterCreationClanNamingStageView";
                case "CharacterCreationReviewStage":
                    return "CharacterCreationReviewStageView";
                case "CharacterCreationOptionsStage":
                    return "CharacterCreationOptionsStageView";
                default:return "";
            }
        }

        public static bool CanSkinClanTextInput(string stage,string typeName)
        {
            return stage=="CharacterCreationClanNamingStage" &&
                !String.IsNullOrEmpty(typeName) &&
                (typeName.IndexOf("EditableText",
                    StringComparison.OrdinalIgnoreCase)>=0 ||
                 typeName.IndexOf("InputText",
                    StringComparison.OrdinalIgnoreCase)>=0);
        }

        public static bool CanSkinDifficultySlider(string stage,string typeName)
        {
            return stage=="CharacterCreationOptionsStage" &&
                !String.IsNullOrEmpty(typeName) &&
                typeName.IndexOf("Slider",
                    StringComparison.OrdinalIgnoreCase)>=0;
        }

        public static bool IsProtectedWidget(string typeName)
        {
            if(String.IsNullOrEmpty(typeName))return true;
            // Never recolor native inputs, the real 3D viewport or
            // banner symbol geometry. Only text and buttons are targeted.
            return typeName.IndexOf("Input",StringComparison.OrdinalIgnoreCase)>=0 ||
                typeName.IndexOf("Editable",StringComparison.OrdinalIgnoreCase)>=0 ||
                typeName.IndexOf("Scene",StringComparison.OrdinalIgnoreCase)>=0 ||
                typeName.IndexOf("ImageIdentifier",StringComparison.OrdinalIgnoreCase)>=0 ||
                typeName.IndexOf("Banner",StringComparison.OrdinalIgnoreCase)>=0 ||
                typeName.IndexOf("Slider",StringComparison.OrdinalIgnoreCase)>=0;
        }

        public static string TextColor(string stage,bool insideButton,
            bool largeHeading)
        {
            if(!CanSkin(stage))return "";
            if(insideButton)return "#F1DEBEFF";
            if(largeHeading)return "#F6D7A5FF";
            return "#D5E0E9FF";
        }

        public static string ButtonTint(string stage)
        {
            return CanSkin(stage)?"#D7B17AFF":"";
        }

        public static string PaletteName
            {get{return "CU Slate / Brass / Parchment";}}
    }
}
