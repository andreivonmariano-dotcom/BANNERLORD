using System;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A genuine native-map proximity duty, not a spawned party or
    /// fictional battle. The behavior supplies independently verified
    /// moving parties and day; the saved state records earned service.
    /// </summary>
    public sealed class SandboxKnightEscortState
    {
        public const string Version="CU_KNIGHT_ESCORT_1";
        public const int RequiredDays=3;
        public const int RenewalCooldownDays=30;
        public string PatronId="",Status="None";
        public int StartDay=-1,LastObservedDay=-1;
        public int ConsecutiveDays,CompletedEscorts,LastFinishedDay=-1;

        public bool IsActive {get{return Status=="Active";}}
        public bool CanBegin(string patron,int day)
        {
            return !String.IsNullOrWhiteSpace(patron) &&
                patron.Length<=80 && day>=0 &&
                day<=Int32.MaxValue-120 &&
                !IsActive &&
                (LastFinishedDay<0 ||
                 day>=LastFinishedDay+RenewalCooldownDays);
        }
        public string Begin(string patron,int day)
        {
            if(!CanBegin(patron,day))return "escort_not_available";
            PatronId=patron;Status="Active";
            StartDay=day;LastObservedDay=day;
            ConsecutiveDays=0;
            return "native_patron_field_escort_accepted";
        }
        public string Observe(int day,bool nearNativePatronParty)
        {
            if(!IsActive)return "no_active_field_escort";
            if(day<=LastObservedDay || day<=StartDay)
                return "no_new_campaign_day";
            if(day>Int32.MaxValue-120 || day<0)
                return "invalid_campaign_day";
            if(!nearNativePatronParty)
            {
                LastObservedDay=day;
                ConsecutiveDays=0;
                return "field_escort_separated";
            }
            // Only consecutive DAYS alongside the real native lord
            // count. Skipping or reloading cannot accumulate credit.
            if(day!=LastObservedDay+1)ConsecutiveDays=0;
            LastObservedDay=day;
            ConsecutiveDays++;
            if(ConsecutiveDays<RequiredDays)
                return "native_field_escort_day_verified";
            Status="Completed";
            LastFinishedDay=day;
            CompletedEscorts++;
            return "verified_native_party_escort_completed";
        }
        public void Interrupt()
        {
            if(IsActive)
            {
                Status="Interrupted";
                ConsecutiveDays=0;
            }
        }
        public string Serialize()
        {
            return Version+"\n"+String.Join("|",new[]{
                PatronId,Status,
                StartDay.ToString(CultureInfo.InvariantCulture),
                LastObservedDay.ToString(CultureInfo.InvariantCulture),
                ConsecutiveDays.ToString(CultureInfo.InvariantCulture),
                CompletedEscorts.ToString(CultureInfo.InvariantCulture),
                LastFinishedDay.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxKnightEscortState Parse(string text)
        {
            var empty=new SandboxKnightEscortState();
            if(String.IsNullOrEmpty(text) || text==Version)return empty;
            if(text.Length>512)return empty;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return empty;
            var x=lines[1].Split('|');
            if(x.Length!=7 || x[0].Length>80 ||
                !new[]{"None","Active","Completed","Interrupted"}
                    .Contains(x[1]))return empty;
            int start,last,progress,completed,finish;
            if(!Int32.TryParse(x[2],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out start) ||
               !Int32.TryParse(x[3],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out last) ||
               !Int32.TryParse(x[4],NumberStyles.None,
                    CultureInfo.InvariantCulture,out progress) ||
               !Int32.TryParse(x[5],NumberStyles.None,
                    CultureInfo.InvariantCulture,out completed) ||
               !Int32.TryParse(x[6],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out finish) ||
               start< -1 || last< -1 || finish< -1 ||
               completed>10000 || progress>RequiredDays ||
               (x[1]=="Active" && (x[0].Length==0 ||
                   start<0 || last<start)) ||
               (finish>=0 && finish<start))return empty;
            return new SandboxKnightEscortState{
                PatronId=x[0],Status=x[1],StartDay=start,
                LastObservedDay=last,ConsecutiveDays=progress,
                CompletedEscorts=completed,LastFinishedDay=finish};
        }
    }
}
