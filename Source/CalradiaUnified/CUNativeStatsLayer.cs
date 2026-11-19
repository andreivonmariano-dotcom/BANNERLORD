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
        private static CUNativeStatsVM activeVM;
        private static bool sliderWarningLogged;
        private static bool sliderProbeUnavailable;
        private static object cachedAgeSlider;
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
                    CUNativeStatsNavigationPolicy.Traits :
                    CUNativeStatsNavigationPolicy.Age);
                next=new GauntletLayer("CUNativeStatsEditor",85,false);
                next.IsFocusLayer=true;
                next.InputRestrictions.SetInputRestrictions();
                try
                {
                    nextMovie=next.LoadMovie("CUNativeStatsEditor",vm);
                    DiagnosticJournal.Event("cu_native_creator_assets",
                        "screen=stats|source=installed_game|art=native");
                }
                catch(Exception nativeArtFailure)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_stats_art_fallback",nativeArtFailure);
                    try
                    {
                        // Original flat assets retain the same working
                        // one-year SliderWidget and all game-affecting
                        // callbacks, no missing vanilla texture required.
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
                screen.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=screen;
                layer=next;
                movie=nextMovie;
                activeVM=vm;
                sliderWarningLogged=false;
                sliderProbeUnavailable=false;
                cachedAgeSlider=null;
                failedHost=null;
                DiagnosticJournal.Event("cu_native_stats_step_attached",
                    "transition=after_narrative_before_banner" +
                    "|hero_real=true|native_stages_preserved=true" +
                    "|default_mode=Native|no_experimental_toggle=true"+
                    "|entry_page="+
                    (returningFromNativeBanner?"Traits":"Age"));
            }
            catch(Exception e)
            {
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

        private static object FindSliderWidget(object root)
        {
            if(root==null)return null;
            // Public FindChild is present on some supported Gauntlet builds.
            // The pinned metadata confirms Widget.Id and ChildCount but
            // does not guarantee any one FindChild overload.
            try
            {
                var find=root.GetType().GetMethod("FindChild",
                    new[] {typeof(string),typeof(bool)});
                if(find!=null)
                {
                    object found=find.Invoke(root,new object[] {
                        "CUPlayerAgeSlider",true});
                    if(found!=null)return found;
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_age_find_child_fallback",error);
            }
            return FindSliderInTree(root,0);
        }

        private static object FindSliderInTree(object root,int depth)
        {
            // Iterative depth limit protects against malformed/recursive
            // prefab trees. The plain and native-art movies share IDs.
            if(root==null || depth>24)return null;
            var type=root.GetType();
            var id=type.GetProperty("Id");
            if(id!=null &&
                String.Equals(id.GetValue(root,null) as string,
                    "CUPlayerAgeSlider",StringComparison.Ordinal))
                return root;
            var countProp=type.GetProperty("ChildCount");
            if(countProp==null)return null;
            object raw=countProp.GetValue(root,null);
            if(!(raw is int))return null;
            int count=Math.Min((int)raw,512);
            var oneIndex=new[] {typeof(int)};
            var getChild=type.GetMethod("GetChild",
                BindingFlags.Public|BindingFlags.NonPublic|
                    BindingFlags.Instance,null,oneIndex,null);
            if(getChild==null)
                getChild=type.GetMethod("GetChildAt",
                    BindingFlags.Public|BindingFlags.NonPublic|
                        BindingFlags.Instance,null,oneIndex,null);
            if(getChild!=null)
            {
                for(int index=0;index<count;index++)
                {
                    var child=getChild.Invoke(root,new object[] {index});
                    var matched=FindSliderInTree(child,depth+1);
                    if(matched!=null)return matched;
                }
            }
            return null;
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
                    cachedAgeSlider=FindSliderWidget(movie.Movie.RootWidget);
                    if(cachedAgeSlider==null)
                    {
                        sliderProbeUnavailable=true;
                        DiagnosticJournal.Event("cu_native_age_slider_probe",
                            "supported=false|reason=slider_not_found"+
                            "|fallback=one_year_buttons");
                        return;
                    }
                    DiagnosticJournal.Event("cu_native_age_slider_probe",
                        "supported=true|source=real_gauntlet_tree");
                }
                var valueProperty=cachedAgeSlider.GetType().GetProperty(
                    "ValueInt");
                if(valueProperty==null)
                {
                    sliderProbeUnavailable=true;
                    return;
                }
                object raw=valueProperty.GetValue(cachedAgeSlider,null);
                if(!(raw is int))return;
                int age=(int)raw;
                if(SandboxFamilyAgePolicy.ValidPlayerAge(age) &&
                    age!=activeVM.PlayerAge)
                {
                    activeVM.PlayerAge=age;
                    DiagnosticJournal.Event("cu_native_age_slider_drag",
                        "chosen_year="+age+"|source=real_slider_widget");
                }
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
        private readonly int selected;
        private readonly Action<string,int> change;
        private readonly Action<string,int> setExact;

        internal CUNativeStatRowVM(string key,int nativeValue,int selectedValue,
            string help,Action<string,int> onChange,
            Action<string,int> onSetExact)
        {
            this.key=key;
            string originalName=key.Substring(key.IndexOf('.')+1);
            name=key.StartsWith("Focus.",StringComparison.Ordinal) ?
                "Focus: "+originalName : key.StartsWith("Skill.",
                StringComparison.Ordinal) ? "Skill: "+originalName :
                originalName;
            description=help;
            native=nativeValue;
            selected=selectedValue;
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
        public string NativeValue {get{return "BASE "+native;}}

        [DataSourceProperty]
        public string SelectedValue {get{return "NEW "+selected;}}

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
        private readonly Action completed;
        private readonly Action back;
        // One short exact-age page follows the native story and precedes
        // optional attributes/skills/traits. The narrative owns its bonuses.
        private int pageIndex=CUNativeStatsNavigationPolicy.Age;
        private int skillGroupIndex;
        private string status="";
        private readonly string[] bonusLines;
        private int bonusIndex;

        [DataSourceProperty]
        public MBBindingList<CUNativeStatRowVM> Rows {get;private set;}

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
            completed=onCompleted;
            back=onBack;
            Rows=new MBBindingList<CUNativeStatRowVM>();
            bonusLines=NativeNarrativeBonusReader.Read(manager);
            Refresh();
        }

        private void Refresh()
        {
            Rows.Clear();
            string current=pageIndex<0 ? "Starting Age" :
                SandboxStatBudgetPolicy.Pages[pageIndex];
            foreach(string name in current=="Starting Age" ?
                new string[0] : current=="Attributes" ?
                SandboxOriginProfile.Attributes :
                current=="Skills" ?
                SandboxOriginProfile.Skills.Where(s=>
                    SandboxStatBudgetPolicy.GroupForSkill(s)==
                    SandboxStatBudgetPolicy.SkillGroups[skillGroupIndex]).ToArray() :
                SandboxOriginProfile.Traits)
            {
                if(current=="Skills")
                {
                    AddRow("Skill."+name,
                        "SKILL LEVEL | 0–330. Increases by 10 per click.");
                    AddRow("Focus."+name,
                        "FOCUS POINTS | Native cap: 5. Learning bonus.");
                }
                else if(current=="Traits")
                    AddRow("Trait."+name,
                        SandboxStatBudgetPolicy.TraitDescription(name));
                else
                    AddRow("Attribute."+name,
                        "Native "+name+" governs its related skills.");
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
            OnPropertyChangedWithValue(status,"Status");
        }

        private void AddRow(string key,string help)
        {
            int native;
            if(!baseline.TryGetValue(key,out native)) return;
            int chosen;
            if(!profile.Overrides.TryGetValue(key,out chosen)) chosen=native;
            Rows.Add(new CUNativeStatRowVM(key,native,chosen,help,
                Change,ChangeExact));
        }

        private void Change(string key,int direction)
        {
            if(profile.ProgressionMode=="Native")
            {
                status="Select Balanced or Unrestricted to edit values.";
                Refresh();
                return;
            }
            int native;
            if(!baseline.TryGetValue(key,out native))
            {
                status="Native value not found. This field cannot be edited.";
                Refresh();
                return;
            }
            int current;
            if(!profile.Overrides.TryGetValue(key,out current)) current=native;
            int next=current+SandboxStatBudgetPolicy.Step(key)*direction;
            ChangeExact(key,next);
        }

        private void ChangeExact(string key,int next)
        {
            if(profile.ProgressionMode=="Native")
            {
                status="Select Balanced or Unrestricted to edit values.";
                Refresh();
                return;
            }
            int native;
            if(!baseline.TryGetValue(key,out native))
            {
                status="Native value not found. This field cannot be edited.";
                Refresh();
                return;
            }
            if(!SandboxStatBudgetPolicy.InRange(key,next))
            {
                status="Native value limit reached.";
                Refresh();
                return;
            }
            var preview=new SortedDictionary<string,int>(
                profile.Overrides,StringComparer.Ordinal);
            if(next==native) preview.Remove(key);
            else preview[key]=next;
            int spent;
            string reason;
            if(!SandboxStatBudgetPolicy.TryEvaluate(
                profile.ProgressionMode,preview,baseline,out spent,out reason))
            {
                status=reason;
                Refresh();
                return;
            }
            profile.Overrides.Clear();
            foreach(var pair in preview)profile.Overrides[pair.Key]=pair.Value;
            status="";
            Refresh();
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
            profile.Overrides.Clear();
            status="All modifications cleared. Native values retained.";
            Refresh();
        }
        public void ExecutePreviousGroup()
        {
            skillGroupIndex=(skillGroupIndex+
                SandboxStatBudgetPolicy.SkillGroups.Length-1)%
                SandboxStatBudgetPolicy.SkillGroups.Length;
            Refresh();
        }
        public void ExecuteNextGroup()
        {
            skillGroupIndex=(skillGroupIndex+1)%
                SandboxStatBudgetPolicy.SkillGroups.Length;
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
                    hero.SetBirthDay(CampaignTime.YearsFromNow(
                        -profile.PlayerAge));
                if(!profile.Save(SandboxOriginProfile.UserPath,out problem))
                    throw new InvalidOperationException(
                        "Could not save starting stats: "+problem);
            }
            catch(Exception error)
            {
                if(ageOverride && hero!=null)
                {
                    try{hero.SetBirthDay(oldBirthday);}catch{}
                }
                status="Starting age or stats were not saved: "+
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
                "mode="+profile.ProgressionMode+
                "|spent="+spent+
                "|budget="+SandboxStatBudgetPolicy.StartingBudget+
                "|changes="+profile.Overrides.Count+
                "|native_baseline=true|before_banner=true");
            completed();
        }

        [DataSourceProperty] public string Title
            {get{return "CALRADIA UNIFIED";}}
        [DataSourceProperty] public string Subtitle
            {get{return "CHAPTER 02 OF 05 | STARTING STATS & TRAITS";}}
        [DataSourceProperty] public string Page
            {get{return pageIndex<0?"Starting Age":
                SandboxStatBudgetPolicy.Pages[pageIndex];}}
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
            {get{return pageIndex==1;}}
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
                status="Exact starting age: "+age+
                    ". Original native story rewards are unchanged.";
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
        public void ExecuteAgeDecrease(){PlayerAge=PlayerAge-1;}
        public void ExecuteAgeIncrease(){PlayerAge=PlayerAge+1;}
        public void ExecuteKeepNativeAge()
        {
            profile.PlayerAge=-1;
            status="Keep the age selected in the native life story.";
            OnPropertyChangedWithValue(PlayerAge,"PlayerAge");
            OnPropertyChangedWithValue(AgeDescription,"AgeDescription");
            OnPropertyChangedWithValue(status,"Status");
        }
        [DataSourceProperty] public string SkillGroup
            {get{return SandboxStatBudgetPolicy.SkillGroups[skillGroupIndex];}}
        [DataSourceProperty] public string BudgetStatus
        {
            get
            {
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
