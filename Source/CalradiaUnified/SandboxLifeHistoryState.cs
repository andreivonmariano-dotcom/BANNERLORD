using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Small, save-backed and independently testable character dossier.
    /// These records describe observed native Heroes; no fabricated
    /// occupation, family links, missions or dialogue are granted here.
    /// </summary>
    public sealed class SandboxLifeHistoryState
    {
        public sealed class Person
        {
            public string Id="",Name="",Culture="",Station="",
                Occupation="",HouseholdId="",Origin="",Ambition="",
                Biography="";
            public int Age;
        }
        public sealed class Bond
        {
            public string From="",To="",Kind="",Evidence="";
        }
        public sealed class Event
        {
            public string Kind="",Actor="",Target="",Detail="";
        }
        public const int MaxPeople=64,MaxBonds=192,MaxEvents=96;
        public readonly List<Person> People=new List<Person>();
        public readonly List<Bond> Bonds=new List<Bond>();
        public readonly List<Event> Events=new List<Event>();

        private static string Safe(string value)
        {
            if(String.IsNullOrEmpty(value))return "";
            return value.Length>240?value.Substring(0,240):value;
        }
        private static string Enc(string value)
        {
            return Uri.EscapeDataString(Safe(value));
        }
        private static string Dec(string value)
        {
            try{return Safe(Uri.UnescapeDataString(value??""));}
            catch{return "";}
        }
        public bool Record(Person p)
        {
            if(p==null || String.IsNullOrWhiteSpace(p.Id))return false;
            var old=People.FirstOrDefault(x=>x.Id==p.Id);
            if(old!=null){People[People.IndexOf(old)]=p;return true;}
            if(People.Count>=MaxPeople)return false;
            People.Add(p);return true;
        }
        // A later native observation must never overwrite an established origin.
        public bool RecordNew(Person p)
        {
            return p!=null && !People.Any(x=>x.Id==p.Id) && Record(p);
        }
        public bool Tie(string from,string to,string kind,string evidence)
        {
            if(String.IsNullOrWhiteSpace(from) ||
                String.IsNullOrWhiteSpace(to) ||
                from==to || String.IsNullOrWhiteSpace(kind))return false;
            if(Bonds.Any(x=>x.From==from && x.To==to &&
                x.Kind==kind))return true;
            if(Bonds.Count>=MaxBonds)return false;
            Bonds.Add(new Bond{From=from,To=to,Kind=kind,
                Evidence=evidence??""});
            return true;
        }
        // A reciprocal native spouse link is evidence of a marriage,
        // not proof of its date or a newly performed ceremony.
        public bool ObserveMarriage(string hero,string spouse,bool reciprocal)
        {
            if(!reciprocal || String.IsNullOrWhiteSpace(hero) ||
                String.IsNullOrWhiteSpace(spouse) || hero==spouse)return false;
            bool known=Bonds.Any(b=>b.Kind=="Spouse" &&
                ((b.From==hero && b.To==spouse) ||
                 (b.From==spouse && b.To==hero)));
            if(!Tie(hero,spouse,"Spouse","VerifiedNativeMarriage") || known)
                return false;
            return Remember("NativeMarriageObserved",hero,spouse,
                "native_marriage_seen=true|date_not_established");
        }
        public Event[] RecentFor(string hero,int limit)
        {
            if(String.IsNullOrWhiteSpace(hero) || limit<=0)return new Event[0];
            return Events.Where(e=>e.Actor==hero || e.Target==hero)
                .Reverse().Take(Math.Min(limit,10)).ToArray();
        }
        // Exact two-person evidence, never another character's event or an
        // inferred friendship. The existing CU_LIFE_1 format is unchanged.
        public Event SharedConversationMemory(string player,string speaker)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker)return null;
            return Events.LastOrDefault(e=>
                ((e.Actor==player && e.Target==speaker) ||
                 (e.Actor==speaker && e.Target==player)) &&
                (e.Kind=="FamilyVisit" || e.Kind=="FamilyGiftConfirmed" ||
                 e.Kind=="HouseholdDutyAppointed" || e.Kind=="HouseholdDutyReleased" ||
                 e.Kind=="HouseholdDutyReviewed" || e.Kind=="NativeMarriageObserved" ||
                 e.Kind=="VerifiedSiblingComradeship" || e.Kind=="PersonalAudienceAnswered" ||
                 (e.Kind=="PersonalDialogueChoice" && !e.Detail.Contains("|topic=Memory|"))));
        }
        public static bool IsConversationTopic(string topic)
        {
            return topic=="Opinions" || topic=="Concerns" || topic=="Ambitions" ||
                topic=="Family" || topic=="Finances" || topic=="Estate" ||
                topic=="Service" || topic=="Standing" || topic=="Memory";
        }
        public bool RecordConversationChoice(string player,string speaker,string topic,
            bool disagreed,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker || player.Length>80 || speaker.Length>80 ||
                !IsConversationTopic(topic) || day<0 || day>2000000)return false;
            // At most one retained conversational choice per pair per day.
            // Eviction of the bounded historical ledger never unlocks a reward.
            string marker="day="+day.ToString(System.Globalization.CultureInfo.InvariantCulture)+"|";
            if(Events.Any(e=>e.Kind=="PersonalDialogueChoice" && e.Actor==player &&
                e.Target==speaker && e.Detail.StartsWith(marker,StringComparison.Ordinal)))
                return false;
            return Remember("PersonalDialogueChoice",player,speaker,
                marker+"topic="+topic+"|stance="+(disagreed?"disagreed":"asked_advice"));
        }
        public bool HasRecentConversationChoice(string player,string speaker,string topic,int day)
        {
            if(!IsConversationTopic(topic) || day<0)return false;
            foreach(var e in Events.Where(e=>e.Kind=="PersonalDialogueChoice" &&
                e.Actor==player && e.Target==speaker))
            {
                var fields=e.Detail.Split('|');
                int recorded;
                if(fields.Length==3 && fields[0].StartsWith("day=",StringComparison.Ordinal) &&
                    Int32.TryParse(fields[0].Substring(4),out recorded) &&
                    fields[1]=="topic="+topic && recorded>=0 &&
                    (long)day-recorded>=0 && (long)day-recorded<7)return true;
            }
            return false;
        }
        // Group advice is an explicit player declaration, not a fulfilled
        // promise, political decision or native relation effect.
        public bool RecordGroupDiscussion(string player,string speaker,string topic,
            string priority,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker || player.Length>80 || speaker.Length>80 ||
                !SandboxPetitionVoicePolicy.GroupTopic(topic) ||
                !SandboxPetitionVoicePolicy.GroupPriority(priority) ||
                day<0 || day>2000000)return false;
            string marker="day="+day.ToString(System.Globalization.CultureInfo.InvariantCulture)+"|";
            if(Events.Any(e=>e.Kind=="GroupCouncilChoice" && e.Actor==player &&
                e.Target==speaker && e.Detail.StartsWith(marker,StringComparison.Ordinal)))return false;
            return Remember("GroupCouncilChoice",player,speaker,
                marker+"topic="+topic+"|priority="+priority);
        }
        public Event LatestGroupDiscussion(string player,string speaker)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker)return null;
            return Events.LastOrDefault(e=>e.Kind=="GroupCouncilChoice" &&
                e.Actor==player && e.Target==speaker);
        }
        public bool Remember(string kind,string actor,string target,
            string detail)
        {
            if(String.IsNullOrWhiteSpace(kind) ||
                String.IsNullOrWhiteSpace(actor))return false;
            // Keep the MOST RECENT observed campaign events, not only
            // the first ninety-six events after a long campaign begins.
            if(Events.Count>=MaxEvents)Events.RemoveAt(0);
            Events.Add(new Event{Kind=kind,Actor=actor,
                Target=target??"",Detail=detail??""});
            return true;
        }
        public string Serialize()
        {
            var rows=new List<string>{"CU_LIFE_1"};
            foreach(var p in People.OrderBy(p=>p.Id,
                StringComparer.Ordinal))
                rows.Add("P|"+String.Join("|",new[]{
                    Enc(p.Id),Enc(p.Name),Enc(p.Culture),
                    p.Age.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Enc(p.Station),Enc(p.Occupation),Enc(p.HouseholdId),
                    Enc(p.Origin),Enc(p.Ambition),Enc(p.Biography)}));
            foreach(var b in Bonds.OrderBy(b=>b.From,
                StringComparer.Ordinal).ThenBy(b=>b.Kind,
                StringComparer.Ordinal).ThenBy(b=>b.To,
                StringComparer.Ordinal))
                rows.Add("B|"+String.Join("|",new[]{
                    Enc(b.From),Enc(b.To),Enc(b.Kind),Enc(b.Evidence)}));
            foreach(var e in Events)
                rows.Add("E|"+String.Join("|",new[]{
                    Enc(e.Kind),Enc(e.Actor),Enc(e.Target),Enc(e.Detail)}));
            return String.Join("\n",rows);
        }
        public static SandboxLifeHistoryState Parse(string packed)
        {
            var state=new SandboxLifeHistoryState();
            if(String.IsNullOrEmpty(packed) || packed.Length>4194304)
                return state;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length==0 || rows.Length>MaxPeople+MaxBonds+MaxEvents+1 || rows[0]!="CU_LIFE_1")
                return state;
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');
                int age;
                if(f.Length==11 && f[0]=="P" &&
                    Int32.TryParse(f[4],out age) &&
                    age>=0 && age<=120)
                {
                    if(!state.People.Any(p=>p.Id==Dec(f[1])))
                    state.Record(new Person{Id=Dec(f[1]),Name=Dec(f[2]),
                        Culture=Dec(f[3]),Age=age,Station=Dec(f[5]),
                        Occupation=Dec(f[6]),HouseholdId=Dec(f[7]),
                        Origin=Dec(f[8]),Ambition=Dec(f[9]),
                        Biography=Dec(f[10])});
                }
                else if(f.Length==5 && f[0]=="B")
                    state.Tie(Dec(f[1]),Dec(f[2]),Dec(f[3]),
                        Dec(f[4]));
                else if(f.Length==5 && f[0]=="E")
                    state.Remember(Dec(f[1]),Dec(f[2]),
                        Dec(f[3]),Dec(f[4]));
            }
            return state;
        }
    }
}
