using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    /// <summary>
    /// Uses the game's own narrative choices and Back/Next. Never creates a
    /// second Gauntlet screen. Link repair is opt-in and subject to game tests.
    /// </summary>
    public static class NativeStartingLifeMenus
    {
        private const string First = "cu_life_role";
        private const string Family = "cu_life_family";
        private const string FamilyConfirm = "cu_life_family_confirm";
        private const string Resources = "cu_life_resources";
        private const string Equipment = "cu_life_equipment";
        private const string Affiliation = "cu_life_affiliation";

        public static bool TryAttach(CharacterCreationManager creator,
            SandboxOriginProfile profile)
        {
            if (creator == null || profile == null || !profile.NativeStageEnabled)
                return false;
            if (creator.GetNarrativeMenuWithId(First) != null) return false;

            // The original Sandbox age menu is the final narrative page,
            // ahead of the native banner/naming/review stages.
            var ages = creator.NarrativeMenus.Where(m => m != null &&
                m.StringId != null &&
                m.StringId.IndexOf("age", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (ages.Count != 1)
            {
                DiagnosticJournal.Event("cu_life_attach_skipped",
                    "age_menu_candidates=" + ages.Count);
                return false;
            }
            var nativeAge = ages[0];
            if (String.IsNullOrEmpty(nativeAge.StringId))
                return false;

            // Build every object before modifying the existing native graph.
            var rewrittenAge = new NarrativeMenu(nativeAge.StringId,
                nativeAge.InputMenuId, First, nativeAge.Title,
                nativeAge.Description, nativeAge.Characters,
                nativeAge.GetNarrativeMenuCharacterArgs);
            foreach (var option in nativeAge.CharacterCreationMenuOptions)
                rewrittenAge.AddNarrativeMenuOption(option);

            var role = NewPage(nativeAge, First, nativeAge.StringId, Family,
                "Starting Life: Identity", "Choose the life you begin with.");
            foreach (var value in SandboxOriginProfile.Roles)
            {
                string selected = value;
                AddOption(role, "role_" + selected, selected,
                    "Choose your starting identity. Kingdom status is verified in-game.",
                    () => SaveChoices(new[] {Pair("Role",selected)}));
            }
            var family = NewPage(nativeAge, Family, First, FamilyConfirm,
                "Starting Life: Family", "Choose the family present in your new Sandbox.");
            AddOption(family, "family_alone", "Independent, no starting family",
                "Begin without spouse, children or siblings.",
                () => SetFamily(false, 0, 0));
            AddOption(family, "family_married", "Married, no children",
                "A spouse template will be available for native hero creation.",
                () => SetFamily(true, 0, 0));
            AddOption(family, "family_one_child", "Married, one young child",
                "A spouse and one child with editable profile details.",
                () => SetFamily(true, 1, 0));
            AddOption(family, "family_two_children", "Married, two young children",
                "A spouse and two children with editable profile details.",
                () => SetFamily(true, 2, 0));
            AddOption(family, "family_single_parent", "Single parent, one young child",
                "A child with the player as one parent, no spouse.",
                () => SetFamily(false, 1, 0));
            AddOption(family, "family_siblings", "Begin with two siblings",
                "Sibling templates use native parent relationships where available.",
                () => SetFamily(false, 0, 2));

            var familyConfirm = NewPage(nativeAge, FamilyConfirm, Family,
                Resources, "Starting Life: Household Members",
                "Create real clan heroes or keep the family as a plan.");
            AddOption(familyConfirm, "family_templates_only",
                "Keep family as a starting profile",
                "No extra wife, children or sibling heroes are created.",
                () => SaveChoices(new[] {Pair("RealFamilyEnabled","false")}));
            AddOption(familyConfirm, "family_create_real",
                "EXPERIMENTAL: Create the selected real family",
                "Uses native hero and marriage APIs. Test only on a disposable save.",
                () => SaveChoices(new[] {Pair("RealFamilyEnabled","true")}));

            var finances = NewPage(nativeAge, Resources, FamilyConfirm, Equipment,
                "Starting Life: Resources", "Pick a starting treasury and influence.");
            AddOption(finances, "resource_native", "Native starting wealth",
                "Keep the game's standard resources.",
                () => SaveChoices(new[] {Pair("Gold","-1"),Pair("Influence","-1")}));
            AddOption(finances, "resource_poor", "Wanderer: 500 gold",
                "A small amount for a difficult beginning.",
                () => SaveChoices(new[] {Pair("Gold","500"),Pair("Influence","0")}));
            AddOption(finances, "resource_soldier", "Soldier: 2,000 gold",
                "Funds for food, equipment and pay.",
                () => SaveChoices(new[] {Pair("Gold","2000"),Pair("Influence","0")}));
            AddOption(finances, "resource_knight", "Knight: 12,000 gold",
                "A modest personal estate and equipment reserve.",
                () => SaveChoices(new[] {Pair("Gold","12000"),Pair("Influence","25")}));
            AddOption(finances, "resource_lord", "Lord: 50,000 gold",
                "Noble reserves and influence if your clan belongs to a kingdom.",
                () => SaveChoices(new[] {Pair("Gold","50000"),Pair("Influence","100")}));
            AddOption(finances, "resource_king", "King: 250,000 gold",
                "Royal wealth, only useful with a valid native King start.",
                () => SaveChoices(new[] {Pair("Gold","250000"),Pair("Influence","500")}));

            var equipment = NewPage(nativeAge, Equipment, Resources,
                Affiliation,
                "Starting Life: Equipment", "Choose a starting equipment theme.");
            foreach (var kit in SandboxOriginProfile.Kits)
            {
                string selected = kit;
                AddOption(equipment, "kit_" + selected, selected + " equipment",
                    "Uses only available base-game items. No external mod DLLs.",
                    () => SaveChoices(new[] {Pair("EquipmentKit",selected)}));
            }

            var affiliation = NewPage(nativeAge, Affiliation, Equipment,
                nativeAge.OutputMenuId,
                "Starting Life: Service and Political Rank",
                "Choose whether Calradia Unified requests actual native service.");
            // A complete route through the Starting Life pages is required
            // before the next-game profile is activated. An abandoned role
            // selection does not automatically grant gold or troops.
            AddOption(affiliation, "service_native",
                "Keep vanilla political status",
                "No kingdom action or role-based renown is applied.",
                () => SaveChoices(new[] {
                    Pair("RoleAffiliationsEnabled","false"),Pair("Enabled","true")}));
            AddOption(affiliation, "service_experimental",
                "EXPERIMENTAL: Begin with matching native service",
                "Soldier becomes a mercenary clan. Knight and Lord seek a vassal contract. King must be a native ruler.",
                () => SaveChoices(new[] {
                    Pair("RoleAffiliationsEnabled","true"),Pair("Enabled","true")}));

            bool removed = false;
            var added = new List<string>();
            try
            {
                creator.DeleteNarrativeMenuWithId(nativeAge.StringId);
                removed = true;
                creator.AddNewMenu(rewrittenAge);
                added.Add(nativeAge.StringId);
                foreach (var page in new[] {role, family, familyConfirm,
                    finances, equipment, affiliation})
                {
                    creator.AddNewMenu(page);
                    added.Add(page.StringId);
                }
                DiagnosticJournal.Event("cu_life_stage_attached",
                    "native_age=" + nativeAge.StringId +
                    "|pages=6|uses_native_navigation=true");
                return true;
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("cu_life_stage_attach", ex);
                foreach (var id in added)
                {
                    try { creator.DeleteNarrativeMenuWithId(id); }
                    catch (Exception) {}
                }
                if (removed)
                {
                    try { creator.AddNewMenu(nativeAge); }
                    catch (Exception restore)
                    {
                        DiagnosticJournal.Exception("cu_life_stage_restore",restore);
                    }
                }
                return false;
            }
        }

        private static NarrativeMenu NewPage(NarrativeMenu visual,
            string id, string input, string output, string title, string prompt)
        {
            return new NarrativeMenu(id, input, output,
                new TextObject(title), new TextObject(prompt),
                visual.Characters, visual.GetNarrativeMenuCharacterArgs);
        }

        private static void AddOption(NarrativeMenu menu, string key,
            string title, string description, Action selected)
        {
            menu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "cu_" + key, new TextObject(title), new TextObject(description),
                manager => new NarrativeMenuOptionArgs(),
                manager => true, manager => selected(), manager => {}));
        }

        private static KeyValuePair<string,string> Pair(string key, string value)
        {
            return new KeyValuePair<string,string>(key,value);
        }

        private static void SetFamily(bool spouse, int children, int siblings)
        {
            var settings = new List<KeyValuePair<string,string>>
            {
                Pair("SpouseEnabled", spouse ? "true" : "false"),
                Pair("ChildCount",children.ToString()),
                Pair("SiblingCount",siblings.ToString())
            };
            for (int i=1; i<=children; i++)
                settings.Add(Pair("Child." + i + ".Age", i.ToString()));
            SaveChoices(settings);
        }

        private static void SaveChoices(IEnumerable<KeyValuePair<string,string>> values)
        {
            var profile = SandboxOriginProfile.Load(SandboxOriginProfile.UserPath);
            foreach (var kv in values)
            {
                if (!profile.TrySet(kv.Key,kv.Value))
                {
                    DiagnosticJournal.Event("cu_life_choice_rejected",kv.Key);
                    return;
                }
            }
            string error;
            if (profile.Save(SandboxOriginProfile.UserPath,out error))
                DiagnosticJournal.Event("cu_life_choice_saved",
                    "role=" + profile.Role + "|family=" + profile.ChildCount);
            else
                DiagnosticJournal.Event("cu_life_choice_unsaved",error);
        }
    }
}
