using System;
using System.Linq;
using System.Globalization;

namespace CalradiaUnified
{
    // One scheduled, optional gathering. Acceptances are modeled responses,
    // not commands to travel. Attendance is checked by the campaign adapter.
    public sealed class SandboxGatheringPlan
    {
        public const string Version="CU_GATHERING_1";
        public string Owner="",Parent="",Seat="",Occasion="",Status="";
        public string[] Invited=new string[0],Accepted=new string[0];
        public int Created=-1,Due=-1,Closed=-1;
        private static bool Id(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=80 &&
                s.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public static bool ValidOccasion(string s)
        {
            return s=="FamilyGathering" || s=="Fellowship" || s=="MarriageCelebration" ||
                s=="Remembrance" || s=="CourtAssembly";
        }
        private static bool ValidParent(string s)
        {
            return s=="cu_people_lineage" || s=="cu_native_royal_council";
        }
        public static bool AcceptInvitation(int relation,string tone,string occasion)
        {
            if(!ValidOccasion(occasion) || relation< -100 || relation>100)return false;
            // A guarded person can decline; a neutral or friendly person
            // accepts an ordinary local invitation without promising travel.
            return relation>=0 || (relation>=-10 && tone=="Dutiful" && occasion=="CourtAssembly");
        }
        public bool CanPlan(int day)
        {
            return day>=0 && day<=1999995 && Status!="Pending" &&
                (Closed<0 || (long)day-Closed>=7);
        }
        public bool Schedule(string owner,string parent,string seat,string occasion,int day,
            string[] invited,string[] accepted)
        {
            if(!CanPlan(day) || !Id(owner) || !ValidParent(parent) || !Id(seat) ||
                !ValidOccasion(occasion) || invited==null || accepted==null ||
                invited.Length<2 || invited.Length>4 || accepted.Length>invited.Length ||
                invited.Any(x=>!Id(x)||x==owner) || invited.Distinct().Count()!=invited.Length ||
                accepted.Distinct().Count()!=accepted.Length || accepted.Any(x=>!invited.Contains(x)))
                return false;
            Owner=owner;Parent=parent;Seat=seat;Occasion=occasion;
            Created=day;Due=day+2;Closed=-1;Status="Pending";
            Invited=invited.ToArray();Accepted=accepted.ToArray();return true;
        }
        public bool IsDue(string owner,string parent,string seat,int day)
        {
            return Status=="Pending" && Owner==owner && Parent==parent && Seat==seat &&
                day>=Due && day<=Due+3;
        }
        public bool Finish(string owner,int day,string[] present)
        {
            if(Status!="Pending" || owner!=Owner || day<Due || day>Due+3 ||
                present==null || present.Length<2 || present.Distinct().Count()!=present.Length ||
                present.Any(x=>!Accepted.Contains(x)))return false;
            Status="Held";Closed=day;return true;
        }
        public bool Cancel(string owner,int day)
        {
            if(Status!="Pending" || owner!=Owner || day<Created || day>2000000)return false;
            Status="Cancelled";Closed=day;return true;
        }
        public void Expire(int day)
        {
            if(Status=="Pending" && day>Due+3 && day<=2000000)
            {Status="Expired";Closed=day;}
        }
        public string Serialize()
        {
            if(Status.Length==0)return "";
            return String.Join("|",new[]{Version,Owner,Parent,Seat,Occasion,Status,
                Created.ToString(CultureInfo.InvariantCulture),Due.ToString(CultureInfo.InvariantCulture),
                Closed.ToString(CultureInfo.InvariantCulture),String.Join(",",Invited),String.Join(",",Accepted)});
        }
        public static SandboxGatheringPlan Parse(string packed)
        {
            var p=new SandboxGatheringPlan();
            if(String.IsNullOrEmpty(packed) || packed.Length>1500)return p;
            var f=packed.Split('|');int created,due,closed;
            if(f.Length!=11 || f[0]!=Version || !Int32.TryParse(f[6],out created) ||
                !Int32.TryParse(f[7],out due) || !Int32.TryParse(f[8],out closed) ||
                !p.Schedule(f[1],f[2],f[3],f[4],created,f[9].Split(','),
                    f[10].Length==0?new string[0]:f[10].Split(',')) || due!=p.Due)
                return new SandboxGatheringPlan();
            if(f[5]=="Pending" && closed==-1)return p;
            if(closed<created || closed>2000000 ||
                (f[5]=="Held" && (closed<due || closed>due+3 || p.Accepted.Length<2)) ||
                (f[5]=="Expired" && closed<=due+3) ||
                (f[5]!="Held" && f[5]!="Expired" && f[5]!="Cancelled"))return new SandboxGatheringPlan();
            p.Status=f[5];p.Closed=closed;return p;
        }
    }
}
