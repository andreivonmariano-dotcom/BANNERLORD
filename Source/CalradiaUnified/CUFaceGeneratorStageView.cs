using System;
using System.Collections.Generic;
using SandBox.GauntletUI.CharacterCreation;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// First in-game CU appearance prototype. The REAL Bannerlord face scene,
    /// BodyGeneratorView, focus, FaceGenVM, history and finalization stay native.
    /// A separate read-only CU heading is displayed only by explicit opt-in.
    /// No stage deletion or hand-rolled body key is involved.
    /// </summary>
    [CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))]
    public sealed class CUFaceGeneratorStageView : CharacterCreationFaceGeneratorView
    {
        private GauntletLayer cuHeader;
        private GauntletMovieIdentifier cuMovie;
        private CUFaceHeaderViewModel cuData;

        public CUFaceGeneratorStageView(
            CharacterCreationManager manager,
            ControlCharacterCreationStage affirmative,
            TextObject affirmativeText,
            ControlCharacterCreationStage negative,
            TextObject negativeText,
            ControlCharacterCreationStage refresh,
            ControlCharacterCreationStageReturnInt currentIndex,
            ControlCharacterCreationStageReturnInt totalStages,
            ControlCharacterCreationStageReturnInt furthestIndex,
            ControlCharacterCreationStageWithInt goToIndex)
            : base(manager, affirmative, affirmativeText, negative,
                negativeText, refresh, currentIndex, totalStages,
                furthestIndex, goToIndex)
        {
            try
            {
                var profile = SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
                if (!profile.Enabled || !profile.NativeStageEnabled) return;
                cuHeader = new GauntletLayer("CUFaceStageHeader", 2)
                {
                    IsFocusLayer = false
                };
                cuData = new CUFaceHeaderViewModel();
                cuMovie = cuHeader.LoadMovie("CUFaceStageHeader", cuData);
                DiagnosticJournal.Event("cu_face_stage_native_wrapped",
                    "native_body_generator_preserved=true|heading_loaded=true");
            }
            catch (Exception exception)
            {
                DiagnosticJournal.Exception("cu_face_stage_heading_failed",
                    exception);
                // Never block the native BodyGenerator on a CU heading error.
                cuHeader = null;
                cuMovie = null;
                cuData = null;
            }
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            foreach (var layer in base.GetLayers())
                yield return layer;
            if (cuHeader != null) yield return cuHeader;
        }

        protected override void OnFinalize()
        {
            if (cuHeader != null && cuMovie != null)
            {
                try { cuHeader.ReleaseMovie(cuMovie); }
                catch (Exception e)
                {
                    DiagnosticJournal.Exception("cu_face_header_release", e);
                }
            }
            cuHeader = null;
            cuMovie = null;
            cuData = null;
            base.OnFinalize();
        }
    }

    public sealed class CUFaceHeaderViewModel : ViewModel
    {
        [DataSourceProperty]
        public string Title { get { return "CALRADIA UNIFIED | APPEARANCE"; } }
        [DataSourceProperty]
        public string Hint
        {
            get
            {
                return "Live Bannerlord model | edit face and body using the native controls";
            }
        }
    }
}
