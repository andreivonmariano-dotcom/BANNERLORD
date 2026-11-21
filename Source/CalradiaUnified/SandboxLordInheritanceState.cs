using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A save-backed, read-only observation of actual native heirs and
    /// native clan leadership. Never installs a regent or moves a fief.
    /// A newer pregame origin.ini never replaces this campaign record.
    /// </summary>
    public sealed class SandboxLordInheritanceState
    {
        public const string Version="CU_LORD_HEIRS_1";
        public string FounderId="",SpouseId="",ClanId="",KingdomId="";
        public string Rule=SandboxLordSuccessionPolicy.EldestChild;
        public string PrimaryHeirId="",NativeLeaderId="",Status="Unrecorded";
        public string RankedHeirIds="";
        public int LastObservedDay=-1,Changes;
        public bool IsRecorded
        {
            get{return FounderId.Length>0 && ClanId.Length>0 &&
                SpouseId.Length>0 && KingdomId.Length>0 &&
                SandboxLordSuccessionPolicy.IsRule(Rule) &&
                Status!="Unrecorded";}
        }
        public string Begin(string founder,string spouse,string clan,
            string kingdom,string rule,
            IEnumerable<SandboxLordSuccessionPolicy.Candidate> heirs,
            int day)
        {
            if(IsRecorded)return "already_recorded";
            if(!Id(founder) || !Id(spouse) || !Id(clan) ||
               !Id(kingdom) || founder==spouse ||
               !SandboxLordSuccessionPolicy.IsRule(rule) ||
               day<0)return "invalid_native_founder";
            var ranked=SandboxLordSuccessionPolicy.Order(rule,heirs);
            if(ranked.Length<1)return "no_verified_native_child";
            FounderId=founder;SpouseId=spouse;
            ClanId=clan;KingdomId=kingdom;Rule=rule;
            NativeLeaderId=founder;
            LastObservedDay=day;Changes=0;
            PrimaryHeirId=ranked[0].Id;
            RankedHeirIds=String.Join(",",ranked.Select(x=>x.Id));
            Status=SandboxLordSuccessionPolicy.Status(
                true,true,founder,ranked[0]);
            return "native_household_heirs_recorded";
        }
        public string Observe(int day,bool founderAlive,
            string nativeClanLeader,
            IEnumerable<SandboxLordSuccessionPolicy.Candidate> heirs)
        {
            if(!IsRecorded)return "not_recorded";
            if(day<LastObservedDay || day<0)
                return "invalid_observation_day";
            if(!Id(nativeClanLeader))
                return "native_leader_not_resolved";
            var ranked=SandboxLordSuccessionPolicy.Order(Rule,heirs);
            string next=ranked.Length==0?"":ranked[0].Id;
            string ordered=String.Join(",",ranked.Select(x=>x.Id));
            string status=SandboxLordSuccessionPolicy.Status(
                founderAlive,nativeClanLeader==FounderId,
                nativeClanLeader,ranked.Length==0?null:ranked[0]);
            bool changed=next!=PrimaryHeirId ||
                ordered!=RankedHeirIds || nativeClanLeader!=NativeLeaderId ||
                status!=Status;
            PrimaryHeirId=next;RankedHeirIds=ordered;
            NativeLeaderId=nativeClanLeader;Status=status;
            LastObservedDay=day;
            if(changed)
            {
                Changes++;
                return "native_succession_observation_changed";
            }
            return "native_succession_unchanged";
        }
        private static bool Id(string id)
        {
            if(String.IsNullOrWhiteSpace(id) || id.Length>100)
                return false;
            return id.All(c=>Char.IsLetterOrDigit(c) ||
                c=='_' || c=='-' || c=='.');
        }
        private static string E(string value)
        {
            return Uri.EscapeDataString(value??"");
        }
        private static string D(string value)
        {
            try{return Uri.UnescapeDataString(value??"");}
            catch{return "";}
        }
        public string Serialize()
        {
            if(!IsRecorded)return Version;
            return Version+"\n"+String.Join("|",new[]{
                E(FounderId),E(SpouseId),E(ClanId),E(KingdomId),
                E(Rule),E(PrimaryHeirId),E(NativeLeaderId),
                E(Status),E(RankedHeirIds),
                LastObservedDay.ToString(CultureInfo.InvariantCulture),
                Changes.ToString(CultureInfo.InvariantCulture)
            });
        }
        public static SandboxLordInheritanceState Parse(string text)
        {
            var empty=new SandboxLordInheritanceState();
            if(String.IsNullOrEmpty(text) || text==Version ||
                text.Length>4096)return empty;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return empty;
            var f=lines[1].Split('|');
            if(f.Length!=11)return empty;
            var rule=D(f[4]);var status=D(f[7]);
            int day,changes;
            if(!SandboxLordSuccessionPolicy.IsRule(rule) ||
               !new[]{
                  "MinorHouseholdClaimOnly","AdultHouseholdClaimOnly",
                  "NativeLeadershipChangedDuringFounderLife",
                  "NativeHeirLeadershipObserved",
                  "MinorClaimNativeDecisionPending",
                  "NativeSuccessionNotFollowingHouseholdOrder",
                  "NoVerifiedLivingChild"
               }.Contains(status,StringComparer.Ordinal) ||
               !Int32.TryParse(f[9],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out day) ||
               !Int32.TryParse(f[10],NumberStyles.None,
                   CultureInfo.InvariantCulture,out changes) ||
               day<0 || changes>1000000)return empty;
            var founder=D(f[0]);var spouse=D(f[1]);
            var clan=D(f[2]);var kingdom=D(f[3]);
            var primary=D(f[5]);var leader=D(f[6]);
            var ranked=D(f[8]);
            var ids=ranked.Length==0?new string[0]:ranked.Split(',');
            if(!Id(founder) || !Id(spouse) || founder==spouse ||
               !Id(clan) || !Id(kingdom) || !Id(leader) ||
               (primary.Length!=0 && !Id(primary)) ||
               ids.Length>SandboxOriginProfile.MaxChildren+12 ||
               ids.Any(x=>!Id(x)) ||
               ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length ||
               (ids.Length>0 && ids[0]!=primary) ||
               (ids.Length==0 && primary.Length>0))
                return empty;
            return new SandboxLordInheritanceState{
                FounderId=founder,SpouseId=spouse,ClanId=clan,
                KingdomId=kingdom,Rule=rule,PrimaryHeirId=primary,
                NativeLeaderId=leader,Status=status,RankedHeirIds=ranked,
                LastObservedDay=day,Changes=changes};
        }
    }
}
