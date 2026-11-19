using System;
using System.Collections;
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
        private static bool narrativeSeen;
        private static bool completed;
        private static bool closeRequested;
        private static bool backRequested;

        public static void Tick(CharacterCreationScreen screen,
            CharacterCreationManager manager)
        {
            if (screen==null || screen.IsFinalized || manager==null)
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
            if(stage=="CharacterCreationNarrativeStage")
            {
                // Replaying native background choices requires a fresh
                // native baseline and another edit confirmation.
                narrativeSeen=true;
                completed=false;
                failedHost=null;
                Detach();
                return;
            }
            if(stage!="CharacterCreationBannerEditorStage")
            {
                Detach();
                return;
            }

            if (closeRequested)
            {
                closeRequested=false;
                completed=true;
                Detach();
                DiagnosticJournal.Event("cu_native_stats_step_closed",
                    "resume=original_banner_stage|wizard_still_native=true");
                return;
            }
            if (backRequested)
            {
                backRequested=false;
                Detach();
                try
                {
                    // Manager, not our UI, controls the real narrative Back.
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
                    ()=>closeRequested=true,()=>backRequested=true);
                next=new GauntletLayer("CUNativeStatsEditor",85,false);
                next.IsFocusLayer=true;
                next.InputRestrictions.SetInputRestrictions();
                nextMovie=next.LoadMovie("CUNativeStatsEditor",vm);
                screen.AddLayer(next);
                ScreenManager.TrySetFocus(next);
                host=screen;
                layer=next;
                movie=nextMovie;
                failedHost=null;
                DiagnosticJournal.Event("cu_native_stats_step_attached",
                    "transition=after_narrative_before_banner" +
                    "|hero_real=true|native_stages_preserved=true" +
                    "|default_mode=Native|no_experimental_toggle=true");
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

        public static void Clear()
        {
            Detach();
            trackedManager=null;
            failedHost=null;
            narrativeSeen=false;
            completed=false;
            closeRequested=false;
            backRequested=false;
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
            name=key.Substring(key.IndexOf('.')+1);
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
        public string NativeValue {get{return "NATIVE "+native;}}

        [DataSourceProperty]
        public string SelectedValue {get{return "CHOSEN "+selected;}}

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
        private int pageIndex;
        private int skillGroupIndex;
        private string status="Choose Native, Balanced or Unrestricted.";
        private readonly string bonusSummary;

        [DataSourceProperty]
        public MBBindingList<CUNativeStatRowVM> Rows {get;private set;}

        internal CUNativeStatsVM(CharacterCreationManager manager,
            SandboxOriginProfile source,Action onCompleted,Action onBack)
        {
            profile=SandboxOriginProfile.Parse(source.Serialize());
            // The existing advanced Resources fields no longer authorize
            // arbitrary early override values. Defaults are native.
            baseline=CUNativeStatsSnapshot.Capture(Hero.MainHero);
            completed=onCompleted;
            back=onBack;
            Rows=new MBBindingList<CUNativeStatRowVM>();
            bonusSummary=NativeNarrativeBonusReader.Read(manager);
            Refresh();
        }

        private void Refresh()
        {
            Rows.Clear();
            string current=SandboxStatBudgetPolicy.Pages[pageIndex];
            foreach(string name in current=="Attributes" ?
                SandboxOriginProfile.Attributes :
                current=="Skills" ?
                SandboxOriginProfile.Skills.Where(s=>
                    SandboxStatBudgetPolicy.GroupForSkill(s)==
                    SandboxStatBudgetPolicy.SkillGroups[skillGroupIndex]).ToArray() :
                SandboxOriginProfile.Traits)
            {
                if(current=="Skills")
                {
                    AddRow("Skill."+name,"Native skill level. "+name+
                        " improves through gameplay.");
                    AddRow("Focus."+name,"Native focus points for "+name+
                        ". These affect skill learning.");
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
            status=key+" selected: "+next+" | native: "+native;
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
            else status=SandboxStatBudgetPolicy.ModeDescription(mode);
            Refresh();
        }

        public void ExecuteNative(){SelectMode("Native");}
        public void ExecuteBalanced(){SelectMode("Balanced");}
        public void ExecuteUnrestricted(){SelectMode("Unrestricted");}
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
            if(pageIndex>0)
            {
                pageIndex--;
                Refresh();
            }
            else back();
        }
        public void ExecuteContinue()
        {
            if(pageIndex<SandboxStatBudgetPolicy.Pages.Length-1)
            {
                pageIndex++;
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
            if(!profile.Save(SandboxOriginProfile.UserPath,out problem))
            {
                status="Could not save starting stats: "+problem;
                Refresh();
                return;
            }
            DiagnosticJournal.Event("cu_native_stats_committed",
                "mode="+profile.ProgressionMode+
                "|spent="+spent+
                "|budget="+SandboxStatBudgetPolicy.StartingBudget+
                "|changes="+profile.Overrides.Count+
                "|native_baseline=true|before_banner=true");
            completed();
        }

        [DataSourceProperty] public string Title
            {get{return "CALRADIA UNIFIED | STARTING STATS";}}
        [DataSourceProperty] public string Subtitle
            {get{return "YOUR CHARACTER | AFTER YOUTH & ADULTHOOD";}}
        [DataSourceProperty] public string Page
            {get{return SandboxStatBudgetPolicy.Pages[pageIndex];}}
        [DataSourceProperty] public string PageProgress
            {get{return "STEP "+(pageIndex+1)+" / 3";}}
        [DataSourceProperty] public string Mode
            {get{return profile.ProgressionMode;}}
        [DataSourceProperty] public string ModeDescription
            {get{return SandboxStatBudgetPolicy.ModeDescription(Mode);}}
        [DataSourceProperty] public string BonusSummary
            {get{return bonusSummary;}}
        [DataSourceProperty] public bool IsNativeMode
            {get{return Mode=="Native";}}
        [DataSourceProperty] public bool IsBalancedMode
            {get{return Mode=="Balanced";}}
        [DataSourceProperty] public bool IsUnrestrictedMode
            {get{return Mode=="Unrestricted";}}
        [DataSourceProperty] public bool IsSkillsPage
            {get{return pageIndex==1;}}
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
            get{return pageIndex==2?"Confirm stats & continue to banner":
                "Next: "+SandboxStatBudgetPolicy.Pages[pageIndex+1];}
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
        public static string Read(CharacterCreationManager manager)
        {
            if(manager==null) return "Native background history unavailable.";
            try
            {
                var type=manager.GetType();
                var flags=BindingFlags.Public|BindingFlags.NonPublic|
                    BindingFlags.Instance;
                object selected=null;
                var prop=type.GetProperty("SelectedOptions",flags);
                if(prop!=null && prop.GetIndexParameters().Length==0)
                    selected=prop.GetValue(manager,null);
                if(selected==null)
                {
                    var field=type.GetField("_selectedOptions",flags) ??
                        type.GetField("SelectedOptions",flags);
                    if(field!=null) selected=field.GetValue(manager);
                }
                var list=selected as IEnumerable;
                if(list==null)
                    return "Native background bonuses: current Hero values " +
                        "are shown below. Detailed selection history is " +
                        "not exposed in this build.";
                var descriptions=new List<string>();
                foreach(object item in list)
                {
                    NarrativeMenuOption option=item as NarrativeMenuOption;
                    if(option==null && item!=null)
                    {
                        var value=item.GetType().GetProperty("Value");
                        if(value!=null)
                            option=value.GetValue(item,null) as NarrativeMenuOption;
                    }
                    if(option==null || option.PositiveEffectText==null)
                        continue;
                    string text=option.PositiveEffectText.ToString();
                    if(!String.IsNullOrWhiteSpace(text) &&
                        !descriptions.Contains(text))
                        descriptions.Add(text);
                }
                return descriptions.Count==0 ?
                    "Native background choices are reflected in the Hero " +
                    "values below. No effect text was available." :
                    "Native life-story bonuses: "+
                        String.Join(" | ",descriptions.Take(5).ToArray());
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception("cu_native_bonus_read",e);
                return "Live native Hero values shown. The detailed " +
                    "narrative effect list is unavailable.";
            }
        }
    }
}
