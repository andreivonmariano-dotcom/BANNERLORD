using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// Save-backed record of a real native kingdom contract and personal
    /// lord. This does NOT fabricate enlisted membership in an NPC party,
    /// double-pay native mercenary wages, or grant unearned noble ranks.
    /// </summary>
    public sealed class SandboxServiceCareerState
    {
        public string Role="",PatronId="",KingdomId="";
        public string Status="Unbound";
        public string PayAuthority="NativeGame";
        public int DaysInService,LastObservedGold,LastObservedGoldChange;
        public bool HasGoldBaseline;
        public int Milestone;
        public bool IsConfigured
        {
            get{return PatronId.Length>0 &&
                (Role=="Knight" || Role=="Soldier");}
        }
        public void Start(string role,string patron,string kingdom)
        {
            if(role!="Soldier" && role!="Knight")return;
            if(String.IsNullOrEmpty(patron) ||
                String.IsNullOrEmpty(kingdom))return;
            Role=role;PatronId=patron;KingdomId=kingdom;
            Status="Active";
            DaysInService=0;Milestone=0;
            HasGoldBaseline=false;
            PayAuthority="NativeGame";
        }
        public string Observe(bool lordAlive,bool sameKingdom,
            int actualPlayerGold)
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
            Status="Active";
            if(HasGoldBaseline)
                LastObservedGoldChange=actualPlayerGold-LastObservedGold;
            else
                LastObservedGoldChange=0;
            LastObservedGold=actualPlayerGold;
            HasGoldBaseline=true;
            DaysInService++;
            if(DaysInService>=90 && Milestone<90)
            {
                Milestone=90;
                return "ninety_days_service_recorded";
            }
            if(DaysInService>=30 && Milestone<30)
            {
                Milestone=30;
                return "thirty_days_service_recorded";
            }
            return "active";
        }
        public string Serialize()
        {
            return "CU_SERVICE_1\n"+
                Uri.EscapeDataString(Role)+"|"+
                Uri.EscapeDataString(PatronId)+"|"+
                Uri.EscapeDataString(KingdomId)+"|"+
                Uri.EscapeDataString(Status)+"|"+
                DaysInService.ToString(CultureInfo.InvariantCulture)+"|"+
                LastObservedGold.ToString(CultureInfo.InvariantCulture)+"|"+
                LastObservedGoldChange.ToString(CultureInfo.InvariantCulture)+"|"+
                HasGoldBaseline.ToString()+"|"+
                Milestone.ToString(CultureInfo.InvariantCulture);
        }
        public static SandboxServiceCareerState Parse(string text)
        {
            var state=new SandboxServiceCareerState();
            if(String.IsNullOrEmpty(text) || text.Length>2048)return state;
            var lines=text.Split('\n');
            if(lines.Length!=2 || lines[0]!="CU_SERVICE_1")return state;
            var f=lines[1].Split('|');
            if(f.Length!=9)return state;
            int days,gold,delta,milestone;
            bool baseline;
            if(!Int32.TryParse(f[4],out days) || days<0 ||
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
                if(role!="Soldier" && role!="Knight")return state;
                if(status!="Active" && status!="PatronDeceased" &&
                   status!="NativeContractChanged")return state;
                state.Role=role;state.PatronId=patron;
                state.KingdomId=kingdom;state.Status=status;
                state.DaysInService=days;
                state.LastObservedGold=gold;
                state.LastObservedGoldChange=delta;
                state.HasGoldBaseline=baseline;state.Milestone=milestone;
            }
            catch{return new SandboxServiceCareerState();}
            return state;
        }
    }
}
