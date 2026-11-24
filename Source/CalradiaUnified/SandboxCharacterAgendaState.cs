using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Persistent authored ambitions with only observed native evidence.
    /// A home is named only when Bannerlord actually locates the Hero.
    /// Shared service can modestly increase genuine native sibling ties.
    /// </summary>
    public sealed class SandboxCharacterAgendaState
    {
        public sealed class Entry
        {
            public string Id="",Goal="",HomeId="",HomeEvidence="Unverified",
                CurrentOccupation="",Culture="",ClanId="";
            public int ConsecutiveDaysTogether,ComradeshipAwards;
            // One transient proposal per daily observation. A proposal
            // is never serialized as a confirmed native relationship.
            public bool AwardProposed;
        }
        public const int MaxEntries=64;
        public readonly List<Entry> Entries=new List<Entry>();
        public Entry Get(string id)
        {
            return Entries.FirstOrDefault(x=>x.Id==id);
        }
        public bool Register(string id,string goal,string home,
            string evidence,string occupation,string culture,string clan)
        {
            if(String.IsNullOrWhiteSpace(id))return false;
            var e=Get(id);
            if(e==null)
            {
                if(Entries.Count>=MaxEntries)return false;
                e=new Entry{Id=id};
                Entries.Add(e);
            }
            e.Goal=goal??"";
            e.HomeId=home??"";
            e.HomeEvidence=evidence??"Unverified";
            e.CurrentOccupation=occupation??"";
            e.Culture=culture??"";
            e.ClanId=clan??"";
            return true;
        }
        public string ObserveHome(string id,string observedHome,
            string evidence)
        {
            var e=Get(id);
            if(e==null)return "unknown";
            if(String.IsNullOrEmpty(observedHome))
            {
                // An old verified home must not masquerade as the Hero's
                // presently confirmed residence. Unknown is not a move.
                if(e.HomeId.Length==0 && e.HomeEvidence=="Unverified")
                    return "same";
                e.HomeId="";
                e.HomeEvidence="Unverified";
                return "residence_no_longer_verified";
            }
            if(e.HomeId==observedHome && e.HomeEvidence==evidence)
                return "same";
            e.HomeId=observedHome;
            e.HomeEvidence=evidence??"NativeObserved";
            return "native_home_changed";
        }
        public string ObserveNativeGoal(string id,string inferredGoal,
            string nativeOccupation,string nativeClanId)
        {
            var e=Get(id);
            if(e==null || String.IsNullOrWhiteSpace(inferredGoal))
                return "no_evidence";
            bool changed=e.Goal!=inferredGoal;
            e.CurrentOccupation=nativeOccupation??"";
            e.ClanId=nativeClanId??"";
            if(!changed)return "same";
            e.Goal=inferredGoal;
            e.ConsecutiveDaysTogether=0;
            e.AwardProposed=false;
            // Persist the inferred goal, never invent a personal decision
            // or spend a relation award without the actual game action.
            return "goal_reassessed_from_native_context";
        }
        public int ObserveComradeship(string id,bool sameRealParty,
            bool adult,bool verifiedSharedParents,int nativeRelation)
        {
            var e=Get(id);
            if(e==null)return 0;
            e.AwardProposed=false;
            if(e.Goal!="ProtectSharedHousehold" ||
                !SandboxGeneratedCharacterGoalPolicy.CanShareMilitaryService(
                    adult,verifiedSharedParents,sameRealParty))
            {
                e.ConsecutiveDaysTogether=0;
                return 0;
            }
            if(e.ConsecutiveDaysTogether<Int32.MaxValue)
                e.ConsecutiveDaysTogether++;
            int increment=
                SandboxGeneratedCharacterGoalPolicy.ComradeshipIncrement(
                    e.ConsecutiveDaysTogether,e.ComradeshipAwards,
                    nativeRelation);
            // The native relation change can fail or be rejected.
            // Propose a point here, but do not consume the lifetime
            // award until the campaign confirms a real relation increase.
            e.AwardProposed=increment>0;
            return increment;
        }
        public bool ConfirmComradeshipAward(string id,
            int nativeRelationBefore,int nativeRelationAfter)
        {
            var e=Get(id);
            if(e==null || !e.AwardProposed)return false;
            e.AwardProposed=false;
            if(e.Goal!="ProtectSharedHousehold" ||
                nativeRelationBefore< -100 || nativeRelationBefore>100 ||
                nativeRelationAfter< -100 || nativeRelationAfter>100 ||
                nativeRelationAfter<=nativeRelationBefore ||
                SandboxGeneratedCharacterGoalPolicy.ComradeshipIncrement(
                    e.ConsecutiveDaysTogether,e.ComradeshipAwards,
                    nativeRelationBefore)<=0)
                return false;
            e.ComradeshipAwards++;
            return true;
        }
        private static string E(string value)
        {
            value=value??"";
            if(value.Length>180)value=value.Substring(0,180);
            return Uri.EscapeDataString(value);
        }
        private static string D(string value)
        {
            try
            {
                var s=Uri.UnescapeDataString(value??"");
                return s.Length>180?s.Substring(0,180):s;
            }
            catch{return "";}
        }
        public string Serialize()
        {
            var rows=new List<string>{"CU_AGENDA_1"};
            foreach(var e in Entries.OrderBy(x=>x.Id,
                StringComparer.Ordinal))
                rows.Add("P|"+String.Join("|",new[]{
                    E(e.Id),E(e.Goal),E(e.HomeId),
                    E(e.HomeEvidence),E(e.CurrentOccupation),
                    E(e.Culture),E(e.ClanId),
                    e.ConsecutiveDaysTogether.ToString(),
                    e.ComradeshipAwards.ToString()}));
            return String.Join("\n",rows);
        }
        public static SandboxCharacterAgendaState Parse(string packed)
        {
            var result=new SandboxCharacterAgendaState();
            if(String.IsNullOrEmpty(packed) || packed.Length>45000)
                return result;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length==0 || rows[0]!="CU_AGENDA_1")
                return result;
            foreach(var row in rows.Skip(1).Take(MaxEntries))
            {
                var f=row.Split('|');
                int days,pulses;
                if(f.Length!=10 || f[0]!="P" ||
                    !Int32.TryParse(f[8],out days) ||
                    !Int32.TryParse(f[9],out pulses) ||
                    days<0 || pulses<0 || pulses>5)continue;
                var id=D(f[1]);
                // A serialized agenda contains one row per hero. Never let
                // a later duplicate replace the first restored evidence.
                if(result.Get(id)!=null)continue;
                if(!result.Register(id,D(f[2]),D(f[3]),
                    D(f[4]),D(f[5]),D(f[6]),D(f[7])))continue;
                var e=result.Get(id);
                e.ConsecutiveDaysTogether=days;
                e.ComradeshipAwards=pulses;
            }
            return result;
        }
    }
}
