using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// Native service is distinct from a personal contact and from actual
    /// third-party enlistment. An external SaS snapshot is accepted only
    /// as observed evidence. CU never adds wages or native rank.
    /// </summary>
    public sealed class SandboxServiceCareerState
    {
        public string Role="",PatronId="",KingdomId="";
        public string Status="Unbound",PayAuthority="NativeGame";
        public int DaysInService,LastObservedGold,LastObservedGoldChange;
        public bool HasGoldBaseline;
        public int Milestone;
        public bool VerifiedExternalEnlistment,ExternalInsideHostParty;
        public string ExternalLordId="",ExternalRank="",ExternalDuty="";
        public int ExternalDailyWage=-1,ExternalTermDaysLeft=-1;
        public bool IsConfigured{get{return PatronId.Length>0 &&
            (Role=="Knight" || Role=="Soldier" ||
             Role=="Recruit" || Role=="HouseholdKnight");}}
        public void Start(string role,string patron,string kingdom)
        {
            if(role!="Soldier" && role!="Knight" &&
                role!="Recruit" && role!="HouseholdKnight")return;
            if(String.IsNullOrEmpty(patron) ||
                String.IsNullOrEmpty(kingdom))return;
            Role=role;PatronId=patron;KingdomId=kingdom;
            Status=role=="Recruit"?"PendingEnlistment":
                role=="HouseholdKnight"?"HouseholdContact":"Active";
            DaysInService=0;Milestone=0;
            HasGoldBaseline=false;PayAuthority="NativeGame";
            VerifiedExternalEnlistment=false;
            ExternalLordId="";ExternalRank="";ExternalDuty="";
            ExternalDailyWage=-1;ExternalTermDaysLeft=-1;
            ExternalInsideHostParty=false;
        }
        public string Observe(bool lordAlive,bool sameKingdom,int playerGold)
        {
            if(!IsConfigured)return "unbound";
            if(Status=="PatronDeceased" ||
                Status=="NativeContractChanged")return Status;
            if(!lordAlive)
            {
                Status="PatronDeceased";
                return "patron_deceased";
            }
            if(!sameKingdom)
            {
                Status="NativeContractChanged";
                return "native_contract_changed";
            }
            // A personal agreement or attempt to enlist is not daily
            // service in somebody's army. Do not accrue fake tenure.
            if(Role=="Recruit" && !VerifiedExternalEnlistment)
                return "pending_enlistment";
            if(Role=="HouseholdKnight" && !VerifiedExternalEnlistment)
                return "household_contact_only";
            if(VerifiedExternalEnlistment)
                return "external_service_authoritative";
            Status="Active";
            if(HasGoldBaseline)
                LastObservedGoldChange=playerGold-LastObservedGold;
            else LastObservedGoldChange=0;
            LastObservedGold=playerGold;
            HasGoldBaseline=true;DaysInService++;
            if(DaysInService>=90 && Milestone<90)
            {Milestone=90;return "ninety_days_service_recorded";}
            if(DaysInService>=30 && Milestone<30)
            {Milestone=30;return "thirty_days_service_recorded";}
            return "active";
        }
        public string ObserveExternal(string lord,string rank,string duty,
            int wage,int days,int term,bool insideHost)
        {
            if(!IsConfigured || String.IsNullOrWhiteSpace(lord) ||
                wage<0 || days<0 || term<0)return "external_invalid";
            bool first=!VerifiedExternalEnlistment;
            bool promoted=VerifiedExternalEnlistment && ExternalRank!=rank;
            bool reassigned=VerifiedExternalEnlistment && ExternalDuty!=duty;
            VerifiedExternalEnlistment=true;
            Status="EnlistedActive";
            PayAuthority="ServeAsSoldier";
            ExternalLordId=lord;ExternalRank=rank??"";
            ExternalDuty=duty??"";
            ExternalDailyWage=wage;ExternalTermDaysLeft=term;
            ExternalInsideHostParty=insideHost;
            DaysInService=days;
            if(first)return "external_enlistment_verified";
            if(promoted)return "external_rank_changed_observed";
            if(reassigned)return "external_duty_changed_observed";
            return "external_active";
        }
        /// <summary>
        /// A missing provider proves neither discharge nor current rank.
        /// Suspend stale saved evidence until a real SaS snapshot returns.
        /// </summary>
        public string SuspendUnverifiedExternal()
        {
            if(!VerifiedExternalEnlistment)return "not_required";
            VerifiedExternalEnlistment=false;
            Status="ExternalStatusUnverified";
            PayAuthority="ExternalProviderUnavailable";
            ExternalDailyWage=-1;
            ExternalTermDaysLeft=-1;
            ExternalInsideHostParty=false;
            return "external_provider_unavailable";
        }
        public string ObserveExternalAbsent()
        {
            if(!VerifiedExternalEnlistment)return "no_external_service";
            VerifiedExternalEnlistment=false;
            Status="EnlistmentEnded";
            ExternalInsideHostParty=false;
            ExternalDailyWage=-1;
            ExternalTermDaysLeft=-1;
            PayAuthority="NativeGame";
            return "external_service_ended_observed";
        }
        public string Serialize()
        {
            return "CU_SERVICE_2\n"+String.Join("|",new[]{
                Uri.EscapeDataString(Role),
                Uri.EscapeDataString(PatronId),
                Uri.EscapeDataString(KingdomId),
                Uri.EscapeDataString(Status),
                DaysInService.ToString(CultureInfo.InvariantCulture),
                LastObservedGold.ToString(CultureInfo.InvariantCulture),
                LastObservedGoldChange.ToString(CultureInfo.InvariantCulture),
                HasGoldBaseline.ToString(),
                Milestone.ToString(CultureInfo.InvariantCulture),
                Uri.EscapeDataString(PayAuthority),
                VerifiedExternalEnlistment.ToString(),
                Uri.EscapeDataString(ExternalLordId),
                Uri.EscapeDataString(ExternalRank),
                Uri.EscapeDataString(ExternalDuty),
                ExternalDailyWage.ToString(CultureInfo.InvariantCulture),
                ExternalTermDaysLeft.ToString(CultureInfo.InvariantCulture),
                ExternalInsideHostParty.ToString()});
        }
        public static SandboxServiceCareerState Parse(string text)
        {
            var state=new SandboxServiceCareerState();
            if(String.IsNullOrEmpty(text)||text.Length>4096)return state;
            var lines=text.Split('\n');
            if(lines.Length!=2 || (lines[0]!="CU_SERVICE_1" &&
                lines[0]!="CU_SERVICE_2"))return state;
            var f=lines[1].Split('|');
            bool newVersion=lines[0]=="CU_SERVICE_2";
            if(f.Length!=(newVersion?17:9))return state;
            int days,gold,delta,milestone;
            bool baseline;
            if(!Int32.TryParse(f[4],out days)||days<0 ||
                !Int32.TryParse(f[5],out gold) ||
                !Int32.TryParse(f[6],out delta) ||
                !Boolean.TryParse(f[7],out baseline) ||
                !Int32.TryParse(f[8],out milestone) ||
                (milestone!=0 && milestone!=30 && milestone!=90))
                return state;
            try
            {
                string role=Uri.UnescapeDataString(f[0]);
                string patron=Uri.UnescapeDataString(f[1]);
                string kingdom=Uri.UnescapeDataString(f[2]);
                string status=Uri.UnescapeDataString(f[3]);
                if(role!="Soldier" && role!="Knight" &&
                    role!="Recruit" && role!="HouseholdKnight")
                    return state;
                if(status!="Active" && status!="PatronDeceased" &&
                    status!="NativeContractChanged" &&
                    status!="PendingEnlistment" &&
                    status!="HouseholdContact" &&
                    status!="EnlistedActive" &&
                    status!="EnlistmentEnded" &&
                    status!="ExternalStatusUnverified")
                    return state;
                state.Role=role;state.PatronId=patron;
                state.KingdomId=kingdom;state.Status=status;
                state.DaysInService=days;
                state.LastObservedGold=gold;
                state.LastObservedGoldChange=delta;
                state.HasGoldBaseline=baseline;state.Milestone=milestone;
                if(newVersion)
                {
                    bool external,inside;
                    int wage,term;
                    if(!Boolean.TryParse(f[10],out external) ||
                        !Boolean.TryParse(f[16],out inside) ||
                        !Int32.TryParse(f[14],out wage) ||
                        !Int32.TryParse(f[15],out term))
                        return new SandboxServiceCareerState();
                    state.PayAuthority=Uri.UnescapeDataString(f[9]);
                    state.VerifiedExternalEnlistment=external;
                    state.ExternalLordId=Uri.UnescapeDataString(f[11]);
                    state.ExternalRank=Uri.UnescapeDataString(f[12]);
                    state.ExternalDuty=Uri.UnescapeDataString(f[13]);
                    state.ExternalDailyWage=wage;
                    state.ExternalTermDaysLeft=term;
                    state.ExternalInsideHostParty=inside;
                }
            }
            catch{return new SandboxServiceCareerState();}
            return state;
        }
    }
}
