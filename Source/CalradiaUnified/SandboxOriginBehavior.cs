using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

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
            DiagnosticJournal.Event("sandbox_origin_new_game_armed",
                "waiting_for_native_main_hero");
            TryApplyOnce();
        }

        private void TryApplyOnDailyTick()
        {
            if (startedNewGame && !applied) TryApplyOnce();
        }

        private void TryApplyOnce()
        {
            if (!startedNewGame || applied || !IsStandaloneSandbox()) return;
            var hero = Hero.MainHero;
            if (hero == null || Clan.PlayerClan == null) return;

            // Commit the one-shot barrier before any resources are changed.
            // Save/load or partial action failure must not duplicate grants.
            applied = true;
            startedNewGame = false;
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
                if (profile.Troops > 0 && MobileParty.MainParty != null &&
                    MobileParty.MainParty.MemberRoster != null &&
                    hero.Culture != null && hero.Culture.BasicTroop != null)
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(
                        hero.Culture.BasicTroop, profile.Troops);
                }

                ApplyPlayerProgression(hero, profile);
                ApplyExistingItems(profile);

                // Family, appearance and relocation remain templates. Never
                // create a Hero with missing parentage or bypass an active siege.
                DiagnosticJournal.Event("sandbox_origin_applied",
                    "role=" + profile.Role + "|gold_target=" + profile.Gold +
                    "|influence_target=" + profile.Influence +
                    "|troops=" + profile.Troops +
                    "|family_templates_only=true" +
                    "|appearance_templates_only=true" +
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
