using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace CalradiaUnified
{
    // Save-local seed and bounded exposure counters. Rendering never advances them.
    public sealed class SandboxDialogueVarietyState
    {
        public const int Capacity=96;
        public const string Version="CU_DIALOGUE_VARIETY_1";
        public string Seed="";
        public sealed class Entry { public string Player="",Speaker="",Topic=""; public int Count,Day; }
        public readonly List<Entry> Entries=new List<Entry>();
        private static bool Id(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=80 &&
                s.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        private static bool Topic(string s)
        {
            return s=="Greeting" || s=="Initiative" || s=="Group" || s=="OfficerDuty" ||
                (s!=null && s.StartsWith("Group:",StringComparison.Ordinal) &&
                    (SandboxPetitionVoicePolicy.GroupTopic(s.Substring(6)) || SandboxGatheringPlan.ValidOccasion(s.Substring(6)))) ||
                SandboxLifeHistoryState.IsConversationTopic(s);
        }
        public void Initialize(string seed)
        {
            if(Seed.Length==0 && Id(seed))Seed=seed;
        }
        private Entry Find(string player,string speaker,string topic)
        {
            return Entries.FirstOrDefault(e=>e.Player==player && e.Speaker==speaker && e.Topic==topic);
        }
        public int Exposure(string player,string speaker,string topic)
        {
            var e=Find(player,speaker,topic);return e==null?0:e.Count;
        }
        public int Variant(string player,string speaker,string topic,int count)
        {
            if(count<=0)return 0;
            uint hash=2166136261u;
            unchecked { foreach(char c in Seed+"|"+player+"|"+speaker+"|"+topic)
                hash=(hash^c)*16777619u; }
            return (int)(((ulong)hash+(ulong)Exposure(player,speaker,topic))%(ulong)count);
        }
        public bool Advance(string player,string speaker,string topic,int day)
        {
            if(Seed.Length==0 || !Id(player) || !Id(speaker) || player==speaker ||
                !Topic(topic) || day<0 || day>2000000)return false;
            var e=Find(player,speaker,topic);
            if(e!=null && day<e.Day)return false;
            if(e==null)
            {
                if(Entries.Count>=Capacity)Entries.RemoveAt(0);
                e=new Entry{Player=player,Speaker=speaker,Topic=topic};Entries.Add(e);
            }
            else {Entries.Remove(e);Entries.Add(e);}
            e.Count=e.Count==2000000000?0:e.Count+1;e.Day=day;return true;
        }
        public string Serialize()
        {
            if(Seed.Length==0)return "";
            var lines=new List<string>{Version+"|"+Seed};
            foreach(var e in Entries)lines.Add(String.Join("|",new[]{e.Player,e.Speaker,e.Topic,
                e.Count.ToString(CultureInfo.InvariantCulture),e.Day.ToString(CultureInfo.InvariantCulture)}));
            return String.Join("\n",lines);
        }
        public static SandboxDialogueVarietyState Parse(string packed)
        {
            var s=new SandboxDialogueVarietyState();
            if(String.IsNullOrEmpty(packed) || packed.Length>30000)return s;
            var rows=packed.Replace("\r","").Split('\n');var header=rows[0].Split('|');
            if(rows.Length>Capacity+1 || header.Length!=2 || header[0]!=Version || !Id(header[1]))return s;
            s.Initialize(header[1]);
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');int count,day;
                if(f.Length!=5 || !Id(f[0]) || !Id(f[1]) || f[0]==f[1] || !Topic(f[2]) ||
                    s.Find(f[0],f[1],f[2])!=null || !Int32.TryParse(f[3],out count) ||
                    !Int32.TryParse(f[4],out day) || count<0 || count>2000000000 || day<0 || day>2000000)
                    return new SandboxDialogueVarietyState();
                s.Entries.Add(new Entry{Player=f[0],Speaker=f[1],Topic=f[2],Count=count,Day=day});
            }
            return s;
        }
    }
}
