using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Reversible in-place skin of ORIGINAL native Gauntlet controls.
    ///
    /// Targeting is strict: the real CharacterCreationScreen._currentStageView
    /// must be one of five pinned game-owned stage views, then the existing
    /// GauntletMovieIdentifier in THAT stage provides Movie.RootWidget.
    /// The original VM, movie, button event delegates, focus, input widgets,
    /// banner assets, 3D scene, and original stage lifecycle are untouched.
    ///
    /// Buttons get a PRIVATE Brush.Clone() with CU colors. This avoids
    /// mutating global native brushes shared across the game. Native text
    /// widgets receive reversible color changes. No new native widgets,
    /// no synthetic action bindings or reparenting. F10 restores originals.
    /// </summary>
    internal static class CUNativeControlSkin
    {
        private sealed class Change
        {
            internal readonly object Target;
            internal readonly PropertyInfo Property;
            internal readonly object Original;
            internal Change(object target,PropertyInfo property,object old)
            {
                Target=target;
                Property=property;
                Original=old;
            }
        }

        private static readonly BindingFlags InstanceFlags=
            BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;

        private static CharacterCreationScreen host;
        private static CharacterCreationManager manager;
        private static object currentView;
        private static Widget currentRoot;
        private static object reportedUnavailable;
        private static readonly List<Change> changes=new List<Change>();
        private static bool enabled=true;
        private static int retryCount;
        private static string activeStage="";
        private static int buttonsStyled;
        private static int textStyled;

        public static void Tick(CharacterCreationScreen screen,
            CharacterCreationManager nativeManager)
        {
            if(screen==null || screen.IsFinalized ||
                nativeManager==null || nativeManager.CurrentStage==null ||
                !SandboxCreatorOverlay.HasAcceptedCustomStart)
            {
                Clear();
                return;
            }
            if(!Object.ReferenceEquals(host,screen) ||
                !Object.ReferenceEquals(manager,nativeManager))
            {
                Clear();
                host=screen;
                manager=nativeManager;
            }

            // The same reversible keyboard escape hatch as the FaceGen
            // F9 dock. F10 touches styling only, never stage navigation.
            if(Input.IsKeyReleased(InputKey.F10))
            {
                enabled=!enabled;
                Restore();
                DiagnosticJournal.Event("cu_native_widget_skin_toggle",
                    "enabled="+enabled+"|via=F10|native_input_retained=true");
            }
            string stage=nativeManager.CurrentStage.GetType().Name;
            if(!CUNativeWidgetSkinPolicy.CanSkin(stage))
            {
                if(currentView!=null) Restore();
                activeStage=stage;
                return;
            }
            if(!enabled) return;

            try
            {
                var field=typeof(CharacterCreationScreen).GetField(
                    "_currentStageView",InstanceFlags);
                object view=field==null ? null : field.GetValue(screen);
                if(view==null ||
                    view.GetType().Name!=
                        CUNativeWidgetSkinPolicy.ExpectedView(stage))
                {
                    if(currentView!=null)Restore();
                    return;
                }

                if(!Object.ReferenceEquals(view,currentView) ||
                    stage!=activeStage)
                {
                    Restore();
                    currentView=view;
                    activeStage=stage;
                    retryCount=0;
                }
                if(currentRoot!=null ||
                    Object.ReferenceEquals(reportedUnavailable,view))
                    return;

                Widget root=GetGameOwnedRoot(view,stage);
                if(root==null)
                {
                    retryCount++;
                    if(retryCount==80)
                    {
                        reportedUnavailable=view;
                        DiagnosticJournal.Event(
                            "cu_native_widget_skin_unavailable",
                            "stage="+stage+
                            "|reason=game_owned_movie_root_missing" +
                            "|original_native_controls_untouched=true");
                    }
                    return;
                }
                Apply(root,stage);
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception("cu_native_widget_skin_failed",e);
                Restore();
                reportedUnavailable=currentView;
            }
        }

        private static Widget GetGameOwnedRoot(object view,string stage)
        {
            // The pinned source stage view is the only source of a native
            // movie root. Do not search all screen layers and accidentally
            // reskin a third-party mod or another CU panel.
            object owner=view;
            if(stage=="CharacterCreationBannerEditorStage")
            {
                var inner=view.GetType().GetField(
                    "_bannerEditorView",InstanceFlags);
                owner=inner==null ? null : inner.GetValue(view);
                if(owner==null)return null;
            }

            // Narrative/Options/Review expose _movie in 1.5.7,
            // ClanNaming exposes _clanNamingStageMovie. Banner editor
            // uses a nested real BannerEditorView, never a replica.
            GauntletMovieIdentifier found=null;
            int count=0;
            for(Type type=owner.GetType();type!=null;type=type.BaseType)
            {
                foreach(FieldInfo field in type.GetFields(InstanceFlags|
                    BindingFlags.DeclaredOnly))
                {
                    if(field.FieldType!=typeof(GauntletMovieIdentifier) ||
                        field.Name.IndexOf("escape",
                            StringComparison.OrdinalIgnoreCase)>=0)
                        continue;
                    var movie=field.GetValue(owner) as
                        GauntletMovieIdentifier;
                    if(movie==null || movie.Movie==null ||
                        movie.Movie.RootWidget==null)
                        continue;
                    found=movie;
                    count++;
                }
            }
            return count==1 ? found.Movie.RootWidget : null;
        }

        private static void Apply(Widget root,string stage)
        {
            var widgets=new List<Widget>();
            widgets.Add(root);
            foreach(Widget child in root.GetAllChildrenRecursive())
            {
                if(widgets.Count>=
                    CUNativeWidgetSkinPolicy.MaxWidgetsPerStage)
                {
                    reportedUnavailable=currentView;
                    DiagnosticJournal.Event("cu_native_widget_skin_unavailable",
                        "stage="+stage+"|reason=native_tree_exceeds_safe_limit");
                    return;
                }
                widgets.Add(child);
            }

            int paintedText=0;
            int paintedButtons=0;
            try
            {
                foreach(Widget widget in widgets)
                {
                    string type=widget.GetType().Name;
                    if(CUNativeWidgetSkinPolicy.IsProtectedWidget(type))
                        continue;
                    if(widget is ButtonWidget)
                    {
                        if(TryPrivateButtonBrush(widget,stage))
                            paintedButtons++;
                    }
                    if(widget is TextWidget)
                    {
                        if(TryNativeTextColor(widget,stage))
                            paintedText++;
                    }
                }
                if(paintedText==0 && paintedButtons==0)
                {
                    reportedUnavailable=currentView;
                    DiagnosticJournal.Event("cu_native_widget_skin_unavailable",
                        "stage="+stage+
                        "|reason=no_writable_native_text_or_button_brush" +
                        "|game_owned_controls_retained=true");
                    return;
                }
                currentRoot=root;
                buttonsStyled=paintedButtons;
                textStyled=paintedText;
                DiagnosticJournal.Event("cu_native_widget_skin_applied",
                    "stage="+stage+
                    "|game_owned_root=true|text="+paintedText+
                    "|private_button_brushes="+paintedButtons+
                    "|total_widgets="+widgets.Count+
                    "|native_click_bindings_retained=true" +
                    "|native_scene_retained=true|F10_restore=true");
            }
            catch
            {
                Restore();
                throw;
            }
        }

        private static bool TryPrivateButtonBrush(Widget widget,string stage)
        {
            // Read the actual brush property from the native button. A
            // shared resource brush is NEVER mutated: clone and replace
            // the button's local instance only if a public setter exists.
            var property=widget.GetType().GetProperty("Brush",
                BindingFlags.Instance|BindingFlags.Public);
            if(property==null || property.GetSetMethod(false)==null ||
                property.PropertyType!=typeof(Brush))
                return false;
            Brush original=property.GetValue(widget,null) as Brush;
            if(original==null)return false;
            Brush own=original.Clone();
            if(own==null || Object.ReferenceEquals(own,original))
                return false;
            own.Color=Hex(CUNativeWidgetSkinPolicy.ButtonTint(stage));
            own.FontColor=Hex(CUNativeWidgetSkinPolicy.TextColor(
                stage,true,false));
            property.SetValue(widget,own,null);
            changes.Add(new Change(widget,property,original));
            return true;
        }

        private static bool TryNativeTextColor(Widget widget,string stage)
        {
            // EditableText, native TextInput, face and banner geometry
            // are excluded by the type allowlist above. Text controls
            // keep the same data-binding and localization source.
            bool insideButton=HasNativeButtonParent(widget);
            bool largeHeading=false;
            var size=widget.GetType().GetProperty("FontSize",
                BindingFlags.Instance|BindingFlags.Public);
            if(size!=null)
            {
                var value=size.GetValue(widget,null);
                largeHeading=value is int && (int)value>=28;
            }
            var next=Hex(CUNativeWidgetSkinPolicy.TextColor(stage,
                insideButton,largeHeading));

            // TextWidget inherits BrushWidget in Bannerlord's Gauntlet
            // tree. Clone its brush locally just like native buttons:
            // setting a shared Brush.FontColor would recolor every screen.
            // The live binding, hover and accessibility identity remain.
            var brushProperty=widget.GetType().GetProperty("Brush",
                BindingFlags.Instance|BindingFlags.Public);
            if(brushProperty!=null &&
                brushProperty.GetSetMethod(false)!=null &&
                brushProperty.PropertyType==typeof(Brush))
            {
                Brush original=brushProperty.GetValue(widget,null) as Brush;
                if(original!=null)
                {
                    Brush local=original.Clone();
                    if(local!=null && !Object.ReferenceEquals(local,original))
                    {
                        local.FontColor=next;
                        brushProperty.SetValue(widget,local,null);
                        changes.Add(new Change(widget,brushProperty,original));
                        return true;
                    }
                }
            }

            // Older or specialized native text widgets can expose an
            // independent Color property instead of a brush setter.
            var property=widget.GetType().GetProperty("Color",
                BindingFlags.Instance|BindingFlags.Public);
            if(property==null || property.GetSetMethod(false)==null ||
                property.PropertyType!=typeof(Color))
                return false;
            object old=property.GetValue(widget,null);
            if(old==null || next.Equals(old))return false;
            property.SetValue(widget,next,null);
            changes.Add(new Change(widget,property,old));
            return true;
        }

        private static bool HasNativeButtonParent(Widget widget)
        {
            // Only use the actual native widget parent hierarchy. Never
            // infer click ownership from names or invent native commands.
            object parent=widget;
            for(int i=0;i<5;i++)
            {
                if(parent==null)break;
                PropertyInfo property=parent.GetType().GetProperty(
                    "ParentWidget",BindingFlags.Instance|BindingFlags.Public);
                if(property==null)break;
                parent=property.GetValue(parent,null);
                if(parent is ButtonWidget)return true;
            }
            return false;
        }

        private static Color Hex(string code)
        {
            if(String.IsNullOrEmpty(code) ||
                code.Length!=9 || code[0]!='#')
                return Color.White;
            var c=CultureInfo.InvariantCulture;
            float r=byte.Parse(code.Substring(1,2),
                NumberStyles.HexNumber,c)/255f;
            float g=byte.Parse(code.Substring(3,2),
                NumberStyles.HexNumber,c)/255f;
            float b=byte.Parse(code.Substring(5,2),
                NumberStyles.HexNumber,c)/255f;
            float a=byte.Parse(code.Substring(7,2),
                NumberStyles.HexNumber,c)/255f;
            return new Color(r,g,b,a);
        }

        private static void Restore()
        {
            for(int i=changes.Count-1;i>=0;i--)
            {
                var item=changes[i];
                try{item.Property.SetValue(item.Target,item.Original,null);}
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_widget_skin_restore_error",error);
                }
            }
            if(changes.Count>0)
                DiagnosticJournal.Event("cu_native_widget_skin_restored",
                    "stage="+activeStage+
                    "|text="+textStyled+
                    "|buttons="+buttonsStyled+
                    "|native_callbacks_untouched=true");
            changes.Clear();
            buttonsStyled=0;
            textStyled=0;
            currentRoot=null;
            currentView=null;
            retryCount=0;
        }

        public static void Clear()
        {
            Restore();
            host=null;
            manager=null;
            reportedUnavailable=null;
            activeStage="";
            enabled=true;
        }
    }
}
