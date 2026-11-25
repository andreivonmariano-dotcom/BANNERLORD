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
        public bool ObserveMarriage(string hero,string spouse,bool reciprocal,int observedDay=-1)
        {
            if(!reciprocal || String.IsNullOrWhiteSpace(hero) ||
                String.IsNullOrWhiteSpace(spouse) || hero==spouse)return false;
            bool known=Bonds.Any(b=>b.Kind=="Spouse" &&
                ((b.From==hero && b.To==spouse) ||
                 (b.From==spouse && b.To==hero)));
            if(!Tie(hero,spouse,"Spouse","VerifiedNativeMarriage") || known)
                return false;
            return Remember("NativeMarriageObserved",hero,spouse,
                "native_marriage_seen=true|date_not_established"+
                (observedDay>=0 && observedDay<=2000000?"|observed_day="+observedDay.ToString(
                    System.Globalization.CultureInfo.InvariantCulture):""));
        }
        public bool RecentNativeMarriageObservation(string hero,string spouse,int day)
        {
            if(String.IsNullOrWhiteSpace(hero) || String.IsNullOrWhiteSpace(spouse) ||
                hero==spouse || day<0)return false;
            var receipt=Events.LastOrDefault(e=>e.Kind=="NativeMarriageObserved" &&
                ((e.Actor==hero && e.Target==spouse) || (e.Actor==spouse && e.Target==hero)));
            if(receipt==null)return false;
            var fields=receipt.Detail.Split('|');
            int observed;
            return fields.Length==3 && fields[0]=="native_marriage_seen=true" &&
                fields[1]=="date_not_established" && fields[2].StartsWith("observed_day=",StringComparison.Ordinal) &&
                Int32.TryParse(fields[2].Substring(13),out observed) && observed>=0 &&
                (long)day-observed>=0 && (long)day-observed<7;
        }
        // A saved native observation, not a reconstructed event date. If an
        // old sample is evicted, the next observation is only a new baseline.
        public bool ObserveFamilyState(string player,string relative,string track,string value,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(relative) ||
                player==relative || player.Length>80 || relative.Length>80 ||
                day<0 || day>2000000 ||
                !(track=="Age" && (value=="Minor" || value=="Adult") ||
                  track=="Presence" && (value=="Together" || value=="Apart")))return false;
            string kind="NativeFamily"+track+"Observed";
            var previous=Events.LastOrDefault(e=>e.Kind==kind && e.Actor==player && e.Target==relative);
            var fields=previous==null?new string[0]:previous.Detail.Split('|');
            int priorDay;
            bool known=fields.Length==3 && fields[0].StartsWith("state=",StringComparison.Ordinal) &&
                fields[1].StartsWith("day=",StringComparison.Ordinal) &&
                Int32.TryParse(fields[1].Substring(4),out priorDay) && priorDay<=day;
            if(previous!=null && !known)return false;
            if(known && fields[0]=="state="+value)return false;
            // An adult never becomes a minor in this observational history.
            if(known && track=="Age" && fields[0]=="state=Adult")return false;
            string change=!known?"Baseline":track=="Age"?"Matured":value=="Together"?"Reunited":"Separated";
            return Remember(kind,player,relative,"state="+value+"|day="+day.ToString(
                System.Globalization.CultureInfo.InvariantCulture)+"|change="+change);
        }
        public string RecentFamilyChange(string player,string relative,string track,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(relative) ||
                player==relative || (track!="Age" && track!="Presence") || day<0)return "";
            var last=Events.LastOrDefault(e=>e.Kind=="NativeFamily"+track+"Observed" &&
                e.Actor==player && e.Target==relative);
            if(last==null)return "";
            var fields=last.Detail.Split('|');
            int observed;
            if(fields.Length!=3 || !fields[1].StartsWith("day=",StringComparison.Ordinal) ||
                !Int32.TryParse(fields[1].Substring(4),out observed) || observed<0 ||
                (long)day-observed<0 || (long)day-observed>=7)return "";
            if(track=="Age" && fields[0]=="state=Adult" && fields[2]=="change=Matured")return "Matured";
            if(track=="Presence" && fields[0]=="state=Together" && fields[2]=="change=Reunited")return "Reunited";
            if(track=="Presence" && fields[0]=="state=Apart" && fields[2]=="change=Separated")return "Separated";
            return "";
        }
        // A native event receipt is separate from daily state observations.
        // Both remain descriptive and carry no reward or action.
        public bool RecordNativeFamilyMilestone(string player,string child,string milestone,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(child) ||
                player==child || player.Length>80 || child.Length>80 ||
                day<0 || day>2000000 || (milestone!="Birth" && milestone!="Adulthood"))return false;
            string kind="NativeFamily"+milestone+"Confirmed";
            if(Events.Any(e=>e.Kind==kind && e.Actor==player && e.Target==child))return false;
            return Remember(kind,player,child,"day="+day.ToString(
                System.Globalization.CultureInfo.InvariantCulture)+"|native_callback=true");
        }
        public bool HasRecentNativeFamilyMilestone(string player,string child,string milestone,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(child) ||
                player==child || (milestone!="Birth" && milestone!="Adulthood") || day<0)return false;
            var receipt=Events.LastOrDefault(e=>e.Kind=="NativeFamily"+milestone+"Confirmed" &&
                e.Actor==player && e.Target==child);
            if(receipt==null)return false;
            var fields=receipt.Detail.Split('|');
            int recorded;
            return fields.Length==2 && fields[0].StartsWith("day=",StringComparison.Ordinal) &&
                fields[1]=="native_callback=true" && Int32.TryParse(fields[0].Substring(4),out recorded) &&
                recorded>=0 && (long)day-recorded>=0 && (long)day-recorded<7;
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
                 e.Kind=="NativePartyDutyAssigned" || e.Kind=="NativeGovernorAssigned" ||
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
            string priority,int day,string occasion="")
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker || player.Length>80 || speaker.Length>80 ||
                !SandboxPetitionVoicePolicy.GroupTopic(topic) ||
                !SandboxPetitionVoicePolicy.GroupPriority(priority) ||
                (!String.IsNullOrEmpty(occasion) && !SandboxGatheringPlan.ValidOccasion(occasion)) ||
                day<0 || day>2000000)return false;
            string marker="day="+day.ToString(System.Globalization.CultureInfo.InvariantCulture)+"|";
            if(Events.Any(e=>e.Kind=="GroupCouncilChoice" && e.Actor==player &&
                e.Target==speaker && e.Detail.StartsWith(marker,StringComparison.Ordinal)))return false;
            return Remember("GroupCouncilChoice",player,speaker,
                marker+"topic="+topic+"|priority="+priority+
                (String.IsNullOrEmpty(occasion)?"":"|occasion="+occasion));
        }
        public Event LatestGroupDiscussion(string player,string speaker)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker)return null;
            return Events.LastOrDefault(e=>e.Kind=="GroupCouncilChoice" &&
                e.Actor==player && e.Target==speaker);
        }
        public bool RecordRelationshipIntention(string player,string speaker,string intention,int day)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker || player.Length>80 || speaker.Length>80 || day<0 || day>2000000 ||
                (intention!="Repair" && intention!="Care"))return false;
            string marker="day="+day.ToString(System.Globalization.CultureInfo.InvariantCulture)+"|";
            if(Events.Any(e=>e.Kind=="RelationshipIntention" && e.Actor==player &&
                e.Target==speaker && e.Detail.StartsWith(marker,StringComparison.Ordinal)))return false;
            return Remember("RelationshipIntention",player,speaker,marker+"intention="+intention);
        }
        public Event LatestRelationshipIntention(string player,string speaker)
        {
            if(String.IsNullOrWhiteSpace(player) || String.IsNullOrWhiteSpace(speaker) ||
                player==speaker)return null;
            return Events.LastOrDefault(e=>e.Kind=="RelationshipIntention" &&
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
