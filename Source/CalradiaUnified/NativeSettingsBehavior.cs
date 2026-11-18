using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

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
                "Calradia Unified settings | Select a category.",
                args => { });
            foreach (string name in new[] { "town", "castle" })
            {
                string source = name;
                starter.AddGameMenuOption(source, "cu_open_settings_" + name,
                    "CU: Configure Calradia Unified",
                    args => true,
                    args =>
                    {
                        returnMenu = source;
                        DiagnosticJournal.Event("settings_menu_open", "source=" + source);
                        GameMenu.SwitchToMenu("cu_settings_main");
                    }, false);
            }

            foreach (var group in SettingsCatalog.Groups)
            {
                string category = group;
                string menu = "cu_settings_group_" + category;
                starter.AddGameMenu(menu,
                    SettingsCatalog.GroupLabel(category) +
                    " | Choose a setting to view and change.",
                    args => { });
                starter.AddGameMenuOption("cu_settings_main",
                    "cu_settings_go_" + category,
                    SettingsCatalog.GroupLabel(category),
                    args => true, args => GameMenu.SwitchToMenu(menu), false);

                foreach (var item in SettingsCatalog.All)
                {
                    if (item.Group != category) continue;
                    var option = item;
                    string detail = "cu_setting_" + option.Key.ToLowerInvariant();
                    starter.AddGameMenuOption(menu, "cu_settings_entry_" + option.Key,
                        (option.Experimental ? "[EXPERIMENTAL] " : "") + option.Label,
                        args => true,
                        args => GameMenu.SwitchToMenu(detail), false);
                    starter.AddGameMenu(detail,
                        option.Label + " | " + (option.Experimental ?
                            "Experimental, use a disposable campaign. " :
                            "Current setting. ") +
                        "Changes are saved to the user profile.",
                        args => { });
                    RegisterDetail(starter, detail, menu, option);
                }
                starter.AddGameMenuOption(menu, "cu_settings_back_" + category,
                    "Back to categories",
                    args => true,
                    args => GameMenu.SwitchToMenu("cu_settings_main"), false);
            }

            starter.AddGameMenuOption("cu_settings_main", "cu_settings_advanced_panel",
                "EXPERIMENTAL: Open native tabbed slider panel",
                args => true, args => OpenExperimentalPanel(), false);
            starter.AddGameMenuOption("cu_settings_main", "cu_settings_profile",
                "Show profile settings location", args => true,
                args => Notify(Settings.UserOverridePath), false);
            starter.AddGameMenuOption("cu_settings_main", "cu_settings_close",
                "Return to settlement", args => true, args =>
                {
                    DiagnosticJournal.Event("settings_menu_close",
                        "return=" + returnMenu);
                    GameMenu.SwitchToMenu(returnMenu);
                }, false);
            DiagnosticJournal.Event("settings_menu_registered",
                "definitions=" + SettingsCatalog.All.Count);
        }

        private void RegisterDetail(CampaignGameStarter starter, string detail,
            string parent, SettingDescriptor option)
        {
            starter.AddGameMenuOption(detail, "cu_current_" + option.Key,
                "Show current value", args => true,
                args => Notify(option.Key + " = " +
                    SettingsCatalog.Read(settings, option)), false);
            if (option.Boolean)
            {
                starter.AddGameMenuOption(detail, "cu_enable_" + option.Key,
                    option.Experimental ? "Enable experimental feature" : "Enable",
                    args => SettingsCatalog.Read(settings, option) == "false",
                    args => Change(option.Key, "true", detail), false);
                starter.AddGameMenuOption(detail, "cu_disable_" + option.Key,
                    "Disable", args =>
                        SettingsCatalog.Read(settings, option) == "true",
                    args => Change(option.Key, "false", detail), false);
            }
            else
            {
                starter.AddGameMenuOption(detail, "cu_decrease_" + option.Key,
                    "Decrease by " + option.Step,
                    args => CurrentNumber(option) > option.Min,
                    args => Nudge(option, -1, 1, detail), false);
                starter.AddGameMenuOption(detail, "cu_increase_" + option.Key,
                    "Increase by " + option.Step,
                    args => CurrentNumber(option) < option.Max,
                    args => Nudge(option, 1, 1, detail), false);
                starter.AddGameMenuOption(detail, "cu_decrease10_" + option.Key,
                    "Decrease by " + (option.Step * 10),
                    args => CurrentNumber(option) > option.Min,
                    args => Nudge(option, -1, 10, detail), false);
                starter.AddGameMenuOption(detail, "cu_increase10_" + option.Key,
                    "Increase by " + (option.Step * 10),
                    args => CurrentNumber(option) < option.Max,
                    args => Nudge(option, 1, 10, detail), false);
                starter.AddGameMenuOption(detail, "cu_range_" + option.Key,
                    "Show allowed minimum and maximum",
                    args => true,
                    args => Notify(option.Label + " range: " +
                        option.Min + " to " + option.Max), false);
            }
            starter.AddGameMenuOption(detail, "cu_detail_back_" + option.Key,
                "Back to " + SettingsCatalog.GroupLabel(option.Group),
                args => true,
                args => GameMenu.SwitchToMenu(parent), false);
        }

        private int CurrentNumber(SettingDescriptor option)
        {
            int value;
            return Int32.TryParse(SettingsCatalog.Read(settings, option),
                out value) ? value : option.Min;
        }

        private void Nudge(SettingDescriptor option, int direction, int factor,
            string menu)
        {
            int value = SettingsCatalog.Next(CurrentNumber(option),
                option, direction, factor);
            Change(option.Key, value.ToString(
                System.Globalization.CultureInfo.InvariantCulture), menu);
        }

        private void OpenExperimentalPanel()
        {
            try
            {
                ScreenManager.PushScreen(new NativeSettingsPanel(settings));
                DiagnosticJournal.Event("gauntlet_settings_push", "screen_requested");
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("gauntlet_settings_failed", ex);
                Notify("Tabbed panel could not open; use the native category menus.");
            }
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
