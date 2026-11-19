using System;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// First native-state compositor slice. Adds one small CU navigation
    /// ribbon to Bannerlord's EXISTING CharacterCreationScreen, leaving
    /// all native culture/face/origin/banner/options layers and their input
    /// untouched. No duplicate GameStateScreen or stage-view registration.
    /// Off by default and available only to a newly accepted CU Sandbox.
    /// </summary>
    internal static class CUNativeStateShell
    {
        private static CharacterCreationScreen host;
        private static CharacterCreationScreen failedHost;
        private static CharacterCreationScreen skippedHost;
        private static GauntletLayer ribbon;
        private static GauntletMovieIdentifier ribbonMovie;
        private static CUNativeStateShellVM viewModel;

        public static void Tick()
        {
            try
            {
                var screen = ScreenManager.TopScreen as CharacterCreationScreen;
                var active = Game.Current == null ||
                    Game.Current.GameStateManager == null ? null :
                    Game.Current.GameStateManager.ActiveState as CharacterCreationState;
                if (screen == null || active == null || screen.IsFinalized)
                {
                    Detach();
                    skippedHost = null;
                    failedHost = null;
                    return;
                }

                if (Object.ReferenceEquals(screen, host))
                {
                    if (viewModel != null)
                        viewModel.Update(active.CharacterCreationManager);
                    return;
                }
                if (Object.ReferenceEquals(screen, failedHost) ||
                    Object.ReferenceEquals(screen, skippedHost))
                    return;

                Detach();
                var profile = SandboxOriginProfile.Load(
                    SandboxOriginProfile.UserPath);
                if (!NativeStateShellPolicy.CanAttach(
                    profile.NativeStateShellEnabled, profile.Enabled,
                    active != null, screen != null,
                    profile.NativeFaceWorkshopEnabled,
                    profile.NativeIntegrationReviewEnabled))
                {
                    skippedHost = screen;
                    return;
                }

                if (active.CharacterCreationManager == null ||
                    active.CharacterCreationManager.CurrentStage == null)
                    return;

                var nextVM = new CUNativeStateShellVM(profile);
                var nextLayer = new GauntletLayer(
                    "CUNativeStateShell", 25, false);
                nextLayer.IsFocusLayer = false;
                GauntletMovieIdentifier nextMovie = null;
                try
                {
                    nextVM.Update(active.CharacterCreationManager);
                    nextMovie = nextLayer.LoadMovie(
                        "CUNativeStateShell", nextVM);
                    screen.AddLayer(nextLayer);
                }
                catch
                {
                    // If LoadMovie or AddLayer fails, do not leave a
                    // partially attached layer on the native screen.
                    try
                    {
                        if (!screen.IsFinalized &&
                            screen.HasLayer(nextLayer))
                            screen.RemoveLayer(nextLayer);
                    }
                    catch (Exception cleanupError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_native_shell_attach_cleanup",cleanupError);
                    }
                    throw;
                }

                host = screen;
                failedHost = null;
                ribbon = nextLayer;
                ribbonMovie = nextMovie;
                viewModel = nextVM;
                DiagnosticJournal.Event("cu_native_state_shell_attached",
                    "native_state=true|native_screen_unchanged=true" +
                    "|real_face_engine=retained|readonly=true" +
                    "|separate_native_pages_still_visible=true");
            }
            catch (Exception error)
            {
                // A ribbon failure never blocks the original native editor.
                DiagnosticJournal.Exception("cu_native_state_shell_failed",error);
                failedHost = ScreenManager.TopScreen as CharacterCreationScreen;
                Detach();
            }
        }

        private static void Detach()
        {
            var oldHost = host;
            var oldLayer = ribbon;
            var oldMovie = ribbonMovie;
            host = null;
            ribbon = null;
            ribbonMovie = null;
            viewModel = null;
            if (oldHost == null || oldLayer == null)
                return;
            try
            {
                // ScreenBase.HandleFinalize owns all added layers after
                // game state removal. Never finalize the same layer twice.
                if (!oldHost.IsFinalized && oldHost.HasLayer(oldLayer))
                {
                    if (oldMovie != null)
                        oldLayer.ReleaseMovie(oldMovie);
                    oldHost.RemoveLayer(oldLayer);
                }
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception("cu_native_shell_detach",error);
            }
        }

        public static void Clear()
        {
            Detach();
            failedHost = null;
            skippedHost = null;
        }
    }

    /// <summary>
    /// A noninteractive summary bound to the real live native creation state.
    /// Selected culture comes from CharacterCreationContent, not from a
    /// decorative profile. Does not create or mutate a Hero.
    /// </summary>
    public sealed class CUNativeStateShellVM : ViewModel
    {
        private readonly string role;
        private readonly string household;
        private string chapter = "CALRADIA UNIFIED";
        private string nativeStep = "NATIVE CHARACTER CREATION";
        private string culture = "CULTURE: PENDING";

        public CUNativeStateShellVM(SandboxOriginProfile profile)
        {
            role = "ROLE: " + profile.Role +
                (String.IsNullOrEmpty(profile.StartingKingdomId) ?
                    "" : " | " + profile.StartingKingdomId);
            household = profile.SpouseEnabled ?
                "FAMILY PLAN: SPOUSE + " + profile.ChildCount +
                    " CHILDREN" : "FAMILY PLAN: INDEPENDENT";
        }

        public void Update(CharacterCreationManager manager)
        {
            if (manager == null) return;
            var stage = manager.CurrentStage;
            string stageName = stage == null ? "" :
                stage.GetType().Name;
            string chapterNext =
                NativeStateShellPolicy.ChapterForStage(stageName);
            string stepNext =
                NativeStateShellPolicy.DetailForStage(stageName);
            var selected = manager.CharacterCreationContent == null ?
                null : manager.CharacterCreationContent.SelectedCulture;
            string cultureNext = selected == null ?
                "CULTURE: PENDING" :
                "CULTURE: " + selected.Name.ToString();
            if (chapterNext != chapter)
            {
                chapter = chapterNext;
                OnPropertyChangedWithValue(chapter,"Chapter");
            }
            if (stepNext != nativeStep)
            {
                nativeStep = stepNext;
                OnPropertyChangedWithValue(nativeStep,"NativeStep");
            }
            if (cultureNext != culture)
            {
                culture = cultureNext;
                OnPropertyChangedWithValue(culture,"Culture");
            }
        }

        [DataSourceProperty]
        public string Title { get { return "CALRADIA UNIFIED | NATIVE BRIDGE"; } }

        [DataSourceProperty]
        public string Chapter { get { return chapter; } }

        [DataSourceProperty]
        public string NativeStep { get { return nativeStep; } }

        [DataSourceProperty]
        public string Culture { get { return culture; } }

        [DataSourceProperty]
        public string Role { get { return role; } }

        [DataSourceProperty]
        public string Household { get { return household; } }

        [DataSourceProperty]
        public string Notice
        {
            get
            {
                return "Native controls remain active. CU shell is a " +
                    "read-only test, not yet one-screen creation.";
            }
        }
    }
}
