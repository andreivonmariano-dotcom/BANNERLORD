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
        public static readonly string[] SevenPaths={
            "Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King"
        };
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
                case "Lord":case "NobleCadet":return "Court";
                case "Knight":case "Soldier":case "Mercenary":
                case "Recruit":
                case "HouseholdKnight":case "Admiral":
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
                case "Lord":case "NobleCadet":return "Land";
                case "Knight":case "Soldier":case "Mercenary":
                case "Recruit":
                case "HouseholdKnight":case "Admiral":
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
        public static string Station(SandboxOriginProfile p)
        {
            return p!=null && p.KnightGuidedStart?
                "UnlandedKnightRetainer":Station(p==null?"":p.Role,
                    p==null?"Independent":p.NobleBirthPosition);
        }
        public static string Station(string role,string birth)
        {
            switch(role)
            {
                case "King":return "RulingClan";
                case "Lord":
                    return SandboxNobleStartPolicy.RequiresNobleElder(birth)?
                        "LandedVassalCadet":"LandedVassal";
                case "NobleCadet":return "UnlandedDynasticCadet";
                case "HouseholdKnight":return "UnlandedKnightRetainer";
                case "Recruit":return "ProspectiveEnlistedRecruit";
                case "Knight":return "LandedKnightVassal";
                case "Soldier":case "Mercenary":
                    return "MercenaryCompany";
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
                case "NobleCadet":return "UnlandedNobleRelative";
                case "HouseholdKnight":return "HouseholdKnightRetainer";
                case "Recruit":return "EnlistmentApplicant";
                case "Knight":return "KnightInVassalService";
                case "Soldier":case "Mercenary":
                    return "MercenaryCaptain";
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
            if(role=="Recruit")return "PendingVerifiedEnlistment";
            if(role=="HouseholdKnight")
                return "RecordedPersonalPatronNoFormalVassalage";
            if(role=="NobleCadet")
                return "BirthFamilyMembershipWithoutLandGrant";
            if(role=="Soldier" || role=="Mercenary")
                return "NativeMercenaryContract";
            if(role=="Admiral")return "NativeFleetService";
            if(role=="Trader")return "TradeAndHouseholdDebtsUnmodeled";
            if(role=="Outlaw")return "BandLoyaltyUnmodeled";
            if(role=="Beggar")return "LivelihoodUnmodeled";
            return "NoFormalService";
        }
        public static string BirthLabel(string value)
        {
            if(value=="Independent")return "No specified noble lineage";
            if(value=="SecondSonOfLord")return "Second son of a lord";
            if(value=="ThirdSonOfLord")return "Third son of a lord";
            if(value=="SecondSonOfKing")return "Second son of a king";
            if(value=="ThirdSonOfKing")return "Third son of a king";
            if(value=="YoungerBrotherOfLord")return "Younger brother of a lord";
            if(value=="YoungerSisterOfLord")return "Younger sister of a lord";
            if(value=="YoungerBrotherOfKing")return "Younger brother of a king";
            if(value=="YoungerSisterOfKing")return "Younger sister of a king";
            return "Unrecognized origin";
        }
        public static string StationLabel(string value)
        {
            switch(value)
            {
                case "RulingClan":return "Ruling monarch";
                case "LandedVassalCadet":
                    return "Cadet noble with a separate native vassal holding";
                case "LandedVassal":return "Landed vassal";
                case "UnlandedDynasticCadet":
                    return "Landless noble cadet, independent native clan";
                case "UnlandedKnightRetainer":
                    return "Landless knight with named household patron";
                case "ProspectiveEnlistedRecruit":
                    return "Recruit seeking enlistment, not yet in an NPC party";
                case "LandedKnightVassal":return "Knight holding a native vassal fief";
                case "MercenaryCompany":
                    return "Independent mercenary captain, not an enlisted soldier";
                case "NativeFleetCommander":return "Fleet commander";
                case "IndependentMerchant":return "Independent merchant";
                case "OutlawBand":
                    return "Independent outlaw in a native bandit start";
                case "UnprotectedTraveler":return "Person without formal protection";
                default:return "Free traveler";
            }
        }
        public static string OccupationLabel(string value)
        {
            switch(value)
            {
                case "Ruler":return "Monarch";
                case "VassalLord":return "Vassal lord";
                case "KnightInVassalService":return "Knight in vassal service";
                case "UnlandedNobleRelative":return "Unlanded noble relative";
                case "HouseholdKnightRetainer":return "Household knight applicant";
                case "EnlistmentApplicant":return "Military recruit";
                case "MercenaryCaptain":return "Mercenary captain";
                case "FleetCommander":return "Fleet commander";
                case "Merchant":return "Merchant";
                case "Outlaw":return "Outlaw";
                case "Beggar":return "Beggar";
                default:return "Wanderer";
            }
        }
        public static string ObligationLabel(string value)
        {
            switch(value)
            {
                case "RuleAndProtectKingdom":return "Rule and protect the kingdom";
                case "NativeVassalService":
                    return "Kingdom vassal duties under native Bannerlord";
                case "NativeMercenaryContract":
                    return "Kingdom mercenary contract, no NPC-party enlistment";
                case "PendingVerifiedEnlistment":
                    return "Enlistment requires a verified service-mod action";
                case "RecordedPersonalPatronNoFormalVassalage":
                    return "Personal lord recorded; formal household duty pending";
                case "BirthFamilyMembershipWithoutLandGrant":
                    return "Native ancestry without an estate grant";
                case "NativeFleetService":return "Native fleet command";
                case "TradeAndHouseholdDebtsUnmodeled":
                    return "Trading obligations not yet simulated";
                case "BandLoyaltyUnmodeled":
                    return "Band loyalty not yet simulated";
                case "LivelihoodUnmodeled":
                    return "Personal livelihood not yet simulated";
                default:return "No formal service";
            }
        }
        public static string Dossier(string role,string birth,
            string earlyHousehold,string ambition)
        {
            return "Birth: "+BirthLabel(birth)+"\n"+
                "Social station: "+StationLabel(Station(role,birth))+"\n"+
                "Occupation: "+OccupationLabel(Occupation(role))+"\n"+
                "Early household: "+Household(role,earlyHousehold)+"\n"+
                "Obligations: "+ObligationLabel(Obligation(role,birth))+"\n"+
                "Personal ambition: "+Ambition(role,birth,ambition);
        }
        public static string FamilyScenario(string role,
            string earlyHousehold)
        {
            string home=Household(role,earlyHousehold);
            if(home=="TradingHouse")return "Mercantile";
            if(home=="Garrison")return "Martial";
            // Court upbringing does not automatically imply moral
            // excellence, and displacement does not imply family conflict.
            // Only relevant vocational household traditions are inferred.
            return "Natural";
        }
    }
}
