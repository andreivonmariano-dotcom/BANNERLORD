using System;

namespace CalradiaUnified
{
    /// <summary>
    /// A recorded kinship interest is NOT a legal fief grant. Native
    /// ownership and culturally contingent inheritance law stay authoritative.
    /// </summary>
    public sealed class SandboxSuccessionInterestState
    {
        public string ParentId="",MotherId="",ClaimantId="",BirthClanId="";
        public string Status="Unrecorded";
        public int EarlierSonsAtStart;
        public bool ParentWasAlive;
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
            return "CU_SUCCESSION_1\n"+String.Join("|",new[]{
                Uri.EscapeDataString(ClaimantId),
                Uri.EscapeDataString(ParentId),
                Uri.EscapeDataString(MotherId),
                Uri.EscapeDataString(BirthClanId),
                EarlierSonsAtStart.ToString(),
                ParentWasAlive.ToString(),
                Uri.EscapeDataString(Status)});
        }
        public static SandboxSuccessionInterestState Parse(string text)
        {
            var state=new SandboxSuccessionInterestState();
            if(String.IsNullOrEmpty(text) || text.Length>2048)return state;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!="CU_SUCCESSION_1")
                return state;
            var f=lines[1].Split('|');
            int older;
            bool alive;
            if(f.Length!=7 || !Int32.TryParse(f[4],out older) ||
                older<0 || older>20 ||
                !Boolean.TryParse(f[5],out alive))return state;
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
            }
            catch{return new SandboxSuccessionInterestState();}
            return state;
        }
    }
}
