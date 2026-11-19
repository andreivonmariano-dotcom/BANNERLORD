using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Independent, testable age-based model for newly generated offspring.
    /// Real in-game values are read from native parent Heroes. No child
    /// reaches a high adult skill simply from a parent's reputation.
    /// </summary>
    public static class SandboxFamilySkillInheritancePolicy
    {
        public static int AgeCap(int age)
        {
            if(age<6)return 0;
            if(age<10)return 12;
            if(age<14)return 28;
            if(age<18)return 55;
            if(age<21)return 75;
            if(age<26)return 120;
            if(age<40)return 180;
            return 240;
        }
        public static int SuggestedSkill(int age,int fatherSkill,
            int motherSkill,bool trained,int variation)
        {
            int cap=AgeCap(age);
            if(cap==0)return 0;
            // Age-specific everyday experience grows separately from
            // parental influence, and comes before formal education.
            int experience=age<10?1:age<14?3:age<18?5:
                9+Math.Min(28,(age-18)*2);
            int parental=Math.Max(0,Math.Min(330,
                (Math.Max(0,fatherSkill)+Math.Max(0,motherSkill))/2));
            // Inheritance is expressed as early training advantage,
            // never literal genetic acquisition of adult mastery.
            int influence=(int)Math.Round(parental*
                (age<14?0.025:age<18?0.085:age<21?0.15:0.23),
                MidpointRounding.AwayFromZero);
            int education=trained ?
                (age<10?3:age<14?7:age<18?15:age<21?22:30):0;
            int value=experience+influence+education+
                Math.Max(0,Math.Min(9,variation));
            return Math.Max(age>=18?4:0,Math.Min(cap,value));
        }
        public static int SuggestedAttribute(int age,int father,
            int mother,int variation)
        {
            int average=(Math.Max(1,Math.Min(10,father))+
                Math.Max(1,Math.Min(10,mother)))/2;
            if(age<6)return 1;
            if(age<10)return Math.Min(2,1+average/8);
            if(age<14)return Math.Min(3,1+average/5);
            if(age<18)return Math.Min(4,2+average/5);
            int value=2+(int)Math.Round(average*0.46,
                MidpointRounding.AwayFromZero)+
                Math.Max(-1,Math.Min(1,variation));
            int cap=age<21?5:age<26?7:10;
            return Math.Max(2,Math.Min(cap,value));
        }
        public static bool IsYouth(int age)
        {
            return age>=18 && age<=21;
        }
    }
}
