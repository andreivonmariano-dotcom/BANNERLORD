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
        private CharacterCreationManager observedNativeCreator;
        private SandboxOriginProfile editing;
        private string returnMenu = "town";

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, RegisterMenus);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(
                this, NewGameCreated);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                this, TryApplyOnDailyTick);
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
                    "|requested_children=" + profile.ChildCount +
                    "|details=" + String.Join(";",
                        profile.Children.Take(profile.ChildCount).Select(
                            c => c.Name + ":" + c.Gender + ":" + c.Age)));
                // Explicitly requested family is created after the native
                // character and start kingdom exist.
                string family = SandboxFamilyCreator.TryCreate(profile,hero);
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

                ApplyPlayerProgression(hero, profile);
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
            foreach (var entry in profile.Overrides)
            {
                string[] parts = entry.Key.Split('.');
                if (parts.Length != 2) continue;
                int value = entry.Value;
                try
                {
                    if (parts[0] == "Skill" || parts[0] == "Focus")
                    {
                        var skillInfo = typeof(DefaultSkills).GetProperty(
                            parts[1], BindingFlags.Public | BindingFlags.Static);
                        var skill = skillInfo == null ? null :
                            skillInfo.GetValue(null, null) as SkillObject;
                        if (skill == null || hero.HeroDeveloper == null) continue;
                        if (parts[0] == "Skill")
                            hero.HeroDeveloper.SetInitialSkillLevel(skill, value);
                        else
                        {
                            int delta = value - hero.HeroDeveloper.GetFocus(skill);
                            if (delta != 0) hero.HeroDeveloper.AddFocus(skill, delta, false);
                        }
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
