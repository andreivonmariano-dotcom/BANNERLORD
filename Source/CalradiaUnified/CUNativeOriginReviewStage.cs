using System;
using System.Collections.Generic;
using SandBox.GauntletUI.CharacterCreation;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// A distinct OPTIONAL final stage. It NEVER replaces the native
    /// culture/face/narrative/review/options stage or its rendering class.
    /// Only register after native stage graph is complete, with explicit
    /// opt-in in the CU creator and no legacy FaceGen registration.
    /// </summary>
    public sealed class CUNativeOriginReviewStage : CharacterCreationStageBase
    {
    }

    /// <summary>
    /// Reuse the actual native 3D review view and its scene/camera/rotation.
    /// The CU movie is non-interactive and never takes keyboard or mouse
    /// focus from the native review. No fabricated Hero or child preview.
    /// </summary>
    [CharacterCreationStageView(typeof(CUNativeOriginReviewStage))]
    public sealed class CUNativeOriginReviewStageView :
        CharacterCreationReviewStageView
    {
        private GauntletLayer summaryLayer;
        private GauntletMovieIdentifier summaryMovie;
        private CUNativeOriginReviewVM summaryViewModel;

        public CUNativeOriginReviewStageView(
            CharacterCreationManager manager,
            ControlCharacterCreationStage affirmativeAction,
            TextObject affirmativeText,
            ControlCharacterCreationStage negativeAction,
            TextObject negativeText,
            ControlCharacterCreationStage refreshAction,
            ControlCharacterCreationStageReturnInt getCurrentIndex,
            ControlCharacterCreationStageReturnInt getTotalCount,
            ControlCharacterCreationStageReturnInt getFurthestIndex,
            ControlCharacterCreationStageWithInt goToIndex)
            : base(manager,affirmativeAction,affirmativeText,
                negativeAction,negativeText,refreshAction,getCurrentIndex,
                getTotalCount,getFurthestIndex,goToIndex)
        {
            // Keep the existing native review layer focused. The separate
            // CU summary has no commands and must not consume input.
            try
            {
                summaryViewModel = new CUNativeOriginReviewVM(manager,
                    SandboxOriginProfile.Load(SandboxOriginProfile.UserPath));
                summaryLayer = new GauntletLayer("CUOriginNativeReview", 2, false);
                summaryLayer.IsFocusLayer = false;
                summaryMovie = summaryLayer.LoadMovie(
                    "CUNativeOriginReview", summaryViewModel);
                DiagnosticJournal.Event("cu_native_review_view_loaded",
                    "native_player_3d=true|native_review_view_reused=true" +
                    "|non_focus_summary=true|family_previews=planned_only");
            }
            catch (Exception error)
            {
                // This page's base native 3D review is still fully usable
                // if our supplementary Gauntlet prefab fails to load.
                summaryLayer = null;
                summaryMovie = null;
                summaryViewModel = null;
                DiagnosticJournal.Exception(
                    "cu_native_review_summary_fallback",error);
            }
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            var layers = new List<ScreenLayer>(base.GetLayers());
            if (summaryLayer != null) layers.Add(summaryLayer);
            return layers;
        }

        protected override void OnFinalize()
        {
            try
            {
                if (summaryLayer != null && summaryMovie != null)
                    summaryLayer.ReleaseMovie(summaryMovie);
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_review_summary_release",error);
            }
            finally
            {
                summaryMovie = null;
                summaryViewModel = null;
                summaryLayer = null;
                // Always finalize native 3D player visuals and camera.
                base.OnFinalize();
            }
        }
    }

    /// <summary>
    /// Only real native player visuals appear: spouse and children are
    /// generated after character creation finishes, so pre-birth hero
    /// portraits would be misleading and risk duplicate registered Heroes.
    /// </summary>
    public sealed class CUNativeOriginReviewVM : ViewModel
    {
        private readonly string title;
        private readonly string role;
        private readonly string spouse;
        private readonly string children;
        private readonly string world;
        private readonly string nativeCulture;

        public CUNativeOriginReviewVM(CharacterCreationManager manager,
            SandboxOriginProfile profile)
        {
            title = "CALRADIA UNIFIED | YOUR START";
            role = "Role: " + profile.Role +
                (String.IsNullOrEmpty(profile.StartingKingdomId) ? "" :
                "  /  " + profile.StartingKingdomId);
            spouse = profile.SpouseEnabled ?
                "Spouse: " + profile.SpouseName + "  |  " +
                profile.SpouseBackground + "  |  age " + profile.SpouseAge :
                "Spouse: none requested";
            children = profile.SpouseEnabled ?
                "Children: " + profile.ChildCount + " planned" :
                "Children: none requested";
            world = "Home: " + (profile.SpouseEnabled ?
                profile.FamilyHomeMode : "not selected");
            var culture = manager.CharacterCreationContent.SelectedCulture;
            nativeCulture = culture == null ?
                "Native culture: pending" :
                "Native culture: " + culture.Name.ToString();
        }

        [DataSourceProperty] public string Title { get { return title; } }
        [DataSourceProperty] public string Role { get { return role; } }
        [DataSourceProperty] public string Spouse { get { return spouse; } }
        [DataSourceProperty] public string Children { get { return children; } }
        [DataSourceProperty] public string World { get { return world; } }
        [DataSourceProperty] public string NativeCulture
            { get { return nativeCulture; } }
        [DataSourceProperty] public string Notice
        {
            get
            {
                return "Your player model is rendered by Bannerlord's native " +
                    "3D review. Family portraits appear only after actual " +
                    "heroes exist in the campaign.";
            }
        }
    }
}
