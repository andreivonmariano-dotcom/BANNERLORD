using System;
using System.IO;

namespace CalradiaUnified
{
    public sealed class Settings
    {
        public bool EnableDiagnostics = true;
        // Enabled by default. Set false in the user's local config to
        // keep legacy CSV logging without automatic session ZIPs.
        public bool AutoSessionReports = true;
        public bool EnableSupplyWarnings = true;
        public int SupplyWarningDays = 3;
        public bool EnableArmyWarnings = true;
        // Gameplay mutation is user-controlled and disabled until tested in v1.5.7.
        public bool EnablePlayerForaging = false;
        public int ForageWhenDaysAtMost = 2;
        public int ForageEveryDays = 5;
        public int GrainPerForage = 2;

        // All new gameplay systems are opt-in, except the read-only NPC advisor.
        public bool EnableCivicSystems = false;
        public bool EnableAbstractShipments = false;
        public bool EnableEconomy = false;
        public bool EnableRecruitment = false;
        public bool EnableLocalDiplomacy = false;
        public bool EnableNpcAdvisory = true;
        // Experimental physical convoys use engine pathfinding and real map parties.
        // Disabled until a v1.5.7 build and new-campaign checks pass.
        public bool EnablePhysicalConvoys = false;
        public int ConvoyCargoGrain = 10;
        public int ConvoyGuards = 8;
        public int MaxActiveConvoys = 2;
        public int ConvoyTimeoutDays = 25;
        // Kingdom actions are opt-in, limited to the player's faction and settlement.
        public bool EnableDepotTrade = false;
        public int MaxDailyCouncilReports = 3;
        public int CouncilPreferredPartySize = 45;
        public bool EnableKingdomLevies = false;
        public bool EnableKingdomDiplomacy = false;
        // Autonomous orders affect player-clan NPC lords only, never army or siege parties.
        // Both features are OFF until a new-campaign save/load and 30-day test on v1.5.7.
        public bool EnableAutonomousClanResupply = false;
        public bool EnableAutonomousClanRecruitTrips = false;
        // Only player-clan NPC lords; friendly, unbesieged own-clan settlement,
        // confirmed peacetime and no active army, battle, or siege orders.
        public bool EnableAutonomousClanPatrol = false;
        public int AutonomousPatrolMinimumTroops = 55;
        public int AutonomousLowFoodDays = 2;
        public int AutonomousRecruitBelow = 35;
        public int AutonomousMinimumSettlementFood = 40;
        public int AutonomousCooldownDays = 7;
        public int AutonomousMaxDailyOrders = 2;
        public int KingdomLevyCostGold = 250;
        public int KingdomLevyCostInfluence = 5;
        public int KingdomLevyDailyCap = 2;
        public int DiplomaticMissionCostGold = 300;
        public int DiplomaticMissionCostInfluence = 10;
        public int DiplomaticMissionCooldownDays = 7;
        public int NpcFoodThreshold = 2;
        public int MaxDepotInvestment = 3;
        public int DepotCapacity = 100;
        public int RecruitmentMaxPartySize = 100;
        public int LocalRecruitDailyCap = 2;
        public bool EnableGarrisonTraining = false;
        public int GarrisonTrainingDailyCap = 4;
        public int GarrisonTrainingMaxTroops = 250;
        // Fief budget is read-only by default. Auto changes remain experimental.
        public bool EnableAutoGarrisonWageCaps = false;
        public bool EnableManualGarrisonTransfers = false;
        public bool EnableManualGarrisonWageCaps = false;
        public bool EnableAutoGarrisonTraining = false;
        public bool EnableVillageHarvest = false;
        public bool EnableSurplusEstateSales = false;
        public int EstateTargetDailySurplus = 250;
        public int EstateOtherCostsReserve = 150;
        public int EstateMaxDailyGarrisonWage = 1800;
        public int EstateMinimumDefensiveWage = 250;
        public int EstateTargetGarrisonSize = 55;
        public int EstateMinimumTrainingGold = 5000;
        public int EstateHearthsPerGrain = 150;
        public int EstateMaxHarvestPerDay = 10;
        public int EstateGrainReserve = 30;
        public int EstateMaxDailyGrainSale = 8;
        public int EstateSalePricePerGrain = 18;

        public static Settings Load()
        {
            var settings=LoadFromPaths(
                Path.Combine(Log.ModuleRoot, "ModuleData", "calradia-config.ini"),
                UserOverridePath);
            settings.ApplyAcceptanceFreeze();
            return settings;
        }

