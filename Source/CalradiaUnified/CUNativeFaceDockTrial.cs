using System;
using System.Reflection;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// v0.38 opt-in rendering experiment: place the EXISTING native 3D
    /// FaceGen SceneView on the right, and reposition the existing generated
    /// FaceGen Gauntlet controls on the left. It never constructs another
    /// BodyGenerator, scene, camera, character or stage view.
    ///
    /// Engine View.SetScale / SetOffset are used elsewhere by native
    /// MBInitialScreenBase. This does NOT prove that input mapping, camera
    /// FOV or lifetime remains correct. F9 restores the stock layout, and
    /// fresh campaigns keep this experiment OFF by default.
    /// </summary>
    internal static class CUNativeFaceDockTrial
    {
        private static CharacterCreationScreen host;
        private static CharacterCreationScreen failedHost;
        private static SceneLayer nativeScene;
        private static GauntletLayer nativeControls;
        private static Widget nativePanel;
        private static Widget nativeExtensions;
        private static HorizontalAlignment oldPanelAlignment;
        private static float oldPanelLeft, oldPanelRight;
        private static HorizontalAlignment oldExtensionsAlignment;
        private static float oldExtensionsLeft, oldExtensionsRight;
        private static bool docked;
        private static bool enabledThisSession;

        public static string Status
        {
            get
            {
                if (docked)
                    return "REAL 3D FACE: RIGHT DOCK TRIAL | F9 = RESTORE";
                if (host != null)
                    return "NATIVE FACE LAYOUT | F9 = RETRY RIGHT DOCK";
                return "NATIVE FACE FULL SCREEN | RIGHT DOCK OFF";
            }
        }

        public static void Tick(CharacterCreationScreen screen,
            CharacterCreationManager manager, bool requested)
        {
            try
            {
                bool isFace = manager != null && manager.CurrentStage != null &&
                    manager.CurrentStage.GetType().Name ==
                        "CharacterCreationFaceGeneratorStage";
                if (!requested || screen == null || screen.IsFinalized || !isFace)
                {
                    Detach();
                    if (!isFace || screen == null) failedHost = null;
                    return;
                }
                if (!Object.ReferenceEquals(host,screen))
                {
                    Detach();
                    if (Object.ReferenceEquals(failedHost,screen)) return;
                    if (!TryBind(screen))
                    {
                        failedHost = screen;
                        DiagnosticJournal.Event("cu_real_face_dock_fallback",
                            "reason=original_face_layer_or_controls_unavailable");
                        return;
                    }
                    host = screen;
                    enabledThisSession = true;
                    if (!TryApply())
                    {
                        failedHost = screen;
                        Detach();
                    }
                }
                if (Object.ReferenceEquals(host,screen) &&
                    Input.IsKeyReleased(InputKey.F9))
                {
                    if (docked)
                    {
                        Restore();
                        enabledThisSession = false;
                    }
                    else
                    {
                        enabledThisSession = true;
                        if (!TryApply()) enabledThisSession = false;
                    }
                    DiagnosticJournal.Event("cu_real_face_dock_toggle",
                        "right_dock="+docked+"|via=F9");
                }
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception("cu_real_face_dock_failed",ex);
                failedHost = screen;
                Detach();
            }
        }

        private static bool TryBind(CharacterCreationScreen screen)
        {
            var stageField = typeof(CharacterCreationScreen).GetField(
                "_currentStageView", BindingFlags.NonPublic |
                    BindingFlags.Instance);
            var stageView = stageField == null ? null : stageField.GetValue(screen);
            if(stageView == null || stageView.GetType().Name !=
                "CharacterCreationFaceGeneratorView") return false;

            var bodyField = stageView.GetType().GetField(
                "_faceGeneratorView", BindingFlags.NonPublic |
                    BindingFlags.Instance);
            object body = bodyField == null ? null : bodyField.GetValue(stageView);
            if(body == null || body.GetType().Name != "BodyGeneratorView")
                return false;

            nativeScene = body.GetType().GetProperty("SceneLayer")
                .GetValue(body,null) as SceneLayer;
            nativeControls = body.GetType().GetProperty("GauntletLayer")
                .GetValue(body,null) as GauntletLayer;
            if(nativeScene == null || nativeControls == null ||
                !screen.HasLayer(nativeScene) ||
                !screen.HasLayer(nativeControls) ||
                nativeScene.SceneView == null)
                return false;

            var identifier = nativeControls.GetMovieIdentifier("FaceGen");
            var root = identifier == null || identifier.Movie == null ?
                null : identifier.Movie.RootWidget;
            if(root == null) return false;
            int panels=0, extensions=0;
            foreach(Widget item in root.GetAllChildrenRecursive())
            {
                if(item.HorizontalAlignment == HorizontalAlignment.Right &&
                   Math.Abs(item.SuggestedWidth-694f)<1f &&
                   Math.Abs(item.SuggestedHeight-1080f)<1f)
                {
                    nativePanel=item;
                    panels++;
                }
                if(item.HorizontalAlignment == HorizontalAlignment.Right &&
                   Math.Abs(item.SuggestedWidth-83f)<1f &&
                   Math.Abs(item.MarginRight-640f)<1f)
                {
                    nativeExtensions=item;
                    extensions++;
                }
            }
            if(panels != 1 || extensions != 1)
            {
                nativePanel=null;
                nativeExtensions=null;
                return false;
            }
            oldPanelAlignment=nativePanel.HorizontalAlignment;
            oldPanelLeft=nativePanel.MarginLeft;
            oldPanelRight=nativePanel.MarginRight;
            oldExtensionsAlignment=nativeExtensions.HorizontalAlignment;
            oldExtensionsLeft=nativeExtensions.MarginLeft;
            oldExtensionsRight=nativeExtensions.MarginRight;
            return true;
        }

        private static bool TryApply()
        {
            if(host == null || host.IsFinalized || nativeScene == null ||
                !host.HasLayer(nativeScene) || nativeScene.SceneView == null ||
                nativePanel == null || nativeExtensions == null ||
                !enabledThisSession) return false;

            try
            {
                // Exactly one original game-owned SceneView, not a copy.
                // 694 native design pixels reserve the existing face panel.
                float width=Screen.RealScreenResolutionWidth;
                if(width < 1366f) return false;
                float leftFraction=694f / width;
                if(leftFraction >= .6f || leftFraction <= .25f) return false;

                nativePanel.HorizontalAlignment=HorizontalAlignment.Left;
                nativePanel.MarginLeft=0f;
                nativePanel.MarginRight=0f;
                nativeExtensions.HorizontalAlignment=HorizontalAlignment.Left;
                nativeExtensions.MarginLeft=625f;
                nativeExtensions.MarginRight=0f;
                nativeScene.SceneView.SetScale(
                    new Vec2(1f-leftFraction,1f));
                nativeScene.SceneView.SetOffset(
                    new Vec2(leftFraction,0f));
                docked=true;
                DiagnosticJournal.Event("cu_real_face_dock_trial_applied",
                    "real_native_scene=true|left_pixels=694"+
                    "|fraction="+leftFraction.ToString("F4",
                        System.Globalization.CultureInfo.InvariantCulture)+
                    "|input_camera_unverified=true|F9_restore=true");
                return true;
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception("cu_real_face_dock_apply_failed",ex);
                Restore();
                return false;
            }
        }

        private static void Restore()
        {
            if (host != null && !host.IsFinalized)
            {
                try
                {
                    if(nativeScene != null && host.HasLayer(nativeScene) &&
                       nativeScene.SceneView != null)
                    {
                        nativeScene.SceneView.SetScale(new Vec2(1f,1f));
                        nativeScene.SceneView.SetOffset(new Vec2(0f,0f));
                    }
                    if(nativeControls != null &&
                       host.HasLayer(nativeControls))
                    {
                        if(nativePanel != null)
                        {
                            nativePanel.HorizontalAlignment=oldPanelAlignment;
                            nativePanel.MarginLeft=oldPanelLeft;
                            nativePanel.MarginRight=oldPanelRight;
                        }
                        if(nativeExtensions != null)
                        {
                            nativeExtensions.HorizontalAlignment=
                                oldExtensionsAlignment;
                            nativeExtensions.MarginLeft=oldExtensionsLeft;
                            nativeExtensions.MarginRight=oldExtensionsRight;
                        }
                    }
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_real_face_dock_restore_failed",error);
                }
            }
            docked=false;
        }

        private static void Detach()
        {
            if(docked) Restore();
            host=null;
            nativeScene=null;
            nativeControls=null;
            nativePanel=null;
            nativeExtensions=null;
            docked=false;
            enabledThisSession=false;
        }

        public static void Clear()
        {
            Detach();
            failedHost=null;
        }
    }
}
