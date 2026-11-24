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
        public static int StartingAge(string office)
        {
            switch(office)
            {
                case "Squire":return 19;
                case "Knight":return 30;
                case "Kingsguard":return 34;
                case "MasterOfArms":return 42;
                case "Marshal":return 48;
                case "RoyalMinister":return 45;
                case "Chamberlain":return 40;
                case "HouseholdSteward":return 39;
                case "Factor":return 36;
                case "Sergeant":return 35;
                case "SecondInCommand":return 33;
                case "FirstMate":return 34;
                case "Navigator":return 32;
                default:return 27;
            }
        }
        public static int RandomAge(string office,Random random)
        {
            int age=StartingAge(office);
            return random==null?age:office=="Squire"?random.Next(18,22):
                age+random.Next(-3,4);
        }
        public static int RandomSkill(string office,int rank,Random random)
        {
            int target=TrainingTarget(office,rank);
            return rank<0 || random==null?target:target+random.Next(-5,6);
        }
        public static int StartingLevel(string office)
        {
            if(office=="Squire")return 10;
            if(office=="Marshal" || office=="MasterOfArms" || office=="RoyalMinister")return 24;
            if(office=="Sergeant" || office=="SecondInCommand")return 17;
            return 20;
        }
        public static int FocusTarget(int rank)
        {
            return rank<0?0:rank==0?4:rank==1?3:rank==2?2:1;
        }
        public static string[] AttributesForOffice(string office)
        {
            if(office=="RoyalMinister" || office=="HouseholdSteward" || office=="Chamberlain" || office=="Factor")
                return new[]{"Intelligence","Social"};
            if(office=="Marshal" || office=="Sergeant" || office=="FirstMate")
                return new[]{"Social","Cunning"};
            if(office=="SecondInCommand" || office=="Navigator")return new[]{"Cunning","Endurance"};
            return new[]{"Vigor","Endurance"};
        }
        // Desired average native item tier (not a fabricated item ID).
        public static int KitTier(string office,bool civilian)
        {
            if(office=="Squire" || office=="SecondInCommand")return 2;
            if(office=="Marshal" || office=="Kingsguard" || office=="Knight" ||
                office=="MasterOfArms")return civilian?3:4;
            if(office=="RoyalMinister" || office=="Chamberlain")return civilian?4:2;
            if(office=="HouseholdSteward" || office=="Factor" || office=="Trader" ||
                office=="Steward" || office=="Healer")return 2;
            return 3;
        }
        public static string OfficeBackground(string office)
        {
            switch(office)
            {
                case "Squire":return "A young attendant entrusted with learning arms and mounted service.";
                case "Knight":return "A sworn household fighter trained for mounted and personal service.";
                case "Marshal":return "A senior officer entrusted with the ordering of the household's forces.";
                case "Kingsguard":return "A personal guard charged with protecting the sovereign.";
                case "MasterOfArms":return "A martial instructor charged with the readiness of the household.";
                case "RoyalMinister":return "An administrator entrusted with the affairs of the royal household.";
                case "SecondInCommand":return "A lieutenant entrusted with keeping the band together.";
                case "Navigator":return "A guide charged with finding safe routes.";
                case "FirstMate":return "An officer charged with the crew's readiness.";
                case "Sergeant":return "An officer charged with discipline and company readiness.";
                case "HouseholdSteward":case "Chamberlain":case "Factor":
                    return "An officer entrusted with the household's stores and accounts.";
                default:return "";
            }
        }
        public static int TrainingTarget(string office,int rank)
        {
            if(rank<0)return 0;
            if(office=="Squire")return rank==0?100:rank==1?85:rank==2?75:55;
            if(office=="Marshal" || office=="MasterOfArms" || office=="RoyalMinister")
                return rank==0?175:rank==1?150:rank==2?125:80;
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
        public bool HasRecords {get{return offices.Count>0;}}
        public bool HasIdentity(string id){return id!=null && offices.ContainsKey(id);}
        public string[] ActiveIds(){return offices.Where(x=>x.Value!="Revoked").Select(x=>x.Key).ToArray();}
        public bool Revoke(string id)
        {
            if(!HasIdentity(id) || offices[id]=="Revoked")return false;
            offices[id]="Revoked";return true;
        }
        public string OfficeFor(string id)
        {
            string office;
            return id!=null && offices.TryGetValue(id,out office) && office!="Revoked"?office:"";
        }
        public bool Register(string id,string office)
        {
            if(String.IsNullOrWhiteSpace(id) || id.Length>128 ||
                !id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-') ||
                SandboxRetinueOfficePolicy.Skills(office).Length==0)return false;
            if(HasIdentity(id))return offices[id]==office;
            if(offices.Count>=32)return false;
            offices.Add(id,office);return true;
        }
        // Replacement changes only a CU household designation. It never
        // assigns a native occupation, party role, governorship or estate.
        public bool Replace(string oldId,string newId,string office)
        {
            if(oldId==newId || OfficeFor(oldId)!=office || HasIdentity(newId) ||
                ActiveIds().Count(id=>OfficeFor(id)==office)!=1)return false;
            if(!Register(newId,office))return false;
            return Revoke(oldId);
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
                if(f.Length!=2 || state.HasIdentity(f[0]))return empty;
                if(f[1]=="Revoked")
                {
                    if(!state.Register(f[0],"Knight"))return empty;
                    state.Revoke(f[0]);
                }
                else if(!state.Register(f[0],f[1]))return empty;
            }
            return state;
        }
    }
}
