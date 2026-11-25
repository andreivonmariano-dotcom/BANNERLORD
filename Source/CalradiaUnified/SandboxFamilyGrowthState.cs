using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CalradiaUnified
{
    /// <summary>
    /// Compact campaign-save state for CU-created offspring only.
    /// String serialization avoids an unregistered custom SaveSystem type.
    /// </summary>
    public sealed class SandboxFamilyGrowthState
    {
        public sealed class Entry
        {
            public string Upbringing;
            public int LastMilestone;
        }
        private readonly SortedDictionary<string,Entry> entries =
            new SortedDictionary<string,Entry>(StringComparer.Ordinal);
        public string Scenario { get; private set; } = "Natural";
        public int Count { get { return entries.Count; } }

        public void SetScenario(string scenario)
        {
            if(!SandboxFamilyDevelopmentPolicy.IsScenario(scenario))
                throw new ArgumentException("Unsupported family scenario");
            Scenario=scenario;
        }
        public void Register(string heroId,string upbringing,int age)
        {
            if(String.IsNullOrWhiteSpace(heroId) ||
                heroId.Length>120 ||
                !SandboxFamilyDevelopmentPolicy.IsUpbringing(upbringing))
                throw new ArgumentException("Invalid generated child");
            if(age<0 || age>80)
                throw new ArgumentOutOfRangeException("age");
            entries[heroId]=new Entry{
                Upbringing=upbringing,
                LastMilestone=SandboxFamilyDevelopmentPolicy
                    .CompletedMilestone(age)
            };
        }
        public bool TryAdvance(string heroId,int newMilestone,
            out string upbringing)
        {
            upbringing="";
            Entry entry;
            if(!entries.TryGetValue(heroId??"",out entry) ||
                !SandboxFamilyDevelopmentPolicy
                    .IsDevelopmentMilestone(newMilestone) ||
                newMilestone<=entry.LastMilestone)
                return false;
            // Commit the milestone first, preventing a repeated grant
            // after save/reload even if native updates fail.
            entry.LastMilestone=newMilestone;
            upbringing=entry.Upbringing;
            return true;
        }
        public string UpbringingFor(string heroId)
        {
            Entry entry;
            return entries.TryGetValue(heroId??"",out entry)?entry.Upbringing:"";
        }
        public bool ReviseUpbringing(string heroId,string upbringing,int age)
        {
            Entry entry;
            if(age<0 || age>=18 ||
                !SandboxFamilyDevelopmentPolicy.IsUpbringing(upbringing) ||
                !entries.TryGetValue(heroId??"",out entry) || entry.Upbringing==upbringing)
                return false;
            // Preserve all previously earned milestones; never replay education.
            entry.Upbringing=upbringing;
            return true;
        }
        public int LastMilestone(string heroId)
        {
            Entry e;
            return entries.TryGetValue(heroId??"",out e) ?
                e.LastMilestone : -1;
        }
        private static string Encode(string text)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(text??""));
        }
        private static string Decode(string text)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(text));
        }
        public string Serialize()
        {
            var b=new StringBuilder();
            b.Append("v1|").Append(Encode(Scenario)).Append('\n');
            foreach(var pair in entries)
            {
                b.Append(Encode(pair.Key)).Append('|')
                    .Append(Encode(pair.Value.Upbringing)).Append('|')
                    .Append(pair.Value.LastMilestone.ToString(
                        CultureInfo.InvariantCulture)).Append('\n');
            }
            return b.ToString();
        }
        public static SandboxFamilyGrowthState Parse(string text)
        {
            var state=new SandboxFamilyGrowthState();
            if(String.IsNullOrWhiteSpace(text))return state;
            var lines=text.Split(new[]{'\n'},
                StringSplitOptions.RemoveEmptyEntries);
            if(lines.Length<1 || !lines[0].StartsWith("v1|",
                StringComparison.Ordinal))return state;
            try
            {
                string scenario=Decode(lines[0].Substring(3));
                state.SetScenario(scenario);
                for(int i=1;i<lines.Length && i<=6;i++)
                {
                    var parts=lines[i].Trim().Split('|');
                    int mark;
                    if(parts.Length!=3 ||
                        !Int32.TryParse(parts[2],
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,out mark))
                        continue;
                    string id=Decode(parts[0]);
                    string upbringing=Decode(parts[1]);
                    if(!SandboxFamilyDevelopmentPolicy
                        .IsDevelopmentMilestone(mark) && mark!=0)
                        continue;
                    state.Register(id,upbringing,0);
                    state.entries[id].LastMilestone=mark;
                }
            }
            catch(FormatException)
            {
                return new SandboxFamilyGrowthState();
            }
            catch(ArgumentException)
            {
                return new SandboxFamilyGrowthState();
            }
            return state;
        }
    }
}
