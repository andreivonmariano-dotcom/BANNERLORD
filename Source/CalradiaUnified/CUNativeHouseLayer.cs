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
        private static bool houseWasShown;
        private static bool returnToHousePending;
        private static int returnToReviewAttempts;
        private static string previousNativeStage="";

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

            // Review holds the native player-name entry. Its real Back
            // may land on Clan Name or Banner, bypassing the House movie.
            // Detect the game-owned transition, then replay only native
            // Next stages until Review can reopen House on this manager.
            // A Back from our House movie has completed=false and never
            // qualifies, so it still reveals the genuine native stage.
            if(CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                previousNativeStage,stage,houseWasShown,completed))
            {
                returnToHousePending=true;
                returnToReviewAttempts=0;
                completed=false;
                failedHost=null;
                DiagnosticJournal.Event("cu_native_review_back_to_house",
                    "from=native_review|observed_stage="+stage+
                    "|reopen=House_and_Fortune|native_back_retained=true");
            }
            previousNativeStage=stage;
            if(returnToHousePending)
            {
                if(stage==CUNativeHouseNavigationPolicy.ReviewStage)
                {
                    returnToHousePending=false;
                    returnToReviewAttempts=0;
                    DiagnosticJournal.Event("cu_native_review_house_reentry",
                        "destination=House_and_Fortune"+
                        "|native_stage=Review|new_stage_created=false");
                }
                else if(CUNativeHouseNavigationPolicy.CanReplayNativeNext(stage)
                    && returnToReviewAttempts<3)
                {
                    returnToReviewAttempts++;
                    Detach();
                    try
                    {
                        // One game-owned step per tick. Never run this from
                        // a Gauntlet button callback or invent a stage.
                        manager.NextStage();
                        DiagnosticJournal.Event("cu_native_review_house_rejoin",
                            "native_next_stage=true|from="+stage+
                            "|step="+returnToReviewAttempts);
                    }
                    catch(Exception error)
                    {
                        returnToHousePending=false;
                        completed=true;
                        failedHost=screen;
                        DiagnosticJournal.Exception(
                            "cu_native_review_house_rejoin_failed",error);
                    }
                    return;
                }
                else
                {
                    // Unknown stage or three failed progress attempts:
                    // let the original native Back destination work.
                    DiagnosticJournal.Event("cu_native_review_house_fallback",
                        "native_stage="+stage+"|attempts="+
                        returnToReviewAttempts+"|native_navigation_preserved=true");
                    returnToHousePending=false;
                    completed=true;
                    failedHost=screen;
                }
            }

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
                    // Never trap the player in an overlay if the engine
                    // declines Back on this particular native stage.
                    completed=true;
                    failedHost=screen;
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
                try
                {
                    bool customLoaded=false;
                    if(CUCreatorArtRegistry.IsAvailable())
                    {
                        try
                        {
                            nextMovie=next.LoadMovie(
                                "CUGuidedCreatorV059Art",vm);
                            customLoaded=true;
                            DiagnosticJournal.Event(
                                "cu_native_creator_assets",
                                "screen=house|layout=v059b_right_panel"+
                                "|source=cu_imported_atlas");
                        }
                        catch(Exception customArtFailure)
                        {
                            DiagnosticJournal.Exception(
                                "cu_guided_house_custom_art_fallback",
                                customArtFailure);
                        }
                    }
                    if(!customLoaded)
                    {
                        nextMovie=next.LoadMovie("CUGuidedCreator",vm);
                        DiagnosticJournal.Event("cu_native_creator_assets",
                            "screen=house|layout=v059b_right_panel"+
                            "|source=installed_game|art=native");
                    }
                }
                catch(Exception nativeArtError)
                {
                    // A game-owned sprite missing in the current build
                    // must not silently remove the household editor.
                    DiagnosticJournal.Exception(
                        "cu_guided_house_art_fallback",nativeArtError);
                    nextMovie=next.LoadMovie("CUGuidedCreatorPlainFallback",vm);
                    DiagnosticJournal.Event("cu_native_creator_assets",
                        "screen=house|source=cu_plain_fallback"+
                        "|native_character_creation_preserved=true");
                }
                screen.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=screen;
                layer=next;
                movie=nextMovie;
                houseWasShown=true;
                failedHost=null;
                DiagnosticJournal.Event("cu_creator_page_checkpoint",
                    "page=House and Fortune|surface=native_creator|phase=layer_attached");
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
                DiagnosticJournal.Event("cu_guided_house_unavailable",
                    "household_not_confirmed=true|reason=both_prefabs_failed"+
                    "|native_game_screen_preserved=true");
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
            houseWasShown=false;
            returnToHousePending=false;
            returnToReviewAttempts=0;
            previousNativeStage="";
        }
    }
}
