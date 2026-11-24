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
        // House selections survive native Back without becoming grants.
        private static SandboxOriginProfile houseDraft;
        // Cache the read-only native Hero scan so Gauntlet getters do
        // not enumerate the entire campaign every UI refresh.
        private string projectedElderName;
        private readonly string projectedPatronName;
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
                houseDraft=null;
                profile.ResetForNewCampaign();
                string error;
                if(!profile.Save(SandboxOriginProfile.UserPath,out error))
                    status="Could not reset previous starting settings: "+error;
            }
            else
            {
                if(houseDraft!=null)
                    profile=SandboxOriginProfile.Parse(houseDraft.Serialize());
                // This is the new-game House editor, not a loaded save.
                profile.NormalizeGuidedCareerDraft();
                // Upgraded World drafts cannot arrive in House without
                // the new guided Lord's required marriage and first child.
                // This is editor initialization, not a live-campaign edit.
                if(profile.LordGuidedStart)
                {
                    profile.SpouseEnabled=true;
                    profile.ChildCount=Math.Max(1,profile.ChildCount);
                }
                if(profile.KingGuidedStart)
                {
                    profile.SpouseEnabled=true;
                    profile.ChildCount=Math.Max(1,profile.ChildCount);
                }
                if(!profile.AutoHouseNamesPrepared)
                {
                    var hero=Hero.MainHero;
                    SandboxAutoHousePlan.Prepare(profile,
                        hero==null || hero.Culture==null ? "" :
                            hero.Culture.StringId,
                        hero==null ? 30 : (int)hero.Age,
                        hero!=null && hero.IsFemale,
                        hero==null ? "" : hero.Name.ToString());
                    DiagnosticJournal.Event("cu_auto_house_preset",
                        "culture="+(hero==null || hero.Culture==null ?
                            "unknown" : hero.Culture.StringId)+
                        "|names=culture_specific|advanced=available");
                }
                houseDraft=SandboxOriginProfile.Parse(profile.Serialize());
                projectedElderName=SandboxDynasticStart.PreviewElderName(
                    profile,Hero.MainHero);
                projectedPatronName=SandboxServicePatron.Preview(
                    profile,Hero.MainHero);
                if(profile.NobleBirthPosition!="Independent")
                    DiagnosticJournal.Event("cu_dynastic_house_preview",
                        "birth="+profile.NobleBirthPosition+
                        "|name="+projectedElderName+
                        "|provisional=true");
            }
            fields=BuildFields();
            // A requested noble elder is not guaranteed to exist in the
            // player's actual native culture and kingdom. If the House
            // preview cannot prove one, focus the corrective birth choice
            // immediately rather than let the user complete every House
            // field and hit the same failed confirmation repeatedly.
            if(!preCampaign &&
                SandboxNobleStartPolicy.RequiresNobleElder(
                    profile.NobleBirthPosition) &&
                !HasEligibleProjectedNoble(profile))
            {
                int step=Array.FindIndex(Visible,
                    f=>f.Key=="NobleBirthPosition");
                if(step>=0)cursor=step;
                status="No eligible native noble relative for "+
                    profile.NobleBirthPosition+" in "+
                    profile.StartingKingdomId+
                    ". Return to World to recreate the guided draft. Your "+
                    "existing family choices are preserved.";
                DiagnosticJournal.Event(
                    "cu_house_unavailable_noble_birth_focused",
                    "birth="+profile.NobleBirthPosition+
                    "|kingdom="+profile.StartingKingdomId+
                    "|native_family_not_invented=true"+
                    "|house_draft_preserved=true");
            }
            Refresh();
        }

        private static bool HasEligibleProjectedNoble(
            SandboxOriginProfile candidate)
        {
            if(candidate==null)return false;
            if(!SandboxNobleStartPolicy.RequiresNobleElder(
                candidate.NobleBirthPosition))return true;
            var hero=Hero.MainHero;
            if(hero==null)return false;
            return SandboxNobleStartPolicy.IsSonOrigin(
                candidate.NobleBirthPosition) ?
                SandboxDynasticSonStart.HasProjectedNativeFather(
                    candidate,hero) :
                SandboxDynasticStart.HasProjectedNativeElder(
                    candidate,hero);
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
                    "Select the first kingdom for United Empire, Last Stand, Invasion or Two-Faction War.",
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
                    "Your starting life",
                    "Choose one of seven careers. Each has one starting path. Family, appearance and kingdom choices remain separate.",
                    choices:SandboxStartingLifePolicy.SevenPaths));
                // New Wanderer collects its sole companion here, before
                // the native player stats and appearance sequence. There
                // is deliberately NO House or Fortune page afterward.
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionEnabled",
                    "Begin with a loyal companion?",
                    "Optional. Exactly one starting companion, relation 100, "+
                    "Native wages apply; CU adds no separate reimbursement.", "boolean"));
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionName","Companion name",
                    "A unique named native Hero, not a random hired wanderer.",
                    "text"));
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionAge","Companion age",
                    "Choose an adult companion aged 18 to 70.",
                    "number",min:18,max:70));
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionGender","Companion gender",
                    "Choose the generated companion's sex.",
                    choices:new[]{"Male","Female"}));
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionSpecialty","Companion specialty",
                    "Choose Guard, Scout, Healer, Steward or Trader.",
                    choices:new[]{"Guard","Scout","Healer","Steward","Trader"}));
                list.Add(new Field("CHAPTER II | YOUR COMPANION",
                    "WandererCompanionSkill","Primary specialty skill",
                    "Exact starting skill from 0 to 220; this does not "+
                    "change the player's native stat editor.",
                    "number",min:0,max:220,step:5,advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD",
                    "EarlyHousehold","The household that raised you",
                    "Childhood shapes suggested family traditions and your biography. It does not invent ancestry.",
                    choices:SandboxStartingLifePolicy.EarlyHouseholds,advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD",
                    "PersonalAmbition","Your personal ambition",
                    "Saved personal goal for future relationships. No new quests or rewards are granted.",
                    choices:SandboxStartingLifePolicy.Ambitions,advanced:true));
                list.Add(new Field("CHAPTER I | YOUR WORLD","StartingKingdomId",
                    "Starting kingdom",
                    "Landed and mercenary roles use native allegiance. Landless roles choose a patron or birth-family kingdom without a native oath.",
                    choices:SandboxOriginProfile.KingdomIds));
                list.Add(new Field("CHAPTER I | YOUR WORLD",
                    "ServicePatronHeroId","Commanding lord Hero ID (optional)",
                    "Optional real lord contact for Soldier or Knight. Soldier remains independent until external enlistment is verified.",
                    "text",advanced:true));
            }
            else
            {
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseEnabled",
                    "Begin married?",
                    "Royal King and Lord: a real spouse is mandatory. Other paths may opt out.",
                    "boolean"));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","FamilyScenario",
                    "Household story",
                    "Natural: varied traits. Honorable: honorable spouse and kinder children. Martial, Scholarly, Mercantile, Seafaring or Strained change family bonds and upbringing.",
                    choices:SandboxFamilyDevelopmentPolicy.Scenarios,advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseName",
                    "Spouse name","Name your generated native spouse. "+
                    "A King or Lord requires a spouse.","text",
                    advanced:!(profile.LordGuidedStart ||
                        profile.KingGuidedStart)));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseAge",
                    "Spouse age","Ages must permit the selected children.",
                    "number",min:18,max:70,advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseGender",
                    "Spouse gender","Auto chooses the opposite of the player's sex. Native marriage rules apply.",
                    choices:new[]{"Auto","Female","Male"},advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseBackground",
                    "Spouse's origin","Lord: a genuine foreign noble birth clan. "+
                    "Verified married parents are used where available; unknown ancestry stays unknown. "+
                    "Commoner is not a guided Lord option.",
                    choices:profile.LordGuidedStart?
                        new[]{"NobleRandom","NobleChosen"}:
                        SandboxFamilyOriginPolicy.Backgrounds,advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseBirthClanId",
                    "Spouse's noble birth clan","Enter the exact eligible clan ID.",
                    "text",advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseCulture",
                    "Spouse's culture",
                    "Leave blank to inherit the player's actual culture.",
                    choices:new[]{"","vlandia","sturgia","battania",
                        "empire","aserai","khuzait"},advanced:true));
                list.Add(new Field("CHAPTER III | YOUR HOUSE","ChildCount",
                    "Starting children",
                    "A King or Lord requires at least one genuine "+
                    "child. Native parent links are checked.",
                    "number",min:(profile.LordGuidedStart ||
                        profile.KingGuidedStart)?1:0,max:6));
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "KingSuccessionRule","Royal household heir ranking",
                    "EldestChild ranks verified living children by birth "+
                    "order. MalePreference ranks sons before daughters. "+
                    "This never elects a native king, assigns a regent "+
                    "or transfers any settlement.",
                    choices:SandboxLordSuccessionPolicy.Rules));
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "LordSuccessionRule","Children's inheritance custom",
                    "EldestChild ranks living native children by birth "+
                    "order regardless of sex. MalePreference ranks sons "+
                    "before daughters. Neither rule changes native "+
                    "clan succession or grants a fief.",
                    choices:SandboxLordSuccessionPolicy.Rules));
                for(int i=1;i<=SandboxOriginProfile.MaxChildren;i++)
                {
                    string prefix="Child."+i+".";
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Name","Child "+i+" | Name",
                        "Give this child a real native Hero name.","text",
                        advanced:!((profile.LordGuidedStart ||
                            profile.KingGuidedStart) && i==1)));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Age","Child "+i+" | Age",
                        "Choose a permitted age. The limit adjusts to both parents.",
                        "number",min:0,max:55,advanced:true));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Gender","Child "+i+" | Gender",
                        "Set this child's native sex.",
                        choices:new[]{"Female","Male"},advanced:true));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Upbringing","Child "+i+" | Upbringing",
                        "FamilyTradition follows the household story; choose Kind, Martial, Scholarly, Mercantile or Independent for this child.",
                        choices:SandboxFamilyDevelopmentPolicy.Upbringings,advanced:true));
                }
                list.Add(new Field("CHAPTER III | YOUR HOUSE","SiblingCount",
                    "Brothers and sisters",
                    "Choose 0 to 4 family members. An older brother created here does not become an existing lord, receive a fief, or lead a separate clan. Independent Lord starts cannot include an older brother.",
                    "number",min:0,max:SandboxOriginProfile.MaxSiblings));
                for(int i=1;i<=SandboxOriginProfile.MaxSiblings;i++)
                {
                    string prefix="Sibling."+i+".";
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Name","Sibling "+i+" | Name",
                        "Give this brother or sister a name.","text",advanced:true));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Age","Sibling "+i+" | Age",
                        "An older or younger sibling. Must be an adult and fit native parental ages.",
                        "number",min:18,max:80,advanced:true));
                    list.Add(new Field("CHAPTER III | YOUR HOUSE",
                        prefix+"Gender","Sibling "+i+" | Gender",
                        "Choose brother or sister. The same parents are shared.",
                        choices:new[]{"Male","Female"},advanced:true));
                }
                list.Add(new Field("CHAPTER III | YOUR HOUSE",
                    "RetinueMode","Your starting household officers",
                    "Cultural Household creates same-culture Hero officers. Knight: one Squire. Lord: Master of Arms and Knight. King: Marshal, Minister and ONE Kingsguard Hero. Family Only creates no officers.",
                    choices:new[]{"Native","RoleBased","FamilyOnly"},
                    advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Gold",
                    "Starting gold",
                    "Use -1 to keep Bannerlord's original start.",
                    "number",min:-1,max:2000000,step:1000,advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Influence",
                    "Starting influence",
                    "Use -1 to retain your native starting influence.",
                    "number",min:-1,max:100000,step:100,advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","Troops",
                    "Additional native recruits",
                    "Up to 50 extra troops from the character's actual culture.",
                    "number",min:0,max:50,advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","StartingFiefMode",
                    "Starting holding",
                    "Native preserves Advanced Start holdings. Optional fief transfers require explicit permission.",
                    choices:SandboxFiefPolicy.Modes,advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","StartingFiefId",
                    "Exact requested fief",
                    "Use a valid native settlement ID; ownership is checked when the game begins.",
                    "text",advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","AllowStartingFiefTransfer",
                    "Authorize starting fief transfer",
                    "Required only when requesting an extra castle, town or exact fief.",
                    "boolean",advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","FamilyHomeMode",
                    "Household residence",
                    "None means no owned home is promised. A permanent residence requires a clan-owned safe town or castle.",
                    choices:(profile.LordGuidedStart || profile.KingGuidedStart)?
                        new[]{"None","OwnedFief","ClanHome"}:SandboxFamilyHomePolicy.Modes,advanced:true));
                list.Add(new Field("CHAPTER IV | YOUR FORTUNE","FamilyHomeId",
                    "Exact family residence",
                    "Specify your owned town or castle's settlement ID.",
                    "text",advanced:true));
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
            if(!preCampaign && profile.IsFixedGuidedKnightField(key))return false;
            if(!preCampaign && (profile.LordGuidedStart || profile.KingGuidedStart) &&
                key=="FamilyHomeId")return false;
            if(key=="EarlyHousehold" && preCampaign &&
                profile.Role=="Wanderer")return false;
            if(!preCampaign && profile.LordGuidedStart &&
                key=="SpouseEnabled")return false;
            if(!preCampaign && profile.KingGuidedStart &&
                key=="SpouseEnabled")return false;
            if(key=="KingSuccessionRule")
                return !preCampaign && profile.KingGuidedStart;
            // New Outlaw household keeps spouses and siblings, but never
            // displays controls for fiefs, influence, noble ancestry or
            // named estate officers it cannot legitimately own.
            if(!preCampaign && profile.OutlawGuidedStart &&
                (key=="RetinueMode" || key=="Influence" ||
                 key=="Troops" ||
                 key=="FamilyHomeMode" || key=="FamilyHomeId"))
                return false;
            // New Lord owns one genuine native vassal house. The
            // creator cannot seize a stranger's fief or add an invented
            // court stipend. The two native cultural officers are fixed.
            if((profile.KingGuidedStart ||
                (preCampaign && profile.Role=="King")) &&
               (key=="NobleBirthPosition" ||
                key=="NobleElderHeroId" ||
                (!preCampaign &&
                 (key=="StartingFiefMode" || key=="StartingFiefId" ||
                  key=="AllowStartingFiefTransfer" ||
                  key=="RetinueMode" || key=="Gold" ||
                  key=="Influence" || key=="Troops"))))
                return false;
            if((profile.LordGuidedStart ||
                (preCampaign && profile.Role=="Lord")) &&
               (key=="NobleBirthPosition" ||
                key=="NobleElderHeroId" ||
                (!preCampaign &&
                 (key=="StartingFiefMode" || key=="StartingFiefId" ||
                  key=="AllowStartingFiefTransfer" ||
                  key=="RetinueMode" || key=="Gold" ||
                  key=="Influence" || key=="Troops"))))
                return false;
            if(!preCampaign && profile.MercenaryGuidedStart &&
                (key=="RetinueMode" || key=="Influence" ||
                 key=="Gold" || key=="Troops" ||
                 key=="FamilyHomeMode" || key=="FamilyHomeId"))
                return false;
            if(!preCampaign && profile.SoldierGuidedStart &&
                (key=="RetinueMode" || key=="Influence" ||
                 key=="Gold" || key=="Troops" ||
                 key=="FamilyHomeMode" || key=="FamilyHomeId"))
                return false;
            if(key=="SiblingCount" ||
                key.StartsWith("Sibling.",StringComparison.Ordinal))
                if(profile.SoldierGuidedStart ||
                   (profile.KnightGuidedStart &&
                    !SandboxKnightStartPolicy.CanCreateSiblings(
                        profile.KnightOrigin)))return false;
            if(key=="LordSuccessionRule")
                return !preCampaign && profile.LordGuidedStart;
            if(key=="KnightOrigin" || key=="SoldierEntryRank" ||
                key=="NobleBirthPosition" || key=="NobleElderHeroId")return false;
            if(key=="WandererCompanionEnabled")
                return preCampaign && profile.Role=="Wanderer";
            if(key.StartsWith("WandererCompanion",
                StringComparison.Ordinal))
                return preCampaign && profile.Role=="Wanderer" &&
                    profile.WandererCompanionEnabled;
            if(key=="ScenarioFaction1")
                return profile.StartingScenario=="LastStand" ||
                    profile.StartingScenario=="InvasionId" ||
                    profile.StartingScenario=="unitedempire" ||
                    profile.StartingScenario=="twofactionwar";
            if(key=="ScenarioFaction2")
                return profile.StartingScenario=="twofactionwar";
            if(key=="ScenarioVariant")
                return profile.StartingScenario=="alternativecalradia";
            if(key=="StartingKingdomId")
                return profile.Role=="Soldier" ||
                    profile.Role=="Knight" ||
                    SandboxNativeStartOptions.RequiresKingdom(
                        SandboxNativeStartOptions.StartTypeForProfile(
                            profile)) ||
                    SandboxCustomLifeRolePolicy.NeedsKingdomContext(
                        profile.Role);
            if(key=="FamilyScenario" ||
                key=="SpouseName" || key=="SpouseAge" ||
                key=="SpouseGender" || key=="SpouseBackground" ||
                key=="SpouseCulture")
                return profile.SpouseEnabled;
            if(key=="SpouseBirthClanId")
                return profile.SpouseEnabled &&
                    profile.SpouseBackground=="NobleChosen";
            if(key=="ChildCount") return profile.SpouseEnabled;
            if(key=="ServicePatronHeroId")
                return preCampaign &&
                    SandboxServicePolicy.NeedsPatron(profile.Role);

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
            if(!preCampaign && profile.IsFixedGuidedKnightField(key))return false;
            if(key=="KnightOrigin" || key=="SoldierEntryRank" ||
                key=="NobleBirthPosition" || key=="NobleElderHeroId")return false;
            if(key=="Role" && Array.IndexOf(SandboxStartingLifePolicy.SevenPaths,value)<0)return false;
            if(!preCampaign && (profile.LordGuidedStart || profile.KingGuidedStart) &&
                (key=="FamilyHomeId" || (key=="FamilyHomeMode" && value=="SpecificOwned")))
            {Report("Choose an owned residence after the native estate is assigned.");return false;}
            var copy=SandboxOriginProfile.Parse(profile.Serialize());
            if(!copy.TrySet(key,value))
            {
                Report("Invalid value for "+key+".");
                return false;
            }
            // Turning off Knight marriage also clears dependent children.
            // ChildCount disappears from House when marriage is disabled.
            if(!preCampaign && copy.KnightGuidedStart &&
                key=="SpouseEnabled" && !copy.SpouseEnabled)
                copy.ChildCount=0;
            if(!preCampaign && copy.LordGuidedStart &&
                key=="SpouseBackground" &&
                copy.SpouseBackground=="Commoner")
            {
                Report("A Lord's spouse must have a native noble "+
                    "birth clan. Select a genuine noble origin.");
                return false;
            }
            if(!preCampaign && copy.KingGuidedStart &&
                (key=="SpouseEnabled" || key=="ChildCount"))
            {
                string required=SandboxKingStartPolicy
                    .ValidateRequiredRoyalFamily(
                        copy.SpouseEnabled,copy.ChildCount);
                if(required.Length>0)
                {
                    Report("Royal household requires a real spouse "+
                        "and at least one native child.");
                    return false;
                }
            }
            if(!preCampaign && copy.LordGuidedStart &&
                (key=="SpouseEnabled" || key=="ChildCount"))
            {
                string required=SandboxLordStartPolicy.ValidateRequiredFamily(
                    copy.SpouseEnabled,copy.ChildCount);
                if(required.Length>0)
                {
                    Report("Lord household requires a spouse and at "+
                        "least one child. Siblings remain optional.");
                    return false;
                }
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
            // Reject edits that directly introduce an older brother
            // in an Independent Lord house. Changing SiblingCount still
            // exposes older stored entries so the player can repair them
            // in Advanced. CommitHouse never accepts a conflicting draft.
            // This rule does not itself change native clan leadership.
            if(!preCampaign &&
                (key=="NobleBirthPosition" ||
                 key.StartsWith("Sibling.",StringComparison.Ordinal)))
            {
                int olderBrother=SandboxHouseholdLineagePolicy
                    .FirstOlderBrotherInIndependentLordHouse(
                        copy,ActualFounderAge);
                if(olderBrother==0)
                    olderBrother=SandboxHouseholdLineagePolicy
                        .FirstOlderBrotherInFoundingKingHouse(
                            copy,ActualFounderAge);
                if(olderBrother==0)
                    olderBrother=SandboxHouseholdLineagePolicy
                        .FirstOlderBrotherInSwornKnightHouse(
                            copy,ActualFounderAge);
                if(olderBrother>0)
                {
                    if(copy.KingGuidedStart)
                        Report("Founding King: sibling "+olderBrother+
                            " is an older brother and cannot enter the "+
                            "uncontested royal founding house.");
                    else if(copy.KnightGuidedStart)
                        Report("Sworn Knight: sibling "+olderBrother+
                            " cannot be an older brother. Set age to "+
                            ActualFounderAge+" or younger, or Female.");
                    else
                        Report("Independent Lord: sibling "+olderBrother+
                            " cannot be an older brother. Set his age to "+
                            ActualFounderAge+" or younger, or choose Female.");
                    return false;
                }
            }
            // Conditional children, kingdom and fief fields can enter
            // or leave the list after this selection. Preserve this
            // field's KEY, not its old numeric cursor, so Back/Continue
            // do not land on a different question after a choice changes.
            string currentKey=Current==null ? key : Current.Key;
            profile=copy;
            if(!preCampaign)
            {
                houseDraft=SandboxOriginProfile.Parse(profile.Serialize());
                if(key=="NobleBirthPosition" ||
                    key=="NobleElderHeroId")
                    projectedElderName=SandboxDynasticStart
                        .PreviewElderName(profile,Hero.MainHero);
            }
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
            Report(advanced ? "Advanced settings: names, ages, retainers, wealth and holdings.":
                "Automatic household details are selected.");
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
            // Resolve this birth choice at its own page. A pre-campaign
            // estimate is not enough to grant an actual native family.
            // Stay on the choice, not a looping final confirmation.
            if(!preCampaign && Current.Key=="NobleBirthPosition" &&
                !HasEligibleProjectedNoble(profile))
            {
                DiagnosticJournal.Event(
                    "cu_house_noble_choice_unavailable",
                    "birth="+profile.NobleBirthPosition+
                    "|kingdom="+profile.StartingKingdomId+
                    "|blocked_on_birth_field=true");
                Report("Unavailable noble birth: no eligible existing "+
                    "clan-leading relative matches your character. "+
                    "Choose Independent to proceed, or go Back to "+
                    "World and select another background or kingdom.");
                Refresh();
                return;
            }
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
            if(!candidate.NormalizeGuidedCareerDraft())
            {Report("Choose one of the seven starting careers.");return;}
            candidate.Enabled=true;
            candidate.NativeStageEnabled=false;
            candidate.NativeIntegrationReviewEnabled=false;
            candidate.NativeFaceWorkshopEnabled=false;
            candidate.NativeStateShellEnabled=true;
            // Live images show an empty black right dock during FaceGen.
            // Keep the real native 3D camera/scene untouched by default.
            candidate.NativeFaceDockTrialEnabled=false;
            candidate.RealFamilyEnabled=false;
            candidate.WandererNoHouseStart=candidate.Role=="Wanderer";
            candidate.OutlawGuidedStart=candidate.Role=="Outlaw";
            candidate.MercenaryGuidedStart=
                candidate.Role=="Mercenary";
            candidate.SoldierGuidedStart=candidate.Role=="Soldier";
            candidate.KnightGuidedStart=candidate.Role=="Knight";
            candidate.LordGuidedStart=candidate.Role=="Lord";
            candidate.KingGuidedStart=candidate.Role=="King";
            if(candidate.KingGuidedStart)
            {
                candidate.SpouseEnabled=true;
                candidate.ChildCount=Math.Max(1,candidate.ChildCount);
                candidate.SpouseBackground="NobleRandom";
                candidate.SpouseBirthClanId="";
                candidate.RoleAffiliationsEnabled=false;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.ServicePatronHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.Gold=-1;
                candidate.Influence=-1;
                candidate.Troops=0;
                candidate.RetinueMode=SandboxKingStartPolicy.Retinue;
                candidate.KingSuccessionRule=
                    SandboxLordSuccessionPolicy.EldestChild;
                candidate.FamilyHomeMode="OwnedFief";
                candidate.FamilyHomeId="";
            }
            if(candidate.LordGuidedStart)
            {
                // Set required household choices before native identity
                // and family creation. All real Heroes come later.
                candidate.SpouseEnabled=true;
                candidate.ChildCount=Math.Max(1,candidate.ChildCount);
                candidate.SpouseBackground="NobleRandom";
                candidate.SpouseBirthClanId="";
                candidate.LordSuccessionRule=
                    SandboxLordSuccessionPolicy.EldestChild;
                candidate.FamilyHomeMode="OwnedFief";
                candidate.FamilyHomeId="";
                candidate.RoleAffiliationsEnabled=false;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.ServicePatronHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.Gold=-1;
                candidate.Influence=-1;
                candidate.Troops=0;
                candidate.RetinueMode=SandboxLordStartPolicy.GuidedRetinue;
            }
            if(candidate.KnightGuidedStart)
            {
                // A visible, editable proposal, never a forced marriage:
                // one real spouse, child and culturally trained Squire.
                candidate.SpouseEnabled=true;
                candidate.ChildCount=Math.Max(1,candidate.ChildCount);
                candidate.RoleAffiliationsEnabled=false;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.FamilyHomeMode="None";
                candidate.FamilyHomeId="";
                candidate.RetinueMode="RoleBased";
                candidate.Gold=-1;
                candidate.Influence=-1;
                candidate.Troops=0;
                if(!SandboxKnightStartPolicy.CanCreateSiblings(
                    candidate.KnightOrigin))candidate.SiblingCount=0;
            }
            if(candidate.SoldierGuidedStart)
            {
                // Real SaS enlistment is controlled by its native creator
                // prompt, not a synthetic political kingdom action.
                candidate.RoleAffiliationsEnabled=false;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.FamilyHomeMode="None";
                candidate.FamilyHomeId="";
                candidate.RetinueMode="FamilyOnly";
                candidate.Gold=-1;
                candidate.Influence=-1;
                candidate.Troops=0;
                candidate.SiblingCount=0;
            }
            if(candidate.MercenaryGuidedStart)
            {
                // The actual game Advanced Start creates the kingdom's
                // paid mercenary contract. Do not invent a personal oath,
                // officer rank, fief, free roster or regular stipend.
                candidate.RoleAffiliationsEnabled=false;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.ServicePatronHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.FamilyHomeMode="None";
                candidate.FamilyHomeId="";
                candidate.RetinueMode="FamilyOnly";
                candidate.Gold=-1;
                candidate.Influence=-1;
                candidate.Troops=0;
            }
            if(candidate.OutlawGuidedStart)
            {
                candidate.StartingKingdomId="";
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.ServicePatronHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.FamilyHomeMode="None";
                candidate.FamilyHomeId="";
                candidate.Influence=-1;
                candidate.RetinueMode="RoleBased";
                // A native bandit troop grant, not extra culture recruits.
                candidate.Troops=0;
            }
            if(candidate.WandererNoHouseStart)
            {
                // A new Wanderer never inherits a hidden old House.
                candidate.EarlyHousehold="Auto";
                candidate.SpouseEnabled=false;
                candidate.ChildCount=0;
                candidate.SiblingCount=0;
                candidate.NobleBirthPosition="Independent";
                candidate.NobleElderHeroId="";
                candidate.StartingFiefMode="Native";
                candidate.StartingFiefId="";
                candidate.AllowStartingFiefTransfer=false;
                candidate.FamilyHomeMode="OwnedFief";
                candidate.FamilyHomeId="";
                if(!candidate.WandererCompanionEnabled)
                    candidate.WandererCompanionOverrides.Clear();
            }
            else
            {
                candidate.WandererCompanionEnabled=false;
                candidate.WandererCompanionOverrides.Clear();
            }
            // Soldier begins without unrelated companions. Advanced House
            // can request a named Sergeant or retain the native roster.
            candidate.RetinueMode=candidate.WandererNoHouseStart?
                "FamilyOnly":candidate.OutlawGuidedStart?
                "RoleBased":candidate.KnightGuidedStart ?
                "RoleBased":candidate.MercenaryGuidedStart ?
                "FamilyOnly":candidate.SoldierGuidedStart ?
                "FamilyOnly":SandboxServicePolicy.DefaultRetinue(
                    candidate.Role);
            if(!SandboxServicePolicy.NeedsPatron(candidate.Role))
                candidate.ServicePatronHeroId="";
            if(!SandboxNobleStartPolicy.ValidForRole(
                candidate.NobleBirthPosition,candidate.Role))
            {
                Report("Noble lineage requires Lord or NobleCadet. NobleCadet must name a valid native family.");
                return;
            }
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
                "|retinue_default="+candidate.RetinueMode+
                "|early_household="+candidate.EarlyHousehold+
                "|ambition="+candidate.PersonalAmbition+
                "|station="+SandboxStartingLifePolicy.Station(candidate)+
                "|service_patron_request="+candidate.ServicePatronHeroId+
                "|noble_birth="+candidate.NobleBirthPosition+
                "|family_requested=false|native_state_next=true"+
                "|wanderer_no_house="+candidate.WandererNoHouseStart+
                "|wanderer_companion="+candidate.WandererCompanionEnabled+
                "|outlaw_guided="+candidate.OutlawGuidedStart+
                "|outlaw_lieutenant_required="+candidate.OutlawGuidedStart+
                "|mercenary_guided="+candidate.MercenaryGuidedStart+
                "|native_contract_required="+
                    candidate.MercenaryGuidedStart+
                "|soldier_guided="+candidate.SoldierGuidedStart+
                "|guided_lord="+candidate.LordGuidedStart+
                "|guided_king="+candidate.KingGuidedStart+
                "|native_crown_and_royal_fief_required="+
                    candidate.KingGuidedStart+
                "|native_vassal_clan_and_fief_required="+
                    candidate.LordGuidedStart+
                "|soldier_requested_entry_rank="+
                    candidate.SoldierEntryRank+
                "|native_sas_enlistment_required="+
                    candidate.SoldierGuidedStart);
            if(complete!=null)complete();
        }

        private void CommitHouse()
        {
            var candidate=SandboxOriginProfile.Parse(profile.Serialize());
            if(!candidate.NormalizeGuidedCareerDraft())
            {Report("Return to World and choose one of the seven starting careers.");return;}
            if((candidate.LordGuidedStart || candidate.KingGuidedStart) && candidate.FamilyHomeMode=="SpecificOwned")
            {Report("Choose OwnedFief, ClanHome or None. Select an exact owned residence after starting.");return;}
            candidate.RealFamilyEnabled=candidate.SpouseEnabled ||
                candidate.ChildCount>0 || candidate.SiblingCount>0;
            string error;
            if(candidate.KingGuidedStart)
            {
                string required=SandboxKingStartPolicy
                    .ValidateRequiredRoyalFamily(
                        candidate.SpouseEnabled,candidate.ChildCount);
                if(required.Length>0)
                {
                    Report("Royal family requires marriage and at "+
                        "least one genuine native child.");
                    return;
                }
            }
            if(candidate.LordGuidedStart)
            {
                string required=SandboxLordStartPolicy.ValidateRequiredFamily(
                    candidate.SpouseEnabled,candidate.ChildCount);
                if(required.Length>0)
                {
                    Report("Lord household: spouse and at least one "+
                        "child are required. Siblings are optional.");
                    return;
                }
            }
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
            error=SandboxFamilyHomePolicy.ValidateRoleRequest(
                candidate.Role,candidate.FamilyHomeMode,
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
            // Old profiles can contain sibling choices that predate the
            // editor guard. Refuse them here, without losing the draft,
            // and open the exact advanced sibling-age field for repair.
            int olderBrother=SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInIndependentLordHouse(
                    candidate,actualAge);
            int knightOlderBrother=SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInSwornKnightHouse(
                    candidate,actualAge);
            int kingOlderBrother=SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInFoundingKingHouse(
                    candidate,actualAge);
            if(olderBrother==0)olderBrother=knightOlderBrother;
            if(olderBrother==0)olderBrother=kingOlderBrother;
            if(olderBrother>0)
            {
                advanced=true;
                int siblingStep=Array.FindIndex(Visible,
                    field=>field.Key=="Sibling."+olderBrother+".Age");
                if(siblingStep>=0)cursor=siblingStep;
                Report((kingOlderBrother>0?"Founding King":
                    knightOlderBrother>0?"Sworn Knight":"Independent Lord")+
                    ": sibling "+olderBrother+
                    " is an older brother. Change his age to "+
                    actualAge+" or younger, or choose Female. "+
                    "The rest of your household stays unchanged.");
                Refresh();
                return;
            }
            if(!candidate.IsValid(actualAge,out error))
            {
                Report("Family and age: "+error);
                return;
            }
            // In the reported Vlandian YoungerBrotherOfLord session,
            // House said "No eligible native elder found" but still
            // continued through irreversible native finalization.
            // Reject the impossible pedigree HERE, with the draft still
            // editable. Phase-9 strict native checks remain mandatory.
            if(SandboxNobleStartPolicy.RequiresNobleElder(
                candidate.NobleBirthPosition))
            {
                bool nativeRelative=hero!=null &&
                    HasEligibleProjectedNoble(candidate);
                if(!nativeRelative)
                {
                    DiagnosticJournal.Event(
                        "cu_house_noble_preflight_blocked",
                        "birth="+candidate.NobleBirthPosition+
                        "|kingdom="+candidate.StartingKingdomId+
                        "|elder_id="+candidate.NobleElderHeroId+
                        "|draft_preserved=true"+
                        "|native_finalization_not_invoked=true");
                    // Bring the corrective choice into view instead
                    // of leaving the player on the final sibling field.
                    // Keep every uncommitted spouse/child/sibling choice.
                    int nobleStep=Array.FindIndex(Visible,
                        field=>field.Key=="NobleBirthPosition");
                    if(nobleStep>=0)cursor=nobleStep;
                    Report("No eligible existing noble relative for "+
                        candidate.NobleBirthPosition+" in "+
                        candidate.StartingKingdomId+
                        ". Return to World and choose a supported career. "+
                        "No native family was created.");
                    Refresh();
                    return;
                }
            }
            if(!candidate.SaveForPlayer(
                SandboxOriginProfile.UserPath,actualAge,out error))
            {
                Report("Household not saved: "+error);
                return;
            }
            houseDraft=null;
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
                // Show the names that the automatic plan will create,
                // without making the player open Advanced for every child.
                if(!preCampaign && Current.Key=="NobleBirthPosition")
                    return profile.NobleBirthPosition=="Independent" ?
                        "Native lord standing stays native. You can "+
                        "keep your chosen spouse, children and siblings." :
                        "Eligible native family: "+projectedElderName+
                        ". An older sibling created in House cannot "+
                        "replace an existing native lord. If no lord "+
                        "qualifies, choose Independent here.";
                if(!preCampaign && Current.Key=="SpouseEnabled")
                    return "Proposed spouse: "+profile.SpouseName+
                        ", age "+profile.SpouseAge+
                        ". Choose Yes to create a spouse. Edit details in Advanced."+
                        (profile.NobleBirthPosition=="Independent"?"":" "+
                        projectedElderName);
                if(!preCampaign && Current.Key=="ChildCount")
                    return (profile.KingGuidedStart?
                        "King requires a spouse and at least one "+
                        "genuine child. ":
                        profile.LordGuidedStart?
                        "Lord requires one or more genuine children. ":
                        "")+"Suggested children: "+
                        (profile.ChildCount==0?"none":String.Join(", ",
                            profile.Children.Take(profile.ChildCount)
                                .Select(x=>x.Name+" ("+x.Age+")"))) +
                        ". Change names and ages in Advanced settings.";
                if(!preCampaign && Current.Key=="SiblingCount")
                    return "Suggested siblings: "+
                        (profile.SiblingCount==0?"none":String.Join(", ",
                            profile.Siblings.Take(profile.SiblingCount)
                                .Select(x=>x.Name+" ("+x.Age+")"))) +
                        ". None automatically becomes your clan leader. "+
                        (profile.Role=="Lord" &&
                         profile.NobleBirthPosition=="Independent" ?
                         "Independent Lord requires you to be the eldest "+
                         "brother. Older sisters and younger brothers "+
                         "are allowed. " :
                         (profile.KnightGuidedStart || profile.KingGuidedStart)?
                         "Older sisters and younger brothers are allowed. ":
                         "Older siblings are allowed for this career. ")+
                        "Edit names and ages in Advanced.";
                if(!preCampaign && profile.Role=="Lord" &&
                    profile.NobleBirthPosition=="Independent" &&
                    Current.Key.StartsWith("Sibling.",
                        StringComparison.Ordinal) &&
                    (Current.Key.EndsWith(".Age",
                        StringComparison.Ordinal) ||
                     Current.Key.EndsWith(".Gender",
                        StringComparison.Ordinal)))
                    return "Independent Lord: you are the eldest "+
                        "brother. An older sister is allowed, but a "+
                        "brother cannot be older than your age ("+
                        ActualFounderAge+"). This does not change "+
                        "native clan leadership.";
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
            if(key=="Role" && raw=="Outlaw")return "Outlaw / Bandit";
            if(key=="Role" && raw=="Mercenary")
                return "Mercenary | contracted company";
            if(key=="Role" && raw=="Soldier")
                return "Soldier | enlisted under a lord";
            if(key=="NobleBirthPosition")
            {
                switch(raw)
                {
                    case "Independent":return "Independent noble house";
                    case "YoungerBrotherOfLord":
                        return "Younger brother of a lord";
                    case "YoungerSisterOfLord":
                        return "Younger sister of a lord";
                    case "SecondSonOfLord":
                        return "Second son in a lord's family";
                    case "ThirdSonOfLord":
                        return "Third son in a lord's family";
                    case "YoungerBrotherOfKing":
                        return "Younger brother of a king";
                    case "YoungerSisterOfKing":
                        return "Younger sister of a king";
                    case "SecondSonOfKing":
                        return "Second son of the royal house";
                    case "ThirdSonOfKing":
                        return "Third son of the royal house";
                }
            }
            if(key=="RetinueMode")
            {
                switch(raw)
                {
                    case "Native":return "Keep native companions";
                    case "RoleBased":
                        return "Replace native companions with cultural household officers";
                    case "ReplaceNative":
                        return "Replace native companions with my court";
                    case "FamilyOnly":
                        return "No starting wanderers; keep chosen family only";
                }
            }
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
            {get{return advanced?"Close advanced settings":"Advanced settings";}}
        [DataSourceProperty] public string Status
            {get{return status??"";}}
        [DataSourceProperty] public string Summary
        {
            get
            {
                if(preCampaign && profile.Role=="King")
                    return "King | native sovereign of a real kingdom\n"+
                        "Chosen kingdom: "+
                        (profile.StartingKingdomId.Length==0?
                            "not selected":profile.StartingKingdomId)+
                        "\nPlayer must genuinely lead its ruling clan "+
                        "and main party. At least one real native fief."+
                        "\nNo invented crown, fief grant, treasury, "+
                        "royal stipend or free army."+
                        "\nThree same-culture Heroes: Marshal and "+
                        "Kingsguard travel with the King. Royal Minister "+
                        "governs a genuine vacant native royal fief."+
                        "\nSpouse and at least one child required. "+
                        "Siblings remain optional."+
                        "\nNo older brother in an uncontested founding house."+
                        "\nRoyal heir ranking is a saved family custom, "+
                        "not a native kingship transfer or regency.";
                if(preCampaign && profile.Role=="Lord")
                    return "Lord | founder of your own native noble house\n"+
                        "True vassal in "+
                        (profile.StartingKingdomId.Length==0?
                            "an unselected kingdom":profile.StartingKingdomId)+
                        "\nNative clan leadership and party command required."+
                        "\nAt least one real native Advanced Start holding."+
                        "\nNo additional fief gift, bonus funds or troops."+
                        "\nTwo cultured native Heroes: Master of Arms "+
                        "and sworn Knight. Both must join the clan party."+
                        "\nSpouse from verified native noble parents "+
                        "and at least one child required."+
                        "\nChild inheritance: eldest living child "+
                        "by default, or chosen male-preference rule."+
                        "\nNative leadership and land stay game-owned."+
                        "\nSiblings optional. No older founding brother."+
                        "\nOlder sisters are allowed."+
                        "\nNo royal title or invented court authority.";
                if(preCampaign && profile.Role=="Knight")
                    return "Knight | independent landless house\n"+
                        "Sworn service: independent landless house, "+
                        "real personal lord and 30-day muster in person.\n"+
                        "Missed muster: native relation -3, oath break: -20.\n"+
                        "One culturally trained Squire. No automatic castle, wage or vassalage.\n"+
                        "Spouse and children remain optional.";
                if(preCampaign && profile.Role=="Soldier")
                    return "Soldier | sworn service under a real lord\n"+
                        "Begin as a recruit (native private).\n"+
                        "Kingdom to join: "+
                        (profile.StartingKingdomId.Length==0?
                            "not selected":profile.StartingKingdomId)+
                        "\nThe SaS start-enlisted menu selects the actual "+
                        "troop and commander. Rank and advancement remain native.\n"+
                        "No independent mercenary company, fief or court.\n"+
                        "Native appearance and full stats remain editable.";
                if(preCampaign && profile.Role=="Mercenary")
                    return "Mercenary | paid native company\n"+
                        "Contract kingdom: "+
                        (profile.StartingKingdomId.Length==0?
                            "not selected":profile.StartingKingdomId)+
                        "\nNo noble rank, fief or NPC-party enlistment"+
                        "\nExisting native troop payroll and wages apply"+
                        "\nFamily: optional spouse, children and siblings; "+
                        "no invented owned residence"+
                        "\nNative player appearance and stats remain editable.";
                if(preCampaign && profile.Role=="Outlaw")
                    return "Outlaw / Bandit | native outlaw start\n"+
                        "No kingdom, estate or noble title\n"+
                        "Band: six low-tier native bandits and "+
                        "one culturally trained lieutenant\n"+
                        "Wages: fourteen-day native payroll reserve, "+
                        "plus starting food\n"+
                        "Family: optional spouse, children and siblings; "+
                        "no owned residence\n"+
                        "Player appearance and stats remain editable.";
                if(preCampaign && profile.Role=="Wanderer")
                    return "Wanderer | independent native start\n"+
                        "House screen: removed\n"+
                        "Companion: "+(profile.WandererCompanionEnabled?
                            profile.WandererCompanionName+" | "+
                            profile.WandererCompanionSpecialty+
                            " | 100 relation | native wages":"none")+
                        "\nNative appearance, attributes, skills and "+
                        "traits remain editable.";
                if(preCampaign)
                    return SandboxStartingLifePolicy.Dossier(
                            profile.Role,profile.NobleBirthPosition,
                            profile.EarlyHousehold,profile.PersonalAmbition)+"\n"+
                        "Starting kingdom: "+
                        (profile.StartingKingdomId.Length==0?"None":
                            profile.StartingKingdomId)+"\n"+
                        
                        (SandboxServicePolicy.NeedsPatron(profile.Role)?
                            "Service: named native lord appointed at start\n":"")+
                        "World: "+
                        DisplayChoice("StartingScenario",profile.StartingScenario)+
                        "\nCulture, appearance and origins follow.";
                var hero=Hero.MainHero;
                return "Native character: "+
                    (hero==null?"pending":hero.Name.ToString())+
                    " | Age "+ActualFounderAge+"\n"+
                    "Role: "+profile.Role+"\n"+
                    (profile.KingGuidedStart?
                        "Royal household heir custom: "+
                        profile.KingSuccessionRule+
                        " (ranked claim, native monarchy unchanged)\n":"")+
                    (profile.LordGuidedStart?
                        "Noble spouse: "+profile.SpouseBackground+
                        "\nHousehold inheritance custom: "+
                        profile.LordSuccessionRule+
                        " (claims only, native succession unchanged)\n":"")+
                    "Social station: "+SandboxStartingLifePolicy.StationLabel(
                        SandboxStartingLifePolicy.Station(profile))+"\n"+
                    "Obligations: "+SandboxStartingLifePolicy.ObligationLabel(
                        SandboxStartingLifePolicy.Obligation(profile))+"\n"+
                    "Early household: "+SandboxStartingLifePolicy.Household(
                        profile.Role,profile.EarlyHousehold)+"\n"+
                    "Personal ambition: "+SandboxStartingLifePolicy.Ambition(
                        profile.Role,profile.NobleBirthPosition,
                        profile.PersonalAmbition)+"\n"+
                    "Spouse: "+(profile.SpouseEnabled?profile.SpouseName:
                        "No spouse")+"\n"+
                    "Household: "+(profile.SpouseEnabled?
                        profile.FamilyScenario:"None")+"\n"+
                    "Children: "+profile.ChildCount+"\n"+
                    "Siblings: "+profile.SiblingCount+"\n"+

                    "Household officers: "+DisplayChoice("RetinueMode",
                        profile.RetinueMode)+"\n"+
                    (SandboxServicePolicy.NeedsPatron(profile.Role)?
                        "Commanding lord: "+projectedPatronName+"\n":"")+
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
