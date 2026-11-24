using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A bounded social story for an actual campaign Hero. Records choices,
    /// verified native effects and a later in-person conversation. No
    /// invented kinship, generic NPC summons or replayed gifts.
    /// </summary>
    public sealed class SandboxPetitionStoryState
    {
        public const string Version="CU_PETITION_STORIES_1";
        public const int FollowUpDays=7;
        public sealed class Story
        {
            public string HeroId="",Kind="",Outcome="Pending";
            public int StartedDay,ResolvedDay=-1,FollowUpDay=-1;
            public int ObservedRelationChange;
            public bool NativePaymentVerified;
        }
        public readonly List<Story> Stories=new List<Story>();
        private static bool ValidId(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        private static bool ValidKind(string kind)
        {
            return kind=="Recognition" || kind=="HouseholdAid" ||
                kind=="LivelihoodAid";
        }
        public Story For(string id)
        {
            return Stories.FirstOrDefault(x=>x.HeroId==id);
        }
        public bool Begin(string id,string kind,int day)
        {
            if(!ValidId(id)||!ValidKind(kind)||day<0 ||
                day>2000000)return false;
            var previous=For(id);
            // A later eligible invitation can start a new personal story.
            if(previous!=null)
            {
                if((long)day-previous.StartedDay<70 ||
                    day<=previous.ResolvedDay || day<=previous.FollowUpDay)return false;
                Stories.Remove(previous);
            }
            else if(Stories.Count>=16)return false;
            Stories.Add(new Story{
                HeroId=id,Kind=kind,StartedDay=day});
            return true;
        }
        public bool Resolve(string id,bool accepted,int day,
            bool nativePaymentVerified,int nativeRelationDelta)
        {
            var story=For(id);
            if(story==null || story.Outcome!="Pending" ||
                day<story.StartedDay || day<0 ||
                nativeRelationDelta< -100 || nativeRelationDelta>100)return false;
            if(accepted && story.Kind!="Recognition" &&
                !nativePaymentVerified)return false;
            story.Outcome=accepted?"Supported":"Declined";
            story.ResolvedDay=day;
            story.ObservedRelationChange=nativeRelationDelta;
            story.NativePaymentVerified=nativePaymentVerified;
            return true;
        }
        public bool CanFollowUp(string id,int day)
        {
            var story=For(id);
            return story!=null &&
                (story.Outcome=="Supported" ||
                 story.Outcome=="Declined") &&
                story.FollowUpDay<0 && story.ResolvedDay>=0 &&
                day>=story.ResolvedDay+FollowUpDays;
        }
        // Keep the resolved matter available for ordinary conversation
        // after the one-time follow-up, but never duplicate the due option.
        public bool CanRecall(string id,int day)
        {
            var story=For(id);
            return story!=null && (story.Outcome=="Supported" ||
                story.Outcome=="Declined") && story.ResolvedDay>=0 &&
                day>=story.ResolvedDay && !CanFollowUp(id,day);
        }
        public string RecordFollowUp(string id,int day)
        {
            if(!CanFollowUp(id,day))return "followup_not_due";
            var story=For(id);
            story.FollowUpDay=day;
            return "face_to_face_story_followup_recorded";
        }
        public string Summary(string id)
        {
            var s=For(id);
            return s==null?"No personal matter recorded":
                s.Kind+" | "+s.Outcome+
                (s.ResolvedDay>=0?" | resolved on day "+s.ResolvedDay:"")+
                (s.FollowUpDay>=0?" | personally revisited":"");
        }
        public string Serialize()
        {
            var rows=new List<string>{Version};
            foreach(var x in Stories.OrderBy(y=>y.HeroId,
                StringComparer.Ordinal))
                rows.Add(String.Join("|",new[]{
                    x.HeroId,x.Kind,x.Outcome,
                    x.StartedDay.ToString(CultureInfo.InvariantCulture),
                    x.ResolvedDay.ToString(CultureInfo.InvariantCulture),
                    x.FollowUpDay.ToString(CultureInfo.InvariantCulture),
                    x.ObservedRelationChange.ToString(CultureInfo.InvariantCulture),
                    x.NativePaymentVerified?"1":"0"}));
            return String.Join("\n",rows);
        }
        public static SandboxPetitionStoryState Parse(string input)
        {
            var empty=new SandboxPetitionStoryState();
            if(String.IsNullOrEmpty(input)||input.Length>4500)return empty;
            var lines=input.Replace("\r","").Split('\n');
            if(lines[0]!=Version||lines.Length>17)return empty;
            var result=new SandboxPetitionStoryState();
            foreach(var line in lines.Skip(1))
            {
                var f=line.Split('|');
                int start,resolved,follow,relation;
                if(f.Length!=8 || !ValidId(f[0]) ||
                    !ValidKind(f[1]) ||
                    (f[2]!="Pending"&&f[2]!="Supported"&&
                     f[2]!="Declined") ||
                    !Int32.TryParse(f[3],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out start) ||
                    !Int32.TryParse(f[4],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out resolved) ||
                    !Int32.TryParse(f[5],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out follow) ||
                    !Int32.TryParse(f[6],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out relation) ||
                    (f[7]!="0"&&f[7]!="1") ||
                    start<0||resolved< -1||follow< -1 ||
                    relation< -100 || relation>100 || result.For(f[0])!=null ||
                    (f[2]=="Pending" && resolved!=-1) ||
                    (f[2]!="Pending" && resolved<start) ||
                    (follow>=0 && (resolved<0 ||
                        follow<resolved+FollowUpDays)))return empty;
                result.Stories.Add(new Story{
                    HeroId=f[0],Kind=f[1],Outcome=f[2],
                    StartedDay=start,ResolvedDay=resolved,
                    FollowUpDay=follow,ObservedRelationChange=relation,
                    NativePaymentVerified=f[7]=="1"});
            }
            return result;
        }
    }
}
