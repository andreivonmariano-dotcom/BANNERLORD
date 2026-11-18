using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandBox;
using SandBox.AdvancedStartOptions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.MountAndBlade;

namespace CalradiaUnified
{
    /// <summary>
    /// Main-menu Sandbox front end for Bannerlord v1.5.7. The normal
    /// SandBoxNewGame click opens this screen BEFORE the built-in
    /// Default Start / Advanced Start selector is constructed.
    /// </summary>
    public static class SandboxCreatorOverlay
    {
        private static bool creatorOpen;
        private static bool launchQueued;
        private static bool cancelled;

        public static bool BlockNewGameProfile { get { return cancelled; } }

        public static void OpenFromMainMenu(Action nativeStart)
        {
            if (creatorOpen || launchQueued) return;
            var profile = SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            if (!profile.OverhauledCreatorEnabled)
            {
                DiagnosticJournal.Event("sandbox_creator_native_fallback",
                    "main_menu_opt_out=true");
                if (nativeStart != null) nativeStart();
                return;
            }

            try
            {
                if (ScreenManager.TopScreen == null)
                    throw new InvalidOperationException("Main menu screen not active");
                creatorOpen = true;
                cancelled = false;
                ScreenManager.PushScreen(new SandboxCreatorScreen());
                DiagnosticJournal.Event("sandbox_creator_screen_requested",
                    "entry=SandBoxNewGame|pre_game=true|native_selector_skipped=true");
            }
            catch (Exception error)
            {
                creatorOpen = false;
                DiagnosticJournal.Exception("sandbox_creator_screen_failed", error);
                try
                {
                    if (ScreenManager.TopScreen is SandboxCreatorScreen)
                        ScreenManager.PopScreen();
                }
                catch (Exception secondary)
                {
                    DiagnosticJournal.Exception("sandbox_creator_recovery_failed",
                        secondary);
                }
                // The only fallback is the preserved original menu action.
                // Do not trap the player on a broken custom screen.
                if (nativeStart != null) nativeStart();
            }
        }

        public static void Tick()
        {
            if (!launchQueued || ScreenManager.TopScreen is SandboxCreatorScreen)
                return;
            launchQueued = false;
            try
            {
                // Same no-selection startup as Bannerlord v1.5.7's
                // GauntletCampaignStartingOptionsView.OnConfirm.
                // The game then owns its normal cinematic and character
                // creation finalization stages.
                var options = AdvancedStartOptionsManager.CreateCampaignStartOptions();
                MBGameManager.StartNewGame(new SandBoxGameManager(
                    () => new Campaign(CampaignGameMode.Campaign,
                        options.GetChangedOptions())));
                DiagnosticJournal.Event("sandbox_direct_game_start_requested",
                    "route=default|native_start_selector_skipped=true");
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception("sandbox_direct_game_start_failed",
                    error);
            }
        }

        public static void Accept()
        {
            cancelled = false;
            if (creatorOpen) launchQueued = true;
            creatorOpen = false;
            DiagnosticJournal.Event("sandbox_creator_pre_game_accepted",
                "queue_native_campaign_start=true");
        }

        public static void Cancel()
        {
            cancelled = true;
            creatorOpen = false;
            launchQueued = false;
            DiagnosticJournal.Event("sandbox_creator_exit",
                "returned_to_main_menu=true|campaign_not_started=true");
        }

        public static void Clear()
        {
            creatorOpen = false;
            launchQueued = false;
            cancelled = false;
        }
    }

    public sealed class SandboxCreatorScreen : ScreenBase
    {
        private GauntletLayer layer;
        private SandboxCreatorViewModel viewModel;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            viewModel = new SandboxCreatorViewModel(CloseSafely);
            layer = new GauntletLayer("CalradiaUnifiedSandboxCreator", 150, false)
            {
                IsFocusLayer = true
            };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUSandboxCreator", viewModel);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (layer != null) ScreenManager.TrySetFocus(layer);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            // Physical escape works even if a movie button has bad dimensions.
            if (ScreenManager.TopScreen == this &&
                Input.IsKeyReleased(InputKey.Escape))
            {
                SandboxCreatorOverlay.Cancel();
                CloseSafely();
            }
        }

