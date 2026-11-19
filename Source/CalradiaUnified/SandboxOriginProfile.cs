using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CalradiaUnified
{
    /// <summary>
    /// Standalone, versioned pre-campaign origin plan.
    /// Saving a profile never modifies a running campaign or creates a hero.
    /// </summary>
    public sealed class SandboxOriginProfile
    {
        public const int MaxChildren = 6;
        public const int MaxSiblings = 4;
        public static readonly string[] Roles =
            { "Wanderer", "Soldier", "Knight", "Lord", "King", "Admiral",
      "Trader", "Outlaw", "Beggar" };
        public static readonly string[] Kits =
            { "Vanilla", "Traveler", "Soldier", "Knight", "Royal", "Custom" };
        public static readonly string[] Attributes =
            { "Vigor", "Control", "Endurance", "Cunning", "Social", "Intelligence" };
        public static readonly string[] Skills =
        {
            "OneHanded","TwoHanded","Polearm","Bow","Crossbow","Throwing",
            "Riding","Athletics","Crafting","Scouting","Tactics","Roguery",
            "Charm","Leadership","Trade","Steward","Medicine","Engineering"
        };
        public static readonly string[] Traits =
            { "Mercy", "Valor", "Honor", "Generosity", "Calculating" };

        public bool Enabled;
        // Clicking Sandbox in the main menu opens the CU creator directly.
        // Set false to restore the original Default/Advanced Start selector.
        public bool OverhauledCreatorEnabled = true;
        public string StartingCulture = "";
        // Face editing remains native until a verified state-specific bridge exists.
        // Dangerous creation actions require three independent opt-ins.
        public bool NativeStageEnabled;
        // A SEPARATE, opt-in review stage appends AFTER native Options.
        // Never substitute a native FaceGen/Culture stage view.
        public bool NativeIntegrationReviewEnabled;
        // Experimental native face-editor composition within a CU stage.
        // Off by default, separate from the v0.29 review trial.
        public bool NativeFaceWorkshopEnabled;
        // Opt-in persistent native-state CU ribbon. Never overrides stage views.
        public bool NativeStateShellEnabled;
        // Original native FaceGen SceneView right-dock experiment; OFF on new game.
        public bool NativeFaceDockTrialEnabled;
        public bool RealFamilyEnabled;
        public bool RoleAffiliationsEnabled;
        public string Role = "Wanderer";
        // Native Advanced Start IDs, not culture IDs.
        public static readonly string[] KingdomIds =
            { "", "vlandia", "sturgia", "battania", "empire",
              "empire_w", "empire_s", "khuzait", "aserai" };
        public static readonly string[] Scenarios =
            { "none", "unitedempire", "LastStand", "twofactionwar",
              "InvasionId", "alternativecalradia" };
        public string StartingKingdomId = "";
        public string StartingScenario = "none";
        public string ScenarioFaction1 = "";
        public string ScenarioFaction2 = "";
        public string ScenarioVariant = "alternativecalradiadefault";
        public bool StartFastMode;
        public bool StartBanditSurge;
        public bool StartCivilUnrest;
        public bool StartRecruitShortage;
        public bool StartSwiftTravel;
        public int WorldSeed = -1;
        public int Gold = -1;
        public int Influence = -1;
        public int Troops;
        public int PlayerAge = -1;
        public string StartingTown = "";
        // A named fief or selected class of fortification may be granted
        // after the native King/Vassal Advanced Start has completed.
        public string StartingFiefMode = "Native";
        public string StartingFiefId = "";
        public bool AllowStartingFiefTransfer;
        public bool RelocateAtStart;
        public string EquipmentKit = "Vanilla";
        public bool SpouseEnabled;
        public string SpouseName = "Companion";
        public int SpouseAge = 30;
        public string SpouseGender = "Auto";
        public string SpouseCulture = "";
        // A NEW spouse is generated. Noble modes can provide a native birth
        // clan with real parent links. No existing hero is married directly.
        public string SpouseBackground = "Commoner";
        public string SpouseBirthClanId = "";
        // Where the generated household lives at campaign start.
        // Only native, owned and safe fortified settlements qualify.
        public string FamilyHomeMode = "OwnedFief";
        public string FamilyHomeId = "";
        public string SpouseFaceSeed = "";
        public string SpouseEquipmentKit = "Vanilla";
        public int ChildCount;
        public int SiblingCount;

        // Explicit overrides only; absent entry means retain the game's value.
        // Native is the safe default. The dedicated post-origins editor
        // changes this only after the real narrative has been completed.
        public string ProgressionMode = "Native";
        public readonly SortedDictionary<string, int> Overrides =
            new SortedDictionary<string, int>(StringComparer.Ordinal);
        // Up to 12 exact, engine-resolved item IDs; missing IDs are never granted.
        public readonly SortedDictionary<int, string> ItemIds =
            new SortedDictionary<int, string>();
        public readonly List<OriginRelative> Children = new List<OriginRelative>();
        public readonly List<OriginRelative> Siblings = new List<OriginRelative>();

        public sealed class OriginRelative
        {
            public string Name;
            public int Age;
            public string Gender;
            public string Culture;
            public string FaceSeed;
            public string EquipmentKit;
            public OriginRelative(string kind, int index)
            {
                // These must pass the same name validator used by Save().
                // The old "Child 1" and "Sibling 1" defaults contained
                // digits and silently rejected the native family presets.
                string[] childNames =
                    { "Alden", "Mira", "Rowan", "Elira", "Tomas", "Nadia" };
                string[] siblingNames =
                    { "Taran", "Leora", "Daric", "Elian" };
                Name = kind == "Child"
                    ? childNames[index - 1] : siblingNames[index - 1];
                Age = kind == "Child" ? 6 + index : 24 + index;
                Gender = index % 2 == 0 ? "Female" : "Male";
                Culture = "";
                FaceSeed = "";
                EquipmentKit = "Vanilla";
            }
        }

        public SandboxOriginProfile()
        {
            for (int i = 1; i <= MaxChildren; i++)
                Children.Add(new OriginRelative("Child", i));
            for (int i = 1; i <= MaxSiblings; i++)
                Siblings.Add(new OriginRelative("Sibling", i));
        }

        /// <summary>
        /// Fresh Sandbox starts cannot inherit another campaign's dependent
        /// count or an authorized fief transfer. Preserve character names,
        /// spouse background and role choices so users may reuse preferences.
        /// The player explicitly chooses a spouse and child count each time.
        /// </summary>
        public void ResetForNewCampaign()
        {
            Enabled=false;
            NativeStageEnabled=false;
            NativeIntegrationReviewEnabled=false;
            NativeFaceWorkshopEnabled=false;
            NativeStateShellEnabled=false;
            NativeFaceDockTrialEnabled=false;
            RealFamilyEnabled=false;
            RoleAffiliationsEnabled=false;
            SpouseEnabled=false;
            ChildCount=0;
            SiblingCount=0;
            ProgressionMode="Native";
            Overrides.Clear();
            // Never carry unimplemented draft requests into a new campaign.
            PlayerAge=-1;
            StartingTown="";
            EquipmentKit="Vanilla";
            StartingFiefMode="Native";
            AllowStartingFiefTransfer=false;
            StartingFiefId="";
            FamilyHomeMode="OwnedFief";
            FamilyHomeId="";
        }

        public static string UserPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "CalradiaUnified", "sandbox-origin.ini");
            }
        }

        private static bool Allowed(string value, string[] values)
        {
            return values.Contains(value, StringComparer.Ordinal);
        }

        private static bool Number(string value, int minimum, int maximum, out int n)
        {
            if (!Int32.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out n)) return false;
            return n >= minimum && n <= maximum;
        }

        private static bool Identifier(string value, int max)
        {
            if (value == null || value.Length > max) return false;
            return value.All(c => Char.IsLetterOrDigit(c) || c == '_' || c == '-');
        }

        private static bool PersonName(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 40) return false;
            return value.All(c => Char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
        }

        public bool TrySet(string key, string value)
        {
            if (String.IsNullOrEmpty(key) || value == null || value.Length > 200)
                return false;
            bool b;
            int n;
            switch (key)
            {
                case "Enabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    Enabled = b; return true;
                case "OverhauledCreatorEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    OverhauledCreatorEnabled = b; return true;
                case "ProgressionMode":
                    if (!SandboxStatBudgetPolicy.IsMode(value)) return false;
                    ProgressionMode = value; return true;
                case "StartingCulture":
                    if (!Identifier(value, 80)) return false;
                    StartingCulture = value; return true;
                case "NativeStageEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    NativeStageEnabled = b; return true;
                case "NativeIntegrationReviewEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    NativeIntegrationReviewEnabled = b; return true;
                case "NativeFaceWorkshopEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    NativeFaceWorkshopEnabled = b; return true;
                case "NativeStateShellEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    NativeStateShellEnabled = b; return true;
                case "NativeFaceDockTrialEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    NativeFaceDockTrialEnabled = b; return true;
                case "RealFamilyEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    RealFamilyEnabled = b; return true;
                case "RoleAffiliationsEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    RoleAffiliationsEnabled = b; return true;
                case "Role":
                    if (!Allowed(value, Roles)) return false;
                    Role = value; return true;
                case "StartingKingdomId":
                    if (!Allowed(value, KingdomIds)) return false;
                    StartingKingdomId = value; return true;
                case "StartingScenario":
                    if (!Allowed(value, Scenarios)) return false;
                    StartingScenario = value; return true;
                case "ScenarioFaction1":
                    if (!Allowed(value, KingdomIds)) return false;
                    ScenarioFaction1 = value; return true;
                case "ScenarioFaction2":
                    if (!Allowed(value, KingdomIds)) return false;
                    ScenarioFaction2 = value; return true;
                case "ScenarioVariant":
                    if (value != "alternativecalradiadefault" &&
                        value != "alternativecalradiafractured" &&
                        value != "alternativecalradiashattered") return false;
                    ScenarioVariant = value; return true;
                case "StartFastMode":
                    if (!Boolean.TryParse(value, out b)) return false;
                    StartFastMode = b; return true;
                case "StartBanditSurge":
                    if (!Boolean.TryParse(value, out b)) return false;
                    StartBanditSurge = b; return true;
                case "StartCivilUnrest":
                    if (!Boolean.TryParse(value, out b)) return false;
                    StartCivilUnrest = b; return true;
                case "StartRecruitShortage":
                    if (!Boolean.TryParse(value, out b)) return false;
                    StartRecruitShortage = b; return true;
                case "StartSwiftTravel":
                    if (!Boolean.TryParse(value, out b)) return false;
                    StartSwiftTravel = b; return true;
                case "WorldSeed":
                    if (!Number(value, -1, 2000000000, out n)) return false;
                    WorldSeed = n; return true;
                case "Gold":
                    if (!Number(value, -1, 2000000, out n)) return false;
                    Gold = n; return true;
                case "Influence":
                    if (!Number(value, -1, 100000, out n)) return false;
                    Influence = n; return true;
                case "Troops":
                    if (!Number(value, 0, 50, out n)) return false;
                    Troops = n; return true;
                case "PlayerAge":
                    if (!Number(value, -1, 80, out n) || (n != -1 && n < 18))
                        return false;
                    PlayerAge = n; return true;
                case "StartingTown":
                    if (!Identifier(value, 80)) return false;
                    StartingTown = value; return true;
                case "StartingFiefMode":
                    if (!Allowed(value, SandboxFiefPolicy.Modes)) return false;
                    StartingFiefMode = value; return true;
                case "StartingFiefId":
                    if (!Identifier(value, 80)) return false;
                    StartingFiefId = value; return true;
                case "AllowStartingFiefTransfer":
                    if (!Boolean.TryParse(value, out b)) return false;
                    AllowStartingFiefTransfer = b; return true;
                case "RelocateAtStart":
                    if (!Boolean.TryParse(value, out b)) return false;
                    RelocateAtStart = b; return true;
                case "EquipmentKit":
                    if (!Allowed(value, Kits)) return false;
                    EquipmentKit = value; return true;
                case "SpouseEnabled":
                    if (!Boolean.TryParse(value, out b)) return false;
                    SpouseEnabled = b; return true;
                case "SpouseName":
                    if (!PersonName(value)) return false;
                    SpouseName = value; return true;
                case "SpouseAge":
                    if (!Number(value, 18, 70, out n)) return false;
                    SpouseAge = n; return true;
                case "SpouseGender":
                    if (value != "Female" && value != "Male" && value != "Auto") return false;
                    SpouseGender = value; return true;
                case "SpouseCulture":
                    if (!Identifier(value, 80)) return false;
                    SpouseCulture = SandboxFamilyOriginPolicy
                        .NormalizeCultureId(value); return true;
                case "SpouseBackground":
                    if (!Allowed(value, SandboxFamilyOriginPolicy.Backgrounds))
                        return false;
                    SpouseBackground = value; return true;
                case "SpouseBirthClanId":
                    if (!Identifier(value, 80)) return false;
                    SpouseBirthClanId = value; return true;
                case "FamilyHomeMode":
                    if (!Allowed(value,SandboxFamilyHomePolicy.Modes))
                        return false;
                    FamilyHomeMode=value; return true;
                case "FamilyHomeId":
                    if (!Identifier(value,80)) return false;
                    FamilyHomeId=value; return true;
                case "SpouseFaceSeed":
                    if (!Identifier(value, 80)) return false;
                    SpouseFaceSeed = value; return true;
                case "SpouseEquipmentKit":
                    if (!Allowed(value, Kits)) return false;
                    SpouseEquipmentKit = value; return true;
                case "ChildCount":
                    if (!Number(value, 0, MaxChildren, out n)) return false;
                    ChildCount = n; return true;
                case "SiblingCount":
                    if (!Number(value, 0, MaxSiblings, out n)) return false;
                    SiblingCount = n; return true;
            }
            if (key.StartsWith("Stat.", StringComparison.Ordinal) ||
                key.StartsWith("Attribute.", StringComparison.Ordinal) ||
                key.StartsWith("Skill.", StringComparison.Ordinal) ||
                key.StartsWith("Focus.", StringComparison.Ordinal) ||
                key.StartsWith("Trait.", StringComparison.Ordinal))
            {
                string[] parts = key.Split('.');
                if (parts.Length != 2) return false;
                bool attribute = parts[0] == "Attribute" || parts[0] == "Stat";
                bool skill = parts[0] == "Skill";
                bool focus = parts[0] == "Focus";
                bool trait = parts[0] == "Trait";
                if (!(attribute && Allowed(parts[1], Attributes) ||
                      (skill || focus) && Allowed(parts[1], Skills) ||
                      trait && Allowed(parts[1], Traits))) return false;
                if (!Number(value,
                    trait ? -2 : attribute ? 1 : 0,
                    trait ? 2 : attribute ? 10 : focus ? 5 : 330,
                    out n)) return false;
                Overrides[(attribute ? "Attribute" : parts[0]) + "." + parts[1]] = n;
                return true;
            }
            if (key.StartsWith("Item.", StringComparison.Ordinal))
            {
                if (!Number(key.Substring(5), 1, 12, out n) ||
                    !Identifier(value, 90)) return false;
                if (value.Length == 0) ItemIds.Remove(n);
                else ItemIds[n] = value;
                return true;
            }
            if (key.StartsWith("Child.", StringComparison.Ordinal) ||
                key.StartsWith("Sibling.", StringComparison.Ordinal))
            {
                string[] parts = key.Split('.');
                if (parts.Length != 3 || !Number(parts[1], 1,
                    parts[0] == "Child" ? MaxChildren : MaxSiblings, out n))
                    return false;
                var relative = parts[0] == "Child" ? Children[n - 1] : Siblings[n - 1];
                int age;
                switch (parts[2])
                {
                    case "Name":
                        if (!PersonName(value)) return false;
                        relative.Name = value; return true;
                    case "Age":
                        if (!Number(value, parts[0] == "Child" ? 0 : 18,
                            parts[0] == "Child" ? 55 : 80, out age)) return false;
                        relative.Age = age; return true;
                    case "Gender":
                        if (value != "Male" && value != "Female") return false;
                        relative.Gender = value; return true;
                    case "Culture":
                        if (!Identifier(value, 80)) return false;
                        relative.Culture = value; return true;
                    case "FaceSeed":
                        if (!Identifier(value, 80)) return false;
                        relative.FaceSeed = value; return true;
                    case "EquipmentKit":
                        if (!Allowed(value, Kits)) return false;
                        relative.EquipmentKit = value; return true;
                }
            }
            return false;
        }

        public bool IsValid(int actualPlayerAge, out string error)
        {
            error = "";
            if (!SandboxStatBudgetPolicy.IsMode(ProgressionMode))
            {
                error = "Unknown starting-stats mode.";
                return false;
            }
            int playerAge = PlayerAge >= 18 ? PlayerAge : actualPlayerAge;
            if (playerAge < 18 || playerAge > 100)
            {
                error = "Player must be an adult.";
                return false;
            }
            if (SpouseEnabled && !PersonName(SpouseName))
            {
                error = "Spouse needs a valid name.";
                return false;
            }
            string familyOriginError = SandboxFamilyOriginPolicy.Validate(
                SpouseBackground,SpouseBirthClanId,SpouseEnabled);
            if (familyOriginError.Length != 0)
            {
                error = familyOriginError;
                return false;
            }
            // A SpecificOwned home ID is entered on the NEXT field in the
            // creator. Allow intermediate edits and check completeness at
            // final acceptance instead; otherwise the choice is impossible.
            for (int i = 0; i < ChildCount; i++)
            {
                var child = Children[i];
                if (!PersonName(child.Name) || child.Age < 0 ||
                    child.Age > playerAge - 16 ||
                    (SpouseEnabled && child.Age > SpouseAge - 16))
                {
                    error = "Child ages must fit both parents' ages and valid names.";
                    return false;
                }
            }
            for (int i = 0; i < SiblingCount; i++)
                if (!PersonName(Siblings[i].Name))
                {
                    error = "Sibling needs a valid name.";
                    return false;
                }
            return true;
        }

        public string Describe()
        {
            return "role=" + Role + ", " + (Gold < 0 ? "native gold" : Gold + " gold") +
                ", " + (Influence < 0 ? "native influence" : Influence + " influence") +
                ", " + Troops + " additional troops, kit=" + EquipmentKit +
                ", family=" + (SpouseEnabled ?
                    SpouseBackground + " spouse " + SpouseName : "none") +
                ", children=" + ChildCount + ", siblings=" + SiblingCount +
                ", kingdom=" + (StartingKingdomId.Length == 0 ? "not selected" : StartingKingdomId) +
                ", scenario=" + StartingScenario +
                ", fief=" + (StartingFiefMode == "Native" ? "native" :
                    StartingFiefMode + ":" + StartingFiefId) +
                ", town=" + (StartingTown.Length == 0 ? "native" : StartingTown) +
                ", stat mode=" + ProgressionMode +
                ", stat overrides=" + Overrides.Count +
                ". Profile applies to a NEW Sandbox campaign only.";
        }

        public string Serialize()
        {
            var lines = new List<string>
            {
                "# CU sandbox origin profile v1. Configure before starting NEW Sandbox.",
                "# -1 means do not overwrite native Gold, Influence or PlayerAge.",
                "# Lord/King standing is granted by vanilla Advanced Starting Options.",
                "# v0.20: Explicit family requests are processed after native character creation.",
                "Version=CU_ORIGIN_1",
                "Enabled=" + Enabled.ToString().ToLowerInvariant(),
                "OverhauledCreatorEnabled=" +
                    OverhauledCreatorEnabled.ToString().ToLowerInvariant(),
                "StartingCulture=" + StartingCulture,
                "ProgressionMode=" + ProgressionMode,
                "NativeStageEnabled=" + NativeStageEnabled.ToString().ToLowerInvariant(),
                "NativeIntegrationReviewEnabled=" +
                    NativeIntegrationReviewEnabled.ToString().ToLowerInvariant(),
                "NativeFaceWorkshopEnabled=" +
                    NativeFaceWorkshopEnabled.ToString().ToLowerInvariant(),
                "NativeStateShellEnabled=" +
                    NativeStateShellEnabled.ToString().ToLowerInvariant(),
                "NativeFaceDockTrialEnabled=" +
                    NativeFaceDockTrialEnabled.ToString().ToLowerInvariant(),
                "RealFamilyEnabled=" + RealFamilyEnabled.ToString().ToLowerInvariant(),
                "RoleAffiliationsEnabled=" + RoleAffiliationsEnabled.ToString().ToLowerInvariant(),
                "Role=" + Role,
                "StartingKingdomId=" + StartingKingdomId,
                "StartingScenario=" + StartingScenario,
                "ScenarioFaction1=" + ScenarioFaction1,
                "ScenarioFaction2=" + ScenarioFaction2,
                "ScenarioVariant=" + ScenarioVariant,
                "StartFastMode=" + StartFastMode.ToString().ToLowerInvariant(),
                "StartBanditSurge=" + StartBanditSurge.ToString().ToLowerInvariant(),
                "StartCivilUnrest=" + StartCivilUnrest.ToString().ToLowerInvariant(),
                "StartRecruitShortage=" + StartRecruitShortage.ToString().ToLowerInvariant(),
                "StartSwiftTravel=" + StartSwiftTravel.ToString().ToLowerInvariant(),
                "WorldSeed=" + WorldSeed,
                "Gold=" + Gold,
                "Influence=" + Influence,
                "Troops=" + Troops,
                "PlayerAge=" + PlayerAge,
                "StartingTown=" + StartingTown,
                "StartingFiefMode=" + StartingFiefMode,
                "StartingFiefId=" + StartingFiefId,
                "AllowStartingFiefTransfer=" +
                    AllowStartingFiefTransfer.ToString().ToLowerInvariant(),
                "RelocateAtStart=" + RelocateAtStart.ToString().ToLowerInvariant(),
                "EquipmentKit=" + EquipmentKit,
                "SpouseEnabled=" + SpouseEnabled.ToString().ToLowerInvariant(),
                "SpouseName=" + SpouseName,
                "SpouseAge=" + SpouseAge,
                "SpouseGender=" + SpouseGender,
                "SpouseCulture=" + SpouseCulture,
                "SpouseBackground=" + SpouseBackground,
                "SpouseBirthClanId=" + SpouseBirthClanId,
                "FamilyHomeMode=" + FamilyHomeMode,
                "FamilyHomeId=" + FamilyHomeId,
                "SpouseFaceSeed=" + SpouseFaceSeed,
                "SpouseEquipmentKit=" + SpouseEquipmentKit,
                "ChildCount=" + ChildCount,
                "SiblingCount=" + SiblingCount
            };
            foreach (var pair in Overrides)
                lines.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var pair in ItemIds)
                lines.Add("Item." + pair.Key + "=" + pair.Value);
            // Preserve unused relative templates so names can be edited before
            // setting ChildCount or SiblingCount without losing pending edits.
            for (int i = 0; i < MaxChildren; i++)
                AddRelative(lines, "Child", i + 1, Children[i]);
            for (int i = 0; i < MaxSiblings; i++)
                AddRelative(lines, "Sibling", i + 1, Siblings[i]);
            return String.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static void AddRelative(List<string> lines, string kind,
            int index, OriginRelative r)
        {
            string prefix = kind + "." + index + ".";
            lines.Add(prefix + "Name=" + r.Name);
            lines.Add(prefix + "Age=" + r.Age);
            lines.Add(prefix + "Gender=" + r.Gender);
            lines.Add(prefix + "Culture=" + r.Culture);
            lines.Add(prefix + "FaceSeed=" + r.FaceSeed);
            lines.Add(prefix + "EquipmentKit=" + r.EquipmentKit);
        }

        public static SandboxOriginProfile Parse(string text)
        {
            var p = new SandboxOriginProfile();
            if (String.IsNullOrEmpty(text)) return p;
            foreach (string raw in text.Split(new[] { '\n' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int equals = line.IndexOf('=');
                if (equals < 1) continue;
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                if (key == "Version") continue;
                p.TrySet(key, value);
            }
            return p;
        }

        public static SandboxOriginProfile Load(string file)
        {
            try
            {
                if (!File.Exists(file)) return new SandboxOriginProfile();
                if (new FileInfo(file).Length > 64 * 1024)
                    throw new InvalidDataException("Origin profile exceeds 64 KiB");
                var p = Parse(File.ReadAllText(file, Encoding.UTF8));
                string problem;
                if (!p.IsValid(30, out problem))
                    throw new InvalidDataException(problem);
                return p;
            }
            catch (Exception e)
            {
                if (!(e is IOException || e is UnauthorizedAccessException ||
                      e is InvalidDataException)) throw;
                return new SandboxOriginProfile();
            }
        }

        public bool Save(string file, out string error)
        {
            if (!IsValid(30, out error)) return false;
            if (String.IsNullOrEmpty(file))
            {
                error = "No profile location";
                return false;
            }
            string temp = null;
            try
            {
                string folder = Path.GetDirectoryName(file);
                if (String.IsNullOrEmpty(folder))
                    throw new InvalidDataException("No settings directory");
                Directory.CreateDirectory(folder);
                temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temp, Serialize(), new UTF8Encoding(false));
                if (File.Exists(file)) File.Replace(temp, file, null);
                else File.Move(temp, file);
                error = "";
                return true;
            }
            catch (Exception e)
            {
                if (!(e is IOException || e is UnauthorizedAccessException ||
                      e is InvalidDataException || e is ArgumentException)) throw;
                error = e.GetType().Name;
                return false;
            }
            finally { if (temp != null && File.Exists(temp)) File.Delete(temp); }
        }
    }
}
