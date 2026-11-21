using System;
using System.Linq;
using SandBox.AdvancedStartOptions;

namespace CalradiaUnified
{
    /// <summary>
    /// Translate Calradia Unified selections to the actual Bannerlord v1.5.7
    /// AdvancedStartOptions IDs. The engine applies royal/vassal start rules
    /// at character-creation phase 8. No label-only roles or fake titles.
    /// </summary>
    public static class SandboxNativeStartOptions
    {
        public static string StartTypeForRole(string role)
        {
            switch (role)
            {
                case "Wanderer":
                case "Recruit":
                case "HouseholdKnight":
                case "NobleCadet": return "default";
                case "Soldier": return "mercenary";
                case "Knight":
                case "Lord": return "vassal";
                case "King": return "king";
                case "Admiral": return "fleetadmiral";
                case "Trader": return "trader";
                case "Outlaw": return "outlaw";
                case "Beggar": return "beggar";
                default: return null;
            }
        }

        public static bool RequiresKingdom(string type)
        {
            return type == "king" || type == "vassal" ||
                   type == "mercenary" || type == "fleetadmiral";
        }

        public static bool TryPrepare(SandboxOriginProfile profile,
            out AdvancedStartOptions options, out string error)
        {
            options = null;
            error = "";
            if (profile == null)
            {
                error = "Starting profile missing.";
                return false;
            }
            // Real NPC-party enlistment belongs to a verified service
            // provider. The public Serve as Soldier API is read-only.
            // A Recruit begins independently and enlists through that
            // mod's own lord dialogue, never through a forged action.
            if(profile.Role=="Recruit" &&
                !SandboxSaSBridge.HasSupportedProvider())
            {
                error="Recruit needs Serve as Soldier public API v1 installed. "+
                    "Without it, choose Soldier (native mercenary captain).";
                return false;
            }
            try
            {
                var candidate = AdvancedStartOptionsManager.CreateCampaignStartOptions();
                var type = StartTypeForRole(profile.Role);
                var types = candidate.GetOption<ListAdvancedStartOption>("StartType");
                if (type == null || types == null ||
                    !types.GetItems().Any(item => item.Identifier == type))
                {
                    error = "This game installation does not provide the selected " +
                            profile.Role + " starting scenario.";
                    return false;
                }
                // The native start-type option is a player progression gate in
                // the vanilla selector. The custom creator directly uses the
                // engine's documented scenario value, not its locked selector.
                types.SetValue(type);

                if(SandboxCustomLifeRolePolicy.NeedsKingdomContext(
                    profile.Role) &&
                    String.IsNullOrEmpty(profile.StartingKingdomId))
                {
                    error="Select your birth-family or patron's kingdom.";
                    return false;
                }
                if (RequiresKingdom(type))
                {
                    var kingdom = candidate.GetOption<ListAdvancedStartOption>("KingdomId");
                    if (String.IsNullOrEmpty(profile.StartingKingdomId))
                    {
                        error = "Select a starting kingdom for " + profile.Role +
                                " in Starting Position.";
                        return false;
                    }
                    if (!SetList(kingdom, profile.StartingKingdomId))
                    {
                        error = "The selected starting kingdom is unavailable.";
                        return false;
                    }
                }

                var scenarios = candidate.GetOption<ListAdvancedStartOption>("Scenario");
                if (!SetList(scenarios, profile.StartingScenario))
                {
                    error = "The selected world scenario is unavailable.";
                    return false;
                }
                string world = profile.StartingScenario;
                if (world == "unitedempire")
                {
                    if (!EmpireKingdom(profile.ScenarioFaction1) ||
                        !SetList(candidate.GetOption<ListAdvancedStartOption>(
                            "UnitedEmpireUnifierKingdomId"), profile.ScenarioFaction1))
                    {
                        error = "United Empire requires one of the three Empire kingdoms.";
                        return false;
                    }
                }
                if (world == "LastStand" &&
                    !SetList(candidate.GetOption<ListAdvancedStartOption>(
                        "LastStandKingdomId"), profile.ScenarioFaction1))
                {
                    error = "Last Stand requires World scenario faction 1.";
                    return false;
                }
                if (world == "InvasionId" &&
                    !SetList(candidate.GetOption<ListAdvancedStartOption>(
                        "InvasionScenarioFactionId"), profile.ScenarioFaction1))
                {
                    error = "Invasion requires World scenario faction 1.";
                    return false;
                }
                if (world == "twofactionwar")
                {
                    if (String.IsNullOrEmpty(profile.ScenarioFaction1) ||
                        String.IsNullOrEmpty(profile.ScenarioFaction2) ||
                        profile.ScenarioFaction1 == profile.ScenarioFaction2 ||
                        !SetList(candidate.GetOption<ListAdvancedStartOption>(
                            "TwoFactionWarFaction1Id"), profile.ScenarioFaction1) ||
                        !SetList(candidate.GetOption<ListAdvancedStartOption>(
                            "TwoFactionWarFaction2Id"), profile.ScenarioFaction2))
                    {
                        error = "Two-Faction War requires two different factions.";
                        return false;
                    }
                }
                if (world == "alternativecalradia" &&
                    !SetList(candidate.GetOption<ListAdvancedStartOption>(
                        "AlternativeCalradiaVariantId"), profile.ScenarioVariant))
                {
                    error = "The selected Alternate Calradia variant is unavailable.";
                    return false;
                }

                SetBool(candidate, "EnableFastMode", profile.StartFastMode);
                SetBool(candidate, "RisenBanditsMultiplier", profile.StartBanditSurge);
                SetBool(candidate, "HighRebellion", profile.StartCivilUnrest);
                SetBool(candidate, "RecruitmentRate", profile.StartRecruitShortage);
                SetBool(candidate, "IncreasedGlobalMovementSpeed",
                    profile.StartSwiftTravel);
                if (profile.WorldSeed >= 0)
                {
                    var seed = candidate.GetOption("Seed");
                    if (seed == null)
                    {
                        error = "Native world seed option is missing.";
                        return false;
                    }
                    seed.SetValue((uint)profile.WorldSeed);
                }
                options = candidate;
                return true;
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("sandbox_aso_prepare", ex);
                error = "Starting scenario setup failed: " + ex.GetType().Name;
                return false;
            }
        }

        private static bool EmpireKingdom(string id)
        {
            return id == "empire" || id == "empire_s" || id == "empire_w";
        }

        private static bool SetList(ListAdvancedStartOption option, string id)
        {
            if (option == null || String.IsNullOrEmpty(id) ||
                !option.GetItems().Any(item => item.Identifier == id))
                return false;
            option.SetValue(id);
            return true;
        }

        private static void SetBool(AdvancedStartOptions options,
            string key, bool value)
        {
            var item = options.GetOption(key);
            if (item != null) item.SetValue(value);
        }
    }
}
