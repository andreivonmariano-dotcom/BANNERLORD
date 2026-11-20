using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// World/role questions run on the already-active main-menu ScreenBase.
    /// A pushed ScreenBase deactivates Bannerlord's animated main menu and
    /// freezes its moving background. Never PushScreen or PopScreen here.
    /// Movie and GauntletLayer are released on a later application tick,
    /// outside a button's Gauntlet callback.
    /// </summary>
    internal static class SandboxCreatorMenuLayer
    {
        private static ScreenBase host;
        private static GauntletLayer layer;
        private static GauntletMovieIdentifier movie;
        private static CUGuidedCreatorVM vm;
        private static bool closeRequested;

        public static bool IsAttached
        {
            get { return host!=null && layer!=null; }
        }

        public static void Open(Action accepted,Action cancelled)
        {
            if(IsAttached)throw new InvalidOperationException(
                "World creator overlay already attached.");
            var active=ScreenManager.TopScreen;
            if(active==null || active.IsFinalized)
                throw new InvalidOperationException(
                    "Animated main menu is not active.");

            GauntletLayer next=null;
            GauntletMovieIdentifier nextMovie=null;
            try
            {
                var nextVm=new CUGuidedCreatorVM(true,accepted,cancelled);
                next=new GauntletLayer(
                    "CalradiaUnifiedWorldOnAnimatedMenu",150,false);
                next.IsFocusLayer=true;
                next.InputRestrictions.SetInputRestrictions();
                try
                {
                    if(CUCreatorArtRegistry.IsAvailable())
                    {
                        try
                        {
                            nextMovie=next.LoadMovie(
                                "CUGuidedCreatorV059Art",nextVm);
                            DiagnosticJournal.Event("cu_native_creator_assets",
                                "screen=World|art=imported_atlas");
                        }
                        catch(Exception artError)
                        {
                            DiagnosticJournal.Exception(
                                "cu_menu_world_art_fallback",artError);
                        }
                    }
                    if(nextMovie==null)
                        nextMovie=next.LoadMovie("CUGuidedCreator",nextVm);
                }
                catch(Exception nativeArtError)
                {
                    DiagnosticJournal.Exception(
                        "cu_menu_world_native_art_fallback",nativeArtError);
                    nextMovie=next.LoadMovie(
                        "CUGuidedCreatorPlainFallback",nextVm);
                }

                active.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=active;
                layer=next;
                movie=nextMovie;
                vm=nextVm;
                closeRequested=false;
                DiagnosticJournal.Event("sandbox_world_menu_overlay_attached",
                    "main_menu_screen_reused=true|push_screen=false"+
                    "|animated_background_uninterrupted_by_new_screen=true");
            }
            catch
            {
                try
                {
                    if(next!=null)
                    {
                        next.IsFocusLayer=false;
                        ScreenManager.TryLoseFocus(next);
                        if(nextMovie!=null)next.ReleaseMovie(nextMovie);
                        if(!active.IsFinalized && active.HasLayer(next))
                            active.RemoveLayer(next);
                    }
                }
                catch(Exception cleanupError)
                {
                    DiagnosticJournal.Exception(
                        "sandbox_world_menu_overlay_open_cleanup_failed",
                        cleanupError);
                }
                throw;
            }
        }

        public static void RequestClose()
        {
            closeRequested=true;
        }

        public static void Tick()
        {
            if(!IsAttached)return;
            if(closeRequested || host.IsFinalized ||
                !Object.ReferenceEquals(ScreenManager.TopScreen,host))
            {
                Detach();
                return;
            }
            if(ScreenManager.FocusedLayer==layer &&
                Input.IsKeyReleased(InputKey.Escape))
            {
                // Follow the same step-aware Back policy as the guided
                // World movie. Only Back at the first question exits.
                if(vm!=null)vm.ExecuteBack();
            }
        }

        public static void Detach()
        {
            var oldHost=host;
            var oldLayer=layer;
            var oldMovie=movie;
            host=null;
            layer=null;
            movie=null;
            vm=null;
            closeRequested=false;
            if(oldHost==null || oldLayer==null)return;
            try
            {
                oldLayer.IsFocusLayer=false;
                ScreenManager.TryLoseFocus(oldLayer);
                if(!oldHost.IsFinalized && oldHost.HasLayer(oldLayer))
                {
                    if(oldMovie!=null)oldLayer.ReleaseMovie(oldMovie);
                    oldHost.RemoveLayer(oldLayer);

                    // Return input focus to the existing animated menu.
                    foreach(var original in oldHost.Layers)
                    {
                        if(original!=oldLayer && original.IsFocusLayer)
                        {
                            ScreenManager.TrySetFocus(original);
                            break;
                        }
                    }
                }
                DiagnosticJournal.Event("sandbox_world_menu_overlay_detached",
                    "existing_menu_preserved=true|screen_pop=false");
            }
            catch(Exception cleanupError)
            {
                DiagnosticJournal.Exception(
                    "sandbox_world_menu_overlay_detach_failed",
                    cleanupError);
            }
        }
    }
}
