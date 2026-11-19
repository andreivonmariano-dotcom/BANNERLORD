using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    // War Sails registers its own SkillObject instances at game start.
    // Do not require the expansion assembly in a base-game installation.
    internal static class SandboxNativeSkillCatalog
    {
        public static readonly string[] Naval =
            { "Mariner", "Boatswain", "Shipmaster" };

        private static string Normalize(string value)
        {
            return new String((value??"").Where(Char.IsLetterOrDigit)
                .Select(Char.ToLowerInvariant).ToArray());
        }
        public static SkillObject Find(string name)
        {
            if(String.IsNullOrWhiteSpace(name))return null;
            var property=typeof(DefaultSkills).GetProperty(name,
                BindingFlags.Public|BindingFlags.Static);
            var standard=property==null?null:
                property.GetValue(null,null) as SkillObject;
            if(standard!=null)return standard;
            try
            {
                if(MBObjectManager.Instance==null)return null;
                string wanted=Normalize(name);
                foreach(var skill in MBObjectManager.Instance
                    .GetObjectTypeList<SkillObject>())
                {
                    if(skill==null)continue;
                    string id=Normalize(skill.StringId);
                    if(id==wanted || id=="naval"+wanted ||
                        Normalize(skill.Name.ToString())==wanted)
                        return skill;
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_naval_skill_lookup_failed",error);
            }
            return null;
        }
        public static IEnumerable<string> AvailableNames()
        {
            foreach(string name in SandboxOriginProfile.Skills)
                if(Find(name)!=null)yield return name;
            foreach(string name in Naval)
                if(Find(name)!=null)yield return name;
        }
        public static string Status()
        {
            string[] actual=Naval.Where(n=>Find(n)!=null).ToArray();
            return actual.Length==3?
                "WAR SAILS | ALL THREE NAVAL SKILLS AVAILABLE":
                "WAR SAILS | "+actual.Length+
                "/3 REGISTERED | MISSING SKILLS HIDDEN";
        }
    }
}
