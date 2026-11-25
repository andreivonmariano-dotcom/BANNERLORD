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
        public const string ExtendedVersion="CU_HOUSEHOLD_DUTIES_2";
        private readonly string[] officerIds=new string[4];
        private readonly int[] lastReportDays={-1,-1,-1,-1};
        private readonly int[] reportCounts=new int[4];
        private readonly Dictionary<string,int> familyAudience=
            new Dictionary<string,int>(StringComparer.Ordinal);
        private readonly Dictionary<string,int> familyGift=
            new Dictionary<string,int>(StringComparer.Ordinal);

        private static int Slot(string office)
        {
            if(office=="Steward")return 0;
            if(office=="Captain")return 1;
            if(office=="Envoy")return 2;
            if(office=="ManAtArms")return 3;
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
            if(!String.IsNullOrEmpty(officerIds[slot]))
                return "office_occupied";
            officerIds[slot]=heroId;
            lastReportDays[slot]=-1;
            reportCounts[slot]=0;
            return "personal_duty_recorded";
        }
        public bool Release(string office,string expectedHeroId)
        {
            int slot=Slot(office);
            if(slot<0 || !Id(expectedHeroId) || officerIds[slot]!=expectedHeroId)
                return false;
            officerIds[slot]="";
            lastReportDays[slot]=-1;
            reportCounts[slot]=0;
            return true;
        }
        public bool CanReport(string office,string heroId,int day)
        {
            int slot=Slot(office);
            return slot>=0 && Id(heroId) && day>=0 &&
                officerIds[slot]==heroId && lastReportDays[slot]<day;
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
                (receipts.ContainsKey(heroId)?
                 (long)day-receipts[heroId]>=FamilyCooldownDays:
                 receipts.Count<16);
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
        public int AudienceDaysRemaining(string heroId,int day)
        {
            int previous;
            return familyAudience.TryGetValue(heroId??"",out previous)?
                (int)Math.Min(Int32.MaxValue,Math.Max(0L,(long)previous+FamilyCooldownDays-day)):0;
        }
        public int GiftDaysRemaining(string heroId,int day)
        {
            int previous;
            return familyGift.TryGetValue(heroId??"",out previous)?
                (int)Math.Min(Int32.MaxValue,Math.Max(0L,(long)previous+FamilyCooldownDays-day)):0;
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
            bool extended=!String.IsNullOrEmpty(officerIds[3]);
            for(int i=0;i<(extended?4:3);i++)
            {
                fields.Add(officerIds[i]??"");
                fields.Add(lastReportDays[i].ToString(
                    CultureInfo.InvariantCulture));
                fields.Add(reportCounts[i].ToString(
                    CultureInfo.InvariantCulture));
            }
            fields.Add(Pack(familyAudience));
            fields.Add(Pack(familyGift));
            return (extended?ExtendedVersion:Version)+"\n"+String.Join("|",fields);
        }
        public static SandboxHouseholdDutyState Parse(string packed)
        {
            var empty=new SandboxHouseholdDutyState();
            if(String.IsNullOrEmpty(packed) || packed.Length>4096)
                return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length!=2 || (rows[0]!=Version && rows[0]!=ExtendedVersion))return empty;
            int slots=rows[0]==ExtendedVersion?4:3;
            var fields=rows[1].Split('|');
            if(fields.Length!=slots*3+2)return empty;
            var result=new SandboxHouseholdDutyState();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<slots;i++)
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
            if(!Unpack(fields[slots*3],result.familyAudience) ||
               !Unpack(fields[slots*3+1],result.familyGift))
                return empty;
            return result;
        }
    }
    // One active personal military retainer, bound to the existing duty
    // slot and commissioning player. No native rank or skill mutation.
    public sealed class SandboxRetainerServiceState
    {
        public const string Version="CU_RETAINER_SERVICE_1";
        public string Owner="",HeroId="",Rank="";
        public int Since=-1,Reports,LastReport=-1,LastPay=-1;
        private static bool Id(string s){return !String.IsNullOrEmpty(s) && s.Length<=80 &&
            s.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');}
        private static bool KnownRank(string s){return s=="ManAtArms" || s=="Sergeant" || s=="Squire" || s=="Knight";}
        public bool Matches(string owner,string hero){return Id(owner) && Id(hero) && Owner==owner && HeroId==hero && KnownRank(Rank);}
        public bool Begin(string owner,string hero,int day)
        {
            if(!Id(owner)||!Id(hero)||owner==hero||day<0||Matches(owner,hero))return false;
            Owner=owner;HeroId=hero;Rank="ManAtArms";Since=day;Reports=0;LastReport=-1;LastPay=-1;return true;
        }
        public void Clear(){Owner="";HeroId="";Rank="";Since=-1;Reports=0;LastReport=-1;LastPay=-1;}
        public bool Record(string owner,string hero,int day)
        {
            if(!Matches(owner,hero)||day<Since||day<=LastReport)return false;
            LastReport=day;if(Reports<10000)Reports++;return true;
        }
        public static int Wage(string rank)
        {
            switch(rank){case "ManAtArms":return 70;case "Sergeant":return 120;case "Squire":return 90;case "Knight":return 200;default:return 0;}
        }
        public bool PayDue(string owner,string hero,int day)
        {return Matches(owner,hero) && day>=Since && (long)day-(LastPay<0?Since:LastPay)>=7;}
        public bool RecordPayAttempt(string owner,string hero,int day)
        {if(!PayDue(owner,hero,day))return false;LastPay=day;return true;}
        public bool CanPromote(string owner,string hero,string next,int day,int martial,int riding,int leadership,bool noble)
        {
            if(!Matches(owner,hero)||day<Since)return false;
            long elapsed=(long)day-Since;
            if(Rank=="ManAtArms" && next=="Sergeant")return elapsed>=30 && Reports>=5 && martial>=80 && leadership>=60;
            if(Rank=="ManAtArms" && next=="Squire")return noble && elapsed>=14 && Reports>=3 && martial>=60 && riding>=60;
            if(Rank=="Squire" && next=="Knight")return noble && elapsed>=60 && Reports>=10 && martial>=120 && riding>=100;
            return false;
        }
        public bool Promote(string owner,string hero,string next,int day,int martial,int riding,int leadership,bool noble)
        {
            if(!CanPromote(owner,hero,next,day,martial,riding,leadership,noble))return false;
            Rank=next;Since=day;Reports=0;return true;
        }
        public bool MarshalReady(string owner,string hero,int day,int leadership,int tactics)
        {return Matches(owner,hero) && Rank=="Knight" && (long)day-Since>=90 && Reports>=15 && leadership>=150 && tactics>=120;}
        public string Serialize(){return KnownRank(Rank)?Version+"\n"+String.Join("|",new[]{Owner,HeroId,Rank,Since.ToString(),Reports.ToString(),LastReport.ToString(),LastPay.ToString()}):Version;}
        public static SandboxRetainerServiceState Parse(string raw)
        {
            var result=new SandboxRetainerServiceState();
            if(String.IsNullOrEmpty(raw)||raw.Length>512)return result;
            var rows=raw.Replace("\r","").Split('\n');if(rows.Length!=2||rows[0]!=Version)return result;
            var p=rows[1].Split('|');int since,reports,last,pay;
            if(p.Length!=7||!Id(p[0])||!Id(p[1])||p[0]==p[1]||!KnownRank(p[2])||
                !Int32.TryParse(p[3],out since)||since<0||!Int32.TryParse(p[4],out reports)||reports<0||reports>10000||
                !Int32.TryParse(p[5],out last)||last< -1||!Int32.TryParse(p[6],out pay)||pay< -1||
                (reports>0 && last<since))return result;
            result.Owner=p[0];result.HeroId=p[1];result.Rank=p[2];result.Since=since;
            result.Reports=reports;result.LastReport=last;result.LastPay=pay;return result;
        }
    }

}
