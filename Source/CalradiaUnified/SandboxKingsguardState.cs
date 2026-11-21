using System;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// The Kingsguard is a saved detachment designation over genuine
    /// regular native troops in the ruler's actual party. It never
    /// invents a CharacterObject, equipment ID, mission Agent or wage.
    /// Individual roster members of the same troop type are not tagged.
    /// </summary>
    public sealed class SandboxKingsguardState
    {
        public const string Version="CU_KINGS_GUARD_1";
        public const int MaxGuard=12;
        public const int RecruitBatch=2;
        public const int MinNativeTier=3;
        public string KingdomId="",RoyalSeatId="",KingId="";
        public string CommanderId="",MarshalId="",MinisterId="";
        public string TroopId="";
        public int DesignatedCount,LastObservedNativeCount;
        public int LastObservedDay=-1,RecruitmentActions;
        public bool IsRecorded
        {
            get{return ValidId(KingdomId) && ValidId(RoyalSeatId) &&
                ValidId(KingId) && ValidId(CommanderId) &&
                ValidId(MarshalId) && ValidId(MinisterId) &&
                DesignatedCount>=0 && DesignatedCount<=MaxGuard &&
                LastObservedNativeCount>=0 &&
                RecruitmentActions>=0 && RecruitmentActions<=10000;}
        }
        private static bool ValidId(string text)
        {
            return !String.IsNullOrWhiteSpace(text) &&
                text.Length<=100 && text.All(c=>
                    Char.IsLetterOrDigit(c) || c=='_' ||
                    c=='-' || c=='.');
        }
        public string Begin(string kingdom,string seat,string king,
            string commander,string marshal,string minister,
            int campaignDay)
        {
            if(IsRecorded)return "already_commissioned";
            if(!ValidId(kingdom) || !ValidId(seat) ||
               !ValidId(king) || !ValidId(commander) ||
               !ValidId(marshal) || !ValidId(minister) ||
               king==commander || king==marshal || king==minister ||
               commander==marshal || commander==minister ||
               marshal==minister || campaignDay<0)
                return "native_royal_identity_unverified";
            KingdomId=kingdom;RoyalSeatId=seat;KingId=king;
            CommanderId=commander;MarshalId=marshal;
            MinisterId=minister;LastObservedDay=campaignDay;
            return "native_kingsguard_commissioned";
        }
        public static int CanRecruit(int availableInGarrison,
            int currentNativePartyCount,int designatedCount,
            int partyOpenSlots,bool eligibleNativeGear,int nativeTier)
        {
            if(!eligibleNativeGear || nativeTier<MinNativeTier ||
               availableInGarrison<=0 || partyOpenSlots<=0 ||
               currentNativePartyCount<0 || designatedCount<0 ||
               designatedCount>MaxGuard)
                return 0;
            return Math.Min(RecruitBatch,Math.Min(availableInGarrison,
                Math.Min(partyOpenSlots,MaxGuard-designatedCount)));
        }
        public string RecordNativeTransfer(string troopId,int moved,
            int beforeGarrison,int afterGarrison,
            int beforeParty,int afterParty,int campaignDay)
        {
            if(!IsRecorded)return "guard_not_commissioned";
            if(campaignDay<LastObservedDay || moved<1 ||
               moved>RecruitBatch || !ValidId(troopId) ||
               (TroopId.Length>0 && TroopId!=troopId) ||
               beforeGarrison-afterGarrison!=moved ||
               afterParty-beforeParty!=moved ||
               afterParty<moved ||
               DesignatedCount+moved>MaxGuard)
                return "native_troop_transfer_not_verified";
            if(TroopId.Length==0)TroopId=troopId;
            DesignatedCount+=moved;
            LastObservedNativeCount=afterParty;
            LastObservedDay=campaignDay;
            RecruitmentActions++;
            return "native_guard_recruitment_verified";
        }
        public string Reconcile(int nativePartyCount,int day)
        {
            if(!IsRecorded)return "guard_not_commissioned";
            if(day<LastObservedDay || nativePartyCount<0)
                return "invalid_native_roster_observation";
            if(TroopId.Length==0)
            {
                LastObservedDay=day;
                return "no_native_kingguard_troops";
            }
            int old=DesignatedCount;
            DesignatedCount=Math.Min(DesignatedCount,nativePartyCount);
            LastObservedNativeCount=nativePartyCount;
            LastObservedDay=day;
            return old!=DesignatedCount?
                "native_guard_casualties_observed":
                "native_guard_roster_unchanged";
        }
        private static string E(string v)
        {
            return Uri.EscapeDataString(v??"");
        }
        private static string D(string v)
        {
            try{return Uri.UnescapeDataString(v??"");}
            catch{return "";}
        }
        public string Serialize()
        {
            if(!IsRecorded)return Version;
            return Version+"\n"+String.Join("|",new[]{
                E(KingdomId),E(RoyalSeatId),E(KingId),
                E(CommanderId),E(MarshalId),E(MinisterId),E(TroopId),
                DesignatedCount.ToString(CultureInfo.InvariantCulture),
                LastObservedNativeCount.ToString(CultureInfo.InvariantCulture),
                LastObservedDay.ToString(CultureInfo.InvariantCulture),
                RecruitmentActions.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxKingsguardState Parse(string source)
        {
            var blank=new SandboxKingsguardState();
            if(String.IsNullOrEmpty(source) || source==Version ||
               source.Length>3072)return blank;
            var rows=source.Split('\n');
            if(rows.Length!=2 || rows[0]!=Version)return blank;
            var parts=rows[1].Split('|');
            if(parts.Length!=11)return blank;
            int count,observed,day,actions;
            if(!Int32.TryParse(parts[7],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out count) ||
               !Int32.TryParse(parts[8],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out observed) ||
               !Int32.TryParse(parts[9],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out day) ||
               !Int32.TryParse(parts[10],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out actions) ||
               day<0 || count<0 || count>MaxGuard ||
               observed<0 || actions<0 || actions>10000)
                return blank;
            var result=new SandboxKingsguardState{
                KingdomId=D(parts[0]),RoyalSeatId=D(parts[1]),
                KingId=D(parts[2]),CommanderId=D(parts[3]),
                MarshalId=D(parts[4]),MinisterId=D(parts[5]),
                TroopId=D(parts[6]),DesignatedCount=count,
                LastObservedNativeCount=observed,LastObservedDay=day,
                RecruitmentActions=actions};
            if(result.TroopId.Length>0 && !ValidId(result.TroopId))
                return blank;
            if(result.TroopId.Length==0 && (count!=0 || actions!=0))
                return blank;
            if(new[]{result.KingId,result.CommanderId,result.MarshalId,
                result.MinisterId}.Distinct(StringComparer.Ordinal).Count()!=4)
                return blank;
            return result.IsRecorded?result:blank;
        }
    }
}
