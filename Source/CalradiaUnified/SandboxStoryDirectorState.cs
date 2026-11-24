using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace CalradiaUnified
{
    // One modeled narrative thread per save. Evidence is a native observation,
    // not an invented conspiracy, promise, marriage or political action.
    public sealed class SandboxStoryDirectorState
    {
        public const string Version="CU_STORY_DIRECTOR_1";
        public const int MaxReceipts=24;
        public int Detail=1,Created=-1,Due=-1,Closed=-1;
        public string Owner="",Actor="",Kind="",Evidence="",Stage="",Response="";
        public readonly List<string> Receipts=new List<string>();
        public static int PeerInterval(int detail){return detail==0?0:detail==1?3:1;}
        public static int StoryCooldown(int detail){return detail==2?7:14;}
        public bool SetDetail(int detail)
        {
            if(detail<0 || detail>2)return false;
            Detail=detail;return true;
        }
        private static bool Id(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=80 &&
                s.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public static bool ValidKind(string kind)
        {
            return kind=="ChangedAmbition" || kind=="ImprovedStanding" ||
                kind=="StrainedStanding" || kind=="ChangedClan";
        }
        private static bool ValidResponse(string response)
        {return response=="Listen" || response=="Caution" || response=="Defer";}
        public static string Fingerprint(string actor,string kind,string detail)
        {
            uint hash=2166136261u;
            unchecked {foreach(char c in actor+"|"+kind+"|"+detail)hash=(hash^c)*16777619u;}
            return hash.ToString("x8",CultureInfo.InvariantCulture);
        }
        public bool Active {get{return Stage=="Open" || Stage=="FollowUp";}}
        public bool CanOpen(int day)
        {
            return Detail!=0 && !Active && day>=0 && day<=1999960 &&
                (Closed<0 || (long)day-Closed>=StoryCooldown(Detail));
        }
        public bool Open(string owner,string actor,string kind,string evidence,int day)
        {
            if(!CanOpen(day) || !Id(owner) || !Id(actor) || owner==actor ||
                !ValidKind(kind) || !Id(evidence) || Receipts.Contains(evidence))return false;
            Owner=owner;Actor=actor;Kind=kind;Evidence=evidence;
            Stage="Open";Response="";Created=day;Due=-1;Closed=-1;
            if(Receipts.Count>=MaxReceipts)Receipts.RemoveAt(0);
            Receipts.Add(evidence);return true;
        }
        public bool CanDiscuss(string owner,string actor,int day)
        {
            return owner==Owner && actor==Actor && Active && day>=Created &&
                (Stage=="Open"?day<=Created+30:day>=Due && day<=Due+21);
        }
        public bool Answer(string owner,string actor,string response,int day)
        {
            if(Stage!="Open" || !CanDiscuss(owner,actor,day) || !ValidResponse(response))return false;
            Response=response;
            if(response=="Defer"){Stage="Closed";Closed=day;return true;}
            Stage="FollowUp";Due=day+7;return true;
        }
        public bool Finish(string owner,string actor,int day)
        {
            if(Stage!="FollowUp" || !CanDiscuss(owner,actor,day))return false;
            Stage="Closed";Closed=day;return true;
        }
        public void Expire(int day)
        {
            if(day<0 || day>2000000)return;
            if((Stage=="Open" && day>Created+30) || (Stage=="FollowUp" && day>Due+21))
            {Stage="Expired";Closed=day;}
        }
        public string Serialize()
        {
            return String.Join("|",new[]{Version,Detail.ToString(CultureInfo.InvariantCulture),
                Owner,Actor,Kind,Evidence,Stage,Response,Created.ToString(CultureInfo.InvariantCulture),
                Due.ToString(CultureInfo.InvariantCulture),Closed.ToString(CultureInfo.InvariantCulture),
                String.Join(",",Receipts)});
        }
        public static SandboxStoryDirectorState Parse(string packed)
        {
            var s=new SandboxStoryDirectorState();
            if(String.IsNullOrEmpty(packed) || packed.Length>3000)return s;
            var f=packed.Split('|');int detail,created,due,closed;
            if(f.Length!=12 || f[0]!=Version || !Int32.TryParse(f[1],out detail) ||
                !s.SetDetail(detail) || !Int32.TryParse(f[8],out created) ||
                !Int32.TryParse(f[9],out due) || !Int32.TryParse(f[10],out closed))
                return new SandboxStoryDirectorState();
            var receipts=f[11].Length==0?new string[0]:f[11].Split(',');
            if(receipts.Length>MaxReceipts || receipts.Any(x=>!Id(x)) ||
                receipts.Distinct().Count()!=receipts.Length)return new SandboxStoryDirectorState();
            if(f[6]=="")
                return f.Skip(2).Take(6).All(x=>x=="") && created==-1 && due==-1 && closed==-1 &&
                    receipts.Length==0?s:new SandboxStoryDirectorState();
            if(!Id(f[2]) || !Id(f[3]) || f[2]==f[3] || !ValidKind(f[4]) ||
                !Id(f[5]) || !receipts.Contains(f[5]) || created<0 || created>1999960 ||
                (f[6]!="Open" && f[6]!="FollowUp" && f[6]!="Closed" && f[6]!="Expired"))
                return new SandboxStoryDirectorState();
            bool follow=f[7]=="Listen" || f[7]=="Caution";
            if(follow?(due<created+7 || due>created+37):due!=-1)return new SandboxStoryDirectorState();
            if(f[6]=="Open" && (f[7]!="" || closed!=-1) ||
                f[6]=="FollowUp" && (!follow || closed!=-1) ||
                f[6]=="Closed" && (closed<created || closed>2000000 ||
                    (!follow && (f[7]!="Defer" || closed>created+30)) ||
                    (follow && (closed<due || closed>due+21))) ||
                f[6]=="Expired" && (closed>2000000 ||
                    (follow?closed<=due+21:f[7]!="" || closed<=created+30)))
                return new SandboxStoryDirectorState();
            s.Owner=f[2];s.Actor=f[3];s.Kind=f[4];s.Evidence=f[5];s.Stage=f[6];s.Response=f[7];
            s.Created=created;s.Due=due;s.Closed=closed;s.Receipts.AddRange(receipts);return s;
        }
    }
}
