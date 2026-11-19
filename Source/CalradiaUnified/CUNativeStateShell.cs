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
        private static bool dockRequested;

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
                    CUNativeCultureLayer.Clear();
                    CUNativeFaceDockTrial.Clear();
                    CUNativeStatsLayer.Clear();
                    Detach();
                    skippedHost = null;
                    failedHost = null;
                    return;
                }

                if (Object.ReferenceEquals(screen, host))
                {
                    if (viewModel != null)
                        viewModel.Update(active.CharacterCreationManager,screen);
                    CUNativeCultureLayer.Tick(screen,
                        active.CharacterCreationManager);
                    CUNativeFaceDockTrial.Tick(screen,
                        active.CharacterCreationManager,dockRequested);
                    CUNativeStatsLayer.Tick(screen,
                        active.CharacterCreationManager);
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
                    nextVM.Update(active.CharacterCreationManager,screen);
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
                dockRequested = profile.NativeFaceDockTrialEnabled;
                CUNativeCultureLayer.Tick(screen,
                    active.CharacterCreationManager);
                CUNativeFaceDockTrial.Tick(screen,
                    active.CharacterCreationManager,dockRequested);
                CUNativeStatsLayer.Tick(screen,
                    active.CharacterCreationManager);
                if (Object.ReferenceEquals(host,screen))
                    DiagnosticJournal.Event("cu_native_state_shell_attached",
                        "native_state=true|native_screen_unchanged=true" +
                        "|real_face_engine=retained|native_vm_used=true" +
                        "|separate_native_pages_still_visible=true");
            }
            catch (Exception error)
            {
                // A ribbon failure never blocks the original native editor.
                DiagnosticJournal.Exception("cu_native_state_shell_failed",error);
                failedHost = ScreenManager.TopScreen as CharacterCreationScreen;
                CUNativeCultureLayer.Clear();
                CUNativeFaceDockTrial.Clear();
                Detach();
            }
        }

        internal static void UseNativeFallback(
            CharacterCreationScreen nativeScreen)
        {
            // Recovery is performed on a later application tick, never
            // inside Gauntlet's click callback. Suppress ALL CU movies for
            // this native screen so the game owns exit/finalization alone.
            CUNativeStatsLayer.Clear();
            Detach();
            skippedHost=nativeScreen;
            DiagnosticJournal.Event("cu_native_culture_full_fallback",
                "native_culture_screen_untouched=true" +
                "|cu_layers_released=true");
        }

        private static void Detach()
        {
            CUNativeStatsLayer.Clear();
            CUNativeFaceDockTrial.Clear();
            dockRequested = false;
            CUNativeCultureLayer.Clear();
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
        private bool isFaceStage;
        private string faceStatus = "";
        private string dockStatus = "";
        private readonly bool faceDockRequested;
        private bool cultureCommitObserved;
        private readonly bool[] chaptersReached = new bool[5];
        private int activeChapterIndex = -1;

        [DataSourceProperty]
        public MBBindingList<CUNativeChapterBadgeVM> Chapters { get; private set; }

        [DataSourceProperty]
        public string ViewportCapability
        {
            get { return CUNativeViewportPolicy.CapabilityStatus(); }
        }

        public CUNativeStateShellVM(SandboxOriginProfile profile)
        {
            faceDockRequested = profile.NativeFaceDockTrialEnabled;
            Chapters = new MBBindingList<CUNativeChapterBadgeVM>();
            for (int i=0;i<CUNativeChapterPolicy.Chapters.Length;i++)
            {
                Chapters.Add(new CUNativeChapterBadgeVM(
                    CUNativeChapterPolicy.Chapters[i],
                    i==3 ? "START OPTIONS SET" : "UPCOMING"));
            }
            role = "ROLE: " + profile.Role +
                (String.IsNullOrEmpty(profile.StartingKingdomId) ?
                    "" : " | " + profile.StartingKingdomId);
            household = profile.SpouseEnabled ?
                "FAMILY PLAN: SPOUSE + " + profile.ChildCount +
                    " CHILDREN" : "FAMILY PLAN: INDEPENDENT";
        }

        public void Update(CharacterCreationManager manager,
            CharacterCreationScreen screen)
        {
            if (manager == null) return;
            string nextDockStatus = CUNativeFaceDockTrial.Status;
            if (dockStatus != nextDockStatus)
            {
                dockStatus = nextDockStatus;
                OnPropertyChangedWithValue(dockStatus,"FaceDockStatus");
            }
            var stage = manager.CurrentStage;
            string stageName = stage == null ? "" :
                stage.GetType().Name;
            string chapterNext =
                NativeStateShellPolicy.ChapterForStage(stageName);
            int chapterIndex=CUNativeChapterPolicy.ActiveChapter(stageName);
            if(chapterIndex>=0 && chapterIndex<Chapters.Count)
            {
                chaptersReached[chapterIndex]=true;
                activeChapterIndex=chapterIndex;
            }
            for(int i=0;i<Chapters.Count;i++)
            {
                Chapters[i].IsSelected=(i==activeChapterIndex);
                Chapters[i].Status=CUNativeChapterPolicy.ChapterStatus(
                    i,activeChapterIndex,chaptersReached[i]);
            }
            string stepNext =
                NativeStateShellPolicy.DetailForStage(stageName);
            var selected = manager.CharacterCreationContent == null ?
                null : manager.CharacterCreationContent.SelectedCulture;
            string cultureNext = selected == null ?
                "CULTURE: PENDING" :
                "CULTURE: " + selected.Name.ToString();
            if (stageName=="CharacterCreationFaceGeneratorStage" &&
                selected!=null && !cultureCommitObserved)
            {
                cultureCommitObserved=true;
                DiagnosticJournal.Event("cu_native_culture_commit_observed",
                    "real_selected_culture="+selected.StringId+
                    "|current_stage=CharacterCreationFaceGeneratorStage" +
                    "|native_manager=true");
            }
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
            bool face=stageName=="CharacterCreationFaceGeneratorStage";
            if(face!=isFaceStage)
            {
                isFaceStage=face;
                faceStatus="";
                OnPropertyChangedWithValue(isFaceStage,"IsFaceStage");
                OnPropertyChangedWithValue(!isFaceStage,"IsNotFaceStage");
                OnPropertyChangedWithValue(IsRailVisible,"IsRailVisible");
                OnPropertyChangedWithValue(IsFaceDebugVisible,"IsFaceDebugVisible");
                OnPropertyChangedWithValue(IsFaceDockTrialStage,"IsFaceDockTrialStage");
            }
            if(face && faceStatus!="REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE")
            {
                string observed;
                CUNativeFaceInspector.TryInspect(screen,out observed);
                if(faceStatus!=observed)
                {
                    faceStatus=observed;
                    OnPropertyChangedWithValue(faceStatus,"FaceStatus");
                    if(observed=="REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE")
                        DiagnosticJournal.Event("cu_native_face_layers_verified",
                            "native_scene=true|native_body_generator=true"+
                            "|native_gauntlet_controls=true|relocated=false");
                }
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
        public bool IsFaceStage { get { return isFaceStage; } }

        [DataSourceProperty]
        public bool IsNotFaceStage { get { return !isFaceStage; } }

        [DataSourceProperty]
        public bool IsRailVisible { get { return !isFaceStage || !faceDockRequested; } }

        [DataSourceProperty]
        public bool IsFaceDebugVisible { get { return isFaceStage && !faceDockRequested; } }

        [DataSourceProperty]
        public bool IsFaceDockTrialStage { get { return isFaceStage && faceDockRequested; } }

        [DataSourceProperty]
        public string FaceStatus { get { return faceStatus; } }

        [DataSourceProperty]
        public string FaceDockStatus { get { return dockStatus; } }

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

    /// <summary>
    /// View-only chapter indicator. Do not expose ExecuteSelect: jumping
    /// native creation stages would skip required OnConsequence callbacks.
    /// </summary>
    public sealed class CUNativeChapterBadgeVM : ViewModel
    {
        private readonly string label;
        private string status;
        private bool isSelected;

        public CUNativeChapterBadgeVM(string label,string status)
        {
            this.label=label;
            this.status=status;
        }

        [DataSourceProperty]
        public string Label { get { return label; } }

        [DataSourceProperty]
        public string Status
        {
            get { return status; }
            set
            {
                if(status==value) return;
                status=value;
                OnPropertyChangedWithValue(status,"Status");
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if(isSelected==value) return;
                isSelected=value;
                OnPropertyChangedWithValue(isSelected,"IsSelected");
            }
        }
    }
}
