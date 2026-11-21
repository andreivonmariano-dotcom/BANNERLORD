using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Deterministic state-only stress of NPC agency, UI petition gates and
    /// native transfer safeguards. Does not simulate real Bannerlord Heroes.
    /// </summary>
    public static class SandboxLivingAffairsSimulation
    {
        public sealed class Result
        {
            public int Seeds,Days,Checks,Failures,Requests,Accepted,
                Declined,Expired,AuthorizedClaims,ContestedClaims,
                RefusedClaims,Reloads,NativeTransferCalls,
                UnguardedTransfers;
            public string FirstFailure="";
        }
        private static readonly string[] Cultures={
            "vlandia","sturgia","battania","empire",
            "aserai","khuzait"
        };
        private static void Check(Result r,bool okay,string message)
        {
            r.Checks++;
            if(okay)return;
            r.Failures++;
            if(r.FirstFailure.Length==0)r.FirstFailure=message;
        }
        public static Result Run(int seeds,int days)
        {
            var r=new Result{Seeds=seeds,Days=days};
            if(seeds<=0 || days<=0)return r;
            for(int seed=0;seed<seeds;seed++)
            {
                var rng=new Random(26000+seed*113);
                string culture=Cultures[seed%Cultures.Length];
                string hero="follower_"+seed;
                string goal=seed%3==0?"ProtectSharedHousehold":
                    seed%3==1?"BuildTradingContacts":
                    "PreserveFamilyPosition";
                var requests=new SandboxAgencyDecisionState();
                var claim=new SandboxInheritanceNegotiationState();
                int gold=1500,earnedNative=0,aidTotal=0;
                int relation=seed%2==0?70:20;
                int olderSons=seed%3;
                bool fatherDeceased=false;
                string model=SandboxCulturalInheritancePolicy.Model(
                    culture);
                for(int d=0;d<days;d++)
                {
                    requests.Tick();
                    if(d==90)fatherDeceased=true;
                    if(requests.Day>=15 &&
                        requests.Day%5==0 && !requests.HasPending)
                    {
                        var past=requests.For(hero);
                        string type=SandboxAgencyDecisionPolicy.Choose(
                            goal,true,true,relation,
                            past==null?0:past.RequestsMade,
                            past==null?1000:
                                requests.Day-past.LastDay);
                        if(type.Length>0 && requests.Propose(hero,type))
                            r.Requests++;
                    }
                    if(requests.HasPending && d%11==0)
                    {
                        bool accept=rng.Next(0,4)!=0;
                        string type=requests.PendingKind;
                        int amount=SandboxAgencyDecisionPolicy.Amount(type);
                        bool transfer=SandboxAgencyDecisionPolicy
                            .NativeTransferPermitted(type,accept,gold,true);
                        if(transfer)
                        {
                            gold-=amount;
                            aidTotal+=amount;
                        }
                        else Check(r,!accept || amount==0 ||
                            gold<amount,"unjustified NPC gift");
                        int delta=SandboxAgencyDecisionPolicy
                            .RelationDelta(type,accept);
                        relation=Math.Max(-100,Math.Min(100,
                            relation+delta));
                        requests.Dismiss(accept?"Accepted":"Declined");
                        if(accept)r.Accepted++;
                        else r.Declined++;
                    }
                    Check(r,gold==1500+earnedNative-aidTotal,
                        "gold duplicated by request");
                    Check(r,requests.Records.Count<=64 &&
                        (!requests.HasPending ||
                         SandboxAgencyDecisionPolicy.Amount(
                            requests.PendingKind)>=0),
                        "agency overflow");
                    if(d==Math.Min(days-1,120))
                    {
                        // Culture-specific consent must differ, and a
                        // deceased parent alone never transfers property.
                        int needed=SandboxInheritanceNegotiationPolicy
                            .ConsentThreshold(culture,olderSons);
                        string outcome=SandboxInheritanceNegotiationPolicy
                            .Evaluate(true,fatherDeceased,true,true,true,
                                2,olderSons,relation,culture);
                        string status=claim.Petition(outcome,
                            "castle_secondary","clan_head",
                            "birth_house",model,requests.Day);
                        if(status=="ClanHeadConsents")r.AuthorizedClaims++;
                        else if(status=="ClaimContested")r.ContestedClaims++;
                        else r.RefusedClaims++;
                        if(status=="ClaimContested")
                        {
                            int fee=SandboxInheritanceNegotiationPolicy
                                .FamilySettlementContribution(culture);
                            bool eligible=SandboxInheritanceNegotiationPolicy
                                .MediationGate(status,claim.Mediations,true,
                                    true,true,gold,culture);
                            Check(r,fee>0 && eligible,
                                "cultural mediation not available");
                            if(eligible)
                            {
                                gold-=fee;aidTotal+=fee;
                                Check(r,claim.RecordMediation() &&
                                    claim.Mediations==1,
                                    "mediation payment not recorded");
                            }
                            if(seed%4==0)
                            {
                                Check(r,claim.Withdraw()=="ClaimWithdrawn" &&
                                    !claim.Authorized &&
                                    claim.Petition("ClanHeadConsents",
                                        "castle_secondary","clan_head",
                                        "birth_house",model,
                                        requests.Day)=="PetitionLimitReached",
                                    "withdrawn dispute reopens");
                            }
                        }
                        Check(r,status=="ClanHeadConsents" ==
                            (fatherDeceased && relation>=needed),
                            "regional consent threshold mismatch");
                        Check(r,!SandboxInheritanceNegotiationPolicy
                            .TransferGate(false,claim.Authorized,
                                "birth_house","birth_house",
                                true,true,2),"implicit grant allowed");
                        Check(r,!SandboxInheritanceNegotiationPolicy
                            .TransferGate(true,claim.Authorized,
                                "birth_house","other_clan",
                                true,true,2),"owner change ignored");
                        Check(r,!SandboxInheritanceNegotiationPolicy
                            .TransferGate(true,claim.Authorized,
                                "birth_house","birth_house",
                                false,true,2),"outside kingdom grant");
                        Check(r,!SandboxInheritanceNegotiationPolicy
                            .TransferGate(true,claim.Authorized,
                                "birth_house","birth_house",
                                true,false,2),"living parent grant");
                        Check(r,!SandboxInheritanceNegotiationPolicy
                            .TransferGate(true,claim.Authorized,
                                "birth_house","birth_house",
                                true,true,1),"single estate taken");
                        // The simulation does not call the native owner
                        // action. Only a real game action can complete it.
                        Check(r,claim.Status!="NativeGrantCompleted",
                            "fictional estate transfer");
                    }
                    if(d%47==0)
                    {
                        string recorded=requests.Serialize();
                        requests=SandboxAgencyDecisionState.Parse(recorded);
                        Check(r,requests.Serialize()==recorded,
                            "request save mismatch");
                        string state=claim.Serialize();
                        claim=SandboxInheritanceNegotiationState.Parse(state);
                        Check(r,claim.Serialize()==state,
                            "petition save mismatch");
                        r.Reloads++;
                    }
                }
                Check(r,claim.Status!="NativeGrantCompleted" &&
                    r.NativeTransferCalls==0,
                    "estate ownership altered without native action");
                Check(r,aidTotal>=0 && gold>=0,
                    "negative aid or treasury");
            }
            return r;
        }
    }
}