        // Acceptance build: legacy settings remain readable for migration,
        // but cannot restore retired/overlapping mutations through old INI files.
        public static bool IsHeldMutation(string key)
        {
            switch(key)
            {
                case "EnablePlayerForaging":
                case "EnableCivicSystems":
                case "EnableAbstractShipments":
                case "EnableEconomy":
                case "EnableRecruitment":
                case "EnableLocalDiplomacy":
                case "EnablePhysicalConvoys":
                case "EnableDepotTrade":
                case "EnableKingdomLevies":
                case "EnableKingdomDiplomacy":
                case "EnableAutonomousClanResupply":
                case "EnableAutonomousClanRecruitTrips":
                case "EnableAutonomousClanPatrol":
                case "EnableGarrisonTraining":
                case "EnableAutoGarrisonWageCaps":
                case "EnableManualGarrisonTransfers":
                case "EnableManualGarrisonWageCaps":
                case "EnableAutoGarrisonTraining":
                case "EnableVillageHarvest":
                case "EnableSurplusEstateSales":
                    return true;
                default:return false;
            }
        }
        public void ApplyAcceptanceFreeze()
        {
            EnablePlayerForaging=false;
            EnableCivicSystems=false;
            EnableAbstractShipments=false;
            EnableEconomy=false;
            EnableRecruitment=false;
            EnableLocalDiplomacy=false;
            EnablePhysicalConvoys=false;
            EnableDepotTrade=false;
            EnableKingdomLevies=false;
            EnableKingdomDiplomacy=false;
            EnableAutonomousClanResupply=false;
            EnableAutonomousClanRecruitTrips=false;
            EnableAutonomousClanPatrol=false;
            EnableGarrisonTraining=false;
            EnableAutoGarrisonWageCaps=false;
            EnableManualGarrisonTransfers=false;
            EnableManualGarrisonWageCaps=false;
            EnableAutoGarrisonTraining=false;
            EnableVillageHarvest=false;
            EnableSurplusEstateSales=false;
        }

        // Direct roster edits are held until the installed garrison owner
        // and native roster conservation are verified in a real campaign.
        public static bool NativeOfficerTransfersEnabled { get { return false; } }

