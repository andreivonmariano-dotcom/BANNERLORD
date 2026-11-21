using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// Save-backed observation of ACTUAL denars held by the native Hero.
    /// Credits are not attributed to wages or revenue, and debits are
    /// not attributed to living expenses without a native event source.
    /// </summary>
    public sealed class SandboxRoleEconomyState
    {
        public string Role="",CareerStatus="";
        public int LastDay=-1,LastGold,LastDelta,DaysObserved;
        public long ObservedCredits,ObservedDebits;
        public int VerifiedExternalDailyWage=-1;
        public bool Started {get{return Role.Length>0;}}

        public bool Start(string role,int gold,int day)
        {
            if(Started || SandboxRoleEconomyPolicy.IncomeAuthority(role)==
                "UnrecognizedRole" || gold<0 || day<0)return false;
            Role=role;LastGold=gold;LastDay=day;
            CareerStatus="AwaitingNativeCareerObservation";
            return true;
        }
        public string Observe(int day,int nativeGold,
            int verifiedExternalWage,string careerStatus)
        {
            if(!Started || day<0 || nativeGold<0 ||
                day<=LastDay)return "not_new_native_day";
            long change=(long)nativeGold-LastGold;
            LastDelta=(int)change;
            if(change>0)ObservedCredits+=change;
            if(change<0)ObservedDebits-=change;
            LastGold=nativeGold;
            LastDay=day;
            if(DaysObserved<Int32.MaxValue)DaysObserved++;
            // -1 means no game-backed payroll. Do not turn a patron or
            // an expected rank into a wage.
            VerifiedExternalDailyWage=verifiedExternalWage>=0?
                verifiedExternalWage:-1;
            CareerStatus=careerStatus??"Unverified";
            return change==0?"native_balance_unchanged":
                "native_balance_change_observed";
        }
        public string Describe()
        {
            return "Role: "+Role+
                ". Income authority: "+
                    SandboxRoleEconomyPolicy.IncomeAuthority(Role)+
                ". Household duty: "+
                    SandboxRoleEconomyPolicy.HouseholdDuty(Role)+
                ". Verified career status: "+CareerStatus+
                ". Native denars last observed: "+LastGold+
                ". Unattributed native credits: "+ObservedCredits+
                ". Unattributed native debits: "+ObservedDebits+
                ". Native payroll confirmed from external public API: "+
                    (VerifiedExternalDailyWage<0?"none":
                        VerifiedExternalDailyWage.ToString())+
                ". No inferred salary, tax, rent, net profit or "+
                "automatic promotion has been granted by this record.";
        }
        private static string E(string text)
        {
            text=text??"";
            if(text.Length>120)text=text.Substring(0,120);
            return Uri.EscapeDataString(text);
        }
        private static string D(string text)
        {
            try
            {
                string value=Uri.UnescapeDataString(text??"");
                return value.Length>120?value.Substring(0,120):value;
            }
            catch{return "";}
        }
        public string Serialize()
        {
            if(!Started)return "CU_ROLE_ECONOMY_1";
            return "CU_ROLE_ECONOMY_1\n"+String.Join("|",new[]{
                E(Role),E(CareerStatus),
                LastDay.ToString(CultureInfo.InvariantCulture),
                LastGold.ToString(CultureInfo.InvariantCulture),
                LastDelta.ToString(CultureInfo.InvariantCulture),
                DaysObserved.ToString(CultureInfo.InvariantCulture),
                ObservedCredits.ToString(CultureInfo.InvariantCulture),
                ObservedDebits.ToString(CultureInfo.InvariantCulture),
                VerifiedExternalDailyWage.ToString(
                    CultureInfo.InvariantCulture)});
        }
        public static SandboxRoleEconomyState Parse(string packed)
        {
            var r=new SandboxRoleEconomyState();
            if(String.IsNullOrEmpty(packed) || packed.Length>2000)return r;
            var lines=packed.Replace("\r","").Split('\n');
            if(lines[0]!="CU_ROLE_ECONOMY_1" ||
                lines.Length==1)return r;
            if(lines.Length!=2)return r;
            var f=lines[1].Split('|');
            int day,gold,delta,days,wage;
            long credits,debits;
            if(f.Length!=9 ||
                !Int32.TryParse(f[2],out day) ||
                !Int32.TryParse(f[3],out gold) ||
                !Int32.TryParse(f[4],out delta) ||
                !Int32.TryParse(f[5],out days) ||
                !Int64.TryParse(f[6],out credits) ||
                !Int64.TryParse(f[7],out debits) ||
                !Int32.TryParse(f[8],out wage) ||
                day<0 || gold<0 || days<0 ||
                credits<0 || debits<0 || wage< -1)
                return r;
            string role=D(f[0]);
            if(SandboxRoleEconomyPolicy.IncomeAuthority(role)==
                "UnrecognizedRole")return r;
            r.Role=role;r.CareerStatus=D(f[1]);
            r.LastDay=day;r.LastGold=gold;r.LastDelta=delta;
            r.DaysObserved=days;r.ObservedCredits=credits;
            r.ObservedDebits=debits;r.VerifiedExternalDailyWage=wage;
            return r;
        }
    }
}
