using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// One bounded record for an actually met Hero. It counts verified
    /// petition choices and native relation deltas, not fictional AI
    /// loyalty, invented hostility or assumed family memories.
    /// </summary>
    public sealed class SandboxSocialBondState
    {
        public const string Version="CU_SOCIAL_BONDS_1";
        public const int MaxPeople=32;
        public sealed class Bond
        {
            public string HeroId="";
            public int Supported,Declined,Positive,Negative,
                FollowUps,LastDay=-1;
        }
        public readonly List<Bond> People=new List<Bond>();
        private static bool Id(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length<=80 &&
                id.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');
        }
        public Bond For(string heroId)
        {
            return People.FirstOrDefault(x=>x.HeroId==heroId);
        }
        private Bond GetOrCreate(string heroId)
        {
            if(!Id(heroId))return null;
            var bond=For(heroId);
            if(bond!=null)return bond;
            if(People.Count>=MaxPeople)return null;
            bond=new Bond{HeroId=heroId};
            People.Add(bond);
            return bond;
        }
        public bool RecordPetition(string heroId,bool accepted,
            int observedNativeRelationDelta,int day)
        {
            if(day<0 || observedNativeRelationDelta< -100 ||
                observedNativeRelationDelta>100)return false;
            var bond=GetOrCreate(heroId);
            if(bond==null || (bond.LastDay>=day &&
                bond.Supported+bond.Declined>0))return false;
            if(accepted)
            {
                if(bond.Supported>=16)return false;
                bond.Supported++;
            }
            else
            {
                if(bond.Declined>=16)return false;
                bond.Declined++;
            }
            if(observedNativeRelationDelta>0)
                bond.Positive=Math.Min(100,
                    bond.Positive+observedNativeRelationDelta);
            if(observedNativeRelationDelta<0)
                bond.Negative=Math.Max(-100,
                    bond.Negative+observedNativeRelationDelta);
            bond.LastDay=day;
            return true;
        }
        public bool RecordFollowUp(string heroId,
            int observedNativeRelationDelta,int day)
        {
            if(day<0 || observedNativeRelationDelta< -100 ||
                observedNativeRelationDelta>100)return false;
            var bond=For(heroId);
            if(bond==null || day<=bond.LastDay ||
                bond.FollowUps>=16)return false;
            bond.FollowUps++;
            if(observedNativeRelationDelta>0)
                bond.Positive=Math.Min(100,
                    bond.Positive+observedNativeRelationDelta);
            if(observedNativeRelationDelta<0)
                bond.Negative=Math.Max(-100,
                    bond.Negative+observedNativeRelationDelta);
            bond.LastDay=day;
            return true;
        }
        public string SocialStanding(string heroId,int nativeRelation)
        {
            var bond=For(heroId);
            if(bond==null)return "No petition history recorded";
            // These are descriptive records, never native
            // kingdom, friendship, war or companion AI orders.
            if(nativeRelation<=-30 && bond.Declined>=2 &&
                bond.Negative<0)return "Repeated grievance";
            if(nativeRelation>=45 && bond.Supported>=2 &&
                bond.Positive>0)return "Established goodwill";
            if(nativeRelation<=-10 && bond.Declined>=1)
                return "Strained relations";
            if(nativeRelation>=20 && bond.Supported>=1)
                return "Recorded goodwill";
            return "History recorded, current standing uncertain";
        }
        public string Serialize()
        {
            var lines=new List<string>{Version};
            foreach(var b in People.OrderBy(x=>x.HeroId,
                StringComparer.Ordinal))
                lines.Add(String.Join("|",new[]{
                    b.HeroId,b.Supported.ToString(
                        CultureInfo.InvariantCulture),
                    b.Declined.ToString(CultureInfo.InvariantCulture),
                    b.Positive.ToString(CultureInfo.InvariantCulture),
                    b.Negative.ToString(CultureInfo.InvariantCulture),
                    b.FollowUps.ToString(CultureInfo.InvariantCulture),
                    b.LastDay.ToString(CultureInfo.InvariantCulture)}));
            return String.Join("\n",lines);
        }
        public static SandboxSocialBondState Parse(string input)
        {
            var empty=new SandboxSocialBondState();
            if(String.IsNullOrEmpty(input) || input.Length>5000)
                return empty;
            var lines=input.Replace("\r","").Split('\n');
            if(lines.Length>MaxPeople+1 || lines[0]!=Version)
                return empty;
            var result=new SandboxSocialBondState();
            foreach(var line in lines.Skip(1))
            {
                var f=line.Split('|');
                int supported,declined,positive,negative,follow,day;
                if(f.Length!=7 || !Id(f[0]) ||
                    result.For(f[0])!=null ||
                    !Int32.TryParse(f[1],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out supported) ||
                    !Int32.TryParse(f[2],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out declined) ||
                    !Int32.TryParse(f[3],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out positive) ||
                    !Int32.TryParse(f[4],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out negative) ||
                    !Int32.TryParse(f[5],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out follow) ||
                    !Int32.TryParse(f[6],NumberStyles.Integer,
                        CultureInfo.InvariantCulture,out day) ||
                    supported<0 || supported>16 ||
                    declined<0 || declined>16 ||
                    positive<0 || positive>100 ||
                    negative>0 || negative< -100 ||
                    follow<0 || follow>16 || day<0)
                    return empty;
                result.People.Add(new Bond{
                    HeroId=f[0],Supported=supported,
                    Declined=declined,Positive=positive,
                    Negative=negative,FollowUps=follow,
                    LastDay=day});
            }
            return result;
        }
    }
}