        public static string UserOverridePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "CalradiaUnified", "calradia-user-config.ini");
            }
        }

        public static Settings LoadFromPaths(string bundled, string userOverride)
        {
            var output = new Settings();
            if (File.Exists(bundled))
                ApplyLines(output, File.ReadAllLines(bundled));
            // The user profile is optional. A malformed, protected or oversized
            // file should never prevent campaign behaviors from loading.
            try
            {
                if (File.Exists(userOverride))
                {
                    if (new FileInfo(userOverride).Length > 128 * 1024)
                        throw new InvalidDataException("User config is too large.");
                    ApplyLines(output, File.ReadAllLines(userOverride));
                }
            }
            catch (IOException ex)
            {
                Log.Write("User settings override ignored: " + ex.GetType().Name);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Write("User settings override ignored: " + ex.GetType().Name);
            }
            return output;
        }

        // Existing INI parser remains the only route for updating live settings.
        private static void ApplyLines(Settings output, string[] lines)
        {
            foreach (string raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                bool b;
                int n;
                if (key == "EnableDiagnostics" && Boolean.TryParse(value, out b)) output.EnableDiagnostics = b;
                else if (key == "AutoSessionReports" && Boolean.TryParse(value, out b)) output.AutoSessionReports = b;
                else if (key == "EnableSupplyWarnings" && Boolean.TryParse(value, out b)) output.EnableSupplyWarnings = b;
                else if (key == "EnableArmyWarnings" && Boolean.TryParse(value, out b)) output.EnableArmyWarnings = b;
                else if (key == "EnablePlayerForaging" && Boolean.TryParse(value, out b)) output.EnablePlayerForaging = b;
                else if (key == "SupplyWarningDays" && Int32.TryParse(value, out n)) output.SupplyWarningDays = Clamp(n, 0, 15);
                else if (key == "ForageWhenDaysAtMost" && Int32.TryParse(value, out n)) output.ForageWhenDaysAtMost = Clamp(n, 0, 15);
                else if (key == "ForageEveryDays" && Int32.TryParse(value, out n)) output.ForageEveryDays = Clamp(n, 1, 90);
                else if (key == "EnableCivicSystems" && Boolean.TryParse(value, out b)) output.EnableCivicSystems = b;
                else if (key == "EnableAbstractShipments" && Boolean.TryParse(value, out b)) output.EnableAbstractShipments = b;
                else if (key == "EnableEconomy" && Boolean.TryParse(value, out b)) output.EnableEconomy = b;
                else if (key == "EnableRecruitment" && Boolean.TryParse(value, out b)) output.EnableRecruitment = b;
                else if (key == "EnableLocalDiplomacy" && Boolean.TryParse(value, out b)) output.EnableLocalDiplomacy = b;
                else if (key == "EnableNpcAdvisory" && Boolean.TryParse(value, out b)) output.EnableNpcAdvisory = b;
                else if (key == "EnablePhysicalConvoys" && Boolean.TryParse(value, out b)) output.EnablePhysicalConvoys = b;
                else if (key == "EnableDepotTrade" && Boolean.TryParse(value, out b)) output.EnableDepotTrade = b;
                else if (key == "MaxDailyCouncilReports" && Int32.TryParse(value, out n)) output.MaxDailyCouncilReports = Clamp(n, 0, 20);
                else if (key == "CouncilPreferredPartySize" && Int32.TryParse(value, out n)) output.CouncilPreferredPartySize = Clamp(n, 1, 500);
                else if (key == "EnableKingdomLevies" && Boolean.TryParse(value, out b)) output.EnableKingdomLevies = b;
                else if (key == "EnableKingdomDiplomacy" && Boolean.TryParse(value, out b)) output.EnableKingdomDiplomacy = b;
                else if (key == "EnableAutonomousClanResupply" && Boolean.TryParse(value, out b)) output.EnableAutonomousClanResupply = b;
                else if (key == "EnableAutonomousClanRecruitTrips" && Boolean.TryParse(value, out b)) output.EnableAutonomousClanRecruitTrips = b;
                else if (key == "EnableAutonomousClanPatrol" && Boolean.TryParse(value, out b)) output.EnableAutonomousClanPatrol = b;
                else if (key == "AutonomousPatrolMinimumTroops" && Int32.TryParse(value, out n)) output.AutonomousPatrolMinimumTroops = Clamp(n, 20, 200);
                else if (key == "AutonomousLowFoodDays" && Int32.TryParse(value, out n)) output.AutonomousLowFoodDays = Clamp(n, 0, 10);
                else if (key == "AutonomousRecruitBelow" && Int32.TryParse(value, out n)) output.AutonomousRecruitBelow = Clamp(n, 10, 200);
                else if (key == "AutonomousMinimumSettlementFood" && Int32.TryParse(value, out n)) output.AutonomousMinimumSettlementFood = Clamp(n, 1, 500);
                else if (key == "AutonomousCooldownDays" && Int32.TryParse(value, out n)) output.AutonomousCooldownDays = Clamp(n, 2, 60);
                else if (key == "AutonomousMaxDailyOrders" && Int32.TryParse(value, out n)) output.AutonomousMaxDailyOrders = Clamp(n, 1, 20);
                else if (key == "ConvoyCargoGrain" && Int32.TryParse(value, out n)) output.ConvoyCargoGrain = Clamp(n, 1, 100);
                else if (key == "ConvoyGuards" && Int32.TryParse(value, out n)) output.ConvoyGuards = Clamp(n, 2, 60);
                else if (key == "MaxActiveConvoys" && Int32.TryParse(value, out n)) output.MaxActiveConvoys = Clamp(n, 1, 10);
                else if (key == "ConvoyTimeoutDays" && Int32.TryParse(value, out n)) output.ConvoyTimeoutDays = Clamp(n, 5, 90);
                else if (key == "KingdomLevyCostGold" && Int32.TryParse(value, out n)) output.KingdomLevyCostGold = Clamp(n, 50, 20000);
                else if (key == "KingdomLevyCostInfluence" && Int32.TryParse(value, out n)) output.KingdomLevyCostInfluence = Clamp(n, 1, 100);
                else if (key == "KingdomLevyDailyCap" && Int32.TryParse(value, out n)) output.KingdomLevyDailyCap = Clamp(n, 1, 10);
                else if (key == "DiplomaticMissionCostGold" && Int32.TryParse(value, out n)) output.DiplomaticMissionCostGold = Clamp(n, 50, 20000);
                else if (key == "DiplomaticMissionCostInfluence" && Int32.TryParse(value, out n)) output.DiplomaticMissionCostInfluence = Clamp(n, 1, 100);
                else if (key == "DiplomaticMissionCooldownDays" && Int32.TryParse(value, out n)) output.DiplomaticMissionCooldownDays = Clamp(n, 1, 120);
                else if (key == "NpcFoodThreshold" && Int32.TryParse(value, out n)) output.NpcFoodThreshold = Clamp(n, 0, 15);
                else if (key == "MaxDepotInvestment" && Int32.TryParse(value, out n)) output.MaxDepotInvestment = Clamp(n, 0, 10);
                else if (key == "DepotCapacity" && Int32.TryParse(value, out n)) output.DepotCapacity = Clamp(n, 20, 1000);
                else if (key == "RecruitmentMaxPartySize" && Int32.TryParse(value, out n)) output.RecruitmentMaxPartySize = Clamp(n, 10, 1000);
                else if (key == "LocalRecruitDailyCap" && Int32.TryParse(value, out n)) output.LocalRecruitDailyCap = Clamp(n, 1, 20);
                else if (key == "EnableGarrisonTraining" && Boolean.TryParse(value, out b)) output.EnableGarrisonTraining = b;
                else if (key == "GarrisonTrainingDailyCap" && Int32.TryParse(value, out n)) output.GarrisonTrainingDailyCap = Clamp(n, 2, 20);
                else if (key == "GarrisonTrainingMaxTroops" && Int32.TryParse(value, out n)) output.GarrisonTrainingMaxTroops = Clamp(n, 20, 2000);
                else if (key == "EnableAutoGarrisonWageCaps" && Boolean.TryParse(value, out b)) output.EnableAutoGarrisonWageCaps = b;
                else if (key == "EnableManualGarrisonTransfers" && Boolean.TryParse(value, out b)) output.EnableManualGarrisonTransfers = b;
                else if (key == "EnableManualGarrisonWageCaps" && Boolean.TryParse(value, out b)) output.EnableManualGarrisonWageCaps = b;
                else if (key == "EnableAutoGarrisonTraining" && Boolean.TryParse(value, out b)) output.EnableAutoGarrisonTraining = b;
                else if (key == "EnableVillageHarvest" && Boolean.TryParse(value, out b)) output.EnableVillageHarvest = b;
                else if (key == "EnableSurplusEstateSales" && Boolean.TryParse(value, out b)) output.EnableSurplusEstateSales = b;
                else if (key == "EstateTargetDailySurplus" && Int32.TryParse(value, out n)) output.EstateTargetDailySurplus = Clamp(n, 0, 5000);
                else if (key == "EstateOtherCostsReserve" && Int32.TryParse(value, out n)) output.EstateOtherCostsReserve = Clamp(n, 0, 5000);
                else if (key == "EstateMaxDailyGarrisonWage" && Int32.TryParse(value, out n)) output.EstateMaxDailyGarrisonWage = Clamp(n, 250, 10000);
                else if (key == "EstateMinimumDefensiveWage" && Int32.TryParse(value, out n)) output.EstateMinimumDefensiveWage = Clamp(n, 0, 2000);
                else if (key == "EstateTargetGarrisonSize" && Int32.TryParse(value, out n)) output.EstateTargetGarrisonSize = Clamp(n, 10, 300);
                else if (key == "EstateMinimumTrainingGold" && Int32.TryParse(value, out n)) output.EstateMinimumTrainingGold = Clamp(n, 0, 500000);
                else if (key == "EstateHearthsPerGrain" && Int32.TryParse(value, out n)) output.EstateHearthsPerGrain = Clamp(n, 50, 1000);
                else if (key == "EstateMaxHarvestPerDay" && Int32.TryParse(value, out n)) output.EstateMaxHarvestPerDay = Clamp(n, 0, 100);
                else if (key == "EstateGrainReserve" && Int32.TryParse(value, out n)) output.EstateGrainReserve = Clamp(n, 0, 1000);
                else if (key == "EstateMaxDailyGrainSale" && Int32.TryParse(value, out n)) output.EstateMaxDailyGrainSale = Clamp(n, 0, 100);
                else if (key == "EstateSalePricePerGrain" && Int32.TryParse(value, out n)) output.EstateSalePricePerGrain = Clamp(n, 1, 100);
                else if (key == "GrainPerForage" && Int32.TryParse(value, out n)) output.GrainPerForage = Clamp(n, 1, 5);
            }
        }

        /// <summary>
        /// UI changes are saved first, then reflected into this shared settings
        /// instance. Other campaign behaviors observe the change on their next
        /// event without receiving a duplicate event registration.
        /// </summary>
        public bool TrySetMenuValue(string key, string value, string path,
            out string error)
        {
            string normalized;
            if (!SettingsMenuPolicy.TryNormalize(key, value, out normalized))
            {
                error = "This setting is not editable in the in-game menu.";
                return false;
            }
            if(normalized=="true" && IsHeldMutation(key))
            {
                error="Held during compatibility acceptance.";
                return false;
            }
            if(!SettingsCatalog.IsRuntimeEditable(key))
            {
                error="This legacy setting is retained for compatibility, not gameplay.";
                return false;
            }
            try
            {
                SettingsMenuPolicy.WriteOverride(path, key, normalized);
                ApplyLines(this, new[] { key + "=" + normalized });
                if (key == "EnableDiagnostics")
                {
                    DiagnosticJournal.Enabled = EnableDiagnostics;
                    if (EnableDiagnostics) DiagnosticJournal.InstallManagedExceptionHooks();
                }
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = "Unable to save setting: " + ex.GetType().Name;
                return false;
            }
        }

        public bool TrySetMenuValue(string key, string value, out string error)
        {
            return TrySetMenuValue(key, value, UserOverridePath, out error);
        }

        private static int Clamp(int x, int a, int b) { return Math.Min(b, Math.Max(a, x)); }
    }
}
