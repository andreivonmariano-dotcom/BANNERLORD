using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Geometry-aware, reversible repositioning of ORIGINAL native Gauntlet
    /// widget panels and Back/Continue buttons. No stage ViewModel rewrite,
    /// no re-parenting, and no movement of native FaceGen/Banner 3D scenes,
    /// thumbnails or heraldry color/symbol geometry.
    ///
    /// Every move requires ACTUAL game-owned global bounds and a root
    /// whose size matches the current unscaled screen. If a real game stage
    /// lacks the measurements or cannot fit safely in the CU content region,
    /// leave that native stage layout unchanged and report why.
    /// </summary>
    internal static class CUNativeLayoutAlignment
    {
        private sealed class SavedOffset
        {
            internal readonly Widget Widget;
            internal readonly PropertyInfo XProperty;
            internal readonly PropertyInfo YProperty;
            internal readonly object OriginalX;
            internal readonly object OriginalY;
            internal SavedOffset(Widget widget,PropertyInfo x,
                PropertyInfo y,object oldX,object oldY)
            {
                Widget=widget;
                XProperty=x;
                YProperty=y;
                OriginalX=oldX;
                OriginalY=oldY;
            }
        }

        private struct Rect
        {
            internal float X;
            internal float Y;
            internal float Width;
            internal float Height;
            internal Rect(float x,float y,float w,float h)
            {X=x;Y=y;Width=w;Height=h;}
        }

        private static readonly BindingFlags PublicInstance=
            BindingFlags.Instance|BindingFlags.Public;
        private static CharacterCreationScreen host;
        private static CharacterCreationManager manager;
        private static object currentView;
        private static Widget root;
        private static string stage="";
        private static bool enabled=true;
        private static bool primaryInspected;
        private static int refreshTicks;
        private static int viewportWidth;
        private static int viewportHeight;
        private static int contentMoved;
        private static int footerMoved;
        private static object unavailableView;
        private static readonly List<SavedOffset> saved=
            new List<SavedOffset>();
        private static readonly HashSet<Widget> adjusted=
            new HashSet<Widget>();

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

            // Matches the actual native brush restoration shortcut. F10
            // returns BOTH original local brushes and original positions,
            // without touching the game's navigation, values or callbacks.
            if(Input.IsKeyReleased(InputKey.F10))
            {
                enabled=!enabled;
                Restore();
                DiagnosticJournal.Event("cu_native_layout_toggle",
                    "enabled="+enabled+
                    "|F10_returns_original_native_position=true");
            }

            string nextStage=nativeManager.CurrentStage.GetType().Name;
            if(!CUNativeLayoutPolicy.CanAlignNativeFooter(nextStage))
            {
                Restore();
                stage=nextStage;
                return;
            }
            if(!enabled)return;

            int width=(int)Screen.RealScreenResolutionWidth;
            int height=(int)Screen.RealScreenResolutionHeight;
            if(!CUNativeLayoutPolicy.IsViewportSupported(width,height))
            {
                Restore();
                return;
            }
            try
            {
                FieldInfo field=typeof(CharacterCreationScreen).GetField(
                    "_currentStageView",
                    BindingFlags.Instance|BindingFlags.Public|
                    BindingFlags.NonPublic);
                object view=field==null?null:field.GetValue(screen);
                if(view==null || view.GetType().Name!=
                    CUNativeWidgetSkinPolicy.ExpectedView(nextStage))
                {
                    Restore();
                    return;
                }

                if(!Object.ReferenceEquals(view,currentView) ||
                    stage!=nextStage || width!=viewportWidth ||
                    height!=viewportHeight)
                {
                    Restore();
                    currentView=view;
                    stage=nextStage;
                    viewportWidth=width;
                    viewportHeight=height;
                }
                if(Object.ReferenceEquals(unavailableView,view))return;
                if(root==null)
                {
                    root=CUNativeControlSkin.GetGameOwnedRoot(view,nextStage);
                    if(root==null)return;
                    Rect full;
                    if(!TryMeasure(root,out full) ||
                        !CUNativeLayoutPolicy.RootMatchesResolution(
                            full.Width,full.Height,width,height))
                    {
                        DiagnosticJournal.Event(
                            "cu_native_layout_native_preserved",
                            "stage="+stage+
                            "|reason=missing_or_scaled_root_measurement");
                        unavailableView=view;
                        root=null;
                        return;
                    }
                }
                if(!primaryInspected || ++refreshTicks>=18)
                {
                    if(refreshTicks>=18)refreshTicks=0;
                    Align(root,nextStage,width,height);
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_layout_alignment_failed",error);
                object failedView=currentView;
                Restore();
                unavailableView=failedView;
            }
        }

        private static void Align(Widget gameRoot,string nativeStage,
            int screenWidth,int screenHeight)
        {
            var widgets=new List<Widget>();
            widgets.Add(gameRoot);
            foreach(Widget descendant in gameRoot.GetAllChildrenRecursive())
            {
                if(widgets.Count>=
                    CUNativeWidgetSkinPolicy.MaxWidgetsPerStage)
                {
                    DiagnosticJournal.Event(
                        "cu_native_layout_native_preserved",
                        "stage="+nativeStage+"|reason=too_many_widgets");
                    primaryInspected=true;
                    return;
                }
                widgets.Add(descendant);
            }

            if(!primaryInspected)
            {
                primaryInspected=true;
                if(CUNativeLayoutPolicy.CanMovePrimaryPanel(nativeStage))
                    TryAlignContent(gameRoot,widgets,nativeStage,
                        screenWidth,screenHeight);
            }
            TryAlignFooter(widgets,nativeStage,screenWidth,screenHeight);
        }

        private static void TryAlignContent(Widget gameRoot,
            List<Widget> widgets,string nativeStage,
            int width,int height)
        {
            Widget winner=null;
            float winnerDX=0;
            float winnerDY=0;
            int bestScore=0;
            bool ambiguous=false;

            foreach(Widget widget in widgets)
            {
                if(widget==gameRoot || adjusted.Contains(widget) ||
                    IsProtected(widget.GetType().Name))
                    continue;

                int depth=DepthUnder(widget,gameRoot);
                if(depth<1 || depth>2)continue;
                Rect bounds;
                if(!TryMeasure(widget,out bounds))continue;
                float dx,dy;
                if(!CUNativeLayoutPolicy.TryPlanPrimary(nativeStage,
                    width,height,bounds.X,bounds.Y,
                    bounds.Width,bounds.Height,out dx,out dy))
                    continue;

                int score=ContentScore(widget,nativeStage);
                if(score<2)continue;
                if(score>bestScore)
                {
                    winner=widget;
                    winnerDX=dx;
                    winnerDY=dy;
                    bestScore=score;
                    ambiguous=false;
                }
                else if(score==bestScore && score>0)
                    ambiguous=true;
            }

            if(winner==null || ambiguous)
            {
                DiagnosticJournal.Event("cu_native_layout_native_preserved",
                    "stage="+nativeStage+
                    "|reason="+(ambiguous?
                        "ambiguous_native_content_panels":
                        "native_content_already_fits_or_is_unmeasured"));
                return;
            }
            if(TryTranslate(winner,winnerDX,winnerDY))
            {
                contentMoved++;
                DiagnosticJournal.Event("cu_native_layout_content_aligned",
                    "stage="+nativeStage+
                    "|dx="+winnerDX.ToString("0.0",
                        CultureInfo.InvariantCulture)+
                    "|dy="+winnerDY.ToString("0.0",
                        CultureInfo.InvariantCulture)+
                    "|original_native_panel=true" +
                    "|source=measured_widget_bounds");
            }
        }

        private static void TryAlignFooter(List<Widget> widgets,
            string nativeStage,int width,int height)
        {
            foreach(Widget widget in widgets)
            {
                if(!(widget is ButtonWidget) ||
                    adjusted.Contains(widget) ||
                    !HasTextChild(widget))
                    continue;
                Rect rect;
                if(!TryMeasure(widget,out rect))continue;
                float dx,dy;
                if(!CUNativeLayoutPolicy.TryPlanFooter(nativeStage,
                    width,height,rect.X,rect.Y,rect.Width,rect.Height,
                    out dx,out dy))continue;
                if(TryTranslate(widget,dx,dy))
                {
                    footerMoved++;
                    DiagnosticJournal.Event("cu_native_layout_footer_aligned",
                        "stage="+nativeStage+
                        "|dx="+dx.ToString("0.0",
                            CultureInfo.InvariantCulture)+
                        "|dy="+dy.ToString("0.0",
                            CultureInfo.InvariantCulture)+
                        "|original_native_button=true");
                }
            }
        }

        private static bool TryTranslate(Widget widget,float dx,float dy)
        {
            var type=widget.GetType();
            PropertyInfo x=type.GetProperty("PositionXOffset",PublicInstance);
            PropertyInfo y=type.GetProperty("PositionYOffset",PublicInstance);
            if(x==null || y==null ||
                x.GetSetMethod(false)==null ||
                y.GetSetMethod(false)==null)return false;
            if(!IsFloatType(x.PropertyType) ||
                !IsFloatType(y.PropertyType))return false;
            object oldX=x.GetValue(widget,null);
            object oldY=y.GetValue(widget,null);
            if(oldX==null || oldY==null)return false;
            float originalX=Convert.ToSingle(oldX,CultureInfo.InvariantCulture);
            float originalY=Convert.ToSingle(oldY,CultureInfo.InvariantCulture);
            if(!Finite(originalX)||!Finite(originalY) ||
                Math.Abs(originalX+dx)>4096 ||
                Math.Abs(originalY+dy)>4096)
                return false;
            try
            {
                x.SetValue(widget,ConvertNumber(originalX+dx,
                    x.PropertyType),null);
                y.SetValue(widget,ConvertNumber(originalY+dy,
                    y.PropertyType),null);
            }
            catch
            {
                try{x.SetValue(widget,oldX,null);}catch{}
                try{y.SetValue(widget,oldY,null);}catch{}
                return false;
            }
            saved.Add(new SavedOffset(widget,x,y,oldX,oldY));
            adjusted.Add(widget);
            return true;
        }

        private static bool IsFloatType(Type type)
        {
            return type==typeof(float) || type==typeof(double);
        }

        private static object ConvertNumber(float value,Type type)
        {
            return type==typeof(double)?(object)(double)value:(object)value;
        }

        private static bool Finite(float value)
        {
            return !Single.IsNaN(value) &&
                !Single.IsInfinity(value);
        }

        private static bool TryMeasure(Widget widget,out Rect bounds)
        {
            bounds=new Rect();
            float w,h;
            if(!TryReadFloat(widget,"ActualWidth",out w) ||
                !TryReadFloat(widget,"ActualHeight",out h) ||
                w<=0 || h<=0)return false;

            object point=ReadProperty(widget,"GlobalPosition");
            if(point==null)point=ReadProperty(widget,"GlobalPositionPixels");
            if(point==null)return false;
            float x,y;
            if(!TryReadFloat(point,"X",out x) &&
                !TryReadFloat(point,"x",out x))return false;
            if(!TryReadFloat(point,"Y",out y) &&
                !TryReadFloat(point,"y",out y))return false;
            if(!Finite(x)||!Finite(y))return false;
            bounds=new Rect(x,y,w,h);
            return true;
        }

        private static bool TryReadFloat(object instance,string name,
            out float result)
        {
            result=0;
            object value=ReadProperty(instance,name);
            if(value==null)
            {
                FieldInfo field=instance.GetType().GetField(name,
                    PublicInstance);
                if(field!=null)
                {
                    try{value=field.GetValue(instance);}catch{}
                }
            }
            if(value==null)return false;
            try
            {
                result=Convert.ToSingle(value,CultureInfo.InvariantCulture);
                return Finite(result);
            }
            catch{return false;}
        }

        private static object ReadProperty(object instance,string name)
        {
            if(instance==null)return null;
            var prop=instance.GetType().GetProperty(name,PublicInstance);
            if(prop==null || prop.GetIndexParameters().Length>0)return null;
            try{return prop.GetValue(instance,null);}catch{return null;}
        }

        private static int DepthUnder(Widget target,Widget ancestor)
        {
            object node=target;
            for(int depth=1;depth<=3;depth++)
            {
                if(node==null)return -1;
                node=ReadProperty(node,"ParentWidget");
                if(Object.ReferenceEquals(node,ancestor))return depth;
            }
            return -1;
        }

        private static bool HasTextChild(Widget widget)
        {
            foreach(Widget child in widget.GetAllChildrenRecursive())
            {
                TextWidget text=child as TextWidget;
                if(text!=null && !String.IsNullOrWhiteSpace(text.Text))
                    return true;
            }
            return false;
        }

        private static int ContentScore(Widget widget,string nativeStage)
        {
            int text=0,button=0,input=0,slider=0;
            foreach(Widget child in widget.GetAllChildrenRecursive())
            {
                string type=child.GetType().Name;
                if(IsProtected(type))return 0;
                if(child is TextWidget && text<12)text++;
                if(child is ButtonWidget && HasTextChild(child) && button<8)
                    button++;
                if(CUNativeWidgetSkinPolicy.CanSkinClanTextInput(
                    nativeStage,type))input++;
                if(CUNativeWidgetSkinPolicy.CanSkinDifficultySlider(
                    nativeStage,type))slider++;
            }
            if(nativeStage=="CharacterCreationNarrativeStage")
                return button>=2?button*3+text:0;
            if(nativeStage=="CharacterCreationClanNamingStage")
                return input>=1?input*4+text:0;
            if(nativeStage=="CharacterCreationReviewStage")
                return text>=4?text:0;
            if(nativeStage=="CharacterCreationOptionsStage")
                return slider>=2?slider*4+text:0;
            return 0;
        }

        private static bool IsProtected(string type)
        {
            if(String.IsNullOrEmpty(type))return true;
            return type.IndexOf("Scene",StringComparison.OrdinalIgnoreCase)>=0 ||
                type.IndexOf("Banner",StringComparison.OrdinalIgnoreCase)>=0 ||
                type.IndexOf("ImageIdentifier",
                    StringComparison.OrdinalIgnoreCase)>=0;
        }

        private static void Restore()
        {
            for(int i=saved.Count-1;i>=0;i--)
            {
                SavedOffset item=saved[i];
                try
                {
                    item.XProperty.SetValue(item.Widget,
                        item.OriginalX,null);
                    item.YProperty.SetValue(item.Widget,
                        item.OriginalY,null);
                }
                catch(Exception e)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_layout_restore_error",e);
                }
            }
            if(saved.Count>0)
                DiagnosticJournal.Event("cu_native_layout_restored",
                    "stage="+stage+
                    "|native_panels="+contentMoved+
                    "|native_footer_buttons="+footerMoved+
                    "|original_geometry_reinstated=true");
            saved.Clear();
            adjusted.Clear();
            contentMoved=0;
            footerMoved=0;
            currentView=null;
            root=null;
            primaryInspected=false;
            refreshTicks=0;
            viewportWidth=0;
            viewportHeight=0;
        }

        public static void Clear()
        {
            Restore();
            host=null;
            manager=null;
            stage="";
            unavailableView=null;
            enabled=true;
        }
    }
}
