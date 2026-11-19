using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Data-only rules for native household relationships, personality and
    /// education. No game API references: these rules run in PolicySmoke.
    /// </summary>
    public static class SandboxFamilyDevelopmentPolicy
    {
        public static readonly string[] Scenarios =
        {
            "Natural", "Honorable", "Martial", "Scholarly",
            "Mercantile", "Strained"
        };
        public static readonly string[] Upbringings =
        {
            "FamilyTradition", "Kind", "Martial", "Scholarly",
            "Mercantile", "Independent"
        };
        public static readonly string[] Personality =
            { "Mercy", "Valor", "Honor", "Generosity", "Calculating" };
        public static bool IsScenario(string value)
        {
            return Array.IndexOf(Scenarios,value)>=0;
        }
        public static bool IsUpbringing(string value)
        {
            return Array.IndexOf(Upbringings,value)>=0;
        }
        public static string ResolveUpbringing(string scenario,
            string requested)
        {
            if(requested!="FamilyTradition")return requested;
            switch(scenario)
            {
                case "Honorable":return "Kind";
                case "Martial":return "Martial";
                case "Scholarly":return "Scholarly";
                case "Mercantile":return "Mercantile";
                case "Strained":return "Independent";
                default:return "Independent";
            }
        }
        public static int SpouseRelationBase(string scenario)
        {
            switch(scenario)
            {
                case "Honorable":return 75;
                case "Martial":return 55;
                case "Scholarly":return 62;
                case "Mercantile":return 55;
                case "Strained":return 12;
                default:return 52;
            }
        }
        public static int ChildRelationBase(string scenario)
        {
            switch(scenario)
            {
                case "Honorable":return 70;
                case "Martial":return 48;
                case "Scholarly":return 55;
                case "Mercantile":return 50;
                case "Strained":return 15;
                default:return 50;
            }
        }
        public static int ClampRelation(int value)
        {
            return Math.Max(-100,Math.Min(100,value));
        }
        public static int Bias(string scenario,string upbringing,
            string trait,bool isSpouse)
        {
            if(isSpouse)
            {
                if(scenario=="Honorable")
                {
                    if(trait=="Honor")return 2;
                    if(trait=="Mercy" || trait=="Generosity")return 1;
                }
                if(scenario=="Martial" && trait=="Valor")return 2;
                if(scenario=="Scholarly" && trait=="Calculating")return 1;
                if(scenario=="Mercantile" && trait=="Calculating")return 2;
                if(scenario=="Strained" && trait=="Generosity")return -1;
                return 0;
            }
            string effective=ResolveUpbringing(scenario,upbringing);
            if(effective=="Kind" && trait=="Mercy")return 2;
            if(effective=="Kind" && trait=="Generosity")return 1;
            if(effective=="Martial" && trait=="Valor")return 2;
            if(effective=="Scholarly" && trait=="Honor")return 1;
            if(effective=="Scholarly" && trait=="Calculating")return 1;
            if(effective=="Mercantile" && trait=="Calculating")return 2;
            if(effective=="Independent" && scenario=="Strained" &&
                trait=="Generosity")return -1;
            return 0;
        }
        public static int PersonalityValue(int parentA,int parentB,
            int bias,int draw,int mutation,int age,bool favored)
        {
            // The game's real parents influence 50% of draws. An
            // upbringing contributes 30%, independent variation 20%.
            // No cloned donor's trait vector survives unchanged.
            int value=draw<25?parentA:
                draw<50?parentB:
                draw<80?bias:mutation;
            if(bias!=0 && draw%3==0)
                value=(value+bias>=0)?
                    (int)Math.Ceiling((value+bias)/2.0):
                    (int)Math.Floor((value+bias)/2.0);
            if(favored && value<1)value=1;
            int limit=age<14?1:2;
            return Math.Max(-limit,Math.Min(limit,value));
        }
        public static int SkillLevel(int age,int rank,int variation)
        {
            if(age<6)return 0;
            if(rank<0 || rank>=6)return 0;
            int skill=age<18 ? 3+age*2 : 42+(age-18)*2;
            skill=Math.Min(140,skill);
            int drop=rank==0?0:rank==1?7:
                rank==2?14:rank==3?19:rank==4?25:31;
            int result=skill-drop+Math.Max(0,Math.Min(14,variation));
            return Math.Max(age>=18?8:0,Math.Min(170,result));
        }
        public static string[] Training(string upbringing)
        {
            switch(upbringing)
            {
                case "Kind":
                    return new[]{"Charm","Medicine","Steward",
                        "Leadership","Riding","Athletics"};
                case "Martial":
                    return new[]{"OneHanded","Riding","Polearm",
                        "Athletics","Tactics","Leadership"};
                case "Scholarly":
                    return new[]{"Medicine","Engineering","Steward",
                        "Trade","Charm","Scouting"};
                case "Mercantile":
                    return new[]{"Trade","Steward","Charm",
                        "Scouting","Riding","Leadership"};
                default:
                    return new[]{"Athletics","Scouting","Riding",
                        "Bow","OneHanded","Charm"};
            }
        }
        public static bool IsDevelopmentMilestone(int age)
        {
            return age==6 || age==10 || age==14 || age==18;
        }
        public static int CompletedMilestone(int age)
        {
            return age>=18?18:age>=14?14:age>=10?10:age>=6?6:0;
        }
    }
}
