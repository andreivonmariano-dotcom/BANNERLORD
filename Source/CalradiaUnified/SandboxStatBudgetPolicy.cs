using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure starting-progression policy. The baseline comes from the actual
    /// Bannerlord Hero AFTER narrative choices, never from guessed class
    /// templates. Spending uses positive gains only; lowering stats cannot
    /// manufacture extra points.
    /// </summary>
    public static class SandboxStatBudgetPolicy
    {
        public const int StartingBudget = 30;
        public static readonly string[] Modes =
            { "Native", "Balanced", "Unrestricted" };
        public static readonly string[] Pages =
            { "Attributes", "Skills", "Traits" };
        public static readonly string[] SkillGroups =
            { "Vigor", "Control", "Endurance", "Cunning", "Social", "Intelligence" };

        public static bool IsMode(string mode)
        {
            return mode == "Native" || mode == "Balanced" ||
                mode == "Unrestricted";
        }

        public static bool IsStatKey(string key)
        {
            if (String.IsNullOrEmpty(key)) return false;
            string[] parts = key.Split('.');
            if (parts.Length != 2) return false;
            if(key==SandboxFamilyLevelPolicy.Key)return true;
            if (parts[0] == "Attribute")
                return Array.IndexOf(SandboxOriginProfile.Attributes,parts[1])>=0;
            if (parts[0] == "Skill" || parts[0] == "Focus")
                return Array.IndexOf(SandboxOriginProfile.AllPotentialSkills,
                    parts[1])>=0;
            if (parts[0] == "Trait")
                return Array.IndexOf(SandboxOriginProfile.Traits,parts[1])>=0;
            return false;
        }

        public static bool InRange(string key,int value)
        {
            if (!IsStatKey(key)) return false;
            string kind = key.Split('.')[0];
            if(key==SandboxFamilyLevelPolicy.Key)
                return SandboxFamilyLevelPolicy.Valid(value);
            if (kind == "Attribute") return value >= 1 && value <= 10;
            if (kind == "Skill") return value >= 0 && value <= 330;
            if (kind == "Focus") return value >= 0 && value <= 5;
            return value >= -2 && value <= 2;
        }

        public static int Step(string key)
        {
            return key != null && key.StartsWith("Skill.",
                StringComparison.Ordinal) ? 10 : 1;
        }

        public static int Cost(string key,int nativeValue,int selectedValue)
        {
            if (!InRange(key,selectedValue)) return Int32.MaxValue;
            int delta = Math.Max(0,selectedValue-nativeValue);
            if (key.StartsWith("Attribute.",StringComparison.Ordinal))
                return delta*4;
            if(key==SandboxFamilyLevelPolicy.Key)return delta*4;
            if (key.StartsWith("Focus.",StringComparison.Ordinal))
                return delta*3;
            if (key.StartsWith("Trait.",StringComparison.Ordinal))
                return Math.Abs(selectedValue-nativeValue)*3;
            return (delta+9)/10;
        }

        public static bool TryEvaluate(string mode,
            IDictionary<string,int> changes,
            IDictionary<string,int> nativeValues,
            out int spent,out string error)
        {
            spent=0;
            error="";
            if (!IsMode(mode))
            {
                error="Unknown starting-stats mode.";
                return false;
            }
            if (mode=="Native") return true;
            if (changes==null || nativeValues==null)
            {
                error="Live native values are required for stat editing.";
                return false;
            }
            foreach (var entry in changes)
            {
                if (!InRange(entry.Key,entry.Value))
                {
                    error="Invalid starting stat: "+entry.Key;
                    return false;
                }
                int native;
                if (!nativeValues.TryGetValue(entry.Key,out native))
                {
                    error="Native value unavailable for "+entry.Key;
                    return false;
                }
                if (mode=="Balanced")
                {
                    int cost=Cost(entry.Key,native,entry.Value);
                    if (cost==Int32.MaxValue ||
                        cost>StartingBudget-spent)
                    {
                        error="Balanced allocation exceeds "+StartingBudget+
                            " points. Reduce an increase or choose Unrestricted.";
                        return false;
                    }
                    spent+=cost;
                }
            }
            return true;
        }

        /// <summary>
        /// Qualitative native trait guidance only. Relationship changes are
        /// event- and dialogue-specific, never an invented flat bonus.
        /// </summary>
        public static string TraitDescription(string trait)
        {
            switch (trait)
            {
                case "Mercy":
                    return "Compassion versus cruelty. Native events and dialogue can check this trait. No automatic relationship bonus.";
                case "Valor":
                    return "Courage versus caution. Can matter in character judgments and native dialogue. No fixed relation gain.";
                case "Honor":
                    return "Trustworthiness versus dishonor. Some native judgments and encounters check honor. Effects vary by event.";
                case "Generosity":
                    return "Generous versus tight-fisted conduct. Native NPC reactions depend on the exact interaction.";
                case "Calculating":
                    return "Pragmatic versus impulsive decisions. Native checks are event-specific, not a universal relationship modifier.";
                default:
                    return "An actual native personality trait. Effects depend on the game event.";
            }
        }

        public static string ModeDescription(string mode)
        {
            switch (mode)
            {
                case "Balanced":
                    return "30 points. Level +1 costs 4; Attribute +1 costs 4; Focus +1 costs 3; Skill +10 costs 1; trait deviation of 1 costs 3. No refunds for decreases.";
                case "Unrestricted":
                    return "Set any supported starting value within native ranges. No point budget.";
                default:
                    return "Keep every native stat, focus point and trait from your background. No overrides.";
            }
        }

        public static string GroupForSkill(string skill)
        {
            switch (skill)
            {
                case "OneHanded":
                case "TwoHanded":
                case "Polearm": return "Vigor";
                case "Bow":
                case "Crossbow":
                case "Throwing": return "Control";
                case "Riding":
                case "Athletics":
                case "Crafting": return "Endurance";
                case "Scouting":
                case "Tactics":
                case "Roguery": return "Cunning";
                case "Charm":
                case "Leadership":
                case "Trade": return "Social";
                case "Steward":
                case "Medicine":
                case "Engineering": return "Intelligence";
                default: return "";
            }
        }
    }
}
