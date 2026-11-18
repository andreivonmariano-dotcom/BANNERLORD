using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

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
            if (starter == null) return;
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
                "Begin alone", args => Editing.SpouseEnabled &&
                    Editing.ChildCount == 0,
                args => Update("SpouseEnabled", "false"), false);
            for (int count = 0; count <= 6; count++)
            {
                string n = count.ToString();
                int value = count;
                starter.AddGameMenuOption("cu_origin_family", "cu_children_" + n,
                    "Choose " + n + " child templates",
                    args => Editing.SpouseEnabled && Editing.ChildCount != value,
                    args => Update("ChildCount", n), false);
            }
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
            AddBack(starter, "cu_origin_location");
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

        private void NewGameCreated(CampaignGameStarter starter)
        {
            if (applied) return;
            // OnGameLoaded does not execute this event. The save key provides
            // a second barrier against reapplying wealth or changing old saves.
            applied = true;
            startedNewGame = true;
            SandboxOriginProfile profile = SandboxOriginProfile.Load(
                SandboxOriginProfile.UserPath);
            if (!profile.Enabled) return;
            var hero = Hero.MainHero;
            if (hero == null || Clan.PlayerClan == null) return;
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
                if (profile.Role == "Lord" && Clan.PlayerClan.Kingdom == null)
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

                // Save-safe world mutation under review: attributes, family,
                // appearance, equipment/relocation only stored in profile for
                // now. Do not create invalid Hero family links or map coordinates.
                DiagnosticJournal.Event("sandbox_origin_applied",
                    "role=" + profile.Role + "|gold_target=" + profile.Gold +
                    "|influence_target=" + profile.Influence +
                    "|troops=" + profile.Troops +
                    "|family_templates_only=true" +
                    "|appearance_templates_only=true" +
                    "|relocation_pending=true" +
                    "|progression_pending=true" +
                    "|equipment_pending=true");
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("sandbox_origin_new_game", ex);
            }
        }
    }
}
