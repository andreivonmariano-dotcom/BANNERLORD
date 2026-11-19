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
        private static int Train(Hero h,string upbringing,
            Hero father=null,Hero mother=null,bool generatedChild=false)
        {
            if(h==null || h.HeroDeveloper==null)
                throw new InvalidOperationException(
                    "Generated relative has no native HeroDeveloper");
            // An adult created from an offspring template can have
            // missing attributes along with zero skills. Keep native
            // higher attributes and supply only an age-based floor.
            int positiveAttributes=0;
            foreach(string name in SandboxOriginProfile.Attributes)
            {
                var prop=typeof(DefaultCharacterAttributes).GetProperty(name,
                    BindingFlags.Public|BindingFlags.Static);
                var attribute=prop==null?null:
                    prop.GetValue(null,null) as CharacterAttribute;
                if(attribute==null)
                    throw new InvalidOperationException(
                        "Native attribute missing: "+name);
                int actual=h.GetAttributeValue(attribute);
                int minimum=h.Age>=18f?2:1;
                int target=minimum;
                if(generatedChild)
                {
                    int paternal=father==null?2:
                        father.GetAttributeValue(attribute);
                    int maternal=mother==null?2:
                        mother.GetAttributeValue(attribute);
                    target=SandboxFamilySkillInheritancePolicy
                        .SuggestedAttribute((int)h.Age,paternal,maternal,
                            MBRandom.RandomInt(-1,2));
                    DiagnosticJournal.Event("cu_family_inherited_attribute",
                        "hero="+h.StringId+"|name="+name+
                        "|age="+h.Age+"|father="+paternal+
                        "|mother="+maternal+
                        "|old_template="+actual+"|target="+target);
                }
                else target=Math.Max(actual,minimum);
                if(actual!=target)
                    h.HeroDeveloper.AddAttribute(
                        attribute,target-actual,false);
                if(h.GetAttributeValue(attribute)>0)
                    positiveAttributes++;
            }
            var random=RandomHero();
            var names=SandboxFamilyDevelopmentPolicy.Training(upbringing);
            if(generatedChild)
            {
                // The donor child template's skill vector must not
                // determine the character. Train ALL 18 skills using
                // parents' final skills, age and upbringing. This removes
                // both zero-stat adults and 18-year-olds with 150+ skills.
                foreach(string name in SandboxOriginProfile.Skills)
                {
                    var property=typeof(DefaultSkills).GetProperty(name,
                        BindingFlags.Public|BindingFlags.Static);
                    var skill=property==null?null:
                        property.GetValue(null,null) as SkillObject;
                    if(skill==null)throw new InvalidOperationException(
                        "Native child skill missing: "+name);
                    int fatherValue=father==null?0:father.GetSkillValue(skill);
                    int motherValue=mother==null?0:mother.GetSkillValue(skill);
                    int before=h.GetSkillValue(skill);
                    bool trained=names.Contains(name);
                    int target=SandboxFamilySkillInheritancePolicy
                        .SuggestedSkill((int)h.Age,fatherValue,motherValue,
                            trained,random.Next(10));
                    // Only newly generated children are set to this
                    // age-bound target. Ordinary Heroes and future game
                    // training are never overwritten.
                    h.HeroDeveloper.SetInitialSkillLevel(skill,target);
                    DiagnosticJournal.Event("cu_family_inherited_skill",
                        "hero="+h.StringId+"|skill="+name+
                        "|age="+h.Age+
                        "|old_template="+before+
                        "|father="+fatherValue+"|mother="+motherValue+
                        "|upbringing="+upbringing+
                        "|trained="+trained+"|applied="+
                        h.GetSkillValue(skill));
                }
            }
            else
            {
                // Existing native adult spouse templates keep their
                // developed careers; training only tops up gaps.
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
                "|positive_skill_count="+positive+
                "|positive_attribute_count="+positiveAttributes);
            if(h.Age>=18f && positiveAttributes<6)
                throw new InvalidOperationException(
                    "Adult relative has missing native attributes");
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
        private static void ApplySpouseCustomization(
            SandboxOriginProfile p,Hero spouse)
        {
            int updated=0;
            foreach(var entry in p.SpouseOverrides)
            {
                string[] parts=entry.Key.Split('.');
                if(parts.Length!=2 ||
                    !SandboxStatBudgetPolicy.InRange(entry.Key,entry.Value))
                    throw new InvalidOperationException(
                        "Invalid saved spouse stat "+entry.Key);
                var flags=BindingFlags.Public|BindingFlags.Static;
                int value=entry.Value;
                if(parts[0]=="Skill" || parts[0]=="Focus")
                {
                    var prop=typeof(DefaultSkills).GetProperty(parts[1],flags);
                    var skill=prop==null?null:
                        prop.GetValue(null,null) as SkillObject;
                    if(skill==null)throw new InvalidOperationException(
                        "Spouse skill missing "+parts[1]);
                    if(parts[0]=="Skill")
                        spouse.HeroDeveloper.SetInitialSkillLevel(skill,value);
                    else
                    {
                        int delta=value-spouse.HeroDeveloper.GetFocus(skill);
                        if(delta!=0)
                            spouse.HeroDeveloper.AddFocus(skill,delta,false);
                    }
                }
                else if(parts[0]=="Attribute")
                {
                    var prop=typeof(DefaultCharacterAttributes)
                        .GetProperty(parts[1],flags);
                    var attribute=prop==null?null:
                        prop.GetValue(null,null) as CharacterAttribute;
                    if(attribute==null)throw new InvalidOperationException(
                        "Spouse attribute missing "+parts[1]);
                    int delta=value-spouse.GetAttributeValue(attribute);
                    if(delta!=0)
                        spouse.HeroDeveloper.AddAttribute(
                            attribute,delta,false);
                }
                else if(parts[0]=="Trait")
                {
                    var prop=typeof(DefaultTraits).GetProperty(parts[1],flags);
                    var trait=prop==null?null:
                        prop.GetValue(null,null) as TraitObject;
                    if(trait==null)throw new InvalidOperationException(
                        "Spouse personality trait missing "+parts[1]);
                    spouse.SetTraitLevel(trait,value);
                }
                updated++;
                DiagnosticJournal.Event("cu_family_spouse_stat_override",
                    "hero="+spouse.StringId+"|key="+entry.Key+
                    "|requested="+value);
            }
            DiagnosticJournal.Event("cu_family_spouse_stats_final",
                "hero="+spouse.StringId+
                "|manual_overrides="+updated+
                "|before_child_generation=true");
        }
        public static void Spouse(SandboxOriginProfile p,Hero player,Hero h)
        {
            Personality(h,null,null,p.FamilyScenario,
                "FamilyTradition",true,0);
            Train(h,SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                p.FamilyScenario,"FamilyTradition"));
            ApplySpouseCustomization(p,h);
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
            if(h.Age>=6f)Train(h,upbringing,father,mother,true);
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
