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
        public bool HasPending{get{return PendingHeroId.Length>0;}}
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
            if(HasPending && Day-PendingOpenedDay>=
                SandboxAgencyDecisionPolicy.ExpiryDays)
                Dismiss("Expired");
        }
        public bool Propose(string heroId,string kind)
        {
            if(HasPending || String.IsNullOrWhiteSpace(heroId) ||
                SandboxAgencyDecisionPolicy.Amount(kind)<0)return false;
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
            PendingHeroId=heroId;
            PendingKind=kind;
            PendingOpenedDay=Day;
            return true;
        }
        public string Dismiss(string reason)
        {
            if(!HasPending)return "none";
            PendingHeroId="";PendingKind="";
            PendingOpenedDay=0;
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
                "CU_AGENCY_1",
                Day+"|"+E(PendingHeroId)+"|"+E(PendingKind)+
                    "|"+PendingOpenedDay};
            foreach(var r in Records.OrderBy(x=>x.HeroId,
                StringComparer.Ordinal))
                rows.Add("R|"+E(r.HeroId)+"|"+r.LastDay+"|"+
                    r.RequestsMade);
            return String.Join("\n",rows);
        }
        public static SandboxAgencyDecisionState Parse(string packed)
        {
            var result=new SandboxAgencyDecisionState();
            if(String.IsNullOrEmpty(packed) || packed.Length>18000)
                return result;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length<2 || rows[0]!="CU_AGENCY_1")
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
            foreach(var row in rows.Skip(2).Take(64))
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
            return result;
        }
    }
}
