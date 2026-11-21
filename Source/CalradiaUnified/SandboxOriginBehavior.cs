using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ModuleManager;
using SandBox.View.CharacterCreation;

namespace CalradiaUnified
{
    /// <summary>
    /// New-campaign-only founder profile. No legacy-save conversion, no NPC
    /// mutation, no early kingdom creation and no unsafe family registration.
    /// </summary>
    public sealed class SandboxOriginBehavior : CampaignBehaviorBase
    {
        private bool applied;
        private bool startedNewGame;
        private bool characterCreationComplete;
        private SandboxFamilyGrowthState familyGrowth =
            new SandboxFamilyGrowthState();
        private SandboxLifeHistoryState livingPersons=
            new SandboxLifeHistoryState();
        private SandboxSocialObservationState socialObservations=
            new SandboxSocialObservationState();
        private SandboxServiceCareerState serviceCareer=
            new SandboxServiceCareerState();
        private SandboxSuccessionInterestState successionInterest=
            new SandboxSuccessionInterestState();
        private CharacterCreationManager observedNativeCreator;
        private SandboxOriginProfile editing;
        private string returnMenu = "town";
        // Saved with this campaign. The external pregame profile is never
        // consulted for later noble allegiance or military obligations.
        private string dynasticElderHeroId = "";
        private string dynasticElderName = "";
        private string dynasticFatherHeroId = "";
        private string dynasticMotherHeroId = "";
        private string dynasticOlderSons = "";
        private string servicePatronHeroId = "";
        private string servicePatronName = "";

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, RegisterMenus);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(
                this, NewGameCreated);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                this, TryApplyOnDailyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                this, AdvanceFamilyUpbringing);
            // v1.5.7 fires ten completion phases. Native King/Vassal starts
            // run at phase 8; our resources and family run at phase 9.
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(
                this, CharacterCreationCompleted);
            CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(
                this, ObserveNativeCharacterCreation);
        }

        // The engine initializes the native narrative graph after receiving
        // OnCharacterCreationInitializedEvent. Register a read-only content
        // handler so the diagnostics reflect the COMPLETE graph, not an empty
        // or partially registered set of menus.
        private void ObserveNativeCharacterCreation(CharacterCreationManager manager)
        {
            if (!IsStandaloneSandbox() || manager == null ||
                Object.ReferenceEquals(observedNativeCreator, manager)) return;
            observedNativeCreator = manager;
            try
            {
                // The custom editor now launches from SandBoxNewGame itself.
                // Keep this observer read-only for native stage diagnostics.
                manager.RegisterCharacterCreationContentHandler(
                    new NativeSandboxStageObserver(), 1600);
                DiagnosticJournal.Event("cu_native_sandbox_creator",
                    "observer_registered=true|mutates_stages=false|custom_entry=main_menu");
            }
            catch (Exception e)
            {
                DiagnosticJournal.Exception("native_sandbox_observer_register", e);
            }
        }

        private sealed class NativeSandboxStageObserver :
            ICharacterCreationContentHandler
        {
            public void InitializeContent(CharacterCreationManager manager)
            {
                // Native handlers own all stage content and choices.
            }

            public void AfterInitializeContent(CharacterCreationManager manager)
            {
                if (manager == null) return;
                try
                {
                    // This opt-in stage has a distinct CharacterCreationStageBase
                    // type and a distinct view attribute. No native stage is
                    // removed and NO FaceGen, Culture, Review or Options view
                    // registration is replaced. Default disabled until the
                    // player's live v1.5.7 acceptance test passes.
                    var originProfile = SandboxOriginProfile.Load(
                        SandboxOriginProfile.UserPath);
                    bool nativeComplete =
                        manager.GetStage<CharacterCreationCultureStage>() != null &&
                        manager.GetStage<CharacterCreationFaceGeneratorStage>() != null &&
                        manager.GetStage<CharacterCreationReviewStage>() != null &&
                        manager.GetStage<CharacterCreationOptionsStage>() != null;
                    // CharacterCreationScreen discovers stage views only
                    // from active assemblies referencing SandBox.View.
                    // Mirror that exact public engine discovery check BEFORE
                    // adding a stage that otherwise could have a blank view.
                    bool viewDiscoverable = false;
                    try
                    {
                        var ownAssembly =
                            typeof(CUNativeOriginReviewStageView).Assembly;
                        var attributeAssembly =
                            typeof(CharacterCreationStageViewAttribute).Assembly;
                        var attr = (CharacterCreationStageViewAttribute)
                            Attribute.GetCustomAttribute(
                                typeof(CUNativeOriginReviewStageView),
                                typeof(CharacterCreationStageViewAttribute),
                                false);
                        viewDiscoverable = attr != null &&
                            attr.StageType == typeof(CUNativeOriginReviewStage) &&
                            (ownAssembly == attributeAssembly ||
                             attributeAssembly.GetActiveReferencingGameAssembliesSafe()
                                 .Contains(ownAssembly));
                    }
                    catch (Exception error)
                    {
                        DiagnosticJournal.Exception(
                            "cu_native_view_discovery_probe",error);
                    }
                    // The native 3D face workshop is another UNIQUE stage
                    // type. It composes (never replaces) the true FaceGenerator
                    // renderer and keeps its input focus. Default is OFF.
                    bool faceWorkshopDiscoverable = false;
                    try
                    {
                        var faceType = typeof(CUFaceWorkshopStageView);
                        var faceAttr = (CharacterCreationStageViewAttribute)
                            Attribute.GetCustomAttribute(faceType,
                                typeof(CharacterCreationStageViewAttribute), false);
                        var stageAssembly = faceType.Assembly;
                        var attributeAssembly =
                            typeof(CharacterCreationStageViewAttribute).Assembly;
                        faceWorkshopDiscoverable = faceAttr != null &&
                            faceAttr.StageType == typeof(CUFaceWorkshopStage) &&
                            (stageAssembly == attributeAssembly ||
                             attributeAssembly.GetActiveReferencingGameAssembliesSafe()
                                 .Contains(stageAssembly));
                    }
                    catch (Exception error)
                    {
                        DiagnosticJournal.Exception(
                            "cu_face_workshop_view_discovery_probe", error);
                    }
                    var availableStages = new[]
                    {
                        manager.GetStage<CharacterCreationCultureStage>() != null ?
                            "CharacterCreationCultureStage" : "",
                        manager.GetStage<CharacterCreationFaceGeneratorStage>() != null ?
                            "CharacterCreationFaceGeneratorStage" : "",
                        manager.GetStage<CharacterCreationNarrativeStage>() != null ?
                            "CharacterCreationNarrativeStage" : "",
                        manager.GetStage<CharacterCreationBannerEditorStage>() != null ?
                            "CharacterCreationBannerEditorStage" : "",
                        manager.GetStage<CharacterCreationClanNamingStage>() != null ?
                            "CharacterCreationClanNamingStage" : "",
                        manager.GetStage<CharacterCreationReviewStage>() != null ?
                            "CharacterCreationReviewStage" : "",
                        manager.GetStage<CharacterCreationOptionsStage>() != null ?
                            "CharacterCreationOptionsStage" : ""
                    };
                    var missingNative =
                        NativeCreatorParityPolicy.MissingStages(availableStages);
                    DiagnosticJournal.Event("cu_face_workshop_native_parity",
                        "missing=" + String.Join(",", missingNative) +
                        "|native_menus=" + manager.NarrativeMenus.Count);
                    if (NativeCreatorParityPolicy.CanAddFaceWorkshop(
                        originProfile.Enabled &&
                        originProfile.NativeFaceWorkshopEnabled,
                        true, originProfile.NativeIntegrationReviewEnabled,
                        faceWorkshopDiscoverable, availableStages) &&
                        manager.GetStage<CUFaceWorkshopStage>() == null)
                    {
                        manager.AddStage(new CUFaceWorkshopStage());
                        DiagnosticJournal.Event("cu_face_workshop_attached",
                            "unique_stage=true|native_face_generator_intact=true" +
                            "|native_options_intact=true|3d=native_editor");
                    }
                    else
                    {
                        DiagnosticJournal.Event("cu_face_workshop_skipped",
                            "opt_in=" + originProfile.NativeFaceWorkshopEnabled +
                            "|native_complete=" + nativeComplete +
                            "|unique_view_discoverable=" +
                            faceWorkshopDiscoverable +
                            "|conflicting_review=" +
                            originProfile.NativeIntegrationReviewEnabled);
                    }
                    if (originProfile.Enabled &&
                        originProfile.NativeIntegrationReviewEnabled &&
                        !originProfile.NativeFaceWorkshopEnabled &&
                        nativeComplete && viewDiscoverable &&
                        manager.GetStage<CUNativeOriginReviewStage>() == null)
                    {
                        manager.AddStage(new CUNativeOriginReviewStage());
                        DiagnosticJournal.Event("cu_native_review_attached",
                            "added_unique_final_stage=true" +
                            "|native_culture_face_review_options_intact=true" +
                            "|3d=native_review_view");
                    }
                    else
                    {
                        DiagnosticJournal.Event("cu_native_review_skipped",
                            "opt_in=" +
                            originProfile.NativeIntegrationReviewEnabled +
                            "|profile_active=" + originProfile.Enabled +
                            "|native_stages_complete=" + nativeComplete +
                            "|view_discoverable=" + viewDiscoverable);
                    }
                    // The discontinued age-menu relinker is never used with
                    // the independent custom front-end. The native graph
                    // remains intact as the escape/recovery fallback.
                    DiagnosticJournal.Event("cu_native_sandbox_stage",
                        "total=" + manager.GetTotalStagesCount() +
                        "|narrative_menus=" + manager.NarrativeMenus.Count);
                    // Direct engine-surface evidence for the next creator:
                    // the 3D face editor must live inside CharacterCreationState.
                    // This probe does not delete stages or replace any view.
                    bool faceStage = manager.GetStage<CharacterCreationFaceGeneratorStage>()
                        != null;
                    bool cultureStage = manager.GetStage<CharacterCreationCultureStage>()
                        != null;
                    bool narrativeStage = manager.GetStage<CharacterCreationNarrativeStage>()
                        != null;
                    bool reviewStage = manager.GetStage<CharacterCreationReviewStage>()
                        != null;
                    // Do not probe native screen view assembly loading
                    // during creation-content initialization. At this point
                    // an unloaded assembly says nothing about final UI state.
                    DiagnosticJournal.Event("sandbox_appearance_engine_probe",
                        "face_stage=" + faceStage +
                        "|culture_stage=" + cultureStage +
                        "|narrative_stage=" + narrativeStage +
                        "|review_stage=" + reviewStage +
                        "|view_resolution=not_inspected" +
                        "|cu_native_review_appended=" +
                        (manager.GetStage<CUNativeOriginReviewStage>() != null) +
                        "|face_view_replacement=false");
                    foreach (var menu in manager.NarrativeMenus)
                    {
                        if (menu == null) continue;
                        DiagnosticJournal.Event("cu_native_sandbox_menu",
                            "id=" + menu.StringId +
                            "|input=" + menu.InputMenuId +
                            "|output=" + menu.OutputMenuId);
                    }
                }
                catch (Exception e)
                {
                    DiagnosticJournal.Exception("native_sandbox_graph", e);
                }
            }

            public void OnStageCompleted(CharacterCreationStageBase stage)
            {
                if (stage != null)
                    DiagnosticJournal.Event("cu_native_sandbox_stage_completed",
                        stage.GetType().Name);
            }

            public void OnCharacterCreationFinalize(CharacterCreationManager manager)
            {
                // CharacterCreationManager.ApplyFinalEffects invokes this
                // BEFORE CharacterCreationState.FinalizeCharacterCreationState
                // destroys the native screen. Release all CU Gauntlet movies
                // here so ScreenBase cannot finalize a live CU movie twice.
                CUNativeStateShell.Clear();
                DiagnosticJournal.Event("cu_native_shell_released_before_finalization",
                    "native_character_creator_owned_by_game=true");
                DiagnosticJournal.Event("cu_native_sandbox_finish",
                    "native_handler_finished=true" +
                    "|native_review_opt_in=" +
                    (manager.GetStage<CUNativeOriginReviewStage>() != null));
            }
        }

        public override void SyncData(IDataStore store)
        {
            store.SyncData("CU_v016_origin_applied", ref applied);
            store.SyncData("CU_v060_dynastic_elder",
                ref dynasticElderHeroId);
            store.SyncData("CU_v062_dynastic_elder_name",
                ref dynasticElderName);
            store.SyncData("CU_v063_native_father",
                ref dynasticFatherHeroId);
            store.SyncData("CU_v063_native_mother",
                ref dynasticMotherHeroId);
            store.SyncData("CU_v063_older_brothers",
                ref dynasticOlderSons);
            store.SyncData("CU_v063_service_patron_id",
                ref servicePatronHeroId);
            store.SyncData("CU_v063_service_patron_name",
                ref servicePatronName);
            // Keep only our generated children's personal upbringing
            // records in the actual campaign save. A different player's
            // profile.ini cannot rewrite this campaign after creation.
            string packed=familyGrowth.Serialize();
            store.SyncData("CU_v052_family_growth",ref packed);
            familyGrowth=SandboxFamilyGrowthState.Parse(packed);
            string biographies=livingPersons.Serialize();
            store.SyncData("CU_v064_living_persons",ref biographies);
            livingPersons=SandboxLifeHistoryState.Parse(biographies);
            string social=socialObservations.Serialize();
            store.SyncData("CU_v065_social_observations",ref social);
            socialObservations=SandboxSocialObservationState.Parse(social);
            string service=serviceCareer.Serialize();
            store.SyncData("CU_v065_service_career",ref service);
            serviceCareer=SandboxServiceCareerState.Parse(service);
            string claim=successionInterest.Serialize();
            store.SyncData("CU_v065_succession_interest",ref claim);
            successionInterest=SandboxSuccessionInterestState.Parse(claim);
        }

        private static void Notify(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "[Calradia Unified] " + text));
        }

        private SandboxOriginProfile Editing
        {
            get { return editing ?? (editing = SandboxOriginProfile.Load(
                SandboxOriginProfile.UserPath)); }
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            // Prefer the first actual session launch over a delayed daily tick.
            TryApplyOnce();
            if (starter == null || !IsStandaloneSandbox()) return;
            starter.AddGameMenu("cu_origin_main",
                "Calradia Unified | Prepare an origin for your NEXT Sandbox game. " +
                "This menu does not edit an existing campaign.", args => { });
            starter.AddGameMenu("cu_origin_roles",
                "Select a role for your next Sandbox. King and Lord still " +
                "require the game's matching Advanced Start.", args => { });
            starter.AddGameMenu("cu_origin_family",
                "Design your starting family. Profile templates are saved; " +
                "NPC birth and marriage are not yet active.", args => { });
            starter.AddGameMenu("cu_origin_wealth",
                "Starting money and influence for a new Sandbox only.",
                args => { });
            starter.AddGameMenu("cu_origin_stats",
                "Profile-only player attributes, skill levels, focus and traits.",
                args => { });
            starter.AddGameMenu("cu_origin_items",
                "Profile-only starter equipment kits and exact item IDs.",
                args => { });
            starter.AddGameMenu("cu_origin_location",
                "Choose a town for your next Sandbox profile. Relocation is " +
                "disabled pending game tests.", args => { });
            foreach (string sourceMenu in new[] { "town", "castle" })
            {
                string from = sourceMenu;
                starter.AddGameMenuOption(sourceMenu, "cu_origin_entry_" + sourceMenu,
                    "CU: Design my NEXT Sandbox starting life",
                    args => true, args =>
                    {
                        returnMenu = from;
                        GameMenu.SwitchToMenu("cu_origin_main");
                    }, false);
                starter.AddGameMenuOption(from,
                    "cu_family_portraits_" + from,
                    "CU: View my household portraits",
                    args => Hero.MainHero != null &&
                        (Hero.MainHero.Spouse != null ||
                         (Hero.MainHero.Children != null &&
                          Hero.MainHero.Children.Count > 0)),
                    args =>
                    {
                        try
                        {
                            ScreenManager.PushScreen(
                                new CUFamilyPortraitScreen());
                        }
                        catch (Exception error)
                        {
                            DiagnosticJournal.Exception(
                                "cu_family_portrait_screen_failed",error);
                            try
                            {
                                if (ScreenManager.TopScreen is
                                    CUFamilyPortraitScreen)
                                    ScreenManager.PopScreen();
                            }
                            catch (Exception secondary)
                            {
                                DiagnosticJournal.Exception(
                                    "cu_family_portrait_screen_recovery",
                                    secondary);
                            }
                            Notify("Household gallery could not open; " +
                                "native Clan screen remains available.");
                        }
                    },false);
                // Explicit repair for existing saves. The player must stand
                // in their OWN castle or town to move the family there.
                // This never changes the child HeroState or adds a minor to
                // a party. It is not an automatic retroactive save migration.
                starter.AddGameMenuOption(from,
                    "cu_family_residence_here_" + from,
                    "CU: Make this my family's home",
                    args => Hero.MainHero != null &&
                        Hero.MainHero.Spouse != null &&
                        Clan.PlayerClan != null &&
                        Settlement.CurrentSettlement != null &&
                        Settlement.CurrentSettlement.OwnerClan ==
                            Clan.PlayerClan &&
                        Settlement.CurrentSettlement.IsFortification,
                    args => Notify("Family residence: " +
                        SandboxFamilyHome.PlaceExistingFamilyAt(
                            Hero.MainHero, Settlement.CurrentSettlement)),
                    false);
                starter.AddGameMenuOption(from,
                    "cu_living_status_"+from,
                    "CU: Review my service, relatives and recorded history",
                    args=>livingPersons.People.Count>0,
                    args=>Notify(
                        "Recorded people: "+livingPersons.People.Count+
                        "; native relationship bonds: "+livingPersons.Bonds.Count+
                        "; observed events: "+livingPersons.Events.Count+
                        ". Personal service: "+serviceCareer.Status+
                        (serviceCareer.IsConfigured?
                            " under "+servicePatronName+
                            ", "+serviceCareer.DaysInService+
                            " daily observations; wages follow native game rules.":"")+
                        ". Family succession: "+successionInterest.Status+
                        " (no estate transfer by CU)."),
                    false);
            }
            starter.AddGameMenuOption("cu_origin_main", "cu_origin_preview",
                "Preview my next-game setup", args => true,
                args => Notify(Editing.Describe()), false);
            starter.AddGameMenuOption("cu_origin_main", "cu_origin_activate",
                "Enable this origin for the NEXT Sandbox",
                args => !Editing.Enabled,
                args => Update("Enabled", "true"), false);
            starter.AddGameMenuOption("cu_origin_main", "cu_origin_deactivate",
                "Disable origin (use vanilla starting setup)",
                args => Editing.Enabled,
                args => Update("Enabled", "false"), false);
            AddNavigation(starter, "cu_origin_main", "roles", "Starting identity",
                "cu_origin_roles");
            AddNavigation(starter, "cu_origin_main", "family", "Starting family",
                "cu_origin_family");
            AddNavigation(starter, "cu_origin_main", "wealth", "Starting wealth",
                "cu_origin_wealth");
            AddNavigation(starter, "cu_origin_main", "location", "Starting town",
                "cu_origin_location");
            AddNavigation(starter, "cu_origin_main", "stats",
                "Attributes, skills, focus and traits", "cu_origin_stats");
            AddNavigation(starter, "cu_origin_main", "items",
                "Starter equipment", "cu_origin_items");
            starter.AddGameMenuOption("cu_origin_main", "cu_origin_file",
                "Show advanced editor profile path", args => true,
                args => Notify(SandboxOriginProfile.UserPath), false);
            starter.AddGameMenuOption("cu_origin_main", "cu_origin_exit",
                "Return to settlement", args => true,
                args => GameMenu.SwitchToMenu(returnMenu), false);
            foreach (string role in SandboxOriginProfile.Roles)
            {
                string choice = role;
                starter.AddGameMenuOption("cu_origin_roles", "cu_origin_role_" + role,
                    "Select " + role,
                    args => Editing.Role != choice,
                    args => Update("Role", choice), false);
            }
            AddBack(starter, "cu_origin_roles");
            starter.AddGameMenuOption("cu_origin_family", "cu_spouse_enable",
                "Begin with spouse template", args => !Editing.SpouseEnabled,
                args => Update("SpouseEnabled", "true"), false);
            starter.AddGameMenuOption("cu_origin_family", "cu_spouse_disable",
                "Begin without spouse", args => Editing.SpouseEnabled,
                args => Update("SpouseEnabled", "false"), false);
            for (int count = 0; count <= 6; count++)
            {
                string n = count.ToString();
                int value = count;
                starter.AddGameMenuOption("cu_origin_family", "cu_children_" + n,
                    "Choose " + n + " child templates",
                    args => Editing.ChildCount != value,
                    args => Update("ChildCount", n), false);
            }
            AddText(starter, "cu_origin_family", "spouse_name",
                "Name your spouse template", "SpouseName", () => Editing.SpouseName);
            AddText(starter, "cu_origin_family", "spouse_age",
                "Spouse age (18-70)", "SpouseAge", () => Editing.SpouseAge.ToString());
            AddText(starter, "cu_origin_family", "spouse_face",
                "Spouse appearance seed", "SpouseFaceSeed", () => Editing.SpouseFaceSeed);
            foreach (var gender in new[] { "Female", "Male" })
            {
                string chosen = gender;
                starter.AddGameMenuOption("cu_origin_family", "cu_spouse_gender_" + chosen,
                    "Spouse gender: " + chosen, args => Editing.SpouseGender != chosen,
                    args => Update("SpouseGender", chosen), false);
            }
            for (int i = 1; i <= SandboxOriginProfile.MaxChildren; i++)
            {
                int index = i;
                AddText(starter, "cu_origin_family", "child_name_" + index,
                    "Child " + index + " name", "Child." + index + ".Name",
                    () => Editing.Children[index - 1].Name);
                AddText(starter, "cu_origin_family", "child_age_" + index,
                    "Child " + index + " age", "Child." + index + ".Age",
                    () => Editing.Children[index - 1].Age.ToString());
                AddText(starter, "cu_origin_family", "child_face_" + index,
                    "Child " + index + " appearance seed",
                    "Child." + index + ".FaceSeed",
                    () => Editing.Children[index - 1].FaceSeed);
                string childGender = "Child." + index + ".Gender";
                starter.AddGameMenuOption("cu_origin_family", "cu_child_gender_" + index,
                    "Change child " + index + " gender",
                    args => true, args =>
                        Update(childGender, Editing.Children[index - 1].Gender ==
                            "Female" ? "Male" : "Female"), false);
            }
            AddText(starter, "cu_origin_family", "siblings",
                "Number of siblings (0-4)", "SiblingCount",
                () => Editing.SiblingCount.ToString());
            starter.AddGameMenuOption("cu_origin_family", "cu_family_preview",
                "Show spouse and child names", args => true,
                args => Notify(Editing.SpouseName + ", children=" +
                    String.Join(", ", Editing.Children.Take(Editing.ChildCount)
                        .Select(c => c.Name + " (" + c.Age + ")"))), false);
            AddBack(starter, "cu_origin_family");
            foreach (int gold in new[] { -1, 0, 500, 3000, 10000, 50000, 250000 })
            {
                int amount = gold;
                starter.AddGameMenuOption("cu_origin_wealth", "cu_gold_" + gold,
                    "Starting gold: " + (gold < 0 ? "vanilla" : gold.ToString()),
                    args => Editing.Gold != amount,
                    args => Update("Gold", amount.ToString()), false);
            }
            foreach (int influence in new[] { -1, 0, 25, 100, 500, 1000 })
            {
                int amount = influence;
                starter.AddGameMenuOption("cu_origin_wealth", "cu_influence_" + influence,
                    "Starting influence: " +
                    (influence < 0 ? "vanilla" : influence.ToString()),
                    args => Editing.Influence != amount,
                    args => Update("Influence", amount.ToString()), false);
            }
            AddBack(starter, "cu_origin_wealth");
            starter.AddGameMenuOption("cu_origin_location", "cu_location_native",
                "Keep the game's original starting location",
                args => Editing.StartingTown.Length > 0,
                args => Update("StartingTown", ""), false);
            // Settlement strings are obtained from the loaded campaign itself.
            // Do not hardcode unsupported town coordinates or fictional IDs.
            foreach (var town in Settlement.All.Where(s => s != null &&
                s.IsTown).Take(60))
            {
                string id = town.StringId;
                string title = town.Name.ToString();
                starter.AddGameMenuOption("cu_origin_location",
                    "cu_location_" + id, "Preferred town: " + title,
                    args => Editing.StartingTown != id,
                    args => Update("StartingTown", id), false);
            }
            AddText(starter, "cu_origin_location", "specific_town",
                "Enter exact settlement StringId", "StartingTown",
                () => Editing.StartingTown);
            AddBack(starter, "cu_origin_location");

            foreach (string attr in SandboxOriginProfile.Attributes)
            {
                string key = "Attribute." + attr;
                AddText(starter, "cu_origin_stats", "attribute_" + attr,
                    "Player attribute " + attr + " (1-10)", key,
                    () => ReadOverride(key));
            }
            foreach (string skill in SandboxOriginProfile.Skills)
            {
                string level = "Skill." + skill;
                string focus = "Focus." + skill;
                AddText(starter, "cu_origin_stats", "level_" + skill,
                    "Player " + skill + " skill (0-330)", level,
                    () => ReadOverride(level));
                AddText(starter, "cu_origin_stats", "focus_" + skill,
                    "Player " + skill + " focus (0-5)", focus,
                    () => ReadOverride(focus));
            }
            foreach (string trait in SandboxOriginProfile.Traits)
            {
                string key = "Trait." + trait;
                AddText(starter, "cu_origin_stats", "trait_" + trait,
                    "Player " + trait + " trait (-2 to 2)", key,
                    () => ReadOverride(key));
            }
            AddText(starter, "cu_origin_stats", "player_age",
                "Player age (18-80, -1 for vanilla)", "PlayerAge",
                () => Editing.PlayerAge.ToString());
            AddBack(starter, "cu_origin_stats");
            foreach (string kit in SandboxOriginProfile.Kits)
            {
                string chosen = kit;
                starter.AddGameMenuOption("cu_origin_items", "cu_kit_" + kit,
                    "Equipment template: " + chosen,
                    args => Editing.EquipmentKit != chosen,
                    args => Update("EquipmentKit", chosen), false);
            }
            for (int i = 1; i <= 12; i++)
            {
                int slot = i;
                AddText(starter, "cu_origin_items", "item_" + slot,
                    "Exact item ID slot " + slot, "Item." + slot,
                    () => Editing.ItemIds.ContainsKey(slot) ?
                        Editing.ItemIds[slot] : "");
            }
            AddBack(starter, "cu_origin_items");
        }

        private string ReadOverride(string key)
        {
            int value;
            return Editing.Overrides.TryGetValue(key, out value)
                ? value.ToString() : "";
        }

        private void AddText(CampaignGameStarter starter, string menu,
            string id, string label, string key, Func<string> current)
        {
            starter.AddGameMenuOption(menu, "cu_origin_edit_" + id, label,
                args => true,
                args => InformationManager.ShowTextInquiry(new TextInquiryData(
                    label, "For your NEXT new Sandbox only.",
                    true, true, "Save", "Cancel",
                    text => Update(key, text), () => { },
                    false, null, "", current())), false);
        }

        private void AddNavigation(CampaignGameStarter starter,
            string menu, string key, string label, string target)
        {
            starter.AddGameMenuOption(menu, "cu_origin_go_" + key,
                label, args => true, args => GameMenu.SwitchToMenu(target), false);
        }
        private void AddBack(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_origin_back_" + menu,
                "Back to origin setup", args => true,
                args => GameMenu.SwitchToMenu("cu_origin_main"), false);
        }

        private void Update(string key, string value)
        {
            var candidate = SandboxOriginProfile.Parse(Editing.Serialize());
            string error;
            if (!candidate.TrySet(key, value) ||
                !candidate.Save(SandboxOriginProfile.UserPath, out error))
            {
                Notify("Origin change refused: " + key);
                return;
            }
            editing = candidate;
            DiagnosticJournal.Event("sandbox_origin_edit", "key=" + key +
                "|value=" + value + "|affects_next_new_game_only");
            Notify("Saved " + key + "=" + value + " for the next Sandbox.");
        }

        private static bool IsStandaloneSandbox()
        {
            // StoryMode.CampaignStoryMode is a subclass of Campaign.
            return Game.Current != null && Game.Current.GameType != null &&
                Game.Current.GameType.GetType() == typeof(Campaign);
        }

        private void NewGameCreated(CampaignGameStarter starter)
        {
            if (applied || !IsStandaloneSandbox()) return;
            // Arm only on a new Sandbox. A loaded save never runs this event.
            startedNewGame = true;
            characterCreationComplete = false;
            DiagnosticJournal.Event("sandbox_origin_new_game_armed",
                "waiting_for_character_creation_phase_9");
        }

        private void CharacterCreationCompleted(int phase)
        {
            if (phase != 9 || applied || !IsStandaloneSandbox()) return;
            characterCreationComplete = true;
            startedNewGame = true;
            DiagnosticJournal.Event("sandbox_origin_creation_complete",
                "phase=9|native_advanced_start_has_run=true");
            TryApplyOnce();
        }

        private void TryApplyOnDailyTick()
        {
            if (characterCreationComplete && startedNewGame && !applied)
                TryApplyOnce();
            ObserveLivingWorld();
        }

        private void ObserveLivingWorld()
        {
            if(!applied || !IsStandaloneSandbox() ||
                livingPersons==null || livingPersons.People.Count==0)
                return;
            var player=Hero.MainHero;
            if(player==null ||
                !livingPersons.People.Any(p=>p.Id==player.StringId))
                return;
            try
            {
                SandboxLivingPersons.ObserveDaily(livingPersons,
                    socialObservations,player);
                if(serviceCareer.IsConfigured && Clan.PlayerClan!=null)
                {
                    var patron=Hero.AllAliveHeroes.FirstOrDefault(h=>
                        h!=null && h.StringId==serviceCareer.PatronId);
                    // Absence from AllAliveHeroes is not proof of death.
                    // Leave the contract pending rather than forging a
                    // death/discharge event for an unloaded native Hero.
                    if(patron!=null)
                    {
                        bool sameKingdom=Clan.PlayerClan.Kingdom!=null &&
                            Clan.PlayerClan.Kingdom.StringId==
                                serviceCareer.KingdomId &&
                            patron.Clan!=null &&
                            patron.Clan.Kingdom==Clan.PlayerClan.Kingdom;
                        string result=serviceCareer.Observe(
                            patron.IsAlive,sameKingdom,player.Gold);
                        if(result!="active" && result!=serviceCareer.Status)
                        {
                            livingPersons.Remember(
                                "NativeServiceStatusObserved",
                                player.StringId,patron.StringId,
                                "result="+result+
                                "|native_contract_authoritative=true");
                            DiagnosticJournal.Event(
                                "cu_personal_service_observed",
                                "result="+result+
                                "|days="+serviceCareer.DaysInService+
                                "|patron="+patron.StringId);
                        }
                    }
                }
                if(successionInterest.IsRecorded &&
                    player.Father!=null &&
                    player.Father.StringId==successionInterest.ParentId)
                {
                    string result=successionInterest.Observe(
                        player.Father.IsAlive);
                    if(result=="parent_death_observed_native_ownership_unchanged")
                    {
                        livingPersons.Remember("PotentialSuccessionReview",
                            player.StringId,player.Father.StringId,
                            "native_fief_ownership_unchanged"+
                            "|legal_entitlement_not_assumed");
                        DiagnosticJournal.Event(
                            "cu_succession_parent_death_observed",
                            "parent="+successionInterest.ParentId+
                            "|claimant="+successionInterest.ClaimantId+
                            "|fiefs_transferred=0");
                    }
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_living_world_daily_observation_failed",error);
            }
        }

        private void AdvanceFamilyUpbringing()
        {
            if(familyGrowth==null || familyGrowth.Count==0 ||
                !IsStandaloneSandbox())return;
            var player=Hero.MainHero;
            if(player==null || player.Children==null)return;
            // This runs for underage, NotSpawned family members too.
            // They need not be active parties for their education to grow.
            foreach(var child in player.Children)
            {
                if(child==null || !child.IsAlive)continue;
                int age=(int)child.Age;
                foreach(int step in new[]{6,10,14,18})
                {
                    if(age<step)continue;
                    string upbringing;
                    if(!familyGrowth.TryAdvance(
                        child.StringId,step,out upbringing))
                        continue;
                    try
                    {
                        SandboxFamilyDevelopment.Milestone(
                            child,familyGrowth.Scenario,upbringing,step);
                    }
                    catch(Exception error)
                    {
                        DiagnosticJournal.Exception(
                            "cu_family_upbringing_milestone_failed",error);
                    }
                }
            }
        }

        private void RegisterFamilyUpbringings(
            SandboxOriginProfile profile,Hero player,string familyResult)
        {
            if(!profile.SpouseEnabled || profile.ChildCount==0 ||
                !familyResult.StartsWith("spouse=",
                    StringComparison.Ordinal) ||
                player.Children==null)
                return;
            familyGrowth.SetScenario(profile.FamilyScenario);
            foreach(var plan in profile.Children.Take(profile.ChildCount))
            {
                var child=player.Children.FirstOrDefault(x=>
                    x!=null && x.Name.ToString()==plan.Name &&
                    x.IsFemale==(plan.Gender=="Female") &&
                    SandboxFamilyAgePolicy.MatchesRequestedAge(
                        x.Age,plan.Age));
                if(child==null)
                {
                    DiagnosticJournal.Event("cu_family_growth_missing_child",
                        "name="+plan.Name+"|scenario="+profile.FamilyScenario);
                    continue;
                }
                familyGrowth.Register(
                    child.StringId,plan.Upbringing,(int)child.Age);
            }
            DiagnosticJournal.Event("cu_family_growth_registered",
                "scenario="+familyGrowth.Scenario+
                "|registered="+familyGrowth.Count+
                "|requested="+profile.ChildCount+
                "|milestone_save_state=true");
        }

        private void TryApplyOnce()
        {
            if (!characterCreationComplete || !startedNewGame ||
                applied || !IsStandaloneSandbox()) return;
            var hero = Hero.MainHero;
            if (hero == null || Clan.PlayerClan == null) return;

            // Commit the one-shot barrier before any resources are changed.
            // Save/load or partial action failure must not duplicate grants.
            applied = true;
            startedNewGame = false;
            if (SandboxCreatorOverlay.BlockNewGameProfile)
            {
                DiagnosticJournal.Event("sandbox_creator_cancelled_no_grants",
                    "native_start_only=true");
                return;
            }
            SandboxOriginProfile profile = SandboxOriginProfile.Load(
                SandboxOriginProfile.UserPath);
            if (!profile.Enabled)
            {
                DiagnosticJournal.Event("sandbox_origin_disabled_for_new_game",
                    "native_start_unchanged");
                return;
            }
            string reason;
            if (!profile.IsValid((int)hero.Age, out reason))
            {
                DiagnosticJournal.Event("sandbox_origin_invalid", reason);
                return;
            }
            // The native start has run at phase 8. Refuse a custom grant
            // plan that cannot satisfy its promised noble family or patron.
            // We cannot roll back the native king/vassal/mercenary start.
            string lifeGate=SandboxStartingLifePreflight.Check(
                profile,hero);
            DiagnosticJournal.Event("cu_starting_life_preflight",
                "result="+(lifeGate.Length==0?"PASS":"FAIL")+
                "|reason="+lifeGate+
                "|role="+profile.Role+
                "|noble_birth="+profile.NobleBirthPosition);
            if(lifeGate.Length>0)
            {
                DiagnosticJournal.Event("sandbox_start_contract",
                    "result=FAIL|reason=preflight_"+lifeGate+
                    "|no_custom_grants_applied=true");
                Notify("The selected Starting Life could not be "+
                    "established: "+lifeGate+
                    ". Native campaign status was left unchanged.");
                return;
            }
            try
            {
                // The player start was already applied by native ASO at
                // phase 8, including King/Vassal affiliation and starting fiefs.
                string actualStart = Campaign.Current.AdvancedStartData == null ?
                    "" : Campaign.Current.AdvancedStartData.GetStartType();
                string affiliation = SandboxRoleAffiliations.TryApply(profile,hero);
                DiagnosticJournal.Event("cu_role_affiliation_status",
                    "role=" + profile.Role +
                    "|native_start_type=" + actualStart +
                    "|kingdom=" + (Clan.PlayerClan.Kingdom == null ?
                        "none" : Clan.PlayerClan.Kingdom.StringId) +
                    "|status=" + affiliation);

                // Soldier and Knight require a real commanding noble.
                // The native vassal or mercenary kingdom allegiance remains.
                Hero patron;
                string patronStatus=SandboxServicePatron.TryAssign(
                    profile,hero,out patron);
                if(patron!=null)
                {
                    servicePatronHeroId=patron.StringId;
                    servicePatronName=patron.Name.ToString();
                    Notify(profile.Role+" service confirmed under "+
                        servicePatronName+" of "+
                        patron.Clan.Name.ToString()+
                        ". The kingdom contract remains native.");
                }
                DiagnosticJournal.Event("cu_service_start_status",
                    "role="+profile.Role+
                    "|result="+patronStatus+
                    "|patron_id="+servicePatronHeroId+
                    "|patron_name="+servicePatronName);
                if(SandboxServicePolicy.NeedsPatron(profile.Role) &&
                    patron==null)
                    Notify(profile.Role+" start has no verified "+
                        "commanding lord: "+patronStatus);

                // Native ASO is the authority for king/vassal/mercenary
                // affiliation. A role label must never forge king status.
                if (profile.Role == "King" &&
                    (Clan.PlayerClan.Kingdom == null ||
                     Clan.PlayerClan.Kingdom.RulingClan != Clan.PlayerClan))
                {
                    DiagnosticJournal.Event("sandbox_origin_role_mismatch",
                        "King requires the native King Advanced Start");
                }
                if (profile.Role == "Lord" &&
                    (Clan.PlayerClan.Kingdom == null ||
                     Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan))
                    DiagnosticJournal.Event("sandbox_origin_role_mismatch",
                        "Lord requires native Vassal Advanced Start");

                // Named fiefs are resolved only after the game exists.
                // A startup castle/town choice is consent-gated and must
                // belong to the actual selected starting kingdom.
                string fiefOutcome = SandboxStartingFief.TryApply(profile,hero);
                DiagnosticJournal.Event("sandbox_start_fief_result",
                    "mode=" + profile.StartingFiefMode +
                    "|requested_id=" + profile.StartingFiefId +
                    "|result=" + fiefOutcome);
                if (fiefOutcome.StartsWith("refused:",
                    StringComparison.Ordinal) ||
                    fiefOutcome.StartsWith("failed:",
                    StringComparison.Ordinal))
                    Notify("Starting fief request could not be fulfilled: " +
                        fiefOutcome);

                // Native Advanced Start can supply independent companions.
                // They are not the player's requested spouse or children.
                // Record this before any family heroes are created.
                DiagnosticJournal.Event("cu_native_companion_snapshot",
                    "clan_companions=" + Clan.PlayerClan.Companions.Count +
                    "|ids=" + String.Join(",",
                        Clan.PlayerClan.Companions.Select(h =>
                            h == null ? "null" : h.StringId)));
                // Compare creator's final saved request with the exact
                // profile loaded after native age/difficulty finalization.
                // This distinguishes a UI save defect from child creation.
                DiagnosticJournal.Event("cu_family_loaded_plan",
                    "spouse=" + profile.SpouseEnabled +
                    "|requested_spouse_age=" + profile.SpouseAge +
                    "|family_scenario=" + profile.FamilyScenario +
                    "|child_upbringings=" + String.Join(",",
                        profile.Children.Take(profile.ChildCount)
                            .Select(x=>x.Upbringing)) +
                    "|actual_player_age=" + hero.Age +
                    "|requested_children=" + profile.ChildCount +
                    "|details=" + String.Join(";",
                        profile.Children.Take(profile.ChildCount).Select(
                            c => c.Name + ":" + c.Gender + ":" + c.Age)));
                // Apply the player's FINAL custom skills and attributes
                // before generating children. Older creator builds built
                // offspring first, so a father's selected OneHanded value
                // had no influence on their initial training.
                ApplyPlayerProgression(hero,profile);
                DiagnosticJournal.Event("cu_family_parent_stats_ready",
                    "player="+hero.StringId+
                    "|player_final_overrides="+profile.Overrides.Count+
                    "|before_family_creation=true");
                // A genuine younger-brother/sister start must bind to a
                // verified older native noble, with both original parents,
                // before any additional generated siblings are created.
                Hero elder;
                string dynasty=SandboxDynasticStart.TryBind(
                    profile,hero,out elder);
                if(elder!=null)
                {
                    if(SandboxNobleStartPolicy.IsSonOrigin(
                        profile.NobleBirthPosition))
                    {
                        var mother=hero.Mother;
                        var father=hero.Father;
                        var elderSons=father.Children.Where(h=>h!=null &&
                            h!=hero && h.IsAlive && !h.IsFemale &&
                            h.Mother==mother && h.Age>hero.Age)
                            .OrderByDescending(h=>h.Age).ToArray();
                        dynasticFatherHeroId=father.StringId;
                        dynasticMotherHeroId=mother.StringId;
                        // House has not yet created the older male
                        // siblings chosen in Advanced. A partial count
                        // here must not masquerade as final birth rank.
                        dynasticOlderSons=String.Join(",",
                            elderSons.Select(h=>h.StringId));
                        Notify("Noble parents linked: "+
                            father.Name.ToString()+" and "+
                            mother.Name.ToString()+
                            ". Birth order is checked after House.");
                        DiagnosticJournal.Event("cu_dynastic_son_verified",
                            "father="+dynasticFatherHeroId+
                            "|mother="+dynasticMotherHeroId+
                            "|older_sons="+dynasticOlderSons+
                            "|requested="+profile.NobleBirthPosition+
                            "|native_parentage=true");
                    }
                    else
                    {
                        dynasticElderHeroId=elder.StringId;
                        dynasticElderName=elder.Name.ToString();
                        Notify("Noble family confirmed: your older relative is "+
                            dynasticElderName+" of "+
                            elder.Clan.Name.ToString()+".");
                        DiagnosticJournal.Event("cu_dynastic_named_elder_confirmed",
                            "elder_id="+dynasticElderHeroId+
                            "|elder_name="+dynasticElderName+
                            "|clan="+elder.Clan.StringId+
                            "|native_parentage=true");
                    }
                }
                DiagnosticJournal.Event("cu_dynastic_start_status",
                    "requested="+profile.NobleBirthPosition+
                    "|result="+dynasty+
                    "|saved_elder="+dynasticElderHeroId+
                    "|saved_elder_name="+dynasticElderName+
                    "|saved_father="+dynasticFatherHeroId+
                    "|saved_older_brothers="+dynasticOlderSons);
                if(SandboxNobleStartPolicy.RequiresNobleElder(
                    profile.NobleBirthPosition) && elder==null)
                    Notify("The requested noble family could not be "+
                        "established with native relatives: "+dynasty);

                // Explicitly requested family is created after the native
                // character and start kingdom exist.
                string family = SandboxFamilyCreator.TryCreate(profile,hero);
                RegisterFamilyUpbringings(profile,hero,family);
                bool finalSonRankValid=true;
                if(SandboxNobleStartPolicy.IsSonOrigin(
                    profile.NobleBirthPosition) && elder!=null)
                {
                    var senior=hero.Father.Children.Where(h=>h!=null &&
                        h!=hero && h.IsAlive && !h.IsFemale &&
                        h.Mother==hero.Mother && h.Age>hero.Age)
                        .OrderByDescending(h=>h.Age)
                        .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                        .ToArray();
                    int required=SandboxNobleStartPolicy.RequiredOlderSons(
                        profile.NobleBirthPosition);
                    finalSonRankValid=
                        SandboxHouseholdLineagePolicy.ExactFinalRank(
                            required,senior.Length);
                    dynasticOlderSons=String.Join(",",
                        senior.Select(h=>h.StringId));
                    if(senior.Length>0)
                    {
                        dynasticElderHeroId=senior[0].StringId;
                        dynasticElderName=senior[0].Name.ToString();
                    }
                    DiagnosticJournal.Event("cu_dynastic_final_birth_rank",
                        "expected="+required+
                        "|actual="+senior.Length+
                        "|result="+(finalSonRankValid?"PASS":"FAIL")+
                        "|father="+dynasticFatherHeroId+
                        "|mother="+dynasticMotherHeroId+
                        "|older_ids="+dynasticOlderSons);
                    if(finalSonRankValid)
                        Notify("Noble birth verified: "+
                            hero.Father.Name.ToString()+" is your father; "+
                            String.Join(", ",senior.Select(
                                h=>h.Name.ToString()))+
                            " stand before you in the family.");
                }
                DiagnosticJournal.Event("cu_family_creation_status",
                    "requested=" + profile.RealFamilyEnabled + "|result=" + family);
                // Native spouse and parent links must be verified first.
                // Place the family at an OWNED castle/town only after the
                // genuine marriage and children have been created.
                string familyHome = family.StartsWith("spouse=",
                    StringComparison.Ordinal) ?
                    SandboxFamilyHome.TryPlace(profile,hero,fiefOutcome) :
                    "skipped:family_not_verified";
                DiagnosticJournal.Event("cu_family_home_status",
                    "result=" + familyHome +
                    "|requested_mode=" + profile.FamilyHomeMode +
                    "|requested_id=" + profile.FamilyHomeId);
                if ((profile.FamilyHomeMode=="SpecificOwned" &&
                    familyHome.StartsWith("skipped:",StringComparison.Ordinal)) ||
                    familyHome.StartsWith("failed:",StringComparison.Ordinal) ||
                    familyHome.StartsWith("refused:",StringComparison.Ordinal))
                    Notify("Family home could not be established: " +
                        familyHome);
                if (profile.RealFamilyEnabled && (
                    family == "not_enabled" ||
                    !family.StartsWith("spouse=",StringComparison.Ordinal)))
                    Notify("Family request could not be completed: " + family);
                if ((profile.Role == "Lord" || profile.Role == "King") &&
                    !SandboxRolePolicy.MatchesNativeStatus(profile.Role,
                        Clan.PlayerClan.Kingdom != null,
                        Clan.PlayerClan.Kingdom != null &&
                        Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan))
                    Notify("Start role was not applied: " + profile.Role);

                if (profile.Gold >= 0)
                {
                    int delta = profile.Gold - Math.Max(0, hero.Gold);
                    if (delta != 0)
                        GiveGoldAction.ApplyBetweenCharacters(null, hero, delta, false);
                }
                if (profile.Influence >= 0 && Clan.PlayerClan.Kingdom != null)
                {
                    // AddInfluenceWithKingdom uses the native clan influence flow.
                    hero.AddInfluenceWithKingdom(profile.Influence -
                        Clan.PlayerClan.Influence);
                }
                bool realRank = SandboxRolePolicy.ConfirmedNativeStanding(
                    profile.Role, affiliation) ||
                    SandboxRolePolicy.MatchesNativeStatus(profile.Role,
                        Clan.PlayerClan.Kingdom != null,
                        Clan.PlayerClan.Kingdom != null &&
                        Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan);
                int troops = profile.RoleAffiliationsEnabled && realRank
                    ? Math.Max(profile.Troops,
                        SandboxRolePolicy.BonusRecruits(profile.Role))
                    : profile.Troops;
                if (troops > 0 && MobileParty.MainParty != null &&
                    MobileParty.MainParty.MemberRoster != null &&
                    hero.Culture != null && hero.Culture.BasicTroop != null)
                    MobileParty.MainParty.MemberRoster.AddToCounts(
                        hero.Culture.BasicTroop, troops);
                if (profile.RoleAffiliationsEnabled && realRank)
                    Clan.PlayerClan.AddRenown(
                        SandboxRolePolicy.StarterRenown(profile.Role),false);
                else if (profile.RoleAffiliationsEnabled)
                    DiagnosticJournal.Event("cu_role_bonus_refused",
                        "role=" + profile.Role + "|affiliation_not_confirmed");

                // Role-based officers use same-culture native noble donors.
                // FamilyOnly removes native wanderers without adding
                // unrelated officers. Native preserves the original roster.
                string retinue=SandboxCulturalRetinue.TryCreate(profile,hero);
                DiagnosticJournal.Event("cu_cultural_retinue_status",
                    "role="+profile.Role+
                    "|mode="+profile.RetinueMode+
                    "|result="+retinue);
                if(retinue.StartsWith("refused:",StringComparison.Ordinal) ||
                   retinue.StartsWith("partial:",StringComparison.Ordinal))
                    Notify("Starting household officers: "+retinue);

                ApplyExistingItems(profile);

                // Acceptance is based on observed Bannerlord state, not a
                // serialized choice, click handler or successful compilation.
                var observedClan = Clan.PlayerClan;
                var observedKingdom = observedClan.Kingdom;
                string[] failures = SandboxStartContract.Validate(
                    SandboxNativeStartOptions.StartTypeForRole(profile.Role),
                    actualStart,
                    profile.StartingKingdomId,
                    observedKingdom == null ? "" : observedKingdom.StringId,
                    observedKingdom != null &&
                        observedKingdom.RulingClan == observedClan,
                    observedClan.Settlements == null ?
                        0 : observedClan.Settlements.Count,
                    profile.SpouseEnabled,
                    hero.Spouse != null,
                    profile.ChildCount,
                    hero.Children == null ? 0 : hero.Children.Count);
                bool spouseAgeMatches=!profile.SpouseEnabled ||
                    (hero.Spouse!=null &&
                     SandboxFamilyAgePolicy.MatchesRequestedAge(
                         hero.Spouse.Age,profile.SpouseAge));
                var unmatched=hero.Children==null ?
                    new Hero[0].ToList() :
                    hero.Children.Where(x=>x!=null).ToList();
                bool allChildAgesMatch=true;
                foreach(var expected in profile.Children.Take(
                    profile.ChildCount))
                {
                    var actual=unmatched.FirstOrDefault(x=>
                        x.Name.ToString()==expected.Name &&
                        x.IsFemale==(expected.Gender=="Female") &&
                        SandboxFamilyAgePolicy.MatchesRequestedAge(
                            x.Age,expected.Age));
                    if(actual==null)
                    {
                        allChildAgesMatch=false;
                        break;
                    }
                    unmatched.Remove(actual);
                }
                if(!spouseAgeMatches || !allChildAgesMatch)
                    failures=failures.Concat(new[]{
                        !spouseAgeMatches?"spouse_age_mismatch":"",
                        !allChildAgesMatch?"child_identity_or_age_mismatch":""
                    }).Where(x=>x.Length>0).ToArray();
                DiagnosticJournal.Event("cu_family_age_contract",
                    "spouse_requested="+profile.SpouseAge+
                    "|spouse_actual="+(hero.Spouse==null?
                        "missing":hero.Spouse.Age.ToString())+
                    "|spouse_age_match="+spouseAgeMatches+
                    "|requested_children="+profile.ChildCount+
                    "|actual_children="+(hero.Children==null?
                        0:hero.Children.Count)+
                    "|child_identities_ages_match="+allChildAgesMatch);
                // Verify exact generated siblings and native parentage,
                // not just the requested children or selected birth label.
                bool siblingsMatch=true;
                if(profile.SiblingCount>0)
                {
                    var remaining=hero.Father==null ||
                        hero.Father.Children==null?
                        new Hero[0].ToList():
                        hero.Father.Children.Where(h=>h!=null &&
                            h!=hero && h.Mother==hero.Mother).ToList();
                    foreach(var expectedSibling in profile.Siblings.Take(
                        profile.SiblingCount))
                    {
                        var actual=remaining.FirstOrDefault(h=>
                            h.Name.ToString()==expectedSibling.Name &&
                            h.IsFemale==(expectedSibling.Gender=="Female") &&
                            SandboxFamilyAgePolicy.MatchesRequestedAge(
                                h.Age,expectedSibling.Age));
                        if(actual==null)
                        {
                            siblingsMatch=false;
                            break;
                        }
                        remaining.Remove(actual);
                    }
                    if(!siblingsMatch ||
                        !family.Contains("|siblings="+profile.SiblingCount))
                        failures=failures.Concat(new[]{
                            "requested_native_siblings_missing"}).ToArray();
                }
                if(!finalSonRankValid)
                    failures=failures.Concat(new[]{
                        "noble_birth_rank_changed_after_house"}).ToArray();
                // A resource-only PASS must not mask the user's selected
                // lineage or lord-service start failing to materialize.
                if(SandboxNobleStartPolicy.RequiresNobleElder(
                    profile.NobleBirthPosition) &&
                    !dynasty.StartsWith("bound:",StringComparison.Ordinal))
                    failures=failures.Concat(new[]{
                        "native_noble_lineage_not_bound"}).ToArray();
                if(SandboxServicePolicy.NeedsPatron(profile.Role) &&
                    patron==null)
                    failures=failures.Concat(new[]{
                        "commanding_lord_not_bound"}).ToArray();
                if(profile.RetinueMode=="FamilyOnly" &&
                    Clan.PlayerClan.Companions.Count>0)
                    failures=failures.Concat(new[]{
                        "family_only_has_unrelated_companions"}).ToArray();
                DiagnosticJournal.Event("sandbox_start_contract",
                    "result=" + (failures.Length == 0 ? "PASS" : "FAIL") +
                    "|role=" + profile.Role +
                    "|native_type=" + actualStart +
                    "|kingdom=" + (observedKingdom == null ?
                        "none" : observedKingdom.StringId) +
                    "|fiefs=" + (observedClan.Settlements == null ?
                        0 : observedClan.Settlements.Count) +
                    "|fief_request=" + fiefOutcome +
                    "|spouse_present=" + (hero.Spouse != null) +
                    "|requested_children=" + profile.ChildCount +
                    "|children=" + (hero.Children == null ?
                        0 : hero.Children.Count) +
                    "|failures=" + String.Join(",",failures));
                if (failures.Length != 0)
                    Notify("Calradia Unified starting conditions failed: " +
                        String.Join(", ",failures));
                else
                {
                    // A successful, ACTUAL native campaign state is the
                    // sole authority for starting biography and service.
                    try
                    {
                        SandboxLivingPersons.Capture(
                            profile,hero,patron,livingPersons);
                        socialObservations=new SandboxSocialObservationState();
                        SandboxLivingPersons.ObserveDaily(livingPersons,
                            socialObservations,hero);
                        if(SandboxServicePolicy.NeedsPatron(profile.Role) &&
                            patron!=null && Clan.PlayerClan.Kingdom!=null)
                        {
                            serviceCareer.Start(profile.Role,
                                patron.StringId,
                                Clan.PlayerClan.Kingdom.StringId);
                            DiagnosticJournal.Event(
                                "cu_personal_service_career_started",
                                "role="+profile.Role+
                                "|patron="+patron.StringId+
                                "|native_wages_authoritative=true"+
                                "|enlisted_in_npc_party=false"+
                                "|automatic_promotion=false");
                        }
                        if(profile.Role=="Lord" &&
                            SandboxNobleStartPolicy.RequiresNobleElder(
                                profile.NobleBirthPosition) &&
                            hero.Father!=null)
                        {
                            int preceding=hero.Father.Children.Count(x=>
                                x!=null && x!=hero && !x.IsFemale &&
                                x.Age>hero.Age &&
                                x.Mother==hero.Mother);
                            successionInterest.Record(hero.StringId,
                                hero.Father.StringId,
                                hero.Mother==null?"":
                                    hero.Mother.StringId,
                                hero.Father.Clan==null?"":
                                    hero.Father.Clan.StringId,
                                preceding,hero.Father.IsAlive);
                            DiagnosticJournal.Event(
                                "cu_native_succession_interest_recorded",
                                "claimant="+hero.StringId+
                                "|father="+hero.Father.StringId+
                                "|preceding_sons="+preceding+
                                "|native_fiefs_transferred=0"+
                                "|legal_right_not_assumed=true");
                        }
                    }
                    catch(Exception historyError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_living_persons_capture_failed",historyError);
                    }
                }

                // Relocation and direct face sliders remain disabled. The
                // opt-in family helper uses native hero/marriage APIs.
                DiagnosticJournal.Event("sandbox_origin_applied",
                    "role=" + profile.Role +
                    "|family_home=" + familyHome +
                    "|gold_target=" + profile.Gold +
                    "|influence_target=" + profile.Influence +
                    "|troops=" + profile.Troops +
                    "|family_status=" + family +
                    "|start_contract_passed=" + (failures.Length == 0) +
                    "|appearance_template_lookup_only=true" +
                    "|relocation_pending=true" +
                    "|progression_mode=" + profile.ProgressionMode +
                    "|progression_requested=" + profile.Overrides.Count +
                    "|individual_items_requested=" + profile.ItemIds.Count);
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("sandbox_origin_new_game", ex);
            }
        }

        private static void ApplyPlayerProgression(Hero hero,
            SandboxOriginProfile profile)
        {
            // Default is a strict no-op. A leftover profile file must not
            // silently alter native life-story rewards in a fresh Sandbox.
            if (profile.ProgressionMode=="Native")
            {
                DiagnosticJournal.Event("cu_progression_native_preserved",
                    "overrides_applied=0");
                return;
            }

            // Re-evaluate against the FINAL native Hero after all narrative
            // and character-creation effects. Reject the WHOLE invalid plan,
            // never partially spend points or grant unrestricted advantages.
            var baseline=CUNativeStatsSnapshot.Capture(hero);
            int spent;
            string error;
            if (!SandboxStatBudgetPolicy.TryEvaluate(
                profile.ProgressionMode,profile.Overrides,baseline,
                out spent,out error))
            {
                DiagnosticJournal.Event("cu_progression_plan_rejected",
                    "mode="+profile.ProgressionMode+"|reason="+error);
                return;
            }
            DiagnosticJournal.Event("cu_progression_plan_accepted",
                "mode="+profile.ProgressionMode+"|spent="+spent+
                "|budget="+SandboxStatBudgetPolicy.StartingBudget+
                "|changes="+profile.Overrides.Count);
            foreach (var entry in profile.Overrides)
            {
                string[] parts = entry.Key.Split('.');
                if (parts.Length != 2) continue;
                int value = entry.Value;
                try
                {
                    if (parts[0] == "Skill" || parts[0] == "Focus")
                    {
                        // War Sails skills live in the installed game's
                        // SkillObject catalogue, not in older DefaultSkills.
                        var skill=SandboxNativeSkillCatalog.Find(parts[1]);
                        if(skill==null)
                        {
                            DiagnosticJournal.Event(
                                "cu_progression_skill_not_registered",
                                "requested="+entry.Key+
                                "|missing_optional_dlc=true");
                            continue;
                        }
                        if(hero.HeroDeveloper==null)continue;
                        if (parts[0] == "Skill")
                            hero.HeroDeveloper.SetInitialSkillLevel(skill, value);
                        else
                        {
                            int delta = value - hero.HeroDeveloper.GetFocus(skill);
                            if (delta != 0) hero.HeroDeveloper.AddFocus(skill, delta, false);
                        }
                    }
                    else if(parts[0]=="Level" && parts[1]=="Character")
                    {
                        if(!SandboxFamilyLevelPolicy.Valid(value) ||
                            hero.HeroDeveloper==null)
                            throw new InvalidOperationException(
                                "Invalid native player level");
                        // Set both saveable native Hero.Level and matching
                        // XP threshold. SetInitialLevel alone changes XP,
                        // not the Level field in this engine version.
                        hero.Level=value;
                        hero.HeroDeveloper.SetInitialLevel(value);
                        DiagnosticJournal.Event("cu_player_level_verified",
                            "requested="+value+
                            "|actual="+hero.Level+
                            "|required_xp="+
                                hero.HeroDeveloper.GetXpRequiredForLevel(value)+
                            "|native_total_xp="+hero.HeroDeveloper.TotalXp);
                    }
                    else if (parts[0] == "Attribute")
                    {
                        var attrInfo = typeof(DefaultCharacterAttributes).GetProperty(
                            parts[1], BindingFlags.Public | BindingFlags.Static);
                        var attribute = attrInfo == null ? null :
                            attrInfo.GetValue(null, null) as CharacterAttribute;
                        if (attribute == null || hero.HeroDeveloper == null) continue;
                        int delta = value - hero.GetAttributeValue(attribute);
                        if (delta != 0)
                            hero.HeroDeveloper.AddAttribute(attribute, delta, false);
                    }
                    else if (parts[0] == "Trait")
                    {
                        var traitInfo = typeof(DefaultTraits).GetProperty(
                            parts[1], BindingFlags.Public | BindingFlags.Static);
                        var trait = traitInfo == null ? null :
                            traitInfo.GetValue(null, null) as TraitObject;
                        if (trait != null) hero.SetTraitLevel(trait, value);
                    }
                    DiagnosticJournal.Event("sandbox_origin_character_override",
                        "key=" + entry.Key + "|target=" + value);
                }
                catch (Exception ex)
                {
                    DiagnosticJournal.Exception("sandbox_origin_override_" +
                        entry.Key, ex);
                }
            }
        }

        private static void ApplyExistingItems(SandboxOriginProfile profile)
        {
            var party = MobileParty.MainParty;
            if (party == null || party.ItemRoster == null) return;
            foreach (var pair in profile.ItemIds)
            {
                try
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(
                        pair.Value);
                    if (item == null)
                    {
                        DiagnosticJournal.Event("sandbox_origin_item_missing",
                            "slot=" + pair.Key + "|id=" + pair.Value);
                        continue;
                    }
                    party.ItemRoster.AddToCounts(item, 1);
                    DiagnosticJournal.Event("sandbox_origin_item_granted",
                        "slot=" + pair.Key + "|id=" + pair.Value);
                }
                catch (Exception ex)
                {
                    DiagnosticJournal.Exception("sandbox_origin_item_" +
                        pair.Key, ex);
                }
            }
        }
    }
}
