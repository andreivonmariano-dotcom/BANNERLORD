using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaUnified
{
    /// <summary>
    /// Optional READ-ONLY v1 public Serve as Soldier API integration.
    /// The external mod owns enlistment, party control, payroll, ranks,
    /// and discharge. No private API, Harmony hooks or unsafe roster edits.
    /// </summary>
    internal static class SandboxSaSBridge
    {
        internal sealed class Snapshot
        {
            public bool Available,Enlisted,InsideHostParty;
            public bool NativeHostPartyLedByLord,LordIsNativeLord;
            public string LordId="",LordKingdomId="",RankId="",DutyId="";
            public int RankNumber,Wage,Days,TermDaysLeft;
        }
        private static Type ApiType()
        {
            try
            {
                var types=AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a=>a.GetType(
                        "ServeAsSoldier.Compat.SaSPublicApi",false))
                    .Where(t=>t!=null).Distinct().ToArray();
                if(types.Length!=1 ||
                    !SandboxSaSApiContract.IsSupported(types[0]))
                    return null;
                return types[0];
            }
            catch{return null;}
        }
        internal static bool HasSupportedProvider()
        {
            return ApiType()!=null;
        }
        private static object Field(object obj,string name)
        {
            if(obj==null)return null;
            var field=obj.GetType().GetField(name,
                BindingFlags.Instance|BindingFlags.Public);
            return field==null?null:field.GetValue(obj);
        }
        private static int Num(object value)
        {
            try{return value==null?0:Convert.ToInt32(value);}
            catch{return 0;}
        }
        internal static Snapshot Read()
        {
            var result=new Snapshot();
            try
            {
                var api=ApiType();
                if(api==null)return result;
                result.Available=true;
                var prop=api.GetProperty("IsPlayerEnlisted",
                    BindingFlags.Static|BindingFlags.Public);
                if(prop==null)
                {
                    result.Available=false;
                    return result;
                }
                var enlistedValue=prop.GetValue(null,null);
                if(!(enlistedValue is bool))
                {
                    result.Available=false;
                    return result;
                }
                result.Enlisted=(bool)enlistedValue;
                if(!result.Enlisted)return result;
                var method=api.GetMethod("GetSnapshot",
                    BindingFlags.Static|BindingFlags.Public,null,
                    Type.EmptyTypes,null);
                if(method==null)
                {
                    return new Snapshot();
                }
                var snap=method.Invoke(null,null);
                if(snap==null)return new Snapshot();
                var lord=Field(snap,"Lord") as Hero;
                var host=Field(snap,"HostParty") as MobileParty;
                result.LordIsNativeLord=lord!=null &&
                    lord.IsAlive && lord.IsActive && lord.IsLord &&
                    lord.Clan!=null;
                result.LordKingdomId=lord!=null &&
                    lord.Clan!=null && lord.Clan.Kingdom!=null ?
                    lord.Clan.Kingdom.StringId:"";
                result.NativeHostPartyLedByLord=
                    host!=null && lord!=null &&
                    host.LeaderHero==lord;
                result.RankNumber=Num(Field(snap,"Rank"));
                result.InsideHostParty=host!=null &&
                    Field(snap,"IsPlayerInsideHostParty") is bool &&
                    (bool)Field(snap,"IsPlayerInsideHostParty");
                result.LordId=lord==null?"":lord.StringId;
                result.RankId=Field(snap,"RankId") as string ?? "";
                // Incomplete enlisted snapshots are unknown, not a
                // discharge. The campaign observer suspends verification.
                if(!result.LordIsNativeLord ||
                    !result.NativeHostPartyLedByLord ||
                    result.LordId.Length==0 ||
                    SandboxSoldierEnlistmentPolicy.NativeRankNumber(
                        result.RankId)==0)return new Snapshot();
                result.DutyId=Field(snap,"DutyId") as string ?? "";
                result.Wage=Math.Max(0,Num(Field(snap,"DailyWage")));
                result.Days=Math.Max(0,Num(Field(snap,"DaysServed")));
                result.TermDaysLeft=Math.Max(0,
                    Num(Field(snap,"TermDaysLeft")));
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_sas_public_api_read_failed",e);
                return new Snapshot();
            }
            return result;
        }
    }
}
