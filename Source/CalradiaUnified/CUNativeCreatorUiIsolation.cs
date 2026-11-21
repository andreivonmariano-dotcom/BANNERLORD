using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Hide only game-owned Gauntlet UI widgets during CU interstitials.
    /// Bannerlord's 3D character/horse/banner scene is owned by its
    /// separate scene layer and is never removed, hidden or recreated.
    /// Every widget's original visibility is restored on detach.
    ///
    /// Version-pinned 1.5.7.125802 guard: inspect UIContext's public
    /// Root, with RootWidget as a compatibility fallback. Fail open
    /// without touching anything if no native Gauntlet root is found.
    /// </summary>
    internal sealed class CUNativeCreatorUiIsolation
    {
        private sealed class HiddenRoot
        {
            internal readonly Widget Root;
            internal readonly bool WasHidden;

            internal HiddenRoot(Widget root)
            {
                Root=root;
                WasHidden=root.IsHidden;
            }
        }

        private readonly List<HiddenRoot> roots=
            new List<HiddenRoot>();

        public int HiddenRootCount {get {return roots.Count;}}

        public bool Hide(ScreenBase screen,GauntletLayer customLayer)
        {
            Restore();
            if(screen==null || screen.IsFinalized || customLayer==null)
                return false;
            var staged=new List<HiddenRoot>();
            try
            {
                foreach(var screenLayer in screen.Layers)
                {
                    var native=screenLayer as GauntletLayer;
                    if(native==null || Object.ReferenceEquals(native,customLayer))
                        continue;
                    // Never hide any other Calradia Unified editor or
                    // diagnostic overlay. Only touch the native stage.
                    var name=native.Name??"";
                    if(name.StartsWith("CU",StringComparison.Ordinal) ||
                       name.StartsWith("CalradiaUnified",
                           StringComparison.Ordinal))
                        continue;
                    Widget root=FindRoot(native);
                    if(root==null)continue;
                    bool duplicate=false;
                    foreach(var record in staged)
                        if(Object.ReferenceEquals(record.Root,root))
                            duplicate=true;
                    if(!duplicate)staged.Add(new HiddenRoot(root));
                }
                if(staged.Count==0)
                {
                    DiagnosticJournal.Event("cu_native_ui_isolation_unavailable",
                        "native_gauntlet_roots=0|scene_preserved=true");
                    return false;
                }
                // Capture all originals before mutating even one widget.
                foreach(var record in staged)
                {
                    roots.Add(record);
                    record.Root.IsHidden=true;
                }
                DiagnosticJournal.Event("cu_native_ui_isolated",
                    "native_widget_roots="+roots.Count+
                    "|scene_layers_touched=0|restore_on_exit=true");
                return true;
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_ui_isolation_failed",error);
                Restore();
                return false;
            }
        }

        private static Widget FindRoot(GauntletLayer native)
        {
            if(native==null || native.UIContext==null)return null;
            var context=native.UIContext;
            Type type=context.GetType();
            const BindingFlags flags=
                BindingFlags.Instance|BindingFlags.Public;
            foreach(var name in new[]{"Root","RootWidget"})
            {
                var property=type.GetProperty(name,flags);
                if(property!=null)
                {
                    var widget=property.GetValue(context,null) as Widget;
                    if(widget!=null)return widget;
                }
                var field=type.GetField(name,flags);
                if(field!=null)
                {
                    var widget=field.GetValue(context) as Widget;
                    if(widget!=null)return widget;
                }
            }
            return null;
        }

        public void Restore()
        {
            for(int i=roots.Count-1;i>=0;i--)
            {
                try
                {
                    roots[i].Root.IsHidden=roots[i].WasHidden;
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_ui_restore_failed",error);
                }
            }
            if(roots.Count>0)
                DiagnosticJournal.Event("cu_native_ui_restored",
                    "native_widget_roots="+roots.Count+
                    "|original_visibility_restored=true");
            roots.Clear();
        }
    }
}
