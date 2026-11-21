using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Persists the exact observed property and consenting native leader.
    /// A clan grant is NOT completed until a native Action changes owner.
    /// </summary>
    public sealed class SandboxInheritanceNegotiationState
    {
        public string Status="NotFiled",SettlementId="",LeaderId="",
            BirthClanId="",CultureModel="",Reason="";
        public int PetitionDay=-1,Attempts,Mediations;
        public bool Authorized{get{return Status=="ClanHeadConsents" &&
            SettlementId.Length>0 && LeaderId.Length>0;}}
        public string Petition(string outcome,string settlement,
            string leader,string birthClan,string model,int day)
        {
            if(Attempts>=3 || Status=="ClaimWithdrawn" ||
                Status=="NativeGrantCompleted")
                return "PetitionLimitReached";
            Attempts++;
            PetitionDay=day;
            Reason=outcome??"Unassessed";
            SettlementId="";LeaderId="";BirthClanId="";
            CultureModel=model??"";
            if(outcome=="ClanHeadConsents" &&
                !String.IsNullOrWhiteSpace(settlement) &&
                !String.IsNullOrWhiteSpace(leader) &&
                !String.IsNullOrWhiteSpace(birthClan))
            {
                Status="ClanHeadConsents";
                SettlementId=settlement;LeaderId=leader;
                BirthClanId=birthClan;
            }
            else Status=outcome=="ClaimContested"?
                "ClaimContested":"ClaimRejected";
            return Status;
        }
        public bool RecordMediation()
        {
            if(Status!="ClaimContested" || Mediations>=2)return false;
            Mediations++;
            return true;
        }
        public string Withdraw()
        {
            if(!SandboxInheritanceNegotiationPolicy.MayWithdraw(
                Status))return "no_open_dispute";
            Status="ClaimWithdrawn";
            SettlementId="";LeaderId="";
            return Status;
        }
        public string Complete(bool nativeOwnerConfirmed)
        {
            if(!Authorized)return "not_authorized";
            if(!nativeOwnerConfirmed)return "native_owner_not_changed";
            Status="NativeGrantCompleted";
            Reason="NativeSettlementOwnerConfirmed";
            return "native_grant_confirmed";
        }
        public string Cancel()
        {
            if(!Authorized)return "no_open_consent";
            Status="GrantCancelled";
            return Status;
        }
        private static string E(string value)
        {
            return Uri.EscapeDataString(value??"");
        }
        private static string D(string value)
        {
            try{return Uri.UnescapeDataString(value??"");}
            catch{return "";}
        }
        public string Serialize()
        {
            return "CU_NEGOTIATION_2\n"+String.Join("|",new[]{
                E(Status),E(SettlementId),E(LeaderId),E(BirthClanId),
                E(CultureModel),E(Reason),PetitionDay.ToString(),
                Attempts.ToString(),Mediations.ToString()});
        }
        public static SandboxInheritanceNegotiationState Parse(
            string value)
        {
            var r=new SandboxInheritanceNegotiationState();
            if(String.IsNullOrEmpty(value) || value.Length>3000)return r;
            var lines=value.Split('\n');
            if(lines.Length!=2 || (lines[0]!="CU_NEGOTIATION_1" &&
                lines[0]!="CU_NEGOTIATION_2"))return r;
            bool second=lines[0]=="CU_NEGOTIATION_2";
            var f=lines[1].Split('|');
            int day,attempts,mediations=0;
            if(f.Length!=(second?9:8) ||
                !Int32.TryParse(f[6],out day) ||
                !Int32.TryParse(f[7],out attempts) ||
                attempts<0 || attempts>3 || day< -1 ||
                (second && (!Int32.TryParse(f[8],out mediations) ||
                    mediations<0 || mediations>2)))return r;
            r.Status=D(f[0]);r.SettlementId=D(f[1]);
            r.LeaderId=D(f[2]);r.BirthClanId=D(f[3]);
            r.CultureModel=D(f[4]);r.Reason=D(f[5]);
            r.PetitionDay=day;r.Attempts=attempts;
            r.Mediations=mediations;
            if(r.Status!="NotFiled" && r.Status!="ClanHeadConsents" &&
               r.Status!="ClaimContested" &&
               r.Status!="ClaimRejected" &&
               r.Status!="NativeGrantCompleted" &&
               r.Status!="GrantCancelled" &&
               r.Status!="ClaimWithdrawn")
                return new SandboxInheritanceNegotiationState();
            if(r.Status=="ClanHeadConsents" &&
               (r.SettlementId.Length==0 || r.LeaderId.Length==0 ||
                r.BirthClanId.Length==0))
                return new SandboxInheritanceNegotiationState();
            return r;
        }
    }
}
