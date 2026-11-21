using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Save-backed personal household duties, not native companion
    /// party roles, clan membership, governors or political offices.
    /// One report per campaign day and one family audience/gift per
    /// person per thirty days. No invented people or resources.
    /// </summary>
    public sealed class SandboxHouseholdDutyState
    {
        public const string Version="CU_HOUSEHOLD_DUTIES_1";
        public const int FamilyCooldownDays=30;
        private readonly string[] officerIds=new string[3];
        private readonly int[] lastReportDays={-1,-1,-1};
        private readonly int[] reportCounts=new int[3];
        private readonly Dictionary<string,int> familyAudience=
            new Dictionary<string,int>(StringComparer.Ordinal);
        private readonly Dictionary<string,int> familyGift=
            new Dictionary<string,int>(StringComparer.Ordinal);

        private static int Slot(string office)
        {
            if(office=="Steward")return 0;
            if(office=="Captain")return 1;
            if(office=="Envoy")return 2;
            return -1;
        }
        private static bool Id(string text)
        {
            return !String.IsNullOrWhiteSpace(text) &&
                text.Length<=80 &&
                text.All(c=>Char.IsLetterOrDigit(c) ||
                    c=='_' || c=='-');
        }
        public string AssignedId(string office)
        {
            int slot=Slot(office);
            return slot<0?"":officerIds[slot]??"";
        }
        public int ReportCount(string office)
        {
            int slot=Slot(office);
            return slot<0?0:reportCounts[slot];
        }
        public string Assign(string office,string heroId)
        {
            int slot=Slot(office);
            if(slot<0 || !Id(heroId))
                return "invalid_office_or_native_hero";
            if(officerIds[slot]==heroId)
                return "already_serving_this_office";
            if(officerIds.Where((id,index)=>index!=slot)
                    .Any(id=>id==heroId))
                return "companion_already_has_duty";
            officerIds[slot]=heroId;
            lastReportDays[slot]=-1;
            reportCounts[slot]=0;
            return "personal_duty_recorded";
        }
        public string Report(string office,string heroId,int day)
        {
            int slot=Slot(office);
            if(slot<0 || !Id(heroId) || day<0 ||
                officerIds[slot]!=heroId)
                return "duty_identity_not_verified";
            if(lastReportDays[slot]>=day)
                return "already_reported_today";
            lastReportDays[slot]=day;
            if(reportCounts[slot]<10000)reportCounts[slot]++;
            return "native_duty_report_recorded";
        }
        private static bool CanRecord(Dictionary<string,int> receipts,
            string heroId,int day)
        {
            return Id(heroId) && day>=0 &&
                (!receipts.ContainsKey(heroId) ||
                 (long)day-receipts[heroId]>=FamilyCooldownDays);
        }
        private static bool Record(Dictionary<string,int> receipts,
            string heroId,int day)
        {
            if(!CanRecord(receipts,heroId,day))return false;
            // Cap old records rather than accepting unbounded save data.
            if(receipts.Count>=16 && !receipts.ContainsKey(heroId))
                return false;
            receipts[heroId]=day;
            return true;
        }
        public bool CanAudience(string heroId,int day)
        {
            return CanRecord(familyAudience,heroId,day);
        }
        public bool RecordAudience(string heroId,int day)
        {
            return Record(familyAudience,heroId,day);
        }
        public bool CanGift(string heroId,int day)
        {
            return CanRecord(familyGift,heroId,day);
        }
        public bool RecordGiftAttempt(string heroId,int day)
        {
            // Commit prior to the native transfer to prevent a second
            // withdrawal after an exception or a save/load retry.
            return Record(familyGift,heroId,day);
        }
        private static string Pack(Dictionary<string,int> receipts)
        {
            return String.Join(",",receipts.OrderBy(p=>p.Key,
                StringComparer.Ordinal).Select(p=>
                    p.Key+":"+p.Value.ToString(
                        CultureInfo.InvariantCulture)));
        }
        private static bool Unpack(string source,
            Dictionary<string,int> target)
        {
            if(String.IsNullOrEmpty(source))return true;
            foreach(var part in source.Split(','))
            {
                var pair=part.Split(':');
                int day;
                if(pair.Length!=2 || !Id(pair[0]) ||
                    !Int32.TryParse(pair[1],NumberStyles.None,
                        CultureInfo.InvariantCulture,out day) ||
                    day<0 || target.Count>=16 ||
                    target.ContainsKey(pair[0]))
                    return false;
                target[pair[0]]=day;
            }
            return true;
        }
        public string Serialize()
        {
            var fields=new List<string>();
            for(int i=0;i<3;i++)
            {
                fields.Add(officerIds[i]??"");
                fields.Add(lastReportDays[i].ToString(
                    CultureInfo.InvariantCulture));
                fields.Add(reportCounts[i].ToString(
                    CultureInfo.InvariantCulture));
            }
            fields.Add(Pack(familyAudience));
            fields.Add(Pack(familyGift));
            return Version+"\n"+String.Join("|",fields);
        }
        public static SandboxHouseholdDutyState Parse(string packed)
        {
            var empty=new SandboxHouseholdDutyState();
            if(String.IsNullOrEmpty(packed) || packed.Length>3000)
                return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length!=2 || rows[0]!=Version)return empty;
            var fields=rows[1].Split('|');
            if(fields.Length!=11)return empty;
            var result=new SandboxHouseholdDutyState();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<3;i++)
            {
                string id=fields[i*3];
                int day,count;
                if((id.Length>0 && (!Id(id) || !seen.Add(id))) ||
                    !Int32.TryParse(fields[i*3+1],
                        NumberStyles.Integer,CultureInfo.InvariantCulture,
                        out day) ||
                    !Int32.TryParse(fields[i*3+2],
                        NumberStyles.None,CultureInfo.InvariantCulture,
                        out count) ||
                    day< -1 || count>10000 ||
                    (id.Length==0 && (day!=-1 || count!=0)))
                    return empty;
                result.officerIds[i]=id;
                result.lastReportDays[i]=day;
                result.reportCounts[i]=count;
            }
            if(!Unpack(fields[9],result.familyAudience) ||
               !Unpack(fields[10],result.familyGift))
                return empty;
            return result;
        }
    }
}
