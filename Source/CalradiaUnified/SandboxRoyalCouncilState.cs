using System;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// One actual Hero per royal office. Kingsguard is a single named
    /// native Hero companion, never a troop stack, formation, recruitable
    /// guard unit or special new CharacterObject.
    /// Saves a local royal council's verified members and meetings only.
    /// Does not modify the game's legal capital or kingdom votes.
    /// </summary>
    public sealed class SandboxRoyalCouncilState
    {
        public const string Version="CU_ROYAL_HERO_COUNCIL_1";
        public string KingdomId="",SeatId="",KingId="";
        public string MarshalId="",MinisterId="",KingsguardHeroId="";
        public int LastMeetingDay=-1,Meetings;
        public bool IsRecorded
        {
            get{return ValidId(KingdomId) && ValidId(SeatId) &&
                ValidId(KingId) && ValidId(MarshalId) &&
                ValidId(MinisterId) && ValidId(KingsguardHeroId) &&
                new[]{KingId,MarshalId,MinisterId,KingsguardHeroId}
                    .Distinct(StringComparer.Ordinal).Count()==4 &&
                LastMeetingDay>=-1 && Meetings>=0 &&
                Meetings<=1000000;}
        }
        private static bool ValidId(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=100 &&
                s.All(c=>Char.IsLetterOrDigit(c) ||
                    c=='_' || c=='-' || c=='.');
        }
        public string Begin(string kingdom,string seat,string king,
            string marshal,string minister,string kingsguard)
        {
            if(IsRecorded)return "already_commissioned";
            if(!ValidId(kingdom) || !ValidId(seat) ||
               !ValidId(king) || !ValidId(marshal) ||
               !ValidId(minister) || !ValidId(kingsguard) ||
               new[]{king,marshal,minister,kingsguard}
                   .Distinct(StringComparer.Ordinal).Count()!=4)
                return "native_royal_council_hero_identity_unverified";
            KingdomId=kingdom;SeatId=seat;KingId=king;
            MarshalId=marshal;MinisterId=minister;
            KingsguardHeroId=kingsguard;
            LastMeetingDay=-1;Meetings=0;
            return "native_royal_hero_council_commissioned";
        }
        public string Convene(int day,bool kingAtSeat,
            bool ministerActuallyGoverns,bool marshalInParty,
            bool kingsguardHeroInParty,bool stillNativeRuler)
        {
            if(!IsRecorded)return "council_not_commissioned";
            if(day<0 || day<LastMeetingDay)
                return "council_day_invalid";
            if(!kingAtSeat || !ministerActuallyGoverns ||
               !marshalInParty || !kingsguardHeroInParty ||
               !stillNativeRuler)
                return "native_royal_council_not_present";
            if(LastMeetingDay==day)
                return "already_convened_today";
            Meetings++;LastMeetingDay=day;
            return "real_royal_hero_council_convened";
        }
        private static string E(string v){return Uri.EscapeDataString(v??"");}
        private static string D(string v)
        {
            try{return Uri.UnescapeDataString(v??"");}
            catch{return "";}
        }
        public string Serialize()
        {
            if(!IsRecorded)return Version;
            return Version+"\n"+String.Join("|",new[]{
                E(KingdomId),E(SeatId),E(KingId),
                E(MarshalId),E(MinisterId),E(KingsguardHeroId),
                LastMeetingDay.ToString(CultureInfo.InvariantCulture),
                Meetings.ToString(CultureInfo.InvariantCulture)
            });
        }
        public static SandboxRoyalCouncilState Parse(string value)
        {
            var blank=new SandboxRoyalCouncilState();
            if(String.IsNullOrEmpty(value) || value==Version ||
               value.Length>3072)return blank;
            var lines=value.Split('\n');
            if(lines.Length!=2 || lines[0]!=Version)return blank;
            var fields=lines[1].Split('|');
            if(fields.Length!=8)return blank;
            int day,meetings;
            if(!Int32.TryParse(fields[6],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out day) ||
               !Int32.TryParse(fields[7],NumberStyles.Integer,
                   CultureInfo.InvariantCulture,out meetings))
                return blank;
            var state=new SandboxRoyalCouncilState{
                KingdomId=D(fields[0]),SeatId=D(fields[1]),
                KingId=D(fields[2]),MarshalId=D(fields[3]),
                MinisterId=D(fields[4]),KingsguardHeroId=D(fields[5]),
                LastMeetingDay=day,Meetings=meetings
            };
            return state.IsRecorded?state:blank;
        }
    }
}
