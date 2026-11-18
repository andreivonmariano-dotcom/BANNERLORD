using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// In-game native campaign menus provide a small MCM-like controls page.
    /// No Harmony, UIExtenderEx, ButterLib or MCM assembly is loaded or patched.
    /// </summary>
    public sealed class NativeSettingsBehavior : CampaignBehaviorBase
    {
        private readonly Settings settings;
        private string returnMenu = "town";

        public NativeSettingsBehavior(Settings settings) { this.settings = settings; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, RegisterMenus);
        }

        public override void SyncData(IDataStore store)
        {
            // Settings belong to a user profile, not a campaign save.
            // Existing civic and party save keys remain unchanged.
        }

        private static void Notify(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "[Calradia Unified] " + message));
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            if (starter == null) return;
            starter.AddGameMenu("cu_settings_main",
                "Calradia Unified | Settings\nChanges persist in your user profile.",
                args => { });
            starter.AddGameMenu("cu_settings_reports",
                "Reports and supply warnings. Changes apply immediately.",
                args => { });
            starter.AddGameMenu("cu_settings_economy",
                "Economy and recruitment. EXPERIMENTAL, no game testing yet.",
                args => { });
            starter.AddGameMenu("cu_settings_castles",
                "Castle management. EXPERIMENTAL, no game testing yet.",
                args => { });

            foreach (string name in new[] { "town", "castle" })
            {
                string source = name;
                starter.AddGameMenuOption(name, "cu_open_settings_" + name,
                    "CU: Configure Calradia Unified",
                    args => true,
                    args => { returnMenu = source;
                        GameMenu.SwitchToMenu("cu_settings_main"); }, false);
            }
            AddGo(starter, "cu_settings_main", "reports", "Reports and warnings",
                "cu_settings_reports");
            AddGo(starter, "cu_settings_main", "economy", "Economy and recruitment",
                "cu_settings_economy");
            AddGo(starter, "cu_settings_main", "castles", "Castle management",
                "cu_settings_castles");
            starter.AddGameMenuOption("cu_settings_main", "cu_config_location",
                "Show my settings file location", args => true,
                args => Notify("Your profile overrides are stored at " +
                    Settings.UserOverridePath), false);
            starter.AddGameMenuOption("cu_settings_main", "cu_config_exit",
                "Return to settlement",
                args => true, args => GameMenu.SwitchToMenu(returnMenu), true);

            AddToggle(starter, "cu_settings_reports", "supplies",
                "Supply warnings", "EnableSupplyWarnings",
                () => settings.EnableSupplyWarnings);
            AddToggle(starter, "cu_settings_reports", "army",
                "Army food warnings", "EnableArmyWarnings",
                () => settings.EnableArmyWarnings);
            AddToggle(starter, "cu_settings_reports", "npc",
                "Read-only clan advice", "EnableNpcAdvisory",
                () => settings.EnableNpcAdvisory);
            AddNumber(starter, "cu_settings_reports", "supply_days",
                "Supply warning threshold (days)", "SupplyWarningDays",
                () => settings.SupplyWarningDays, 0, 15, 1);
            AddBack(starter, "cu_settings_reports");

            AddToggle(starter, "cu_settings_economy", "depots",
                "EXPERIMENTAL depot deposits and withdrawals",
                "EnableCivicSystems", () => settings.EnableCivicSystems);
            AddToggle(starter, "cu_settings_economy", "production",
                "EXPERIMENTAL depot production", "EnableEconomy",
                () => settings.EnableEconomy);
            AddToggle(starter, "cu_settings_economy", "recruits",
                "EXPERIMENTAL native-culture recruitment",
                "EnableRecruitment", () => settings.EnableRecruitment);
            AddToggle(starter, "cu_settings_economy", "trading",
                "EXPERIMENTAL depot trading", "EnableDepotTrade",
                () => settings.EnableDepotTrade);
            AddNumber(starter, "cu_settings_economy", "recruits_per_day",
                "Maximum local recruits per settlement per day",
                "LocalRecruitDailyCap", () => settings.LocalRecruitDailyCap,
                1, 20, 1);
            AddBack(starter, "cu_settings_economy");

            AddToggle(starter, "cu_settings_castles", "transfers",
                "EXPERIMENTAL existing garrison troop transfers",
                "EnableManualGarrisonTransfers",
                () => settings.EnableManualGarrisonTransfers);
            AddToggle(starter, "cu_settings_castles", "wagecap",
                "EXPERIMENTAL manual castle wage caps",
                "EnableManualGarrisonWageCaps",
                () => settings.EnableManualGarrisonWageCaps);
            AddToggle(starter, "cu_settings_castles", "training",
                "EXPERIMENTAL grain-funded garrison training",
                "EnableGarrisonTraining",
                () => settings.EnableGarrisonTraining);
            AddBack(starter, "cu_settings_castles");
        }

        private void AddGo(CampaignGameStarter starter, string menu,
            string id, string label, string next)
        {
            starter.AddGameMenuOption(menu, "cu_settings_go_" + id,
                label, args => true,
                args => GameMenu.SwitchToMenu(next), false);
        }

        private void AddBack(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, menu + "_back",
                "Back to settings categories", args => true,
                args => GameMenu.SwitchToMenu("cu_settings_main"), true);
        }

        private void AddToggle(CampaignGameStarter starter, string menu,
            string id, string label, string key, Func<bool> current)
        {
            starter.AddGameMenuOption(menu, "cu_s_" + id + "_enable",
                "Enable " + label,
                args => !current(),
                args => Change(key, "true", menu), false);
            starter.AddGameMenuOption(menu, "cu_s_" + id + "_disable",
                "Disable " + label,
                args => current(),
                args => Change(key, "false", menu), false);
        }

        private void AddNumber(CampaignGameStarter starter, string menu,
            string id, string label, string key, Func<int> current,
            int min, int max, int increment)
        {
            starter.AddGameMenuOption(menu, "cu_s_" + id + "_show",
                "Show current " + label,
                args => true,
                args => Notify(label + ": " + current()), false);
            starter.AddGameMenuOption(menu, "cu_s_" + id + "_plus",
                "Increase " + label + " by " + increment,
                args => current() <= max - increment,
                args => Change(key, (current() + increment).ToString(), menu),
                false);
            starter.AddGameMenuOption(menu, "cu_s_" + id + "_minus",
                "Decrease " + label + " by " + increment,
                args => current() >= min + increment,
                args => Change(key, (current() - increment).ToString(), menu),
                false);
        }

        private void Change(string key, string value, string menu)
        {
            string error;
            if (!settings.TrySetMenuValue(key, value, out error))
            {
                Notify(error);
                return;
            }
            DiagnosticJournal.Event("native_settings_change",
                "key=" + key + "|value=" + value);
            Notify(key + " set to " + value + ". Changes saved to your profile.");
            GameMenu.SwitchToMenu(menu);
        }
    }
}