        private void CloseSafely()
        {
            if (ScreenManager.TopScreen != this) return;
            ScreenManager.PopScreen();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            if (layer != null)
            {
                layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(layer);
            }
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            if (layer != null) RemoveLayer(layer);
            layer = null;
            viewModel = null;
        }
    }

    public sealed class SandboxCreatorViewModel : ViewModel
    {
        private sealed class Field
        {
            internal string Page, Key, Label, Kind;
            internal int Min, Max, Step;
            internal string[] Choices;
            internal Field(string page, string key, string label,
                string kind, int min=0, int max=0, int step=1,
                string[] choices=null)
            {
                Page=page; Key=key; Label=label; Kind=kind;
                Min=min; Max=max; Step=step; Choices=choices;
            }
        }

        private static readonly string[] Pages =
        {
            "Identity", "Household", "Progression",
            "Equipment", "Resources", "Location", "Review"
        };

        private readonly Action close;
        private readonly List<Field> fields;
        private SandboxOriginProfile profile;
        private string page="Identity";
        private int selectedIndex;
        private int pendingNumber;
        private string status="";

        public SandboxCreatorViewModel(Action close)
        {
            this.close=close;
            profile=SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            // A stale prior profile must not become active simply from
            // visiting the new creator, including a cancelled new Sandbox.
            profile.Enabled=false;
            profile.NativeStageEnabled=false;
            profile.RealFamilyEnabled=false;
            profile.RoleAffiliationsEnabled=false;
            string error;
            if (!profile.Save(SandboxOriginProfile.UserPath,out error))
                status="Profile could not be reset: "+error;
            fields=BuildFields();
            Refresh();
        }

        private static List<Field> BuildFields()
        {
            var f=new List<Field>
            {
                new Field("Identity","Role","Starting role","choice",
                    choices:SandboxOriginProfile.Roles),
                new Field("Identity","StartingCulture",
                    "Preferred culture (confirm in native appearance stage)",
                    "choice",choices:new[]{"","empire","vlandia","sturgia",
                        "aserai","khuzait","battania"}),
                new Field("Identity","PlayerAge",
                    "Starting age (-1 means native)","number",18,80),
                new Field("Household","SpouseEnabled",
                    "Begin married","boolean"),
                new Field("Household","SpouseName",
                    "Spouse name","text"),
                new Field("Household","SpouseAge",
                    "Spouse age","number",18,70),
                new Field("Household","SpouseGender",
                    "Spouse gender","choice",
                    choices:new[]{"Female","Male"}),
                new Field("Household","SpouseCulture",
                    "Spouse culture ID","text"),
                new Field("Household","SpouseFaceSeed",
                    "Spouse appearance template ID","text"),
                new Field("Household","ChildCount",
                    "Number of children (0-6)","number",0,6),
                new Field("Household","SiblingCount",
                    "Number of siblings (0-4)","number",0,4),
                new Field("Equipment","EquipmentKit",
                    "Starting equipment theme","choice",
                    choices:SandboxOriginProfile.Kits),
                new Field("Resources","Gold",
                    "Starting gold (-1 means native)","number",0,2000000,1000),
                new Field("Resources","Influence",
                    "Starting influence (-1 means native)","number",0,100000,100),
                new Field("Resources","Troops",
                    "Additional native-culture recruits","number",0,50),
                new Field("Location","StartingTown",
                    "Preferred starting settlement ID","text"),
                new Field("Review","RealFamilyEnabled",
                    "EXPERIMENTAL: create real spouse and children","boolean"),
                new Field("Review","RoleAffiliationsEnabled",
                    "EXPERIMENTAL: request native military contracts","boolean")
            };
            foreach (string attribute in SandboxOriginProfile.Attributes)
                f.Add(new Field("Progression","Attribute."+attribute,
                    "Attribute: "+attribute,"number",1,10));
            foreach (string skill in SandboxOriginProfile.Skills)
            {
                f.Add(new Field("Progression","Skill."+skill,
                    "Skill: "+skill,"number",0,330,5));
                f.Add(new Field("Progression","Focus."+skill,
                    "Focus: "+skill,"number",0,5));
            }
            foreach (string trait in SandboxOriginProfile.Traits)
                f.Add(new Field("Progression","Trait."+trait,
                    "Personality: "+trait,"number",-2,2));
            for (int i=1;i<=SandboxOriginProfile.MaxChildren;i++)
            {
                string prefix="Child."+i+".";
                f.Add(new Field("Household",prefix+"Name",
                    "Child "+i+" name","text"));
                f.Add(new Field("Household",prefix+"Age",
                    "Child "+i+" age","number",0,55));
                f.Add(new Field("Household",prefix+"Gender",
                    "Child "+i+" gender","choice",
                    choices:new[]{"Female","Male"}));
                f.Add(new Field("Household",prefix+"FaceSeed",
                    "Child "+i+" appearance template ID","text"));
            }
            for (int i=1;i<=SandboxOriginProfile.MaxSiblings;i++)
            {
                string prefix="Sibling."+i+".";
                f.Add(new Field("Household",prefix+"Name",
                    "Sibling "+i+" name","text"));
                f.Add(new Field("Household",prefix+"Age",
                    "Sibling "+i+" age","number",18,80));
                f.Add(new Field("Household",prefix+"Gender",
                    "Sibling "+i+" gender","choice",
                    choices:new[]{"Female","Male"}));
            }
            for (int slot=1;slot<=12;slot++)
                f.Add(new Field("Equipment","Item."+slot,
                    "Exact game item ID "+slot,"text"));
            return f;
        }

        private Field Current
        {
            get
            {
                var choices=fields.Where(f=>f.Page==page).ToArray();
                return choices.Length>0 ? choices[Math.Min(selectedIndex,
                    choices.Length-1)] : null;
            }
        }

        private string RawValue(string key)
        {
            var field=typeof(SandboxOriginProfile).GetField(key);
            if (field!=null)
            {
                object value=field.GetValue(profile);
                return value is bool
                    ? ((bool)value ? "true":"false")
                    : Convert.ToString(value,CultureInfo.InvariantCulture) ?? "";
            }
            int number;
            if (profile.Overrides.TryGetValue(key,out number))
                return number.ToString(CultureInfo.InvariantCulture);
            if (key.StartsWith("Item.",StringComparison.Ordinal))
            {
                int slot;
                return Int32.TryParse(key.Substring(5),out slot) &&
                    profile.ItemIds.ContainsKey(slot) ? profile.ItemIds[slot]:"";
            }
            var parts=key.Split('.');
            int index;
            if (parts.Length==3 && Int32.TryParse(parts[1],out index) &&
                index>=1)
            {
                var relatives=parts[0]=="Child" ? profile.Children :
                    parts[0]=="Sibling" ? profile.Siblings : null;
                if (relatives!=null && index<=relatives.Count)
                {
                    var relative=relatives[index-1];
                    switch (parts[2])
                    {
                        case "Name": return relative.Name;
                        case "Age": return relative.Age.ToString();
                        case "Gender": return relative.Gender;
                        case "FaceSeed": return relative.FaceSeed;
                    }
                }
            }
            return "";
        }

        private void Refresh()
        {
            Field field=Current;
            int n;
            string value=field==null ? "" : RawValue(field.Key);
            pendingNumber=Int32.TryParse(value,NumberStyles.Integer,
                CultureInfo.InvariantCulture,out n) ? n :
                field==null ? 0 : field.Min;
            if(field!=null && field.Kind=="number")
                pendingNumber=Math.Max(field.Min,Math.Min(field.Max,pendingNumber));
            OnPropertyChangedWithValue(PageTitle,"PageTitle");
            OnPropertyChangedWithValue(PageDescription,"PageDescription");
            OnPropertyChangedWithValue(SelectedLabel,"SelectedLabel");
            OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            OnPropertyChangedWithValue(SelectedPosition,"SelectedPosition");
            OnPropertyChangedWithValue(IsNumber,"IsNumber");
            OnPropertyChangedWithValue(IsBoolean,"IsBoolean");
            OnPropertyChangedWithValue(IsChoice,"IsChoice");
            OnPropertyChangedWithValue(IsText,"IsText");
            OnPropertyChangedWithValue(IsReviewPage,"IsReviewPage");
            OnPropertyChangedWithValue(NumberMinimum,"NumberMinimum");
            OnPropertyChangedWithValue(NumberMaximum,"NumberMaximum");
            OnPropertyChangedWithValue(pendingNumber,"PendingNumber");
            OnPropertyChangedWithValue(ProfileSummary,"ProfileSummary");
            OnPropertyChangedWithValue(Status,"Status");
        }

        [DataSourceProperty] public string PageTitle { get { return page; } }
        [DataSourceProperty] public string PageDescription
        {
            get
            {
                if (page=="Identity") return "Choose your own Calradian identity. King and Admiral are experimental, not instant titles.";
                if (page=="Household") return "Plan a spouse, children or siblings. Actual hero creation requires separate consent.";
                if (page=="Progression") return "Set attributes, 18 skills, focus and personality traits for your new character.";
                if (page=="Equipment") return "Select a kit theme and exact existing game item IDs.";
                if (page=="Resources") return "Set your treasury, influence and basic native-culture recruits.";
                if (page=="Location") return "Record the town where you want to start. Actual relocation is pending.";
                return "Review the profile, opt in to risky actions only on a disposable campaign, then apply.";
            }
        }
        [DataSourceProperty] public string SelectedLabel
        {
            get { return Current==null ? "" : Current.Label; }
        }
        [DataSourceProperty] public string SelectedValue
        {
            get
            {
                if(Current==null) return "";
                string value=RawValue(Current.Key);
                if(value=="-1" || value=="") return "Use the native/default value";
                if(Current.Kind=="number") return pendingNumber.ToString();
                return value;
            }
        }
        [DataSourceProperty] public string SelectedPosition
        {
            get
            {
                int count=fields.Count(f=>f.Page==page);
                return "Field "+(selectedIndex+1)+" of "+count;
            }
        }
        [DataSourceProperty] public bool IsNumber
        {
            get { return Current!=null && Current.Kind=="number"; }
        }
        [DataSourceProperty] public bool IsBoolean
        {
            get { return Current!=null && Current.Kind=="boolean"; }
        }
        [DataSourceProperty] public bool IsChoice
        {
            get { return Current!=null && Current.Kind=="choice"; }
        }
        [DataSourceProperty] public bool IsText
        {
            get { return Current!=null && Current.Kind=="text"; }
        }
        [DataSourceProperty] public bool IsReviewPage { get {return page=="Review";} }
        [DataSourceProperty] public int NumberMinimum
        {
            get {return Current==null?0:Current.Min;}
        }
        [DataSourceProperty] public int NumberMaximum
        {
            get {return Current==null?0:Current.Max;}
        }
        [DataSourceProperty] public int PendingNumber
        {
            get { return pendingNumber; }
            set
            {
                if(!IsNumber) return;
                pendingNumber=Math.Max(NumberMinimum,Math.Min(NumberMaximum,value));
                OnPropertyChangedWithValue(pendingNumber,"PendingNumber");
                OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            }
        }
        [DataSourceProperty] public string Status {get {return status;} }
        [DataSourceProperty] public string ProfileSummary
        {
            get
            {
                return profile.Role+" | "+(profile.SpouseEnabled ? "married" : "independent")+
                    " | children: "+profile.ChildCount+
                    " | gold: "+(profile.Gold<0 ? "native":profile.Gold.ToString())+
                    " | family creation: "+(profile.RealFamilyEnabled?"EXPERIMENTAL":"off");
            }
        }

        private void Switch(string next)
        {
            DiagnosticJournal.Event("sandbox_creator_page_clicked", "page=" + next);
            page=next;
            selectedIndex=0;
            Refresh();
        }
        public void ExecuteIdentity() {Switch("Identity");}
        public void ExecuteHousehold() {Switch("Household");}
        public void ExecuteProgression() {Switch("Progression");}
        public void ExecuteEquipment() {Switch("Equipment");}
        public void ExecuteResources() {Switch("Resources");}
        public void ExecuteLocation() {Switch("Location");}
        public void ExecuteReview() {Switch("Review");}

        public void ExecuteNextField()
        {
            int count=fields.Count(f=>f.Page==page);
            if(count>0) selectedIndex=(selectedIndex+1)%count;
            Refresh();
        }
        public void ExecutePreviousField()
        {
            int count=fields.Count(f=>f.Page==page);
            if(count>0) selectedIndex=(selectedIndex+count-1)%count;
            Refresh();
        }

        private void SetStatus(string message)
        {
            status=message;
            OnPropertyChangedWithValue(status,"Status");
        }

        private bool Write(string key,string value)
        {
            var candidate=SandboxOriginProfile.Parse(profile.Serialize());
            string error;
            if(!candidate.TrySet(key,value))
            {
                SetStatus("Invalid value for "+key+".");
                return false;
            }
            if(!candidate.Save(SandboxOriginProfile.UserPath,out error))
            {
                SetStatus("Not saved: "+error);
                return false;
            }
            profile=candidate;
            DiagnosticJournal.Event("sandbox_overhaul_field_saved",
                "key="+key+"|role="+profile.Role);
            SetStatus("Saved "+key+" for the new Sandbox.");
            Refresh();
            return true;
        }

        public void ExecuteToggle()
        {
            if(IsBoolean)
                Write(Current.Key,RawValue(Current.Key)=="true"?"false":"true");
        }
        private void Choose(int direction)
        {
            if(!IsChoice || Current.Choices==null ||
                Current.Choices.Length==0) return;
            var options=Current.Choices;
            int index=Array.IndexOf(options,RawValue(Current.Key));
            if(index<0) index=0;
            index=(index+options.Length+direction)%options.Length;
            Write(Current.Key,options[index]);
        }
        public void ExecutePreviousChoice(){Choose(-1);}
        public void ExecuteNextChoice(){Choose(1);}
        public void ExecuteDecrease()
        {
            if(IsNumber)
                PendingNumber=(int)Math.Max(NumberMinimum,
                    (long)pendingNumber-Current.Step);
        }
        public void ExecuteIncrease()
        {
            if(IsNumber)
                PendingNumber=(int)Math.Min(NumberMaximum,
                    (long)pendingNumber+Current.Step);
        }
        public void ExecuteSaveNumber()
        {
            if(IsNumber)
                Write(Current.Key,pendingNumber.ToString(
                    CultureInfo.InvariantCulture));
        }
        public void ExecuteUseNative()
        {
            if(Current!=null && (Current.Key=="Gold"||
                Current.Key=="Influence"||Current.Key=="PlayerAge"))
                Write(Current.Key,"-1");
        }
        public void ExecuteEditText()
        {
            if(!IsText) return;
            string key=Current.Key;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                Current.Label,"Only validated profile data are saved.",
                true,true,"Save","Cancel",
                text=>Write(key,text),()=>{},false,null,"",RawValue(key)));
        }

        public void ExecuteApplyAndContinue()
        {
            var candidate=SandboxOriginProfile.Parse(profile.Serialize());
            candidate.Enabled=true;
            candidate.NativeStageEnabled=false;
            string error;
            if(!candidate.Save(SandboxOriginProfile.UserPath,out error))
            {
                SetStatus("Profile not activated: "+error+
                    ". Check spouse and child ages.");
                return;
            }
            profile=candidate;
            SandboxCreatorOverlay.Accept();
            DiagnosticJournal.Event("sandbox_overhaul_accepted",
                "role="+profile.Role+"|hand_off=native_appearance_and_finish");
            if(close!=null) close();
        }

        public void ExecuteReturnToNative()
        {
            // Do not grant old origin resources if this front-end was cancelled.
            profile.Enabled=false;
            string error;
            profile.Save(SandboxOriginProfile.UserPath,out error);
            SandboxCreatorOverlay.Cancel();
            if(close!=null) close();
        }
    }
}
