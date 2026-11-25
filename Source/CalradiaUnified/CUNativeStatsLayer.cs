using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// A focused CU step at the native Narrative -> Banner transition.
    /// The original narrative has already advanced and remains the owner of
    /// choices/consequences. The native banner stage is PAUSED beneath an
    /// opaque Gauntlet movie, not removed or replaced. No extra stage is
    /// registered, no stage is skipped, and there is no manual opt-in.
    /// </summary>
    internal static class CUNativeStatsLayer
    {
        private static CharacterCreationManager trackedManager;
        private static CharacterCreationScreen host;
        private static CharacterCreationScreen failedHost;
        private static GauntletLayer layer;
        private static GauntletMovieIdentifier movie;
        private static readonly CUNativeCreatorUiIsolation nativeUi=
            new CUNativeCreatorUiIsolation();
        private static CUNativeStatsVM activeVM;
        private static bool sliderWarningLogged;
        private static bool sliderProbeUnavailable;
        private static SliderWidget cachedAgeSlider;
        private static int ageSliderProbeFrames;
        private static int lastSliderValue=-1;
        private static int lastModelValue=-1;
        // In the 1650x927 live game the native slider rendered but
        // never delivered a drag update. A focused pointer capture path
        // uses measured Gauntlet coordinates rather than a guessed bar.
        private static bool previousMouseDown;
        private static bool ageHitWarningLogged;
        private static bool narrativeSeen;
        private static bool completed;
        private static bool closeRequested;
        private static bool backRequested;
        private static bool returningFromNativeBanner;
        private static string previousNativeStage;

        public static void Tick(CharacterCreationScreen screen,
            CharacterCreationManager manager)
        {
            if (screen==null || screen.IsFinalized || manager==null ||
                !SandboxCreatorOverlay.HasAcceptedCustomStart)
            {
                Clear();
                return;
            }
            if (!Object.ReferenceEquals(trackedManager,manager))
            {
                Clear();
                trackedManager=manager;
            }
            string stage=manager.CurrentStage==null ? "" :
                manager.CurrentStage.GetType().Name;
            bool isNarrative=stage=="CharacterCreationNarrativeStage";
            bool isBanner=stage=="CharacterCreationBannerEditorStage";
            if(isNarrative &&
                previousNativeStage=="CharacterCreationBannerEditorStage" &&
                completed)
            {
                // User clicked the ORIGINAL native Banner Back. Native
                // stage history knows nothing about CU's Traits overlay.
                // Reopen Traits over the real narrative page, then let
                // Bannerlord advance back to Banner on confirmation.
                returningFromNativeBanner=true;
                completed=false;
                failedHost=null;
                DiagnosticJournal.Event("cu_native_banner_back_to_traits",
                    "original_back_callback_retained=true"+
                    "|reopen=Traits|native_stage=narrative");
            }
            previousNativeStage=stage;
            if(isNarrative)
            {
                narrativeSeen=true;
                if(!returningFromNativeBanner)
                {
                    // An explicit Back from CU's Age step reveals
                    // the original native life story.
                    completed=false;
                    failedHost=null;
                    Detach();
                    return;
                }
            }
            if(!isBanner && !(isNarrative && returningFromNativeBanner))
            {
                Detach();
                return;
            }

            if (closeRequested)
            {
                closeRequested=false;
                bool needsBannerReturn=returningFromNativeBanner;
                Detach();
                returningFromNativeBanner=false;
                completed=true;
                if(needsBannerReturn)
                {
                    try
                    {
                        // Return through the game-owned native transition.
                        // Do not create an extra Banner stage or game state.
                        manager.NextStage();
                        DiagnosticJournal.Event(
                            "cu_native_stats_return_to_banner",
                            "from=Traits|native_next_stage=true");
                    }
                    catch(Exception error)
                    {
                        completed=false;
                        DiagnosticJournal.Exception(
                            "cu_native_stats_return_to_banner_failed",error);
                    }
                }
                else
                    DiagnosticJournal.Event("cu_native_stats_step_closed",
                        "resume=original_banner_stage|wizard_still_native=true");
                return;
            }
            if (backRequested)
            {
                backRequested=false;
                bool alreadyOnNarrative=returningFromNativeBanner;
                Detach();
                returningFromNativeBanner=false;
                if(alreadyOnNarrative)
                {
                    // Banner's native Back already returned the game to
                    // the Narrative stage. Revealing that same native
                    // stage is the correct boundary, not PreviousStage
                    // again, which could jump behind the life story.
                    DiagnosticJournal.Event("cu_native_stats_back",
                        "destination=visible_native_narrative"+
                        "|no_double_previous_stage=true");
                    return;
                }
                try
                {
                    manager.PreviousStage();
                    DiagnosticJournal.Event("cu_native_stats_back",
                        "destination=native_narrative");
                }
                catch(Exception e)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_stats_back_failed",e);
                }
                return;
            }
            if (Object.ReferenceEquals(host,screen))
            {
                // The live slider ValueInt is the authoritative drag
                // position. Some game versions only push a read-only VM
                // binding, so poll the actual pinned Gauntlet widget and
                // feed the value into the same validated age setter.
                MaintainAgeLayerFocus();
                TraceAgeMouseInput();
                ReadNativeSliderValue();
                // Match native Escape=Back. Teardown remains deferred,
                // outside the movie click / input callback.
                if(ScreenManager.FocusedLayer==layer &&
                    Input.IsKeyReleased(InputKey.Escape))
                    backRequested=true;
                return;
            }
            if (!narrativeSeen || completed ||
                Object.ReferenceEquals(failedHost,screen))
                return;

            GauntletLayer next=null;
            GauntletMovieIdentifier nextMovie=null;
            try
            {
                var hero=Hero.MainHero;
                if(hero==null || hero.HeroDeveloper==null)
                {
                    DiagnosticJournal.Event("cu_native_stats_fallback",
                        "reason=main_hero_not_ready|native_banner_continues");
                    completed=true;
                    return;
                }
                var profile=SandboxOriginProfile.Load(
                    SandboxOriginProfile.UserPath);
                if(!profile.Enabled)
                {
                    completed=true;
                    return;
                }
                var vm=new CUNativeStatsVM(manager,profile,
                    ()=>closeRequested=true,()=>backRequested=true,
                    returningFromNativeBanner ?
                    CUNativeStatsNavigationPolicy.Attributes :
                    CUNativeStatsNavigationPolicy.Age);
                next=new GauntletLayer("CUNativeStatsEditor",85,false);
                next.IsFocusLayer=true;
                next.InputRestrictions.SetInputRestrictions();
                try
                {
                    // Right-side native-style v0.59b composition leaves
                    // the actual Bannerlord character preview visible.
                    // It retains the original input-owning Age slider
                    // and the six verified button-only stat rows.
                    bool customLoaded=false;
                    try
                    {
                        nextMovie=next.LoadMovie(
                            "CUNativeStatsEditorNativeReview",vm);
                        customLoaded=true;
                        DiagnosticJournal.Event(
                            "cu_native_creator_assets",
                            "screen=Age_and_Stats"+
                            "|layout=review_shell_v059f"+
                            "|art=installed_game"+
                            "|native_age_slider=retained|stat_sliders=0");
                    }
                    catch(Exception reviewShellError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_stats_review_shell_fallback",
                            reviewShellError);
                    }
                    if(!customLoaded && CUCreatorArtRegistry.IsAvailable())
                    {
                        try
                        {
                            nextMovie=next.LoadMovie(
                                "CUNativeStatsEditorV059Art",vm);
                            customLoaded=true;
                            DiagnosticJournal.Event(
                                "cu_native_creator_assets",
                                "screen=stats|layout=v059b_right_panel"+
                                "|source=cu_imported_atlas|stat_sliders=0");
                        }
                        catch(Exception customArtFailure)
                        {
                            DiagnosticJournal.Exception(
                                "cu_stats_v059_custom_art_fallback",
                                customArtFailure);
                        }
                    }
                    if(!customLoaded)
                    {
                        nextMovie=next.LoadMovie("CUNativeStatsEditorV059",vm);
                        DiagnosticJournal.Event("cu_native_creator_assets",
                            "screen=stats|layout=v059b_right_panel"+
                            "|source=installed_game|age_slider=retained"+
                            "|stat_sliders=0");
                    }
                }
                catch(Exception newLayoutFailure)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_v059_layout_fallback",newLayoutFailure);
                    try
                    {
                        nextMovie=next.LoadMovie("CUNativeStatsEditor",vm);
                        DiagnosticJournal.Event("cu_native_creator_assets",
                            "screen=stats|layout=legacy_fallback"+
                            "|age_slider=retained|stat_sliders=0");
                    }
                    catch(Exception nativeArtFailure)
                    {
                        DiagnosticJournal.Exception(
                            "cu_native_stats_art_fallback",nativeArtFailure);
                        try
                        {
                            nextMovie=next.LoadMovie(
                                "CUNativeStatsEditorPlainFallback",vm);
                        }
                        catch(Exception sliderFailure)
                        {
                            DiagnosticJournal.Exception(
                                "cu_native_age_slider_fallback",sliderFailure);
                            nextMovie=next.LoadMovie(
                                "CUNativeStatsEditorSafe",vm);
                        }
                    }
                }
                // Isolate only the game-owned Gauntlet widget roots.
                // The native 3D character scene layer is never changed.
                // If the pinned engine cannot expose those roots,
                // preserve edit functionality through the fully opaque
                // known-working editor rather than stacking two UIs.
                if(!nativeUi.Hide(screen,next))
                {
                    if(nextMovie!=null)next.ReleaseMovie(nextMovie);
                    nextMovie=next.LoadMovie("CUNativeStatsEditor",vm);
                    DiagnosticJournal.Event("cu_native_creator_safe_fallback",
                        "screen=Age_and_Stats|native_ui_isolation=false"+
                        "|layout=opaque_legacy|gameplay_commands=retained");
                }
                screen.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=screen;
                layer=next;
                movie=nextMovie;
                activeVM=vm;
                sliderWarningLogged=false;
                sliderProbeUnavailable=false;
                cachedAgeSlider=null;
                ageSliderProbeFrames=0;
                lastSliderValue=-1;
                lastModelValue=-1;
                previousMouseDown=false;
                ageHitWarningLogged=false;
                failedHost=null;
                DiagnosticJournal.Event("cu_native_stats_step_attached",
                    "focused="+Object.ReferenceEquals(
                        ScreenManager.FocusedLayer,next)+
                    "|input_order="+next.InputRestrictions.Order+
                    "|transition=after_narrative_before_banner" +
                    "|hero_real=true|native_stages_preserved=true" +
                    "|default_mode=Native|no_experimental_toggle=true"+
                    "|entry_page="+
                    (returningFromNativeBanner?"Traits":"Age"));
                DiagnosticJournal.Event("cu_creator_page_checkpoint",
                    "page="+(returningFromNativeBanner?"All Stats":"Age")+
                    "|surface=stats_editor|phase=layer_attached");
            }
            catch(Exception e)
            {
                nativeUi.Restore();
                failedHost=screen;
                DiagnosticJournal.Exception("cu_native_stats_attach_failed",e);
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
                        "cu_native_stats_cleanup_failed",cleanup);
                }
                // The default native story is never blocked by a CU movie.
                completed=true;
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
            activeVM=null;
            sliderWarningLogged=false;
            sliderProbeUnavailable=false;
            cachedAgeSlider=null;
            ageSliderProbeFrames=0;
            lastSliderValue=-1;
            lastModelValue=-1;
            previousMouseDown=false;
            ageHitWarningLogged=false;
            nativeUi.Restore();
            if(oldHost==null || oldLayer==null) return;
            try
            {
                if(!oldHost.IsFinalized && oldHost.HasLayer(oldLayer))
                {
                    oldLayer.IsFocusLayer=false;
                    ScreenManager.TryLoseFocus(oldLayer);
                    if(oldMovie!=null) oldLayer.ReleaseMovie(oldMovie);
                    oldHost.RemoveLayer(oldLayer);
                    // Return focus to the still-active, real banner layer.
                    foreach(var native in oldHost.Layers)
                    {
                        if(native!=oldLayer && native.IsFocusLayer)
                        {
                            ScreenManager.TrySetFocus(native);
                            break;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception("cu_native_stats_detach_failed",e);
            }
        }

        private static SliderWidget FindSliderWidget(Widget root)
        {
            if(root==null)return null;
            // Prefer the actual Gauntlet widget API, not reflected
            // private method guesses that can fail in a different build.
            var direct=root.FindChild("CUPlayerAgeSlider",true)
                as SliderWidget;
            return direct ?? FindSliderInTree(root,0);
        }

        private static SliderWidget FindSliderInTree(Widget root,int depth)
        {
            if(root==null || depth>24)return null;
            if(root.Id=="CUPlayerAgeSlider")
                return root as SliderWidget;
            int count=Math.Min(root.ChildCount,512);
            for(int i=0;i<count;i++)
            {
                var candidate=FindSliderInTree(root.GetChild(i),depth+1);
                if(candidate!=null)return candidate;
            }
            return null;
        }

        // Diagnoses where every real mouse-down/up reaches the
        // game-owned Gauntlet event hierarchy. Do not access
        // EventManager.MousePosition: the installed game raised
        // MissingMethodException for that return signature on v0.50.
        // The widget lookup is a reflective, fail-safe diagnostic only.
        private static void TraceAgeMouseInput()
        {
            if(activeVM==null || !activeVM.IsAgePage || layer==null)
                return;
            bool down=Input.IsKeyDown(InputKey.LeftMouseButton);
            if(down==previousMouseDown)return;
            previousMouseDown=down;
            string target="unavailable";
            try
            {
                var manager=layer.UIContext.EventManager;
                var type=manager.GetType();
                var property=type.GetProperty("LatestMouseDownWidget",
                    BindingFlags.Public|BindingFlags.Instance);
                object hovered=property==null ? null :
                    property.GetValue(manager,null);
                var widget=hovered as Widget;
                target=widget==null?"none":
                    widget.GetType().Name+":"+
                    (String.IsNullOrEmpty(widget.Id)?"unnamed":widget.Id);
            }
            catch(Exception error)
            {
                target="reflection_unavailable:"+error.GetType().Name;
            }
            var focused=ScreenManager.FocusedLayer;
            DiagnosticJournal.Event("cu_native_age_hit_trace",
                "edge="+(down?"down":"up")+
                "|age="+activeVM.PlayerAge+
                "|focused="+Object.ReferenceEquals(focused,layer)+
                "|focused_layer="+(focused==null?"none":
                    focused.GetType().Name)+
                "|target="+target+
                "|slider_found="+(cachedAgeSlider!=null));
        }

        private static void MaintainAgeLayerFocus()
        {
            if(layer==null || activeVM==null || !activeVM.IsAgePage)
                return;
            var focused=ScreenManager.FocusedLayer;
            if(Object.ReferenceEquals(focused,layer))
            {
                ageHitWarningLogged=false;
                return;
            }
            // Never steal focus from a higher-order text inquiry or
            // confirmation modal. Only reclaim focus from the original
            // native banner layer, whose order is lower than CU's.
            bool canReclaim=focused==null ||
                focused.InputRestrictions.Order <
                    layer.InputRestrictions.Order;
            if(canReclaim)
                ScreenManager.TrySetFocus(layer);
            if(!ageHitWarningLogged)
            {
                DiagnosticJournal.Event("cu_native_age_focus_status",
                    "focused_before="+(focused==null?"none":
                        focused.GetType().Name)+
                    "|can_reclaim="+canReclaim+
                    "|focused_after="+
                    Object.ReferenceEquals(ScreenManager.FocusedLayer,layer));
                ageHitWarningLogged=true;
            }
        }

        private static void ReadNativeSliderValue()
        {
            if(activeVM==null || !activeVM.IsAgePage ||
                movie==null || movie.Movie==null ||
                sliderProbeUnavailable)
                return;
            try
            {
                if(cachedAgeSlider==null)
                {
                    // Loaded movies may create the widget tree one tick
                    // after LoadMovie, so retry for a bounded interval.
                    cachedAgeSlider=FindSliderWidget(movie.Movie.RootWidget);
                    if(cachedAgeSlider==null)
                    {
                        if(++ageSliderProbeFrames>=60)
                        {
                            sliderProbeUnavailable=true;
                            DiagnosticJournal.Event("cu_native_age_slider_probe",
                                "supported=false|reason=slider_not_found"+
                                "|fallback=one_year_buttons_and_exact_entry");
                        }
                        return;
                    }
                    if(cachedAgeSlider.SliderArea==null ||
                        cachedAgeSlider.Handle==null ||
                        cachedAgeSlider.MinValueInt!=18 ||
                        cachedAgeSlider.MaxValueInt!=80)
                    {
                        sliderProbeUnavailable=true;
                        cachedAgeSlider=null;
                        DiagnosticJournal.Event("cu_native_age_slider_probe",
                            "supported=false|reason=invalid_widget_contract"+
                            "|fallback=one_year_buttons_and_exact_entry");
                        return;
                    }
                    lastModelValue=activeVM.PlayerAge;
                    lastSliderValue=cachedAgeSlider.ValueInt;
                    if(lastSliderValue!=lastModelValue)
                    {
                        cachedAgeSlider.ValueInt=lastModelValue;
                        lastSliderValue=lastModelValue;
                    }
                    DiagnosticJournal.Event("cu_native_age_slider_probe",
                        "supported=true|source=real_gauntlet_tree"+
                        "|widget=native_SliderWidget|handle=ButtonWidget"+
                        "|range=18-80");
                    return;
                }
                int model=activeVM.PlayerAge;
                int widget=cachedAgeSlider.ValueInt;
                bool pressed=cachedAgeSlider.Handle!=null &&
                    cachedAgeSlider.Handle.IsPressed;
                if(model!=lastModelValue)
                {
                    // A native two-way binding or year/exact-entry action
                    // already changed the model. The model takes priority
                    // over a stale widget to prevent age reversal.
                    if(widget!=model)
                        cachedAgeSlider.ValueInt=model;
                    if(pressed && SandboxFamilyAgePolicy.ValidPlayerAge(model))
                        DiagnosticJournal.Event("cu_native_age_slider_drag",
                            "chosen_year="+model+"|source=pressed_native_handle");
                    lastModelValue=model;
                    lastSliderValue=model;
                    return;
                }
                if(widget==lastSliderValue)return;
                if(!SandboxFamilyAgePolicy.ValidPlayerAge(widget))
                {
                    cachedAgeSlider.ValueInt=model;
                    lastSliderValue=model;
                    DiagnosticJournal.Event("cu_native_age_slider_rejected",
                        "reason=out_of_range|value="+widget);
                    return;
                }
                lastSliderValue=widget;
                activeVM.PlayerAge=widget;
                lastModelValue=activeVM.PlayerAge;
                DiagnosticJournal.Event("cu_native_age_slider_value_changed",
                    "chosen_year="+widget+"|source=native_slider_widget"+
                    "|handle_pressed="+pressed);
                // Never label a programmatic binding update as a mouse drag
                // without a pressed native handle.
                if(pressed)
                    DiagnosticJournal.Event("cu_native_age_slider_drag",
                        "chosen_year="+widget+"|source=pressed_native_handle");
            }
            catch(Exception error)
            {
                sliderProbeUnavailable=true;
                if(!sliderWarningLogged)
                {
                    sliderWarningLogged=true;
                    DiagnosticJournal.Exception(
                        "cu_native_age_slider_probe_failed",error);
                }
            }
        }

        public static void Clear()
        {
            Detach();
            trackedManager=null;
            failedHost=null;
            narrativeSeen=false;
            completed=false;
            closeRequested=false;
            backRequested=false;
            returningFromNativeBanner=false;
            previousNativeStage=null;
        }
    }

    /// <summary>
    /// Editable single row bound to a live native Hero stat. No direct Hero
    /// mutation occurs in this movie: edits are validated profile drafts.
    /// The one-shot campaign completion applies approved values later.
    /// </summary>
    public sealed class CUNativeStatRowVM : ViewModel
    {
        private readonly string key;
        private readonly string name;
        private readonly string description;
        private readonly int native;
        private int selected;
        private readonly bool autoValue;
        private readonly bool generatedTarget;
        private readonly Action<string,int> change;
        private readonly Action<string,int> setExact;

        internal CUNativeStatRowVM(string key,int nativeValue,int selectedValue,
            string help,Action<string,int> onChange,
            Action<string,int> onSetExact,bool automatic=false,
            bool generated=false)
        {
            this.key=key;
            string originalName=key.Substring(key.IndexOf('.')+1);
            name=key==SandboxFamilyLevelPolicy.Key?
                "Character Level":
                key.StartsWith("Focus.",StringComparison.Ordinal) ?
                "Focus: "+originalName : key.StartsWith("Skill.",
                StringComparison.Ordinal) ? "Skill: "+originalName :
                originalName;
            description=help;
            native=nativeValue;
            selected=selectedValue;
            autoValue=automatic;
            generatedTarget=generated;
            change=onChange;
            setExact=onSetExact;
        }

        public void ExecuteIncrease(){change(key,+1);}
        public void ExecuteDecrease(){change(key,-1);}
        public void ExecuteEditExact()
        {
            // Optional native inquiry for an exact legal skill value. The
            // same cost and range validation runs after its confirmation.
            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Set "+name,
                "Enter the exact starting value. Native attribute, skill, " +
                    "focus and trait limits still apply.",
                true,true,"Apply","Cancel",
                value=>
                {
                    int exact;
                    if(Int32.TryParse(value,out exact))
                        setExact(key,exact);
                },()=>{},false,null,"",selected.ToString()));
        }

        [DataSourceProperty]
        public string Name {get{return name;}}

        [DataSourceProperty]
        public string NativeValue
            {get{return generatedTarget?"Auto":
                "Base "+native;}}

        internal void ApplySelected(int updated)
        {
            // Native fixed-slot bindings belong to the parent editor.
            // Do not fire independent Gauntlet row item-template events.
            selected=updated;
        }
        [DataSourceProperty] public int SelectedInt
        {
            get{return selected;}
            set
            {
                if(value!=selected)
                    setExact(key,value);
            }
        }
        [DataSourceProperty] public int Minimum
            {get{return key==SandboxFamilyLevelPolicy.Key?1:
                key.StartsWith("Attribute.")?1:
                key.StartsWith("Trait.")?-2:0;}}
        [DataSourceProperty] public int Maximum
            {get{return key==SandboxFamilyLevelPolicy.Key?
                SandboxFamilyLevelPolicy.Maximum:
                key.StartsWith("Attribute.")?10:
                key.StartsWith("Trait.")?2:
                key.StartsWith("Focus.")?5:330;}}
        [DataSourceProperty] public int Increment
            {get{return key.StartsWith("Skill.")?10:1;}}
        public string Key {get{return key;}}
        [DataSourceProperty]
        public string SelectedValue
            {get{return autoValue&&selected==native?
                "AUTO":""+selected;}}

        [DataSourceProperty]
        public string Difference
        {
            get
            {
                int delta=selected-native;
                return delta==0?"UNCHANGED":(delta>0?"+":"")+delta;
            }
        }

        [DataSourceProperty]
        public string Description {get{return description;}}
    }

    /// <summary>
    /// Three modes, three short editors. Native attributes/skills/traits
    /// are inspected only after the player has finished the native narrative.
    /// The actual narrative has its own effect text; unavailable private
    /// selections are never replaced with invented origin bonuses.
    /// </summary>
    public sealed class CUNativeStatsVM : ViewModel
    {
        private readonly SandboxOriginProfile profile;
        private readonly IDictionary<string,int> baseline;
        private readonly IDictionary<string,int> spouseBaseline;
        private bool editingSpouse;
        private bool EditingWandererCompanion
            {get{return profile.Role=="Wanderer" &&
                profile.WandererNoHouseStart;}}
        private readonly Action completed;
        private readonly Action back;
        // One short exact-age page follows the native story and precedes
        // optional attributes/skills/traits. The narrative owns its bonuses.
        private int pageIndex=CUNativeStatsNavigationPolicy.Age;
        private int statsRowPageIndex;
        private int statsRowPageCount=1;
        private string status="";
        private readonly string[] bonusLines;
        private int bonusIndex;

        // The user's 1.5.7 game exited again after v0.56 generated
        // all six static stat bindings. v0.58 keeps six rows with
        // +/- and exact-entry commands but avoids stat SliderWidgets.
        private readonly List<CUNativeStatRowVM> rows =
            new List<CUNativeStatRowVM>();

        internal CUNativeStatsVM(CharacterCreationManager manager,
            SandboxOriginProfile source,Action onCompleted,Action onBack,
            int startingPage)
        {
            if(!CUNativeStatsNavigationPolicy.IsValidPage(startingPage))
                throw new ArgumentOutOfRangeException("startingPage");
            pageIndex=startingPage;
            profile=SandboxOriginProfile.Parse(source.Serialize());
            // The existing advanced Resources fields no longer authorize
            // arbitrary early override values. Defaults are native.
            baseline=CUNativeStatsSnapshot.Capture(Hero.MainHero);
            spouseBaseline=new SortedDictionary<string,int>(
                StringComparer.Ordinal);
            // No spouse Hero exists yet. Display a provisional level,
            // never claim that this is the generated character's actual
            // native starting level. Manual entry is applied exactly later.
            spouseBaseline[SandboxFamilyLevelPolicy.Key]=
                Math.Max(1,Math.Min(SandboxFamilyLevelPolicy.Maximum,
                    8+((EditingWandererCompanion?
                        profile.WandererCompanionAge:profile.SpouseAge)-18)/3));
            foreach(var name in SandboxOriginProfile.Attributes)
                spouseBaseline["Attribute."+name]=3;
            foreach(var name in SandboxNativeSkillCatalog.AvailableNames())
            {
                spouseBaseline["Skill."+name]=0;
                spouseBaseline["Focus."+name]=0;
            }
            foreach(var name in SandboxOriginProfile.Traits)
                spouseBaseline["Trait."+name]=0;
            completed=onCompleted;
            back=onBack;
            bonusLines=NativeNarrativeBonusReader.Read(manager);
            Refresh();
        }

        private IDictionary<string,int> BaseForTarget
            {get{return editingSpouse?spouseBaseline:baseline;}}
        private IDictionary<string,int> ChangesForTarget
            {get{return editingSpouse?
                (EditingWandererCompanion?
                    profile.WandererCompanionOverrides:
                    profile.SpouseOverrides):
                profile.Overrides;}}
        // One logical All Stats screen with six active Gauntlet sliders
        // per page. Building >50 slider widgets in an unverified native
        // scroll hierarchy caused a hard game exit on the user's 1.5.7
        // runtime when leaving the confirmed-working Age screen.
        private List<string> AllStatKeys()
        {
            var keys=new List<string>();
            keys.Add(SandboxFamilyLevelPolicy.Key);
            foreach(string name in SandboxOriginProfile.Attributes)
                keys.Add("Attribute."+name);
            foreach(string name in SandboxNativeSkillCatalog.AvailableNames())
            {
                keys.Add("Skill."+name);
                keys.Add("Focus."+name);
            }
            foreach(string name in SandboxOriginProfile.Traits)
                keys.Add("Trait."+name);
            return keys;
        }
        private void Refresh()
        {
            DiagnosticJournal.Event("cu_stats_page_refresh_enter",
                "creator_page="+pageIndex+
                "|target="+(editingSpouse?
                    (EditingWandererCompanion?"companion":"spouse"):
                    "player")+
                "|row_page="+statsRowPageIndex);
            rows.Clear();
            if(pageIndex>=0)
            {
                var keys=AllStatKeys();
                statsRowPageCount=SandboxStatsPagePolicy.PageCount(
                    keys.Count);
                statsRowPageIndex=SandboxStatsPagePolicy.ClampPage(
                    statsRowPageIndex,keys.Count);
                int start=SandboxStatsPagePolicy.StartIndex(
                    statsRowPageIndex,keys.Count);
                int count=SandboxStatsPagePolicy.PageSize(
                    statsRowPageIndex,keys.Count);
                for(int i=start;i<start+count;i++)
                {
                    string key=keys[i];
                    if(key==SandboxFamilyLevelPolicy.Key)
                        AddRow(key,"Hero level | 1-50 | native XP matched");
                    else if(key.StartsWith("Attribute.",
                        StringComparison.Ordinal))
                        AddRow(key,"Native attribute | 1-10");
                    else if(key.StartsWith("Skill.",
                        StringComparison.Ordinal))
                        AddRow(key,"Native skill | 0-330 | inherited training");
                    else if(key.StartsWith("Focus.",
                        StringComparison.Ordinal))
                        AddRow(key,"Native focus | 0-5 | learning potential");
                    else if(key.StartsWith("Trait.",
                        StringComparison.Ordinal))
                        AddRow(key,SandboxStatBudgetPolicy.TraitDescription(
                            key.Substring("Trait.".Length)));
                }
                DiagnosticJournal.Event("cu_stats_page_rows_built",
                    "page="+(statsRowPageIndex+1)+"/"+
                    statsRowPageCount+
                    "|visible="+rows.Count+
                    "|total="+keys.Count+
                    "|scrollable_panel=false"+
                    "|item_template=false"+
                    "|fixed_slots=6|game_native_slider=true");
            }
            OnPropertyChangedWithValue(Page,"Page");
            OnPropertyChangedWithValue(PageProgress,"PageProgress");
            OnPropertyChangedWithValue(Mode,"Mode");
            OnPropertyChangedWithValue(ModeDescription,"ModeDescription");
            OnPropertyChangedWithValue(BudgetStatus,"BudgetStatus");
            OnPropertyChangedWithValue(IsSkillsPage,"IsSkillsPage");
            OnPropertyChangedWithValue(IsAgePage,"IsAgePage");
            OnPropertyChangedWithValue(IsStatsPage,"IsStatsPage");
            OnPropertyChangedWithValue(PlayerAge,"PlayerAge");
            OnPropertyChangedWithValue(AgeDescription,"AgeDescription");
            OnPropertyChangedWithValue(SkillGroup,"SkillGroup");
            OnPropertyChangedWithValue(NextLabel,"NextLabel");
            OnPropertyChangedWithValue(IsNativeMode,"IsNativeMode");
            OnPropertyChangedWithValue(IsBalancedMode,"IsBalancedMode");
            OnPropertyChangedWithValue(IsUnrestrictedMode,"IsUnrestrictedMode");
            OnPropertyChangedWithValue(IsPlayerTarget,"IsPlayerTarget");
            OnPropertyChangedWithValue(IsSpouseTarget,"IsSpouseTarget");
            OnPropertyChangedWithValue(
                IsSecondaryStatsTargetVisible,
                "IsSecondaryStatsTargetVisible");
            OnPropertyChangedWithValue(
                SecondaryStatsTargetLabel,
                "SecondaryStatsTargetLabel");
            OnPropertyChangedWithValue(TargetHelp,"TargetHelp");
            OnPropertyChangedWithValue(NavalStatus,"NavalStatus");
            OnPropertyChangedWithValue(BonusSummary,"BonusSummary");
            OnPropertyChangedWithValue(status,"Status");
            NotifyStaticSlots();
            DiagnosticJournal.Event("cu_stats_static_slots_notified",
                "visible="+rows.Count+
                "|template=false|static_widgets=6"+
                "|row_page="+(statsRowPageIndex+1));
            if(pageIndex>=0)
                DiagnosticJournal.Event(
                    "cu_stats_button_only_bindings_refreshed",
                    "visible="+rows.Count+
                    "|stat_sliders=0|input=plus_minus_exact"+
                    "|age_slider_unchanged=true"+
                    "|row_page="+(statsRowPageIndex+1));
            DiagnosticJournal.Event("cu_creator_page_checkpoint",
                "page="+(pageIndex==CUNativeStatsNavigationPolicy.Age?
                    "Age":"All Stats")+
                "|surface=stats_editor|phase=bindings_refreshed"+
                "|target="+(editingSpouse?
                    (EditingWandererCompanion?"companion":"spouse"):
                    "player")+
                "|row_page="+(statsRowPageIndex+1));
        }
        private void RefreshRowValues()
        {
            var changes=ChangesForTarget;
            var foundation=BaseForTarget;
            foreach(var row in rows)
            {
                int starting;
                if(!foundation.TryGetValue(row.Key,out starting))continue;
                int chosen;
                if(!changes.TryGetValue(row.Key,out chosen))
                    chosen=starting;
                row.ApplySelected(chosen);
            }
            OnPropertyChangedWithValue(BudgetStatus,"BudgetStatus");
            OnPropertyChangedWithValue(status,"Status");
            NotifyStaticSlots();
        }
        // Fixed stat slots bind directly to this ViewModel. No repeating
        // Gauntlet ItemTemplate or row data context is created at runtime.
        private CUNativeStatRowVM AtSlot(int slot)
        {
            return slot>=0 && slot<rows.Count?rows[slot]:null;
        }
        private bool VisibleSlot(int slot)
        {
            return pageIndex>=0 && AtSlot(slot)!=null;
        }
        private string NameSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?"":row.Name;
        }
        private string BaseSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?"":row.NativeValue;
        }
        private string SelectedSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?"":row.SelectedValue;
        }
        private int ValueSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?0:row.SelectedInt;
        }
        private int MinSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?0:row.Minimum;
        }
        private int MaxSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?1:row.Maximum;
        }
        private int IncrementSlot(int slot)
        {
            var row=AtSlot(slot);
            return row==null?1:row.Increment;
        }
        private void SetSlot(int slot,int next)
        {
            var row=AtSlot(slot);
            if(row!=null && row.SelectedInt!=next)
                ChangeExact(row.Key,next);
        }
        private void ChangeSlot(int slot,int direction)
        {
            var row=AtSlot(slot);
            if(row!=null)Change(row.Key,direction);
        }
        private void ExactSlot(int slot)
        {
            var row=AtSlot(slot);
            if(row==null)return;
            // This is still the native TextInquiryData entry and
            // the exact same validation as +/- and the slider.
            row.ExecuteEditExact();
        }
        private void NotifyStaticSlots()
        {
            // All Stats uses six named rows and button-only editors.
            // Native stat sliders are isolated after the user's repeated
            // Age->All Stats process exit. Notify only visible text fields.
            // The original Age slider remains unchanged.
            OnPropertyChangedWithValue(Slot1Visible,"Slot1Visible");
            OnPropertyChangedWithValue(Slot1Name,"Slot1Name");
            OnPropertyChangedWithValue(Slot1Native,"Slot1Native");
            OnPropertyChangedWithValue(Slot1Selected,"Slot1Selected");




            OnPropertyChangedWithValue(Slot2Visible,"Slot2Visible");
            OnPropertyChangedWithValue(Slot2Name,"Slot2Name");
            OnPropertyChangedWithValue(Slot2Native,"Slot2Native");
            OnPropertyChangedWithValue(Slot2Selected,"Slot2Selected");




            OnPropertyChangedWithValue(Slot3Visible,"Slot3Visible");
            OnPropertyChangedWithValue(Slot3Name,"Slot3Name");
            OnPropertyChangedWithValue(Slot3Native,"Slot3Native");
            OnPropertyChangedWithValue(Slot3Selected,"Slot3Selected");




            OnPropertyChangedWithValue(Slot4Visible,"Slot4Visible");
            OnPropertyChangedWithValue(Slot4Name,"Slot4Name");
            OnPropertyChangedWithValue(Slot4Native,"Slot4Native");
            OnPropertyChangedWithValue(Slot4Selected,"Slot4Selected");




            OnPropertyChangedWithValue(Slot5Visible,"Slot5Visible");
            OnPropertyChangedWithValue(Slot5Name,"Slot5Name");
            OnPropertyChangedWithValue(Slot5Native,"Slot5Native");
            OnPropertyChangedWithValue(Slot5Selected,"Slot5Selected");




            OnPropertyChangedWithValue(Slot6Visible,"Slot6Visible");
            OnPropertyChangedWithValue(Slot6Name,"Slot6Name");
            OnPropertyChangedWithValue(Slot6Native,"Slot6Native");
            OnPropertyChangedWithValue(Slot6Selected,"Slot6Selected");




        }
        [DataSourceProperty] public bool Slot1Visible {get{return VisibleSlot(0);}}
        [DataSourceProperty] public string Slot1Name {get{return NameSlot(0);}}
        [DataSourceProperty] public string Slot1Native {get{return BaseSlot(0);}}
        [DataSourceProperty] public string Slot1Selected {get{return SelectedSlot(0);}}
        [DataSourceProperty] public int Slot1Value
        {
            get{return ValueSlot(0);}
            set{SetSlot(0,value);}
        }
        [DataSourceProperty] public int Slot1Min {get{return MinSlot(0);}}
        [DataSourceProperty] public int Slot1Max {get{return MaxSlot(0);}}
        [DataSourceProperty] public int Slot1Increment {get{return IncrementSlot(0);}}
        public void ExecuteSlot1Increase(){ChangeSlot(0,1);}
        public void ExecuteSlot1Decrease(){ChangeSlot(0,-1);}
        public void ExecuteSlot1Exact(){ExactSlot(0);}
        [DataSourceProperty] public bool Slot2Visible {get{return VisibleSlot(1);}}
        [DataSourceProperty] public string Slot2Name {get{return NameSlot(1);}}
        [DataSourceProperty] public string Slot2Native {get{return BaseSlot(1);}}
        [DataSourceProperty] public string Slot2Selected {get{return SelectedSlot(1);}}
        [DataSourceProperty] public int Slot2Value
        {
            get{return ValueSlot(1);}
            set{SetSlot(1,value);}
        }
        [DataSourceProperty] public int Slot2Min {get{return MinSlot(1);}}
        [DataSourceProperty] public int Slot2Max {get{return MaxSlot(1);}}
        [DataSourceProperty] public int Slot2Increment {get{return IncrementSlot(1);}}
        public void ExecuteSlot2Increase(){ChangeSlot(1,1);}
        public void ExecuteSlot2Decrease(){ChangeSlot(1,-1);}
        public void ExecuteSlot2Exact(){ExactSlot(1);}
        [DataSourceProperty] public bool Slot3Visible {get{return VisibleSlot(2);}}
        [DataSourceProperty] public string Slot3Name {get{return NameSlot(2);}}
        [DataSourceProperty] public string Slot3Native {get{return BaseSlot(2);}}
        [DataSourceProperty] public string Slot3Selected {get{return SelectedSlot(2);}}
        [DataSourceProperty] public int Slot3Value
        {
            get{return ValueSlot(2);}
            set{SetSlot(2,value);}
        }
        [DataSourceProperty] public int Slot3Min {get{return MinSlot(2);}}
        [DataSourceProperty] public int Slot3Max {get{return MaxSlot(2);}}
        [DataSourceProperty] public int Slot3Increment {get{return IncrementSlot(2);}}
        public void ExecuteSlot3Increase(){ChangeSlot(2,1);}
        public void ExecuteSlot3Decrease(){ChangeSlot(2,-1);}
        public void ExecuteSlot3Exact(){ExactSlot(2);}
        [DataSourceProperty] public bool Slot4Visible {get{return VisibleSlot(3);}}
        [DataSourceProperty] public string Slot4Name {get{return NameSlot(3);}}
        [DataSourceProperty] public string Slot4Native {get{return BaseSlot(3);}}
        [DataSourceProperty] public string Slot4Selected {get{return SelectedSlot(3);}}
        [DataSourceProperty] public int Slot4Value
        {
            get{return ValueSlot(3);}
            set{SetSlot(3,value);}
        }
        [DataSourceProperty] public int Slot4Min {get{return MinSlot(3);}}
        [DataSourceProperty] public int Slot4Max {get{return MaxSlot(3);}}
        [DataSourceProperty] public int Slot4Increment {get{return IncrementSlot(3);}}
        public void ExecuteSlot4Increase(){ChangeSlot(3,1);}
        public void ExecuteSlot4Decrease(){ChangeSlot(3,-1);}
        public void ExecuteSlot4Exact(){ExactSlot(3);}
        [DataSourceProperty] public bool Slot5Visible {get{return VisibleSlot(4);}}
        [DataSourceProperty] public string Slot5Name {get{return NameSlot(4);}}
        [DataSourceProperty] public string Slot5Native {get{return BaseSlot(4);}}
        [DataSourceProperty] public string Slot5Selected {get{return SelectedSlot(4);}}
        [DataSourceProperty] public int Slot5Value
        {
            get{return ValueSlot(4);}
            set{SetSlot(4,value);}
        }
        [DataSourceProperty] public int Slot5Min {get{return MinSlot(4);}}
        [DataSourceProperty] public int Slot5Max {get{return MaxSlot(4);}}
        [DataSourceProperty] public int Slot5Increment {get{return IncrementSlot(4);}}
        public void ExecuteSlot5Increase(){ChangeSlot(4,1);}
        public void ExecuteSlot5Decrease(){ChangeSlot(4,-1);}
        public void ExecuteSlot5Exact(){ExactSlot(4);}
        [DataSourceProperty] public bool Slot6Visible {get{return VisibleSlot(5);}}
        [DataSourceProperty] public string Slot6Name {get{return NameSlot(5);}}
        [DataSourceProperty] public string Slot6Native {get{return BaseSlot(5);}}
        [DataSourceProperty] public string Slot6Selected {get{return SelectedSlot(5);}}
        [DataSourceProperty] public int Slot6Value
        {
            get{return ValueSlot(5);}
            set{SetSlot(5,value);}
        }
        [DataSourceProperty] public int Slot6Min {get{return MinSlot(5);}}
        [DataSourceProperty] public int Slot6Max {get{return MaxSlot(5);}}
        [DataSourceProperty] public int Slot6Increment {get{return IncrementSlot(5);}}
        public void ExecuteSlot6Increase(){ChangeSlot(5,1);}
        public void ExecuteSlot6Decrease(){ChangeSlot(5,-1);}
        public void ExecuteSlot6Exact(){ExactSlot(5);}

        public void ExecuteEditPlayer()
        {
            editingSpouse=false;
            statsRowPageIndex=0;
            status="Editing the founder after native life-story bonuses.";
            DiagnosticJournal.Event("cu_all_stats_target","target=player");
            Refresh();
        }
        public void ExecuteEditSpouse()
        {
            if(EditingWandererCompanion &&
                !profile.WandererCompanionEnabled)
                return;
            editingSpouse=true;
            statsRowPageIndex=0;
            status=EditingWandererCompanion?
                "Editing the one Wanderer companion. Requested native "+
                "stats are applied when the campaign begins.":
                "Editing spouse before House generates a native Hero."+
                " Manual values influence children's training.";
            DiagnosticJournal.Event("cu_all_stats_target",
                "target="+(EditingWandererCompanion?
                    "wanderer_companion":"spouse"));
            Refresh();
        }
        [DataSourceProperty] public bool IsPlayerTarget
            {get{return !editingSpouse;}}
        [DataSourceProperty] public bool IsSpouseTarget
            {get{return editingSpouse;}}
        [DataSourceProperty] public bool IsSecondaryStatsTargetVisible
            {get{return IsStatsPage && (!EditingWandererCompanion ||
                profile.WandererCompanionEnabled);}}
        [DataSourceProperty] public string SecondaryStatsTargetLabel
            {get{return EditingWandererCompanion?
                "Edit companion stats":"Edit your spouse";}}
        [DataSourceProperty] public string NavalStatus
            {get{return SandboxNativeSkillCatalog.Status();}}
        [DataSourceProperty] public string TargetHelp
            {get{return editingSpouse ?
                (EditingWandererCompanion?
                    "COMPANION | Edit level, attributes, skills, focus "+
                    "and traits. Changes are applied at start.":
                    "SPOUSE | Edit with - / + / Exact. Native values stay"+
                    " unchanged until you choose an override.") :
                "PLAYER | Nine pages. Edit using - / + / Exact."+
                " Naval skills appear with War Sails.";}}

        private void AddRow(string key,string help)
        {
            int native;
            if(!BaseForTarget.TryGetValue(key,out native)) return;
            int chosen;
            bool custom=ChangesForTarget.TryGetValue(key,out chosen);
            if(!custom)chosen=native;
            rows.Add(new CUNativeStatRowVM(key,native,chosen,help,
                Change,ChangeExact,editingSpouse&&!custom,
                editingSpouse));
        }

        private void Change(string key,int direction)
        {
            int current;
            if(!ChangesForTarget.TryGetValue(key,out current))
            {
                if(!BaseForTarget.TryGetValue(key,out current))
                    return;
            }
            ChangeExact(key,current+
                SandboxStatBudgetPolicy.Step(key)*direction);
        }

        private void ChangeExact(string key,int next)
        {
            if(!editingSpouse && profile.ProgressionMode=="Native")
            {
                status="Select Balanced or Unrestricted to edit founder stats.";
                RefreshRowValues();
                return;
            }
            int native;
            if(!BaseForTarget.TryGetValue(key,out native))
            {
                status="Native stat not found.";
                RefreshRowValues();
                return;
            }
            if(!SandboxStatBudgetPolicy.InRange(key,next))
            {
                status="Native stat value must be inside its legal range.";
                RefreshRowValues();
                return;
            }
            if(editingSpouse)
            {
                // Companion baseline is provisional, not the actual
                // native donor's eventual values. An explicit zero is
                // still an exact request, never a hidden reset.
                if(EditingWandererCompanion)
                    ChangesForTarget[key]=next;
                else if(next==native)ChangesForTarget.Remove(key);
                else ChangesForTarget[key]=next;
                status=(EditingWandererCompanion?
                    "Companion":"Spouse")+
                    " stat requested: "+key+" = "+next+
                    ". Applied after native Hero generation.";
                DiagnosticJournal.Event(
                    EditingWandererCompanion?
                        "cu_wanderer_companion_stat_edit":
                        "cu_spouse_stat_slider_edit",
                    "key="+key+"|requested="+next);
                RefreshRowValues();
                return;
            }
            var preview=new SortedDictionary<string,int>(
                profile.Overrides,StringComparer.Ordinal);
            if(next==native)preview.Remove(key);
            else preview[key]=next;
            int spent;
            string reason;
            if(!SandboxStatBudgetPolicy.TryEvaluate(
                profile.ProgressionMode,preview,baseline,out spent,out reason))
            {
                status=reason;
                RefreshRowValues();
                return;
            }
            profile.Overrides.Clear();
            foreach(var pair in preview)profile.Overrides[pair.Key]=pair.Value;
            status="";
            RefreshRowValues();
        }

        private void SelectMode(string mode)
        {
            if(!SandboxStatBudgetPolicy.IsMode(mode)) return;
            profile.ProgressionMode=mode;
            if(mode=="Native") profile.Overrides.Clear();
            int spent;
            string reason;
            if(!SandboxStatBudgetPolicy.TryEvaluate(
                mode,profile.Overrides,baseline,out spent,out reason))
            {
                status="Existing edits exceed the balanced budget. " +
                    "Reset changes or select Unrestricted.";
            }
            else status="";
            Refresh();
        }

        public void ExecuteNative(){SelectMode("Native");}
        public void ExecuteBalanced(){SelectMode("Balanced");}
        public void ExecuteUnrestricted(){SelectMode("Unrestricted");}
        public void ExecutePreviousBonus()
        {
            bonusIndex=Math.Max(0,bonusIndex-2);
            OnPropertyChangedWithValue(BonusSummary,"BonusSummary");
        }
        public void ExecuteNextBonus()
        {
            if(bonusIndex+2<bonusLines.Length)
                bonusIndex+=2;
            OnPropertyChangedWithValue(BonusSummary,"BonusSummary");
        }
        public void ExecuteReset()
        {
            ChangesForTarget.Clear();
            status=editingSpouse?
                "Spouse stat overrides cleared. Native auto values kept.":
                "Player modifications cleared. Native values retained.";
            Refresh();
        }
        public void ExecutePreviousGroup()
        {
            statsRowPageIndex=Math.Max(0,statsRowPageIndex-1);
            DiagnosticJournal.Event("cu_stats_rows_page_button",
                "direction=previous|page="+(statsRowPageIndex+1));
            Refresh();
        }
        public void ExecuteNextGroup()
        {
            statsRowPageIndex=Math.Min(statsRowPageCount-1,
                statsRowPageIndex+1);
            DiagnosticJournal.Event("cu_stats_rows_page_button",
                "direction=next|page="+(statsRowPageIndex+1));
            Refresh();
        }
        public void ExecuteBack()
        {
            // This always goes back ONE page, regardless of stats mode.
            // The native narrative boundary is requested only from Age.
            if(CUNativeStatsNavigationPolicy.BackToNativeNarrative(pageIndex))
            {
                DiagnosticJournal.Event("cu_stats_navigation",
                    "from=Age|back=native_narrative|mode="+
                    profile.ProgressionMode);
                back();
                return;
            }
            int old=pageIndex;
            pageIndex=CUNativeStatsNavigationPolicy.BackPage(pageIndex);
            DiagnosticJournal.Event("cu_stats_navigation",
                "direction=back|from="+old+"|to="+pageIndex+
                "|mode="+profile.ProgressionMode);
            Refresh();
        }
        public void ExecuteContinue()
        {
            // Native/Balanced/Unrestricted never alter screen order.
            // In the past Native prematurely finalized from the Age page,
            // so the same Back/Continue buttons had different meanings.
            if(!CUNativeStatsNavigationPolicy.IsFinal(pageIndex))
            {
                int old=pageIndex;
                pageIndex=CUNativeStatsNavigationPolicy.NextPage(pageIndex);
                DiagnosticJournal.Event("cu_stats_navigation",
                    "direction=next|from="+old+"|to="+pageIndex+
                    "|mode="+profile.ProgressionMode);
                Refresh();
                return;
            }
            if(profile.ProgressionMode=="Native")
                profile.Overrides.Clear();
            int spent;
            string problem;
            if(!SandboxStatBudgetPolicy.TryEvaluate(profile.ProgressionMode,
                profile.Overrides,baseline,out spent,out problem))
            {
                status=problem;
                Refresh();
                return;
            }
            // Apply the exact birthday to the REAL native Hero, not a
            // UI-only number. Preserve the native age selection bonuses,
            // story rewards and real BodyProperties. If anything fails,
            // restore the original birthday and keep this editor open.
            var hero=Hero.MainHero;
            bool ageOverride=profile.PlayerAge>=18;
            CampaignTime oldBirthday=hero==null ?
                CampaignTime.Zero : hero.BirthDay;
            try
            {
                if(ageOverride && hero!=null)
                {
                    hero.SetBirthDay(CampaignTime.YearsFromNow(
                        -profile.PlayerAge));
                    if(!SandboxFamilyAgePolicy.MatchesRequestedAge(
                        hero.Age,profile.PlayerAge))
                        throw new InvalidOperationException(
                            "Native hero age does not match requested year");
                }
                if(!profile.SaveForPlayer(SandboxOriginProfile.UserPath,hero==null?30:(int)hero.Age,out problem))
                    throw new InvalidOperationException(
                        "Could not save starting stats: "+problem);
            }
            catch(Exception error)
            {
                if(ageOverride && hero!=null)
                {
                    try{hero.SetBirthDay(oldBirthday);}catch{}
                }
                DiagnosticJournal.Event("cu_creator_validation_rejected","stage=Stats|"+
                    profile.ValidationContext(hero==null?30:(int)hero.Age)+"|reason="+error.Message);
                status="Could not save age or skills: "+
                    error.Message;
                Refresh();
                return;
            }
            DiagnosticJournal.Event("cu_native_player_age_committed",
                "native_hero=true|age="+
                (hero==null?"none":hero.Age.ToString("0.0"))+
                "|requested="+profile.PlayerAge+
                "|native_story_bonuses_preserved=true");
            DiagnosticJournal.Event("cu_native_stats_committed",
                "spouse_overrides="+profile.SpouseOverrides.Count+
                "|companion_overrides="+
                    profile.WandererCompanionOverrides.Count+
                "|all_stats_one_page=true|mode="+profile.ProgressionMode+
                "|spent="+spent+
                "|budget="+SandboxStatBudgetPolicy.StartingBudget+
                "|changes="+profile.Overrides.Count+
                "|native_baseline=true|before_banner=true");
            completed();
        }

        [DataSourceProperty] public string Title
            {get{return "CALRADIA UNIFIED";}}
        [DataSourceProperty] public string Subtitle
            {get{return "Age and skills";}}
        [DataSourceProperty] public string Page
            {get{return pageIndex<0?"Starting Age":
                "Attributes, skills and traits";}}
        [DataSourceProperty] public string PageProgress
            {get{return "STEP "+
                CUNativeStatsNavigationPolicy.StepNumber(pageIndex)+
                " / "+CUNativeStatsNavigationPolicy.PageCount;}}
        [DataSourceProperty] public string Mode
            {get{return profile.ProgressionMode;}}
        [DataSourceProperty] public string ModeDescription
            {get{return SandboxStatBudgetPolicy.ModeDescription(Mode);}}
        [DataSourceProperty] public string BonusSummary
        {
            get
            {
                int end=Math.Min(bonusIndex+2,bonusLines.Length);
                return "NATIVE EFFECTS "+(bonusIndex+1)+"-"+end+
                    " OF "+bonusLines.Length+"\n"+
                    String.Join("\n\n",bonusLines.Skip(bonusIndex)
                        .Take(2).ToArray());
            }
        }
        [DataSourceProperty] public bool IsNativeMode
            {get{return Mode=="Native";}}
        [DataSourceProperty] public bool IsBalancedMode
            {get{return Mode=="Balanced";}}
        [DataSourceProperty] public bool IsUnrestrictedMode
            {get{return Mode=="Unrestricted";}}
        [DataSourceProperty] public bool IsSkillsPage
            {get{return false;}}
        [DataSourceProperty] public bool IsAgePage
            {get{return pageIndex<0;}}
        [DataSourceProperty] public bool IsStatsPage
            {get{return pageIndex>=0;}}
        [DataSourceProperty] public int PlayerAge
        {
            get
            {
                if(profile.PlayerAge>=18)return profile.PlayerAge;
                var hero=Hero.MainHero;
                return hero==null ? 30 : Math.Max(18,
                    Math.Min(80,(int)hero.Age));
            }
            set
            {
                int age=Math.Max(SandboxFamilyAgePolicy.MinimumPlayerAge,
                    Math.Min(SandboxFamilyAgePolicy.MaximumPlayerAge,value));
                if(age==PlayerAge)return;
                profile.PlayerAge=age;
                status="Age: "+age+
                    ".";
                OnPropertyChangedWithValue(age,"PlayerAge");
                OnPropertyChangedWithValue(AgeDescription,"AgeDescription");
                OnPropertyChangedWithValue(status,"Status");
            }
        }
        [DataSourceProperty] public string AgeDescription
        {
            get
            {
                return "AGE "+PlayerAge+" YEARS  |  CHOOSE 18–80"+
                    "\nEach slider step is one year. The native age"+
                    " choice still decides its original attribute and"+
                    " focus bonuses.";
            }
        }
        public void ExecuteAgeDecrease()
        {
            PlayerAge=PlayerAge-1;
            DiagnosticJournal.Event("cu_native_age_button",
                "direction=decrease|selected="+PlayerAge);
        }
        public void ExecuteAgeIncrease()
        {
            PlayerAge=PlayerAge+1;
            DiagnosticJournal.Event("cu_native_age_button",
                "direction=increase|selected="+PlayerAge);
        }
        public void ExecuteEnterExactAge()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Enter exact starting age",
                "Enter a whole number from 18 to 80.",
                true,false,"Set age","Cancel",
                entered=>{
                    int age;
                    if(!Int32.TryParse(entered,out age) ||
                        !SandboxFamilyAgePolicy.ValidPlayerAge(age))
                    {
                        status="Age must be a whole number from 18 to 80.";
                        Refresh();
                        return;
                    }
                    PlayerAge=age;
                    DiagnosticJournal.Event("cu_native_age_exact_entry",
                        "requested="+age+"|displayed="+PlayerAge);
                },()=>{},false,null,"",PlayerAge.ToString()));
        }
        public void ExecuteKeepNativeAge()
        {
            profile.PlayerAge=-1;
            status="Keep the age selected in the native life story.";
            OnPropertyChangedWithValue(PlayerAge,"PlayerAge");
            OnPropertyChangedWithValue(AgeDescription,"AgeDescription");
            OnPropertyChangedWithValue(status,"Status");
        }
        [DataSourceProperty] public string SkillGroup
            {get{return "STATS "+(statsRowPageIndex+1)+
                " / "+statsRowPageCount;}}
        [DataSourceProperty] public string BudgetStatus
        {
            get
            {
                if(editingSpouse)return
                    (EditingWandererCompanion?"COMPANION | "+
                        profile.WandererCompanionOverrides.Count:
                        "SPOUSE | "+profile.SpouseOverrides.Count)+
                    " MANUAL VALUES | NATIVE AUTO FOR OTHERS";
                if(Mode=="Native")return "NATIVE STATS | NO OVERRIDES";
                if(Mode=="Unrestricted")
                    return "UNRESTRICTED | NATIVE RANGE LIMITS";
                int spent;
                string reason;
                return SandboxStatBudgetPolicy.TryEvaluate(Mode,
                    profile.Overrides,baseline,out spent,out reason) ?
                    "BALANCED | "+spent+" / "+
                        SandboxStatBudgetPolicy.StartingBudget+" POINTS" :
                    "BALANCED | OVER BUDGET";
            }
        }
        [DataSourceProperty] public string NextLabel
        {
            get{return CUNativeStatsNavigationPolicy.NextLabel(pageIndex);}
        }
        [DataSourceProperty] public string Status {get{return status;}}
    }

    /// <summary>
    /// Read-only runtime probe. The pinned public API exposes the actual
    /// NarrativeMenuOption.PositiveEffectText but not a public selected-option
    /// sequence. Inspect only likely private history fields. If unsupported,
    /// show honest native Hero values rather than guessed reward totals.
    /// </summary>
    internal static class NativeNarrativeBonusReader
    {
        public static string[] Read(CharacterCreationManager manager)
        {
            if(manager==null) return new[] {
                "Native background history unavailable."};
            try
            {
                // Verified from Bannerlord.ReferenceAssemblies
                // 1.5.7.125802: CharacterCreationManager.SelectedOptions
                // is a private Dictionary<NarrativeMenu,NarrativeMenuOption>.
                // Read actual chosen effects without changing the narrative.
                var flags=BindingFlags.Public|BindingFlags.NonPublic|
                    BindingFlags.Instance;
                var field=typeof(CharacterCreationManager).GetField(
                    "SelectedOptions",flags);
                var selected=field==null ? null :
                    field.GetValue(manager) as
                        IDictionary<NarrativeMenu,NarrativeMenuOption>;
                if(selected==null)
                    return new[] {
                        "Native background bonuses: current Hero values " +
                        "are shown below. Detailed selection history is " +
                        "not exposed in this build."};
                var descriptions=new List<string>();
                foreach(var option in selected.Values)
                {
                    if(option==null || option.PositiveEffectText==null)
                        continue;
                    string text=option.PositiveEffectText.ToString();
                    if(!String.IsNullOrWhiteSpace(text) &&
                        !descriptions.Contains(text))
                        descriptions.Add(text);
                }
                return descriptions.Count==0 ?
                    new[] {"Native choices are reflected in Hero values. " +
                        "No individual effect text was available."} :
                    descriptions.ToArray();
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception("cu_native_bonus_read",e);
                return new[] {
                    "Live native Hero values shown. The detailed " +
                    "narrative effect list is unavailable."};
            }
        }
    }
}
