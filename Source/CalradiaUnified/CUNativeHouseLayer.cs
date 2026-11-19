using System;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Post-identity family/fortune interstitial. One real game-owned
    /// CharacterCreationScreen, unchanged native banner, clan, review and
    /// difficulty stages. The guided CU movie appears automatically at
    /// first native Review (or Options fallback) after banner and clan.
    /// The native manager retains real Back, Next and finalization.
    /// </summary>
    internal static class CUNativeHouseLayer
    {
        private static CharacterCreationManager trackedManager;
        private static CharacterCreationScreen host;
        private static CharacterCreationScreen failedHost;
        private static GauntletLayer layer;
        private static GauntletMovieIdentifier movie;
        private static bool bannerSeen, clanSeen, completed;
        private static bool closeRequested, backRequested;

        public static void Tick(CharacterCreationScreen screen,
            CharacterCreationManager manager)
        {
            if(screen==null || screen.IsFinalized || manager==null ||
                !SandboxCreatorOverlay.HasAcceptedCustomStart)
            {
                Clear();
                return;
            }
            if(!Object.ReferenceEquals(trackedManager,manager))
            {
                Clear();
                trackedManager=manager;
            }
            string stage=manager.CurrentStage==null ? "" :
                manager.CurrentStage.GetType().Name;

            // Returning to earlier native steps means the player's banner
            // or clan can change. Reconfirm the family and holding request
            // after those new choices rather than leaving stale grants.
            if(stage=="CharacterCreationBannerEditorStage")
            {
                bannerSeen=true;
                completed=false;
                Detach();
                return;
            }
            if(stage=="CharacterCreationClanNamingStage")
            {
                clanSeen=true;
                completed=false;
                Detach();
                return;
            }
            if(stage!="CharacterCreationReviewStage" &&
                stage!="CharacterCreationOptionsStage")
            {
                Detach();
                return;
            }

            if(closeRequested)
            {
                closeRequested=false;
                completed=true;
                Detach();
                DiagnosticJournal.Event("cu_guided_house_completed",
                    "native_review_or_options_resumed=true" +
                    "|extra_native_stage_registered=false");
                return;
            }
            if(backRequested)
            {
                backRequested=false;
                Detach();
                try
                {
                    manager.PreviousStage();
                    DiagnosticJournal.Event("cu_guided_house_back",
                        "native_previous_stage_invoked=true");
                }
                catch(Exception e)
                {
                    DiagnosticJournal.Exception(
                        "cu_guided_house_back_failed",e);
                }
                return;
            }
            if(Object.ReferenceEquals(host,screen))
            {
                if(Input.IsKeyReleased(InputKey.Escape))
                    backRequested=true;
                return;
            }
            if(!bannerSeen || !clanSeen || completed ||
                Object.ReferenceEquals(failedHost,screen))
                return;

            GauntletLayer next=null;
            GauntletMovieIdentifier nextMovie=null;
            try
            {
                var profile=SandboxOriginProfile.Load(
                    SandboxOriginProfile.UserPath);
                if(!profile.Enabled)
                {
                    completed=true;
                    return;
                }
                var vm=new CUGuidedCreatorVM(false,
                    ()=>closeRequested=true,()=>backRequested=true);
                next=new GauntletLayer("CUGuidedHouseFortune",85,false);
                next.IsFocusLayer=true;
                next.InputRestrictions.SetInputRestrictions();
                nextMovie=next.LoadMovie("CUGuidedCreator",vm);
                screen.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=screen;
                layer=next;
                movie=nextMovie;
                failedHost=null;
                DiagnosticJournal.Event("cu_guided_house_attached",
                    "after_banner=true|after_clan=true" +
                    "|before_native_finalization=true" +
                    "|family_after_identity=true|native_stages_intact=true");
            }
            catch(Exception e)
            {
                failedHost=screen;
                completed=true;
                DiagnosticJournal.Exception(
                    "cu_guided_house_attach_failed",e);
                try
                {
                    if(next!=null && !screen.IsFinalized)
                    {
                        if(nextMovie!=null) next.ReleaseMovie(nextMovie);
                        if(screen.HasLayer(next)) screen.RemoveLayer(next);
                    }
                }
                catch(Exception cleanup)
                {
                    DiagnosticJournal.Exception(
                        "cu_guided_house_cleanup_failed",cleanup);
                }
            }
        }

        private static void Detach()
        {
            var oldHost=host;
            var oldLayer=layer;
            var oldMovie=movie;
            host=null;
            layer=null;
            movie=null;
            if(oldHost==null || oldLayer==null)return;
            try
            {
                if(!oldHost.IsFinalized && oldHost.HasLayer(oldLayer))
                {
                    oldLayer.IsFocusLayer=false;
                    ScreenManager.TryLoseFocus(oldLayer);
                    if(oldMovie!=null)oldLayer.ReleaseMovie(oldMovie);
                    oldHost.RemoveLayer(oldLayer);
                    // Return original Bannerlord controls, not a duplicate
                    // native GameStateScreen or a second stage controller.
                    foreach(var candidate in oldHost.Layers)
                    {
                        if(candidate!=oldLayer && candidate.IsFocusLayer)
                        {
                            ScreenManager.TrySetFocus(candidate);
                            break;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_guided_house_detach_failed",e);
            }
        }

        public static void Clear()
        {
            Detach();
            trackedManager=null;
            failedHost=null;
            bannerSeen=false;
            clanSeen=false;
            completed=false;
            closeRequested=false;
            backRequested=false;
        }
    }
}
