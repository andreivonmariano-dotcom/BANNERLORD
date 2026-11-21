using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Deterministic policy-level stress test of medieval starting lives.
    /// Pure C#: cannot exercise native Hero, Party, clan AI or actual scenes.
    /// All mutations stay in model state, never in a Bannerlord campaign.
    /// </summary>
    public static class SandboxMedievalLifeSimulation
    {
        public sealed class Result
        {
            public int Seeds,Days,Checks,Failures,RegionCases,LandlessCases,
                ExternalEnlistments,ComradeshipAwards,ParentDeaths,
                SaveReloads,NativeWagesGranted,NativeFiefsTransferred;
            public string FirstFailure="";
        }
        private static readonly string[] Cultures={
            "vlandia","sturgia","battania","empire","aserai","khuzait"
        };
        private static readonly string[] Roles={
            "Recruit","HouseholdKnight","NobleCadet","Soldier"
        };
        private static void Require(Result result,bool okay,string issue)
        {
            result.Checks++;
            if(okay)return;
            result.Failures++;
            if(result.FirstFailure.Length==0)
                result.FirstFailure=issue;
        }
        public static Result Run(int seeds,int days)
        {
            var result=new Result{Seeds=seeds,Days=days};
            if(seeds<=0 || days<=0)return result;
            for(int s=0;s<seeds;s++)
            {
                var rng=new Random(33013+s*131);
                string culture=Cultures[s%Cultures.Length];
                string role=Roles[s%Roles.Length];
                bool landless=SandboxCustomLifeRolePolicy.IsLandless(role);
                int olderSons=(s%3);
                int olderPlanned=rng.Next(0,olderSons+1);
                int existing=rng.Next(0,olderSons-olderPlanned+1);
                int missing=SandboxHouseholdLineagePolicy
                    .AdditionalOlderBrothers(
                        olderSons,existing,olderPlanned);
                Require(result,missing>=0 &&
                    olderPlanned+existing+missing==olderSons,
                    "computed brother count "+s);
                Require(result,
                    SandboxCulturalInheritancePolicy.Model(culture)!=
                    "Unestablished","missing culture model "+s);
                result.RegionCases++;
                if(landless)
                {
                    Require(result,
                        SandboxCustomLifeRolePolicy.NativeLandlessContract(
                            false,0),"landless requires native zero fiefs");
                    Require(result,
                        !SandboxCustomLifeRolePolicy.NativeLandlessContract(
                            true,0) &&
                        !SandboxCustomLifeRolePolicy.NativeLandlessContract(
                            false,1),"landless accepts false vassal");
                    result.LandlessCases++;
                }
                var claim=new SandboxSuccessionInterestState();
                claim.Record("cadet"+s,"father"+s,"mother"+s,
                    "birth_clan"+s,olderSons,true);
                claim.SetCulture(culture,rng.Next(0,7));
                Require(result,claim.PriorityScore==-1 &&
                    claim.CustomaryModel==
                        SandboxCulturalInheritancePolicy.Model(culture) &&
                    !SandboxCulturalInheritancePolicy
                        .CanTransferEstateWithoutNativeAction(),
                    "inheritance automatically transfers property");
                int originalFiefs=claim.BirthClanFiefsAtStart;
                var agenda=new SandboxCharacterAgendaState();
                string siblingId="sibling"+s;
                string goal=SandboxGeneratedCharacterGoalPolicy.Goal(
                    culture,"RecordedSibling","HouseholdSoldier",true,true);
                agenda.Register(siblingId,goal,"",
                    "Unverified","HouseholdSoldier",culture,"birth_clan"+s);
                Require(result,goal=="ProtectSharedHousehold",
                    "generated sibling motivation invalid");
                var service=new SandboxServiceCareerState();
                bool withPatron=role!="NobleCadet";
                if(withPatron)
                    service.Start(role,"lord"+s,"kingdom"+s);
                Require(result,withPatron?
                    service.IsConfigured &&
                    (role!="Recruit" ||
                     service.Status=="PendingEnlistment"):
                    !service.IsConfigured,
                    "fake service contract");
                int relation=10,actualFiefs=0;
                bool parentAlive=true,externalWasActive=false;
                for(int day=0;day<days;day++)
                {
                    if(day==Math.Min(80,days-1))
                    {
                        parentAlive=false;
                        string death=claim.Observe(false);
                        Require(result,death==
                            "parent_death_observed_native_ownership_unchanged" &&
                            claim.Status=="ReviewNativeSuccession",
                            "native death did not enter legal review");
                        result.ParentDeaths++;
                    }
                    else claim.Observe(parentAlive);
                    if(withPatron)
                    {
                        if(role=="Recruit" && day==20)
                        {
                            Require(result,service.ObserveExternal(
                                "lord"+s,"private","guard_duty",12,1,90,
                                true)=="external_enlistment_verified",
                                "SaS real enlistment not recorded");
                            result.ExternalEnlistments++;
                            externalWasActive=true;
                        }
                        if(role=="Recruit" && day>20 && day<180)
                            service.ObserveExternal(
                                "lord"+s,day>=90?"corporal":"private",
                                "guard_duty",12,day-19,
                                Math.Max(0,110-day),true);
                        if(role=="Recruit" && day==180)
                        {
                            Require(result,
                                service.ObserveExternalAbsent()==
                                "external_service_ended_observed",
                                "SaS discharge not noticed");
                            externalWasActive=false;
                        }
                        if(!externalWasActive)
                        {
                            string status=service.Observe(true,true,500);
                            if(role=="Recruit")
                                Require(result,service.DaysInService==
                                    (day<20?0:day<180?day-19:160),
                                    "pending recruit granted false tenure");
                            if(role=="HouseholdKnight")
                                Require(result,service.DaysInService==0,
                                    "personal knight contact earns fake salary");
                            if(role=="Soldier" && day>=90)
                                Require(result,service.Milestone==90,
                                    "native mercenary career lost tenure");
                            if(status=="active")
                                Require(result,role=="Soldier",
                                    "fake native service active");
                        }
                    }
                    bool sameRealParty=day%180<125;
                    int award=agenda.ObserveComradeship(siblingId,
                        sameRealParty,true,true,relation);
                    relation+=award;
                    result.ComradeshipAwards+=award;
                    Require(result,award<=1 && relation<=15 &&
                        agenda.Get(siblingId).ComradeshipAwards<=5,
                        "unbounded family relation reward");
                    Require(result,actualFiefs==0 &&
                        originalFiefs==claim.BirthClanFiefsAtStart,
                        "simulation fabricated property transfer");
                    if(day%60==0)
                    {
                        var savedAgenda=agenda.Serialize();
                        agenda=SandboxCharacterAgendaState.Parse(savedAgenda);
                        Require(result,agenda.Serialize()==savedAgenda,
                            "agenda save mismatch");
                        var savedClaim=claim.Serialize();
                        claim=SandboxSuccessionInterestState.Parse(savedClaim);
                        Require(result,claim.Serialize()==savedClaim &&
                            claim.CustomaryModel==
                                SandboxCulturalInheritancePolicy.Model(
                                    culture),"cultural claim reload mismatch");
                        if(withPatron)
                        {
                            var savedService=service.Serialize();
                            service=SandboxServiceCareerState.Parse(
                                savedService);
                            Require(result,service.Serialize()==savedService,
                                "service reload lost rank or contract");
                        }
                        result.SaveReloads++;
                    }
                }
                Require(result,actualFiefs==0,
                    "native fief mutated in model");
            }
            return result;
        }
    }
}
