using System;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Optional native Gauntlet UI. The campaign-menu control panel remains a
    /// fully functional fallback if this exact v1.5.7 prefab fails to load.
    /// </summary>
    public sealed class NativeSettingsPanel : ScreenBase
    {
        private readonly Settings settings;
        private GauntletLayer layer;
        private NativeSettingsPanelVM dataSource;

        public NativeSettingsPanel(Settings settings) { this.settings = settings; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            dataSource = new NativeSettingsPanelVM(settings,
                () => ScreenManager.PopScreen());
            layer = new GauntletLayer(100) { IsFocusLayer = true };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUSettingsPanel", dataSource);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (layer != null) ScreenManager.TrySetFocus(layer);
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
            dataSource = null;
        }
    }

    public sealed class NativeSettingsPanelVM : ViewModel
    {
        private readonly Settings settings;
        private readonly Action close;
        private SettingDescriptor[] choices;
        private int currentIndex;
        private int pending;
        private string tab;

        public NativeSettingsPanelVM(Settings settings, Action close)
        {
            this.settings = settings;
            this.close = close;
            SetTab("Core");
        }

        private SettingDescriptor Current
        {
            get { return choices != null && choices.Length > currentIndex
                ? choices[currentIndex] : null; }
        }

        private void SetTab(string target)
        {
            tab = target;
            choices = SettingsCatalog.All.Where(x =>
                target == "Core"
                    ? x.Group == "general" || x.Group == "diagnostics" ||
                        x.Group == "supply"
                    : target == "Kingdom"
                        ? x.Group == "economy" || x.Group == "recruitment" ||
                            x.Group == "clan" || x.Group == "kingdom" ||
                            x.Group == "convoys"
                        : x.Group == "castles").ToArray();
            currentIndex = 0;
            Refresh();
        }

        private void Refresh()
        {
            int number;
            pending = Int32.TryParse(SettingsCatalog.Read(settings, Current),
                out number) ? number : 0;
            OnPropertyChangedWithValue(TabName, "TabName");
            OnPropertyChangedWithValue(SettingName, "SettingName");
            OnPropertyChangedWithValue(SettingValue, "SettingValue");
            OnPropertyChangedWithValue(Minimum, "Minimum");
            OnPropertyChangedWithValue(Maximum, "Maximum");
            OnPropertyChangedWithValue(pending, "PendingValue");
            OnPropertyChangedWithValue(IsNumeric, "IsNumeric");
            OnPropertyChangedWithValue(IsBoolean, "IsBoolean");
            OnPropertyChangedWithValue(Status, "Status");
        }

        [DataSourceProperty]
        public string TabName { get { return tab ?? ""; } }

        [DataSourceProperty]
        public string SettingName
        {
            get { return Current == null ? "No settings" :
                (Current.Experimental ? "[EXPERIMENTAL] " : "") + Current.Label; }
        }

        [DataSourceProperty]
        public string SettingValue
        {
            get { return Current == null ? "" :
                Current.Boolean ? SettingsCatalog.Read(settings, Current) :
                pending.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        }

        [DataSourceProperty]
        public int Minimum { get { return Current == null ? 0 : Current.Min; } }

        [DataSourceProperty]
        public int Maximum { get { return Current == null ? 0 : Current.Max; } }

        [DataSourceProperty]
        public int PendingValue
        {
            get { return pending; }
            set
            {
                if (Current == null || Current.Boolean) return;
                int bounded = Math.Max(Current.Min, Math.Min(Current.Max, value));
                if (bounded == pending) return;
                pending = bounded;
                OnPropertyChangedWithValue(pending, "PendingValue");
                OnPropertyChangedWithValue(SettingValue, "SettingValue");
            }
        }

        [DataSourceProperty]
        public bool IsNumeric { get { return Current != null && !Current.Boolean; } }

        [DataSourceProperty]
        public bool IsBoolean { get { return Current != null && Current.Boolean; } }

        [DataSourceProperty]
        public string Status { get; private set; }

        public void ExecuteCoreTab() { SetTab("Core"); }
        public void ExecuteKingdomTab() { SetTab("Kingdom"); }
        public void ExecuteCastleTab() { SetTab("Castles"); }
        public void ExecuteNextSetting()
        {
            if (choices == null || choices.Length == 0) return;
            currentIndex = (currentIndex + 1) % choices.Length;
            Refresh();
        }
        public void ExecutePreviousSetting()
        {
            if (choices == null || choices.Length == 0) return;
            currentIndex = (currentIndex + choices.Length - 1) % choices.Length;
            Refresh();
        }
        public void ExecuteIncrease()
        {
            if (!IsNumeric) return;
            PendingValue = SettingsCatalog.Next(pending, Current, 1, 1);
        }
        public void ExecuteDecrease()
        {
            if (!IsNumeric) return;
            PendingValue = SettingsCatalog.Next(pending, Current, -1, 1);
        }
        public void ExecuteApply()
        {
            if (!IsNumeric || Current == null) return;
            Save(pending.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        public void ExecuteToggle()
        {
            if (!IsBoolean || Current == null) return;
            Save(SettingsCatalog.Read(settings, Current) == "true"
                ? "false" : "true");
        }
        private void Save(string value)
        {
            string error;
            if (settings.TrySetMenuValue(Current.Key, value, out error))
            {
                Status = "Saved " + Current.Key + " = " + value;
                DiagnosticJournal.Event("gauntlet_settings_change",
                    Current.Key + "=" + value);
            }
            else Status = error;
            Refresh();
        }
        public void ExecuteClose()
        {
            DiagnosticJournal.Event("gauntlet_settings_close", "user_closed");
            if (close != null) close();
        }
    }
}
