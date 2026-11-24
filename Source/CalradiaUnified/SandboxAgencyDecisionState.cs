using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Bounded save-backed NPC invitations. One pending request prevents
    /// spam, and actual native effects are applied only by a player choice.
    /// </summary>
    public sealed class SandboxAgencyDecisionState
    {
        public sealed class Record
        {
            public string HeroId="";
            public int LastDay=-1000,RequestsMade;
        }
        public int Day;
        public string PendingHeroId="",PendingKind="";
        public int PendingOpenedDay;
        public readonly List<Record> Records=new List<Record>();
        public const int MaxPending=8;
        public sealed class PendingRequest
        {
            public string HeroId="",Kind="";
            public int OpenedDay;
        }
        private readonly List<PendingRequest> waiting=new List<PendingRequest>();
        public bool HasPending{get{return PendingHeroId.Length>0;}}
        public PendingRequest[] PendingRequests
        {
            get{return (HasPending?new[]{new PendingRequest{HeroId=PendingHeroId,Kind=PendingKind,OpenedDay=PendingOpenedDay}}:
                new PendingRequest[0]).Concat(waiting.Select(x=>new PendingRequest{HeroId=x.HeroId,Kind=x.Kind,OpenedDay=x.OpenedDay})).ToArray();}
        }
        public int PendingCount{get{return (HasPending?1:0)+waiting.Count;}}
        public PendingRequest PendingFor(string id){return PendingRequests.FirstOrDefault(x=>x.HeroId==id);}
        public bool HasRoom{get{return PendingCount<MaxPending;}}
        public bool SelectPending(string id)
        {
            if(!HasPending)return false;
            if(PendingHeroId==id)return true;
            var next=waiting.FirstOrDefault(x=>x.HeroId==id);
            if(next==null)return false;
            waiting.Remove(next);
            waiting.Insert(0,new PendingRequest{HeroId=PendingHeroId,Kind=PendingKind,OpenedDay=PendingOpenedDay});
            PendingHeroId=next.HeroId;PendingKind=next.Kind;PendingOpenedDay=next.OpenedDay;
            return true;
        }
        public bool RemovePending(string id,string reason)
        {
            if(PendingFor(id)==null)return false;
            if(PendingHeroId==id){Dismiss(reason);return true;}
            return waiting.RemoveAll(x=>x.HeroId==id)==1;
        }
        public Record For(string id)
        {
            return Records.FirstOrDefault(x=>x.HeroId==id);
        }
        // Rotate opportunities among known people without bypassing
        // eligibility, cooldowns, or the one-pending-request rule.
        public string[] Prioritize(IEnumerable<string> ids)
        {
            if(ids==null)return new string[0];
            return ids.Where(id=>!String.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id=>For(id)==null?0:For(id).RequestsMade)
                .ThenBy(id=>For(id)==null?-1:For(id).LastDay)
                .ThenBy(id=>id,StringComparer.Ordinal).ToArray();
        }
        public void Tick()
        {
            if(Day<Int32.MaxValue)Day++;
            foreach(var request in PendingRequests.Where(x=>(long)Day-x.OpenedDay>=SandboxAgencyDecisionPolicy.ExpiryDays))
                RemovePending(request.HeroId,"Expired");
        }
        public bool Propose(string heroId,string kind)
        {
            return !HasPending && Enqueue(heroId,kind);
        }
        public bool Enqueue(string heroId,string kind)
        {
            if(!HasRoom || PendingFor(heroId)!=null || String.IsNullOrWhiteSpace(heroId) ||
                heroId.Length>160 || SandboxAgencyDecisionPolicy.Amount(kind)<0)return false;
            var r=For(heroId);
            if(r==null)
            {
                if(Records.Count>=64)return false;
                r=new Record{HeroId=heroId};
                Records.Add(r);
            }
            if(r.RequestsMade>=SandboxAgencyDecisionPolicy
                .MaxRequestsPerHero ||
                (long)Day-r.LastDay<
                    SandboxAgencyDecisionPolicy.CooldownDays)
                return false;
            r.RequestsMade++;
            r.LastDay=Day;
            if(!HasPending){PendingHeroId=heroId;PendingKind=kind;PendingOpenedDay=Day;}
            else waiting.Add(new PendingRequest{HeroId=heroId,Kind=kind,OpenedDay=Day});
            return true;
        }
        public string Dismiss(string reason)
        {
            if(!HasPending)return "none";
            PendingHeroId="";PendingKind="";
            PendingOpenedDay=0;
            if(waiting.Count>0)
            {
                var next=waiting[0];waiting.RemoveAt(0);
                PendingHeroId=next.HeroId;PendingKind=next.Kind;PendingOpenedDay=next.OpenedDay;
            }
            return reason;
        }
        private static string E(string s)
        {
            s=s??"";
            if(s.Length>160)s=s.Substring(0,160);
            return Uri.EscapeDataString(s);
        }
        private static string D(string s)
        {
            try
            {
                string value=Uri.UnescapeDataString(s??"");
                return value.Length>160?value.Substring(0,160):value;
            }
            catch{return "";}
        }
        public string Serialize()
        {
            var rows=new List<string>{
                waiting.Count==0?"CU_AGENCY_1":"CU_AGENCY_2",
                Day+"|"+E(PendingHeroId)+"|"+E(PendingKind)+
                    "|"+PendingOpenedDay};
            foreach(var r in Records.OrderBy(x=>x.HeroId,
                StringComparer.Ordinal))
                rows.Add("R|"+E(r.HeroId)+"|"+r.LastDay+"|"+
                    r.RequestsMade);
            foreach(var p in waiting)rows.Add("Q|"+E(p.HeroId)+"|"+E(p.Kind)+"|"+p.OpenedDay);
            return String.Join("\n",rows);
        }
        public static SandboxAgencyDecisionState Parse(string packed)
        {
            var result=new SandboxAgencyDecisionState();
            if(String.IsNullOrEmpty(packed) || packed.Length>131072)
                return result;
            var rows=packed.Replace("\r","").Split('\n');
            bool queued=rows[0]=="CU_AGENCY_2";
            if(rows.Length<2 || rows.Length>(queued?73:66) || (!queued && rows[0]!="CU_AGENCY_1"))
                return result;
            var head=rows[1].Split('|');
            int day,opened;
            if(head.Length!=4 || !Int32.TryParse(head[0],out day) ||
                !Int32.TryParse(head[3],out opened) ||
                day<0 || opened<0 || opened>day)
                return result;
            result.Day=day;
            string hero=D(head[1]),kind=D(head[2]);
            if(hero.Length>0 &&
                SandboxAgencyDecisionPolicy.Amount(kind)>=0 &&
                day-opened<SandboxAgencyDecisionPolicy.ExpiryDays)
            {
                result.PendingHeroId=hero;
                result.PendingKind=kind;
                result.PendingOpenedDay=opened;
            }
            foreach(var row in rows.Skip(2).Where(x=>x.StartsWith("R|",StringComparison.Ordinal)).Take(64))
            {
                var f=row.Split('|');
                int last,count;
                if(f.Length!=4 || f[0]!="R" ||
                    !Int32.TryParse(f[2],out last) ||
                    !Int32.TryParse(f[3],out count) ||
                    count<1 || count>SandboxAgencyDecisionPolicy
                        .MaxRequestsPerHero || last<0 || last>day)
                    continue;
                string id=D(f[1]);
                if(id.Length==0 || result.For(id)!=null)continue;
                result.Records.Add(new Record{
                    HeroId=id,LastDay=last,RequestsMade=count});
            }
            // Every legitimate pending invitation was created by Propose:
            // its person has a stored quota record, and that record's
            // LastDay is exactly the invitation's opening day. A damaged
            // save must not display a phantom NPC request or invent a
            // history event after loading.
            if(result.HasPending)
            {
                var pendingRecord=result.For(result.PendingHeroId);
                if(pendingRecord==null ||
                    pendingRecord.RequestsMade<1 ||
                    pendingRecord.LastDay!=result.PendingOpenedDay)
                    result.Dismiss("UnverifiedRestoredRequest");
            }
            if(queued)
                foreach(var row in rows.Skip(2).Where(x=>x.StartsWith("Q|",StringComparison.Ordinal)).Take(MaxPending-1))
                {
                    var q=row.Split('|');int openedDay;
                    if(q.Length!=4 || !Int32.TryParse(q[3],out openedDay) || openedDay<0 || openedDay>day ||
                        (long)day-openedDay>=SandboxAgencyDecisionPolicy.ExpiryDays)continue;
                    string id=D(q[1]),k=D(q[2]);var quota=result.For(id);
                    if(id.Length==0 || quota==null || quota.LastDay!=openedDay ||
                        SandboxAgencyDecisionPolicy.Amount(k)<0 || result.PendingFor(id)!=null || !result.HasRoom)continue;
                    if(!result.HasPending){result.PendingHeroId=id;result.PendingKind=k;result.PendingOpenedDay=openedDay;}
                    else result.waiting.Add(new PendingRequest{HeroId=id,Kind=k,OpenedDay=openedDay});
                }
            return result;
        }
    }

    // A modeled NPC-to-NPC initiative, never native consent or an
    // actual relation, marriage, gold, office or allegiance change.
    public sealed class SandboxPeerInitiativeState
    {
        public const string Version="CU_PEER_INITIATIVE_1";
        public const int MaxRecords=64, IntervalDays=7, PersonCooldown=30;
        public sealed class Entry
        {
            public string Actor="",Target="",Kind="",Outcome="";
            public int Day;
        }
        private readonly List<Entry> entries=new List<Entry>();
        public Entry[] Entries { get { return entries.ToArray(); } }
        private static bool Id(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=80 &&
                s.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public static string Kind(string goal)
        {
            switch(goal)
            {
                case "ProtectSharedHousehold":case "SupportHousehold":
                case "MaintainHousehold": return "HouseholdCooperation";
                case "EstablishLivelihood":case "BuildTradingContacts":
                    return "LivelihoodContact";
                case "GainMilitaryTraining":case "PreserveFamilyPosition":
                    return "PatronageInterest";
                default:return "";
            }
        }
        public static string Response(int relation,int generosity,int calculating)
        {
            if(relation< -10)return "ModeledDecline";
            if(relation>=15 && (generosity>0 || calculating<=0))
                return "ModeledInterest";
            return "ModeledDefer";
        }
        public string[] PrioritizePeers(IEnumerable<string> ids,int limit)
        {
            if(ids==null || limit<=0)return new string[0];
            return ids.Where(Id).Distinct(StringComparer.Ordinal)
                .OrderBy(id=>entries.Count(e=>e.Actor==id || e.Target==id))
                .ThenBy(id=>entries.Where(e=>e.Actor==id || e.Target==id)
                    .Select(e=>e.Day).DefaultIfEmpty(-1).Max())
                .ThenBy(id=>id,StringComparer.Ordinal).Take(Math.Min(limit,64)).ToArray();
        }
        private bool CanRecord(string actor,string target,int day)
        {
            return Id(actor) && Id(target) && actor!=target && day>=0 &&
                entries.Count<MaxRecords &&
                (entries.Count==0 || (long)day-entries[entries.Count-1].Day>=IntervalDays) &&
                !entries.Any(e=>(e.Actor==actor || e.Target==actor ||
                    e.Actor==target || e.Target==target) &&
                    (long)day-e.Day<PersonCooldown);
        }
        public bool Record(string actor,string target,string goal,int day,
            bool bothEligible,bool samePlace,int relation,int generosity,int calculating)
        {
            string kind=Kind(goal);
            if(!bothEligible || !samePlace || kind.Length==0 ||
                !CanRecord(actor,target,day))return false;
            entries.Add(new Entry{Actor=actor,Target=target,Kind=kind,
                Day=day,Outcome=Response(relation,generosity,calculating)});
            return true;
        }
        public string Serialize()
        {
            return String.Join("\n",new[]{Version}.Concat(entries.Select(e=>
                String.Join("|",new[]{e.Actor,e.Target,e.Kind,e.Outcome,
                    e.Day.ToString(System.Globalization.CultureInfo.InvariantCulture)}))));
        }
        public static SandboxPeerInitiativeState Parse(string packed)
        {
            var empty=new SandboxPeerInitiativeState();
            if(String.IsNullOrEmpty(packed)||packed.Length>16000)return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows[0]!=Version || rows.Length>MaxRecords+1)return empty;
            var state=new SandboxPeerInitiativeState();
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');int day;
                if(f.Length!=5 || !Int32.TryParse(f[4],out day) ||
                    !state.CanRecord(f[0],f[1],day) ||
                    (f[2]!="HouseholdCooperation" && f[2]!="LivelihoodContact" &&
                     f[2]!="PatronageInterest") ||
                    (f[3]!="ModeledDecline" && f[3]!="ModeledInterest" &&
                     f[3]!="ModeledDefer"))return empty;
                state.entries.Add(new Entry{Actor=f[0],Target=f[1],Kind=f[2],
                    Outcome=f[3],Day=day});
            }
            return state;
        }
    }
}
