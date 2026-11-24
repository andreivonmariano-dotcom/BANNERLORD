using System;

namespace CalradiaUnified
{
    /// <summary>
    /// New Outlaw only. The band receives actual native wages, a modest
    /// starting purse, and food. This is a 14-day STARTING buffer, not a
    /// permanent desertion immunity or a made-up recurring revenue stream.
    /// The true native party total wage and capacity are read in-game.
    /// </summary>
    public static class SandboxOutlawBandPolicy
    {
        public const int TargetBandits=6;
        public const int MaximumInitialParty=12;
        public const int PayrollDays=14;
        public const int MinimumCashReserve=750;
        public const int FoodCashReserve=200;
        public const int StartingGrain=12;
        public const int MaximumDailyWage=250;

        public static readonly string[] CulturalBanditFactions=
        {
            "looters","mountain_bandits","forest_bandits",
            "sea_raiders","steppe_bandits","desert_bandits"
        };

        public static string PreferredBanditCulture(string culture)
        {
            switch(culture)
            {
                case "vlandia":return "mountain_bandits";
                case "battania":return "forest_bandits";
                case "sturgia":return "sea_raiders";
                case "khuzait":return "steppe_bandits";
                case "aserai":return "desert_bandits";
                case "empire":return "looters";
                default:return "looters";
            }
        }

        public static int MissingBandits(int existing)
        {
            return existing<0 || existing>MaximumInitialParty?
                -1:Math.Max(0,TargetBandits-existing);
        }

        public static int RequiredStartingGold(int actualDailyWage)
        {
            if(actualDailyWage<0 ||
                actualDailyWage>MaximumDailyWage)return -1;
            return Math.Max(MinimumCashReserve,
                actualDailyWage*PayrollDays+FoodCashReserve);
        }

        public static string ValidateBeforeGrant(
            int existingRegulars,int existingBandits,
            int troopLimit,int actualDailyWage)
        {
            if(existingRegulars<0 || existingBandits<0 ||
                existingBandits>existingRegulars)
                return "invalid_native_roster";
            int add=MissingBandits(existingBandits);
            if(add<0 || existingRegulars+add>MaximumInitialParty)
                return "oversized_native_party";
            if(troopLimit<=0 || existingRegulars+add+2>troopLimit)
                return "party_size_limit_too_small";
            if(RequiredStartingGold(actualDailyWage)<0)
                return "native_wage_outside_starter_band";
            return "";
        }
    }
}
