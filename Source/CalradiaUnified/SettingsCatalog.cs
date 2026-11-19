using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CalradiaUnified
{
    /// <summary>One audited catalog for native menu options, value bounds and test coverage.</summary>
    public sealed class SettingDescriptor
    {
        public readonly string Key;
        public readonly string Label;
        public readonly string Group;
        public readonly bool Boolean;
        public readonly int Min;
        public readonly int Max;
        public readonly int Step;
        public readonly bool Experimental;
        public SettingDescriptor(string key, string label, string group, bool boolean,
            int min, int max, int step, bool experimental)
        {
            Key = key;
            Label = label;
            Group = group;
            Boolean = boolean;
            Min = min;
            Max = max;
            Step = step;
            Experimental = experimental;
        }
    }

    public static class SettingsCatalog
    {
        public static readonly IList<SettingDescriptor> All =
            Array.AsReadOnly(new SettingDescriptor[]
        {
            new SettingDescriptor("EnableDiagnostics", "Diagnostics", "diagnostics", true, 0, 0, 0, false),
            new SettingDescriptor("AutoSessionReports", "Automatic Session ZIP Reports", "diagnostics", true, 0, 0, 0, false),
            new SettingDescriptor("EnableSupplyWarnings", "Supply Warnings", "supply", true, 0, 0, 0, false),
            new SettingDescriptor("EnableArmyWarnings", "Army Warnings", "supply", true, 0, 0, 0, false),
            new SettingDescriptor("EnablePlayerForaging", "Player Foraging", "supply", true, 0, 0, 0, true),
            new SettingDescriptor("EnableCivicSystems", "Civic Systems", "general", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAbstractShipments", "Abstract Shipments", "economy", true, 0, 0, 0, true),
            new SettingDescriptor("EnableEconomy", "Economy", "economy", true, 0, 0, 0, true),
            new SettingDescriptor("EnableRecruitment", "Recruitment", "recruitment", true, 0, 0, 0, true),
            new SettingDescriptor("EnableLocalDiplomacy", "Local Diplomacy", "kingdom", true, 0, 0, 0, true),
            new SettingDescriptor("EnableNpcAdvisory", "Npc Advisory", "clan", true, 0, 0, 0, false),
            new SettingDescriptor("EnablePhysicalConvoys", "Physical Convoys", "convoys", true, 0, 0, 0, true),
            new SettingDescriptor("EnableDepotTrade", "Depot Trade", "economy", true, 0, 0, 0, true),
            new SettingDescriptor("EnableKingdomLevies", "Kingdom Levies", "kingdom", true, 0, 0, 0, true),
            new SettingDescriptor("EnableKingdomDiplomacy", "Kingdom Diplomacy", "kingdom", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAutonomousClanResupply", "Autonomous Clan Resupply", "clan", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAutonomousClanRecruitTrips", "Autonomous Clan Recruit Trips", "clan", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAutonomousClanPatrol", "Autonomous Clan Patrol", "clan", true, 0, 0, 0, true),
            new SettingDescriptor("EnableGarrisonTraining", "Garrison Training", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAutoGarrisonWageCaps", "Auto Garrison Wage Caps", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("EnableManualGarrisonTransfers", "Manual Garrison Transfers", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("EnableManualGarrisonWageCaps", "Manual Garrison Wage Caps", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("EnableAutoGarrisonTraining", "Auto Garrison Training", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("EnableVillageHarvest", "Village Harvest", "economy", true, 0, 0, 0, true),
            new SettingDescriptor("EnableSurplusEstateSales", "Surplus Estate Sales", "castles", true, 0, 0, 0, true),
            new SettingDescriptor("SupplyWarningDays", "Supply Warning Days", "supply", false, 0, 15, 1, false),
            new SettingDescriptor("ForageWhenDaysAtMost", "Forage When Days At Most", "supply", false, 0, 15, 1, false),
            new SettingDescriptor("ForageEveryDays", "Forage Every Days", "supply", false, 1, 90, 5, false),
            new SettingDescriptor("GrainPerForage", "Grain Per Forage", "supply", false, 1, 5, 1, false),
            new SettingDescriptor("ConvoyCargoGrain", "Convoy Cargo Grain", "convoys", false, 1, 100, 5, false),
            new SettingDescriptor("ConvoyGuards", "Convoy Guards", "convoys", false, 2, 60, 5, false),
            new SettingDescriptor("MaxActiveConvoys", "Max Active Convoys", "convoys", false, 1, 10, 1, false),
            new SettingDescriptor("ConvoyTimeoutDays", "Convoy Timeout Days", "convoys", false, 5, 90, 5, false),
            new SettingDescriptor("MaxDailyCouncilReports", "Max Daily Council Reports", "clan", false, 0, 20, 1, false),
            new SettingDescriptor("CouncilPreferredPartySize", "Council Preferred Party Size", "clan", false, 1, 500, 25, false),
            new SettingDescriptor("AutonomousPatrolMinimumTroops", "Autonomous Patrol Minimum Troops", "clan", false, 20, 200, 25, false),
            new SettingDescriptor("AutonomousLowFoodDays", "Autonomous Low Food Days", "clan", false, 0, 10, 1, false),
            new SettingDescriptor("AutonomousRecruitBelow", "Autonomous Recruit Below", "clan", false, 10, 200, 25, false),
            new SettingDescriptor("AutonomousMinimumSettlementFood", "Autonomous Minimum Settlement Food", "clan", false, 1, 500, 25, false),
            new SettingDescriptor("AutonomousCooldownDays", "Autonomous Cooldown Days", "clan", false, 2, 60, 5, false),
            new SettingDescriptor("AutonomousMaxDailyOrders", "Autonomous Max Daily Orders", "clan", false, 1, 20, 1, false),
            new SettingDescriptor("KingdomLevyCostGold", "Kingdom Levy Cost Gold", "kingdom", false, 50, 20000, 500, false),
            new SettingDescriptor("KingdomLevyCostInfluence", "Kingdom Levy Cost Influence", "kingdom", false, 1, 100, 5, false),
            new SettingDescriptor("KingdomLevyDailyCap", "Kingdom Levy Daily Cap", "kingdom", false, 1, 10, 1, false),
            new SettingDescriptor("DiplomaticMissionCostGold", "Diplomatic Mission Cost Gold", "kingdom", false, 50, 20000, 500, false),
            new SettingDescriptor("DiplomaticMissionCostInfluence", "Diplomatic Mission Cost Influence", "kingdom", false, 1, 100, 5, false),
            new SettingDescriptor("DiplomaticMissionCooldownDays", "Diplomatic Mission Cooldown Days", "kingdom", false, 1, 120, 5, false),
            new SettingDescriptor("NpcFoodThreshold", "Npc Food Threshold", "clan", false, 0, 15, 1, false),
            new SettingDescriptor("MaxDepotInvestment", "Max Depot Investment", "economy", false, 0, 10, 1, false),
            new SettingDescriptor("DepotCapacity", "Depot Capacity", "economy", false, 20, 1000, 25, false),
            new SettingDescriptor("RecruitmentMaxPartySize", "Recruitment Max Party Size", "recruitment", false, 10, 1000, 25, false),
            new SettingDescriptor("LocalRecruitDailyCap", "Local Recruit Daily Cap", "recruitment", false, 1, 20, 1, false),
            new SettingDescriptor("GarrisonTrainingDailyCap", "Garrison Training Daily Cap", "castles", false, 2, 20, 1, false),
            new SettingDescriptor("GarrisonTrainingMaxTroops", "Garrison Training Max Troops", "castles", false, 20, 2000, 100, false),
            new SettingDescriptor("EstateTargetDailySurplus", "Estate Target Daily Surplus", "castles", false, 0, 5000, 100, false),
            new SettingDescriptor("EstateOtherCostsReserve", "Estate Other Costs Reserve", "castles", false, 0, 5000, 100, false),
            new SettingDescriptor("EstateMaxDailyGarrisonWage", "Estate Max Daily Garrison Wage", "castles", false, 250, 10000, 500, false),
            new SettingDescriptor("EstateMinimumDefensiveWage", "Estate Minimum Defensive Wage", "castles", false, 0, 2000, 100, false),
            new SettingDescriptor("EstateTargetGarrisonSize", "Estate Target Garrison Size", "castles", false, 10, 300, 25, false),
            new SettingDescriptor("EstateMinimumTrainingGold", "Estate Minimum Training Gold", "castles", false, 0, 500000, 500, false),
            new SettingDescriptor("EstateHearthsPerGrain", "Estate Hearths Per Grain", "castles", false, 50, 1000, 25, false),
            new SettingDescriptor("EstateMaxHarvestPerDay", "Estate Max Harvest Per Day", "castles", false, 0, 100, 5, false),
            new SettingDescriptor("EstateGrainReserve", "Estate Grain Reserve", "castles", false, 0, 1000, 25, false),
            new SettingDescriptor("EstateMaxDailyGrainSale", "Estate Max Daily Grain Sale", "castles", false, 0, 100, 5, false),
            new SettingDescriptor("EstateSalePricePerGrain", "Estate Sale Price Per Grain", "castles", false, 1, 100, 5, false),
        });

        public static readonly string[] Groups =
            new[] { "general", "diagnostics", "supply", "economy",
                "recruitment", "kingdom", "clan", "convoys", "castles" };

        public static SettingDescriptor Find(string key)
        {
            return All.FirstOrDefault(x => x.Key == key);
        }

        public static string GroupLabel(string id)
        {
            switch (id)
            {
                case "general": return "General";
                case "diagnostics": return "Diagnostics";
                case "supply": return "Supply and foraging";
                case "clan": return "Clan AI and reports";
                case "convoys": return "Physical convoys";
                case "kingdom": return "Kingdom and diplomacy";
                case "castles": return "Castles and estates";
                case "recruitment": return "Recruitment";
                case "economy": return "Depots and economy";
                default: return "Miscellaneous";
            }
        }

        public static string Read(Settings settings, SettingDescriptor option)
        {
            if (settings == null || option == null) return "";
            var field = typeof(Settings).GetField(option.Key,
                BindingFlags.Public | BindingFlags.Instance);
            if (field == null ||
                field.FieldType != (option.Boolean ? typeof(bool) : typeof(int)))
                return "";
            object value = field.GetValue(settings);
            return option.Boolean ? ((bool)value ? "true" : "false")
                : ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryNormalize(string key, string raw, out string normalized)
        {
            normalized = null;
            if (String.IsNullOrEmpty(key) || raw == null) return false;
            var option = Find(key);
            if (option == null) return false;
            if (option.Boolean)
            {
                bool b;
                if (!Boolean.TryParse(raw, out b)) return false;
                normalized = b ? "true" : "false";
                return true;
            }
            int n;
            if (!Int32.TryParse(raw, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out n) ||
                n < option.Min || n > option.Max) return false;
            normalized = n.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public static int Next(int value, SettingDescriptor option, int direction,
            int factor)
        {
            if (option == null || option.Boolean) return value;
            long step = (long)option.Step * Math.Max(1, Math.Min(10, factor));
            long candidate = (long)value + (direction > 0 ? step : -step);
            return (int)Math.Max(option.Min, Math.Min(option.Max, candidate));
        }
    }
}
