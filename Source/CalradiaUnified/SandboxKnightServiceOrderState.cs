using System;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Real sworn-lord duty orders. They are personal service records,
    /// never a native kingdom promotion, quest-target spawn or wage.
    /// Reports must be made in person to the exact living oath patron.
    /// </summary>
    public sealed class SandboxKnightServiceOrderState
    {
        public const string Version="CU_KNIGHT_SERVICE_1";
        public string PatronId="",Order="",Status="Available";
        public string FirstTownId="",SecondTownId="";
        public int StartDay=-1,DueDay=-1,LastObservedDay=-1,
            CompletedOrders,MissedOrders,LastReportedDay=-1;
        public bool Active {get{return Status=="Active";}}
        private static bool Id(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public bool CanBegin(string patron,int day)
        {
            return Id(patron) && day>=0 && day<Int32.MaxValue-30 &&
                !Active && (LastReportedDay<0 ||
                   day>=LastReportedDay+7);
        }
        public string Begin(string patron,string order,int day)
        {
            if(!CanBegin(patron,day) ||
                (order!="Scout" && order!="Muster"))
                return "native_service_order_unavailable";
            PatronId=patron;Order=order;Status="Active";
            StartDay=day;DueDay=day+14;
            LastObservedDay=day;FirstTownId="";SecondTownId="";
            return "native_patron_order_accepted";
        }
        public string Expire(int day)
        {
            if(!Active || day<=DueDay)return "order_not_expired";
            Status="Expired";
            if(MissedOrders<10000)MissedOrders++;
            return "native_order_expired";
        }
        public string Observe(int day,bool partyReady,string townId,
            bool belongsToPatronRealm)
        {
            if(!Active)return "no_active_patron_order";
            if(day>DueDay)
            {
                Status="Expired";
                if(MissedOrders<10000)MissedOrders++;
                return "native_order_expired";
            }
            if(day<=LastObservedDay)return "not_new_native_day";
            LastObservedDay=day;
            if(Order=="Muster")
            {
                // Enough troops, after maintaining the native party
                // for at least three in-game days.
                if(day>=StartDay+3 && partyReady)
                    return "muster_strength_verified";
                return "muster_strength_not_yet_verified";
            }
            if(Order=="Scout" && belongsToPatronRealm &&
                Id(townId))
            {
                if(FirstTownId.Length==0)
                {
                    FirstTownId=townId;
                    return "native_first_town_scouted";
                }
                if(SecondTownId.Length==0 &&
                    townId!=FirstTownId)
                {
                    SecondTownId=townId;
                    return "native_second_town_scouted";
                }
            }
            return "native_scout_evidence_missing";
        }
        public bool CanReport(string patron,int day,
            bool partyReady)
        {
            return Active && PatronId==patron &&
                day>=StartDay && day<=DueDay &&
                (Order=="Scout"?FirstTownId.Length>0 &&
                    SecondTownId.Length>0:
                    Order=="Muster" && day>=StartDay+3 &&
                    partyReady);
        }
        public string Report(string patron,int day,bool partyReady)
        {
            if(!CanReport(patron,day,partyReady))
                return "unverified_native_service_report";
            Status="Reported";
            LastReportedDay=day;
            if(CompletedOrders<10000)CompletedOrders++;
            return "native_patron_service_report_completed";
        }
        public string PersonalStanding()
        {
            if(CompletedOrders>=6)return "Veteran retainer";
            if(CompletedOrders>=3)return "Trusted household knight";
            if(CompletedOrders>=1)return "Proven sworn sword";
            return "Newly sworn knight";
        }
        public string Serialize()
        {
            return Version+"\n"+String.Join("|",new[]{
                PatronId,Order,Status,FirstTownId,SecondTownId,
                StartDay.ToString(CultureInfo.InvariantCulture),
                DueDay.ToString(CultureInfo.InvariantCulture),
                LastObservedDay.ToString(CultureInfo.InvariantCulture),
                CompletedOrders.ToString(CultureInfo.InvariantCulture),
                MissedOrders.ToString(CultureInfo.InvariantCulture),
                LastReportedDay.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxKnightServiceOrderState Parse(string packed)
        {
            var empty=new SandboxKnightServiceOrderState();
            if(String.IsNullOrEmpty(packed)||packed.Length>850)
                return empty;
            var lines=packed.Replace("\r","").Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return empty;
            var f=lines[1].Split('|');
            if(f.Length!=11)return empty;
            int start,due,observed,completed,missed,reported;
            if((f[0].Length>0&&!Id(f[0])) ||
                (f[1]!=""&&f[1]!="Scout"&&f[1]!="Muster") ||
                (f[2]!="Available"&&f[2]!="Active"&&
                 f[2]!="Reported"&&f[2]!="Expired") ||
                (f[3].Length>0&&!Id(f[3])) ||
                (f[4].Length>0&&!Id(f[4])) ||
                !Int32.TryParse(f[5],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out start) ||
                !Int32.TryParse(f[6],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out due) ||
                !Int32.TryParse(f[7],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out observed) ||
                !Int32.TryParse(f[8],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out completed) ||
                !Int32.TryParse(f[9],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out missed) ||
                !Int32.TryParse(f[10],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out reported) ||
                start< -1 || due< -1 || observed< -1 ||
                completed<0 || completed>10000 ||
                missed<0 || missed>10000 || reported< -1 ||
                (f[2]=="Active" &&
                 (f[0].Length==0 || start<0 || due!=start+14)))
                return empty;
            return new SandboxKnightServiceOrderState{
                PatronId=f[0],Order=f[1],Status=f[2],
                FirstTownId=f[3],SecondTownId=f[4],
                StartDay=start,DueDay=due,LastObservedDay=observed,
                CompletedOrders=completed,MissedOrders=missed,
                LastReportedDay=reported};
        }
    }
}
