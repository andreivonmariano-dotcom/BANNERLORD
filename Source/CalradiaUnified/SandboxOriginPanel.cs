using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Experimental pre-Sandbox editor, accessible from the main menu.
    /// All changes are only to an external profile. It never starts or
    /// mutates a running game and has no dependency on third-party UI DLLs.
    /// </summary>
    public sealed class SandboxOriginPanel : ScreenBase
    {
        private GauntletLayer layer;
        private SandboxOriginPanelVM vm;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            vm = new SandboxOriginPanelVM(() => ScreenManager.PopScreen());
            layer = new GauntletLayer("CalradiaUnifiedOrigin", 101, false)
            {
                IsFocusLayer = true
            };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUOriginPanel", vm);
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
            vm = null;
        }
    }

    public sealed class SandboxOriginPanelVM : ViewModel
    {
        private sealed class Field
        {
            internal readonly string Key;
            internal readonly string Name;
            internal readonly string Section;
            internal readonly int Min, Max, Step;
            internal readonly string[] Choices;
            internal readonly bool Boolean, Text;
            internal Field(string section, string key, string name,
                int min = 0, int max = 0, int step = 1,
                string[] choices = null, bool boolean = false, bool text = false)
            {
                Section = section;
                Key = key;
                Name = name;
                Min = min;
                Max = max;
                Step = step;
                Choices = choices;
                Boolean = boolean;
                Text = text;
            }
        }

        private readonly Action close;
        private readonly List<Field> fields = BuildFields();
        private SandboxOriginProfile profile;
        private string section = "Identity";
        private int index, pending;

        public SandboxOriginPanelVM(Action close)
        {
            this.close = close;
            profile = SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            Refresh();
        }

        private static List<Field> BuildFields()
        {
            var all = new List<Field>
            {
                new Field("Identity", "Enabled", "Activate for next NEW Sandbox",
                    boolean: true),
                new Field("Identity", "Role", "Starting background",
                    choices: SandboxOriginProfile.Roles),
                new Field("Identity", "Gold", "Gold target (-1 keeps vanilla)",
                    min: 0, max: 2000000, step: 1000),
                new Field("Identity", "Influence", "Clan influence (-1 keeps vanilla)",
                    min: 0, max: 100000, step: 100),
                new Field("Identity", "Troops", "Additional culture basic troops",
                    min: 0, max: 50, step: 1),
                new Field("Identity", "PlayerAge", "Age template (-1 keeps vanilla)",
                    min: 18, max: 80, step: 1),
                new Field("Family", "SpouseEnabled", "Spouse template present",
                    boolean: true),
                new Field("Family", "SpouseName", "Spouse name", text: true),
                new Field("Family", "SpouseAge", "Spouse age",
                    min: 18, max: 70, step: 1),
                new Field("Family", "SpouseGender", "Spouse gender",
                    choices: new[] { "Female", "Male" }),
                new Field("Family", "SpouseCulture", "Spouse culture ID", text: true),
                new Field("Family", "SpouseFaceSeed", "Spouse face seed", text: true),
                new Field("Family", "ChildCount", "Starting children (templates)",
                    min: 0, max: 6, step: 1),
                new Field("Family", "SiblingCount", "Starting siblings (templates)",
                    min: 0, max: 4, step: 1),
                new Field("Location", "StartingTown", "Preferred starting town StringId",
                    text: true),
                new Field("Equipment", "EquipmentKit", "Personal equipment template",
                    choices: SandboxOriginProfile.Kits),
                new Field("Equipment", "SpouseEquipmentKit", "Spouse equipment template",
                    choices: SandboxOriginProfile.Kits),
            };
            for (int i = 1; i <= SandboxOriginProfile.MaxChildren; i++)
            {
                string prefix = "Child." + i + ".";
                all.Add(new Field("Family", prefix + "Name", "Child " + i + " name", text:true));
                all.Add(new Field("Family", prefix + "Age", "Child " + i + " age",
                    min: 0, max: 55));
                all.Add(new Field("Family", prefix + "Gender", "Child " + i + " gender",
                    choices: new[] {"Female", "Male"}));
                all.Add(new Field("Family", prefix + "FaceSeed",
                    "Child " + i + " face seed", text:true));
            }
            for (int i = 1; i <= SandboxOriginProfile.MaxSiblings; i++)
            {
                string prefix = "Sibling." + i + ".";
                all.Add(new Field("Family", prefix + "Name", "Sibling " + i + " name", text:true));
                all.Add(new Field("Family", prefix + "Age", "Sibling " + i + " age",
                    min:18, max:80));
                all.Add(new Field("Family", prefix + "Gender", "Sibling " + i + " gender",
                    choices:new[]{"Female","Male"}));
            }
            foreach (string attr in SandboxOriginProfile.Attributes)
                all.Add(new Field("Progression", "Attribute." + attr,
                    "Attribute: " + attr, min:1,max:10));
            foreach (string skill in SandboxOriginProfile.Skills)
            {
                all.Add(new Field("Progression", "Skill." + skill,
                    "Skill: " + skill, min:0,max:330,step:5));
                all.Add(new Field("Progression", "Focus." + skill,
                    "Focus: " + skill, min:0,max:5));
            }
            foreach (string trait in SandboxOriginProfile.Traits)
                all.Add(new Field("Progression", "Trait." + trait,
                    "Personality: " + trait, min:-2,max:2));
            for (int slot = 1; slot <= 12; slot++)
                all.Add(new Field("Equipment", "Item." + slot,
                    "Extra item ID slot " + slot, text:true));
            return all;
        }

        private Field Current
        {
            get
            {
                var options = fields.Where(f => f.Section == section).ToArray();
                return options.Length == 0 ? null : options[Math.Min(index,options.Length - 1)];
            }
        }

        private string GetValue(string key)
        {
            if (key == "Role") return profile.Role;
            if (key == "Enabled") return profile.Enabled ? "true" : "false";
            if (key == "Gold") return profile.Gold.ToString(CultureInfo.InvariantCulture);
            if (key == "Influence") return profile.Influence.ToString(CultureInfo.InvariantCulture);
            if (key == "Troops") return profile.Troops.ToString(CultureInfo.InvariantCulture);
            if (key == "PlayerAge") return profile.PlayerAge.ToString(CultureInfo.InvariantCulture);
            if (key == "SpouseEnabled") return profile.SpouseEnabled ? "true" : "false";
            if (key == "SpouseName") return profile.SpouseName;
            if (key == "SpouseAge") return profile.SpouseAge.ToString(CultureInfo.InvariantCulture);
            if (key == "SpouseGender") return profile.SpouseGender;
            if (key == "SpouseCulture") return profile.SpouseCulture;
            if (key == "SpouseFaceSeed") return profile.SpouseFaceSeed;
            if (key == "StartingTown") return profile.StartingTown;
            if (key == "EquipmentKit") return profile.EquipmentKit;
            if (key == "SpouseEquipmentKit") return profile.SpouseEquipmentKit;
            if (key == "ChildCount") return profile.ChildCount.ToString(CultureInfo.InvariantCulture);
            if (key == "SiblingCount") return profile.SiblingCount.ToString(CultureInfo.InvariantCulture);
            int value;
            if (profile.Overrides.TryGetValue(key, out value))
                return value.ToString(CultureInfo.InvariantCulture);
            if (key.StartsWith("Item.", StringComparison.Ordinal))
            {
                int slot;
                return Int32.TryParse(key.Substring(5), out slot) &&
                    profile.ItemIds.ContainsKey(slot) ? profile.ItemIds[slot] : "";
            }
            string[] parts = key.Split('.');
            int n;
            if (parts.Length == 3 && Int32.TryParse(parts[1], out n) && n > 0)
            {
                var list = parts[0] == "Child" ? profile.Children :
                    parts[0] == "Sibling" ? profile.Siblings : null;
                if (list != null && n <= list.Count)
                {
                    var item = list[n-1];
                    switch (parts[2])
                    {
                        case "Name": return item.Name;
                        case "Age": return item.Age.ToString(CultureInfo.InvariantCulture);
                        case "Gender": return item.Gender;
                        case "FaceSeed": return item.FaceSeed;
                    }
                }
            }
            return "native";
        }

        private void SelectSection(string name)
        {
            section = name;
            index = 0;
            Refresh();
        }

        private void Refresh()
        {
            int n;
            Field current = Current;
            string value = current == null ? "" : GetValue(current.Key);
            pending = Int32.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out n) ? n : current == null ? 0 : current.Min;
            if (current != null) pending = Math.Max(current.Min, Math.Min(current.Max, pending));
            OnPropertyChangedWithValue(SectionLabel,"SectionLabel");
            OnPropertyChangedWithValue(SelectedLabel,"SelectedLabel");
            OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            OnPropertyChangedWithValue(Status,"Status");
            OnPropertyChangedWithValue(Minimum,"Minimum");
            OnPropertyChangedWithValue(Maximum,"Maximum");
            OnPropertyChangedWithValue(IsNumeric,"IsNumeric");
            OnPropertyChangedWithValue(IsBoolean,"IsBoolean");
            OnPropertyChangedWithValue(IsChoice,"IsChoice");
            OnPropertyChangedWithValue(IsText,"IsText");
            OnPropertyChangedWithValue(pending,"PendingValue");
            OnPropertyChangedWithValue(ProfileSummary,"ProfileSummary");
        }

        [DataSourceProperty] public string SectionLabel { get { return section; } }
        [DataSourceProperty] public string SelectedLabel
        {
            get { return Current == null ? "" : Current.Name; }
        }
        [DataSourceProperty] public string SelectedValue
        {
            get
            {
                if (Current == null) return "";
                string existing = GetValue(Current.Key);
                return IsNumeric && (existing == "native" || existing == "-1")
                    ? "Native / unset" : IsNumeric
                        ? pending.ToString(CultureInfo.InvariantCulture) : existing;
            }
        }
        [DataSourceProperty] public bool IsNumeric
        {
            get { return Current != null && !Current.Boolean && !Current.Text &&
                Current.Choices == null; }
        }
        [DataSourceProperty] public bool IsBoolean
        {
            get { return Current != null && Current.Boolean; }
        }
        [DataSourceProperty] public bool IsChoice
        {
            get { return Current != null && Current.Choices != null; }
        }
        [DataSourceProperty] public bool IsText
        {
            get { return Current != null && Current.Text; }
        }
        [DataSourceProperty] public int Minimum { get { return Current == null ? 0 : Current.Min; } }
        [DataSourceProperty] public int Maximum { get { return Current == null ? 0 : Current.Max; } }
        [DataSourceProperty] public int PendingValue
        {
            get { return pending; }
            set
            {
                if (!IsNumeric) return;
                pending = Math.Max(Minimum, Math.Min(Maximum, value));
                OnPropertyChangedWithValue(pending,"PendingValue");
                OnPropertyChangedWithValue(SelectedValue,"SelectedValue");
            }
        }
        [DataSourceProperty] public string Status { get; private set; }
        [DataSourceProperty] public string ProfileSummary
        {
            get
            {
                return (profile.Enabled ? "ACTIVE" : "INACTIVE") +
                    " for NEXT new Sandbox | role " + profile.Role +
                    " | family templates: " + profile.ChildCount + " children";
            }
        }

        public void ExecuteIdentity() { SelectSection("Identity"); }
        public void ExecuteFamily() { SelectSection("Family"); }
        public void ExecuteProgression() { SelectSection("Progression"); }
        public void ExecuteEquipment() { SelectSection("Equipment"); }
        public void ExecuteLocation() { SelectSection("Location"); }

        public void ExecuteNextField()
        {
            int count = fields.Count(f => f.Section == section);
            if (count == 0) return;
            index = (index + 1) % count;
            Refresh();
        }

        public void ExecutePreviousField()
        {
            int count = fields.Count(f => f.Section == section);
            if (count == 0) return;
            index = (index + count - 1) % count;
            Refresh();
        }

        public void ExecuteToggle()
        {
            if (!IsBoolean) return;
            Save(Current.Key, GetValue(Current.Key) == "true" ? "false" : "true");
        }

        public void ExecuteNextChoice()
        {
            Choose(1);
        }
        public void ExecutePreviousChoice()
        {
            Choose(-1);
        }

        private void Choose(int direction)
        {
            if (!IsChoice) return;
            var options = Current.Choices;
            int i = Array.IndexOf(options, GetValue(Current.Key));
            i = (i + options.Length + direction) % options.Length;
            Save(Current.Key, options[i]);
        }

        public void ExecuteIncrease()
        {
            if (IsNumeric) PendingValue = (int)Math.Min(Maximum,
                (long)pending + Current.Step);
        }
        public void ExecuteDecrease()
        {
            if (IsNumeric) PendingValue = (int)Math.Max(Minimum,
                (long)pending - Current.Step);
        }
        public void ExecuteSaveNumber()
        {
            if (IsNumeric) Save(Current.Key,
                pending.ToString(CultureInfo.InvariantCulture));
        }
        public void ExecuteKeepVanilla()
        {
            if (Current != null &&
                (Current.Key == "Gold" || Current.Key == "Influence" ||
                 Current.Key == "PlayerAge"))
                Save(Current.Key, "-1");
        }
        public void ExecuteEditText()
        {
            if (!IsText) return;
            string key = Current.Key;
            InformationManager.ShowTextInquiry(new TextInquiryData(
                Current.Name,
                "Origin profile only. Values are validated and saved for a new Sandbox.",
                true, true, "Save", "Cancel",
                text => Save(key, text), () => { }, false, null, "", GetValue(key)));
        }

        private void Save(string key, string value)
        {
            var candidate = SandboxOriginProfile.Parse(profile.Serialize());
            string error;
            if (!candidate.TrySet(key, value))
            {
                Status = "Rejected value for " + key;
            }
            else if (!candidate.Save(SandboxOriginProfile.UserPath, out error))
            {
                Status = "Not saved: " + error +
                    ". Check family ages and spouse requirements.";
            }
            else
            {
                profile = candidate;
                Status = "Saved for NEXT new Sandbox: " + key + " = " + value;
                DiagnosticJournal.Event("sandbox_origin_profile_change",
                    "key=" + key + "|value=" + value);
            }
            Refresh();
        }

        public void ExecuteClose()
        {
            if (close != null) close();
        }
    }
}
