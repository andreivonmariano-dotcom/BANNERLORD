using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    // A bounded social model, independent of native relation mutation.
    // Each pair is canonical and records actual native standing at sampling.
    public sealed class SandboxPeerSocialState
    {
        public const int MaxPairs=48, Interval=7;
        public const string Version="CU_PEER_SOCIAL_1";
        public sealed class Pair
        {
            public string A="",B="",Interest="";
            public int FirstDay,LastDay,NativeRelation,Meetings;
        }
        public readonly List<Pair> Pairs=new List<Pair>();
        private static bool ValidId(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.Length<=80 &&
                s.All(c=>Char.IsLetterOrDigit(c) || c=='_' || c=='-');
        }
        private static bool ValidInterest(string s)
        {
            return s=="CommonInterest" || s=="DifferentPriorities";
        }
        private static bool ValidPair(string a,string b)
        {
            return ValidId(a) && ValidId(b) && a!=b;
        }
        public Pair For(string a,string b)
        {
            if(!ValidPair(a,b))return null;
            if(StringComparer.Ordinal.Compare(a,b)>0){string t=a;a=b;b=t;}
            return Pairs.FirstOrDefault(p=>p.A==a && p.B==b);
        }
        public bool CanObserve(string a,string b,int day)
        {
            if(!ValidPair(a,b) || day<0 || day>2000000)return false;
            if(Pairs.Any(p=>p.LastDay>=day))return false;
            var pair=For(a,b);
            return pair==null?Pairs.Count<MaxPairs:(long)day-pair.LastDay>=Interval;
        }
        public bool Observe(string a,string b,int day,int nativeRelation,string interest)
        {
            if(!CanObserve(a,b,day) || nativeRelation< -100 || nativeRelation>100 ||
                !ValidInterest(interest))return false;
            var p=For(a,b);
            if(p==null)
            {
                if(StringComparer.Ordinal.Compare(a,b)>0){string t=a;a=b;b=t;}
                p=new Pair{A=a,B=b,FirstDay=day};
                Pairs.Add(p);
            }
            p.LastDay=day;p.NativeRelation=nativeRelation;p.Interest=interest;
            p.Meetings=Math.Min(10000,p.Meetings+1);
            return true;
        }
        public static string Describe(int nativeRelation,string interest)
        {
            if(!ValidInterest(interest))return "";
            if(nativeRelation<=-20)return "Strained relations";
            if(nativeRelation>=30)return interest=="CommonInterest"?
                "Good standing and common interests":"Good standing, different priorities";
            return interest=="CommonInterest"?"Common interests":"Different priorities";
        }
        public string Serialize()
        {
            var rows=new List<string>{Version};
            foreach(var p in Pairs.OrderBy(x=>x.A,StringComparer.Ordinal).ThenBy(x=>x.B,StringComparer.Ordinal))
                rows.Add(String.Join("|",new[]{p.A,p.B,p.FirstDay.ToString(CultureInfo.InvariantCulture),
                    p.LastDay.ToString(CultureInfo.InvariantCulture),p.NativeRelation.ToString(CultureInfo.InvariantCulture),
                    p.Meetings.ToString(CultureInfo.InvariantCulture),p.Interest}));
            return String.Join("\n",rows);
        }
        public static SandboxPeerSocialState Parse(string packed)
        {
            var s=new SandboxPeerSocialState();
            if(String.IsNullOrEmpty(packed) || packed.Length>20000)return s;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows.Length>MaxPairs+1 || rows[0]!=Version)return s;
            foreach(var row in rows.Skip(1))
            {
                var f=row.Split('|');int first,last,relation,count;
                if(f.Length!=7 || !ValidPair(f[0],f[1]) ||
                    StringComparer.Ordinal.Compare(f[0],f[1])>=0 || s.For(f[0],f[1])!=null ||
                    !Int32.TryParse(f[2],out first) || !Int32.TryParse(f[3],out last) ||
                    !Int32.TryParse(f[4],out relation) || !Int32.TryParse(f[5],out count) ||
                    first<0 || last<first || last>2000000 || relation< -100 || relation>100 ||
                    count<1 || count>10000 || (long)(count-1)*Interval>last-first ||
                    !ValidInterest(f[6]))return new SandboxPeerSocialState();
                s.Pairs.Add(new Pair{A=f[0],B=f[1],FirstDay=first,LastDay=last,
                    NativeRelation=relation,Meetings=count,Interest=f[6]});
            }
            return s;
        }
    }
}
