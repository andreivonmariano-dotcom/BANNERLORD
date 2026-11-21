using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CalradiaUnified;

namespace CalradiaUnified.AuditSimulator
{
    /// <summary>
    /// Tests the same pure policies as the runtime source. It does not load
    /// TaleWorlds assemblies, game scenes, native Heroes or a campaign save.
    /// The reported "reloads" are serializer checkpoint round-trips only.
    /// </summary>
    internal static class Program
    {
        private static long checks, simulatedDays, checkpoints;
        private static long giftProofs, agencyRequests, recoveryReviews;
        private static long agePointerCases, navigationCases;
        private static long adverseSaveCases;
        private static readonly string[] Roles = {
            "King","Lord","Knight","Soldier","Admiral","Trader",
            "Outlaw","Beggar","Wanderer","Recruit",
            "HouseholdKnight","NobleCadet"
        };
        private const int SeedsPerRole=48;
        private const int Days=365;
        private const int Seed=20261121;

        private static void Require(bool ok,string context)
        {
            checks++;
            if(!ok)throw new InvalidOperationException(
                "AUDIT POLICY SIMULATION FAILURE: "+context);
        }

        private static void SimulateCreatorNavigation()
        {
            Require(CUNativeStatsNavigationPolicy.StepNumber(
                CUNativeStatsNavigationPolicy.Age)==1,
                "Age has first creator step");
            Require(CUNativeStatsNavigationPolicy.StepNumber(
                CUNativeStatsNavigationPolicy.Attributes)==2,
                "All Stats has second creator step");
            for(int n=0;n<10000;n++)
            {
                int age=CUNativeStatsNavigationPolicy.Age;
                int stats=CUNativeStatsNavigationPolicy.NextPage(age);
                Require(stats==CUNativeStatsNavigationPolicy.Attributes &&
                    CUNativeStatsNavigationPolicy.IsFinal(stats) &&
                    CUNativeStatsNavigationPolicy.BackPage(stats)==age &&
                    CUNativeStatsNavigationPolicy.BackToNativeNarrative(age) &&
                    !CUNativeStatsNavigationPolicy.BackToNativeNarrative(stats),
                    "Age / All Stats Back / Continue cycle "+n);
                Require(CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                    CUNativeHouseNavigationPolicy.ReviewStage,
                    n%2==0?CUNativeHouseNavigationPolicy.ClanStage:
                        CUNativeHouseNavigationPolicy.BannerStage,
                    true,true),
                    "native Review Back returns to House "+n);
                Require(!CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                    CUNativeHouseNavigationPolicy.ReviewStage,
                    CUNativeHouseNavigationPolicy.OptionsStage,true,true) &&
                    !CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                    CUNativeHouseNavigationPolicy.ReviewStage,
                    CUNativeHouseNavigationPolicy.ClanStage,true,false),
                    "Review Next or own House Back cannot reenter "+n);
                navigationCases++;
            }
            Require(SandboxStatsPagePolicy.PageCount(54)==9,
                "54 edited stats occupy nine logical groups");
            int seen=0;
            for(int p=0;p<9;p++)
            {
                Require(SandboxStatsPagePolicy.StartIndex(p,54)==p*6 &&
                    SandboxStatsPagePolicy.PageSize(p,54)==6,
                    "stat group "+p+" contains its own 6 values");
                seen+=SandboxStatsPagePolicy.PageSize(p,54);
            }
            Require(seen==54,"all 54 values represented once");
            Require(SandboxStatsPagePolicy.ClampPage(-999,54)==0 &&
                SandboxStatsPagePolicy.ClampPage(999,54)==8,
                "stat navigation clamps safely");
            for(int age=SandboxFamilyAgePolicy.MinimumPlayerAge;
                age<=SandboxFamilyAgePolicy.MaximumPlayerAge;age++)
            {
                const float left=125,trackWidth=478,handleWidth=38,
                    grabOffset=19;
                float x=left+grabOffset+(age-18)*(trackWidth-handleWidth)/62f;
                int observed=SandboxFamilyAgePolicy.AgeFromSliderPointer(
                    left,trackWidth,handleWidth,x,grabOffset);
                Require(observed==age,
                    "age input pointer maps to requested year "+age);
                agePointerCases++;
            }
            Require(SandboxFamilyAgePolicy.AgeFromSliderPointer(
                125,478,38,-999,19)==18 &&
                SandboxFamilyAgePolicy.AgeFromSliderPointer(
                125,478,38,99999,19)==80 &&
                SandboxFamilyAgePolicy.AgeFromSliderPointer(
                125,478,38,float.NaN,19)==18,
                "age pointer handles out of bounds and invalid input");
            for(int player=18;player<=80;player+=2)
                for(int spouse=18;spouse<=80;spouse+=2)
                {
                    int limit=SandboxFamilyAgePolicy.ChildAgeMaximum(
                        player,spouse);
                    Require(limit>=0 && limit<=55 &&
                        SandboxFamilyAgePolicy.CanChooseChildAge(
                            limit,player,spouse) &&
                        !SandboxFamilyAgePolicy.CanChooseChildAge(
                            limit+1,player,spouse),
                        "parental ages reject underaged parentage");
                }
        }

        private static void SimulateRetinueGrantGuard()
        {
            Require(SandboxStartContract.RetinueReadyForOtherGrants(
                "Native","native_companions_preserved"),
                "original native companions require exact preservation");
            for(int n=0;n<5000;n++)
            {
                int count=n%17;
                int dismissed=n%3;
                string receipt="officers="+count+
                    "|native_companions_dismissed="+dismissed+
                    "|same_culture=true";
                bool shouldPass=count>0 && count<=16 && dismissed<=1;
                Require(SandboxStartContract.RetinueReadyForOtherGrants(
                    "RoleBased",receipt)==shouldPass,
                    "complete native officer receipt rule "+n);
                Require(!SandboxStartContract.RetinueReadyForOtherGrants(
                    "RoleBased","officers="+count+
                    "|native_companions_dismissed="+dismissed),
                    "truncated retinue cannot authorize gifts "+n);
                Require(!SandboxStartContract.RetinueReadyForOtherGrants(
                    "RoleBased",receipt+"|unverified_extra"),
                    "retinue receipt cannot accept appended data "+n);
                Require(!SandboxStartContract.RetinueReadyForOtherGrants(
                    "FamilyOnly",receipt),
                    "non-family retinue cannot pose as family-only "+n);
            }
            Require(SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly",
                "officers=0|native_companions_dismissed=1|family_only=true"),
                "one original native companion was safely dismissed");
            Require(!SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly",
                "officers=0|native_companions_dismissed=2|family_only=true"),
                "multiple original companion dismissals refused");
        }

        private static void SimulateNativeEffects(int amount,int gold)
        {
            int payer=Math.Max(1000,gold);
            int receiver=17;
            Require(SandboxNativeEffectProof.GiftResult(
                payer,payer-amount,receiver,receiver+amount,amount)
                =="confirmed","both sides of true native gift");
            Require(SandboxNativeEffectProof.GiftResult(
                payer,payer-amount,receiver,receiver,amount)
                =="unbalanced_native_effect",
                "payer debit alone does not prove recipient credit");
            Require(SandboxNativeEffectProof.GiftResult(
                payer,payer,receiver,receiver+amount,amount)
                =="unbalanced_native_effect",
                "recipient credit alone does not prove payer debit");
            Require(SandboxNativeEffectProof.GiftResult(
                payer,payer,receiver,receiver,amount)
                =="no_native_effect",
                "unchanged native gift is no operation");
            giftProofs+=4;
        }

        private static void SimulateRole(string role,int run)
        {
            var random=new Random(Seed+run*131+Array.IndexOf(Roles,role)*10007);
            int initial=random.Next(100,20000),gold=initial;
            var economic=new SandboxRoleEconomyState();
            Require(economic.Start(role,gold,0),
                "role starts with observed gold "+role);
            Require(SandboxRoleEconomyPolicy.IncomeAuthority(role)!=
                    "UnrecognizedRole" &&
                    SandboxRoleEconomyPolicy.HouseholdDuty(role)!=
                    "UnrecognizedRole",
                "known income authority and household duty for "+role);
            var agency=new SandboxAgencyDecisionState();
            var family=new SandboxFamilyRecoveryState();
            string owner=role+"_"+run, npc1="kin_"+run,
                npc2="friend_"+run;
            family.RecordFailure(owner,role,"synthetic_partial_family",
                true,2,1,false,0,0);
            Require(family.NeedsReview,"failure is kept for manual review");
            for(int day=1;day<=Days;day++)
            {
                simulatedDays++;
                int next=Math.Max(0,gold+random.Next(-75,86));
                gold=next;
                string status=SandboxRoleEconomyPolicy.CareerEvidence(
                    role,false,false,role=="Soldier",
                    role=="Soldier",false);
                string observed=economic.Observe(day,gold,-1,status);
                Require(observed=="native_balance_unchanged" ||
                    observed=="native_balance_change_observed",
                    "daily economic model reads genuine-number input");
                Require(economic.ObservedCredits-economic.ObservedDebits==
                    gold-(long)initial,"observed credits/debits conservation");
                Require(economic.VerifiedExternalDailyWage==-1,
                    "no simulated native wages claimed");
                agency.Tick();
                Require(agency.Day==day,
                    "NPC request ledger advances once per modeled day");

                if(day%7==0)
                {
                    string hero=(day/7)%2==0?npc1:npc2;
                    string goal=day%3==0?"EstablishLivelihood":
                        day%3==1?"ProtectSharedHousehold":
                            "PreserveFamilyPosition";
                    string kind=SandboxAgencyDecisionPolicy.ChooseInitiative(
                        goal,true,true,random.Next(-30,65),
                        agency.For(hero)==null?0:agency.For(hero).RequestsMade,
                        agency.For(hero)==null?999:
                            day-agency.For(hero).LastDay,
                        random.Next(-2,3),random.Next(-2,3),
                        random.Next(-2,3),random.Next(0,3501));
                    if(kind.Length>0 && agency.Propose(hero,kind))
                        agencyRequests++;
                }
                int children=day>=120?2:day>=75?1:0;
                int siblings=day>=160?1:0;
                bool spouse=day>=30;
                if(day%13==0)
                {
                    Require(family.Observe(owner,spouse,children,siblings)!=
                        "no_matching_partial_start",
                        "native-family counts recorded for correct player");
                    Require(family.NeedsReview,
                        "count match never auto-certifies identity");
                    recoveryReviews++;
                }
                if(day%17==0)
                    SimulateNativeEffects(150,gold);
                if(day%29==0 || day==Days)
                {
                    string pack=economic.Serialize();
                    var newEconomy=SandboxRoleEconomyState.Parse(pack);
                    Require(newEconomy.Serialize()==pack &&
                        newEconomy.ObservedCredits==economic.ObservedCredits &&
                        newEconomy.ObservedDebits==economic.ObservedDebits &&
                        newEconomy.LastGold==gold,
                        "economic state checkpoint "+role+"/"+day);
                    economic=newEconomy;

                    string requests=agency.Serialize();
                    var newAgency=SandboxAgencyDecisionState.Parse(requests);
                    Require(newAgency.Serialize()==requests &&
                        newAgency.Records.All(x=>
                            x.RequestsMade<=SandboxAgencyDecisionPolicy
                                .MaxRequestsPerHero),
                        "NPC request save checkpoint "+role+"/"+day);
                    agency=newAgency;

                    string kin=family.Serialize();
                    var newFamily=SandboxFamilyRecoveryState.Parse(kin);
                    Require(newFamily.Serialize()==kin &&
                        newFamily.PlayerId==owner &&
                        newFamily.NeedsReview,
                        "family failure save checkpoint "+role+"/"+day);
                    family=newFamily;
                    checkpoints++;
                }
            }
            Require(family.Status==
                "NativeCountsMatchReviewStillRequired",
                "same native counts leave original family request under review");
            Require(family.Observe("wrong_player",true,6,4)==
                "no_matching_partial_start",
                "unrelated player cannot overwrite saved recovery");
            Require(economic.Describe().Contains(
                "No inferred salary, tax, rent"),
                "role UI description excludes speculative income claims");
            Require(agency.Records.All(x=>x.RequestsMade<=3) &&
                agency.Records.Count<=64,
                "NPC contact count and request limits remain bounded");
        }

        private static void SimulateAdverseSaveRecovery()
        {
            // Malformed snapshots and player identity mismatches must not
            // create NPC actions, alter family facts or invent wages.
            // These are state-string fixtures, never engine SaveGame files.
            for(int i=0;i<10000;i++)
            {
                int day=15+i%120;
                string hero="npc_"+i;
                string head="CU_AGENCY_1\n"+day+"|"+hero+
                    "|HouseholdAid|"+day;
                string row="\nR|"+hero+"|"+day+"|1";
                var restored=SandboxAgencyDecisionState.Parse(head+row);
                Require(restored.HasPending &&
                    restored.PendingHeroId==hero &&
                    restored.For(hero)!=null,
                    "valid request and quota survive restore "+i);
                Require(!SandboxAgencyDecisionState.Parse(head).HasPending,
                    "orphan pending request rejected "+i);
                Require(!SandboxAgencyDecisionState.Parse(head+
                    "\nR|wrong_person|"+day+"|1").HasPending,
                    "foreign quota cannot authorize request "+i);
                Require(!SandboxAgencyDecisionState.Parse(head+
                    "\nR|"+hero+"|"+(day-1)+"|1").HasPending,
                    "pending opening day must match quota "+i);
                Require(!SandboxAgencyDecisionState.Parse(head+
                    "\nR|"+hero+"|-2147483648|1").HasPending,
                    "negative saved cooldown rejected "+i);
                Require(!SandboxAgencyDecisionState.Parse(head+
                    "\nR|"+hero+"|"+day+"|0").HasPending,
                    "zero-request quota cannot authorize invitation "+i);
                Require(!SandboxAgencyDecisionState.Parse(
                    "CU_AGENCY_1\n"+(day+35)+"|"+hero+
                    "|HouseholdAid|"+day+row).HasPending,
                    "expired request cannot reopen after restore "+i);

                var failed=new SandboxFamilyRecoveryState();
                failed.RecordFailure("player_"+i,"Recruit",
                    "simulated_partial_family",true,2,1,false,1,0);
                string prior=failed.Serialize();
                Require(failed.Observe("other",true,2,1)==
                    "no_matching_partial_start" &&
                    failed.Serialize()==prior,
                    "different player cannot mutate recovery "+i);
                Require(failed.Observe("player_"+i,true,-1,1)==
                    "invalid_native_counts" &&
                    failed.Serialize()==prior,
                    "invalid family count leaves original fact intact "+i);
                Require(failed.Observe("player_"+i,true,1000,1000)==
                    "native_family_counts_changed" &&
                    failed.ChildrenObserved==99 &&
                    failed.SiblingsObserved==99 &&
                    failed.Status=="NativeCountsMatchReviewStillRequired",
                    "oversized telemetry stays bounded and under review "+i);
                Require(failed.Observe("player_"+i,true,1000,1000)=="same",
                    "same bounded observation does not fabricate moves "+i);
                string normalized=failed.Serialize();
                Require(SandboxFamilyRecoveryState.Parse(normalized)
                    .Serialize()==normalized,
                    "bounded family dossier survives parse "+i);
                Require(!SandboxFamilyRecoveryState.Parse(
                    normalized.Replace(
                    "NativeCountsMatchReviewStillRequired",
                    "TamperedConsent")).NeedsReview,
                    "unrecognized family status never trusted "+i);

                var money=new SandboxRoleEconomyState();
                Require(money.Start("Trader",100,0) &&
                    money.Observe(1,96,-1,"NativeCommerce")==
                        "native_balance_change_observed",
                    "valid native balance observation "+i);
                string validMoney=money.Serialize();
                Require(SandboxRoleEconomyState.Parse(validMoney)
                    .Serialize()==validMoney,
                    "economic ledger survives checkpoint "+i);
                Require(!SandboxRoleEconomyState.Parse(
                    validMoney.Replace("|96|-4|","|-1|-4|")).Started,
                    "negative native balance rejected on reload "+i);
                adverseSaveCases++;
            }

            var emptyFamily=new SandboxFamilyRecoveryState();
            emptyFamily.RecordFailure("player_empty","Wanderer",
                "post_family_failed",false,0,0,false,0,0);
            Require(emptyFamily.Observe("player_empty",false,0,0)==
                "native_family_counts_changed" &&
                emptyFamily.Status=="NativeCountsMatchReviewStillRequired",
                "status transition is recorded even with unchanged counts");
            var nearMaxDay=new SandboxAgencyDecisionState{
                Day=Int32.MaxValue};
            Require(nearMaxDay.Propose("late_campaign_npc","Recognition") &&
                SandboxAgencyDecisionState.Parse(
                    nearMaxDay.Serialize()).HasPending,
                "cooldown cannot overflow at the largest campaign day");
        }

        private static int Main(string[] args)
        {
            try
            {
                SimulateCreatorNavigation();
                SimulateRetinueGrantGuard();
                SimulateAdverseSaveRecovery();
                foreach(string role in Roles)
                    for(int run=0;run<SeedsPerRole;run++)
                        SimulateRole(role,run);
                var report=new {
                    status="PASS",
                    model="Calradia Unified current-feature QA policy stress",
                    gameVersionTarget="Bannerlord 1.5.7.125802",
                    engineExecuted=false,
                    nativeUiRendered=false,
                    nativeClickOrDragExecuted=false,
                    gameSaveReloadExecuted=false,
                    realNativeTransactionsExecuted=false,
                    simulatedCreatorNavigationCases=navigationCases,
                    simulatedAgePointerCases=agePointerCases,
                    roles=Roles,
                    seedsPerRole=SeedsPerRole,
                    modeledDaysPerScenario=Days,
                    modeledDays=simulatedDays,
                    policyAssertions=checks,
                    serializedCheckpointRoundTrips=checkpoints,
                    syntheticDoubleEntryGiftCases=giftProofs,
                    modeledNpcRequests=agencyRequests,
                    modeledFamilyCountReviews=recoveryReviews,
                    simulatedAdverseSavedStateCases=adverseSaveCases,
                    liveGameStatus="NOT RUN"
                };
                string json=JsonSerializer.Serialize(report,
                    new JsonSerializerOptions{WriteIndented=true});
                string output=null;
                for(int a=0;a<args.Length;a++)
                    if(args[a]=="--json" && a+1<args.Length)
                        output=args[a+1];
                if(!string.IsNullOrEmpty(output))
                {
                    string full=Path.GetFullPath(output);
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(full));
                    File.WriteAllText(full,json);
                }
                Console.WriteLine("AUDIT POLICY SIMULATION PASS | modeled_days="+
                    simulatedDays+" | assertions="+checks+" | checkpoints="+
                    checkpoints+" | navigation="+navigationCases);
                Console.WriteLine("MODEL ONLY: no Bannerlord rendering, "+
                    "mouse clicks, native Heroes or real saves executed.");
                return 0;
            }
            catch(Exception error)
            {
                Console.Error.WriteLine(error.ToString());
                return 1;
            }
        }
    }
}
