using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    /// <summary>Sets only newly-generated CU relatives, never donor templates.</summary>
    internal static class SandboxFamilyDevelopment
    {
        private static readonly TraitObject[] Traits={
            DefaultTraits.Mercy,DefaultTraits.Valor,DefaultTraits.Honor,
            DefaultTraits.Generosity,DefaultTraits.Calculating
        };
        private static Random RandomHero()
        {
            return new Random(MBRandom.RandomInt(1,Int32.MaxValue));
        }
        private static string TraitsText(Hero h)
        {
            return String.Join(",",Traits.Select(t=>
                t.StringId+":"+h.GetTraitLevel(t)));
        }
        private static void Personality(Hero h,Hero mother,Hero father,
            string scenario,string upbringing,bool spouse,int index)
        {
            var r=RandomHero();
            for(int i=0;i<Traits.Length;i++)
            {
                var trait=Traits[i];
                var name=SandboxFamilyDevelopmentPolicy.Personality[i];
                int bias=SandboxFamilyDevelopmentPolicy.Bias(
                    scenario,upbringing,name,spouse);
                int a=mother==null?r.Next(-1,2):
                    mother.GetTraitLevel(trait);
                int b=father==null?r.Next(-1,2):
                    father.GetTraitLevel(trait);
                bool favored=(spouse && scenario=="Honorable" &&
                    name=="Honor") || (!spouse && name=="Mercy" &&
                    SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                        scenario,upbringing)=="Kind");
                int result=SandboxFamilyDevelopmentPolicy.PersonalityValue(
                    a,b,bias,r.Next(100),r.Next(-2,3),
                    (int)h.Age,favored);
                if(spouse && scenario=="Honorable" && name=="Honor")
                    result=2;
                h.SetTraitLevel(trait,result);
            }
            bool copied=!spouse && mother!=null &&
                mother.Children.Any(x=>x!=null && x!=h &&
                    x.Father==h.Father && x.Mother==h.Mother &&
                    Traits.All(t=>x.GetTraitLevel(t)==h.GetTraitLevel(t)));
            if(copied || Traits.All(t=>h.GetTraitLevel(t)==0))
            {
                var t=Traits[(index+r.Next(Traits.Length))%Traits.Length];
                int current=h.GetTraitLevel(t);
                h.SetTraitLevel(t,current>=2?1:current<=-2?-1:current+1);
            }
            DiagnosticJournal.Event("cu_family_personality_initialized",
                "id="+h.StringId+"|scenario="+scenario+
                "|upbringing="+upbringing+"|traits="+TraitsText(h)+
                "|parental_inheritance="+(mother!=null && father!=null));
        }
        private static int Train(Hero h,string upbringing)
        {
            if(h==null || h.HeroDeveloper==null)
                throw new InvalidOperationException(
                    "Generated relative has no native HeroDeveloper");
            var random=RandomHero();
            var names=SandboxFamilyDevelopmentPolicy.Training(upbringing);
            for(int i=0;i<names.Length;i++)
            {
                var property=typeof(DefaultSkills).GetProperty(names[i],
                    BindingFlags.Public|BindingFlags.Static);
                var skill=property==null?null:
                    property.GetValue(null,null) as SkillObject;
                if(skill==null)
                    throw new InvalidOperationException(
                        "Native skill missing: "+names[i]);
                int level=SandboxFamilyDevelopmentPolicy.SkillLevel(
                    (int)h.Age,i,random.Next(15));
                if(level>h.GetSkillValue(skill))
                    h.HeroDeveloper.SetInitialSkillLevel(skill,level);
            }
            int positive=0;
            foreach(string name in SandboxOriginProfile.Skills)
            {
                var property=typeof(DefaultSkills).GetProperty(name,
                    BindingFlags.Public|BindingFlags.Static);
                var skill=property==null?null:
                    property.GetValue(null,null) as SkillObject;
                if(skill!=null && h.GetSkillValue(skill)>0)positive++;
            }
            DiagnosticJournal.Event("cu_family_native_skills_audited",
                "hero="+h.StringId+"|age="+h.Age+
                "|upbringing="+upbringing+
                "|positive_skill_count="+positive);
            if(h.Age>=18f && positive<3)
                throw new InvalidOperationException(
                    "Adult relative has fewer than three developed native skills");
            return positive;
        }
        private static void Relation(Hero a,Hero b,int target,string kind)
        {
            if(a==null || b==null || a==b)return;
            int requested=SandboxFamilyDevelopmentPolicy.ClampRelation(target);
            a.SetPersonalRelation(b,requested);
            DiagnosticJournal.Event("cu_family_relation_initialized",
                "kind="+kind+"|a="+a.StringId+"|b="+b.StringId+
                "|requested="+requested+
                "|actual="+a.GetRelation(b)+
                "|reverse="+b.GetRelation(a));
        }
        public static void Spouse(SandboxOriginProfile p,Hero player,Hero h)
        {
            Personality(h,null,null,p.FamilyScenario,
                "FamilyTradition",true,0);
            Train(h,SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                p.FamilyScenario,"FamilyTradition"));
            Relation(player,h,
                SandboxFamilyDevelopmentPolicy.SpouseRelationBase(
                    p.FamilyScenario)+RandomHero().Next(-6,7),"spouse");
        }
        public static void Child(SandboxOriginProfile p,Hero player,
            Hero spouse,Hero h,SandboxOriginProfile.OriginRelative plan,int index)
        {
            Hero mother=player.IsFemale?player:spouse;
            Hero father=player.IsFemale?spouse:player;
            string upbringing=SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                p.FamilyScenario,plan.Upbringing);
            Personality(h,mother,father,p.FamilyScenario,
                plan.Upbringing,false,index);
            if(h.Age>=6f)Train(h,upbringing);
            var random=RandomHero();
            int baseline=SandboxFamilyDevelopmentPolicy.ChildRelationBase(
                p.FamilyScenario);
            Relation(player,h,baseline+random.Next(-8,9),"player_child");
            Relation(spouse,h,baseline+random.Next(-8,9),"spouse_child");
            foreach(var sibling in player.Children)
                if(sibling!=null && sibling!=h &&
                    sibling.Mother==h.Mother && sibling.Father==h.Father)
                    Relation(sibling,h,baseline-10+random.Next(-10,11),
                        "siblings");
        }
        public static void Milestone(Hero child,string scenario,
            string upbringing,int age)
        {
            if(child==null || !child.IsAlive ||
                !SandboxFamilyDevelopmentPolicy.IsDevelopmentMilestone(age))
                return;
            var random=RandomHero();
            string resolved=SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                scenario,upbringing);
            string favored=resolved=="Kind"?"Mercy":
                resolved=="Martial"?"Valor":
                resolved=="Scholarly"?"Honor":
                resolved=="Mercantile"?"Calculating":
                SandboxFamilyDevelopmentPolicy.Personality[
                    random.Next(Traits.Length)];
            int i=Array.IndexOf(
                SandboxFamilyDevelopmentPolicy.Personality,favored);
            var trait=Traits[i];
            int before=child.GetTraitLevel(trait);
            if(random.Next(100)<65 && before<2)
                child.SetTraitLevel(trait,before+1);
            if(child.Age>=6f)Train(child,resolved);
            DiagnosticJournal.Event("cu_family_upbringing_milestone",
                "hero="+child.StringId+"|milestone="+age+
                "|scenario="+scenario+"|upbringing="+upbringing+
                "|trait="+favored+"|before="+before+
                "|after="+child.GetTraitLevel(trait));
        }
    }
}
