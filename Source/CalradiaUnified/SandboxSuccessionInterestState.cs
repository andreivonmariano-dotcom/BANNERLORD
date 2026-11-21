using System;

namespace CalradiaUnified
{
    /// <summary>
    /// A potential native family interest is not a legal fief grant.
    /// Regional custom is a proposed gameplay interpretation; native
    /// ownership, living claimants and actual faction decisions win.
    /// </summary>
    public sealed class SandboxSuccessionInterestState
    {
        public string ParentId="",MotherId="",ClaimantId="",BirthClanId="";
        public string Status="Unrecorded";
        public int EarlierSonsAtStart;
        public bool ParentWasAlive;
        public string CultureId="",CustomaryModel="Unestablished";
        public int BirthClanFiefsAtStart,PriorityScore=-1;
        public bool IsRecorded{get{return ClaimantId.Length>0 &&
            ParentId.Length>0;}}
        public void Record(string claimant,string father,string mother,
            string birthClan,int olderSons,bool parentAlive)
        {
            if(String.IsNullOrWhiteSpace(claimant) ||
               String.IsNullOrWhiteSpace(father) ||
               olderSons<0)return;
            ClaimantId=claimant;ParentId=father;MotherId=mother??"";
            BirthClanId=birthClan??"";
            EarlierSonsAtStart=olderSons;
            ParentWasAlive=parentAlive;
            Status="PotentialFamilyInterest";
        }
        public void SetCulture(string culture,int nativeFiefs)
        {
            CultureId=culture??"";
            CustomaryModel=SandboxCulturalInheritancePolicy.Model(
                CultureId);
            BirthClanFiefsAtStart=Math.Max(0,nativeFiefs);
            // No native evidence of all claimant preferences or council
            // votes. Do not fake a legal entitlement or succession order.
            PriorityScore=-1;
        }
        public string Observe(bool parentAlive)
        {
            if(!IsRecorded)return "unrecorded";
            if(!parentAlive && ParentWasAlive)
            {
                Status="ReviewNativeSuccession";
                ParentWasAlive=false;
                return "parent_death_observed_native_ownership_unchanged";
            }
            ParentWasAlive=parentAlive;
            return Status;
        }
        public string Serialize()
        {
            return "CU_SUCCESSION_2\n"+String.Join("|",new[]{
                Uri.EscapeDataString(ClaimantId),
                Uri.EscapeDataString(ParentId),
                Uri.EscapeDataString(MotherId),
                Uri.EscapeDataString(BirthClanId),
                EarlierSonsAtStart.ToString(),
                ParentWasAlive.ToString(),
                Uri.EscapeDataString(Status),
                Uri.EscapeDataString(CultureId),
                Uri.EscapeDataString(CustomaryModel),
                BirthClanFiefsAtStart.ToString(),
                PriorityScore.ToString()});
        }
        public static SandboxSuccessionInterestState Parse(string text)
        {
            var state=new SandboxSuccessionInterestState();
            if(String.IsNullOrEmpty(text)||text.Length>4096)return state;
            var lines=text.Split('\n');
            if(lines.Length!=2 || (lines[0]!="CU_SUCCESSION_1" &&
                lines[0]!="CU_SUCCESSION_2"))return state;
            bool upgraded=lines[0]=="CU_SUCCESSION_2";
            var f=lines[1].Split('|');
            int older;bool alive;
            if(f.Length!=(upgraded?11:7) ||
                !Int32.TryParse(f[4],out older) || older<0 ||
                older>20 || !Boolean.TryParse(f[5],out alive))
                return state;
            try
            {
                var status=Uri.UnescapeDataString(f[6]);
                if(status!="PotentialFamilyInterest" &&
                   status!="ReviewNativeSuccession")return state;
                state.Record(Uri.UnescapeDataString(f[0]),
                    Uri.UnescapeDataString(f[1]),
                    Uri.UnescapeDataString(f[2]),
                    Uri.UnescapeDataString(f[3]),older,alive);
                state.Status=status;
                if(upgraded)
                {
                    int fiefs,priority;
                    if(!Int32.TryParse(f[9],out fiefs) || fiefs<0 ||
                        !Int32.TryParse(f[10],out priority) ||
                        priority< -1)return new SandboxSuccessionInterestState();
                    state.CultureId=Uri.UnescapeDataString(f[7]);
                    state.CustomaryModel=Uri.UnescapeDataString(f[8]);
                    if(state.CustomaryModel!=
                        SandboxCulturalInheritancePolicy.Model(
                            state.CultureId))
                        return new SandboxSuccessionInterestState();
                    state.BirthClanFiefsAtStart=fiefs;
                    state.PriorityScore=priority;
                }
            }
            catch{return new SandboxSuccessionInterestState();}
            return state;
        }
    }
}
