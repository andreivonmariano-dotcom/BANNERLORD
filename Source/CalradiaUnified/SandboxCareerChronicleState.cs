using System;
using System.Globalization;

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
}
