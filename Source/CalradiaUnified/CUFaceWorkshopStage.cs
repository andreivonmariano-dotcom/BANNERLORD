using System;
using System.Collections.Generic;
using SandBox.GauntletUI.CharacterCreation;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Temporary opt-in bridge for a real, editable 3D player in a CU shell.
    /// This is a UNIQUE stage type. Never bind our view to the native
    /// CharacterCreationFaceGeneratorStage or remove the native stage.
    /// </summary>
    public sealed class CUFaceWorkshopStage : CharacterCreationStageBase
    {
    }

    /// <summary>
    /// Composes the engine face generator, scene and input. No copied 3D model,
    /// no parallel BodyGeneratorView, and no new Hero or BodyProperties save.
    /// Native editor remains the only owner of character face modifications.
    /// </summary>
    [CharacterCreationStageView(typeof(CUFaceWorkshopStage))]
    public sealed class CUFaceWorkshopStageView : CharacterCreationStageViewBase
    {
        private CharacterCreationFaceGeneratorView faceEditor;
        private GauntletLayer summaryLayer;
        private GauntletMovieIdentifier summaryMovie;
        private CUFaceWorkshopVM summaryVM;

        public CUFaceWorkshopStageView(
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
            // The actual, version-pinned native face editor owns the mutable
            // BodyGeneratorView, the preview agent and FaceGenHistory.
            faceEditor = new CharacterCreationFaceGeneratorView(
                manager, affirmativeAction, affirmativeText,
                negativeAction, negativeText, refreshAction,
                getCurrentIndex, getTotalCount, getFurthestIndex, goToIndex);
            try
            {
                summaryVM = new CUFaceWorkshopVM(manager,
                    SandboxOriginProfile.Load(SandboxOriginProfile.UserPath));
                summaryLayer = new GauntletLayer("CUFaceWorkshop", 2, false);
                summaryLayer.IsFocusLayer = false;
                summaryMovie = summaryLayer.LoadMovie("CUFaceWorkshop", summaryVM);
                DiagnosticJournal.Event("cu_face_workshop_view_loaded",
                    "native_body_generator=true|cu_nonfocus_overlay=true" +
                    "|native_input_kept=true|duplicate_face_stage=false");
            }
            catch (Exception ex)
            {
                // The genuine face editor must still work without CU art.
                DiagnosticJournal.Exception("cu_face_workshop_overlay_fallback", ex);
                summaryMovie = null;
                summaryVM = null;
                summaryLayer = null;
            }
        }

        public override void SetGenericScene(TaleWorlds.Engine.Scene scene)
        {
            faceEditor.SetGenericScene(scene);
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            var layers = new List<ScreenLayer>(faceEditor.GetLayers());
            if (summaryLayer != null) layers.Add(summaryLayer);
            return layers;
        }

        public override void Tick(float dt)
        {
            faceEditor.Tick(dt);
        }

        public override int GetVirtualStageCount()
        {
            return faceEditor.GetVirtualStageCount();
        }

        public override void NextStage()
        {
            faceEditor.NextStage();
        }

        public override void PreviousStage()
        {
            faceEditor.PreviousStage();
        }

        public override void GoToIndex(int index)
        {
            faceEditor.GoToIndex(index);
        }

        public override void LoadEscapeMenuMovie()
        {
            faceEditor.LoadEscapeMenuMovie();
        }

        public override void ReleaseEscapeMenuMovie()
        {
            faceEditor.ReleaseEscapeMenuMovie();
        }

        protected override void OnFinalize()
        {
            try
            {
                if (summaryLayer != null && summaryMovie != null)
                    summaryLayer.ReleaseMovie(summaryMovie);
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("cu_face_workshop_overlay_release", ex);
            }
            finally
            {
                summaryMovie = null;
                summaryVM = null;
                summaryLayer = null;
                try
                {
                    if (faceEditor != null)
                        ((ICharacterCreationStageListener)faceEditor)
                            .OnStageFinalize();
                }
                finally
                {
                    faceEditor = null;
                }
            }
        }
    }

    /// <summary>
    /// Read-only CU context. The real face editor is the source of truth.
    /// Family members are created only after native finalization.
    /// </summary>
    public sealed class CUFaceWorkshopVM : ViewModel
    {
        private readonly string role;
        private readonly string culture;
        private readonly string household;

        public CUFaceWorkshopVM(CharacterCreationManager manager,
            SandboxOriginProfile profile)
        {
            role = "Role: " + profile.Role +
                (String.IsNullOrEmpty(profile.StartingKingdomId) ? "" :
                 " | " + profile.StartingKingdomId);
            var selected = manager.CharacterCreationContent.SelectedCulture;
            culture = selected == null ? "Culture: pending" :
                "Culture: " + selected.Name.ToString();
            household = profile.SpouseEnabled ?
                "Family plan: spouse + " + profile.ChildCount + " children" :
                "Family plan: independent";
        }

        [DataSourceProperty]
        public string Title { get { return "CALRADIA UNIFIED | FACE WORKSHOP"; } }

        [DataSourceProperty]
        public string Role { get { return role; } }

        [DataSourceProperty]
        public string Culture { get { return culture; } }

        [DataSourceProperty]
        public string Household { get { return household; } }

        [DataSourceProperty]
        public string Notice
        {
            get
            {
                return "Experimental native 3D editor. Your changes use " +
                    "Bannerlord face data. This is not the unified creator yet.";
            }
        }
    }
}
