using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure design contract for honest born-noble starts and a cultural
    /// starting retinue. Titles are never assigned to unrelated NPCs.
    /// Native kingdom status, parent links and clan membership are
    /// validated by SandboxDynasticStart before any mutation.
    /// </summary>
    public static class SandboxNobleStartPolicy
    {
        public static readonly string[] BirthPositions = {
            "Independent",
            "YoungerBrotherOfLord",
            "YoungerSisterOfLord",
            "SecondSonOfLord",
            "ThirdSonOfLord",
            "YoungerBrotherOfKing",
            "YoungerSisterOfKing",
            "SecondSonOfKing",
            "ThirdSonOfKing"
        };
        public static readonly string[] RetinueModes = {
            "Native", "RoleBased", "ReplaceNative", "FamilyOnly"
        };
        public static bool IsPosition(string value)
        {
            return Array.IndexOf(BirthPositions,value)>=0;
        }
        public static bool IsRetinueMode(string value)
        {
            return Array.IndexOf(RetinueModes,value)>=0;
        }
        public static bool RequiresNobleElder(string value)
        {
            return value!="Independent" && IsPosition(value);
        }
        public static bool IsSonOrigin(string value)
        {
            return value!=null && (value.StartsWith("SecondSon",
                StringComparison.Ordinal) || value.StartsWith("ThirdSon",
                StringComparison.Ordinal));
        }
        public static bool RoyalElder(string value)
        {
            return value!=null && value.EndsWith("OfKing",
                StringComparison.Ordinal);
        }
        public static bool RequiresFemalePlayer(string value)
        {
            return value!=null && value.Contains("Sister");
        }
        public static bool RequiresMalePlayer(string value)
        {
            return RequiresNobleElder(value) && !RequiresFemalePlayer(value);
        }
        public static int RequiredOlderSons(string value)
        {
            if(value!=null && value.StartsWith("ThirdSon",
                StringComparison.Ordinal))return 2;
            if(value!=null && value.StartsWith("SecondSon",
                StringComparison.Ordinal))return 1;
            return 0;
        }
        public static bool ValidForRole(string position,string role)
        {
            if(position=="Independent")return role!="NobleCadet";
            return SandboxCustomLifeRolePolicy.IsNobleFamilyRole(role);
        }
        public static bool CanBindElder(string position,string role,
            bool playerFemale,bool older,bool bothParents,
            bool sameCulture,bool sameKingdom,bool leaderOfClan,
            bool royalLeader,int existingOlderSons)
        {
            if(!RequiresNobleElder(position) ||
                !ValidForRole(position,role) || !older ||
                !bothParents || !sameCulture || !sameKingdom ||
                !leaderOfClan || royalLeader!=RoyalElder(position))
                return false;
            if(RequiresFemalePlayer(position)!=playerFemale)
                return false;
            return existingOlderSons>=RequiredOlderSons(position);
        }
        public static string[] RetinueOffices(string role,bool brotherAvailable)
        {
            switch(role)
            {
                case "King":return brotherAvailable ?
                    new[]{"Knight","Chamberlain"} :
                    new[]{"Marshal","MasterOfArms"};
                // A brother is kin, not an automatic replacement for
                // professional household officers. Every Lord begins
                // with a martial master and a sworn household knight.
                case "Lord":return new[]{"MasterOfArms","Knight"};
                case "Knight":return new[]{"Squire"};
                case "Admiral":return new[]{"FirstMate","Navigator"};
                case "Soldier":return new[]{"Sergeant"};
                case "Trader":return new[]{"Factor"};
                case "Outlaw":return new[]{"SecondInCommand"};
                default:return new string[0];
            }
        }
        public static bool EligibleForRetinue(string mode,string role)
        {
            return mode!="Native" && IsRetinueMode(mode) &&
                RetinueOffices(role,false).Length>0;
        }
        public static string CultureTitle(string culture,string office)
        {
            string prefix= culture=="vlandia"?"Ser " :
                culture=="sturgia"?"Druzhinnik " :
                culture=="battania"?"Champion " :
                culture=="khuzait"?"Noyan " :
                culture=="aserai"?"Sayyid " :
                culture=="empire"?"Tribune " : "";
            return prefix+office.Replace("Of"," of ");
        }
    }
}
