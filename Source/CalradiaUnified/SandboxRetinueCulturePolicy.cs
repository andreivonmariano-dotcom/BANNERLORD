using System;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Six distinct native culture profiles for freshly created officers.
    /// Names and equipment come from the player's existing game culture.
    /// A title is not confused with an engine-granted army commission.
    /// </summary>
    public static class SandboxRetinueCulturePolicy
    {
        public static string GearStyle(string culture,string office)
        {
            if(office=="Navigator")return "Scout";
            if(office=="FirstMate")return "Boarding";
            if(office=="Kingsguard")return "Shieldguard";
            if(office=="Sergeant" || office=="ManAtArms")
                return culture=="sturgia"?"Axeguard":culture=="battania"?"Longbow":
                    culture=="aserai"?"Skirmisher":"Infantry";
            if(office=="SecondInCommand")return "Bandit";
            if(office=="Marshal")return "Commander";
            if(!SandboxRetinueOfficePolicy.IsFieldOfficer(office))
                return "Court";
            switch(culture)
            {
                case "vlandia":return "Lancer";
                case "sturgia":return "Axeguard";
                case "battania":return "Longbow";
                case "empire":return "Shieldguard";
                case "khuzait":return "HorseArcher";
                case "aserai":return "Skirmisher";
                default:return "Guard";
            }
        }

        public static string[] Training(string culture,string office)
        {
            string[] role=SandboxRetinueOfficePolicy.Skills(office);
            if(role.Length==0)return role;
            if(office=="FirstMate")return role.Concat(new[]{"OneHanded","Athletics","Boatswain"}).Distinct().ToArray();
            if(office=="Navigator")return role.Concat(new[]{"Mariner","Shipmaster"}).Distinct().ToArray();
            if(!SandboxRetinueOfficePolicy.IsFieldOfficer(office))
                return role.Concat(new[]{"OneHanded","Riding"}).Distinct().ToArray();
            string cultureWeapon=culture=="sturgia"?"TwoHanded":
                culture=="battania"?"Bow":
                culture=="khuzait"?"Bow":
                culture=="aserai"?"Throwing":
                culture=="vlandia"?"Polearm":"OneHanded";
            if(office=="Knight" || office=="Squire")
                return new[]{"Riding",cultureWeapon}.Concat(role).Concat(new[]{"OneHanded","Athletics"}).Distinct().ToArray();
            return role.Concat(new[]{cultureWeapon,"OneHanded","Athletics"}).Distinct().ToArray();
        }

        public static string PrimaryTrait(string culture,string office)
        {
            if(office=="SecondInCommand" || office=="RoyalMinister" || office=="Marshal")return "Calculating";
            if(office=="Squire" || office=="Kingsguard")return "Valor";
            if(office=="MasterOfArms")return "Honor";
            switch(culture)
            {
                case "vlandia":return "Honor";
                case "sturgia":return "Valor";
                case "battania":return "Mercy";
                case "empire":return "Honor";
                case "khuzait":return "Calculating";
                case "aserai":return "Generosity";
                default:return "Honor";
            }
        }

        public static string SecondaryTrait(string culture,string office)
        {
            if(office=="SecondInCommand")return "Valor";
            if(office=="Squire")return "Mercy";
            if(office=="Kingsguard" || office=="Marshal")return "Honor";
            if(office=="RoyalMinister")return "Generosity";
            if(office=="MasterOfArms")return "Calculating";
            if(!SandboxRetinueOfficePolicy.IsFieldOfficer(office))
                return "Calculating";
            switch(culture)
            {
                case "sturgia":return "Generosity";
                case "empire":return "Calculating";
                case "aserai":return "Honor";
                default:return "Valor";
            }
        }

        public static int TraitLevel(string trait,string culture,string office)
        {
            if(office=="Squire")return 1;
            if(office=="Marshal" && trait=="Calculating" || office=="RoyalMinister" && trait=="Calculating" ||
                office=="Kingsguard" && trait=="Honor")return 2;
            if(office=="SecondInCommand" && trait=="Calculating")
                return 2;
            if(trait=="Valor" && culture=="sturgia")return 2;
            if(trait=="Honor" && culture=="vlandia")return 2;
            if(trait=="Mercy" && culture=="battania")return 2;
            if(trait=="Generosity" && culture=="aserai")return 2;
            if(trait=="Calculating" && culture=="khuzait")return 2;
            return 1;
        }

        /// <summary>
        /// Evaluate categories taken from actual installed native ItemObjects,
        /// not guessed item IDs. Higher score means a better job-specific kit.
        /// </summary>
        public static int EquipmentScore(string style,string[] itemTypes)
        {
            if(itemTypes==null)return -1000;
            var types=itemTypes.Select(x=>(x??"").ToLowerInvariant()).ToArray();
            bool mount=types.Contains("horse");
            bool bowKit=types.Contains("bow") && types.Contains("arrows");
            bool crossbowKit=types.Contains("crossbow") && types.Contains("bolts");
            bool shieldKit=types.Contains("shield") && types.Contains("onehandedweapon");
            bool melee=types.Any(t=>t=="onehandedweapon" || t=="twohandedweapon" || t=="polearm");
            int score=(types.Contains("bodyarmor")?12:-30)+(melee?10:-30);
            if(types.Contains("bow")&&!bowKit)score-=100;
            if(types.Contains("crossbow")&&!crossbowKit)score-=100;
            if(types.Contains("horseharness")&&!mount)score-=60;
            if(style=="Lancer")score+=mount&&types.Contains("polearm")?90:-60;
            if(style=="HorseArcher")score+=mount&&bowKit?90:-60;
            if(style=="Longbow")score+=bowKit?65:-50;
            if(style=="Shieldguard" || style=="Infantry")score+=shieldKit?65:-45;
            if(style=="Commander")score+=mount?65:-45;
            if(style=="Bandit")score+=(melee?25:0)+(shieldKit?15:0)-(mount?15:0);
            if(style=="Court")score+=(melee?10:0)-(mount?30:0)-(bowKit||crossbowKit?35:0);
            if(style=="Scout")score+=(mount?30:0)+(bowKit?20:0);
            foreach(var item in itemTypes)
            {
                var type=(item??"").ToLowerInvariant();
                if(type.Length==0)continue;
                bool horse=type=="horse";
                bool bow=type.Contains("bow") || type.Contains("arrow") ||
                    type.Contains("bolt");
                bool shield=type.Contains("shield");
                bool spear=type.Contains("polearm");
                bool axe=type.Contains("twohanded");
                bool throwing=type.Contains("throw");
                bool armor=type.Contains("armor") || type.Contains("cape");
                if(style=="Lancer")
                    score+=(horse?30:0)+(spear?24:0)+(shield?6:0);
                else if(style=="HorseArcher")
                    score+=(horse?30:0)+(bow?25:0);
                else if(style=="Longbow")
                    score+=(bow?28:0)+(armor?2:0)-(horse?3:0);
                else if(style=="Axeguard")
                    score+=(axe?20:0)+(shield?18:0)+(armor?4:0);
                else if(style=="Shieldguard")
                    score+=(shield?28:0)+(armor?6:0);
                else if(style=="Skirmisher")
                    score+=(throwing?25:0)+(horse?12:0)+
                        (shield?8:0);
                else if(style=="Boarding")
                    score+=(shield?15:0)+(armor?5:0)-(horse?10:0);
                else if(style=="Scout")
                    score+=(horse?14:0)+(bow?15:0);
                else if(style=="Court")
                    score+=(armor?6:0)-(horse?16:0);
                else score+=(armor?3:0)+(shield?7:0);
                score++;
            }
            return score;
        }
    }
}
