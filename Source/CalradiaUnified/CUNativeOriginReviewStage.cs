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
        CharacterCreationStageViewBase
    {
        // Composition is deliberate. The native review view itself carries
        // [CharacterCreationStageView(typeof(CharacterCreationReviewStage))].
        // Inheriting it can expose the inherited attribute during
        // CharacterCreationScreen's inherit:true reflection scan.
        // Keeping this class directly under the attribute-free BASE avoids
        // ever rebinding or competing with the game's own Review view.
        private CharacterCreationReviewStageView nativeReview;
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
            : base(affirmativeAction, negativeAction, refreshAction,
                getCurrentIndex, getTotalCount, getFurthestIndex, goToIndex)
        {
            // The engine-owned native review constructs and manages its
            // REAL 3D player AgentVisuals, scene, camera and all input.
            nativeReview = new CharacterCreationReviewStageView(
                manager, affirmativeAction, affirmativeText,
                negativeAction, negativeText, refreshAction,
                getCurrentIndex, getTotalCount, getFurthestIndex, goToIndex);
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
                    "|stage_registration=unique_noninherited" +
                    "|non_focus_summary=true|family_previews=planned_only");
            }
            catch (Exception error)
            {
                // The native review must remain navigable even if our
                // supplementary XML has invalid brushes or unavailable fonts.
                summaryLayer = null;
                summaryMovie = null;
                summaryViewModel = null;
                DiagnosticJournal.Exception(
                    "cu_native_review_summary_fallback", error);
            }
        }

        public override void SetGenericScene(TaleWorlds.Engine.Scene scene)
        {
            nativeReview.SetGenericScene(scene);
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            var layers = new List<ScreenLayer>(nativeReview.GetLayers());
            if (summaryLayer != null) layers.Add(summaryLayer);
            return layers;
        }

        public override int GetVirtualStageCount()
        {
            return nativeReview.GetVirtualStageCount();
        }

        public override void Tick(float dt)
        {
            nativeReview.Tick(dt);
        }

        public override void NextStage()
        {
            nativeReview.NextStage();
        }

        public override void PreviousStage()
        {
            nativeReview.PreviousStage();
        }

        public override void GoToIndex(int index)
        {
            nativeReview.GoToIndex(index);
        }

        public override void LoadEscapeMenuMovie()
        {
            nativeReview.LoadEscapeMenuMovie();
        }

        public override void ReleaseEscapeMenuMovie()
        {
            nativeReview.ReleaseEscapeMenuMovie();
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
                    "cu_native_review_summary_release", error);
            }
            finally
            {
                summaryMovie = null;
                summaryViewModel = null;
                summaryLayer = null;
                try
                {
                    // The native view's actual protected OnFinalize is
                    // dispatched through its public stage-listener contract.
                    // This clears its own AgentVisuals and scene layers.
                    if (nativeReview != null)
                        ((ICharacterCreationStageListener)nativeReview)
                            .OnStageFinalize();
                }
                finally
                {
                    nativeReview = null;
                }
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
                return "Real 3D player model. Family portraits unlock " +
                    "in towns and castles after starting.";
            }
        }
    }
}
