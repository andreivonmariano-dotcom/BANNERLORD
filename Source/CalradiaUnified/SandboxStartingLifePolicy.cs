using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Social position describes what the native start actually provides.
    /// It does not assign an unlanded person a fictional fief or invent
    /// rank-and-file enlistment into a lord's army.
    /// </summary>
    public static class SandboxStartingLifePolicy
    {
        public static readonly string[] EarlyHouseholds={
            "Auto","Rural","Town","Garrison","TradingHouse","Court","Displaced"
        };
        public static readonly string[] Ambitions={
            "Auto","Duty","Recognition","Security","Prosperity",
            "Independence","Land","Reconciliation"
        };
        public static bool ValidHousehold(string value)
        {
            return Array.IndexOf(EarlyHouseholds,value)>=0;
        }
        public static bool ValidAmbition(string value)
        {
            return Array.IndexOf(Ambitions,value)>=0;
        }
        public static string DefaultHousehold(string role)
        {
            switch(role)
            {
                case "King":return "Court";
                case "Lord":return "Court";
                case "Knight":case "Soldier":case "Admiral":
                    return "Garrison";
                case "Trader":return "TradingHouse";
                case "Outlaw":case "Beggar":return "Displaced";
                default:return "Rural";
            }
        }
        public static string Household(string role,string requested)
        {
            return requested=="Auto" || !ValidHousehold(requested)?
                DefaultHousehold(role):requested;
        }
        public static string DefaultAmbition(string role,string birth)
        {
            if(SandboxNobleStartPolicy.IsSonOrigin(birth))
                return "Recognition";
            switch(role)
            {
                case "King":return "Duty";
                case "Lord":return "Land";
                case "Knight":case "Soldier":case "Admiral":
                    return "Recognition";
                case "Trader":return "Prosperity";
                case "Outlaw":return "Independence";
                case "Beggar":return "Security";
                default:return "Independence";
            }
        }
        public static string Ambition(string role,string birth,
            string requested)
        {
            return requested=="Auto" || !ValidAmbition(requested)?
                DefaultAmbition(role,birth):requested;
        }
        public static string Station(string role,string birth)
        {
            switch(role)
            {
                case "King":return "RulingClan";
                case "Lord":
                    return SandboxNobleStartPolicy.RequiresNobleElder(birth)?
                        "LandedVassalCadet":"LandedVassal";
                case "Knight":return "LandedKnightVassal";
                case "Soldier":return "MercenaryCompany";
                case "Admiral":return "NativeFleetCommander";
                case "Trader":return "IndependentMerchant";
                case "Outlaw":return "OutlawBand";
                case "Beggar":return "UnprotectedTraveler";
                default:return "FreeTraveler";
            }
        }
        public static string Occupation(string role)
        {
            switch(role)
            {
                case "King":return "Ruler";
                case "Lord":return "VassalLord";
                case "Knight":return "KnightInVassalService";
                case "Soldier":return "MercenaryCaptain";
                case "Admiral":return "FleetCommander";
                case "Trader":return "Merchant";
                case "Outlaw":return "Outlaw";
                case "Beggar":return "Beggar";
                default:return "Wanderer";
            }
        }
        public static string Obligation(string role,string birth)
        {
            if(role=="King")return "RuleAndProtectKingdom";
            if(role=="Lord" || role=="Knight")
                return "NativeVassalService";
            if(role=="Soldier")return "NativeMercenaryContract";
            if(role=="Admiral")return "NativeFleetService";
            if(role=="Trader")return "TradeAndHouseholdDebtsUnmodeled";
            if(role=="Outlaw")return "BandLoyaltyUnmodeled";
            if(role=="Beggar")return "LivelihoodUnmodeled";
            return "NoFormalService";
        }
        public static string Dossier(string role,string birth,
            string earlyHousehold,string ambition)
        {
            return "Birth: "+(birth=="Independent"?
                "No specified noble lineage":birth)+"\n"+
                "Social station: "+Station(role,birth)+"\n"+
                "Occupation: "+Occupation(role)+"\n"+
                "Early household: "+Household(role,earlyHousehold)+"\n"+
                "Obligations: "+Obligation(role,birth)+"\n"+
                "Personal ambition: "+Ambition(role,birth,ambition);
        }
        public static string FamilyScenario(string role,
            string earlyHousehold)
        {
            string home=Household(role,earlyHousehold);
            if(home=="TradingHouse")return "Mercantile";
            if(home=="Garrison")return "Martial";
            if(home=="Court")return "Honorable";
            if(home=="Displaced")return "Strained";
            return "Natural";
        }
    }
}
