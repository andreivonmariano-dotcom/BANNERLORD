using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A royal household preference and a read-only native monarchy
    /// observation. This can never elect a king, change a clan leader,
    /// transfer a settlement or invent an underage regent.
    /// Optional marriage and children are valid founding configurations.
    /// </summary>
    public sealed class SandboxKingSuccessionState
    {
        public const string Version="CU_KING_CLAIMS_1";
        public string FounderId="",RulingClanId="",KingdomId="";
        public string Rule=SandboxLordSuccessionPolicy.EldestChild;
        public string PreferredHeirId="",RankedHeirIds="";
        public string ActualRulingClanId="",ActualRulerId="";
        public string Status="Unrecorded";
        public int LastObservedDay=-1,Changes;
        public bool IsRecorded {get{return
            FounderId.Length>0 && RulingClanId.Length>0 &&
            KingdomId.Length>0 &&
            SandboxLordSuccessionPolicy.IsRule(Rule) &&
            Status!="Unrecorded";}}
        private static bool ValidId(string v)
        {
            return !String.IsNullOrWhiteSpace(v) &&
                v.Length<=100 && v.All(c=>Char.IsLetterOrDigit(c) ||
                    c=='_' || c=='-' || c=='.');
        }
        private static string Encode(string v)
        {
            return Uri.EscapeDataString(v??"");
        }
        private static string Decode(string v)
        {
            try{return Uri.UnescapeDataString(v??"");}
            catch{return "";}
        }
        private static string ObservedStatus(bool founderAlive,
            bool sameRulingHouse,string founderId,string currentRuler,
            SandboxLordSuccessionPolicy.Candidate heir)
        {
            if(founderAlive)
                return sameRulingHouse && currentRuler==founderId ?
                    (heir==null?"NativeReignNoChildClaim":
                     "NativeReignHouseholdClaimOnly"):
                    "NativeRulershipChangedDuringFounderLife";
            if(heir==null)return "FounderDeadNoVerifiedHeir";
            if(sameRulingHouse && currentRuler==heir.Id)
                return "NativeRoyalHeirObserved";
            return heir.Minor?"MinorRoyalClaimNativeDecisionPending":
                "NativeRoyalDecisionDoesNotMatchPreference";
        }
        public string Begin(string founder,string rulingClan,
            string kingdom,string rule,
            IEnumerable<SandboxLordSuccessionPolicy.Candidate> children,
            int day)
        {
            if(IsRecorded)return "already_recorded";
            if(!ValidId(founder) || !ValidId(rulingClan) ||
                !ValidId(kingdom) ||
                !SandboxLordSuccessionPolicy.IsRule(rule) || day<0)
                return "invalid_native_royal_identity";
            var ranked=SandboxLordSuccessionPolicy.Order(rule,children);
            FounderId=founder;RulingClanId=rulingClan;
            KingdomId=kingdom;Rule=rule;
            ActualRulerId=founder;ActualRulingClanId=rulingClan;
            RankedHeirIds=String.Join(",",ranked.Select(c=>c.Id));
            PreferredHeirId=ranked.Length>0?ranked[0].Id:"";
            Status=ObservedStatus(true,true,founder,founder,
                ranked.Length>0?ranked[0]:null);
            LastObservedDay=day;
            return "native_royal_household_recorded";
        }
        public string Observe(int day,bool founderAlive,
            string observedRulingClan,string observedRuler,
            IEnumerable<SandboxLordSuccessionPolicy.Candidate> children)
        {
            if(!IsRecorded)return "not_recorded";
            if(day<LastObservedDay || day<0)
                return "invalid_observation_day";
            if(!ValidId(observedRulingClan) ||
                !ValidId(observedRuler))
                return "native_kingdom_ruler_not_verified";
            var ranked=SandboxLordSuccessionPolicy.Order(Rule,children);
            var next=ranked.Length>0?ranked[0].Id:"";
            var all=String.Join(",",ranked.Select(c=>c.Id));
            var status=ObservedStatus(founderAlive,
                observedRulingClan==RulingClanId,FounderId,
                observedRuler,ranked.Length>0?ranked[0]:null);
            bool change=next!=PreferredHeirId ||
                all!=RankedHeirIds ||
                observedRulingClan!=ActualRulingClanId ||
                observedRuler!=ActualRulerId || status!=Status;
            PreferredHeirId=next;RankedHeirIds=all;
            ActualRulingClanId=observedRulingClan;
            ActualRulerId=observedRuler;
            Status=status;LastObservedDay=day;
            if(change)
            {
                Changes++;
                return "native_crown_observation_changed";
            }
            return "native_crown_unchanged";
        }
        public string Serialize()
        {
            if(!IsRecorded)return Version;
            return Version+"\n"+String.Join("|",new[]{
                Encode(FounderId),Encode(RulingClanId),
                Encode(KingdomId),Encode(Rule),
                Encode(PreferredHeirId),Encode(RankedHeirIds),
                Encode(ActualRulingClanId),Encode(ActualRulerId),
                Encode(Status),
                LastObservedDay.ToString(CultureInfo.InvariantCulture),
                Changes.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxKingSuccessionState Parse(string text)
        {
            var blank=new SandboxKingSuccessionState();
            if(String.IsNullOrEmpty(text) || text==Version ||
                text.Length>4096)return blank;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return blank;
            var f=lines[1].Split('|');
            int day,changes;
            if(f.Length!=11 ||
                !Int32.TryParse(f[9],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out day) ||
                !Int32.TryParse(f[10],NumberStyles.None,
                    CultureInfo.InvariantCulture,out changes) ||
                day<0 || changes>1000000)return blank;
            var ids=new string[9];
            for(int i=0;i<9;i++)ids[i]=Decode(f[i]);
            if(!ValidId(ids[0]) || !ValidId(ids[1]) ||
                !ValidId(ids[2]) ||
                !SandboxLordSuccessionPolicy.IsRule(ids[3]) ||
                (ids[4].Length>0 && !ValidId(ids[4])) ||
                !ValidId(ids[6]) || !ValidId(ids[7]) ||
                !new[]{
                    "NativeReignNoChildClaim",
                    "NativeReignHouseholdClaimOnly",
                    "NativeRulershipChangedDuringFounderLife",
                    "FounderDeadNoVerifiedHeir",
                    "NativeRoyalHeirObserved",
                    "MinorRoyalClaimNativeDecisionPending",
                    "NativeRoyalDecisionDoesNotMatchPreference"
                }.Contains(ids[8],StringComparer.Ordinal))return blank;
            var heirs=ids[5].Length==0?
                new string[0]:ids[5].Split(',');
            if(heirs.Length>SandboxOriginProfile.MaxChildren+12 ||
                heirs.Any(x=>!ValidId(x)) ||
                heirs.Distinct(StringComparer.Ordinal).Count()!=heirs.Length ||
                (heirs.Length==0 && ids[4].Length>0) ||
                (heirs.Length>0 && heirs[0]!=ids[4]))return blank;
            return new SandboxKingSuccessionState{
                FounderId=ids[0],RulingClanId=ids[1],
                KingdomId=ids[2],Rule=ids[3],
                PreferredHeirId=ids[4],RankedHeirIds=ids[5],
                ActualRulingClanId=ids[6],ActualRulerId=ids[7],
                Status=ids[8],LastObservedDay=day,Changes=changes};
        }
    }
}
