using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Three role-specific career chapters, earned only from native
    /// campaign evidence. This is a CU journal, not a TaleWorlds
    /// QuestBase, native rank, salary, title or kingdom appointment.
    /// </summary>
    public sealed class SandboxCareerChronicleState
    {
        public const string Version="CU_CAREER_CHRONICLE_1";
        public string Role="",FirstTownId="",SecondTownId="";
        public int Chapter,LastEvidenceDay=-1,ConsecutiveDays;
        public bool Started { get { return Role.Length>0; } }

        private static bool ValidRole(string role)
        {
            return role=="Wanderer" || role=="Outlaw" ||
                role=="Mercenary" || role=="Soldier" ||
                role=="Knight" || role=="Lord" || role=="King";
        }
        private static bool ValidId(string id)
        {
            if(id==null || id.Length>80)return false;
            foreach(char c in id)
                if(!Char.IsLetterOrDigit(c) && c!='_' && c!='-')
                    return false;
            return true;
        }
        public bool Begin(string role)
        {
            if(Started || !ValidRole(role))return false;
            Role=role;
            Chapter=0;
            LastEvidenceDay=-1;
            ConsecutiveDays=0;
            return true;
        }
        public static string ChapterObjective(string role,int chapter)
        {
            switch(role)
            {
                case "Wanderer":
                    return chapter==0?"Enter your first real town":
                        chapter==1?"Travel to a different town":
                        chapter==2?"Visit a third distinct town":
                        "Your three roads are recorded";
                case "Outlaw":
                    return chapter==0?"Gather five native party members":
                        chapter==1?"Keep five party members for three days":
                        chapter==2?"Keep ten party members for seven days":
                        "Your band has earned its history";
                case "Mercenary":
                    return chapter==0?"Take a native mercenary contract":
                        chapter==1?"Serve seven days under contract":
                        chapter==2?"Remain hired for 21 further days":
                        "Your paid service is recorded";
                case "Soldier":
                    return chapter==0?"Complete real enlistment":
                        chapter==1?"Serve seven confirmed days":
                        chapter==2?"Serve 30 confirmed days":
                        "Your enlisted service is recorded";
                case "Knight":
                    return chapter==0?"Pledge before your sworn lord":
                        chapter==1?"Complete three days of field escort":
                        chapter==2?"Complete a second field escort":
                        "Your sworn service is recorded";
                case "Lord":
                    return chapter==0?"Hold your first native fief":
                        chapter==1?"Hold native land across seven days":
                        chapter==2?"Hold two native settlements":
                        "Your house has established its estates";
                case "King":
                    return chapter==0?"Be confirmed as native ruler":
                        chapter==1?"Convene a real royal council":
                        chapter==2?"Convene three verified councils":
                        "Your council's work is recorded";
                default:
                    return "No verified starting career";
            }
        }
        private int RequiredDays()
        {
            // The opening deed is earned immediately. Only the
            // later survival/service chapters require a full streak.
            if(Chapter==0)return 1;
            if(Role=="Outlaw")return Chapter==1?3:7;
            if(Role=="Mercenary")return Chapter==1?7:21;
            if(Role=="Lord" && Chapter==1)return 7;
            return 1;
        }
        public string Observe(int day,bool firstProof,bool secondProof,
            bool thirdProof,string observedTownId)
        {
            if(!Started || day<0 || day<=LastEvidenceDay)
                return "not_new_native_day";
            // Missed native days break a continuous contract/party
            // streak. A load or an observer outage grants no backpay.
            if(LastEvidenceDay>=0 &&
                (long)day-LastEvidenceDay!=1)
                ConsecutiveDays=0;
            LastEvidenceDay=day;
            if(Chapter>=3)return "career_chapters_complete";
            bool proof=Chapter==0?firstProof:
                Chapter==1?secondProof:thirdProof;
            if(Role=="Wanderer")
            {
                if(!ValidId(observedTownId) ||
                    String.IsNullOrEmpty(observedTownId))
                    proof=false;
                else if(Chapter==1)
                    proof=proof && observedTownId!=FirstTownId;
                else if(Chapter==2)
                    proof=proof && observedTownId!=FirstTownId &&
                        observedTownId!=SecondTownId;
            }
            if(!proof)
            {
                ConsecutiveDays=0;
                return "native_objective_not_verified";
            }
            ConsecutiveDays++;
            if(ConsecutiveDays<RequiredDays())
                return "native_objective_in_progress";
            if(Role=="Wanderer")
            {
                if(Chapter==0)FirstTownId=observedTownId;
                if(Chapter==1)SecondTownId=observedTownId;
            }
            Chapter++;
            ConsecutiveDays=0;
            return "career_chapter_"+Chapter+"_verified";
        }
        public string Serialize()
        {
            return Version+"\n"+String.Join("|",new[]{
                Role,Chapter.ToString(CultureInfo.InvariantCulture),
                LastEvidenceDay.ToString(CultureInfo.InvariantCulture),
                ConsecutiveDays.ToString(CultureInfo.InvariantCulture),
                FirstTownId,SecondTownId});
        }
        public static SandboxCareerChronicleState Parse(string packed)
        {
            var empty=new SandboxCareerChronicleState();
            if(String.IsNullOrEmpty(packed) || packed.Length>512)
                return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length!=2 || rows[0]!=Version)return empty;
            var f=rows[1].Split('|');
            int chapter,day,streak;
            if(f.Length!=6 || !ValidRole(f[0]) ||
                !Int32.TryParse(f[1],NumberStyles.None,
                    CultureInfo.InvariantCulture,out chapter) ||
                !Int32.TryParse(f[2],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out day) ||
                !Int32.TryParse(f[3],NumberStyles.None,
                    CultureInfo.InvariantCulture,out streak) ||
                chapter<0 || chapter>3 || day< -1 ||
                streak>21 || !ValidId(f[4]) || !ValidId(f[5]))
                return empty;
            var result=new SandboxCareerChronicleState {
                Role=f[0],Chapter=chapter,
                LastEvidenceDay=day,ConsecutiveDays=streak,
                FirstTownId=f[4],SecondTownId=f[5]};
            // Only accept combinations reachable through Begin/Observe.
            if((day<0 && (chapter!=0 || streak!=0)) ||
                (chapter==3 && streak!=0) ||
                (chapter<3 && streak>=result.RequiredDays()))return empty;
            if(result.Role=="Wanderer")
            {
                if((chapter>=1)!=(f[4].Length>0) ||
                    (chapter>=2)!=(f[5].Length>0) ||
                    (f[5].Length>0 && f[4]==f[5]))return empty;
            }
            else if(f[4].Length!=0 || f[5].Length!=0)return empty;
            return result;
        }
    }

    /// <summary>Repeatable, voluntary reviews of current evidence.
    /// These receipts never award gold, rank, relation or completed duties.</summary>
    public sealed class SandboxCareerReviewState
    {
        public const string Version="CU_CAREER_REVIEWS_1";
        public const int IntervalDays=7, MaxRecords=64;
        public sealed class Receipt
        {
            public string HeroId="", Role="";
            public int LastDay, Count;
        }
        private readonly List<Receipt> records=new List<Receipt>();
        private static bool Id(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c) || c=='_' || c=='-');
        }
        private static bool Role(string role)
        {
            return Array.IndexOf(SandboxStartingLifePolicy.SevenPaths,role)>=0;
        }
        public Receipt For(string hero,string role)
        {
            return records.FirstOrDefault(r=>r.HeroId==hero && r.Role==role);
        }
        public static string Activity(string role)
        {
            switch(role)
            {
                case "Wanderer": return "Town livelihood review";
                case "Outlaw": return "Independent band readiness review";
                case "Mercenary": return "Active company contract review";
                case "Soldier": return "Verified enlisted service review";
                case "Knight": return "Current oath and muster review";
                case "Lord": return "Native estate and household review";
                case "King": return "Native crown and court review";
                default: return "";
            }
        }
        public bool Record(string hero,string role,int day,bool verified)
        {
            if(!verified || !Id(hero) || !Role(role) || day<0)return false;
            var receipt=For(hero,role);
            if(receipt!=null)
            {
                if((long)day-receipt.LastDay<IntervalDays)return false;
                receipt.LastDay=day;
                receipt.Count++;
                return true;
            }
            if(records.Count>=MaxRecords)return false;
            records.Add(new Receipt{HeroId=hero,Role=role,LastDay=day,Count=1});
            return true;
        }
        public string Serialize()
        {
            var rows=new List<string>{Version};
            foreach(var r in records.OrderBy(x=>x.HeroId,StringComparer.Ordinal)
                .ThenBy(x=>x.Role,StringComparer.Ordinal))
                rows.Add(String.Join("|",new[]{r.HeroId,r.Role,
                    r.LastDay.ToString(CultureInfo.InvariantCulture),
                    r.Count.ToString(CultureInfo.InvariantCulture)}));
            return String.Join("\n",rows);
        }
        public static SandboxCareerReviewState Parse(string packed)
        {
            var empty=new SandboxCareerReviewState();
            if(String.IsNullOrEmpty(packed) || packed.Length>16000)return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length>MaxRecords+1 || rows[0]!=Version)return empty;
            var state=new SandboxCareerReviewState();
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');
                int day,count;
                if(f.Length!=4 || !Id(f[0]) || !Role(f[1]) ||
                    !Int32.TryParse(f[2],NumberStyles.None,CultureInfo.InvariantCulture,out day) ||
                    !Int32.TryParse(f[3],NumberStyles.None,CultureInfo.InvariantCulture,out count) ||
                    day<0 || count<1 || (long)(count-1)*IntervalDays>day ||
                    state.For(f[0],f[1])!=null)return empty;
                state.records.Add(new Receipt{HeroId=f[0],Role=f[1],
                    LastDay=day,Count=count});
            }
            return state;
        }
    }

    // Observed/selected career episodes. No inferred start before the
    // first receipt, no automatic assignment of native status or rewards.
    public sealed class SandboxLifetimeCareerState
    {
        public const string Version="CU_LIFETIME_CAREER_1";
        public const int MaxEpisodes=64;
        public sealed class Episode
        {
            public string HeroId="",Role="";
            public int FirstDay,LastDay;
        }
        private readonly List<Episode> episodes=new List<Episode>();
        public Episode[] For(string hero)
        {
            return episodes.Where(e=>e.HeroId==hero).ToArray();
        }
        private static bool Id(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c) || c=='_' || c=='-');
        }
        private static bool Role(string role)
        {
            return Array.IndexOf(SandboxStartingLifePolicy.SevenPaths,role)>=0;
        }
        public bool Record(string hero,string role,int day,bool verified,bool chosen)
        {
            if(!verified || !chosen || !Id(hero) || !Role(role) || day<0)return false;
            var last=For(hero).LastOrDefault();
            if(last!=null)
            {
                if(day<=last.LastDay)return false;
                if(last.Role==role){last.LastDay=day;return true;}
            }
            if(episodes.Count>=MaxEpisodes)return false;
            episodes.Add(new Episode{HeroId=hero,Role=role,FirstDay=day,LastDay=day});
            return true;
        }
        public string Serialize()
        {
            return String.Join("\n",new[]{Version}.Concat(episodes.Select(e=>
                String.Join("|",new[]{e.HeroId,e.Role,
                    e.FirstDay.ToString(CultureInfo.InvariantCulture),
                    e.LastDay.ToString(CultureInfo.InvariantCulture)}))));
        }
        public static SandboxLifetimeCareerState Parse(string packed)
        {
            var empty=new SandboxLifetimeCareerState();
            if(String.IsNullOrEmpty(packed) || packed.Length>16000)return empty;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows[0]!=Version || rows.Length>MaxEpisodes+1)return empty;
            var state=new SandboxLifetimeCareerState();
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');
                int first,last;
                if(f.Length!=4 || !Id(f[0]) || !Role(f[1]) ||
                    !Int32.TryParse(f[2],NumberStyles.None,CultureInfo.InvariantCulture,out first) ||
                    !Int32.TryParse(f[3],NumberStyles.None,CultureInfo.InvariantCulture,out last) ||
                    first<0 || last<first)return empty;
                var previous=state.For(f[0]).LastOrDefault();
                if(previous!=null && (first<=previous.LastDay || previous.Role==f[1]))
                    return empty;
                state.episodes.Add(new Episode{HeroId=f[0],Role=f[1],FirstDay=first,LastDay=last});
            }
            return state;
        }
    }
    // One voluntary charge at a time. Existing campaign systems supply
    // evidence and the existing recurring review ledger owns completion.
    public sealed class SandboxCareerChargeState
    {
        public const string Version="CU_CAREER_CHARGE_1";
        public string HeroId {get;private set;}="";
        public string Role {get;private set;}="";
        public string Kind {get;private set;}="";
        public string TargetId {get;private set;}="";
        public int StartedDay {get;private set;}=-1;
        public int Baseline {get;private set;}
        public int ClosedDay {get;private set;}=-1;
        public bool Active {get{return HeroId.Length>0 && ClosedDay<0;}}
        private static bool Id(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public static string DefaultKind(string role)
        {
            switch(role)
            {
                case "Wanderer":return "VisitTown";
                case "Outlaw":return "BuildBand";
                case "Mercenary":return "CompanyBattle";
                case "Soldier":return "ServeWeek";
                case "Knight":return "Muster";
                case "Lord":return "VisitEstate";
                case "King":return "Council";
                default:return "";
            }
        }
        public static string Label(string kind)
        {
            switch(kind)
            {
                case "VisitTown":return "Seek livelihood in another town";
                case "BuildBand":return "Strengthen the band by three members";
                case "CompanyBattle":return "Win a witnessed campaign battle";
                case "ServeWeek":return "Serve seven further days under the same lord";
                case "Muster":return "Attend a fresh verified muster";
                case "VisitEstate":return "Visit a named holding of our house";
                case "StrengthenGarrison":return "Add five soldiers to this holding's native garrison";
                case "Council":return "Convene the actual royal council on a later day";
                default:return "";
            }
        }
        private static bool Valid(string hero,string role,string kind,string target,int day,int baseline)
        {
            return Id(hero) && DefaultKind(role).Length>0 &&
                (kind==DefaultKind(role) || (role=="Lord" && kind=="StrengthenGarrison")) &&
                Id(target) && day>=0 && day<=2000000 && baseline>=-1 && baseline<=2000000;
        }
        public bool Start(string hero,string role,string kind,string target,int day,int baseline,bool verified)
        {
            if(!verified || !Valid(hero,role,kind,target,day,baseline) ||
                (HeroId==hero && (Active || day<=ClosedDay)))return false;
            HeroId=hero;Role=role;Kind=kind;TargetId=target;StartedDay=day;
            Baseline=baseline;ClosedDay=-1;return true;
        }
        public bool CanComplete(string hero,string role,int day,string observedTarget,int observedValue,bool verified)
        {
            if(!Active || !verified || hero!=HeroId || role!=Role ||
                day<=StartedDay || day>2000000)return false;
            if(Kind=="VisitTown")return Id(observedTarget) && observedTarget!=TargetId;
            if(observedTarget!=TargetId)return false;
            switch(Kind)
            {
                case "VisitEstate":return observedValue==1;
                case "BuildBand":return (long)observedValue>=Baseline+3L;
                case "StrengthenGarrison":return (long)observedValue>=Baseline+5L;
                case "ServeWeek":return (long)observedValue>=Baseline+7L;
                case "CompanyBattle":
                case "Muster":
                case "Council":return observedValue>StartedDay && observedValue>Baseline && observedValue<=day;
                default:return false;
            }
        }
        public bool Close(string hero,int day)
        {
            if(!Active || hero!=HeroId || day<StartedDay || day>2000000)return false;
            ClosedDay=day;return true;
        }
        public string Serialize()
        {
            return HeroId.Length==0?Version:Version+"\n"+
                String.Join("|",new[]{HeroId,Role,Kind,TargetId,
                    StartedDay.ToString(CultureInfo.InvariantCulture),
                    Baseline.ToString(CultureInfo.InvariantCulture),
                    ClosedDay.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxCareerChargeState Parse(string packed)
        {
            var state=new SandboxCareerChargeState();
            if(String.IsNullOrEmpty(packed)||packed.Length>512)return state;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length!=2||rows[0]!=Version)return state;
            var f=rows[1].Split('|');
            int day,baseline,closed;
            if(f.Length!=7||!Int32.TryParse(f[4],out day)||
                !Int32.TryParse(f[5],out baseline)||!Int32.TryParse(f[6],out closed)||
                closed< -1 || (closed>=0 && (closed<day || closed>2000000)) ||
                !state.Start(f[0],f[1],f[2],f[3],day,baseline,true))return new SandboxCareerChargeState();
            state.ClosedDay=closed;return state;
        }
    }

}
