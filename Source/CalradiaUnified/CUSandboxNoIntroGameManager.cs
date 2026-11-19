using System;
using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaUnified
{
    /// <summary>
    /// Skip only the native NEW Sandbox campaign_intro.ivf for accepted CU
    /// starts. The engine's original SandBoxGameManager.OnLoadFinished
    /// development-mode path creates the same CharacterCreationState.
    /// No Harmony patch, saved-game interception or global video setting.
    /// All later campaign cutscenes remain game-owned.
    /// </summary>
    internal sealed class CUSandboxNoIntroGameManager : SandBoxGameManager
    {
        internal CUSandboxNoIntroGameManager(
            SandBoxGameManager.CampaignCreatorDelegate creator)
            : base(creator) { }

        public override void OnLoadFinished()
        {
            if(LoadingSavedGame || !SandboxCreatorOverlay.HasAcceptedCustomStart ||
                Game.Current==null ||
                Game.Current.GameStateManager==null)
            {
                base.OnLoadFinished();
                return;
            }
            try
            {
                var state=Game.Current.GameStateManager
                    .CreateState<CharacterCreationState>();
                Game.Current.GameStateManager.CleanAndPushState(state,0);
                base.IsLoaded=true;
                DiagnosticJournal.Event("cu_intro_cinematic_skipped",
                    "new_sandbox_only=true|native_character_creation=true" +
                    "|saves_and_other_cinematics_unchanged=true");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_intro_cinematic_skip_failed",error);
                // If the direct, game-owned character-creation state did
                // not initialize, let SandBoxGameManager use its own intro.
                base.OnLoadFinished();
            }
        }
    }
}
