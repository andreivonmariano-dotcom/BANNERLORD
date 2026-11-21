using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Save-backed, read-only record of a failed Starting Life family.
    /// Never retries phase 9 or disables, creates or teleports native Heroes.
    /// Matching later counts does not prove original identities or ages.
    /// </summary>
    public sealed class SandboxFamilyRecoveryState
    {
        public string PlayerId="",Role="",Failure="",Status="NotRecorded";
        public bool SpouseRequested,SpouseObserved;
        public int ChildrenRequested,SiblingsRequested;
        public int ChildrenObserved,SiblingsObserved,NativeChecks;

        public bool NeedsReview {get{return Status!="NotRecorded";}}

        public void RecordFailure(string playerId,string role,string failure,
            bool spouseRequested,int childrenRequested,
            int siblingsRequested,bool spouseObserved,
            int childrenObserved,int siblingsObserved)
        {
            if(NeedsReview || String.IsNullOrWhiteSpace(playerId))return;
            PlayerId=playerId;
            Role=role??"";
            Failure=failure??"unknown";
            SpouseRequested=spouseRequested;
            ChildrenRequested=Math.Max(0,Math.Min(6,childrenRequested));
            SiblingsRequested=Math.Max(0,Math.Min(4,siblingsRequested));
            SpouseObserved=spouseObserved;
            ChildrenObserved=Math.Max(0,Math.Min(99,childrenObserved));
            SiblingsObserved=Math.Max(0,Math.Min(99,siblingsObserved));
            Status="PartialStartNeedsReview";
        }
        public string Observe(string playerId,bool spousePresent,
            int children,int siblings)
        {
            if(!NeedsReview || playerId!=PlayerId)
                return "no_matching_partial_start";
            bool changed=SpouseObserved!=spousePresent ||
                ChildrenObserved!=children || SiblingsObserved!=siblings;
            SpouseObserved=spousePresent;
            ChildrenObserved=Math.Max(0,Math.Min(99,children));
            SiblingsObserved=Math.Max(0,Math.Min(99,siblings));
            if(NativeChecks<Int32.MaxValue)NativeChecks++;
            bool countsMatch=(!SpouseRequested || spousePresent) &&
                children>=ChildrenRequested &&
                siblings>=SiblingsRequested;
            Status=countsMatch?"NativeCountsMatchReviewStillRequired":
                "PartialStartNeedsReview";
            // No restored grants, automatic retry, or claim of matching
            // specific player-requested names, parentage, and ages.
            return changed?"native_family_counts_changed":"same";
        }
        public string Describe()
        {
            return "Recorded Starting Life failure: "+Failure+
                ". Native spouse observed: "+SpouseObserved+
                ". Native children: "+ChildrenObserved+"/"+ChildrenRequested+
                ". Native siblings: "+SiblingsObserved+"/"+SiblingsRequested+
                ". Status: "+Status+
                ". This is read-only. No new Heroes or starting gifts "+
                "were replayed. Compare the actual family in the "+
                "vanilla Clan screen before continuing.";
        }
        private static string E(string s)
        {
            s=s??"";
            return Uri.EscapeDataString(s.Length>150?s.Substring(0,150):s);
        }
        private static string D(string s)
        {
            try
            {
                string v=Uri.UnescapeDataString(s??"");
                return v.Length>150?v.Substring(0,150):v;
            }
            catch{return "";}
        }
        public string Serialize()
        {
            if(!NeedsReview)return "CU_FAMILY_RECOVERY_1";
            return "CU_FAMILY_RECOVERY_1\n"+String.Join("|",new[]{
                E(PlayerId),E(Role),E(Failure),E(Status),
                SpouseRequested?"1":"0",SpouseObserved?"1":"0",
                ChildrenRequested.ToString(),SiblingsRequested.ToString(),
                ChildrenObserved.ToString(),SiblingsObserved.ToString(),
                NativeChecks.ToString()});
        }
        public static SandboxFamilyRecoveryState Parse(string packed)
        {
            var r=new SandboxFamilyRecoveryState();
            if(String.IsNullOrEmpty(packed) || packed.Length>2000)
                return r;
            var rows=packed.Replace("\r","").Split('\n');
            if(rows[0]!="CU_FAMILY_RECOVERY_1" ||
                rows.Length==1)return r;
            if(rows.Length!=2)return r;
            var f=rows[1].Split('|');
            int children,siblings,observedChildren,observedSiblings,checks;
            if(f.Length!=11 || f[4]!="0" && f[4]!="1" ||
                f[5]!="0" && f[5]!="1" ||
                !Int32.TryParse(f[6],out children) ||
                !Int32.TryParse(f[7],out siblings) ||
                !Int32.TryParse(f[8],out observedChildren) ||
                !Int32.TryParse(f[9],out observedSiblings) ||
                !Int32.TryParse(f[10],out checks) ||
                children<0 || children>6 || siblings<0 || siblings>4 ||
                observedChildren<0 || observedChildren>99 ||
                observedSiblings<0 || observedSiblings>99 || checks<0)
                return r;
            string status=D(f[3]);
            if(status!="PartialStartNeedsReview" &&
                status!="NativeCountsMatchReviewStillRequired")return r;
            r.PlayerId=D(f[0]);r.Role=D(f[1]);r.Failure=D(f[2]);
            if(r.PlayerId.Length==0)return new SandboxFamilyRecoveryState();
            r.Status=status;
            r.SpouseRequested=f[4]=="1";
            r.SpouseObserved=f[5]=="1";
            r.ChildrenRequested=children;r.SiblingsRequested=siblings;
            r.ChildrenObserved=observedChildren;
            r.SiblingsObserved=observedSiblings;r.NativeChecks=checks;
            return r;
        }
    }
}
