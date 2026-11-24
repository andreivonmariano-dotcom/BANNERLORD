using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Military and court specialties. Only native party offices are
    /// assigned official gameplay duties.
    /// </summary>
    public static class SandboxRetinueOfficePolicy
    {
        public static string Label(string office)
        {
            switch(office)
            {
                case "MasterOfArms":return "Master of Arms";
                case "RoyalMinister":return "Royal Minister";
                case "HouseholdSteward":return "Household Steward";
                case "FirstMate":return "First Mate";
                case "SecondInCommand":return "Band Lieutenant";
                default:return Skills(office).Length>0?office:"";
            }
        }
        public static string Key(string label)
        {
            if(label=="Band Lieutenant" || label=="Second In Command")return "SecondInCommand";
            if(label=="Master of Arms" || label=="Master ofArms")return "MasterOfArms";
            string key=(label??"").Replace(" ","");
            return Skills(key).Length>0?key:"";
        }
        public static string LegacyOffice(string name)
        {
            if(String.IsNullOrEmpty(name) || !name.EndsWith(")",StringComparison.Ordinal))return "";
            int start=name.LastIndexOf(" (",StringComparison.Ordinal);
            return start<1?"":Key(name.Substring(start+2,name.Length-start-3));
        }
        public static string[] WandererSupportSkills(string specialty)
        {
            switch(specialty)
            {
                case "Guard":return new[]{"Athletics","Polearm"};
                case "Scout":return new[]{"Riding","Bow"};
                case "Healer":return new[]{"Steward","Athletics"};
                case "Steward":return new[]{"Trade","Charm"};
                case "Trader":return new[]{"Steward","Charm"};
                default:return new string[0];
            }
        }
        public static int TrainingTarget(string office,int rank)
        {
            if(rank<0)return 0;
            if(office=="Squire")return rank==0?100:rank==1?85:rank==2?75:55;
            if(office=="Sergeant" || office=="SecondInCommand")return rank==0?125:rank==1?110:rank==2?90:65;
            return rank==0?150:rank==1?130:rank==2?110:75;
        }
        public static string[] Skills(string office)
        {
            switch(office)
            {
                case "Knight":return new[]{"Riding","Polearm","OneHanded"};
                case "MasterOfArms":return new[]{"OneHanded","Leadership","Tactics"};
                case "Marshal":return new[]{"Leadership","Tactics","Riding"};
                case "HouseholdSteward":return new[]{"Steward","Trade","Charm"};
                case "Chamberlain":return new[]{"Steward","Charm","Trade"};
                case "RoyalMinister":return new[]{"Steward","Charm","Trade"};
                case "Kingsguard":return new[]{"OneHanded","Athletics","Riding"};
                case "Squire":return new[]{"Riding","Athletics","OneHanded"};
                case "FirstMate":return new[]{"Leadership","Tactics","Steward"};
                case "Navigator":return new[]{"Scouting","Tactics","Riding"};
                case "Sergeant":return new[]{"Leadership","Athletics","OneHanded"};
                case "Factor":return new[]{"Trade","Steward","Charm"};
                case "SecondInCommand":return new[]{"Tactics","Roguery","Leadership"};
                default:return new string[0];
            }
        }
        public static bool IsFieldOfficer(string office)
        {
            return office=="Knight" || office=="MasterOfArms" ||
                office=="Marshal" || office=="Kingsguard" ||
                office=="Squire" ||
                office=="FirstMate" || office=="Sergeant" ||
                office=="SecondInCommand";
        }
        public static string PartyDuty(string office)
        {
            if(office=="HouseholdSteward" || office=="Chamberlain" ||
                office=="Factor")return "Quartermaster";
            if(office=="Navigator")return "Scout";
            return "None";
        }
    }
    public sealed class SandboxRetinueOfficeState
    {
        private const string Version="CU_RETINUE_OFFICES_1";
        private readonly Dictionary<string,string> offices=new Dictionary<string,string>(StringComparer.Ordinal);
        public string OfficeFor(string id)
        {
            string office;
            return id!=null && offices.TryGetValue(id,out office)?office:"";
        }
        public bool Register(string id,string office)
        {
            if(String.IsNullOrWhiteSpace(id) || id.Length>128 ||
                !id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-') ||
                SandboxRetinueOfficePolicy.Skills(office).Length==0)return false;
            string previous=OfficeFor(id);
            if(previous.Length>0)return previous==office;
            if(offices.Count>=32)return false;
            offices.Add(id,office);return true;
        }
        public string Serialize()
        {
            return Version+String.Concat(offices.OrderBy(x=>x.Key,StringComparer.Ordinal)
                .Select(x=>"\n"+x.Key+"|"+x.Value));
        }
        public static SandboxRetinueOfficeState Parse(string text)
        {
            var empty=new SandboxRetinueOfficeState();
            if(String.IsNullOrEmpty(text)||text.Length>8192)return empty;
            var lines=text.Replace("\r","").Split('\n');
            if(lines[0]!=Version || lines.Length>33)return empty;
            var state=new SandboxRetinueOfficeState();
            foreach(var line in lines.Skip(1))
            {
                var f=line.Split('|');
                if(f.Length!=2 || state.OfficeFor(f[0]).Length>0 || !state.Register(f[0],f[1]))return empty;
            }
            return state;
        }
    }
}
