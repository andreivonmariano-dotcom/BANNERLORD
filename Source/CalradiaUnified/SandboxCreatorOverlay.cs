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
                var profile = SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
                SandBox.AdvancedStartOptions.AdvancedStartOptions options;
                string reason = "Origin profile is disabled.";
                if (!profile.Enabled ||
                    !SandboxNativeStartOptions.TryPrepare(profile,out options,out reason))
                {
                    DiagnosticJournal.Event("sandbox_direct_game_start_refused",
                        "profile_enabled=" + profile.Enabled + "|reason=" + reason);
                    return;
                }
                // This creates the actual native King/Vassal/Mercenary/etc.
                // starting data, rather than the former empty default start.
                var gameOptions = options.GetChangedOptions();
                MBGameManager.StartNewGame(new SandBoxGameManager(
                    () => new Campaign(CampaignGameMode.Campaign,gameOptions)));
                DiagnosticJournal.Event("sandbox_direct_game_start_requested",
                    "role=" + profile.Role + "|type=" +
                    SandboxNativeStartOptions.StartTypeForRole(profile.Role) +
                    "|kingdom=" + profile.StartingKingdomId +
                    "|scenario=" + profile.StartingScenario);
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
            "Identity", "Household", "Resources", "Location", "Review"
        };

        private readonly Action close;
        private readonly List<Field> fields;
        private SandboxOriginProfile profile;
        private string page="Identity";
        private int selectedIndex;
        private int pendingNumber;
        private string status="";
        private bool advancedFieldsVisible;

        public SandboxCreatorViewModel(Action close)
        {
            this.close=close;
            profile=SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            // A stale prior profile must not become active simply from
            // visiting the new creator, including a cancelled new Sandbox.
            profile.ResetForNewCampaign();
            DiagnosticJournal.Event("sandbox_creator_fresh_start",
                "spouse=false|children=0|fief=Native" +
                "|next=native_culture_appearance_origins_age_difficulty");
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
                new Field("Identity","StartingKingdomId",
                    "Starting kingdom (required for Lord, King, Soldier and Knight)",
                    "choice",choices:SandboxOriginProfile.KingdomIds),
                new Field("Identity","NativeIntegrationReviewEnabled",
                    "Preview CU choices inside Bannerlord's 3D review",
                    "boolean"),
                new Field("Identity","NativeFaceWorkshopEnabled",
                    "Experimental: edit live 3D face in CU workshop",
                    "boolean"),
                new Field("Identity","NativeStateShellEnabled",
                    "Experimental: CU culture chooser and real 3D face audit",
                    "boolean"),
                new Field("Identity","NativeFaceDockTrialEnabled",
                    "Experimental: move real 3D face to RIGHT (F9 restores)",
                    "boolean"),
                new Field("Identity","PlayerAge",
                    "Starting age (use native creator)","unavailable"),
                new Field("Household","SpouseEnabled",
                    "Begin married","boolean"),
                new Field("Household","SpouseName",
                    "Spouse name","text"),
                new Field("Household","SpouseAge",
                    "Spouse age","number",18,70),
                new Field("Household","SpouseGender",
                    "Spouse gender (Auto selects the opposite gender)",
                    "choice",choices:new[]{"Auto","Female","Male"}),
                new Field("Household","SpouseBackground",
                    "Generated spouse's family background",
                    "choice", choices:SandboxFamilyOriginPolicy.Backgrounds),
                new Field("Household","SpouseBirthClanId",
                    "Exact chosen noble birth-clan ID",
                    "text"),
                new Field("Household","SpouseCulture",
                    "Spouse culture (blank uses your culture)",
                    "choice",choices:new[]{"","vlandia","sturgia",
                        "battania","empire","aserai","khuzait"}),
                new Field("Household","ChildCount",
                    "Number of children (0-6)","number",0,6),
                new Field("Household","SiblingCount",
                    "Starting siblings (requires parent Heroes)","unavailable"),
                new Field("Resources","EquipmentKit",
                    "Equipment theme (not applied yet)","unavailable"),
                new Field("Resources","Gold",
                    "Starting gold (-1 means native)","number",0,2000000,1000),
                new Field("Resources","Influence",
                    "Starting influence (-1 means native)","number",0,100000,100),
                new Field("Resources","Troops",
                    "Additional native-culture recruits","number",0,50),
                new Field("Location","FamilyHomeMode",
                    "Where should your family live?",
                    "choice",choices:SandboxFamilyHomePolicy.Modes),
                new Field("Location","FamilyHomeId",
                    "Exact owned castle or town ID for your family","text"),
                new Field("Location","StartingTown",
                    "Preferred starting town (relocation unavailable)","unavailable"),
                new Field("Location","StartingFiefMode",
                    "Founding fief: native, same-kingdom castle, town, or exact ID",
                    "choice",choices:SandboxFiefPolicy.Modes),
                new Field("Location","StartingFiefId",
                    "Specific fief's exact settlement ID (world-validated)","text"),
                new Field("Location","AllowStartingFiefTransfer",
                    "Authorize transfer from another clan in your kingdom",
                    "boolean"),
                new Field("Location","StartingScenario",
                    "Native world scenario","choice",
                    choices:SandboxOriginProfile.Scenarios),
                new Field("Location","ScenarioFaction1",
                    "World scenario faction 1 / unifier","choice",
                    choices:SandboxOriginProfile.KingdomIds),
                new Field("Location","ScenarioFaction2",
                    "Two-Faction War second faction","choice",
                    choices:SandboxOriginProfile.KingdomIds),
                new Field("Location","ScenarioVariant",
                    "Alternate Calradia variation","choice",
                    choices:new[]{"alternativecalradiadefault",
                        "alternativecalradiafractured",
                        "alternativecalradiashattered"}),
                new Field("Location","WorldSeed",
                    "Optional world seed (-1 keeps random)","number",0,2000000000),
                new Field("Location","StartFastMode","Advanced: fast mode","boolean"),
                new Field("Location","StartBanditSurge","World: bandit surge","boolean"),
                new Field("Location","StartCivilUnrest","World: civil unrest","boolean"),
                new Field("Location","StartRecruitShortage","World: recruit shortage","boolean"),
                new Field("Location","StartSwiftTravel","World: swift travel","boolean"),
                new Field("Review","Role",
                    "Confirmed native starting role","choice",
                    choices:SandboxOriginProfile.Roles)
            };
            foreach (string attribute in SandboxOriginProfile.Attributes)
                f.Add(new Field("Resources","Attribute."+attribute,
                    "Attribute: "+attribute,"number",1,10));
            foreach (string skill in SandboxOriginProfile.Skills)
            {
                f.Add(new Field("Resources","Skill."+skill,
                    "Skill: "+skill,"number",0,330,5));
                f.Add(new Field("Resources","Focus."+skill,
                    "Focus: "+skill,"number",0,5));
            }
            foreach (string trait in SandboxOriginProfile.Traits)
                f.Add(new Field("Resources","Trait."+trait,
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
                f.Add(new Field("Resources","Item."+slot,
                    "Exact game item ID "+slot,"text"));
            return f;
        }

        private bool IsAvailable(Field field)
        {
            if (field.Page != page) return false;
            string key=field.Key;
            if (page=="Review") return false;
            if (key == "StartingKingdomId")
                return SandboxNativeStartOptions.RequiresKingdom(
                    SandboxNativeStartOptions.StartTypeForRole(profile.Role));
            // Native compositing is now selected automatically for every
            // accepted CU Sandbox start. No experimental toggle is exposed
            // in the player-facing five-chapter form. The native fallback
            // still protects all original creation callbacks.
            if (key == "NativeIntegrationReviewEnabled" ||
                key == "NativeFaceWorkshopEnabled" ||
                key == "NativeStateShellEnabled" ||
                key == "NativeFaceDockTrialEnabled")
                return false;
            if (key == "SpouseBirthClanId")
                return profile.SpouseEnabled &&
                    profile.SpouseBackground == "NobleChosen";
            if (key == "SpouseName" || key == "SpouseAge" ||
                key == "SpouseGender" || key == "SpouseBackground")
                return profile.SpouseEnabled;
            if (key == "SpouseCulture")
                return profile.SpouseEnabled && advancedFieldsVisible;
            if (key == "ChildCount") return profile.SpouseEnabled;
            if (key.StartsWith("Child.",StringComparison.Ordinal))
            {
                var parts=key.Split('.');
                int index;
                if (!Int32.TryParse(parts[1],out index) ||
                    index>profile.ChildCount) return false;
                return advancedFieldsVisible ||
                    parts[2]=="Name" || parts[2]=="Age" ||
                    parts[2]=="Gender";
            }
            if (key=="FamilyHomeMode")
                return profile.SpouseEnabled;
            if (key=="FamilyHomeId")
                return profile.SpouseEnabled &&
                    profile.FamilyHomeMode=="SpecificOwned";
            if (key=="StartingFiefId")
                return profile.StartingFiefMode=="Specific";
            if (key=="AllowStartingFiefTransfer")
                return profile.StartingFiefMode!="Native";
            if (key=="ScenarioFaction1")
                return profile.StartingScenario=="LastStand" ||
                    profile.StartingScenario=="InvasionId" ||
                    profile.StartingScenario=="unitedempire";
            if (key=="ScenarioFaction2")
                return profile.StartingScenario=="twofactionwar";
            if (key=="ScenarioVariant")
                return profile.StartingScenario=="alternativecalradia";
            // All starting attributes, skills, focus and traits live in
            // the new post-narrative native editor. Never ask twice in the
            // pre-campaign Resources pages, not even under More options.
            if (key.StartsWith("Skill.",StringComparison.Ordinal) ||
                key.StartsWith("Focus.",StringComparison.Ordinal) ||
                key.StartsWith("Attribute.",StringComparison.Ordinal) ||
                key.StartsWith("Trait.",StringComparison.Ordinal))
                return false;
            if (key=="WorldSeed" || key=="StartingTown" ||
                key=="PlayerAge" || key=="SiblingCount" ||
                key=="EquipmentKit" ||
                key.StartsWith("Skill.",StringComparison.Ordinal) ||
                key.StartsWith("Focus.",StringComparison.Ordinal) ||
                key.StartsWith("Attribute.",StringComparison.Ordinal) ||
                key.StartsWith("Trait.",StringComparison.Ordinal) ||
                key.StartsWith("Item.",StringComparison.Ordinal) ||
                key=="StartFastMode" || key=="StartBanditSurge" ||
                key=="StartCivilUnrest" || key=="StartRecruitShortage" ||
                key=="StartSwiftTravel")
                return advancedFieldsVisible;
            // Culture, face and age are selected once in the native wizard.
            if (key.StartsWith("Sibling.",StringComparison.Ordinal))
                return false;
            if (key=="StartingCulture") return false;
            return true;
        }

        private Field[] VisibleFields
        {
            get { return fields.Where(IsAvailable).ToArray(); }
        }

        private Field Current
        {
            get
            {
                var choices=VisibleFields;
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
            // Changing role, spouse count or fief mode can hide fields.
            // Keep the cursor within the currently visible guided choices.
            if (selectedIndex >= VisibleFields.Length)
                selectedIndex = Math.Max(0, VisibleFields.Length-1);
            Field field=Current;
            int n;
            string value=field==null ? "" : RawValue(field.Key);
            pendingNumber=Int32.TryParse(value,NumberStyles.Integer,
                CultureInfo.InvariantCulture,out n) ? n :
                field==null ? 0 : field.Min;
            if(field!=null && field.Kind=="number")
                pendingNumber=Math.Max(field.Min,Math.Min(field.Max,pendingNumber));
            OnPropertyChangedWithValue(PageTitle,"PageTitle");
            OnPropertyChangedWithValue(AdvancedToggleLabel,"AdvancedToggleLabel");
            OnPropertyChangedWithValue(FieldHelp,"FieldHelp");
            OnPropertyChangedWithValue(PageProgress,"PageProgress");
            OnPropertyChangedWithValue(IsIdentitySelected,"IsIdentitySelected");
            OnPropertyChangedWithValue(IsHouseholdSelected,"IsHouseholdSelected");
            OnPropertyChangedWithValue(IsProgressionSelected,"IsProgressionSelected");
            OnPropertyChangedWithValue(IsEquipmentSelected,"IsEquipmentSelected");
            OnPropertyChangedWithValue(IsResourcesSelected,"IsResourcesSelected");
            OnPropertyChangedWithValue(IsLocationSelected,"IsLocationSelected");
            OnPropertyChangedWithValue(IsReviewSelected,"IsReviewSelected");
            OnPropertyChangedWithValue(PageDescription,"PageDescription");
            OnPropertyChangedWithValue(SelectedLabel,"SelectedLabel");
            OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            OnPropertyChangedWithValue(SelectedPosition,"SelectedPosition");
            OnPropertyChangedWithValue(IsNumber,"IsNumber");
            OnPropertyChangedWithValue(IsBoolean,"IsBoolean");
            OnPropertyChangedWithValue(IsChoice,"IsChoice");
            OnPropertyChangedWithValue(IsText,"IsText");
            OnPropertyChangedWithValue(IsUnavailable,"IsUnavailable");
            OnPropertyChangedWithValue(IsReviewPage,"IsReviewPage");
            OnPropertyChangedWithValue(IsNotReviewPage,"IsNotReviewPage");
            OnPropertyChangedWithValue(NumberMinimum,"NumberMinimum");
            OnPropertyChangedWithValue(NumberMaximum,"NumberMaximum");
            OnPropertyChangedWithValue(pendingNumber,"PendingNumber");
            OnPropertyChangedWithValue(ProfileSummary,"ProfileSummary");
            OnPropertyChangedWithValue(RoleSummary,"RoleSummary");
            OnPropertyChangedWithValue(FamilySummary,"FamilySummary");
            OnPropertyChangedWithValue(ChildPreview,"ChildPreview");
            OnPropertyChangedWithValue(LandSummary,"LandSummary");
            OnPropertyChangedWithValue(WorldSummary,"WorldSummary");
            OnPropertyChangedWithValue(FamilyHomeSummary,"FamilyHomeSummary");
            OnPropertyChangedWithValue(NativeNextSummary,"NativeNextSummary");
            OnPropertyChangedWithValue(SpousePreviewSummary,"SpousePreviewSummary");
            OnPropertyChangedWithValue(StartNotice,"StartNotice");
            OnPropertyChangedWithValue(Status,"Status");
        }

        [DataSourceProperty] public string PageTitle
        {
            get
            {
                if(page=="Identity") return "Choose your starting life";
                if(page=="Household") return "A family in Calradia";
                if(page=="Resources") return "Starting resources and skills";
                if(page=="Location") return "Kingdom, fief and world";
                return "Review your new campaign";
            }
        }
        [DataSourceProperty] public string AdvancedToggleLabel
        {
            get { return advancedFieldsVisible ?
                "Fewer options" : "More options"; }
        }
        [DataSourceProperty] public string FieldHelp
        {
            get
            {
                if (Current==null)
                    return "Part 2: Native culture, 3D appearance, origins, " +
                        "age and difficulty follow. Your selected family " +
                        "will be created after native finalization.";
                string key=Current.Key;
                if (key=="Role") return "Lord starts as a vassal. King starts as a ruler. This changes your real native starting conditions.";
                if (key=="StartingKingdomId") return "Required for Lord, Knight, King and Soldier. Select your actual political allegiance.";
                if (key=="NativeIntegrationReviewEnabled") return "Experimental and off by default: append one CU summary AFTER native difficulty. It reuses the actual native 3D review. No face or culture view is replaced. Disable if the new stage fails.";
                if (key=="NativeFaceWorkshopEnabled") return "Experimental 3D prototype, OFF by default. Adds a CU-branded face editor after the native pages. Edits real player body properties, but this does NOT yet remove the native wizard. Do not enable together with CU 3D Review.";
                if (key=="NativeStateShellEnabled") return "Experimental and OFF by default. Uses the real native culture controller, game-owned stages and CU chapter rail. No native callbacks are skipped.";
                if (key=="NativeFaceDockTrialEnabled") return "REQUIRES CU native-state shell = Yes. Experimental and OFF by default: moves the ORIGINAL native 3D FaceGen render to the right and its stock sliders to the left. Press F9 during the face stage to restore or retry stock layout. This is not a persistent scene across all five chapters.";
                if (key=="StartingCulture") return "Culture will still be confirmed in Bannerlord's safe native character editor.";
                if (key=="PlayerAge") return "Unavailable here: choose your actual player age using Bannerlord\'s native character creation until the CU native-state bridge is finished.";
                if (key=="SiblingCount") return "Unavailable: sibling creation requires real native parent Heroes. This build never adds placeholder relatives.";
                if (key=="EquipmentKit") return "Unavailable: equipment themes are not applied. Use the native starting equipment or advanced exact item IDs.";
                if (key=="StartingTown") return "Unavailable: starting-town relocation is not active. Use the native starting location and supported fief options.";
                if (key=="SpouseEnabled") return "Choose Yes to request a real spouse. No spouse means no starting children.";
                if (key=="SpouseGender") return "Auto uses the opposite gender after your native character is finalized.";
                if (key=="SpouseBackground") return "Commoner: no noble ancestors. NobleRandom: a generated child of a real noble couple in your kingdom. NobleChosen: type a clan ID.";
                if (key=="SpouseBirthClanId") return "Use a real clan ID from your starting kingdom. A living married couple must be old enough to be your spouse's parents.";
                if (key=="SpouseCulture") return "Leave blank to match your player culture, or select an official game culture ID. Vlandian is stored as vlandia.";
                if (key=="ChildCount") return "Each campaign begins with zero children. Plus, minus and the slider save immediately. Set a number, then enter each child's name, age and sex.";
                if (key=="FamilyHomeMode") return "OwnedFief: any safe castle or town you own. ClanHome: prefer your clan home. SpecificOwned: exact owned settlement. A baby is housed as a child, never recruited to a party.";
                if (key=="FamilyHomeId") return "Enter the exact internal ID of a castle or town already owned by your clan. Unsafe, besieged or foreign holdings are refused.";
                if (key=="StartingFiefMode") return "Native preserves Bannerlord's grants. Castle/Town/Specific can transfer land from a same-kingdom clan.";
                if (key=="StartingFiefId") return "Enter an exact settlement ID from this game version. Unknown and foreign holdings are refused.";
                if (key=="AllowStartingFiefTransfer") return "Required for a non-native starting fief. This can remove a holding from another clan.";
                if (key=="StartingScenario") return "Choose the world's starting conditions. Default is closest to normal Sandbox.";
                if (key=="Gold" || key=="Influence") return "Use default to keep the resources granted by the native starting role.";
                return "Adjust this value, then save. Advanced settings are optional and may need separate gameplay verification.";
            }
        }
        [DataSourceProperty] public string PageProgress
        {
            get
            {
                int i=Array.IndexOf(Pages,page);
                return "PAGE "+(i+1)+" / "+Pages.Length;
            }
        }
        [DataSourceProperty] public bool IsIdentitySelected { get { return page=="Identity"; } }
        [DataSourceProperty] public bool IsHouseholdSelected { get { return page=="Household"; } }
        [DataSourceProperty] public bool IsProgressionSelected { get { return page=="Progression"; } }
        [DataSourceProperty] public bool IsEquipmentSelected { get { return page=="Equipment"; } }
        [DataSourceProperty] public bool IsResourcesSelected { get { return page=="Resources"; } }
        [DataSourceProperty] public bool IsLocationSelected { get { return page=="Location"; } }
        [DataSourceProperty] public bool IsReviewSelected { get { return page=="Review"; } }
        [DataSourceProperty] public string PageDescription
        {
            get
            {
                if (page=="Identity") return "Your place in Calradia. Choose a career first, then a kingdom if required.";
                if (page=="Household") return "Design a real starting family: spouse, noble ancestry, children and ages. Only registered native Heroes receive portraits after creation.";
                if (page=="Resources") return "Choose starting gold, influence and recruits. Starting stats and personality traits follow your native youth and adulthood choices.";
                if (page=="Location") return "Set your family's home in a town or castle, then choose your fief policy. No party recall is needed for infants.";
                return "Your starting life is ready. Continue to Bannerlord's true culture, live 3D body editor and native origins. Opt-in CU 3D review runs after difficulty.";
            }
        }
        [DataSourceProperty] public string SelectedLabel
        {
            get
            {
                if (Current==null && page=="Review")
                    return "Confirm your starting plan on the right";
                return Current==null ? "" : Current.Label;
            }
        }
        [DataSourceProperty] public string SelectedValue
        {
            get
            {
                if(Current==null) return "";
                if(Current.Kind=="unavailable") return "NOT AVAILABLE IN THIS BUILD";
                string value=RawValue(Current.Key);
                if(value=="-1" || value=="") return "Keep the native default";
                if(Current.Kind=="number") return pendingNumber.ToString();
                if(Current.Kind=="boolean")
                    return value=="true" ? "YES" : "NO";
                if(Current.Key=="StartingKingdomId")
                    return value.Length>0 ?
                        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value):
                        "Choose a kingdom";
                return value;
            }
        }
        [DataSourceProperty] public string SelectedPosition
        {
            get
            {
                int count=VisibleFields.Length;
                return page=="Review" ? "READY TO BEGIN" :
                    count==0 ? "NO EXTRA SETTINGS" :
                    "SETTING "+(selectedIndex+1)+" / "+count;
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
        [DataSourceProperty] public bool IsUnavailable
        {
            get { return Current!=null && Current.Kind=="unavailable"; }
        }
        [DataSourceProperty] public bool IsReviewPage { get {return page=="Review";} }
        [DataSourceProperty] public bool IsNotReviewPage { get {return page!="Review";} }
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
                if (!IsNumber || Current==null) return;
                int next=Math.Max(NumberMinimum,Math.Min(NumberMaximum,value));
                if (next==pendingNumber) return;
                pendingNumber=next;
                // Never display an unsaved child count as a chosen value.
                if (!Write(Current.Key,next.ToString(
                    CultureInfo.InvariantCulture)))
                    Refresh();
            }
        }
        [DataSourceProperty] public string Status {get {return status;} }
        [DataSourceProperty] public string RoleSummary
        {
            get
            {
                var kingdom = String.IsNullOrEmpty(profile.StartingKingdomId) ?
                    "" : "  /  " + CultureInfo.InvariantCulture.TextInfo
                        .ToTitleCase(profile.StartingKingdomId);
                return profile.Role + kingdom;
            }
        }
        [DataSourceProperty] public string FamilySummary
        {
            get
            {
                if (!profile.SpouseEnabled)
                    return "Unmarried, no children";
                return profile.SpouseBackground + " spouse / " +
                    profile.ChildCount +
                    (profile.ChildCount==1 ? " child" : " children");
            }
        }
        [DataSourceProperty] public string SpousePreviewSummary
        {
            get
            {
                if (!profile.SpouseEnabled)
                    return "Marriage not selected";
                return profile.SpouseName + ", " + profile.SpouseAge +
                    ", " + profile.SpouseGender;
            }
        }
        [DataSourceProperty] public string NativeNextSummary
        {
            get
            {
                return profile.NativeFaceWorkshopEnabled ?
                    "CU FACE WORKSHOP: EXPERIMENTAL" :
                    (profile.NativeIntegrationReviewEnabled ?
                        "NATIVE 3D REVIEW: OPT-IN ON" :
                        "LIVE 3D FACE: NATIVE NEXT");
            }
        }
        [DataSourceProperty] public string ChildPreview
        {
            get
            {
                if (!profile.SpouseEnabled || profile.ChildCount==0)
                    return "No child requested";
                if (profile.ChildCount==1)
                {
                    var child=profile.Children[0];
                    return child.Name + "  |  " + child.Gender +
                        "  |  age " + child.Age;
                }
                return String.Join(", ",
                    profile.Children.Take(Math.Min(2,profile.ChildCount))
                        .Select(c=>c.Name)) +
                    (profile.ChildCount>2 ? " and others" : "");
            }
        }
        [DataSourceProperty] public string LandSummary
        {
            get
            {
                if (profile.StartingFiefMode=="Native")
                    return "Native starting holdings";
                return profile.StartingFiefMode=="Specific" ?
                    "Requested: " + profile.StartingFiefId :
                    "Requested: " + profile.StartingFiefMode;
            }
        }
        [DataSourceProperty] public string WorldSummary
        {
            get
            {
                return profile.StartingScenario=="none" ?
                    "World: Normal Sandbox" :
                    "World: " + profile.StartingScenario;
            }
        }
        [DataSourceProperty] public string FamilyHomeSummary
        {
            get
            {
                if (!profile.SpouseEnabled)
                    return "Home: no household";
                if (profile.FamilyHomeMode=="SpecificOwned")
                    return "Family home: " + profile.FamilyHomeId;
                return profile.FamilyHomeMode=="ClanHome" ?
                    "Family home: clan's owned seat" :
                    "Family home: owned town or castle";
            }
        }
        [DataSourceProperty] public string StartNotice
        {
            get
            {
                return "NEXT: native culture, face, origins, age, starting stats and difficulty. A minor stays a child, never a party troop.";
            }
        }
        [DataSourceProperty] public string ProfileSummary
        {
            get
            {
                return "Starting plan: " + RoleSummary +
                    " | " + FamilySummary +
                    " | " + LandSummary;
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

        public void ExecuteResources() {Switch("Resources");}
        public void ExecuteLocation() {Switch("Location");}
        public void ExecuteReview() {Switch("Review");}
        public void ExecuteToggleAdvanced()
        {
            advancedFieldsVisible=!advancedFieldsVisible;
            selectedIndex=0;
            SetStatus(advancedFieldsVisible ?
                "Advanced options shown. Starting stats are edited after your life story." :
                "Quick setup active. Only relevant starting choices are shown.");
            Refresh();
        }

        public void ExecuteNextField()
        {
            int count=VisibleFields.Length;
            if(selectedIndex+1<count)
            {
                selectedIndex++;
                Refresh();
                return;
            }
            int pageIndex=Array.IndexOf(Pages,page);
            if(pageIndex>=0 && pageIndex+1<Pages.Length)
                Switch(Pages[pageIndex+1]);
        }
        public void ExecutePreviousField()
        {
            if(selectedIndex>0)
            {
                selectedIndex--;
                Refresh();
                return;
            }
            int pageIndex=Array.IndexOf(Pages,page);
            if(pageIndex>0)
            {
                Switch(Pages[pageIndex-1]);
                selectedIndex=Math.Max(0,VisibleFields.Length-1);
                Refresh();
            }
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
                "key="+key+"|value="+value+"|role="+profile.Role+
                "|child_count="+profile.ChildCount);
            SetStatus(key=="ChildCount" ?
                "Saved child count: " + profile.ChildCount +
                ". This is the number the game will create." :
                "Saved "+key+" for the new Sandbox.");
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
            if (!IsNumber || Current==null) return;
            string key=Current.Key;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                Current.Label,
                "Enter an exact whole number. Valid values save immediately.",
                true,true,"Save","Cancel",
                value=>Write(key,value),()=>{},false,null,"",
                RawValue(key)));
        }
        public void ExecuteUseNative()
        {
            if(Current!=null && (Current.Key=="Gold"||
                Current.Key=="Influence" ||
                Current.Key=="WorldSeed"))
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
            // Crash recovery: never request competing CharacterCreationStageView
            // handlers. The original native FaceGen owns appearance in v0.23.
            candidate.NativeStageEnabled=false;
            // v0.39 automatic native bridge. No user-facing experimental
            // switch is required. The optional extra stages remain disabled,
            // and the existing native face and finalization remain game-owned.
            // Face dock has an F9 restore and reverts on failed inspection.
            candidate.NativeIntegrationReviewEnabled=false;
            candidate.NativeFaceWorkshopEnabled=false;
            candidate.NativeStateShellEnabled=true;
            candidate.NativeFaceDockTrialEnabled=true;
            DiagnosticJournal.Event("cu_native_creator_auto_bridge",
                "shell=true|culture=native_vm|face=original_dock_with_restore" +
                "|supplemental_stages=false|visible_wizard_still_pending=true");
            if (candidate.NativeFaceWorkshopEnabled &&
                candidate.NativeIntegrationReviewEnabled)
            {
                SetStatus("Choose one experimental 3D stage: CU Face Workshop OR CU 3D Review.");
                return;
            }
            if (candidate.NativeStateShellEnabled &&
                (candidate.NativeFaceWorkshopEnabled ||
                 candidate.NativeIntegrationReviewEnabled))
            {
                SetStatus("CU native-state shell cannot be combined with a separate extra 3D stage.");
                return;
            }
            if (candidate.NativeFaceDockTrialEnabled &&
                !candidate.NativeStateShellEnabled)
            {
                SetStatus("Right-hand real 3D FaceGen trial requires the CU native-state shell to be Yes.");
                return;
            }
            // A deliberate spouse/children choice is an actual native family
            // request. Do not leave users with a decorative family profile.
            candidate.RealFamilyEnabled = candidate.SpouseEnabled ||
                candidate.ChildCount > 0 || candidate.SiblingCount > 0;
            string error;
            if (candidate.ChildCount > 0 && !candidate.SpouseEnabled)
            {
                SetStatus("Children require a selected spouse in this build.");
                return;
            }
            if (candidate.SiblingCount > 0)
            {
                SetStatus("Siblings need native parent heroes and are not " +
                    "available at fresh Sandbox start. Set sibling count to zero.");
                return;
            }
            // SpecificOwned has two separate edits: choose mode, then type
            // ID. Validate the complete request only at final acceptance.
            string homeError = SandboxFamilyHomePolicy.ValidateRequest(
                candidate.FamilyHomeMode,candidate.FamilyHomeId,
                candidate.SpouseEnabled);
            if (homeError.Length != 0)
            {
                SetStatus("Family home request: " + homeError);
                return;
            }
            string fiefError = SandboxFiefPolicy.ValidateRequest(
                candidate.Role, candidate.StartingFiefMode,
                candidate.StartingFiefId,
                candidate.AllowStartingFiefTransfer);
            if (fiefError.Length != 0)
            {
                SetStatus("Starting fief request: " + fiefError);
                return;
            }
            if (!SandboxNativeStartOptions.TryPrepare(candidate,
                out SandBox.AdvancedStartOptions.AdvancedStartOptions plan,
                out error))
            {
                SetStatus(error);
                DiagnosticJournal.Event("sandbox_creator_profile_refused",error);
                return;
            }
            // Only committed values are accepted. Record the actual list
            // passed to the native post-creation family constructor.
            DiagnosticJournal.Event("sandbox_creator_family_plan",
                "spouse=" + candidate.SpouseEnabled +
                "|child_count=" + candidate.ChildCount +
                "|children=" + String.Join(";",
                    candidate.Children.Take(candidate.ChildCount)
                        .Select(c => c.Name + ":" + c.Gender + ":" + c.Age)) +
                "|next=native_culture_appearance_origins_age_difficulty");
            if(!candidate.Save(SandboxOriginProfile.UserPath,out error))
            {
                SetStatus("Profile not activated: "+error+
                    ". Check spouse and child ages.");
                return;
            }
            profile=candidate;
            SandboxCreatorOverlay.Accept();
            DiagnosticJournal.Event("sandbox_overhaul_accepted",
                "role="+profile.Role+
                "|kingdom="+profile.StartingKingdomId+
                "|family_requested="+profile.RealFamilyEnabled+
                "|spouse_requested="+profile.SpouseEnabled+
                "|child_count="+profile.ChildCount+
                "|family_home_mode="+profile.FamilyHomeMode+
                "|family_home_id="+profile.FamilyHomeId+
                "|fief_mode="+profile.StartingFiefMode+
                "|native_face_header="+profile.NativeStageEnabled+
                "|native_cu_review="+profile.NativeIntegrationReviewEnabled+
                "|native_face_workshop="+profile.NativeFaceWorkshopEnabled+
                "|native_state_shell="+profile.NativeStateShellEnabled+
                "|real_face_dock_trial="+profile.NativeFaceDockTrialEnabled+
                "|hand_off=native_culture_appearance_origins_age_difficulty");
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
