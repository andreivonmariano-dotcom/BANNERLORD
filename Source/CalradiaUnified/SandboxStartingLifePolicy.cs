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
        // Guidance only: never a grant of native rank, assets or authority.
        public static string CareerContract(string path)
        {
            switch(path)
            {
                case "Wanderer": return "Pursue livelihood, travel, family and patronage; no formal service required.";
                case "Outlaw": return "Maintain your band and resources; criminal consequences remain native or Fourberie-owned.";
                case "Mercenary": return "Honor native paid contracts, maintain your company and negotiate release.";
                case "Soldier": return "Serve your named lord; enlistment, rank, pay and release belong to Serve as Soldier.";
                case "Knight": return "Fulfill the personal oath, muster and escort duties; no automatic vassalage or land.";
                case "Lord": return "Maintain your noble house and estates; native vassalage and ownership control political rights.";
                case "King": return "Protect the kingdom and convene your court; native rulership controls the crown.";
                default: return "";
            }
        }
        // Independent dimensions: a fief or kinship is not political office.
        public static string PoliticalStanding(bool clanLeader,
            bool inKingdom,bool rulingClan,bool mercenary)
        {
            if(!inKingdom)
                return rulingClan || mercenary ? "Unverified political status" :
                    "Independent clan";
            if(rulingClan && mercenary)return "Unverified political status";
            if(rulingClan)return clanLeader?"Native ruler":"Ruling-clan member";
            if(mercenary)return "Native mercenary clan";
            return clanLeader?"Native kingdom clan leader":"Native kingdom clan member";
        }

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
        public static string Station(SandboxOriginProfile p,
            bool verifiedSoldierStart=false)
        {
            if(p!=null && p.Role=="Knight" && p.KnightGuidedStart)
                return "UnlandedKnightRetainer";
            if(p!=null && p.Role=="Soldier" && p.SoldierGuidedStart)
                return verifiedSoldierStart?"VerifiedEnlistedSoldier":
                    "ProspectiveEnlistedRecruit";
            return Station(p==null?"":p.Role,
                p==null?"Independent":p.NobleBirthPosition);
        }
        public static string Occupation(SandboxOriginProfile p,
            bool verifiedSoldierStart=false)
        {
            if(p!=null && p.Role=="Knight" && p.KnightGuidedStart)
                return "HouseholdKnightRetainer";
            if(p!=null && p.Role=="Soldier" && p.SoldierGuidedStart)
                return verifiedSoldierStart?"VerifiedEnlistedSoldier":
                    "EnlistmentApplicant";
            return Occupation(p==null?"":p.Role);
        }
        public static string Obligation(SandboxOriginProfile p)
        {
            if(p!=null && p.Role=="Knight" && p.KnightGuidedStart)
                return "RecordedPersonalPatronNoFormalVassalage";
            if(p!=null && p.Role=="Soldier" && p.SoldierGuidedStart)
                return "PendingVerifiedEnlistment";
            return Obligation(p==null?"":p.Role,
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
                    return "Recruit seeking verified enlistment";
                case "VerifiedEnlistedSoldier":
                    return "Native enlisted soldier at verified start";
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
                case "VerifiedEnlistedSoldier":return "Enlisted soldier at verified start";
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
