using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Tracks ACTUAL native relation scores across daily observations.
    /// An observed score change is not assigned a fictional cause.
    /// </summary>
    public sealed class SandboxSocialObservationState
    {
        public sealed class Change
        {
            public string Target="";
            public int Before,After;
        }
        public readonly Dictionary<string,int> NativeRelations=
            new Dictionary<string,int>(StringComparer.Ordinal);
        public const int MaxObserved=64;
        public Change Observe(string heroId,int nativeRelation)
        {
            if(String.IsNullOrWhiteSpace(heroId))return null;
            int before;
            if(!NativeRelations.TryGetValue(heroId,out before))
            {
                if(NativeRelations.Count<MaxObserved)
                    NativeRelations[heroId]=nativeRelation;
                return null;
            }
            if(before==nativeRelation)return null;
            NativeRelations[heroId]=nativeRelation;
            return new Change{
                Target=heroId,Before=before,After=nativeRelation
            };
        }
        public string Serialize()
        {
            var rows=new List<string>{"CU_SOCIAL_1"};
            foreach(var pair in NativeRelations.OrderBy(x=>x.Key,
                StringComparer.Ordinal))
                rows.Add(Uri.EscapeDataString(pair.Key)+"|"+pair.Value);
            return String.Join("\n",rows);
        }
        public static SandboxSocialObservationState Parse(string text)
        {
            var state=new SandboxSocialObservationState();
            if(String.IsNullOrEmpty(text) || text.Length>12000)return state;
            var lines=text.Replace("\r","").Split('\n');
            if(lines.Length==0 || lines[0]!="CU_SOCIAL_1")return state;
            foreach(var row in lines.Skip(1).Take(MaxObserved))
            {
                var f=row.Split('|');
                int score;
                if(f.Length!=2 || !Int32.TryParse(f[1],out score) ||
                   score< -100 || score>100)continue;
                try
                {
                    var id=Uri.UnescapeDataString(f[0]);
                    if(id.Length>0 && id.Length<=128)
                        state.Observe(id,score);
                }
                catch{}
            }
            return state;
        }
    }
}
