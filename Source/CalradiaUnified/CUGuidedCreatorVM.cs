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
                list.Add(new Field("CHAPTER III | YOUR HOUSE","FamilyScenario",
                    "Household story",
                    "Natural: varied traits. Honorable: honorable spouse and kinder children. Martial, Scholarly, Mercantile, Seafaring or Strained change family bonds and upbringing.",
                    choices:SandboxFamilyDevelopmentPolicy.Scenarios));
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
                        "Choose a permitted age. The limit adjusts to both parents.",
                        "number",min:0,max:55));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Gender","Child "+i+" | Gender",
                        "Set this child's native sex.",
                        choices:new[]{"Female","Male"}));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Upbringing","Child "+i+" | Upbringing",
                        "FamilyTradition follows the household story; choose Kind, Martial, Scholarly, Mercantile or Independent for this child.",
                        choices:SandboxFamilyDevelopmentPolicy.Upbringings));
                }
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SiblingCount",
                    "Brothers and sisters",
                    "Choose 0 to 4 actual clan siblings. Native parents are generated if missing; relatives share both parents.",
                    "number",min:0,max:SandboxOriginProfile.MaxSiblings));
                for(int i=1;i<=SandboxOriginProfile.MaxSiblings;i++)
                {
                    string prefix="Sibling."+i+".";
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Name","Sibling "+i+" | Name",
                        "Give this brother or sister a name.","text"));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Age","Sibling "+i+" | Age",
                        "An older or younger sibling. Must be an adult and fit native parental ages.",
                        "number",min:18,max:80));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Gender","Sibling "+i+" | Gender",
                        "Choose brother or sister. The same parents are shared.",
                        choices:new[]{"Male","Female"}));
                }
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "NobleBirthPosition","Your noble birth",
                    "Optional: be a real younger brother or sister of an existing lord or king in the selected kingdom. Requires an older native noble with both parents.",
                    choices:SandboxNobleStartPolicy.BirthPositions));
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "NobleElderHeroId","Older noble's Hero ID",
                    "Optional exact native hero ID. Leave blank to select the first matching older clan leader with verified parents.",
                    "text",advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "RetinueMode","Your starting household officers",
                    "Native: keep the game's roster. RoleBased: add culture-matched knights and officers. ReplaceNative: first recruit your court, then dismiss native starting wanderers.",
                    choices:SandboxNobleStartPolicy.RetinueModes));
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
            if(key=="FamilyScenario" ||
                key=="SpouseName" || key=="SpouseAge" ||
                key=="SpouseGender" || key=="SpouseBackground" ||
                key=="SpouseCulture")
                return profile.SpouseEnabled;
            if(key=="SpouseBirthClanId")
                return profile.SpouseEnabled &&
                    profile.SpouseBackground=="NobleChosen";
            if(key=="ChildCount") return profile.SpouseEnabled;
            if(key=="NobleBirthPosition")return profile.Role=="Lord";
            if(key=="NobleElderHeroId")
                return profile.Role=="Lord" &&
                    profile.NobleBirthPosition!="Independent";
            if(key.StartsWith("Sibling.",StringComparison.Ordinal))
            {
                string[] p=key.Split('.');
                int number;
                return p.Length==3 && Int32.TryParse(p[1],out number) &&
                    number<=profile.SiblingCount;
            }
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

        private int ActualFounderAge
        {
            get
            {
                var hero=Hero.MainHero;
                return hero==null ? (profile.PlayerAge>=18?
                    profile.PlayerAge:30) : (int)hero.Age;
            }
        }

        private int EffectiveMaximum(Field field)
        {
            if(field!=null &&
                field.Key.StartsWith("Child.",StringComparison.Ordinal) &&
                field.Key.EndsWith(".Age",StringComparison.Ordinal))
                return SandboxFamilyAgePolicy.ChildAgeMaximum(
                    ActualFounderAge,profile.SpouseAge);
            return field==null?0:field.Max;
        }

        private void ClampChildrenToParents()
        {
            // Historical creator builds silently overwrote child ages
            // after a parent-age edit. Keep every user's entered age and
            // show an explicit conflict until the user corrects the plan.
            if(preCampaign || !profile.SpouseEnabled)return;
            int max=SandboxFamilyAgePolicy.ChildAgeMaximum(
                ActualFounderAge,profile.SpouseAge);
            var conflicts=Enumerable.Range(0,
                Math.Min(profile.ChildCount,profile.Children.Count))
                .Where(i=>profile.Children[i].Age>max)
                .Select(i=>(i+1)+":"+profile.Children[i].Age)
                .ToArray();
            if(conflicts.Length>0)
                status="AGE CONFLICT: children "+
                    String.Join(", ",conflicts)+
                    " exceed "+max+
                    ". Edit their ages or increase a parent's age."+
                    " Nothing has been changed automatically.";
            else if(status!=null &&
                status.StartsWith("AGE CONFLICT:",StringComparison.Ordinal))
                status="";
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
            if(parts.Length==3 &&
                (parts[0]=="Child" || parts[0]=="Sibling") &&
                Int32.TryParse(parts[1],out index) && index>=1)
            {
                var relatives=parts[0]=="Child" ?
                    profile.Children:profile.Siblings;
                if(index>relatives.Count)return "";
                var relative=relatives[index-1];
                switch(parts[2])
                {
                    case "Name":return relative.Name;
                    case "Age":return relative.Age.ToString(
                        CultureInfo.InvariantCulture);
                    case "Gender":return relative.Gender;
                    case "Upbringing":return relative.Upbringing;
                }
            }
            return "";
        }

        private void Refresh()
        {
            ClampChildrenToParents();
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
            OnPropertyChangedWithValue(IsChildAge,"IsChildAge");
            OnPropertyChangedWithValue(AgeLimitLabel,"AgeLimitLabel");
            var field=Current;
            DiagnosticJournal.Event("cu_creator_page_checkpoint",
                "page="+(preCampaign?"World Setup":"House and Fortune")+
                "|surface=guided_creator|phase=bindings_refreshed"+
                "|chapter="+Chapter+"|step="+StepProgress+
                "|field="+(field==null?"none":field.Key));
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
            if(key.StartsWith("Child.",StringComparison.Ordinal) &&
                key.EndsWith(".Age",StringComparison.Ordinal))
            {
                int parsed;
                if(!Int32.TryParse(value,out parsed) ||
                    !SandboxFamilyAgePolicy.CanChooseChildAge(
                        parsed,ActualFounderAge,copy.SpouseAge))
                {
                    Report("Child age must be 0–"+
                        SandboxFamilyAgePolicy.ChildAgeMaximum(
                            ActualFounderAge,copy.SpouseAge)+
                        ". Your age: "+ActualFounderAge+
                        ", spouse age: "+copy.SpouseAge+".");
                    return false;
                }
            }
            // Conditional children, kingdom and fief fields can enter
            // or leave the list after this selection. Preserve this
            // field's KEY, not its old numeric cursor, so Back/Continue
            // do not land on a different question after a choice changes.
            string currentKey=Current==null ? key : Current.Key;
            profile=copy;
            var afterWrite=Visible;
            int currentAt=Array.FindIndex(afterWrite,
                f=>f.Key==currentKey);
            cursor=currentAt>=0 ? currentAt :
                Math.Max(0,Math.Min(cursor,afterWrite.Length-1));
            DiagnosticJournal.Event("cu_guided_navigation_state",
                "field="+currentKey+"|cursor="+cursor+
                "|visible_steps="+afterWrite.Length+
                "|conditional_choices_stable=true");
            // The old internal key/value string was appearing on the
            // actual household UI as "Selected: SpouseEnabled = true".
            // Feedback is conveyed by the visible selected choice.
            Report("");
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
                Math.Min((long)EffectiveMaximum(field),(long)value+
                    (long)direction*field.Step));
            TryWrite(field.Key,next.ToString(CultureInfo.InvariantCulture));
        }
        public void ExecuteExact()
        {
            Field field=Current;
            if(field==null || field.Kind!="number")return;
            string key=field.Key;
            int minimum=field.Min;
            int maximum=EffectiveMaximum(field);
            InformationManager.ShowTextInquiry(new TextInquiryData(
                field.Label,"Enter a whole number from "+minimum+
                    " to "+maximum+".",
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
            // Live images show an empty black right dock during FaceGen.
            // Keep the real native 3D camera/scene untouched by default.
            candidate.NativeFaceDockTrialEnabled=false;
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
                candidate.ChildCount>0 || candidate.SiblingCount>0;
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
                "|spouse_age="+candidate.SpouseAge+
                "|family_scenario="+candidate.FamilyScenario+
                "|children="+candidate.ChildCount+
                "|siblings="+candidate.SiblingCount+
                "|noble_birth="+candidate.NobleBirthPosition+
                "|retinue="+candidate.RetinueMode+
                "|child_upbringings="+String.Join(",",
                    candidate.Children.Take(candidate.ChildCount)
                        .Select(x=>x.Upbringing)) +
                "|child_ages="+String.Join(",",
                    candidate.Children.Take(candidate.ChildCount)
                        .Select(x=>x.Age.ToString()))+
                "|gold="+
                candidate.Gold+"|fief="+candidate.StartingFiefMode+
                "|native_review_next=true");
            if(complete!=null)complete();
        }

        // Art-only flags. World has no native 3D scene before campaign
        // construction. House retains the game-owned native review scene.
        [DataSourceProperty] public bool IsWorldSetup
            {get{return preCampaign;}}
        [DataSourceProperty] public bool IsHouseSetup
            {get{return !preCampaign;}}
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
        {
            get
            {
                if(Current==null)return "";
                if(IsChildAge)return "Allowed age: 0 to "+
                    EffectiveMaximum(Current)+" years. Your age: "+
                    ActualFounderAge+". Spouse age: "+profile.SpouseAge+
                    ". Ages beyond this are disabled.";
                return Current.Hint;
            }
        }
        private static string DisplayChoice(string key,string raw)
        {
            if(String.IsNullOrEmpty(raw))return "Native default";
            if(key=="StartingScenario" && raw=="none")
                return "Native Sandbox";
            if(key=="StartingScenario" && raw=="alternativecalradia")
                return "Alternate Calradia";
            if(key=="StartingScenario" && raw=="twofactionwar")
                return "Two-Faction War";
            if(key=="StartingScenario" && raw=="unitedempire")
                return "United Empire";
            if(key=="ScenarioVariant")
                return raw.Replace("alternativecalradia","").ToUpperInvariant();
            if(key=="StartingKingdomId" || key=="ScenarioFaction1" ||
                key=="ScenarioFaction2")
            {
                if(raw=="none")return "No kingdom";
                return Char.ToUpperInvariant(raw[0])+
                    (raw.Length>1?raw.Substring(1):"");
            }
            if(raw=="none")return "None";
            return raw;
        }

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
                    (raw=="true"?"Yes":"No"):
                    Current.Kind=="choice" ?
                        DisplayChoice(Current.Key,raw):raw;
            }
        }
        [DataSourceProperty] public bool IsChoice
            {get{return Current!=null && Current.Kind=="choice";}}
        [DataSourceProperty] public bool IsBoolean
            {get{return Current!=null && Current.Kind=="boolean";}}
        [DataSourceProperty] public bool IsNumber
            {get{return Current!=null && Current.Kind=="number";}}
        [DataSourceProperty] public bool IsChildAge
            {get{return Current!=null &&
                Current.Key.StartsWith("Child.",StringComparison.Ordinal) &&
                Current.Key.EndsWith(".Age",StringComparison.Ordinal);}}
        [DataSourceProperty] public string AgeLimitLabel
        {
            get
            {
                if(!IsChildAge)return "";
                return "PERMITTED AGE: 0–"+EffectiveMaximum(Current)+
                    " YEARS  |  YOU: "+ActualFounderAge+
                    "  |  SPOUSE: "+profile.SpouseAge;
            }
        }
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
                (preCampaign?"Create Character":
                    "Confirm & Continue") :
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
                        "World: "+
                        DisplayChoice("StartingScenario",profile.StartingScenario)+
                        "\nCulture, appearance and origins follow.";
                var hero=Hero.MainHero;
                return "Native character: "+
                    (hero==null?"pending":hero.Name.ToString())+
                    " | Age "+ActualFounderAge+"\n"+
                    "Role: "+profile.Role+"\n"+
                    "Spouse: "+(profile.SpouseEnabled?profile.SpouseName:
                        "No spouse")+"\n"+
                    "Household: "+(profile.SpouseEnabled?
                        profile.FamilyScenario:"None")+"\n"+
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
