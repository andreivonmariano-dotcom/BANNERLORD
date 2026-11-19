using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    /// <summary>
    /// Read actual game-owned Hero values after the narrative stage has
    /// advanced to the banner stage. No hand-written class stats or fake
    /// culture/childhood bonuses. Only known exact stat keys are returned.
    /// </summary>
    internal static class CUNativeStatsSnapshot
    {
        public static SortedDictionary<string,int> Capture(Hero hero)
        {
            var result = new SortedDictionary<string,int>(
                StringComparer.Ordinal);
            if (hero == null || hero.HeroDeveloper == null)
                return result;

            foreach (string name in SandboxOriginProfile.Attributes)
            {
                var info = typeof(DefaultCharacterAttributes).GetProperty(
                    name,BindingFlags.Public|BindingFlags.Static);
                var attr = info==null ? null :
                    info.GetValue(null,null) as CharacterAttribute;
                if(attr!=null)
                    result["Attribute."+name] = hero.GetAttributeValue(attr);
            }
            result[SandboxFamilyLevelPolicy.Key]=hero.Level;
            foreach (string name in SandboxNativeSkillCatalog.AvailableNames())
            {
                var skill=SandboxNativeSkillCatalog.Find(name);
                if(skill==null) continue;
                result["Skill."+name] = hero.GetSkillValue(skill);
                result["Focus."+name] = hero.HeroDeveloper.GetFocus(skill);
            }
            foreach (string name in SandboxOriginProfile.Traits)
            {
                var info = typeof(DefaultTraits).GetProperty(
                    name,BindingFlags.Public|BindingFlags.Static);
                var trait = info==null ? null :
                    info.GetValue(null,null) as TraitObject;
                if(trait!=null)
                    result["Trait."+name] = hero.GetTraitLevel(trait);
            }
            return result;
        }
    }
}
