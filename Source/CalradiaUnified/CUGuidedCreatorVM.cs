using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SandBox.AdvancedStartOptions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// The same full-screen one-question Gauntlet movie is used at the
    /// pre-campaign ASO boundary and later inside native character creation.
    /// World/role can only be changed BEFORE SandBoxGameManager starts.
    /// Family, resources, holdings and household can only be entered AFTER
    /// the native identity/origin/clan steps. No duplicate questions.
    /// </summary>
    public sealed class CUGuidedCreatorVM : ViewModel
    {
        // Session-only marker. The native visual shell reloads family
        // context only after the guided post-clan plan was saved, not on
        // every frame of native character creation.
        internal static int HouseRevision {get;private set;}
        private sealed class Field
        {
            internal readonly string Chapter, Key, Label, Hint, Kind;
            internal readonly string[] Choices;
            internal readonly int Min, Max, Step;
            internal readonly bool Advanced;
            internal Field(string chapter,string key,string label,string hint,
                string kind="choice",string[] choices=null,
                int min=0,int max=0,int step=1,bool advanced=false)
            {
                Chapter=chapter; Key=key; Label=label; Hint=hint;
                Kind=kind; Choices=choices; Min=min; Max=max;
                Step=step; Advanced=advanced;
            }
        }

        private readonly bool preCampaign;
        private readonly Action complete;
        private readonly Action back;
        private SandboxOriginProfile profile;
        private readonly List<Field> fields;
        private int cursor;
        private bool advanced;
        private string status;

        public CUGuidedCreatorVM(bool preCampaign,Action onComplete,Action onBack)
        {
            this.preCampaign=preCampaign;
            complete=onComplete;
            back=onBack;
            profile=SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            if(preCampaign)
            {
                // Prior campaign data must never leak family, fief, traits
                // or any post-origin grant into an unconfirmed new game.
                profile.ResetForNewCampaign();
                string error;
                if(!profile.Save(SandboxOriginProfile.UserPath,out error))
                    status="Could not reset previous starting settings: "+error;
            }
            fields=BuildFields();
            Refresh();
        }

        private List<Field> BuildFields()
        {
            var list=new List<Field>();
            if(preCampaign)
            {
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartingScenario",
                    "Choose your world",
                    "Select the native campaign scenario. It is committed before the campaign exists.",
                    choices:SandboxOriginProfile.Scenarios));
                list.Add(new Field("CHAPTER I | YOUR WORLD","ScenarioFaction1",
                    "First scenario faction",
                    "Select the native unifier or invasion faction.",
                    choices:SandboxOriginProfile.KingdomIds));
                list.Add(new Field("CHAPTER I | YOUR WORLD","ScenarioFaction2",
                    "Second scenario faction",
                    "Select the opponent for the Two-Faction War.",
                    choices:SandboxOriginProfile.KingdomIds));
                list.Add(new Field("CHAPTER I | YOUR WORLD","ScenarioVariant",
                    "World variation",
                    "Choose the native Alternate Calradia variant.",
                    choices:new[]{"alternativecalradiadefault",
                        "alternativecalradiafractured",
                        "alternativecalradiashattered"}));
                list.Add(new Field("CHAPTER I | YOUR WORLD","WorldSeed",
                    "World seed",
                    "Use -1 for a random seed. Exact changes require world restart.",
                    "number",min:-1,max:2000000000,advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartFastMode",
                    "Fast campaign mode","Optional native world modifier.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartBanditSurge",
                    "Bandit surge","Optional native world modifier.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartCivilUnrest",
                    "Civil unrest","Optional native world modifier.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartRecruitShortage",
                    "Recruit shortage","Optional native world modifier.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartSwiftTravel",
                    "Swift travel","Optional native world modifier.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","Role",
                    "Your starting position",
                    "Choose your starting career, then kingdom if the role requires one.",
                    choices:SandboxOriginProfile.Roles));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartingKingdomId",
                    "Starting kingdom",
                    "The selected King, Lord, Knight, Soldier or Admiral start uses this native affiliation.",
                    choices:SandboxOriginProfile.KingdomIds));
            }
            else
            {
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseEnabled",
                    "Begin married?",
                    "Your character's native age and culture are now known. No spouse is created until the campaign begins.",
                    "boolean"));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseName",
                    "Spouse name","Name your generated starting spouse.","text"));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseAge",
                    "Spouse age","Ages must permit the selected children.",
                    "number",min:18,max:70));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseGender",
                    "Spouse gender","Auto chooses the opposite of the player's sex.",
                    choices:new[]{"Auto","Female","Male"}));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseBackground",
                    "Spouse's origin","Choose a supported native family background.",
                    choices:SandboxFamilyOriginPolicy.Backgrounds));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseBirthClanId",
                    "Spouse's noble birth clan","Enter the exact eligible clan ID.",
                    "text"));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseCulture",
                    "Spouse's culture",
                    "Leave blank to inherit the player's actual culture.",
                    choices:new[]{"","vlandia","sturgia","battania",
                        "empire","aserai","khuzait"},advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","ChildCount",
                    "Starting children",
                    "A spouse is required. Ages are checked against the actual player and spouse.",
                    "number",min:0,max:6));
                for(int i=1;i<=SandboxOriginProfile.MaxChildren;i++)
                {
                    string prefix="Child."+i+".";
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Name","Child "+i+" | Name",
                        "Give this child a real native Hero name.","text"));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Age","Child "+i+" | Age",
                        "Child must be at least 16 years younger than both parents.",
                        "number",min:0,max:55));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Gender","Child "+i+" | Gender",
                        "Set this child's native sex.",
                        choices:new[]{"Female","Male"}));
                }
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Gold",
                    "Starting gold",
                    "Use -1 to keep Bannerlord's original start.",
                    "number",min:-1,max:2000000,step:1000));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Influence",
                    "Starting influence",
                    "Use -1 to retain your native starting influence.",
                    "number",min:-1,max:100000,step:100));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Troops",
                    "Additional native recruits",
                    "Up to 50 extra troops from the character's actual culture.",
                    "number",min:0,max:50));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","StartingFiefMode",
                    "Starting holding",
                    "Native preserves Advanced Start holdings. Optional fief transfers require explicit permission.",
                    choices:SandboxFiefPolicy.Modes));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","StartingFiefId",
                    "Exact requested fief",
                    "Use a valid native settlement ID; ownership is checked when the game begins.",
                    "text"));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","AllowStartingFiefTransfer",
                    "Authorize starting fief transfer",
                    "Required only when requesting an extra castle, town or exact fief.",
                    "boolean"));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","FamilyHomeMode",
                    "Household residence",
                    "Choose a valid clan-owned town or castle. A child is never placed in a party.",
                    choices:SandboxFamilyHomePolicy.Modes));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","FamilyHomeId",
                    "Exact family residence",
                    "Specify your owned town or castle's settlement ID.",
                    "text"));
                // The next real Bannerlord Review/Options stage already
                // displays the final character. Do not insert a duplicate
                // CU review page before that genuine native step.
            }
            return list;
        }

        private bool Available(Field field)
        {
            if(field.Advanced && !advanced) return false;
            string key=field.Key;
            if(key=="ScenarioFaction1")
                return profile.StartingScenario=="LastStand" ||
                    profile.StartingScenario=="InvasionId" ||
                    profile.StartingScenario=="unitedempire";
            if(key=="ScenarioFaction2")
                return profile.StartingScenario=="twofactionwar";
            if(key=="ScenarioVariant")
                return profile.StartingScenario=="alternativecalradia";
            if(key=="StartingKingdomId")
                return SandboxNativeStartOptions.RequiresKingdom(
                    SandboxNativeStartOptions.StartTypeForRole(profile.Role));
            if(key=="SpouseName" || key=="SpouseAge" ||
                key=="SpouseGender" || key=="SpouseBackground" ||
                key=="SpouseCulture")
                return profile.SpouseEnabled;
            if(key=="SpouseBirthClanId")
                return profile.SpouseEnabled &&
                    profile.SpouseBackground=="NobleChosen";
            if(key=="ChildCount") return profile.SpouseEnabled;
            if(key.StartsWith("Child.",StringComparison.Ordinal))
            {
                string[] p=key.Split('.');
                int number;
                return p.Length==3 && Int32.TryParse(p[1],out number) &&
                    profile.SpouseEnabled && number<=profile.ChildCount;
            }
            if(key=="StartingFiefMode")
                return SandboxFiefPolicy.IsEligibleRole(profile.Role);
            if(key=="StartingFiefId")
                return SandboxFiefPolicy.IsEligibleRole(profile.Role) &&
                    profile.StartingFiefMode=="Specific";
            if(key=="AllowStartingFiefTransfer")
                return SandboxFiefPolicy.IsEligibleRole(profile.Role) &&
                    profile.StartingFiefMode!="Native";
            if(key=="FamilyHomeMode") return profile.SpouseEnabled;
            if(key=="FamilyHomeId")
                return profile.SpouseEnabled &&
                    profile.FamilyHomeMode=="SpecificOwned";
            return true;
        }

        private Field[] Visible {get {return fields.Where(Available).ToArray();}}
        private Field Current
        {
            get
            {
                var visible=Visible;
                return visible.Length==0 ? null :
                    visible[Math.Min(cursor,visible.Length-1)];
            }
        }

        private string Raw(string key)
        {
            var property=typeof(SandboxOriginProfile).GetField(key);
            if(property!=null)
            {
                var value=property.GetValue(profile);
                return value is bool ? ((bool)value ? "true":"false"):
                    Convert.ToString(value,CultureInfo.InvariantCulture)??"";
            }
            string[] parts=key.Split('.');
            int index;
            if(parts.Length==3 && parts[0]=="Child" &&
                Int32.TryParse(parts[1],out index) && index>=1 &&
                index<=profile.Children.Count)
            {
                var child=profile.Children[index-1];
                switch(parts[2])
                {
                    case "Name":return child.Name;
                    case "Age":return child.Age.ToString(CultureInfo.InvariantCulture);
                    case "Gender":return child.Gender;
                }
            }
            return "";
        }

        private void Refresh()
        {
            cursor=Math.Max(0,Math.Min(cursor,Visible.Length-1));
            // Version-pinned TaleWorlds ViewModel notifications use the
            // value-based API. Do not assume WPF's OnPropertyChanged exists.
            OnPropertyChangedWithValue(Chapter,"Chapter");
            OnPropertyChangedWithValue(StepProgress,"StepProgress");
            OnPropertyChangedWithValue(StepTitle,"StepTitle");
            OnPropertyChangedWithValue(Hint,"Hint");
            OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            OnPropertyChangedWithValue(IsChoice,"IsChoice");
            OnPropertyChangedWithValue(IsBoolean,"IsBoolean");
            OnPropertyChangedWithValue(IsNumber,"IsNumber");
            OnPropertyChangedWithValue(IsText,"IsText");
            OnPropertyChangedWithValue(IsReview,"IsReview");
            OnPropertyChangedWithValue(IsNativeDefaultAvailable,
                "IsNativeDefaultAvailable");
            OnPropertyChangedWithValue(IsLastStep,"IsLastStep");
            OnPropertyChangedWithValue(ContinueLabel,"ContinueLabel");
            OnPropertyChangedWithValue(Status,"Status");
            OnPropertyChangedWithValue(Summary,"Summary");
            OnPropertyChangedWithValue(AdvancedLabel,"AdvancedLabel");
            OnPropertyChangedWithValue(PhaseLabel,"PhaseLabel");
        }

        private void Report(string message)
        {
            status=message;
            OnPropertyChangedWithValue(status,"Status");
        }

        private bool TryWrite(string key,string value)
        {
            var copy=SandboxOriginProfile.Parse(profile.Serialize());
            if(!copy.TrySet(key,value))
            {
                Report("Invalid value for "+key+".");
                return false;
            }
            profile=copy;
            Report("Selected: "+key+" = "+value);
            Refresh();
            return true;
        }

        public void ExecutePreviousChoice() { MoveChoice(-1); }
        public void ExecuteNextChoice() { MoveChoice(+1); }
        private void MoveChoice(int direction)
        {
            Field field=Current;
            if(field==null || field.Kind!="choice" ||
                field.Choices==null || field.Choices.Length==0)return;
            int index=Array.IndexOf(field.Choices,Raw(field.Key));
            if(index<0) index=0;
            index=(index+field.Choices.Length+direction)%
                field.Choices.Length;
            TryWrite(field.Key,field.Choices[index]);
        }
        public void ExecuteToggle()
        {
            Field field=Current;
            if(field!=null && field.Kind=="boolean")
                TryWrite(field.Key,Raw(field.Key)=="true"?"false":"true");
        }
        public void ExecuteIncrease() { MoveNumber(+1); }
        public void ExecuteDecrease() { MoveNumber(-1); }
        private void MoveNumber(int direction)
        {
            Field field=Current;
            if(field==null || field.Kind!="number")return;
            int value;
            if(!Int32.TryParse(Raw(field.Key),out value))return;
            int next=(int)Math.Max(field.Min,
                Math.Min((long)field.Max,(long)value+
                    (long)direction*field.Step));
            TryWrite(field.Key,next.ToString(CultureInfo.InvariantCulture));
        }
        public void ExecuteExact()
        {
            Field field=Current;
            if(field==null || field.Kind!="number")return;
            string key=field.Key;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                field.Label,"Enter a whole number in the native allowed range.",
                true,true,"Apply","Cancel",
                value=>TryWrite(key,value),()=>{},false,null,"",Raw(key)));
        }
        public void ExecuteEditText()
        {
            Field field=Current;
            if(field==null || field.Kind!="text")return;
            string key=field.Key;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                field.Label,field.Hint,true,true,"Apply","Cancel",
                value=>TryWrite(key,value),()=>{},false,null,"",Raw(key)));
        }
        public void ExecuteNativeDefault()
        {
            Field field=Current;
            if(field==null || field.Kind!="number")return;
            if(field.Key=="WorldSeed" || field.Key=="Gold" ||
                field.Key=="Influence")
                TryWrite(field.Key,"-1");
        }
        public void ExecuteAdvanced()
        {
            // Showing optional settings should not silently send the player
            // back to the first question in a long conditional family plan.
            string selected=Current==null?"":Current.Key;
            advanced=!advanced;
            var available=Visible;
            int matched=Array.FindIndex(available,
                field=>field.Key==selected);
            cursor=matched>=0 ? matched :
                Math.Max(0,Math.Min(cursor,available.Length-1));
            Report(advanced ? "Optional world and spouse settings shown.":
                "Only the main choices are shown.");
            Refresh();
        }
        public void ExecuteBack()
        {
            if(cursor>0)
            {
                cursor--;
                Refresh();
            }
            else if(back!=null) back();
        }
        public void ExecuteContinue()
        {
            if(Current==null)return;
            if(cursor<Visible.Length-1)
            {
                cursor++;
                Refresh();
            }
            else if(preCampaign) CommitWorld();
            else CommitHouse();
        }

        private void CommitWorld()
        {
            var candidate=SandboxOriginProfile.Parse(profile.Serialize());
            candidate.Enabled=true;
            candidate.NativeStageEnabled=false;
            candidate.NativeIntegrationReviewEnabled=false;
            candidate.NativeFaceWorkshopEnabled=false;
            candidate.NativeStateShellEnabled=true;
            candidate.NativeFaceDockTrialEnabled=true;
            candidate.RealFamilyEnabled=false;
            SandBox.AdvancedStartOptions.AdvancedStartOptions gameOptions;
            string error;
            if(!SandboxNativeStartOptions.TryPrepare(
                candidate,out gameOptions,out error))
            {
                Report(error);
                return;
            }
            if(!candidate.Save(SandboxOriginProfile.UserPath,out error))
            {
                Report("World start not saved: "+error);
                return;
            }
            DiagnosticJournal.Event("cu_guided_world_start_committed",
                "role="+candidate.Role+"|kingdom="+
                candidate.StartingKingdomId+"|scenario="+
                candidate.StartingScenario+
                "|family_requested=false|native_state_next=true");
            if(complete!=null)complete();
        }

        private void CommitHouse()
        {
            var candidate=SandboxOriginProfile.Parse(profile.Serialize());
            candidate.RealFamilyEnabled=candidate.SpouseEnabled ||
                candidate.ChildCount>0;
            string error;
            if(candidate.ChildCount>0 && !candidate.SpouseEnabled)
            {
                Report("Starting children require a spouse.");
                return;
            }
            error=SandboxFamilyHomePolicy.ValidateRequest(
                candidate.FamilyHomeMode,candidate.FamilyHomeId,
                candidate.SpouseEnabled);
            if(error.Length!=0)
            {
                Report("Family residence: "+error);
                return;
            }
            error=SandboxFiefPolicy.ValidateRequest(
                candidate.Role,candidate.StartingFiefMode,
                candidate.StartingFiefId,candidate.AllowStartingFiefTransfer);
            if(error.Length!=0)
            {
                Report("Starting holding: "+error);
                return;
            }
            var hero=Hero.MainHero;
            int actualAge=hero==null?30:(int)hero.Age;
            if(!candidate.IsValid(actualAge,out error))
            {
                Report("Family and age: "+error);
                return;
            }
            if(!candidate.SaveForPlayer(
                SandboxOriginProfile.UserPath,actualAge,out error))
            {
                Report("Household not saved: "+error);
                return;
            }
            HouseRevision++;
            DiagnosticJournal.Event("cu_guided_house_fortune_committed",
                "age="+actualAge+"|spouse="+candidate.SpouseEnabled+
                "|children="+candidate.ChildCount+"|gold="+
                candidate.Gold+"|fief="+candidate.StartingFiefMode+
                "|native_review_next=true");
            if(complete!=null)complete();
        }

        [DataSourceProperty] public string Title
            {get{return "CALRADIA UNIFIED";}}
        [DataSourceProperty] public string Chapter
            {get{return Current==null?"YOUR LEGACY":Current.Chapter;}}
        [DataSourceProperty] public string PhaseLabel
            {get{return preCampaign ? "YOUR WORLD | BEFORE CHARACTER CREATION" :
                "YOUR HOUSE & FORTUNE | AFTER IDENTITY AND ORIGINS";}}
        [DataSourceProperty] public string StepProgress
            {get{return "STEP "+(cursor+1)+" / "+Visible.Length;}}
        [DataSourceProperty] public string StepTitle
            {get{return Current==null?"":Current.Label;}}
        [DataSourceProperty] public string Hint
            {get{return Current==null?"":Current.Hint;}}
        [DataSourceProperty] public string SelectedValue
        {
            get
            {
                if(Current==null)return "";
                if(Current.Kind=="review")
                    return "Your house and estate are ready. Confirm to " +
                        "continue to the game's final review and difficulty.";
                string raw=Raw(Current.Key);
                return Current.Kind=="boolean" ?
                    (raw=="true"?"Yes":"No"):raw;
            }
        }
        [DataSourceProperty] public bool IsChoice
            {get{return Current!=null && Current.Kind=="choice";}}
        [DataSourceProperty] public bool IsBoolean
            {get{return Current!=null && Current.Kind=="boolean";}}
        [DataSourceProperty] public bool IsNumber
            {get{return Current!=null && Current.Kind=="number";}}
        [DataSourceProperty] public bool IsText
            {get{return Current!=null && Current.Kind=="text";}}
        [DataSourceProperty] public bool IsReview
            {get{return Current!=null && Current.Kind=="review";}}
        [DataSourceProperty] public bool IsNativeDefaultAvailable
            {get{return Current!=null &&
                (Current.Key=="WorldSeed" ||
                Current.Key=="Gold" || Current.Key=="Influence");}}
        [DataSourceProperty] public bool IsLastStep
            {get{return cursor>=Visible.Length-1;}}
        [DataSourceProperty] public string ContinueLabel
            {get{return IsLastStep ?
                (preCampaign?"Continue to your character":
                    "Confirm house & continue to native review") :
                "Continue";}}
        [DataSourceProperty] public string AdvancedLabel
            {get{return advanced?"Hide optional choices":"Show optional choices";}}
        [DataSourceProperty] public string Status
            {get{return status??"";}}
        [DataSourceProperty] public string Summary
        {
            get
            {
                if(preCampaign)
                    return "Starting role: "+profile.Role+"\n"+
                        "Starting kingdom: "+
                        (profile.StartingKingdomId.Length==0?"None":
                            profile.StartingKingdomId)+"\n"+
                        "World: "+profile.StartingScenario+
                        "\nCulture, appearance and origins follow.";
                var hero=Hero.MainHero;
                return "Native character: "+
                    (hero==null?"pending":hero.Name.ToString())+"\n"+
                    "Role: "+profile.Role+"\n"+
                    "Spouse: "+(profile.SpouseEnabled?profile.SpouseName:
                        "No spouse")+"\n"+
                    "Children: "+profile.ChildCount+"\n"+
                    "Gold: "+(profile.Gold<0?"Native":
                        profile.Gold.ToString())+"\n"+
                    "Additional troops: "+profile.Troops+"\n"+
                    "Holding: "+profile.StartingFiefMode+"\n"+
                    "Family home: "+(profile.SpouseEnabled?
                        profile.FamilyHomeMode:"None");
            }
        }
    }
}
