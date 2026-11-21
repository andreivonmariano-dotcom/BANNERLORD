using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// A saved personal oath, NOT native clan membership or vassalage.
    /// All rewards require a separately verified native action.
    /// Each pledge, overdue notice and consent transition is one-shot.
    /// </summary>
    public sealed class SandboxKnightOathState
    {
        public const string Version="CU_KNIGHT_OATH_1";
        public const int MusterDays=30;
        public const int MusterWindowDays=7;
        public string PlayerId="",PatronId="",PatronClanId="",KingdomId="";
        public string Status="Unbound";
        public int PledgedDay=-1,LastMusterDay=-1,NextMusterDay=-1;
        public int CompletedMusters,MissedMusters,Successions;
        public bool IsConfigured
        {
            get { return PlayerId.Length>0 && PatronId.Length>0 &&
                PatronClanId.Length>0 && KingdomId.Length>0 &&
                Status!="Unbound"; }
        }
        public bool HasCurrentOath
        {
            get { return IsConfigured &&
                (Status=="Active" || Status=="MusterDue" ||
                 Status=="Overdue" || Status=="PatronCaptive"); }
        }
        private static bool Id(string value)
        {
            if(String.IsNullOrWhiteSpace(value) || value.Length>80)
                return false;
            foreach(char c in value)
                if(!(Char.IsLetterOrDigit(c) || c=='_' || c=='-'))
                    return false;
            return true;
        }
        public string Begin(string player,string patron,string clan,
            string kingdom,int day)
        {
            if(IsConfigured)return "already_configured";
            if(!Id(player) || !Id(patron) || !Id(clan) ||
                !Id(kingdom) || player==patron || day<0 ||
                day>Int32.MaxValue-MusterDays)
                return "invalid_native_oath_identity";
            PlayerId=player;PatronId=patron;
            PatronClanId=clan;KingdomId=kingdom;
            PledgedDay=day;LastMusterDay=-1;
            NextMusterDay=day+MusterDays;
            CompletedMusters=0;MissedMusters=0;Successions=0;
            Status="Active";
            return "oath_recorded";
        }
        public string Observe(int day,bool patronResolved,
            bool patronAlive,bool sameClan,bool sameKingdom,
            bool patronCaptive=false)
        {
            if(!IsConfigured)return "not_configured";
            if(!HasCurrentOath)return Status.ToLowerInvariant();
            if(day<PledgedDay || day<0)return "invalid_day";
            if(!patronResolved)return "patron_not_verified";
            if(!patronAlive)
            {
                Status="SuccessionPending";
                return "successor_consent_required";
            }
            if(!sameClan || !sameKingdom)
            {
                Status="Interrupted";
                return "native_patron_allegiance_changed";
            }
            if(patronCaptive)
            {
                if(Status=="PatronCaptive")
                    return "patron_still_captive";
                Status="PatronCaptive";
                return "patron_captivity_pause";
            }
            if(Status=="PatronCaptive")
            {
                Status="Active";
                NextMusterDay=day+MusterDays;
                return "patron_returned_muster_reset";
            }
            if(day>NextMusterDay && Status!="Overdue")
            {
                Status="Overdue";
                MissedMusters++;
                return "new_muster_breach";
            }
            if(day>=NextMusterDay-MusterWindowDays &&
                Status=="Active")
            {
                Status="MusterDue";
                return "muster_window_open";
            }
            return Status=="Overdue"?"muster_still_overdue":"oath_active";
        }
        public bool CanMuster(int day,bool inPerson)
        {
            return HasCurrentOath && Status!="PatronCaptive" &&
                inPerson && day>=PledgedDay &&
                day>=NextMusterDay-MusterWindowDays &&
                (LastMusterDay<0 || day>LastMusterDay) &&
                day<=Int32.MaxValue-MusterDays;
        }
        public string RecordMuster(int day,bool inPerson)
        {
            if(!CanMuster(day,inPerson))return "muster_not_eligible";
            LastMusterDay=day;
            NextMusterDay=day+MusterDays;
            CompletedMusters++;
            Status="Active";
            return "native_meeting_muster_recorded";
        }
        public string Release(bool metPatron)
        {
            if(!HasCurrentOath || !metPatron)
                return "personal_release_not_verified";
            Status="Released";
            return "consensual_release";
        }
        public string Break()
        {
            if(!HasCurrentOath)return "no_oath_to_break";
            Status="Broken";
            return "oath_broken";
        }
        public string AcceptSuccessor(string newPatron,string sameClan,
            string sameKingdom,int day,bool confirmedMeeting)
        {
            if(Status!="SuccessionPending" || !confirmedMeeting ||
                !IsConfigured || !Id(newPatron) ||
                newPatron==PatronId || sameClan!=PatronClanId ||
                sameKingdom!=KingdomId || day<PledgedDay ||
                day>Int32.MaxValue-MusterDays)
                return "unverified_successor_or_no_consent";
            PatronId=newPatron;
            PledgedDay=day;
            LastMusterDay=-1;
            NextMusterDay=day+MusterDays;
            Successions++;
            Status="Active";
            return "oath_renewed_to_native_successor";
        }
        public string Serialize()
        {
            if(!IsConfigured)return Version;
            return Version+"\n"+String.Join("|",new[]{
                PlayerId,PatronId,PatronClanId,KingdomId,Status,
                PledgedDay.ToString(CultureInfo.InvariantCulture),
                LastMusterDay.ToString(CultureInfo.InvariantCulture),
                NextMusterDay.ToString(CultureInfo.InvariantCulture),
                CompletedMusters.ToString(CultureInfo.InvariantCulture),
                MissedMusters.ToString(CultureInfo.InvariantCulture),
                Successions.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxKnightOathState Parse(string text)
        {
            var empty=new SandboxKnightOathState();
            if(String.IsNullOrEmpty(text) || text==Version)return empty;
            if(text.Length>1024)return empty;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return empty;
            var f=lines[1].Split('|');
            if(f.Length!=11 || !Id(f[0]) || !Id(f[1]) ||
               !Id(f[2]) || !Id(f[3]) || f[0]==f[1])
                return empty;
            if(f[4]!="Active" && f[4]!="MusterDue" &&
               f[4]!="Overdue" && f[4]!="SuccessionPending" &&
               f[4]!="PatronCaptive" &&
               f[4]!="Interrupted" && f[4]!="Released" &&
               f[4]!="Broken")return empty;
            int pledged,last,next,completed,missed,successions;
            if(!Int32.TryParse(f[5],NumberStyles.None,
                    CultureInfo.InvariantCulture,out pledged) ||
               !Int32.TryParse(f[6],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out last) ||
               !Int32.TryParse(f[7],NumberStyles.None,
                    CultureInfo.InvariantCulture,out next) ||
               !Int32.TryParse(f[8],NumberStyles.None,
                    CultureInfo.InvariantCulture,out completed) ||
               !Int32.TryParse(f[9],NumberStyles.None,
                    CultureInfo.InvariantCulture,out missed) ||
               !Int32.TryParse(f[10],NumberStyles.None,
                    CultureInfo.InvariantCulture,out successions) ||
               pledged<0 || pledged>Int32.MaxValue-MusterDays ||
               last < -1 || next < pledged+MusterDays ||
               last>next || completed>100000 || missed>100000 ||
               successions>100000)
                return empty;
            var state=new SandboxKnightOathState();
            state.PlayerId=f[0];state.PatronId=f[1];
            state.PatronClanId=f[2];state.KingdomId=f[3];
            state.Status=f[4];state.PledgedDay=pledged;
            state.LastMusterDay=last;state.NextMusterDay=next;
            state.CompletedMusters=completed;
            state.MissedMusters=missed;
            state.Successions=successions;
            return state;
        }
    }
}
