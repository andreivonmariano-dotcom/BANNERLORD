using System;
using System.Linq;
using CalradiaUnified;

internal static class Program
{
    private static int passed;
    private static void Check(string name, bool ok)
    {
        if (!ok) throw new Exception("FAILED: " + name);
        passed++;
        Console.WriteLine("PASS " + name);
    }
    private static string SandboxNativeTypeForPolicy(string role)
    {
        return SandboxRoleConsistencyPolicy.NativeType(role);
    }

    private static bool ResetAndCheck(SandboxOriginProfile plan)
    {
        plan.ResetForNewCampaign();
        return true;
    }
    private static void Main()
    {
        // v0.69h source/model matrix audit. A passing assertion does not
        // prove a Bannerlord campaign start or saved native Hero state.
        var nativeRoles=new [] {
            "Wanderer","Outlaw","Mercenary","Recruit","Soldier",
            "HouseholdKnight","Knight","NobleCadet","Lord","King",
            "Admiral","Trader","Beggar"
        };
        var expectedTypes=new [] {
            "default","outlaw","mercenary","default","mercenary",
            "default","vassal","default","vassal","king",
            "fleetadmiral","trader","beggar"
        };
        Check("13 compatible roles retain exact native start IDs",
            SandboxOriginProfile.Roles.Length==13 &&
            SandboxOriginProfile.Roles.Distinct().Count()==13);
        for(int i=0;i<nativeRoles.Length;i++)
            Check("native role matrix "+nativeRoles[i],
                SandboxOriginProfile.Roles.Contains(nativeRoles[i]) &&
                SandboxNativeTypeForPolicy(nativeRoles[i])==
                    expectedTypes[i]);

        Check("missing changed-option native type is safe only for default",
            SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default","",false,false,0) &&
            SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default",null,false,false,0) &&
            !SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default","mercenary",false,false,0) &&
            !SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default","",true,false,0) &&
            !SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "default","",false,false,1) &&
            !SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "vassal","",false,false,0) &&
            !SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                "king","",false,false,0));
        Check("native status validation accepts empty default only if landless",
            SandboxStartContract.Validate("default","",
                "vlandia","",false,0,false,false,0,0).Length==0 &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "HouseholdKnight","",false,false,0)=="" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "NobleCadet",null,false,false,0)=="" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Wanderer","",false,false,0)=="" &&
            SandboxStartContract.Validate("default","",
                "vlandia","vlandia",false,0,
                false,false,0,0).Contains("native_start_type_mismatch") &&
            SandboxStartContract.Validate("default","",
                "vlandia","",false,1,
                false,false,0,0).Contains("native_start_type_mismatch"));
        Check("nondefault native starts still need exact source IDs",
            SandboxStartContract.Validate("vassal","default",
                "vlandia","vlandia",false,1,
                false,false,0,0).Contains("native_start_type_mismatch") &&
            SandboxStartContract.Validate("king","",
                "vlandia","vlandia",true,1,
                false,false,0,0).Contains("native_start_type_mismatch"));
        Check("all six world scenario names are unique and parseable",
            SandboxOriginProfile.Scenarios.Length==6 &&
            SandboxOriginProfile.Scenarios.Distinct().Count()==6);
        foreach(string scenario in SandboxOriginProfile.Scenarios)
        {
            var p=new SandboxOriginProfile();
            Check("world scenario plan parses "+scenario,
                p.TrySet("StartingScenario",scenario) &&
                SandboxOriginProfile.Parse(p.Serialize())
                    .StartingScenario==scenario);
        }
        Check("all nine noble birth positions are distinct",
            SandboxNobleStartPolicy.BirthPositions.Length==9 &&
            SandboxNobleStartPolicy.BirthPositions.Distinct().Count()==9);
        Check("starting kingdoms present and unique",
            SandboxOriginProfile.KingdomIds.Length==9 &&
            SandboxOriginProfile.KingdomIds.Distinct().Count()==9);
        Check("four explicit fief modes include native default",
            SandboxFiefPolicy.Modes.Length==4 &&
            SandboxFiefPolicy.Modes.Contains("Native") &&
            SandboxFiefPolicy.Modes.Contains("Specific") &&
            SandboxFiefPolicy.ValidateRequest(
                "HouseholdKnight","Castle","",true)==
                    "fief_role_ineligible" &&
            SandboxFiefPolicy.ValidateRequest(
                "Lord","Specific","",true)=="fief_id_required");
        Check("retinue modes include compatibility alias",
            SandboxNobleStartPolicy.RetinueModes.Length==4 &&
            SandboxNobleStartPolicy.RetinueModes.Contains("Native") &&
            SandboxNobleStartPolicy.RetinueModes.Contains("FamilyOnly"));
        Check("native gift proof confirms BOTH payer debit and recipient credit",
            SandboxNativeEffectProof.GiftResult(500,350,20,170,150)==
                "confirmed" &&
            SandboxNativeEffectProof.GiftResult(500,350,20,20,150)==
                "unbalanced_native_effect" &&
            SandboxNativeEffectProof.GiftResult(500,500,20,170,150)==
                "unbalanced_native_effect" &&
            SandboxNativeEffectProof.GiftResult(500,500,20,20,150)==
                "no_native_effect" &&
            SandboxNativeEffectProof.GiftResult(100,100,0,0,-1)==
                "invalid" &&
            SandboxNativeEffectProof.AnyPayerDebit(400,399));
        Check("relation memories require actual verified game score changes",
            SandboxNativeEffectProof.RelationConfirmed(20,22,2) &&
            SandboxNativeEffectProof.RelationConfirmed(99,100,2) &&
            SandboxNativeEffectProof.RelationConfirmed(-99,-100,-2) &&
            !SandboxNativeEffectProof.RelationConfirmed(20,20,2) &&
            !SandboxNativeEffectProof.RelationConfirmed(20,21,2) &&
            SandboxNativeEffectProof.ObservedDelta(20,21)==1 &&
            SandboxNativeEffectProof.RelationConfirmed(20,20,0));
        var failedFamily=new SandboxFamilyRecoveryState();
        failedFamily.RecordFailure("player","NobleCadet",
            "family_creation_incomplete",true,2,1,true,1,0);
        string savedRecovery=failedFamily.Serialize();
        Check("save-backed failed family preserves original failure evidence",
            failedFamily.NeedsReview &&
            failedFamily.Status=="PartialStartNeedsReview" &&
            SandboxFamilyRecoveryState.Parse(savedRecovery).Serialize()==
                savedRecovery &&
            !SandboxFamilyRecoveryState.Parse(
                "CU_FAMILY_RECOVERY_1").NeedsReview);
        Check("recovery recount is read-only and never an auto-replay",
            failedFamily.Observe("another_hero",true,4,2)==
                "no_matching_partial_start" &&
            failedFamily.Observe("player",true,2,1)==
                "native_family_counts_changed" &&
            failedFamily.Status==
                "NativeCountsMatchReviewStillRequired" &&
            failedFamily.Describe().Contains("read-only") &&
            failedFamily.Observe("player",true,2,1)=="same" &&
            SandboxFamilyRecoveryState.Parse(failedFamily.Serialize())
                .Status=="NativeCountsMatchReviewStillRequired");
        var wandererDefault=new SandboxOriginProfile();
        Check("legacy Wanderer retains House path unless explicitly migrated",
            wandererDefault.Role=="Wanderer" &&
            !wandererDefault.WandererNoHouseStart &&
            !wandererDefault.WandererCompanionEnabled);
        var soloWanderer=new SandboxOriginProfile();
        Check("new Wanderer has no House and is independent",
            soloWanderer.TrySet("WandererNoHouseStart","true") &&
            soloWanderer.TrySet("RetinueMode","FamilyOnly") &&
            soloWanderer.IsValid(30,out _) &&
            !soloWanderer.SpouseEnabled &&
            soloWanderer.ChildCount==0 &&
            soloWanderer.SiblingCount==0);
        Check("Wanderer companion choices round-trip exactly",
            soloWanderer.TrySet("WandererCompanionEnabled","true") &&
            soloWanderer.TrySet("WandererCompanionName","Elira") &&
            soloWanderer.TrySet("WandererCompanionAge","29") &&
            soloWanderer.TrySet("WandererCompanionGender","Female") &&
            soloWanderer.TrySet("WandererCompanionSpecialty","Healer") &&
            soloWanderer.TrySet("WandererCompanionSkill","185") &&
            soloWanderer.IsValid(30,out _) &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionEnabled &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionName=="Elira" &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionAge==29 &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionGender=="Female" &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionSpecialty=="Healer" &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionSkill==185);
        Check("new Wanderer rejects House family, invalid skills and titles",
            !soloWanderer.TrySet("WandererCompanionSkill","221") &&
            !soloWanderer.TrySet("WandererCompanionSpecialty","RoyalAdvisor") &&
            !soloWanderer.TrySet("WandererCompanionName","Companion 1") &&
            soloWanderer.TrySet("SpouseEnabled","true") &&
            !soloWanderer.IsValid(30,out _) &&
            soloWanderer.TrySet("SpouseEnabled","false") &&
            soloWanderer.TrySet("NobleBirthPosition","YoungerBrotherOfLord") &&
            !soloWanderer.IsValid(30,out _) &&
            soloWanderer.TrySet("NobleBirthPosition","Independent"));
        Check("Wanderer full companion native stats survive profile save",
            soloWanderer.TrySet(
                "WandererCompanionStat.Skill.Medicine","180") &&
            soloWanderer.TrySet(
                "WandererCompanionStat.Attribute.Intelligence","8") &&
            soloWanderer.TrySet(
                "WandererCompanionStat.Focus.Medicine","5") &&
            soloWanderer.TrySet(
                "WandererCompanionStat.Level.Character","25") &&
            soloWanderer.IsValid(30,out _) &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionOverrides.Count==4 &&
            SandboxOriginProfile.Parse(soloWanderer.Serialize())
                .WandererCompanionOverrides["Skill.Medicine"]==180 &&
            !soloWanderer.TrySet(
                "WandererCompanionStat.Skill.Medicine","400"));
        soloWanderer.ResetForNewCampaign();
        Check("new-game reset clears single-companion and no-House flags",
            !soloWanderer.WandererNoHouseStart &&
            !soloWanderer.WandererCompanionEnabled &&
            soloWanderer.WandererCompanionName=="Taran" &&
            soloWanderer.WandererCompanionOverrides.Count==0 &&
            soloWanderer.RetinueMode=="Native");
        var guidedOutlaw=new SandboxOriginProfile();
        Check("new Outlaw keeps native bandit start without old save migration",
            guidedOutlaw.TrySet("Role","Outlaw") &&
            guidedOutlaw.TrySet("OutlawGuidedStart","true") &&
            guidedOutlaw.TrySet("RetinueMode","RoleBased") &&
            guidedOutlaw.TrySet("FamilyHomeMode","None") &&
            guidedOutlaw.IsValid(30,out _) &&
            SandboxRoleConsistencyPolicy.NativeType("Outlaw")=="outlaw" &&
            SandboxNobleStartPolicy.RetinueOffices("Outlaw",false)
                .SequenceEqual(new[]{"SecondInCommand"}) &&
            SandboxNobleStartPolicy.CultureTitle(
                "vlandia","SecondInCommand")=="Band Lieutenant");
        Check("guided Outlaw household and tenure preserve landlessness",
            guidedOutlaw.TrySet("SpouseEnabled","true") &&
            guidedOutlaw.IsValid(30,out _) &&
            guidedOutlaw.TrySet("FamilyHomeMode","OwnedFief") &&
            !guidedOutlaw.IsValid(30,out _) &&
            guidedOutlaw.TrySet("FamilyHomeMode","None") &&
            guidedOutlaw.TrySet("Troops","6") &&
            !guidedOutlaw.IsValid(30,out _) &&
            guidedOutlaw.TrySet("Troops","0") &&
            guidedOutlaw.IsValid(30,out _));
        Check("six native bandits and cultural bandit mapping",
            SandboxOutlawBandPolicy.TargetBandits==6 &&
            SandboxOutlawBandPolicy.MissingBandits(0)==6 &&
            SandboxOutlawBandPolicy.MissingBandits(3)==3 &&
            SandboxOutlawBandPolicy.MissingBandits(6)==0 &&
            SandboxOutlawBandPolicy.MissingBandits(13)==-1 &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("vlandia")==
                "mountain_bandits" &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("battania")==
                "forest_bandits" &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("sturgia")==
                "sea_raiders" &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("khuzait")==
                "steppe_bandits" &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("aserai")==
                "desert_bandits" &&
            SandboxOutlawBandPolicy.PreferredBanditCulture("empire")==
                "looters");
        Check("bandit payroll is fourteen real native wage days",
            SandboxOutlawBandPolicy.PayrollDays==14 &&
            SandboxOutlawBandPolicy.StartingGrain==12 &&
            SandboxOutlawBandPolicy.RequiredStartingGold(0)==750 &&
            SandboxOutlawBandPolicy.RequiredStartingGold(50)==900 &&
            SandboxOutlawBandPolicy.RequiredStartingGold(250)==3700 &&
            SandboxOutlawBandPolicy.RequiredStartingGold(251)==-1);
        Check("small band respects actual native roster and party limit",
            SandboxOutlawBandPolicy.ValidateBeforeGrant(0,0,25,40)=="" &&
            SandboxOutlawBandPolicy.ValidateBeforeGrant(6,6,25,40)=="" &&
            SandboxOutlawBandPolicy.ValidateBeforeGrant(7,6,25,40)=="" &&
            SandboxOutlawBandPolicy.ValidateBeforeGrant(9,0,25,40)==
                "oversized_native_party" &&
            SandboxOutlawBandPolicy.ValidateBeforeGrant(0,0,7,40)==
                "party_size_limit_too_small" &&
            SandboxOutlawBandPolicy.ValidateBeforeGrant(0,0,25,251)==
                "native_wage_outside_starter_band");
        Check("new guided Outlaw profile roundtrips without old save leak",
            SandboxOriginProfile.Parse(guidedOutlaw.Serialize())
                .OutlawGuidedStart);
        guidedOutlaw.ResetForNewCampaign();
        Check("Outlaw starter grants do not bleed into next campaign",
            !guidedOutlaw.OutlawGuidedStart);
        var guidedMercenary=new SandboxOriginProfile();
        Check("Mercenary is distinct from the legacy Soldier start",
            guidedMercenary.TrySet("Role","Mercenary") &&
            guidedMercenary.TrySet("MercenaryGuidedStart","true") &&
            guidedMercenary.TrySet("StartingKingdomId","vlandia") &&
            guidedMercenary.TrySet("RetinueMode","FamilyOnly") &&
            guidedMercenary.TrySet("FamilyHomeMode","None") &&
            guidedMercenary.IsValid(30,out _) &&
            SandboxRoleConsistencyPolicy.NativeType("Mercenary")==
                "mercenary" &&
            SandboxRoleConsistencyPolicy.NativeType("Soldier")==
                "mercenary" &&
            !SandboxServicePolicy.NeedsPatron("Mercenary") &&
            SandboxServicePolicy.DefaultRetinue("Mercenary")==
                "FamilyOnly" &&
            SandboxStartingLifePolicy.Station(
                "Mercenary","Independent")=="MercenaryCompany");
        Check("new mercenary role requires native paid-company standing",
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",true,true,true,
                false,0)=="" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",false,true,true,
                false,0)=="native_mercenary_contract_missing" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","sturgia",true,true,true,
                false,0)=="native_contract_kingdom_mismatch" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","",true,true,true,
                false,0)=="native_kingdom_missing" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","","vlandia",true,true,true,
                false,0)=="contract_kingdom_missing");
        Check("native mercenary cannot claim lordship or foreign command",
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",true,true,true,
                false,1)=="mercenary_land_or_rulership_invalid" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",true,true,true,
                true,0)=="mercenary_land_or_rulership_invalid" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",true,false,true,
                false,0)=="player_does_not_lead_company_clan" &&
            SandboxMercenaryStartPolicy.Validate(
                "Mercenary","vlandia","vlandia",true,true,false,
                false,0)=="player_not_leading_independent_party" &&
            SandboxMercenaryStartPolicy.Validate(
                "Soldier","vlandia","vlandia",true,true,true,
                false,0)=="not_guided_mercenary");
        Check("mercenary spouse remains optional without an owned home",
            guidedMercenary.TrySet("SpouseEnabled","true") &&
            guidedMercenary.IsValid(40,out _) &&
            guidedMercenary.TrySet("FamilyHomeMode","OwnedFief") &&
            !guidedMercenary.IsValid(40,out _) &&
            guidedMercenary.TrySet("FamilyHomeMode","None") &&
            guidedMercenary.TrySet("SpouseEnabled","false") &&
            guidedMercenary.IsValid(40,out _));
        Check("mercenary rejects free troops, personal stipend and fief",
            guidedMercenary.TrySet("Troops","4") &&
            !guidedMercenary.IsValid(30,out _) &&
            guidedMercenary.TrySet("Troops","0") &&
            guidedMercenary.TrySet("Gold","2000") &&
            !guidedMercenary.IsValid(30,out _) &&
            guidedMercenary.TrySet("Gold","-1") &&
            guidedMercenary.TrySet("Influence","10") &&
            !guidedMercenary.IsValid(30,out _) &&
            guidedMercenary.TrySet("Influence","-1") &&
            guidedMercenary.TrySet("StartingFiefMode","Castle") &&
            !guidedMercenary.IsValid(30,out _) &&
            guidedMercenary.TrySet("StartingFiefMode","Native") &&
            guidedMercenary.IsValid(30,out _));
        Check("mercenary profile flag persists without changing native role",
            SandboxOriginProfile.Parse(guidedMercenary.Serialize())
                .MercenaryGuidedStart &&
            SandboxOriginProfile.Parse(guidedMercenary.Serialize())
                .Role=="Mercenary" &&
            SandboxOriginProfile.Parse(guidedMercenary.Serialize())
                .StartingKingdomId=="vlandia");
        guidedMercenary.ResetForNewCampaign();
        Check("new-game reset clears mercenary grant flag",
            !guidedMercenary.MercenaryGuidedStart &&
            !guidedMercenary.OutlawGuidedStart &&
            !guidedMercenary.WandererNoHouseStart);
        Check("retinue receipt requires complete native success fields",
            SandboxStartContract.RetinueReadyForOtherGrants(
                "Native","native_companions_preserved") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly",
                "officers=0|native_companions_dismissed=0|family_only=true") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly",
                "officers=0|native_companions_dismissed=1|family_only=true") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased",
                "officers=2|native_companions_dismissed=0|same_culture=true") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "ReplaceNative",
                "officers=1|native_companions_dismissed=1|same_culture=true") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased",
                "officers=2|native_companions_dismissed=2|same_culture=true") &&
            SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly",
                "officers=0|native_companions_dismissed=2|family_only=true"));
        Check("retinue grant blocks malformed and incomplete receipts",
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly","partial:native_companions_dismissed=1") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","refused:no_retinue_for_role") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","officers=0|native_companions_dismissed=0|same_culture=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","officers=2|native_companions_dismissed=0") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","officers=foo|native_companions_dismissed=0|same_culture=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","officers=2|native_companions_dismissed=-1|same_culture=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "RoleBased","officers=2|native_companions_dismissed=17|same_culture=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly","officers=0|native_companions_dismissed=0|same_culture=false") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly","officers=1|native_companions_dismissed=0|family_only=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "FamilyOnly","officers=0|native_companions_dismissed=0|family_only=true|extra") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "Native","officers=2|native_companions_dismissed=0|same_culture=true") &&
            !SandboxStartContract.RetinueReadyForOtherGrants(
                "UnsupportedMode","native_companions_preserved"));
        var nativeEconomyRoles=SandboxOriginProfile.Roles;
        Check("all 13 compatible roles declare accurate income authority and duty",
            nativeEconomyRoles.Length==13 &&
            nativeEconomyRoles.All(role=>
                SandboxRoleEconomyPolicy.IncomeAuthority(role)!=
                    "UnrecognizedRole" &&
                SandboxRoleEconomyPolicy.HouseholdDuty(role)!=
                    "UnrecognizedRole") &&
            SandboxRoleEconomyPolicy.IncomeAuthority("Recruit")==
                "NoWageUntilVerifiedEnlistment" &&
            SandboxRoleEconomyPolicy.IncomeAuthority("NobleCadet")==
                "BirthKinNoAutomaticEstateIncome" &&
            SandboxRoleEconomyPolicy.IncomeAuthority("Soldier")==
                "NativeMercenaryContract" &&
            SandboxRoleEconomyPolicy.IncomeAuthority("Mercenary")==
                "NativeMercenaryContract" &&
            SandboxRoleEconomyPolicy.CareerEvidence("Mercenary",
                false,false,true,true,false)=="NativeMercenaryVerified");
        Check("career evidence distinguishes ruler, vassal and enlistment",
            SandboxRoleEconomyPolicy.CareerEvidence("King",
                false,false,false,true,true)==
                    "NativeRulerStatusVerified" &&
            SandboxRoleEconomyPolicy.CareerEvidence("Knight",
                false,true,false,true,false)==
                    "NativeNobleServiceVerified" &&
            SandboxRoleEconomyPolicy.CareerEvidence("Recruit",
                false,false,false,false,false)==
                    "EnlistmentNotVerified" &&
            SandboxRoleEconomyPolicy.CareerEvidence("Recruit",
                true,false,false,false,false)==
                    "ExternalEnlistmentVerified" &&
            SandboxRoleEconomyPolicy.CareerEvidence("Soldier",
                false,false,true,true,false)==
                    "NativeMercenaryVerified" &&
            SandboxRoleEconomyPolicy.CareerEvidence("HouseholdKnight",
                false,false,false,false,false)==
                    "PersonalPatronNotFormalEmployment");
        var observedEconomy=new SandboxRoleEconomyState();
        Check("native personal economy starts without assigning a wage",
            observedEconomy.Start("Recruit",500,10) &&
            observedEconomy.VerifiedExternalDailyWage==-1 &&
            !observedEconomy.Start("King",2000,11));
        Check("native gold debits and credits survive save without wage fiction",
            observedEconomy.Observe(11,480,-1,
                "EnlistmentNotVerified")=="native_balance_change_observed" &&
            observedEconomy.ObservedDebits==20 &&
            observedEconomy.ObservedCredits==0 &&
            observedEconomy.Observe(11,480,-1,
                "EnlistmentNotVerified")=="not_new_native_day" &&
            observedEconomy.Observe(12,580,12,
                "ExternalEnlistmentVerified")==
                    "native_balance_change_observed" &&
            observedEconomy.ObservedCredits==100 &&
            observedEconomy.VerifiedExternalDailyWage==12 &&
            observedEconomy.Describe().Contains(
                "No inferred salary, tax, rent") &&
            SandboxRoleEconomyState.Parse(observedEconomy.Serialize())
                .Serialize()==observedEconomy.Serialize());
        Check("NPC personal initiative responds to real traits and resources",
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "ProtectSharedHousehold",true,true,30,0,100,
                1,0,0,100)=="HouseholdAid" &&
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "ProtectSharedHousehold",true,true,30,0,100,
                1,0,0,2000)=="Recognition" &&
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "PreserveFamilyPosition",true,true,5,0,100,
                0,0,2,100)=="" &&
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "EstablishLivelihood",true,true,5,0,100,
                0,2,0,700)=="" &&
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "EstablishLivelihood",true,true,5,0,100,
                0,0,0,100)=="LivelihoodAid" &&
            SandboxAgencyDecisionPolicy.ChooseInitiative(
                "EstablishLivelihood",true,false,5,0,100,
                0,0,0,100)=="");
        var phantomConsent=new SandboxInheritanceNegotiationState();
        phantomConsent.Petition("ClanHeadConsents","castle_x",
            "real_leader","birth_clan","HouseholdSeniority",0);
        Check("high native relation is not game-backed NPC estate consent",
            phantomConsent.Authorized &&
            !phantomConsent.NativeHeadConsentObserved &&
            !SandboxInheritanceNegotiationState.Parse(
                phantomConsent.Serialize()).NativeHeadConsentObserved);
        Check("native Review Back to Clan Name restores House/Fortune",
            CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                CUNativeHouseNavigationPolicy.ReviewStage,
                CUNativeHouseNavigationPolicy.ClanStage,true,true));
        Check("native Review Back directly to Banner restores House/Fortune",
            CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                CUNativeHouseNavigationPolicy.ReviewStage,
                CUNativeHouseNavigationPolicy.BannerStage,true,true));
        Check("House overlay Back stays native without recursive reentry",
            !CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                CUNativeHouseNavigationPolicy.ReviewStage,
                CUNativeHouseNavigationPolicy.ClanStage,true,false) &&
            !CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                CUNativeHouseNavigationPolicy.ReviewStage,
                CUNativeHouseNavigationPolicy.ClanStage,false,true));
        Check("native Review Next is not mistaken for Back",
            !CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(
                CUNativeHouseNavigationPolicy.ReviewStage,
                CUNativeHouseNavigationPolicy.OptionsStage,true,true) &&
            CUNativeHouseNavigationPolicy.CanReplayNativeNext(
                CUNativeHouseNavigationPolicy.ClanStage) &&
            CUNativeHouseNavigationPolicy.CanReplayNativeNext(
                CUNativeHouseNavigationPolicy.BannerStage) &&
            !CUNativeHouseNavigationPolicy.CanReplayNativeNext(
                CUNativeHouseNavigationPolicy.ReviewStage));
        // In a 2026-11-21 real Vlandian session, the chosen native
        // elder was absent. Early House refusal must not erase spouse,
        // children and siblings if the player selects Independent instead.
        Check("younger brother of a lord cannot bind a parentless elder",
            !SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfLord","Lord",false,true,false,
                true,true,true,false,0) &&
            !SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfLord","Lord",false,false,true,
                true,true,true,false,0) &&
            SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfLord","Lord",false,true,true,
                true,true,true,false,0));
        var realSessionHouse=new SandboxOriginProfile();
        Check("young lord's requested Vlandian household remains editable",
            realSessionHouse.TrySet("Role","Lord") &&
            realSessionHouse.TrySet("StartingKingdomId","vlandia") &&
            realSessionHouse.TrySet(
                "NobleBirthPosition","YoungerBrotherOfLord") &&
            realSessionHouse.TrySet("SpouseEnabled","true") &&
            realSessionHouse.TrySet("SpouseAge","38") &&
            realSessionHouse.TrySet("ChildCount","2") &&
            realSessionHouse.TrySet("Child.1.Age","9") &&
            realSessionHouse.TrySet("Child.2.Age","7") &&
            realSessionHouse.TrySet("SiblingCount","2"));
        var recoverableHouse=SandboxOriginProfile.Parse(
            realSessionHouse.Serialize());
        Check("correcting unsupported noble birth retains whole family",
            recoverableHouse.TrySet(
                "NobleBirthPosition","Independent") &&
            SandboxNobleStartPolicy.ValidForRole(
                recoverableHouse.NobleBirthPosition,
                recoverableHouse.Role) &&
            recoverableHouse.SpouseEnabled &&
            recoverableHouse.SpouseAge==38 &&
            recoverableHouse.ChildCount==2 &&
            recoverableHouse.Children[0].Age==9 &&
            recoverableHouse.Children[1].Age==7 &&
            recoverableHouse.SiblingCount==2 &&
            recoverableHouse.StartingKingdomId=="vlandia" &&
            SandboxOriginProfile.Parse(recoverableHouse.Serialize())
                .Serialize()==recoverableHouse.Serialize());
        // For an Independent Lord, the player is eldest brother.
        // Native clan leadership remains a separate game-owned role.
        var eldestBrotherPlan=new SandboxOriginProfile();
        var suggestedLordHouse=new SandboxOriginProfile();
        Check("automatic independent noble household has valid seniority",
            suggestedLordHouse.TrySet("Role","Lord") &&
            suggestedLordHouse.TrySet("NobleBirthPosition","Independent") &&
            suggestedLordHouse.TrySet("SiblingCount","2"));
        SandboxAutoHousePlan.Prepare(suggestedLordHouse,
            "vlandia",40,false,"Founder");
        Check("automatic older sibling is a sister, not an older brother",
            suggestedLordHouse.Siblings[1].Age>40 &&
            suggestedLordHouse.Siblings[1].Gender=="Female" &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                suggestedLordHouse,40)=="" &&
            SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInIndependentLordHouse(
                    suggestedLordHouse,40)==0);
        Check("independent lord preserves incompatible custom sibling sex",
            suggestedLordHouse.TrySet("Sibling.2.Gender","Male") &&
            suggestedLordHouse.Siblings[1].Gender=="Male" &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                suggestedLordHouse,40)=="independent_lord_older_brother");
        // Other origins do not receive Independent Lord seniority edits.
        var cadetHouse=new SandboxOriginProfile();
        cadetHouse.TrySet("Role","Lord");
        cadetHouse.TrySet("NobleBirthPosition","YoungerBrotherOfLord");
        cadetHouse.TrySet("SiblingCount","2");
        SandboxAutoHousePlan.Prepare(cadetHouse,
            "vlandia",40,false,"Cadet");
        cadetHouse.TrySet("Sibling.2.Gender","Male");
        Check("automatic non-independent cadet can retain older brother",
            cadetHouse.Siblings[1].Age>40 &&
            cadetHouse.Siblings[1].Gender=="Male");
        Check("independent lord eldest-brother fixture",
            eldestBrotherPlan.TrySet("Role","Lord") &&
            eldestBrotherPlan.TrySet("StartingKingdomId","vlandia") &&
            eldestBrotherPlan.TrySet("NobleBirthPosition","Independent") &&
            eldestBrotherPlan.TrySet("SiblingCount","1") &&
            eldestBrotherPlan.TrySet("Sibling.1.Gender","Male") &&
            eldestBrotherPlan.TrySet("Sibling.1.Age","42"));
        string independentHouseError;
        Check("independent lord refuses older brother at House validation",
            SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInIndependentLordHouse(
                    eldestBrotherPlan,40)==1 &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="independent_lord_older_brother" &&
            !eldestBrotherPlan.IsValid(40,out independentHouseError) &&
            independentHouseError.Contains(
                "independent_lord_older_brother"));
        Check("old profile with older brother remains detectable",
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                SandboxOriginProfile.Parse(
                    eldestBrotherPlan.Serialize()),40)==
                    "independent_lord_older_brother");
        Check("independent lord permits older sister",
            eldestBrotherPlan.TrySet("Sibling.1.Gender","Female") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="" &&
            SandboxHouseholdLineagePolicy
                .FirstOlderBrotherInIndependentLordHouse(
                    eldestBrotherPlan,40)==0);
        Check("independent lord permits younger or same-age brother",
            eldestBrotherPlan.TrySet("Sibling.1.Gender","Male") &&
            eldestBrotherPlan.TrySet("Sibling.1.Age","39") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="" &&
            eldestBrotherPlan.TrySet("Sibling.1.Age","40") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="");
        Check("disabled older brother is not considered",
            eldestBrotherPlan.TrySet("Sibling.1.Age","42") &&
            eldestBrotherPlan.TrySet("SiblingCount","0") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="");
        Check("other noble backgrounds retain separate older sibling rules",
            eldestBrotherPlan.TrySet("SiblingCount","1") &&
            eldestBrotherPlan.TrySet("NobleBirthPosition",
                "YoungerBrotherOfLord") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="");
        Check("non-lord independent house allows an older brother",
            eldestBrotherPlan.TrySet("NobleBirthPosition","Independent") &&
            eldestBrotherPlan.TrySet("Role","Knight") &&
            SandboxHouseholdLineagePolicy.ValidateHousePlan(
                eldestBrotherPlan,40)=="");
        Check("younger brother of a king requires real old king lineage",
            SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfKing","Lord",false,true,true,
                true,true,true,true,0) &&
            !SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfKing","Lord",false,true,false,
                true,true,true,true,0) &&
            !SandboxNobleStartPolicy.CanBindElder(
                "YoungerBrotherOfKing","Lord",false,true,true,
                true,true,true,false,0));
        Check("second and third sons need older native sons",
            SandboxNobleStartPolicy.RequiredOlderSons(
                "SecondSonOfLord")==1 &&
            SandboxNobleStartPolicy.RequiredOlderSons(
                "ThirdSonOfKing")==2 &&
            !SandboxNobleStartPolicy.CanBindElder(
                "ThirdSonOfKing","Lord",false,true,true,
                true,true,true,true,1) &&
            SandboxNobleStartPolicy.CanBindElder(
                "ThirdSonOfKing","Lord",false,true,true,
                true,true,true,true,2));
        Check("younger noble sister respects character sex",
            SandboxNobleStartPolicy.CanBindElder(
                "YoungerSisterOfLord","Lord",true,true,true,
                true,true,true,false,0) &&
            !SandboxNobleStartPolicy.CanBindElder(
                "YoungerSisterOfLord","Lord",false,true,true,
                true,true,true,false,0));
        Check("lineage cannot fabricate a royal king rank",
            !SandboxNobleStartPolicy.ValidForRole(
                "SecondSonOfKing","King") &&
            SandboxNobleStartPolicy.ValidForRole(
                "SecondSonOfKing","Lord") &&
            SandboxNobleStartPolicy.ValidForRole(
                "Independent","Wanderer"));
        Check("noble retinue uses office names instead of wanderer class",
            SandboxNobleStartPolicy.RetinueOffices("Lord",false)
                .SequenceEqual(new[]{"MasterOfArms","Knight"}) &&
            SandboxNobleStartPolicy.RetinueOffices("Lord",true)
                .SequenceEqual(new[]{"MasterOfArms","Knight"}) &&
            SandboxNobleStartPolicy.RetinueOffices("Admiral",false)
                .SequenceEqual(new[]{"FirstMate","Navigator"}) &&
            SandboxNobleStartPolicy.RetinueOffices("Wanderer",false)
                .Length==0);
        Check("native family cleanup only disables a new unattached Hero",
            SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,false,false,false,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                false,false,false,false,false,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,true,false,false,false,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,true,false,false,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,false,true,false,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,false,false,true,false,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,false,false,false,true,false) &&
            !SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,false,false,false,false,false,true));
        Check("allocated native child prevents cleanup of parent spouse",
            SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(
                false,0,false) &&
            !SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(
                false,1,false) &&
            !SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(
                true,0,false) &&
            !SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(
                false,0,true));

        // One game-backed social contract matrix covers every selectable role.
        var auditRoles=new[]{"Wanderer","Outlaw","Mercenary",
            "Recruit","Soldier","HouseholdKnight","Knight",
            "NobleCadet","Lord","King","Admiral","Trader","Beggar"};
        var auditStarts=new[]{"default","outlaw","mercenary",
            "default","mercenary","default","vassal","default",
            "vassal","king","fleetadmiral","trader","beggar"};
        Check("all 13 compatible roles map to one exact native start",
            SandboxOriginProfile.Roles.Length==auditRoles.Length &&
            !auditRoles.Except(SandboxOriginProfile.Roles).Any() &&
            !SandboxOriginProfile.Roles.Except(auditRoles).Any() &&
            auditRoles.Distinct().Count()==13 &&
            auditRoles.Select((role,index)=>
                SandboxRoleConsistencyPolicy.NativeType(role)==
                    auditStarts[index]).All(x=>x) &&
            SandboxRoleConsistencyPolicy.NativeType("InventedTitle")=="");
        Check("all 13 starting stations have factual native requirements",
            auditRoles.Select((role,index)=>{
                bool kingdom=role=="King" || role=="Lord" ||
                    role=="Knight" || role=="Soldier" ||
                    role=="Mercenary" || role=="Admiral";
                bool royal=role=="King";
                int actualFiefs=royal || role=="Lord" ||
                    role=="Knight"?1:0;
                return SandboxRoleConsistencyPolicy.CheckNativeStation(
                    role,auditStarts[index],kingdom,royal,
                    actualFiefs)=="" &&
                    SandboxStartingLifePolicy.Station(
                        role,"Independent")!="" &&
                    SandboxStartingLifePolicy.Occupation(role)!="" &&
                    SandboxStartingLifePolicy.Obligation(
                        role,"Independent")!="";
            }).All(x=>x));
        Check("native rank matrix rejects fictional land and authority",
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "NobleCadet","default",false,false,1)==
                    "independent_start_has_kingdom_or_fief" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "HouseholdKnight","vassal",true,false,1)==
                    "native_start_type_mismatch" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Recruit","default",true,false,0)==
                    "independent_start_has_kingdom_or_fief" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "King","king",true,false,2)==
                    "ruler_status_or_fief_missing" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Knight","vassal",true,false,0)==
                    "vassal_status_or_fief_missing" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Soldier","mercenary",false,false,0)==
                    "native_military_kingdom_missing" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Mercenary","mercenary",false,false,0)==
                    "mercenary_company_station_or_land_missing" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Mercenary","mercenary",true,false,1)==
                    "mercenary_company_station_or_land_missing" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Mercenary","mercenary",true,false,0)=="" &&
            SandboxRoleConsistencyPolicy.CheckNativeStation(
                "Trader","trader",false,false,2)==
                    "independent_start_has_kingdom_or_fief");

        Check("native second and third sons identify a lord as father",
            SandboxNobleStartPolicy.IsSonOrigin("SecondSonOfLord") &&
            SandboxNobleStartPolicy.IsSonOrigin("ThirdSonOfKing") &&
            !SandboxNobleStartPolicy.IsSonOrigin("YoungerBrotherOfLord") &&
            SandboxNobleStartPolicy.RequiredOlderSons("SecondSonOfLord")==1 &&
            SandboxNobleStartPolicy.RequiredOlderSons("ThirdSonOfLord")==2);
        Check("soldier starts with family only and a native lord patron",
            SandboxServicePolicy.NeedsPatron("Soldier") &&
            SandboxServicePolicy.NeedsPatron("Knight") &&
            !SandboxServicePolicy.NeedsPatron("Lord") &&
            SandboxServicePolicy.DefaultRetinue("Soldier")=="FamilyOnly" &&
            SandboxServicePolicy.DefaultRetinue("Knight")=="RoleBased" &&
            SandboxServicePolicy.CanSelectPatron("Soldier","vlandia") &&
            !SandboxServicePolicy.CanSelectPatron("Soldier","") &&
            SandboxNobleStartPolicy.IsRetinueMode("FamilyOnly"));
        var nobleProfile=new SandboxOriginProfile();
        Check("noble line and role retinue survive profile roundtrip",
            nobleProfile.TrySet("Role","Lord") &&
            nobleProfile.TrySet("StartingKingdomId","vlandia") &&
            nobleProfile.TrySet("NobleBirthPosition",
                "YoungerBrotherOfKing") &&
            nobleProfile.TrySet("NobleElderHeroId","lord_1_1") &&
            nobleProfile.TrySet("RetinueMode","RoleBased") &&
            nobleProfile.TrySet("ServicePatronHeroId","lord_1_1") &&
            nobleProfile.TrySet("SiblingCount","2") &&
            nobleProfile.TrySet("Sibling.1.Name","Andric") &&
            nobleProfile.TrySet("Sibling.2.Gender","Female") &&
            SandboxOriginProfile.Parse(nobleProfile.Serialize())
                .NobleBirthPosition=="YoungerBrotherOfKing" &&
            SandboxOriginProfile.Parse(nobleProfile.Serialize())
                .NobleElderHeroId=="lord_1_1" &&
            SandboxOriginProfile.Parse(nobleProfile.Serialize())
                .RetinueMode=="RoleBased" &&
            SandboxOriginProfile.Parse(nobleProfile.Serialize())
                .ServicePatronHeroId=="lord_1_1" &&
            SandboxOriginProfile.Parse(nobleProfile.Serialize())
                .Siblings[1].Gender=="Female");
        // The real guided creator edits a Serialize/Parse copy on
        // every field change, then reopens House after a native Back.
        // Inactive conditional fields must preserve genuine user input.
        var editedHouse=new SandboxOriginProfile();
        Check("guided House retains named kin across hidden fields",
            editedHouse.TrySet("Role","Lord") &&
            editedHouse.TrySet("SpouseEnabled","true") &&
            editedHouse.TrySet("SpouseName","Evelyn") &&
            editedHouse.TrySet("SpouseAge","34") &&
            editedHouse.TrySet("ChildCount","2") &&
            editedHouse.TrySet("Child.1.Name","Mira") &&
            editedHouse.TrySet("Child.1.Age","9") &&
            editedHouse.TrySet("Child.2.Name","Rowan") &&
            editedHouse.TrySet("Child.2.Age","11") &&
            editedHouse.TrySet("SiblingCount","1") &&
            editedHouse.TrySet("Sibling.1.Name","Elian") &&
            editedHouse.TrySet("RetinueMode","RoleBased") &&
            editedHouse.TrySet("Gold","1250"));
        var hiddenHouse=SandboxOriginProfile.Parse(
            editedHouse.Serialize());
        Check("conditional child count hides but never destroys entries",
            hiddenHouse.TrySet("ChildCount","0") &&
            hiddenHouse.TrySet("SpouseEnabled","false") &&
            hiddenHouse.TrySet("SiblingCount","0") &&
            SandboxOriginProfile.Parse(hiddenHouse.Serialize())
                .Children[0].Name=="Mira" &&
            SandboxOriginProfile.Parse(hiddenHouse.Serialize())
                .Children[1].Age==11 &&
            SandboxOriginProfile.Parse(hiddenHouse.Serialize())
                .SpouseName=="Evelyn");
        var revisitedHouse=SandboxOriginProfile.Parse(
            hiddenHouse.Serialize());
        Check("native Review Back reopens preserved House draft",
            revisitedHouse.TrySet("SpouseEnabled","true") &&
            revisitedHouse.TrySet("ChildCount","2") &&
            revisitedHouse.TrySet("SiblingCount","1") &&
            revisitedHouse.SpouseName=="Evelyn" &&
            revisitedHouse.SpouseAge==34 &&
            revisitedHouse.Children[0].Name=="Mira" &&
            revisitedHouse.Children[0].Age==9 &&
            revisitedHouse.Children[1].Name=="Rowan" &&
            revisitedHouse.Children[1].Age==11 &&
            revisitedHouse.Siblings[0].Name=="Elian" &&
            revisitedHouse.Gold==1250 &&
            revisitedHouse.RetinueMode=="RoleBased" &&
            SandboxOriginProfile.Parse(revisitedHouse.Serialize())
                .Serialize()==revisitedHouse.Serialize());
        Check("invalid House input never changes saved selection",
            !revisitedHouse.TrySet("Child.1.Age","999") &&
            revisitedHouse.Children[0].Age==9 &&
            !revisitedHouse.TrySet("SpouseName","Invalid123") &&
            revisitedHouse.SpouseName=="Evelyn");
        Check("unsafe invented royal identity rejected in profile",
            !nobleProfile.TrySet("NobleBirthPosition","FakeRoyal") &&
            !nobleProfile.TrySet("RetinueMode","RandomWanderers") &&
            !nobleProfile.TrySet("ServicePatronHeroId","../../../file") &&
            !nobleProfile.TrySet("NobleElderHeroId","../../../file"));
        nobleProfile.ResetForNewCampaign();
        Check("fresh Sandbox forgets prior royal and retinue grants",
            nobleProfile.NobleBirthPosition=="Independent" &&
            nobleProfile.NobleElderHeroId=="" &&
            nobleProfile.ServicePatronHeroId=="" &&
            nobleProfile.RetinueMode=="Native" &&
            nobleProfile.SiblingCount==0);
        Check("medieval Starting Life uses native rank not invented enlistment",
            SandboxStartingLifePolicy.Station("Soldier","Independent")==
                "MercenaryCompany" &&
            SandboxStartingLifePolicy.Station("Lord","SecondSonOfLord")==
                "LandedVassalCadet" &&
            SandboxStartingLifePolicy.Obligation("Soldier","Independent")==
                "NativeMercenaryContract" &&
            SandboxStartingLifePolicy.Occupation("Knight")==
                "KnightInVassalService" &&
            SandboxStartingLifePolicy.Dossier("Knight","Independent",
                "Auto","Auto").Contains("Knight holding") &&
            SandboxStartingLifePolicy.Dossier("Soldier","Independent",
                "Auto","Auto").Contains("not an enlisted soldier"));
        var revisedLife=new SandboxOriginProfile();
        Check("Advanced life history choices persist with valid defaults",
            revisedLife.TrySet("EarlyHousehold","Town") &&
            revisedLife.TrySet("PersonalAmbition","Security") &&
            SandboxOriginProfile.Parse(revisedLife.Serialize())
                .EarlyHousehold=="Town" &&
            SandboxOriginProfile.Parse(revisedLife.Serialize())
                .PersonalAmbition=="Security" &&
            !revisedLife.TrySet("EarlyHousehold","FictionalTitle") &&
            !revisedLife.TrySet("PersonalAmbition","InfiniteGold"));
        revisedLife.ResetForNewCampaign();
        Check("new campaigns discard previous detailed life choices",
            revisedLife.EarlyHousehold=="Auto" &&
            revisedLife.PersonalAmbition=="Auto");
        var birthPlan=new SandboxOriginProfile();
        birthPlan.Role="Lord";
        birthPlan.NobleBirthPosition="SecondSonOfLord";
        birthPlan.SiblingCount=2;
        birthPlan.Siblings[0].Gender="Male";
        birthPlan.Siblings[0].Age=24;
        birthPlan.Siblings[1].Gender="Female";
        birthPlan.Siblings[1].Age=27;
        Check("older named House brother occupies the second-son slot",
            SandboxHouseholdLineagePolicy.PlannedOlderBrothers(
                birthPlan,20)==1 &&
            SandboxHouseholdLineagePolicy.AdditionalOlderBrothers(
                1,0,1)==0 &&
            birthPlan.IsValid(20,out var lineageGood));
        birthPlan.Siblings[1].Gender="Male";
        Check("second-son plan cannot generate two older male siblings",
            !birthPlan.IsValid(20,out var lineageIssue) &&
            lineageIssue.Contains("too_many_older_male_siblings") &&
            SandboxHouseholdLineagePolicy.AdditionalOlderBrothers(
                1,0,2)==-1);
        var chronicles=new SandboxLifeHistoryState();
        Check("living persons records true IDs and relationship bonds",
            chronicles.Record(new SandboxLifeHistoryState.Person{
                Id="heroA",Name="Élodie | daughter",Culture="vlandia",
                Age=22,Station="RecordedOffspring",Occupation="FamilyChild",
                HouseholdId="clan_a",Origin="PriorHistoryNotEstablished",
                Ambition="NotYetModeled",Biography="A native Hero."}) &&
            chronicles.Record(new SandboxLifeHistoryState.Person{
                Id="heroB",Name="Alric",Age=55}) &&
            chronicles.Tie("heroA","heroB","Father",
                "VerifiedNativeParent") &&
            chronicles.Tie("heroA","heroB","Father",
                "VerifiedNativeParent") &&
            chronicles.Remember("StartingLifeRecorded","heroA","heroB",
                "real source|not an invented past") &&
            chronicles.People.Count==2 &&
            chronicles.Bonds.Count==1);
        var packedChronicle=chronicles.Serialize();
        var replayedChronicle=SandboxLifeHistoryState.Parse(packedChronicle);
        Check("save-backed biographies retain unicode and escaped content",
            replayedChronicle.People.Count==2 &&
            replayedChronicle.People[0].Name=="Élodie | daughter" &&
            replayedChronicle.Bonds.Count==1 &&
            replayedChronicle.Events.Count==1 &&
            replayedChronicle.Events[0].Detail.Contains("|") &&
            replayedChronicle.Serialize()==packedChronicle &&
            SandboxLifeHistoryState.Parse("invalid version")
                .People.Count==0);
        var career=new SandboxServiceCareerState();
        career.Start("Soldier","lord_vlandia","vlandia");
        string careerMilestone="";
        for(int d=0;d<30;d++)
            careerMilestone=career.Observe(true,true,100+d*5);
        Check("native service contract records tenure, never invents wages",
            career.Status=="Active" &&
            career.DaysInService==30 &&
            careerMilestone=="thirty_days_service_recorded" &&
            career.Milestone==30 &&
            career.PayAuthority=="NativeGame" &&
            career.LastObservedGoldChange==5);
        var careerSaved=SandboxServiceCareerState.Parse(
            career.Serialize());
        Check("service tenure survives save and detects lost native patronage",
            careerSaved.PatronId=="lord_vlandia" &&
            careerSaved.DaysInService==30 &&
            careerSaved.Observe(true,false,120)=="native_contract_changed" &&
            careerSaved.Status=="NativeContractChanged" &&
            SandboxServiceCareerState.Parse("wrong").IsConfigured==false);
        var observed=new SandboxSocialObservationState();
        var firstObservation=observed.Observe("family_sibling",42);
        var changedRelation=observed.Observe("family_sibling",55);
        Check("social memory records observed native changes only",
            firstObservation==null &&
            changedRelation!=null &&
            changedRelation.Before==42 &&
            changedRelation.After==55 &&
            observed.Observe("family_sibling",55)==null &&
            SandboxSocialObservationState.Parse(observed.Serialize())
                .NativeRelations["family_sibling"]==55);
        var potentialClaim=new SandboxSuccessionInterestState();
        potentialClaim.Record("younger_son","native_father",
            "native_mother","father_clan",1,true);
        Check("succession stays an expectation, never transfers estates",
            potentialClaim.IsRecorded &&
            potentialClaim.Status=="PotentialFamilyInterest" &&
            potentialClaim.Observe(false)==
                "parent_death_observed_native_ownership_unchanged" &&
            potentialClaim.Status=="ReviewNativeSuccession" &&
            SandboxSuccessionInterestState.Parse(
                potentialClaim.Serialize()).Status=="ReviewNativeSuccession");
        var recentlyObserved=new SandboxLifeHistoryState();
        for(int e=0;e<SandboxLifeHistoryState.MaxEvents+7;e++)
            recentlyObserved.Remember("Observed"+e,"hero","lord","native");
        Check("long campaigns keep recent memories instead of freezing",
            recentlyObserved.Events.Count==SandboxLifeHistoryState.MaxEvents &&
            recentlyObserved.Events[0].Kind=="Observed7" &&
            recentlyObserved.Events[
                SandboxLifeHistoryState.MaxEvents-1].Kind==
                    "Observed"+(SandboxLifeHistoryState.MaxEvents+6));
        Check("native landless beginnings never confer a fief or kingdom",
            SandboxCustomLifeRolePolicy.IsLandless("NobleCadet") &&
            SandboxCustomLifeRolePolicy.IsLandless("Recruit") &&
            SandboxCustomLifeRolePolicy.IsLandless("HouseholdKnight") &&
            !SandboxCustomLifeRolePolicy.IsLandless("Lord") &&
            SandboxCustomLifeRolePolicy.NativeLandlessContract(false,0) &&
            !SandboxCustomLifeRolePolicy.NativeLandlessContract(true,0) &&
            !SandboxCustomLifeRolePolicy.NativeLandlessContract(false,1) &&
            SandboxNobleStartPolicy.ValidForRole(
                "SecondSonOfLord","NobleCadet") &&
            !SandboxNobleStartPolicy.ValidForRole(
                "Independent","NobleCadet"));
        var landlessPlan=new SandboxOriginProfile();
        landlessPlan.TrySet("Role","NobleCadet");
        landlessPlan.TrySet("StartingKingdomId","vlandia");
        landlessPlan.TrySet("NobleBirthPosition","SecondSonOfLord");
        Check("native landless cadet keeps valid birth without a fief",
            landlessPlan.IsValid(20,out var landlessError) &&
            SandboxOriginProfile.Parse(landlessPlan.Serialize())
                .Role=="NobleCadet" &&
            SandboxStartingLifePolicy.Station("NobleCadet",
                "SecondSonOfLord")=="UnlandedDynasticCadet" &&
            SandboxStartingLifePolicy.Dossier("NobleCadet",
                "SecondSonOfLord","Auto","Auto").Contains("Landless"));
        var recruitPlan=new SandboxOriginProfile();
        recruitPlan.TrySet("Role","Recruit");
        recruitPlan.TrySet("StartingKingdomId","vlandia");
        Check("recruit and household knight are independent applicants",
            recruitPlan.IsValid(20,out var recruitError) &&
            SandboxServicePolicy.NeedsPatron("Recruit") &&
            SandboxServicePolicy.NeedsPatron("HouseholdKnight") &&
            SandboxServicePolicy.DefaultRetinue("Recruit")=="FamilyOnly" &&
            SandboxServicePolicy.DefaultRetinue("HouseholdKnight")==
                "FamilyOnly" &&
            SandboxStartingLifePolicy.Station("Recruit","Independent")==
                "ProspectiveEnlistedRecruit");
        var pending=new SandboxServiceCareerState();
        pending.Start("Recruit","native_lord","vlandia");
        Check("pending recruit receives no fake wages tenure or rank",
            pending.IsConfigured &&
            pending.Status=="PendingEnlistment" &&
            pending.Observe(true,true,500)=="pending_enlistment" &&
            pending.DaysInService==0 &&
            pending.Milestone==0);
        Check("external API alone verifies actual enlisted rank and wage",
            pending.ObserveExternal("native_lord","private",
                "guard_duty",12,7,83,true)==
                "external_enlistment_verified" &&
            pending.Status=="EnlistedActive" &&
            pending.VerifiedExternalEnlistment &&
            pending.PayAuthority=="ServeAsSoldier" &&
            pending.ExternalDailyWage==12 &&
            pending.DaysInService==7 &&
            pending.ExternalInsideHostParty);
        var lostProvider=SandboxServiceCareerState.Parse(
            pending.Serialize());
        Check("missing service provider suspends saved enlistment proof",
            lostProvider.SuspendUnverifiedExternal()==
                "external_provider_unavailable" &&
            !lostProvider.VerifiedExternalEnlistment &&
            lostProvider.Status=="ExternalStatusUnverified" &&
            lostProvider.ExternalDailyWage==-1 &&
            SandboxServiceCareerState.Parse(lostProvider.Serialize())
                .Status=="ExternalStatusUnverified");
        var afterEnlist=SandboxServiceCareerState.Parse(
            pending.Serialize());
        Check("verified external service survives saved dossier",
            afterEnlist.IsConfigured &&
            afterEnlist.ExternalRank=="private" &&
            afterEnlist.ExternalDuty=="guard_duty" &&
            afterEnlist.ExternalDailyWage==12 &&
            afterEnlist.Serialize()==pending.Serialize() &&
            afterEnlist.ObserveExternalAbsent()==
                "external_service_ended_observed" &&
            !afterEnlist.VerifiedExternalEnlistment);
        var firstKinGoal=SandboxGeneratedCharacterGoalPolicy.Goal(
            "vlandia","RecordedSibling","HouseholdSoldier",true,true);
        Check("sibling outside the real party pursues family standing",
            SandboxGeneratedCharacterGoalPolicy.Goal(
                "vlandia","RecordedSibling","FamilyMember",true,
                false)=="PreserveFamilyPosition" &&
            SandboxGeneratedCharacterGoalPolicy.Goal(
                "vlandia","RecordedSibling","FamilyMember",true,
                true)=="ProtectSharedHousehold");
        var evolvingAgenda=new SandboxCharacterAgendaState();
        evolvingAgenda.Register("cadet","PreserveFamilyPosition",
            "castle_v1","NativeHomeSettlement",
            "FamilyMember","vlandia","birth_house");
        Check("observed native co-service changes inferred NPC ambition",
            evolvingAgenda.ObserveNativeGoal(
                "cadet","ProtectSharedHousehold",
                "FamilyMember","player_clan")==
                    "goal_reassessed_from_native_context" &&
            evolvingAgenda.Get("cadet").Goal=="ProtectSharedHousehold" &&
            evolvingAgenda.Get("cadet").ClanId=="player_clan" &&
            evolvingAgenda.ObserveNativeGoal(
                "cadet","ProtectSharedHousehold",
                "FamilyMember","player_clan")=="same" &&
            SandboxCharacterAgendaState.Parse(evolvingAgenda.Serialize())
                .Get("cadet").Goal=="ProtectSharedHousehold");
        Check("loss of native residence clears stale location evidence",
            evolvingAgenda.ObserveHome("cadet","","Unverified")==
                "residence_no_longer_verified" &&
            evolvingAgenda.Get("cadet").HomeId=="" &&
            evolvingAgenda.Get("cadet").HomeEvidence=="Unverified" &&
            evolvingAgenda.ObserveHome("cadet","","Unverified")=="same" &&
            SandboxCharacterAgendaState.Parse(evolvingAgenda.Serialize())
                .Get("cadet").HomeId=="");
        Check("native co-service opens household-support requests",
            SandboxAgencyDecisionPolicy.Choose(
                evolvingAgenda.Get("cadet").Goal,true,true,20,0,1000)==
                    "HouseholdAid" &&
            SandboxAgencyDecisionPolicy.Choose(
                "PreserveFamilyPosition",true,true,20,0,1000)==
                    "Recognition" &&
            SandboxAgencyDecisionPolicy.Choose(
                "ProtectSharedHousehold",true,false,20,0,1000)=="");
        Check("native departure changes inferred goal without erasing kin",
            evolvingAgenda.ObserveNativeGoal(
                "cadet","PreserveFamilyPosition",
                "FamilyMember","birth_house")==
                    "goal_reassessed_from_native_context" &&
            evolvingAgenda.Get("cadet").ConsecutiveDaysTogether==0 &&
            evolvingAgenda.Get("cadet").Goal=="PreserveFamilyPosition" &&
            SandboxCharacterAgendaState.Parse(evolvingAgenda.Serialize())
                .Get("cadet").Goal=="PreserveFamilyPosition");
        Check("observed departure switches the later request to recognition",
            SandboxAgencyDecisionPolicy.Choose(
                evolvingAgenda.Get("cadet").Goal,true,true,20,0,1000)==
                "Recognition");
        var siblingAgenda=new SandboxCharacterAgendaState();
        siblingAgenda.Register("brother","ProtectSharedHousehold","",
            "Unverified","HouseholdSoldier","vlandia","clanA");
        int friendship=0;
        for(int day=0;day<150;day++)
        {
            int proposed=siblingAgenda.ObserveComradeship(
                "brother",true,true,true,10+friendship);
            if(proposed>0 && siblingAgenda.ConfirmComradeshipAward(
                "brother",10+friendship,10+friendship+proposed))
                friendship+=proposed;
        }
        Check("family companionship influences bounded native relations",
            firstKinGoal=="ProtectSharedHousehold" &&
            friendship==5 &&
            siblingAgenda.Get("brother").ComradeshipAwards==5 &&
            SandboxCharacterAgendaState.Parse(
                siblingAgenda.Serialize()).Get("brother")
                    .ComradeshipAwards==5);
        var failedKinAction=new SandboxCharacterAgendaState();
        failedKinAction.Register("brother","ProtectSharedHousehold","",
            "Unverified","HouseholdSoldier","vlandia","clanA");
        int proposedAt30=0;
        for(int day=0;day<30;day++)
            proposedAt30=failedKinAction.ObserveComradeship(
                "brother",true,true,true,25);
        Check("failed native relation action does not spend kinship reward",
            proposedAt30==1 &&
            !failedKinAction.ConfirmComradeshipAward(
                "brother",25,25) &&
            !failedKinAction.ConfirmComradeshipAward(
                "brother",25,24) &&
            failedKinAction.Get("brother").ComradeshipAwards==0 &&
            SandboxCharacterAgendaState.Parse(
                failedKinAction.Serialize()).Get("brother")
                    .ComradeshipAwards==0);
        int proposedAt60=0;
        for(int day=30;day<60;day++)
            proposedAt60=failedKinAction.ObserveComradeship(
                "brother",true,true,true,25);
        Check("verified native relation action credits kinship on retry",
            proposedAt60==1 &&
            failedKinAction.ConfirmComradeshipAward("brother",25,26) &&
            !failedKinAction.ConfirmComradeshipAward(
                "brother",25,26) &&
            failedKinAction.Get("brother").ComradeshipAwards==1 &&
            SandboxCharacterAgendaState.Parse(
                failedKinAction.Serialize()).Get("brother")
                    .ComradeshipAwards==1);
        var fixedHomes=new SandboxCharacterAgendaState();
        fixedHomes.Register("sister","PreserveFamilyPosition",
            "castle_a","NativeHomeSettlement",
            "HouseholdNoble","vlandia","birth_clan");
        Check("real household homes update from native location",
            fixedHomes.ObserveHome("sister","castle_b",
                "NativeCurrentSettlement")=="native_home_changed" &&
            SandboxCharacterAgendaState.Parse(
                fixedHomes.Serialize()).Get("sister")
                    .HomeId=="castle_b" &&
            fixedHomes.ObserveComradeship(
                "sister",true,true,true,10)==0);
        Check("six cultures use distinct succession expectations without transfer",
            new[]{"vlandia","sturgia","battania","empire",
                "aserai","khuzait"}
                .Select(SandboxCulturalInheritancePolicy.Model)
                .Distinct().Count()==6 &&
            !SandboxCulturalInheritancePolicy
                .CanTransferEstateWithoutNativeAction() &&
            SandboxCulturalInheritancePolicy.ClaimPriority(
                "vlandia",0,true,true,50,30) >
            SandboxCulturalInheritancePolicy.ClaimPriority(
                "vlandia",2,true,true,50,30));
        var culturalClaim=new SandboxSuccessionInterestState();
        culturalClaim.Record("cadet","father","mother",
            "birth_clan",1,true);
        culturalClaim.SetCulture("khuzait",3);
        Check("culture-specific claim survives save and legacy migration",
            culturalClaim.CustomaryModel=="ClanCoalition" &&
            culturalClaim.PriorityScore==-1 &&
            SandboxSuccessionInterestState.Parse(
                culturalClaim.Serialize()).CustomaryModel==
                    "ClanCoalition" &&
            SandboxSuccessionInterestState.Parse(
                "CU_SUCCESSION_1\ncadet|father|mother|clan|1|True|PotentialFamilyInterest")
                .IsRecorded);
        var medievalStress=SandboxMedievalLifeSimulation.Run(96,3650);
        Console.WriteLine("MEDIEVAL LIFE SIMULATION "+
            (medievalStress.Failures==0?"PASS":"FAIL")+
            " | seeds="+medievalStress.Seeds+
            " | days_per_seed="+medievalStress.Days+
            " | checks="+medievalStress.Checks+
            " | landless_cases="+medievalStress.LandlessCases+
            " | regions="+medievalStress.RegionCases+
            " | external_enlistments="+medievalStress.ExternalEnlistments+
            " | shared_service_awards="+medievalStress.ComradeshipAwards+
            " | parent_deaths="+medievalStress.ParentDeaths+
            " | reloads="+medievalStress.SaveReloads+
            " | first_failure="+medievalStress.FirstFailure);
        Check("96 seeds through 3650 days satisfy medieval start invariants",
            medievalStress.Failures==0 &&
            medievalStress.Checks>600000 &&
            medievalStress.LandlessCases==72 &&
            medievalStress.RegionCases==96 &&
            medievalStress.ExternalEnlistments==24 &&
            medievalStress.ParentDeaths==96 &&
            medievalStress.ComradeshipAwards>0 &&
            medievalStress.SaveReloads>5000 &&
            medievalStress.NativeWagesGranted==0 &&
            medievalStress.NativeFiefsTransferred==0);
        var npcRequests=new SandboxAgencyDecisionState();
        for(int i=0;i<15;i++)npcRequests.Tick();
        Check("autonomous NPC invitations need real adult and physical access",
            SandboxAgencyDecisionPolicy.Choose("ProtectSharedHousehold",
                true,true,25,0,90)=="HouseholdAid" &&
            SandboxAgencyDecisionPolicy.Choose("EstablishLivelihood",
                true,false,25,0,90)=="" &&
            SandboxAgencyDecisionPolicy.Choose("MaintainHousehold",
                false,true,25,0,90)=="" &&
            SandboxAgencyDecisionPolicy.Choose("PreserveFamilyPosition",
                true,true,25,0,90)=="Recognition");
        Check("character-generated household requests have native costs",
            npcRequests.Propose("brother","HouseholdAid") &&
            npcRequests.HasPending &&
            SandboxAgencyDecisionPolicy.Amount(
                npcRequests.PendingKind)==150 &&
            !npcRequests.Propose("factor","LivelihoodAid") &&
            SandboxAgencyDecisionPolicy.NativeTransferPermitted(
                "HouseholdAid",true,300,true) &&
            !SandboxAgencyDecisionPolicy.NativeTransferPermitted(
                "HouseholdAid",true,100,true) &&
            SandboxAgencyDecisionPolicy.RelationDelta(
                "HouseholdAid",false)==-1);
        Check("saved NPC invitation requires the matching quota record",
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|phantom|HouseholdAid|15")
                .HasPending &&
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|brother|HouseholdAid|15"+
                "\nR|sister|15|1").HasPending &&
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|brother|HouseholdAid|15"+
                "\nR|brother|14|1").HasPending &&
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|brother|HouseholdAid|15"+
                "\nR|brother|15|0").HasPending);
        Check("invalid saved request timestamps cannot bypass quotas",
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|brother|HouseholdAid|15"+
                "\nR|brother|-2147483648|1").HasPending &&
            SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n15|brother|HouseholdAid|15"+
                "\nR|brother|-2147483648|1").Records.Count==0 &&
            !SandboxAgencyDecisionState.Parse(
                "CU_AGENCY_1\n50|brother|HouseholdAid|15"+
                "\nR|brother|15|1").HasPending);
        var nearMaxNpcDay=new SandboxAgencyDecisionState{
            Day=Int32.MaxValue};
        Check("large campaign day cannot overflow first NPC cooldown",
            nearMaxNpcDay.Propose("late_hero","Recognition") &&
            nearMaxNpcDay.HasPending &&
            nearMaxNpcDay.For("late_hero").RequestsMade==1 &&
            SandboxAgencyDecisionState.Parse(
                nearMaxNpcDay.Serialize()).HasPending);
        string originalRequest=npcRequests.Serialize();
        var reloadedRequest=SandboxAgencyDecisionState.Parse(
            originalRequest);
        Check("active character requests survive native campaign saves",
            reloadedRequest.Serialize()==originalRequest &&
            reloadedRequest.PendingHeroId=="brother" &&
            reloadedRequest.Dismiss("Accepted")=="Accepted" &&
            !reloadedRequest.HasPending);
        for(int i=0;i<36;i++)npcRequests.Tick();
        Check("unanswered local NPC requests expire without gold transfer",
            !npcRequests.HasPending &&
            !npcRequests.Propose("brother","HouseholdAid") &&
            npcRequests.Records[0].RequestsMade==1);
        var inheritanceCourt=new SandboxInheritanceNegotiationState();
        Check("six cultural court thresholds remain distinct",
            new[]{"vlandia","sturgia","battania","empire",
                "aserai","khuzait"}
                .Select(c=>SandboxInheritanceNegotiationPolicy
                    .ConsentThreshold(c,1)).Distinct().Count()>=5 &&
            SandboxInheritanceNegotiationPolicy.Evaluate(
                true,true,true,true,true,2,1,90,
                "vlandia")=="ClanHeadConsents" &&
            SandboxInheritanceNegotiationPolicy.Evaluate(
                true,false,true,true,true,2,1,90,
                "vlandia")=="ParentStillLiving" &&
            SandboxInheritanceNegotiationPolicy.Evaluate(
                true,true,true,true,false,2,1,90,
                "vlandia")=="NativeAllegianceRequired" &&
            SandboxInheritanceNegotiationPolicy.Evaluate(
                true,true,true,true,true,1,1,90,
                "vlandia")=="NoSecondaryEstate");
        Check("contested family claim cannot transfer before clan approval",
            inheritanceCourt.Petition("ClaimContested",
                "castle_b","lord","clan_a","HouseholdSeniority",120)==
                "ClaimContested" &&
            !inheritanceCourt.Authorized &&
            !SandboxInheritanceNegotiationPolicy.TransferGate(
                true,false,"clan_a","clan_a",true,true,2));
        Check("consented secondary family grant needs explicit native choice",
            inheritanceCourt.Petition("ClanHeadConsents",
                "castle_b","lord","clan_a","HouseholdSeniority",200)==
                "ClanHeadConsents" &&
            inheritanceCourt.Authorized &&
            !SandboxInheritanceNegotiationPolicy.TransferGate(
                false,true,"clan_a","clan_a",true,true,2) &&
            SandboxInheritanceNegotiationPolicy.TransferGate(
                true,true,"clan_a","clan_a",true,true,2) &&
            !SandboxInheritanceNegotiationPolicy.TransferGate(
                true,true,"clan_a","clan_b",true,true,2) &&
            !SandboxInheritanceNegotiationPolicy.TransferGate(
                true,true,"clan_a","clan_a",false,true,2));
        string oldPetition=inheritanceCourt.Serialize();
        var newPetition=SandboxInheritanceNegotiationState.Parse(
            oldPetition);
        Check("saved family-consent record cannot invent native ownership",
            newPetition.Serialize()==oldPetition &&
            newPetition.Authorized &&
            newPetition.Complete(false)=="native_owner_not_changed" &&
            newPetition.Status=="ClanHeadConsents" &&
            newPetition.Complete(true)=="native_grant_confirmed" &&
            newPetition.Status=="NativeGrantCompleted");
        var mediationState=new SandboxInheritanceNegotiationState();
        mediationState.Petition("ClaimContested",
            "castle_s","head","clan","KindredCouncil",100);
        Check("real cultural family mediation needs native money and court",
            SandboxInheritanceNegotiationPolicy
                .FamilySettlementContribution("empire")==700 &&
            SandboxInheritanceNegotiationPolicy
                .FamilySettlementContribution("battania")==250 &&
            SandboxInheritanceNegotiationPolicy.MediationGate(
                "ClaimContested",0,true,true,true,500,"battania") &&
            !SandboxInheritanceNegotiationPolicy.MediationGate(
                "ClaimContested",0,false,true,true,500,"battania") &&
            !SandboxInheritanceNegotiationPolicy.MediationGate(
                "ClaimContested",2,true,true,true,500,"battania") &&
            mediationState.RecordMediation() &&
            mediationState.Mediations==1 &&
            SandboxInheritanceNegotiationState.Parse(
                mediationState.Serialize()).Mediations==1);
        Check("withdrawal closes a disputed family grant permanently",
            mediationState.Withdraw()=="ClaimWithdrawn" &&
            !mediationState.Authorized &&
            mediationState.Petition("ClanHeadConsents",
                "castle_s","head","clan","KindredCouncil",110)==
                "PetitionLimitReached" &&
            SandboxInheritanceNegotiationState.Parse(
                mediationState.Serialize()).Status=="ClaimWithdrawn");
        var affairsStress=SandboxLivingAffairsSimulation.Run(120,730);
        Console.WriteLine("LIVING AFFAIRS SIMULATION "+
            (affairsStress.Failures==0?"PASS":"FAIL")+
            " | seeds="+affairsStress.Seeds+
            " | days_per_seed="+affairsStress.Days+
            " | checks="+affairsStress.Checks+
            " | requests="+affairsStress.Requests+
            " | accepted="+affairsStress.Accepted+
            " | declined="+affairsStress.Declined+
            " | cultural_grants_authorized="+
                affairsStress.AuthorizedClaims+
            " | contested="+affairsStress.ContestedClaims+
            " | petition_refusals="+affairsStress.RefusedClaims+
            " | reloads="+affairsStress.Reloads+
            " | first_failure="+affairsStress.FirstFailure);
        Check("120 campaign seeds for two years preserve agency and estate gates",
            affairsStress.Failures==0 &&
            affairsStress.Seeds==120 &&
            affairsStress.Checks>100000 &&
            affairsStress.Requests>=200 &&
            affairsStress.AuthorizedClaims>0 &&
            affairsStress.ContestedClaims>0 &&
            affairsStress.Reloads>1000 &&
            affairsStress.NativeTransferCalls==0 &&
            affairsStress.UnguardedTransfers==0);
        var autoHouse=new SandboxOriginProfile();
        autoHouse.Role="Lord";
        SandboxAutoHousePlan.Prepare(autoHouse,"vlandia",43,false,"Cassian");
        Check("automatic House uses Vlandian names and valid child ages",
            autoHouse.AutoHouseNamesPrepared &&
            autoHouse.SpouseBackground=="NobleRandom" &&
            autoHouse.FamilyScenario=="Natural" &&
            autoHouse.SpouseName=="Aveline" &&
            autoHouse.Children.All(ch=>ch.Age>=0 &&
                ch.Age<=autoHouse.SpouseAge-16) &&
            autoHouse.Siblings.All(s=>s.Age>=18 && s.Age<=80) &&
            SandboxOriginProfile.Parse(autoHouse.Serialize())
                .AutoHouseNamesPrepared);
        Check("culture-specific names avoid duplicate household suggestions",
            SandboxCulturalNames.Suggest("sturgia",false,0,
                new string[0])=="Mstislav" &&
            SandboxCulturalNames.Suggest("aserai",true,0,
                new string[0])=="Zahra" &&
            SandboxCulturalNames.Suggest("vlandia",true,0,
                new[]{"Aveline"})=="Ysabel");
        Check("court and military retinues have distinct native roles",
            SandboxRetinueOfficePolicy.Skills("Knight")[0]=="Riding" &&
            SandboxRetinueOfficePolicy.Skills("HouseholdSteward")[0]=="Steward" &&
            SandboxRetinueOfficePolicy.PartyDuty("Navigator")=="Scout" &&
            SandboxRetinueOfficePolicy.PartyDuty("HouseholdSteward")=="Quartermaster" &&
            SandboxRetinueOfficePolicy.PartyDuty("Knight")=="None");
        Check("retinue cultures have distinct native outfit goals",
            SandboxRetinueCulturePolicy.GearStyle("vlandia","Knight")=="Lancer" &&
            SandboxRetinueCulturePolicy.GearStyle("khuzait","Knight")=="HorseArcher" &&
            SandboxRetinueCulturePolicy.GearStyle("battania","Knight")=="Longbow" &&
            SandboxRetinueCulturePolicy.GearStyle("sturgia","Knight")=="Axeguard" &&
            SandboxRetinueCulturePolicy.GearStyle("empire","Knight")=="Shieldguard" &&
            SandboxRetinueCulturePolicy.GearStyle("aserai","Knight")=="Skirmisher");
        Check("native equipment scorer rewards mounted archery, no fake item IDs",
            SandboxRetinueCulturePolicy.EquipmentScore("HorseArcher",
                new[]{"Horse","Bow","Arrows","BodyArmor"}) >
            SandboxRetinueCulturePolicy.EquipmentScore("HorseArcher",
                new[]{"Shield","OneHandedWeapon","BodyArmor"}) &&
            SandboxRetinueCulturePolicy.EquipmentScore("Shieldguard",
                new[]{"Shield","OneHandedWeapon","BodyArmor"}) >
            SandboxRetinueCulturePolicy.EquipmentScore("Shieldguard",
                new[]{"Horse","Bow","Arrows"}));
        Check("culture modifies officers' skills and personality",
            SandboxRetinueCulturePolicy.Training("khuzait","Knight")
                .Contains("Bow") &&
            SandboxRetinueCulturePolicy.Training("sturgia","Knight")
                .Contains("TwoHanded") &&
            SandboxRetinueCulturePolicy.PrimaryTrait("vlandia","Knight")=="Honor" &&
            SandboxRetinueCulturePolicy.PrimaryTrait("aserai","Knight")=="Generosity" &&
            SandboxRetinueCulturePolicy.PrimaryTrait("khuzait","Knight")=="Calculating" &&
            SandboxRetinueCulturePolicy.SecondaryTrait("aserai","Factor")=="Calculating");
        var family=new SandboxOriginProfile();
        Check("family starts natural and no old preset leaks",
            family.FamilyScenario=="Natural" &&
            family.Children.All(c=>c.Upbringing=="FamilyTradition"));
        Check("seven household stories and upbringing choices",
            SandboxFamilyDevelopmentPolicy.Scenarios.Length==7 &&
            SandboxFamilyDevelopmentPolicy.Upbringings.Length==7 &&
            SandboxFamilyDevelopmentPolicy.IsScenario("Honorable") &&
            !SandboxFamilyDevelopmentPolicy.IsScenario("Nonexistent"));
        Check("naval tradition affects training but ages stay bounded",
            SandboxFamilyDevelopmentPolicy.IsScenario("Seafaring") &&
            SandboxFamilyDevelopmentPolicy.IsUpbringing("Seafaring") &&
            SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                "Seafaring","FamilyTradition")=="Seafaring" &&
            SandboxFamilyDevelopmentPolicy.Training("Seafaring")
                .Contains("Mariner") &&
            SandboxFamilyDevelopmentPolicy.Training("Seafaring")
                .Contains("Boatswain") &&
            SandboxFamilyDevelopmentPolicy.Training("Seafaring")
                .Contains("Shipmaster") &&
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                4,330,330,true,9)==0);
        Check("native character level is bounded below XP sentinel",
            SandboxFamilyLevelPolicy.Valid(1) &&
            SandboxFamilyLevelPolicy.Valid(50) &&
            !SandboxFamilyLevelPolicy.Valid(0) &&
            !SandboxFamilyLevelPolicy.Valid(51) &&
            SandboxStatBudgetPolicy.InRange(
                SandboxFamilyLevelPolicy.Key,20) &&
            !SandboxStatBudgetPolicy.InRange(
                SandboxFamilyLevelPolicy.Key,55));
        Check("War Sails skills are accepted as native skill/focus keys",
            new[]{"Mariner","Boatswain","Shipmaster"}.All(n=>
                SandboxStatBudgetPolicy.IsStatKey("Skill."+n) &&
                SandboxStatBudgetPolicy.IsStatKey("Focus."+n)) &&
            SandboxOriginProfile.AllPotentialSkills.Length==21);
        var navalProfile=new SandboxOriginProfile();
        Check("spouse and player War Sails skills and levels roundtrip",
            navalProfile.TrySet("Skill.Mariner","75") &&
            navalProfile.TrySet("Focus.Boatswain","3") &&
            navalProfile.TrySet("Level.Character","25") &&
            navalProfile.TrySet("SpouseStat.Skill.Shipmaster","90") &&
            navalProfile.TrySet("SpouseStat.Level.Character","32") &&
            SandboxOriginProfile.Parse(navalProfile.Serialize())
                .Overrides["Level.Character"]==25 &&
            SandboxOriginProfile.Parse(navalProfile.Serialize())
                .SpouseOverrides["Level.Character"]==32 &&
            SandboxOriginProfile.Parse(navalProfile.Serialize())
                .SpouseOverrides["Skill.Shipmaster"]==90);
        Check("no invalid player or spouse level accepted",
            !navalProfile.TrySet("Level.Character","75") &&
            !navalProfile.TrySet("SpouseStat.Level.Character","-1"));
        Check("family story and upbringing persist in profile",
            family.TrySet("FamilyScenario","Honorable") &&
            family.TrySet("Child.1.Upbringing","Kind") &&
            SandboxOriginProfile.Parse(family.Serialize()).FamilyScenario==
                "Honorable" &&
            SandboxOriginProfile.Parse(family.Serialize())
                .Children[0].Upbringing=="Kind");
        Check("no unsupported upbringing or family story",
            !family.TrySet("FamilyScenario","Exploit") &&
            !family.TrySet("Child.1.Upbringing","Unknown") &&
            !family.TrySet("Sibling.1.Upbringing","Kind"));
        Check("new Sandbox reset clears household personalities",
            family.Children[0].Upbringing=="Kind" &&
            ResetAndCheck(family) &&
            family.FamilyScenario=="Natural" &&
            family.Children[0].Upbringing=="FamilyTradition");
        Check("honorable marriage and kind children's bias",
            SandboxFamilyDevelopmentPolicy.Bias(
                "Honorable","FamilyTradition","Honor",true)==2 &&
            SandboxFamilyDevelopmentPolicy.Bias(
                "Honorable","FamilyTradition","Mercy",false)==2 &&
            SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                "Honorable","FamilyTradition")=="Kind");
        Check("independent child upbringing overrides family default",
            SandboxFamilyDevelopmentPolicy.ResolveUpbringing(
                "Honorable","Martial")=="Martial" &&
            SandboxFamilyDevelopmentPolicy.Bias(
                "Honorable","Martial","Valor",false)==2);
        Check("children inherit from either parent and can differ",
            SandboxFamilyDevelopmentPolicy.PersonalityValue(
                2,-1,0,1,0,20,false)==2 &&
            SandboxFamilyDevelopmentPolicy.PersonalityValue(
                2,-1,0,31,0,20,false)==-1 &&
            SandboxFamilyDevelopmentPolicy.PersonalityValue(
                0,0,2,65,0,20,true)>=1);
        Check("adult child skills are age-appropriate and never zero",
            SandboxFamilyDevelopmentPolicy.SkillLevel(18,0,0)>=40 &&
            SandboxFamilyDevelopmentPolicy.SkillLevel(25,2,0)>20 &&
            SandboxFamilyDevelopmentPolicy.SkillLevel(16,1,0)>0 &&
            SandboxFamilyDevelopmentPolicy.SkillLevel(4,0,9)==0 &&
            SandboxFamilyDevelopmentPolicy.Training("Martial").Length>=5);
        Check("family relation story ranges include warm and strained",
            SandboxFamilyDevelopmentPolicy.SpouseRelationBase(
                "Honorable")>70 &&
            SandboxFamilyDevelopmentPolicy.ChildRelationBase(
                "Honorable")>60 &&
            SandboxFamilyDevelopmentPolicy.SpouseRelationBase(
                "Strained")<20 &&
            SandboxFamilyDevelopmentPolicy.ClampRelation(123)==100);
        var growth=new SandboxFamilyGrowthState();
        growth.SetScenario("Honorable");
        growth.Register("generated_child_a","Kind",10);
        Check("historical milestones not granted twice",
            growth.LastMilestone("generated_child_a")==10 &&
            !growth.TryAdvance("generated_child_a",10,out _) &&
            growth.TryAdvance("generated_child_a",14,out _) &&
            !growth.TryAdvance("generated_child_a",14,out _));
        var loaded=SandboxFamilyGrowthState.Parse(growth.Serialize());
        Check("upbringing survives campaign save/reload without replay",
            loaded.Scenario=="Honorable" &&
            loaded.Count==1 &&
            loaded.LastMilestone("generated_child_a")==14 &&
            !loaded.TryAdvance("generated_child_a",10,out _) &&
            loaded.TryAdvance("generated_child_a",18,out var future) &&
            future=="Kind");
        Check("only milestone ages can grant upbringing changes",
            !loaded.TryAdvance("generated_child_a",19,out _) &&
            SandboxFamilyDevelopmentPolicy.IsDevelopmentMilestone(18) &&
            !SandboxFamilyDevelopmentPolicy.IsDevelopmentMilestone(17));
        Check("stats screen limits active Gauntlet slider rows",
            SandboxStatsPagePolicy.VisibleRows==6 &&
            SandboxStatsPagePolicy.PageCount(54)==9 &&
            SandboxStatsPagePolicy.StartIndex(8,54)==48 &&
            SandboxStatsPagePolicy.PageSize(8,54)==6 &&
            SandboxStatsPagePolicy.PageSize(9,55)==1 &&
            SandboxStatsPagePolicy.ClampPage(500,55)==9 &&
            SandboxStatsPagePolicy.PageCount(0)==1);
        Check("single logical All Stats stage retains Age Back/Next",
            CUNativeStatsNavigationPolicy.PageCount==2 &&
            CUNativeStatsNavigationPolicy.NextPage(
                CUNativeStatsNavigationPolicy.Age)==
                CUNativeStatsNavigationPolicy.Attributes);
        Check("one page of all native stats after verified age control",
            CUNativeStatsNavigationPolicy.Age==-1 &&
            CUNativeStatsNavigationPolicy.Attributes==0 &&
            CUNativeStatsNavigationPolicy.PageCount==2 &&
            CUNativeStatsNavigationPolicy.IsFinal(
                CUNativeStatsNavigationPolicy.Attributes) &&
            !CUNativeStatsNavigationPolicy.IsFinal(
                CUNativeStatsNavigationPolicy.Age));
        Check("back from all stats returns to exact age",
            CUNativeStatsNavigationPolicy.BackPage(
                CUNativeStatsNavigationPolicy.Attributes)==
                CUNativeStatsNavigationPolicy.Age &&
            CUNativeStatsNavigationPolicy.BackToNativeNarrative(
                CUNativeStatsNavigationPolicy.Age));
        Check("age and all-stats page labels match actual transitions",
            CUNativeStatsNavigationPolicy.NextPage(-1)==0 &&
            CUNativeStatsNavigationPolicy.StepNumber(-1)==1 &&
            CUNativeStatsNavigationPolicy.StepNumber(0)==2 &&
            CUNativeStatsNavigationPolicy.NextLabel(-1)=="Next: All Stats" &&
            CUNativeStatsNavigationPolicy.NextLabel(0)=="Confirm & Continue");
        Check("stat editor never enters obsolete multi-page skill editor",
            !CUNativeStatsNavigationPolicy.IsValidPage(
                CUNativeStatsNavigationPolicy.Skills) &&
            !CUNativeStatsNavigationPolicy.IsValidPage(
                CUNativeStatsNavigationPolicy.Traits));
        Check("nineteen year old cannot inherit an adult's full skill",
            SandboxFamilySkillInheritancePolicy.AgeCap(19)==75 &&
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                19,330,330,true,9)<=75);
        Check("father's skilled one-handed training shapes child's aptitude",
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                19,270,30,true,2)>
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                19,0,30,true,2));
        Check("both parents influence child independently",
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                25,200,180,true,3)>
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                25,200,0,true,3));
        Check("preschoolers never inherit advanced combat ability",
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                4,330,330,true,9)==0 &&
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                8,330,330,true,9)<=12 &&
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                13,330,330,true,9)<=28);
        Check("offspring attributes respect age and both parents",
            SandboxFamilySkillInheritancePolicy.SuggestedAttribute(
                4,10,10,1)==1 &&
            SandboxFamilySkillInheritancePolicy.SuggestedAttribute(
                19,10,10,1)<=5 &&
            SandboxFamilySkillInheritancePolicy.SuggestedAttribute(
                25,10,10,1)<=7 &&
            SandboxFamilySkillInheritancePolicy.SuggestedAttribute(
                19,8,8,0)>
            SandboxFamilySkillInheritancePolicy.SuggestedAttribute(
                19,1,1,0));
        Check("young adults gain a broad, nonzero baseline",
            SandboxFamilySkillInheritancePolicy.SuggestedSkill(
                19,0,0,false,0)>=4 &&
            SandboxFamilySkillInheritancePolicy.IsYouth(19) &&
            !SandboxFamilySkillInheritancePolicy.IsYouth(25));
        var spousePlan=new SandboxOriginProfile();
        Check("spouse slider stats are independent from player overrides",
            spousePlan.TrySet("SpouseStat.Skill.OneHanded","175") &&
            spousePlan.TrySet("SpouseStat.Attribute.Vigor","5") &&
            spousePlan.SpouseOverrides["Skill.OneHanded"]==175 &&
            spousePlan.Overrides.Count==0);
        Check("spouse exact native stat values survive profile reload",
            SandboxOriginProfile.Parse(spousePlan.Serialize())
                .SpouseOverrides["Skill.OneHanded"]==175 &&
            SandboxOriginProfile.Parse(spousePlan.Serialize())
                .SpouseOverrides["Attribute.Vigor"]==5);
        Check("invalid spouse stat keys and values are refused",
            !spousePlan.TrySet("SpouseStat.Skill.Nonexistent","12") &&
            !spousePlan.TrySet("SpouseStat.Focus.OneHanded","13") &&
            !spousePlan.TrySet("SpouseStat.Trait.Mercy","8"));
        spousePlan.ResetForNewCampaign();
        Check("spouse edits do not leak into next Sandbox",
            spousePlan.SpouseOverrides.Count==0 &&
            spousePlan.Overrides.Count==0);
        Check("minor residence recorded is distinct from castle scene roster",
            SandboxFamilyResidencePolicy.Classify(true,true,false)==
                SandboxFamilyResidencePolicy.MinorResident &&
            SandboxFamilyResidencePolicy.Classify(false,true,false)==
                SandboxFamilyResidencePolicy.AdultUnplaced &&
            SandboxFamilyResidencePolicy.Classify(false,true,true)==
                SandboxFamilyResidencePolicy.AdultPresent);
        Check("female and male minors share the same age/state policy",
            SandboxFamilyResidencePolicy.HumanStatus(
                SandboxFamilyResidencePolicy.MinorResident).Contains("underage") &&
            !SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                true,true,false,false,false) &&
            !SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                true,true,true,false,false));
        Check("adult missing-resident and underage recall remain distinct",
            SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                false,true,true,false,false) &&
            !SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                false,true,false,false,false) &&
            !SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                false,true,true,false,true));

        Check("native age slider maps endpoints to 18 and 80",
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,120f,20f)==18 &&
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,580f,20f)==80);
        Check("native age slider midpoint and handle grip",
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,350f,20f)==49 &&
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,345f,15f)==49 &&
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,-500f,20f)==18 &&
            SandboxFamilyAgePolicy.AgeFromSliderPointer(
                100f,500f,40f,1500f,20f)==80);
        Check("exact birthday matches requested spouse age, not donor age",
            SandboxFamilyAgePolicy.MatchesRequestedAge(48.0f,48) &&
            SandboxFamilyAgePolicy.MatchesRequestedAge(48.03f,48) &&
            !SandboxFamilyAgePolicy.MatchesRequestedAge(22.25f,48) &&
            !SandboxFamilyAgePolicy.MatchesRequestedAge(float.NaN,48));
        Check("six offspring valid with age 50 lord and 48 year spouse",
            new[] {25,22,20,18,16,15}.All(a=>
                SandboxFamilyAgePolicy.CanChooseChildAge(a,50,48)));
        Check("40-year-old spouse rejects child age over 24",
            SandboxFamilyAgePolicy.ChildAgeMaximum(50,40)==24 &&
            !SandboxFamilyAgePolicy.CanChooseChildAge(25,50,40) &&
            SandboxFamilyAgePolicy.CanChooseChildAge(24,50,40));
        Check("four adult offspring older than 20 fit two parents",
            new[] {24,23,22,21,16,15}.All(a=>
                SandboxFamilyAgePolicy.CanChooseChildAge(a,50,40)));
        Check("starting character age permits each adult year 18 to 80",
            SandboxFamilyAgePolicy.ValidPlayerAge(18) &&
            SandboxFamilyAgePolicy.ValidPlayerAge(37) &&
            SandboxFamilyAgePolicy.ValidPlayerAge(80) &&
            !SandboxFamilyAgePolicy.ValidPlayerAge(17) &&
            !SandboxFamilyAgePolicy.ValidPlayerAge(81));
        Check("maximum child age follows younger actual parent, not age 30",
            SandboxFamilyAgePolicy.ChildAgeMaximum(35,30)==14 &&
            SandboxFamilyAgePolicy.ChildAgeMaximum(60,25)==9 &&
            SandboxFamilyAgePolicy.ChildAgeMaximum(50,60)==34);
        Check("child age never exceeds 55 or drops below 0",
            SandboxFamilyAgePolicy.ChildAgeMaximum(75,80)==55 &&
            SandboxFamilyAgePolicy.ChildAgeMaximum(18,18)==2 &&
            SandboxFamilyAgePolicy.ClampChildAge(28,20,40)==4 &&
            SandboxFamilyAgePolicy.ClampChildAge(-5,20,40)==0);
        Check("age eligibility is shown before parent-age validation",
            SandboxFamilyAgePolicy.CanChooseChildAge(10,26,31) &&
            !SandboxFamilyAgePolicy.CanChooseChildAge(11,26,31) &&
            SandboxFamilyAgePolicy.CanChooseChildAge(0,18,18));
        Check("skill levels reach 330 even though focus caps at five",
            SandboxStatBudgetPolicy.InRange("Skill.Riding",330) &&
            !SandboxStatBudgetPolicy.InRange("Skill.Riding",331) &&
            SandboxStatBudgetPolicy.InRange("Focus.Riding",5) &&
            !SandboxStatBudgetPolicy.InRange("Focus.Riding",6) &&
            SandboxStatBudgetPolicy.Step("Skill.Riding")==10);

        Check("native content alignment targets story clan review and difficulty",
            CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationNarrativeStage") &&
            CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationClanNamingStage") &&
            CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationReviewStage") &&
            CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationOptionsStage"));
        Check("native banner, FaceGen and culture panels are never moved",
            !CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationBannerEditorStage") &&
            !CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationFaceGeneratorStage") &&
            !CUNativeLayoutPolicy.CanMovePrimaryPanel(
                "CharacterCreationCultureStage"));
        Check("all five existing native stages can align real footer buttons",
            CUNativeLayoutPolicy.CanAlignNativeFooter(
                "CharacterCreationNarrativeStage") &&
            CUNativeLayoutPolicy.CanAlignNativeFooter(
                "CharacterCreationBannerEditorStage") &&
            CUNativeLayoutPolicy.CanAlignNativeFooter(
                "CharacterCreationClanNamingStage") &&
            CUNativeLayoutPolicy.CanAlignNativeFooter(
                "CharacterCreationReviewStage") &&
            CUNativeLayoutPolicy.CanAlignNativeFooter(
                "CharacterCreationOptionsStage"));
        Check("CU compact and wide native content left inset matches guide",
            CUNativeLayoutPolicy.ContentLeft(1366)==28 &&
            CUNativeLayoutPolicy.ContentLeft(1920)==260 &&
            CUNativeLayoutPolicy.ContentRight(1920)==1562 &&
            CUNativeLayoutPolicy.ContentBottom(1080)==987);
        float moveX,moveY;
        Check("native narrative content clears CU wide sidebar/header",
            CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationNarrativeStage",1920,1080,
                180,95,900,600,out moveX,out moveY) &&
            moveX==80 && moveY==21);
        Check("fitting native panels preserve their original position",
            !CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationNarrativeStage",1920,1080,
                300,130,900,600,out moveX,out moveY) &&
            moveX==0 && moveY==0);
        Check("native 1366 content receives only measured small corrections",
            CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationNarrativeStage",1366,768,
                20,60,830,500,out moveX,out moveY) &&
            moveX==8 && moveY==56);
        Check("native panel that cannot fit vertical area stays game-owned",
            !CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationNarrativeStage",1366,768,
                30,60,830,600,out moveX,out moveY));
        Check("wide content repositions away from right-hand CU context",
            CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationReviewStage",1920,1080,
                1300,150,500,500,out moveX,out moveY) &&
            moveX==-238 && moveY==0);
        Check("out of range large primary-panel movement is refused",
            !CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationReviewStage",1920,1080,
                0,130,900,500,out moveX,out moveY));
        Check("native scene and banner are never relocated by primary policy",
            !CUNativeLayoutPolicy.TryPlanPrimary(
                "CharacterCreationBannerEditorStage",1920,1080,
                180,95,900,600,out moveX,out moveY));
        Check("native Back footer matches CU left 29px and bottom 24px",
            CUNativeLayoutPolicy.TryPlanFooter(
                "CharacterCreationNarrativeStage",1920,1080,
                44,1000,170,50,out moveX,out moveY) &&
            moveX==-15 && moveY==6);
        Check("native Continue footer matches CU right 31px and bottom 24px",
            CUNativeLayoutPolicy.TryPlanFooter(
                "CharacterCreationOptionsStage",1920,1080,
                1700,1000,190,52,out moveX,out moveY) &&
            moveX==-1 && moveY==4);
        Check("central narrative controls never look like footer buttons",
            !CUNativeLayoutPolicy.TryPlanFooter(
                "CharacterCreationNarrativeStage",1920,1080,
                800,1000,190,52,out moveX,out moveY));
        Check("wildly displaced footer buttons are not relocated",
            !CUNativeLayoutPolicy.TryPlanFooter(
                "CharacterCreationNarrativeStage",1920,1080,
                210,1000,170,50,out moveX,out moveY));
        float labelLeft,labelRight;
        Check("original native buttons get symmetric CU text padding",
            CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationNarrativeStage",300,180,
                0,0,out labelLeft,out labelRight) &&
            labelLeft==12 && labelRight==12);
        Check("long localized native button labels are never clipped",
            !CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationNarrativeStage",300,290,
                0,0,out labelLeft,out labelRight));
        Check("larger game-owned native button margins are kept intact",
            !CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationNarrativeStage",300,180,
                14,14,out labelLeft,out labelRight));
        Check("native banner text labels can match CU spacing without moving artwork",
            CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationBannerEditorStage",260,180,
                2,2,out labelLeft,out labelRight) &&
            labelLeft==12 && labelRight==12);
        Check("culture and FaceGen labels retain original native placement",
            !CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationFaceGeneratorStage",260,180,
                0,0,out labelLeft,out labelRight) &&
            !CUNativeLayoutPolicy.TryPlanLabelPadding(
                "CharacterCreationCultureStage",260,180,
                0,0,out labelLeft,out labelRight));
        Check("native root must match the physical unscaled viewport",
            CUNativeLayoutPolicy.RootMatchesResolution(1920,1080,1920,1080) &&
            !CUNativeLayoutPolicy.RootMatchesResolution(
                1200,675,1920,1080));
        Check("smaller and invalid resolutions fail without layout changes",
            !CUNativeLayoutPolicy.IsViewportSupported(1024,768) &&
            !CUNativeLayoutPolicy.IsViewportSupported(1366,600) &&
            !CUNativeLayoutPolicy.RootMatchesResolution(
                Single.NaN,1080,1920,1080));

        Check("native skin targets game-owned story/house/legacy stages",
            CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationNarrativeStage") &&
            CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationBannerEditorStage") &&
            CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationClanNamingStage") &&
            CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationReviewStage") &&
            CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationOptionsStage"));
        Check("native skin never duplicates culture or modifies FaceGen",
            !CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationCultureStage") &&
            !CUNativeWidgetSkinPolicy.CanSkin(
                "CharacterCreationFaceGeneratorStage"));
        Check("native skin does not target unknown other-mod stages",
            !CUNativeWidgetSkinPolicy.CanSkin("OtherModStage") &&
            CUNativeWidgetSkinPolicy.ExpectedView("OtherModStage")=="");
        Check("native skin binds exact 1.5.7 native narrative view",
            CUNativeWidgetSkinPolicy.ExpectedView(
                "CharacterCreationNarrativeStage")==
                "CharacterCreationNarrativeStageView");
        Check("native skin binds exact 1.5.7 native banner view",
            CUNativeWidgetSkinPolicy.ExpectedView(
                "CharacterCreationBannerEditorStage")==
                "CharacterCreationBannerEditorView");
        Check("native skin binds exact 1.5.7 native clan view",
            CUNativeWidgetSkinPolicy.ExpectedView(
                "CharacterCreationClanNamingStage")==
                "CharacterCreationClanNamingStageView");
        Check("native skin binds real review and options view",
            CUNativeWidgetSkinPolicy.ExpectedView(
                "CharacterCreationReviewStage")==
                "CharacterCreationReviewStageView" &&
            CUNativeWidgetSkinPolicy.ExpectedView(
                "CharacterCreationOptionsStage")==
                "CharacterCreationOptionsStageView");
        Check("native skin protects 3D and banner imagery",
            CUNativeWidgetSkinPolicy.IsProtectedWidget("SceneWidget") &&
            CUNativeWidgetSkinPolicy.IsProtectedWidget("ImageIdentifierWidget") &&
            CUNativeWidgetSkinPolicy.IsProtectedWidget("BannerWidget") &&
            CUNativeWidgetSkinPolicy.IsProtectedWidget("SliderWidget"));
        Check("native skin protects editable clan inputs",
            CUNativeWidgetSkinPolicy.IsProtectedWidget("EditableTextWidget") &&
            CUNativeWidgetSkinPolicy.IsProtectedWidget("InputTextWidget"));
        Check("native skin accepts native button and text widgets",
            !CUNativeWidgetSkinPolicy.IsProtectedWidget("ButtonWidget") &&
            !CUNativeWidgetSkinPolicy.IsProtectedWidget("TextWidget"));
        Check("native clan naming receives local input color without input mutations",
            CUNativeWidgetSkinPolicy.CanSkinClanTextInput(
                "CharacterCreationClanNamingStage","EditableTextWidget") &&
            CUNativeWidgetSkinPolicy.CanSkinClanTextInput(
                "CharacterCreationClanNamingStage","InputTextWidget") &&
            !CUNativeWidgetSkinPolicy.CanSkinClanTextInput(
                "CharacterCreationBannerEditorStage","EditableTextWidget"));
        Check("native difficulty sliders receive local slider brush only",
            CUNativeWidgetSkinPolicy.CanSkinDifficultySlider(
                "CharacterCreationOptionsStage","SliderWidget") &&
            !CUNativeWidgetSkinPolicy.CanSkinDifficultySlider(
                "CharacterCreationFaceGeneratorStage","SliderWidget") &&
            !CUNativeWidgetSkinPolicy.CanSkinDifficultySlider(
                "CharacterCreationBannerEditorStage","SliderWidget"));
        Check("native skin uses parchment-gold text and buttons",
            CUNativeWidgetSkinPolicy.TextColor(
                "CharacterCreationNarrativeStage",false,true)=="#F6D7A5FF" &&
            CUNativeWidgetSkinPolicy.TextColor(
                "CharacterCreationNarrativeStage",true,false)=="#F1DEBEFF" &&
            CUNativeWidgetSkinPolicy.ButtonTint(
                "CharacterCreationOptionsStage")=="#D7B17AFF");
        Check("unsupported native stages receive no restyling color",
            CUNativeWidgetSkinPolicy.TextColor(
                "CharacterCreationFaceGeneratorStage",false,false)=="" &&
            CUNativeWidgetSkinPolicy.ButtonTint(
                "OtherModStage")=="");
        Check("large native trees fail closed before expensive skinning",
            CUNativeWidgetSkinPolicy.MaxWidgetsPerStage>100 &&
            CUNativeWidgetSkinPolicy.MaxWidgetsPerStage<=800);

        Check("all seven real native stages have unified visual metadata",
            Array.TrueForAll(NativeCreatorParityPolicy.RequiredNativeStages,
                name=>CUNativeVisualPolicy.IsNativeChapterStage(name) &&
                    CUNativeVisualPolicy.StageTitle(name)!="CHARACTER CREATION" &&
                    CUNativeVisualPolicy.StageGuidance(name).Length>30));
        Check("native culture and FaceGen use dedicated visual surfaces",
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationCultureStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationFaceGeneratorStage"));
        Check("native original titles remain unoccluded by CU duplicate chrome",
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationNarrativeStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationBannerEditorStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationClanNamingStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationReviewStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "CharacterCreationOptionsStage"));
        Check("compact 1366 leaves original native controls unobscured",
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationNarrativeStage",1366) &&
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationOptionsStage",1366));
        Check("native full-screen stages never receive a covering CU rail",
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationNarrativeStage",1920) &&
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationReviewStage",1920));
        Check("native 3D FaceGen never gets an opaque chapter rail",
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationFaceGeneratorStage",1920));
        Check("native culture never gets a second overlapping chrome",
            !CUNativeVisualPolicy.ShouldShowChapterRail(
                "CharacterCreationCultureStage",1920));
        Check("unknown mod-added stage is left game-owned and untouched",
            !CUNativeVisualPolicy.IsNativeChapterStage(
                "AnotherModExtraCharacterStage") &&
            !CUNativeVisualPolicy.IsStandaloneNativeChrome(
                "AnotherModExtraCharacterStage"));
        Check("origin chapter matches actual game story before stats",
            CUNativeVisualPolicy.StepProgress(
                "CharacterCreationNarrativeStage").Contains("LIFE STORY") &&
            CUNativeVisualPolicy.NextHint(
                "CharacterCreationNarrativeStage").Contains("STARTING STATS"));
        Check("clan naming precedes guided house and fortune",
            CUNativeVisualPolicy.NextHint(
                "CharacterCreationClanNamingStage").Contains(
                    "HOUSEHOLD & FORTUNE"));
        Check("native review and difficulty not hardcoded to one order",
            CUNativeVisualPolicy.NextHint(
                "CharacterCreationReviewStage").Contains("NATIVE") &&
            CUNativeVisualPolicy.NextHint(
                "CharacterCreationOptionsStage").Contains(
                    "NATIVE FINALIZATION"));
        Check("native visual chapter label uses real five-chapter order",
            CUNativeVisualPolicy.ChapterPosition(
                "CharacterCreationNarrativeStage")=="CHAPTER 2 OF 5" &&
            CUNativeVisualPolicy.ChapterPosition(
                "CharacterCreationBannerEditorStage")=="CHAPTER 3 OF 5" &&
            CUNativeVisualPolicy.ChapterPosition(
                "CharacterCreationOptionsStage")=="CHAPTER 5 OF 5");

        Check("starting stats Native is the default for every new profile",
            new SandboxOriginProfile().ProgressionMode=="Native");
        Check("stat modes are exactly Native Balanced and Unrestricted",
            SandboxStatBudgetPolicy.IsMode("Native") &&
            SandboxStatBudgetPolicy.IsMode("Balanced") &&
            SandboxStatBudgetPolicy.IsMode("Unrestricted") &&
            !SandboxStatBudgetPolicy.IsMode("Unlimited"));
        Check("all six native attributes supported",
            SandboxOriginProfile.Attributes.Length==6 &&
            SandboxStatBudgetPolicy.IsStatKey("Attribute.Vigor") &&
            SandboxStatBudgetPolicy.IsStatKey("Attribute.Intelligence"));
        Check("all 18 skills have native attribute groups",
            SandboxOriginProfile.Skills.Length==18 &&
            System.Array.TrueForAll(SandboxOriginProfile.Skills,
                name=>SandboxStatBudgetPolicy.GroupForSkill(name)!=""));
        Check("skill and focus are independent native values",
            SandboxStatBudgetPolicy.IsStatKey("Skill.OneHanded") &&
            SandboxStatBudgetPolicy.IsStatKey("Focus.OneHanded"));
        Check("traits use native -2 to 2 ranges",
            SandboxStatBudgetPolicy.InRange("Trait.Honor",-2) &&
            SandboxStatBudgetPolicy.InRange("Trait.Honor",2) &&
            !SandboxStatBudgetPolicy.InRange("Trait.Honor",3));
        Check("attribute and focus limits enforced",
            !SandboxStatBudgetPolicy.InRange("Attribute.Vigor",0) &&
            !SandboxStatBudgetPolicy.InRange("Attribute.Vigor",11) &&
            !SandboxStatBudgetPolicy.InRange("Focus.Bow",6));
        Check("balanced attributes cost four points per increase",
            SandboxStatBudgetPolicy.Cost("Attribute.Vigor",3,5)==8);
        Check("balanced focus costs three points per increase",
            SandboxStatBudgetPolicy.Cost("Focus.Bow",1,3)==6);
        Check("balanced skills cost one point per ten levels",
            SandboxStatBudgetPolicy.Cost("Skill.Bow",30,60)==3);
        Check("balanced traits cost three points per change",
            SandboxStatBudgetPolicy.Cost("Trait.Honor",0,-2)==6);
        Check("lowering attributes and skills cannot refund budget",
            SandboxStatBudgetPolicy.Cost("Attribute.Vigor",5,2)==0 &&
            SandboxStatBudgetPolicy.Cost("Skill.Bow",70,0)==0);
        var nativeBaseline=new System.Collections.Generic.Dictionary<string,int>
        {
            {"Attribute.Vigor",3},{"Skill.Bow",30},
            {"Focus.Bow",1},{"Trait.Honor",0}
        };
        var chosenStats=new System.Collections.Generic.Dictionary<string,int>
        {
            {"Attribute.Vigor",5},{"Skill.Bow",60},
            {"Focus.Bow",3},{"Trait.Honor",1}
        };
        int usedPoints;
        string statsProblem;
        Check("balanced valid changes use exact point cost",
            SandboxStatBudgetPolicy.TryEvaluate("Balanced",chosenStats,
                nativeBaseline,out usedPoints,out statsProblem) &&
            usedPoints==20);
        chosenStats["Attribute.Vigor"]=10;
        Check("balanced allocations over 30 points rejected",
            !SandboxStatBudgetPolicy.TryEvaluate("Balanced",chosenStats,
                nativeBaseline,out usedPoints,out statsProblem) &&
            statsProblem.Contains("exceeds"));
        Check("unrestricted permits legal values beyond budget",
            SandboxStatBudgetPolicy.TryEvaluate("Unrestricted",chosenStats,
                nativeBaseline,out usedPoints,out statsProblem) &&
            usedPoints==0);
        chosenStats["Trait.Honor"]=9;
        Check("unrestricted still rejects illegal native values",
            !SandboxStatBudgetPolicy.TryEvaluate("Unrestricted",chosenStats,
                nativeBaseline,out usedPoints,out statsProblem));
        Check("native mode ignores residual draft overrides",
            SandboxStatBudgetPolicy.TryEvaluate("Native",chosenStats,
                nativeBaseline,out usedPoints,out statsProblem) &&
            usedPoints==0);
        var storedStats=new SandboxOriginProfile();
        Check("profile roundtrips chosen stat mode",
            storedStats.TrySet("ProgressionMode","Balanced") &&
            SandboxOriginProfile.Parse(storedStats.Serialize())
                .ProgressionMode=="Balanced");
        storedStats.TrySet("Skill.Bow","80");
        Check("draft override exists before new-campaign reset",
            storedStats.Overrides.Count==1);
        storedStats.ResetForNewCampaign();
        Check("fresh campaign clears stat changes and uses native",
            storedStats.ProgressionMode=="Native" &&
            storedStats.Overrides.Count==0);
        var newFounder=new SandboxOriginProfile();
        newFounder.TrySet("SpouseEnabled","true");
        newFounder.TrySet("SpouseAge","50");
        newFounder.TrySet("ChildCount","1");
        newFounder.TrySet("Child.1.Age","19");
        string founderAgeError;
        Check("family validity checks the actual player age",
            !newFounder.IsValid(30,out founderAgeError) &&
            newFounder.IsValid(45,out founderAgeError));
        var savedFounder=System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "CU-Guided-Family-"+Guid.NewGuid().ToString("N"),"profile.ini");
        try
        {
            Check("post-identity save uses real founder age not age 30",
                newFounder.SaveForPlayer(savedFounder,45,
                    out founderAgeError) &&
                SandboxOriginProfile.Load(savedFounder).ChildCount==1 &&
                SandboxOriginProfile.Load(savedFounder).Children[0].Age==19);
        }
        finally
        {
            var dir=System.IO.Path.GetDirectoryName(savedFounder);
            if(System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir,true);
        }
        var stale=new SandboxOriginProfile();
        stale.TrySet("Gold","50000");
        stale.TrySet("Influence","200");
        stale.TrySet("Troops","10");
        stale.TrySet("WorldSeed","44");
        stale.TrySet("StartFastMode","true");
        stale.TrySet("Item.1","grain");
        stale.ResetForNewCampaign();
        Check("fresh guided start clears hidden resource/item grants",
            stale.Gold==-1 && stale.Influence==-1 &&
            stale.Troops==0 && stale.ItemIds.Count==0 &&
            stale.WorldSeed==-1 && !stale.StartFastMode);
        Check("nonfamily creator remains independent of native clan",
            !stale.SpouseEnabled && stale.ChildCount==0 &&
            stale.StartingFiefMode=="Native" &&
            !stale.AllowStartingFiefTransfer);
        Check("all five traits have effect descriptions",
            System.Array.TrueForAll(SandboxOriginProfile.Traits,
                name=>SandboxStatBudgetPolicy.TraitDescription(name).Length>48));

        Check("Native FaceGen right dock only attempts wide-enough screen",
            CUNativeViewportPolicy.CanTryNativeFaceDock(1366,768) &&
            CUNativeViewportPolicy.CanTryNativeFaceDock(1920,1080) &&
            !CUNativeViewportPolicy.CanTryNativeFaceDock(1024,768) &&
            !CUNativeViewportPolicy.CanTryNativeFaceDock(1366,700));
        Check("Native FaceGen right dock leaves half screen at 1366",
            CUNativeViewportPolicy.NativeFaceDockRightFraction(1366)>.48f);
        Check("Native FaceGen right dock grants wide model viewport at 1920",
            CUNativeViewportPolicy.NativeFaceDockRightFraction(1920)>.63f);
        Check("Native FaceGen right-dock helper is guarded against zero width",
            CUNativeViewportPolicy.NativeFaceDockLeftFraction(0)==0f);
        Check("Native right-dock experiment is not falsely treated as a completed permanent scene",
            !CUNativeViewportPolicy.HasVerifiedRectangularSceneDock);
        Check("Native FaceGen dock defaults off in new Sandbox",
            !new SandboxOriginProfile().NativeFaceDockTrialEnabled);
        Check("Native FaceGen right-dock flag survives strict serialization",
            SandboxOriginProfile.Parse("NativeFaceDockTrialEnabled=true\n")
                .NativeFaceDockTrialEnabled);
        Check("CU five chapters keep world before identity and household",
            CUNativeChapterPolicy.Chapters.Length==5 &&
            CUNativeChapterPolicy.Chapters[0]=="01  YOUR WORLD" &&
            CUNativeChapterPolicy.Chapters[1]=="02  YOUR CHARACTER" &&
            CUNativeChapterPolicy.Chapters[2]=="03  YOUR HOUSE" &&
            CUNativeChapterPolicy.Chapters[3]=="04  YOUR FORTUNE" &&
            CUNativeChapterPolicy.Chapters[4]=="05  BEGIN YOUR LEGACY");
        Check("CU native face is part of Your Character",
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationFaceGeneratorStage")==1);
        Check("CU native culture and life story share Your Character",
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationCultureStage")==1 &&
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationNarrativeStage")==1);
        Check("CU native banner and clan share Your House",
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationBannerEditorStage")==2 &&
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationClanNamingStage")==2);
        Check("CU native difficulty remains final",
            CUNativeChapterPolicy.ActiveChapter("CharacterCreationOptionsStage")==4);
        Check("CU pregame World and post-clan Fortune require distinct lifecycles",
            !CUNativeChapterPolicy.IsLiveNativeChoice(0) &&
            !CUNativeChapterPolicy.IsLiveNativeChoice(3) &&
            CUNativeChapterPolicy.ChapterStatus(0,1,false)=="WORLD CONFIGURED" &&
            CUNativeChapterPolicy.ChapterStatus(3,1,false)=="AFTER NATIVE CLAN");
        Check("CU native tracker does not skip game callbacks",
            CUNativeChapterPolicy.ChapterStatus(2,2,true)=="NATIVE STEP ACTIVE");
        Check("CU 1366 layout leaves actual model viewport room",
            CUNativeViewportPolicy.IsUsableLayout(1366,768));
        Check("CU 1920 viewport target is large",
            CUNativeViewportPolicy.IsUsableLayout(1920,1080) &&
            CUNativeViewportPolicy.ProposedViewportWidth(1920)>=1100);
        Check("native 3D scene crop is unverified and cannot claim completed dock",
            !CUNativeViewportPolicy.HasVerifiedRectangularSceneDock &&
            CUNativeViewportPolicy.CapabilityStatus().Contains("NOT YET VERIFIED"));
        Check("native CU shell maps culture to Your Character",
            NativeStateShellPolicy.ChapterForStage(
                "CharacterCreationCultureStage") == "02  YOUR CHARACTER");
        Check("native CU shell maps real editable face to Your Character",
            NativeStateShellPolicy.ChapterForStage(
                "CharacterCreationFaceGeneratorStage") == "02  YOUR CHARACTER");
        Check("native CU shell maps narrative to Your Character",
            NativeStateShellPolicy.ChapterForStage(
                "CharacterCreationNarrativeStage") == "02  YOUR CHARACTER");
        Check("native CU shell maps banner to Your House",
            NativeStateShellPolicy.ChapterForStage(
                "CharacterCreationBannerEditorStage") == "03  YOUR HOUSE");
        Check("native CU shell maps options to Begin Your Legacy",
            NativeStateShellPolicy.ChapterForStage(
                "CharacterCreationOptionsStage") == "05  BEGIN YOUR LEGACY");
        Check("native CU shell knows live face stage only",
            NativeStateShellPolicy.UsesNativeLiveFace(
                "CharacterCreationFaceGeneratorStage") &&
            !NativeStateShellPolicy.UsesNativeLiveFace(
                "CharacterCreationCultureStage"));
        Check("native CU shell is off by default",
            !NativeStateShellPolicy.CanAttach(false,true,true,true,false,false));
        Check("native CU shell refuses non-CU campaign",
            !NativeStateShellPolicy.CanAttach(true,false,true,true,false,false));
        Check("native CU shell refuses outside native game state",
            !NativeStateShellPolicy.CanAttach(true,true,false,true,false,false));
        Check("native CU shell refuses native face workshop collision",
            !NativeStateShellPolicy.CanAttach(true,true,true,true,true,false));
        Check("native CU shell refuses extra review stage collision",
            !NativeStateShellPolicy.CanAttach(true,true,true,true,false,true));
        Check("native CU shell permits only explicit native-state opt-in",
            NativeStateShellPolicy.CanAttach(true,true,true,true,false,false));

        Check("native creator requires all seven game-owned stages",
            NativeCreatorParityPolicy.MissingStages(
                NativeCreatorParityPolicy.RequiredNativeStages).Length == 0);
        Check("native creator rejects absent face generator",
            Array.IndexOf(NativeCreatorParityPolicy.MissingStages(new[] {
                "CharacterCreationCultureStage", "CharacterCreationOptionsStage"}),
                "CharacterCreationFaceGeneratorStage") >= 0);
        Check("experimental face workshop refuses missing native stages",
            !NativeCreatorParityPolicy.CanAddFaceWorkshop(true, true,
                false, true, new[] { "CharacterCreationCultureStage" }));
        Check("experimental face workshop refuses conflicting review",
            !NativeCreatorParityPolicy.CanAddFaceWorkshop(true, true,
                true, true, NativeCreatorParityPolicy.RequiredNativeStages));
        Check("experimental face workshop refuses missing view mapping",
            !NativeCreatorParityPolicy.CanAddFaceWorkshop(true, true,
                false, false, NativeCreatorParityPolicy.RequiredNativeStages));
        Check("experimental face workshop refuses default-off mode",
            !NativeCreatorParityPolicy.CanAddFaceWorkshop(false, true,
                false, true, NativeCreatorParityPolicy.RequiredNativeStages));
        Check("experimental face workshop permits verified opt-in parity",
            NativeCreatorParityPolicy.CanAddFaceWorkshop(true, true,
                false, true, NativeCreatorParityPolicy.RequiredNativeStages));

        Check("phase-9 grants accept a verified native spouse household",
            SandboxStartContract.PostFamilyGrantGate(true,
                "spouse=verified_hero|children=2|siblings=1",
                true,false,"independent")=="");
        Check("resource-only starts do not require fabricated family",
            SandboxStartContract.PostFamilyGrantGate(false,
                "not_enabled",true,false,"independent")=="");
        Check("partial native family never unlocks starting fief or gold",
            SandboxStartContract.PostFamilyGrantGate(true,
                "partial:InvalidOperationException:children=1/2",
                true,false,"independent")=="family_creation_incomplete" &&
            SandboxStartContract.PostFamilyGrantGate(true,
                "refused:native_marriage",true,false,"independent")==
                    "family_creation_incomplete" &&
            SandboxStartContract.PostFamilyGrantGate(true,
                "failed:family_reference_mismatch",true,false,"independent")==
                    "family_creation_incomplete" &&
            SandboxStartContract.PostFamilyGrantGate(true,
                "not_enabled",true,false,"independent")==
                    "family_creation_incomplete");
        Check("changed noble birth rank blocks late custom grants",
            SandboxStartContract.PostFamilyGrantGate(true,
                "spouse=none|children=0|siblings=2",
                false,true,"bound:father_x")==
                    "noble_birth_rank_changed_after_house");
        Check("missing native noble family stops fief and retinue",
            SandboxStartContract.PostFamilyGrantGate(false,
                "not_enabled",true,true,"refused:no_valid_native_father")==
                    "native_noble_lineage_not_bound" &&
            SandboxStartContract.PostFamilyGrantGate(false,
                "not_enabled",true,true,"bound:lord_x")=="");

        Check("start contract accepts genuine Vlandian Lord with fief and spouse",
            SandboxStartContract.Validate("vassal","vassal","vlandia",
                "vlandia",false,1,true,true,0,0).Length == 0);
        Check("start contract refuses fake vanilla Lord",
            Array.IndexOf(SandboxStartContract.Validate("vassal","default",
                "vlandia","",false,0,false,false,0,0),
                "native_start_type_mismatch") >= 0);
        Check("start contract refuses wrong kingdom",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","sturgia",false,1,false,false,0,0),
                "starting_kingdom_mismatch") >= 0);
        Check("start contract refuses fake king without ruling clan",
            Array.IndexOf(SandboxStartContract.Validate("king","king",
                "vlandia","vlandia",false,2,false,false,0,0),
                "ruler_status_or_fief_missing") >= 0);
        Check("start contract refuses missing vassal fief",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,0,false,false,0,0),
                "vassal_status_or_fief_missing") >= 0);
        Check("start contract refuses spouse fields without real hero",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,1,true,false,0,0),
                "requested_spouse_missing") >= 0);
        Check("start contract refuses missing requested children",
            Array.IndexOf(SandboxStartContract.Validate("vassal","vassal",
                "vlandia","vlandia",false,1,true,true,2,1),
                "requested_children_missing") >= 0);
        Check("start contract accepts independent Wanderer",
            SandboxStartContract.Validate("default","default","","",
                false,0,false,false,0,0).Length == 0);

        Check("fief native default needs no transfer",
            SandboxFiefPolicy.ValidateRequest("Wanderer", "Native",
                "", false) == "");
        Check("fief opt-in requires real native political role",
            SandboxFiefPolicy.ValidateRequest("Wanderer", "Castle",
                "", true) == "fief_role_ineligible");
        Check("fief transfer refuses no consent",
            SandboxFiefPolicy.ValidateRequest("Lord", "Specific",
                "castle_test", false) == "fief_transfer_not_authorized");
        Check("fief specific requires a settlement id",
            SandboxFiefPolicy.ValidateRequest("King", "Specific",
                "", true) == "fief_id_required");
        Check("fief grant accepts kingdom castle request",
            SandboxFiefPolicy.ValidateRequest("Lord", "Castle",
                "", true) == "");
        Check("fief grant blocks enemy fief",
            SandboxFiefPolicy.ValidateTarget(true, false, false,
                false, false) == "fief_outside_starting_kingdom");
        Check("fief grant blocks sieged settlement",
            SandboxFiefPolicy.ValidateTarget(true, true, true,
                false, false) == "fief_contested");
        Check("fief grant blocks villages",
            SandboxFiefPolicy.ValidateTarget(false, true, false,
                false, false) == "not_a_fortification");
        Check("fief round-trip keeps exact settlement choice",
            new SandboxOriginProfile().TrySet("StartingFiefMode","Specific") &&
            SandboxOriginProfile.Parse("StartingFiefMode=Specific\n" +
                "StartingFiefId=castle_x\nAllowStartingFiefTransfer=true")
                .StartingFiefId == "castle_x");
        Check("legacy display culture Vlandian maps to real game ID",
            SandboxFamilyOriginPolicy.NormalizeCultureId("Vlandian") == "vlandia" &&
            SandboxFamilyOriginPolicy.NormalizeCultureId("vlandia") == "vlandia");
        Check("legacy Empire display maps to real empire culture",
            SandboxFamilyOriginPolicy.NormalizeCultureId("Imperial") == "empire");
        Check("generated spouse background defaults commoner",
            new SandboxOriginProfile().SpouseBackground == "Commoner");
        Check("generated noble random and chosen modes validate",
            SandboxFamilyOriginPolicy.Validate("NobleRandom","",true) == "" &&
            SandboxFamilyOriginPolicy.Validate("NobleChosen","clan_vlandia_1",true) == "");
        Check("chosen noble birth clan requires exact id",
            SandboxFamilyOriginPolicy.Validate("NobleChosen","",true) ==
                "spouse_noble_clan_required");
        Check("invalid generated social background rejected",
            SandboxFamilyOriginPolicy.Validate("Invented","",true) ==
                "unknown_spouse_background");
        Check("noble parent ages and marriage required",
            SandboxFamilyOriginPolicy.CanBeNobleParents(55,48,30,true,true) &&
            !SandboxFamilyOriginPolicy.CanBeNobleParents(45,47,30,true,true) &&
            !SandboxFamilyOriginPolicy.CanBeNobleParents(55,48,30,false,true));
        Check("social background survives profile save and reload",
            SandboxOriginProfile.Parse("SpouseEnabled=true\n"+
                "SpouseBackground=NobleChosen\n"+
                "SpouseBirthClanId=clan_x\nSpouseCulture=Vlandian")
                .SpouseCulture == "vlandia" &&
            SandboxOriginProfile.Parse("SpouseEnabled=true\n"+
                "SpouseBackground=NobleChosen\n"+
                "SpouseBirthClanId=clan_x").SpouseBirthClanId == "clan_x");
        Check("active marriageable generated couple passes preflight",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,true,true) == "");
        Check("family home defaults to a safe owned fief",
            new SandboxOriginProfile().FamilyHomeMode=="OwnedFief");
        Check("family home refuses unknown mode",
            SandboxFamilyHomePolicy.ValidateRequest("UnknownMode","",true) ==
                "family_home_mode_invalid");
        Check("no fixed home is a supported explicit option",
            SandboxFamilyHomePolicy.ValidateRequest("None","",true) == "" &&
            SandboxFamilyHomePolicy.ValidateRequest("None","",false) == "" &&
            SandboxOriginProfile.Parse("SpouseEnabled=true\n"+
                "FamilyHomeMode=None").FamilyHomeMode=="None");
        Check("requested family home requires confirmed native placement",
            SandboxFamilyHomePolicy.ValidateObservedPlacement(
                "OwnedFief",true,"placed:castle_vlandia_1:children=2")=="" &&
            SandboxFamilyHomePolicy.ValidateObservedPlacement(
                "OwnedFief",true,"partial:castle_vlandia_1:children=1")==
                    "requested_family_home_unfulfilled" &&
            SandboxFamilyHomePolicy.ValidateObservedPlacement(
                "SpecificOwned",true,"skipped:no_safe_owned_town_or_castle")==
                    "requested_family_home_unfulfilled" &&
            SandboxFamilyHomePolicy.ValidateObservedPlacement(
                "None",true,"not_requested:no_fixed_home")=="" &&
            SandboxFamilyHomePolicy.ValidateObservedPlacement(
                "OwnedFief",false,"skipped:no_registered_player_family")=="");
        Check("family home exact settlement requires ID",
            SandboxFamilyHomePolicy.ValidateRequest("SpecificOwned","",true) ==
                "family_home_id_required");
        var incompleteFamilyHome = new SandboxOriginProfile();
        incompleteFamilyHome.TrySet("SpouseEnabled","true");
        incompleteFamilyHome.TrySet("FamilyHomeMode","SpecificOwned");
        Check("family residence mode can be saved before ID entry",
            incompleteFamilyHome.IsValid(30,out var incompleteHomeError));
        Check("family residence must be complete at final confirmation",
            SandboxFamilyHomePolicy.ValidateRequest(
                incompleteFamilyHome.FamilyHomeMode,
                incompleteFamilyHome.FamilyHomeId,
                incompleteFamilyHome.SpouseEnabled) ==
                "family_home_id_required");
        Check("family home accepts owned unbesieged fortified destination",
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,false,false)=="");
        Check("family home refuses cross-clan siege or village",
            SandboxFamilyHomePolicy.ValidateSettlement(false,true,false,false) ==
                "not_player_clan_fief" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,true,false) ==
                "settlement_under_siege" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,false,false,false) ==
                "not_castle_or_town" &&
            SandboxFamilyHomePolicy.ValidateSettlement(true,true,false,true) ==
                "settlement_in_battle");
        Check("a baby remains a minor without party after homing",
            SandboxFamilyHomePolicy.CanPlaceMinor(true,true,true,true) &&
            !SandboxFamilyHomePolicy.CanPlaceMinor(true,true,true,false) &&
            !SandboxFamilyHomePolicy.CanPlaceMinor(true,true,false,true));
        var familyHomeProfile = new SandboxOriginProfile();
        familyHomeProfile.TrySet("FamilyHomeMode","SpecificOwned");
        familyHomeProfile.TrySet("FamilyHomeId","castle_V1");
        familyHomeProfile.TrySet("SpouseEnabled","true");
        Check("family residence selected castle survives serialize",
            SandboxOriginProfile.Parse(familyHomeProfile.Serialize())
                .FamilyHomeMode=="SpecificOwned" &&
            SandboxOriginProfile.Parse(familyHomeProfile.Serialize())
                .FamilyHomeId=="castle_V1");
        familyHomeProfile.ResetForNewCampaign();
        Check("new campaign resets family home choice",
            familyHomeProfile.FamilyHomeMode=="OwnedFief" &&
            familyHomeProfile.FamilyHomeId=="");
        var landlessHouse=new SandboxOriginProfile{
            Role="HouseholdKnight",StartingKingdomId="vlandia",
            SpouseEnabled=true,FamilyHomeMode="None"
        };
        Check("landless spouse can start without false owned home",
            landlessHouse.IsValid(30,out var landlessHomeError) &&
            SandboxOriginProfile.Parse(landlessHouse.Serialize())
                .FamilyHomeMode=="None" &&
            SandboxFamilyHomePolicy.ValidateRoleRequest(
                "HouseholdKnight","None",true)=="");
        landlessHouse.FamilyHomeMode="OwnedFief";
        Check("landless spouse cannot promise a native owned fief",
            !landlessHouse.IsValid(30,out var impossibleHomeError) &&
            impossibleHomeError=="landless_cannot_start_with_owned_home" &&
            SandboxFamilyHomePolicy.ValidateRoleRequest(
                "NobleCadet","SpecificOwned",true)==
                    "landless_cannot_start_with_owned_home" &&
            SandboxFamilyHomePolicy.ValidateRoleRequest(
                "Recruit","OwnedFief",true)==
                    "landless_cannot_start_with_owned_home");
        var commonerHouse=new SandboxOriginProfile{Role="Trader"};
        SandboxAutoHousePlan.Prepare(commonerHouse,"vlandia",30,false,"Trader");
        Check("non-landed automatic House plan retains no fixed home",
            commonerHouse.FamilyHomeMode=="None" &&
            SandboxOriginProfile.Parse(commonerHouse.Serialize())
                .FamilyHomeMode=="None");
        Check("native fresh NotSpawned hero is refused",
            SandboxGeneratedMarriagePolicy.Check(
                true,false,true,false,true,true,true) ==
                "generated_spouse_not_active");
        Check("born noble clan must be native-marriage-suitable",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,false,true) ==
                "spouse_birth_clan_invalid");
        Check("native marriage eligibility must be verified after activation",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,false,true,true,true) ==
                "generated_spouse_not_eligible");
        Check("native marriage fails on same-sex pair",
            SandboxGeneratedMarriagePolicy.Check(
                true,true,true,true,true,true,false) ==
                "sex_pair_rejected");
        Check("automatic spouse gender yields opposite of male player",
            SandboxFamilyPolicy.ResolveSpouseFemale("Auto",false));
        Check("automatic spouse gender yields opposite of female player",
            !SandboxFamilyPolicy.ResolveSpouseFemale("Auto",true));
        Check("explicit spouse female and male choices respected",
            SandboxFamilyPolicy.ResolveSpouseFemale("Female",false) &&
            !SandboxFamilyPolicy.ResolveSpouseFemale("Male",true));
        Check("family profile defaults automatic spouse gender",
            new SandboxOriginProfile().SpouseGender == "Auto" &&
            SandboxOriginProfile.Parse("SpouseGender=Auto").SpouseGender == "Auto");
        Check("exact shortage threshold", SupplyPolicy.IsLowFood(3, 3));
        Check("unknown supply not shortage", !SupplyPolicy.IsLowFood(-1, 3));
        Check("ample food not shortage", !SupplyPolicy.IsLowFood(4, 3));
        Check("player foraging gated off", !SupplyPolicy.ShouldForage(false, 1, 2, false, false, false, false, 9, 5));
        Check("eligible forage", SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, false, 5, 5));
        Check("too soon to forage", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, false, 4, 5));
        Check("never forage during battle", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, true, false, 10, 5));
        Check("never forage inside settlement", !SupplyPolicy.ShouldForage(true, 1, 2, true, false, false, false, 10, 5));
        Check("never forage within army", !SupplyPolicy.ShouldForage(true, 1, 2, false, true, false, false, 10, 5));
        Check("never forage under siege", !SupplyPolicy.ShouldForage(true, 1, 2, false, false, false, true, 10, 5));
        Check("unknown army status", SupplyPolicy.ArmyRisk(0, 0) == "unknown");
        Check("mixed army", SupplyPolicy.ArmyRisk(1, 3) == "mixed");
        Check("all army parties critical", SupplyPolicy.ArmyRisk(2, 2) == "critical");

        Check("deposit requires stock", CivicPolicy.CanDeposit(true, true, 5, 5));
        Check("deposit forbids unrelated fiefs", !CivicPolicy.CanDeposit(true, false, 5, 5));
        Check("withdraw prevents negative ledger", !CivicPolicy.CanWithdraw(true, true, 4, 5));
        Check("withdraw exact balance", CivicPolicy.CanWithdraw(true, true, 5, 5));
        Check("investment gold guard", !CivicPolicy.CanInvest(true, true, 299, 300, 0, 3));
        Check("investment cap", !CivicPolicy.CanInvest(true, true, 1000, 300, 3, 3));
        Check("investment allowed", CivicPolicy.CanInvest(true, true, 300, 300, 2, 3));
        Check("recruitment cap", !CivicPolicy.CanRecruit(true, true, 200, 200, 100, 100));
        Check("recruitment allowed", CivicPolicy.CanRecruit(true, true, 200, 200, 99, 100));
        Check("aid requires notable", !CivicPolicy.CanAid(true, true, 5, 5, false));
        Check("shipment requires recipient", !CivicPolicy.CanSendShipment(true, true, 5, 5, false, false));
        Check("shipment cannot stack", !CivicPolicy.CanSendShipment(true, true, 5, 5, true, true));
        Check("shipment allowed", CivicPolicy.CanSendShipment(true, true, 5, 5, true, false));
        Check("production respects depot cap", CivicPolicy.DailyDepotOutput(3, 99, 100) == 1);
        Check("no unpaid production", CivicPolicy.DailyDepotOutput(0, 2, 100) == 0);
        Check("npc advisor restricted to own clan", !CivicPolicy.SafeToAdviseNpc(true, false, true, false, false, false, 1, 2));
        Check("npc advisor skips army parties", !CivicPolicy.SafeToAdviseNpc(true, true, true, false, true, false, 1, 2));
        Check("npc advisor recommends low-food action", CivicPolicy.SafeToAdviseNpc(true, true, true, false, false, false, 1, 2));
        var state = new CivicState();
        state.Get("town_A").Grain = 9;
        state.Get("town_A").Investment = 2;
        state.Shipments.Add(new CivicState.Shipment { From="town_A", To="town_B", Grain=5, DaysRemaining=3 });
        var recovered = CivicState.Parse(state.Serialize());
        Check("save round trip depot stock", recovered.Stock("town_A") == 9);
        Check("save round trip investment", recovered.Get("town_A").Investment == 2);
        Check("save round trip shipment", recovered.Shipments.Count == 1 && recovered.Shipments[0].To == "town_B");
        Check("malformed save fails closed", CivicState.Parse("INVALID").Depots.Count == 0);
        Check("invalid settlement id blocked", !CivicState.Valid("bad;id"));
        Check("empty save state safe", CivicState.Parse("").Serialize() == "CU5");
        Check("legacy CU3 depot loads", CivicState.Parse("CU3;D,old_town,6,1").Stock("old_town") == 6);
        Check("legacy CU3 shipment loads", CivicState.Parse("CU3;S,town_A,town_B,5,3").Shipments.Count == 1);
        Check("convoy disabled by default", !ConvoyPolicy.CanDispatch(false, true, true, 20, 10, 0, 2, false));
        Check("convoy requires both owned", !ConvoyPolicy.CanDispatch(true, true, false, 20, 10, 0, 2, false));
        Check("convoy allowed", ConvoyPolicy.CanDispatch(true, true, true, 20, 10, 1, 2, false));
        Check("convoy active cap", !ConvoyPolicy.CanDispatch(true, true, true, 20, 10, 2, 2, false));
        Check("convoy stock guard", !ConvoyPolicy.CanDispatch(true, true, true, 9, 10, 0, 2, false));
        Check("convoy not delivered before arrival", !ConvoyPolicy.Delivered(true, true, false, true));
        Check("convoy can be lost", ConvoyPolicy.Lost(false, false, false));
        Check("convoy cargo limited by actual food", ConvoyPolicy.DeliverableGrain(10, 3, 15) == 3);
        Check("convoy target cap", ConvoyPolicy.DeliverableGrain(10, 10, 2) == 2);
        Check("convoy timer timeout", ConvoyPolicy.TimedOut(0));
        Check("depot free capacity exactly", DepotLedgerPolicy.FreeCapacity(94, 100) == 6);
        Check("depot zero capacity rejected", DepotLedgerPolicy.FreeCapacity(1, 0) == 0);
        Check("depot negative stock fails closed", DepotLedgerPolicy.FreeCapacity(-1, 100) == 0);
        Check("depot deposit fits precisely", DepotLedgerPolicy.CanDeposit(true, true, 5, 5, 95, 100));
        Check("depot deposit refuses overflow", !DepotLedgerPolicy.CanDeposit(true, true, 5, 5, 96, 100));
        Check("depot deposit refuses disabled", !DepotLedgerPolicy.CanDeposit(false, true, 5, 5, 95, 100));
        Check("depot withdraw blocks overdraft", !DepotLedgerPolicy.CanWithdraw(true, true, 4, 5));
        Check("depot arrival respects cap", DepotLedgerPolicy.Creditable(10, 98, 100) == 2);
        Check("depot arrival blocks negative amount", DepotLedgerPolicy.Creditable(-2, 0, 100) == 0);
        Check("depot arrival remains bounded when full", DepotLedgerPolicy.Creditable(10, 100, 100) == 0);
        Check("shipment remainder bounded", DepotLedgerPolicy.Remainder(5, 7) == 0);
        Check("shipment remainder exact", DepotLedgerPolicy.Remainder(5, 2) == 3);
        Check("convoy dispatch blocks full recipient", !ConvoyPolicy.CanDispatchToDepot(true,true,true,true,false,50,10,95,100,0,2,false));
        Check("convoy dispatch exact recipient space", ConvoyPolicy.CanDispatchToDepot(true,true,true,true,false,50,10,90,100,0,2,false));
        Check("convoy dispatch blocks besieged recipient", !ConvoyPolicy.CanDispatchToDepot(true,true,true,true,true,50,10,0,100,0,2,false));
        Check("convoy dispatch blocks same settlement", !ConvoyPolicy.CanDispatchToDepot(true,true,true,false,false,50,10,0,100,0,2,false));
        Check("convoy unload tracks remaining manifest", DepotLedgerPolicy.Remainder(10, ConvoyPolicy.DeliverableGrain(10,10,3)) == 7);
        Check("convoy never credits more than cargo", ConvoyPolicy.DeliverableGrain(10,7,100) == 7);

        Check("levy opt-in", !KingdomPolicy.CanLevy(false, true, true, true, 500, 10, 40, 100, 0, 2, 250, 5));
        Check("levy requires influence", !KingdomPolicy.CanLevy(true, true, true, true, 500, 4, 40, 100, 0, 2, 250, 5));
        Check("levy requires kingdom", !KingdomPolicy.CanLevy(true, true, false, true, 500, 10, 40, 100, 0, 2, 250, 5));
        Check("levy daily cap", !KingdomPolicy.CanLevy(true, true, true, true, 500, 10, 40, 100, 2, 2, 250, 5));
        Check("kingdom levy allowed", KingdomPolicy.CanLevy(true, true, true, true, 500, 10, 40, 100, 1, 2, 250, 5));
        Check("diplomatic cooldown", !KingdomPolicy.CanMission(true, true, true, true, 500, 15, 2, 300, 10));
        Check("diplomatic external kingdom blocked", !KingdomPolicy.CanMission(true, false, true, true, 500, 15, 0, 300, 10));
        Check("diplomatic mission allowed", KingdomPolicy.CanMission(true, true, true, true, 500, 15, 0, 300, 10));
        recovered.Shipments[0].Physical = true;
        recovered.Shipments[0].PartyId = "cu_convoy_1";
        var king = recovered.CouncilFor("clan_1"); king.MissionCooldown = 7;
        var more = CivicState.Parse(recovered.Serialize());
        Check("convoy save roundtrip", more.Shipments.Count == 1 && more.Shipments[0].Physical
            && more.Shipments[0].PartyId == "cu_convoy_1");
        Check("council save roundtrip", more.CouncilFor("clan_1").MissionCooldown == 7);
        Check("refuse physical missing id", CivicState.Parse("CU4;S,a,b,5,20,P,").Shipments.Count == 0);
        Check("depot full stock price", TradePolicy.UnitPrice(100, 100) == 18);
        Check("depot scarce price", TradePolicy.UnitPrice(2, 100) == 30);
        Check("depot medium price", TradePolicy.UnitPrice(40, 100) == 24);
        Check("depot trade disabled", !TradePolicy.CanPurchase(false,true,true,20,5,900,18));
        Check("depot trade blocked by ownership", !TradePolicy.CanPurchase(true,false,true,20,5,900,18));
        Check("depot trade blocked by empty stock", !TradePolicy.CanPurchase(true,true,true,4,5,900,18));
        Check("depot trade blocked by gold", !TradePolicy.CanPurchase(true,true,true,20,5,89,18));
        Check("depot trade valid", TradePolicy.CanPurchase(true,true,true,20,5,90,18));
        Check("depot trade rejects invalid amount", TradePolicy.BuyCost(0,18) == -1);
        Check("kingdom batch blocks capacity overflow", !KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,99,100,0,4,250,5,2));
        Check("kingdom batch blocks daily cap", !KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,50,100,1,2,250,5,2));
        Check("kingdom batch recruits pair", KingdomPolicy.CanBatchLevy(true,true,true,true,
            1000,20,50,100,0,2,250,5,2));
        Check("strategy battle holds", KingdomAdvisorPolicy.Decide(true,false,false,false,1,20,45,false)
            == KingdomAdvisorPolicy.Priority.Hold);
        Check("strategy defends against threat", KingdomAdvisorPolicy.Decide(false,false,false,false,4,20,45,true)
            == KingdomAdvisorPolicy.Priority.Defend);
        Check("strategy resupplies low food", KingdomAdvisorPolicy.Decide(false,false,false,false,1,20,45,false)
            == KingdomAdvisorPolicy.Priority.Resupply);
        Check("strategy regroups armies", KingdomAdvisorPolicy.Decide(false,false,true,false,7,20,45,false)
            == KingdomAdvisorPolicy.Priority.Regroup);
        Check("strategy recruits in settlement", KingdomAdvisorPolicy.Decide(false,false,false,true,7,20,45,false)
            == KingdomAdvisorPolicy.Priority.Recruit);
        Check("strategy patrols when ready", KingdomAdvisorPolicy.Decide(false,false,false,false,7,50,45,false)
            == KingdomAdvisorPolicy.Priority.Patrol);
        Check("strategy advisory limit", !KingdomAdvisorPolicy.ShouldReport(true,true,true,3,3));
        Check("autonomy disabled by default", KingdomAutonomyPolicy.Decide(false,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy player party blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,true,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy other clan blocked", KingdomAutonomyPolicy.Decide(true,false,true,true,false,true,true,false,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy battle blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,true,false,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy army blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,true,false,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy siege blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,true,false,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy settlement departure blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,true,1,2,50,35,10,0) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy cooldown blocked", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,11) == KingdomAutonomyPolicy.Order.Hold);
        Check("autonomy resupply allowed", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,1,2,50,35,10,9) == KingdomAutonomyPolicy.Order.Resupply);
        Check("autonomy recruitment trip", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,5,2,20,35,10,9) == KingdomAutonomyPolicy.Order.Recruit);
        Check("autonomy unknown food does not resupply", KingdomAutonomyPolicy.Decide(true,true,true,true,false,true,true,false,false,false,false,-1,2,50,35,10,9) == KingdomAutonomyPolicy.Order.None);
        Check("autonomy unknown depot food blocked", !KingdomAutonomyPolicy.SafeDepot(true,true,false,false,0,40,true));
        Check("autonomy enemy depot blocked", !KingdomAutonomyPolicy.SafeDepot(false,true,false,true,200,40,true));
        Check("autonomy sieged depot blocked", !KingdomAutonomyPolicy.SafeDepot(true,true,true,true,200,40,true));
        Check("autonomy valid depot", KingdomAutonomyPolicy.SafeDepot(true,true,false,true,200,40,true));
        Check("autonomy cooldown lower bound", KingdomAutonomyPolicy.NextOrderDay(10,0) == 12);
        Check("autonomy cooldown overflow", KingdomAutonomyPolicy.NextOrderDay(int.MaxValue,8) == int.MaxValue);

        Check("kingdom strategy refuses inactive lord", KingdomStrategyPolicy.Choose(false,false,false,false,false,true,5,2,60,35,false,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves battle", KingdomStrategyPolicy.Choose(true,true,false,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves existing army order", KingdomStrategyPolicy.Choose(true,false,true,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy preserves siege order", KingdomStrategyPolicy.Choose(true,false,false,true,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy prioritizes defence", KingdomStrategyPolicy.Choose(true,false,false,false,true,true,1,2,20,35,true,true) == KingdomStrategyPolicy.Priority.Defend);
        Check("kingdom strategy resupplies before recruitment", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,1,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Resupply);
        Check("kingdom strategy wartime recovery", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,5,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Recover);
        Check("kingdom strategy peacetime recruitment", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,20,35,false,true) == KingdomStrategyPolicy.Priority.Recruit);
        Check("kingdom strategy siege support advisory", KingdomStrategyPolicy.Choose(true,false,false,false,false,true,5,2,80,35,true,true) == KingdomStrategyPolicy.Priority.SiegeSupport);
        Check("kingdom strategy patrols safe areas", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,80,35,false,true) == KingdomStrategyPolicy.Priority.Patrol);
        Check("kingdom strategy no unsafe patrol", KingdomStrategyPolicy.Choose(true,false,false,false,false,false,5,2,80,35,false,false) == KingdomStrategyPolicy.Priority.Hold);
        Check("kingdom strategy blocks besieged defence target", !KingdomStrategyPolicy.EligibleDefenseTarget(true,true,true,true,false));
        Check("kingdom strategy blocks enemy defence target", !KingdomStrategyPolicy.EligibleDefenseTarget(false,false,true,true,false));
        Check("kingdom strategy accepts friendly safe target", KingdomStrategyPolicy.EligibleDefenseTarget(true,false,true,true,false));

        Check("strategy command rejects battle override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,true,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects army override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,true,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects siege override", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,true,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects settlement departure", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,true,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects foreign clan", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,false,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects cooldown", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,false,
            false,false,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command resupply opt in", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Resupply,true,true,true,false,false,false,false,false,
            false,true,true,false,true,true,true,false) == KingdomCommandPolicy.TravelOrder.Resupply);
        Check("strategy command recovery permits recruiting", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Recover,true,true,true,false,false,false,false,false,
            false,true,true,false,true,false,true,false) == KingdomCommandPolicy.TravelOrder.Recruit);
        Check("strategy command rejects siege support", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.SiegeSupport,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy command rejects direct defense", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Defend,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol requires confirmed peace", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,false,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol rejects moving parties", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            true,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol requires confirmed safe settlement", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,false,true,true,true) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol off by default", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,false) == KingdomCommandPolicy.TravelOrder.Hold);
        Check("strategy patrol allowed in safe peacetime", KingdomCommandPolicy.Choose(
            KingdomStrategyPolicy.Priority.Patrol,true,true,true,false,false,false,false,false,
            false,true,true,true,true,true,true,true) == KingdomCommandPolicy.TravelOrder.Patrol);
        Check("local recruit daily cap", !LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 200, 200, 20, 100, 2, 2));
        Check("local recruit lacks gold", !LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 199, 200, 20, 100, 0, 2));
        Check("local recruit ownership guarded", !LocalRecruitmentPolicy.CanRecruit(
            true, false, true, 200, 200, 20, 100, 0, 2));
        Check("local recruit allowed within cap", LocalRecruitmentPolicy.CanRecruit(
            true, true, true, 200, 200, 20, 100, 1, 2));
        Check("garrison disabled by default", !GarrisonTrainingPolicy.CanTrain(
            false,true,false,true,true,250,250,5,5,90,100,0,4,2));
        Check("garrison under siege rejected", !GarrisonTrainingPolicy.CanTrain(
            true,true,true,true,true,250,250,5,5,90,100,0,4,2));
        Check("garrison requires real party", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,false,true,250,250,5,5,90,100,0,4,2));
        Check("garrison grain shortage rejected", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,4,5,90,100,0,4,2));
        Check("garrison full-party cap", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,99,100,0,4,2));
        Check("garrison daily cap", !GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,90,100,4,4,2));
        Check("garrison exact capacity allowed", GarrisonTrainingPolicy.CanTrain(
            true,true,false,true,true,250,250,5,5,98,100,0,4,2));
        var legacyCouncil = CivicState.Parse("CU4;K,town_a,1,6");
        Check("legacy CU4 council migration",
            legacyCouncil.CouncilFor("town_a").LevyToday == 1 &&
            legacyCouncil.CouncilFor("town_a").MissionCooldown == 6 &&
            legacyCouncil.CouncilFor("town_a").LocalToday == 0 &&
            legacyCouncil.CouncilFor("town_a").GarrisonToday == 0);
        var newCounters = new CivicState();
        newCounters.CouncilFor("town_a").LocalToday = 1;
        newCounters.CouncilFor("town_a").GarrisonToday = 2;
        var reloadedCounters = CivicState.Parse(newCounters.Serialize());
        Check("new daily counters survive CU5 reload",
            reloadedCounters.CouncilFor("town_a").LocalToday == 1 &&
            reloadedCounters.CouncilFor("town_a").GarrisonToday == 2);
        Check("estate forecast castle + two healthy villages",
            FeudalBudgetPolicy.EstimateEstateIncome(1500, 900) == 1450);
        Check("estate forecast ignores negative unknown input",
            FeudalBudgetPolicy.EstimateEstateIncome(-10, 900) == 0);
        Check("wage cap leaves positive target",
            FeudalBudgetPolicy.WageBudget(1450, 500, 150, 250, 1800) == 550);
        Check("wage cap refuses unaffordable field party",
            FeudalBudgetPolicy.WageBudget(1450, 7000, 150, 250, 1800) == 0);
        Check("healthy castle projected net after cap",
            FeudalBudgetPolicy.ProjectedBalance(1450, 500, 550, 150, 0) == 250);
        Check("overstaffed castle baseline loses 10k possible",
            FeudalBudgetPolicy.ProjectedBalance(1450, 500, 10000, 150, 0) < -5000);
        Check("garrison cap may reduce unlimited prior expense",
            FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 8000, 550, 250));
        Check("never lower garrison wages during siege",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, true, 8000, 550, 250));
        Check("do not raise an existing manual cap",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 300, 550, 250));
        Check("zero current cap untouched",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 0, 550, 250));
        Check("reject a garrison cap below defensive threshold",
            !FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false, 8000, 200, 250));
        Check("harvest respects depot free space",
            FeudalBudgetPolicy.VillageHarvest(900, 5, 150, 10) == 5);
        Check("raided villages produce no extra grain",
            FeudalBudgetPolicy.VillageHarvest(0, 100, 150, 10) == 0);
        Check("harvest stops when depot full",
            FeudalBudgetPolicy.VillageHarvest(900, 0, 150, 10) == 0);
        Check("surplus sale respects reserve and daily cap",
            FeudalBudgetPolicy.SurplusSale(65, 30, 8) == 8);
        Check("surplus sale never drains protected grain",
            FeudalBudgetPolicy.SurplusSale(30, 30, 8) == 0);
        Check("automated garrison training only with fiscal room",
            FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 450, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison stops when wages reach cap",
            !FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 525, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison stops during siege",
            !FeudalBudgetPolicy.AffordableTraining(true, true, true,
                1450, 500, 450, 550, 150, 250, 50,
                10000, 5000, 40, 55));
        Check("automated garrison obeys player gold reserve",
            !FeudalBudgetPolicy.AffordableTraining(true, true, false,
                1450, 500, 450, 550, 150, 250, 50,
                3000, 5000, 40, 55));
        Check("castle required income", CastleManagementPolicy.RequiredIncome(500, 750, 150, 250) == 1650);
        Check("castle modeled shortfall", CastleManagementPolicy.ModeledShortfall(1450, 1650) == 200);
        Check("castle shortfall zero", CastleManagementPolicy.ModeledShortfall(1650, 1650) == 0);
        Check("castle required rejects negative", CastleManagementPolicy.RequiredIncome(-1, 750, 150, 250) == 0);
        Check("castle required saturates", CastleManagementPolicy.RequiredIncome(int.MaxValue, 10, 0, 0) == int.MaxValue);
        Check("manual transfer disabled", !CastleManagementPolicy.CanMove(false, true, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer bounded", CastleManagementPolicy.CanMove(true, true, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer low source rejected", !CastleManagementPolicy.CanMove(true, true, true, false, true, true, 4, 40, 50, 5));
        Check("manual transfer siege rejected", !CastleManagementPolicy.CanMove(true, true, true, true, true, true, 5, 40, 50, 5));
        Check("manual transfer foreign rejected", !CastleManagementPolicy.CanMove(true, false, true, false, true, true, 5, 40, 50, 5));
        Check("manual transfer full destination rejected", !CastleManagementPolicy.CanMove(true, true, true, false, true, true, 5, 46, 50, 5));
        Check("manual transfer missing garrison rejected", !CastleManagementPolicy.CanMove(true, true, true, false, false, true, 5, 40, 50, 5));
        Check("manual wage cap allowed", CastleManagementPolicy.CanApplyCap(true, true, true, false, 2000, 1000, 250, 900));
        Check("manual wage cap no layoffs", !CastleManagementPolicy.CanApplyCap(true, true, true, false, 2000, 1000, 250, 1100));
        Check("manual wage cap siege rejected", !CastleManagementPolicy.CanApplyCap(true, true, true, true, 2000, 1000, 250, 900));
        Check("manual wage cap disabled", !CastleManagementPolicy.CanApplyCap(false, true, true, false, 2000, 1000, 250, 900));
        var catalogKeys = new System.Collections.Generic.HashSet<string>();
        foreach (var option in SettingsCatalog.All)
        {
            Check("catalog has unique field " + option.Key,
                catalogKeys.Add(option.Key));
            Check("catalog field matches Settings type " + option.Key,
                typeof(Settings).GetField(option.Key) != null &&
                typeof(Settings).GetField(option.Key).FieldType ==
                (option.Boolean ? typeof(bool) : typeof(int)));
            Check("catalog default value is valid " + option.Key,
                SettingsCatalog.TryNormalize(option.Key,
                    SettingsCatalog.Read(new Settings(), option), out var normalizedDefault));
            if (!option.Boolean)
            {
                Check("catalog numeric upper bound " + option.Key,
                    !SettingsCatalog.TryNormalize(option.Key,
                        ((long)option.Max + 1).ToString(), out var tooBig));
                Check("catalog numeric lower bound " + option.Key,
                    !SettingsCatalog.TryNormalize(option.Key,
                        ((long)option.Min - 1).ToString(), out var tooSmall));
                Check("catalog numeric clamp " + option.Key,
                    SettingsCatalog.Next(option.Max, option, 1, 10) == option.Max &&
                    SettingsCatalog.Next(option.Min, option, -1, 10) == option.Min);
            }
        }
        Check("catalog covers every public configurable value",
            catalogKeys.Count == 65);
        var origin = new SandboxOriginProfile();
        Check("roles cannot forge king or lord titles",
            !SandboxRolePolicy.MatchesNativeStatus("King", false, false) &&
            SandboxRolePolicy.MatchesNativeStatus("King", true, true) &&
            !SandboxRolePolicy.MatchesNativeStatus("Lord", true, true) &&
            !SandboxRolePolicy.MatchesNativeStatus("Knight", false, false) &&
            SandboxRolePolicy.MatchesNativeStatus("Soldier", true, false));
        Check("role packages have distinct troops and renown",
            SandboxRolePolicy.BonusRecruits("Wanderer") == 0 &&
            SandboxRolePolicy.BonusRecruits("Soldier") == 4 &&
            SandboxRolePolicy.BonusRecruits("Knight") == 2 &&
            SandboxRolePolicy.BonusRecruits("King") == 8 &&
            SandboxRolePolicy.StarterRenown("Knight") == 40);
        Check("role bonuses require confirmed native contract result",
            SandboxRolePolicy.ConfirmedNativeStanding("Soldier",
                "native_mercenary_contract") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("Soldier",
                "native_affiliation_retained") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("Knight",
                "native_mercenary_contract") &&
            SandboxRolePolicy.ConfirmedNativeStanding("Knight",
                "native_vassal_contract") &&
            SandboxRolePolicy.ConfirmedNativeStanding("King",
                "native_king_confirmed") &&
            !SandboxRolePolicy.ConfirmedNativeStanding("King",
                "king_requires_official_advanced_start"));
        Check("native affiliations require explicit opt-in",
            SandboxRolePolicy.CanRequestAffiliation("Soldier", false, true) &&
            !SandboxRolePolicy.CanRequestAffiliation("Soldier", true, true) &&
            !SandboxRolePolicy.CanRequestAffiliation("Soldier", false, false) &&
            !SandboxRolePolicy.CanRequestAffiliation("King", false, true));
        Check("family creator refuses duplicate native spouse",
            SandboxFamilyPolicy.Guard(true,true,0,false,0,true,false,true)
                == "existing_spouse");
        Check("family creator refuses duplicate native children",
            SandboxFamilyPolicy.Guard(false,false,2,true,0,true,false,true)
                == "existing_children");
        Check("family creator refuses fake siblings without parents",
            SandboxFamilyPolicy.Guard(false,false,0,false,2,false,false,true)
                == "native_parents_missing");
        Check("family creator rejects untested same-sex native marriage",
            SandboxFamilyPolicy.Guard(true,false,0,false,0,true,true,true)
                == "native_marriage_same_gender_untested");
        Check("single-parent child age validation",
            SandboxFamilyPolicy.ValidChildAge(3,20,false,0) &&
            !SandboxFamilyPolicy.ValidChildAge(7,20,false,0) &&
            !SandboxFamilyPolicy.ValidChildAge(10,30,true,24));
        Check("family defaults use valid hero names",
            new SandboxOriginProfile().TrySet("ChildCount", "2") &&
            new SandboxOriginProfile().Children[0].Name == "Alden");
        var nativeReviewOptIn = new SandboxOriginProfile();
        Check("native CU 3D review is off by default",
            !nativeReviewOptIn.NativeIntegrationReviewEnabled);
        nativeReviewOptIn.TrySet("NativeIntegrationReviewEnabled","true");
        Check("native CU 3D review opt-in survives exact profile serialization",
            SandboxOriginProfile.Parse(nativeReviewOptIn.Serialize())
                .NativeIntegrationReviewEnabled);
        nativeReviewOptIn.ResetForNewCampaign();
        Check("native CU 3D review does not silently carry into new games",
            !nativeReviewOptIn.NativeIntegrationReviewEnabled);
        var priorCampaign = new SandboxOriginProfile();
        priorCampaign.TrySet("Role","Lord");
        priorCampaign.TrySet("SpouseEnabled","true");
        priorCampaign.TrySet("ChildCount","2");
        priorCampaign.TrySet("Child.1.Name","Alina");
        priorCampaign.TrySet("Child.1.Gender","Female");
        priorCampaign.TrySet("StartingFiefMode","Castle");
        priorCampaign.TrySet("AllowStartingFiefTransfer","true");
        priorCampaign.ResetForNewCampaign();
        Check("fresh Sandbox clears previous child count and fief transfer",
            priorCampaign.ChildCount==0 && !priorCampaign.SpouseEnabled &&
            priorCampaign.StartingFiefMode=="Native" &&
            !priorCampaign.AllowStartingFiefTransfer &&
            !priorCampaign.Enabled);
        Check("fresh Sandbox keeps role and child name as optional presets",
            priorCampaign.Role=="Lord" &&
            priorCampaign.Children[0].Name=="Alina" &&
            priorCampaign.Children[0].Gender=="Female");
        priorCampaign.TrySet("SpouseEnabled","true");
        priorCampaign.TrySet("ChildCount","1");
        var oneChildReload=SandboxOriginProfile.Parse(
            priorCampaign.Serialize());
        Check("exactly one child remains after save/reload of a fresh profile",
            oneChildReload.ChildCount==1 &&
            oneChildReload.SpouseEnabled &&
            oneChildReload.Children[0].Name=="Alina" &&
            oneChildReload.Children[1].Name!="Alina" &&
            oneChildReload.IsValid(30,out var oneChildValidity));

        var nativeFamilyPreset = new SandboxOriginProfile();
        nativeFamilyPreset.TrySet("ChildCount", "2");
        nativeFamilyPreset.TrySet("SpouseEnabled", "true");
        nativeFamilyPreset.TrySet("Child.1.Age", "1");
        nativeFamilyPreset.TrySet("Child.2.Age", "2");
        Check("native married-two-children preset validates without text edits",
            nativeFamilyPreset.IsValid(30, out var presetProblem));
        nativeFamilyPreset.TrySet("SpouseEnabled", "false");
        nativeFamilyPreset.TrySet("ChildCount", "1");
        Check("native single-parent preset validates without text edits",
            nativeFamilyPreset.IsValid(20, out var singleParentProblem));
        nativeFamilyPreset.TrySet("ChildCount", "0");
        nativeFamilyPreset.TrySet("SiblingCount", "2");
        Check("native sibling-only preset validates without text edits",
            nativeFamilyPreset.IsValid(20, out var siblingProblem));
        Check("the visual creator defaults on, with an explicit native fallback",
            new SandboxOriginProfile().OverhauledCreatorEnabled &&
            new SandboxOriginProfile().TrySet("OverhauledCreatorEnabled","false") &&
            !SandboxOriginProfile.Parse("OverhauledCreatorEnabled=false").OverhauledCreatorEnabled);
        Check("culture preferences are bounded profile data only",
            origin.TrySet("StartingCulture","empire") &&
            !origin.TrySet("StartingCulture","bad culture id"));
        Check("Admiral is a selectable profile but requires separate naval integration",
            Array.IndexOf(SandboxOriginProfile.Roles,"Admiral")>=0 &&
            SandboxRolePolicy.BonusRecruits("Admiral")==0 &&
            !SandboxRolePolicy.MatchesNativeStatus("Admiral",true,true));
        Check("new sandbox origin disabled by default", !origin.Enabled);
        Check("sandbox all native start roles included", SandboxOriginProfile.Roles.Length >= 9 &&
            Array.IndexOf(SandboxOriginProfile.Roles, "Wanderer") >= 0 &&
            Array.IndexOf(SandboxOriginProfile.Roles, "King") >= 0);
        Check("three risky v0.17 features remain default-off",
            !new SandboxOriginProfile().NativeStageEnabled &&
            !new SandboxOriginProfile().RealFamilyEnabled &&
            !new SandboxOriginProfile().RoleAffiliationsEnabled);
        Check("profile opt-ins are strict booleans and survive roundtrip",
            !origin.TrySet("NativeStageEnabled", "yes") &&
            origin.TrySet("NativeStageEnabled", "true") &&
            origin.TrySet("RealFamilyEnabled", "true") &&
            origin.TrySet("RoleAffiliationsEnabled", "true") &&
            SandboxOriginProfile.Parse(origin.Serialize()).NativeStageEnabled &&
            SandboxOriginProfile.Parse(origin.Serialize()).RealFamilyEnabled &&
            SandboxOriginProfile.Parse(origin.Serialize()).RoleAffiliationsEnabled);
        Check("sandbox default does not replace native treasury",
            origin.Gold == -1 && origin.Influence == -1);
        Check("sandbox reject invalid role", !origin.TrySet("Role", "God"));
        Check("sandbox accept knight role", origin.TrySet("Role", "Knight") &&
            origin.Role == "Knight");
        Check("sandbox reject unbounded gold", !origin.TrySet("Gold", "9000000"));
        Check("sandbox accept explicit wealth", origin.TrySet("Gold", "12000") &&
            origin.Gold == 12000);
        Check("sandbox accept configurable spouse",
            origin.TrySet("SpouseEnabled", "true") &&
            origin.TrySet("SpouseName", "Lady Aria") &&
            origin.TrySet("SpouseAge", "35"));
        Check("sandbox child requires parental ages",
            origin.TrySet("ChildCount", "2") &&
            origin.TrySet("Child.1.Name", "Aelia") &&
            origin.TrySet("Child.1.Age", "8") &&
            origin.TrySet("Child.2.Name", "Cassian") &&
            origin.TrySet("Child.2.Age", "9") &&
            origin.IsValid(34, out var validOrigin));
        Check("sandbox reject child older than parent",
            origin.TrySet("Child.1.Age", "30") &&
            !origin.IsValid(34, out var childAgeError));
        origin.TrySet("Child.1.Age", "8");
        Check("sandbox supports single parent with existing children",
            origin.TrySet("SpouseEnabled", "false") &&
            origin.IsValid(34, out var noSpouseError));
        origin.TrySet("SpouseEnabled", "true");
        Check("sandbox reject bad hero name",
            !origin.TrySet("SpouseName", "Bad\nName"));
        Check("sandbox skill override bounded",
            origin.TrySet("Skill.OneHanded", "220") &&
            !origin.TrySet("Skill.OneHanded", "440"));
        Check("sandbox attributes bounded",
            origin.TrySet("Attribute.Vigor", "8") &&
            !origin.TrySet("Attribute.Vigor", "11"));
        Check("sandbox focus bounded",
            origin.TrySet("Focus.Bow", "4") &&
            !origin.TrySet("Focus.Bow", "9"));
        Check("sandbox traits bounded",
            origin.TrySet("Trait.Mercy", "-2") &&
            !origin.TrySet("Trait.Mercy", "-4"));
        Check("sandbox equipment exact id allow-list",
            origin.TrySet("Item.1", "empire_sword_1_t2") &&
            !origin.TrySet("Item.13", "empire_sword_1_t2") &&
            !origin.TrySet("Item.2", "../../evil"));
        var inactiveChild = new SandboxOriginProfile();
        Check("sandbox remembers child name before count activation",
            inactiveChild.TrySet("Child.1.Name", "Mira") &&
            SandboxOriginProfile.Parse(inactiveChild.Serialize()).Children[0].Name
                == "Mira");
        Check("sandbox roundtrip all family and stats data",
            SandboxOriginProfile.Parse(origin.Serialize()).Children[0].Name == "Aelia" &&
            SandboxOriginProfile.Parse(origin.Serialize()).Overrides["Skill.OneHanded"] == 220 &&
            SandboxOriginProfile.Parse(origin.Serialize()).ItemIds[1] == "empire_sword_1_t2");
        var originTemp = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "CU-Origin-" + Guid.NewGuid().ToString("N"), "origin.ini");
        try
        {
            string saveError;
            Check("sandbox origin persisted atomically",
                origin.Save(originTemp, out saveError) &&
                SandboxOriginProfile.Load(originTemp).SpouseName == "Lady Aria");
            var broken = SandboxOriginProfile.Load(originTemp);
            broken.ChildCount = 1;
            broken.SpouseEnabled = false;
            broken.Children[0].Age = 50;
            string previous = System.IO.File.ReadAllText(originTemp);
            Check("invalid sandbox family cannot overwrite profile",
                !broken.Save(originTemp, out saveError) &&
                System.IO.File.ReadAllText(originTemp) == previous);
            Check("malformed origin profile is safe",
                !SandboxOriginProfile.Parse("Enabled=nope\nRole=wrong\nGold=abc").Enabled);
        }
        finally
        {
            string folder = System.IO.Path.GetDirectoryName(originTemp);
            if (System.IO.Directory.Exists(folder))
                System.IO.Directory.Delete(folder, true);
        }
        Check("settings allow-list accepts reporting toggles",
            SettingsMenuPolicy.TryNormalize("EnableSupplyWarnings", "false", out var normalizedBool)
            && normalizedBool == "false");
        Check("settings allow-list rejects external patch switch",
            !SettingsMenuPolicy.TryNormalize("EnableHarmonyPatching", "true", out var unknown));
        Check("settings catalog exposes risky patrol under explicit experimental state",
            SettingsMenuPolicy.TryNormalize("EnableAutonomousClanPatrol", "true", out var forbidden)
            && SettingsCatalog.Find("EnableAutonomousClanPatrol").Experimental);
        Check("settings rejects malformed bool",
            !SettingsMenuPolicy.TryNormalize("EnableEconomy", "yes", out var badBool));
        Check("settings accepts maximum day threshold",
            SettingsMenuPolicy.TryNormalize("SupplyWarningDays", "15", out var bounded)
            && bounded == "15");
        Check("settings rejects day threshold overflow",
            !SettingsMenuPolicy.TryNormalize("SupplyWarningDays", "16", out var overflow));
        Check("settings rejects negative recruit cap",
            !SettingsMenuPolicy.TryNormalize("LocalRecruitDailyCap", "-1", out var negative));
        Check("settings replace preserves unknown keys and comments",
            SettingsMenuPolicy.UpdateLines("# user note\nUnknownCustomOption=yes\nEnableSupplyWarnings=true\n",
                "EnableSupplyWarnings", "false").Contains("UnknownCustomOption=yes")
            && SettingsMenuPolicy.UpdateLines("# user note\nEnableSupplyWarnings=true\n",
                "EnableSupplyWarnings", "false").Contains("# user note"));
        Check("settings replace prevents duplicate definitions",
            SettingsMenuPolicy.UpdateLines("EnableEconomy=true\nEnableEconomy=true",
                "EnableEconomy", "false").Split(new[] {"EnableEconomy="},
                StringSplitOptions.None).Length == 2);
        var tempSettings = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "CU-Settings-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempSettings);
        try
        {
            string baseline = System.IO.Path.Combine(tempSettings, "baseline.ini");
            string userIni = System.IO.Path.Combine(tempSettings, "user.ini");
            System.IO.File.WriteAllText(baseline,
                "# default configuration\nEnableEconomy=false\nSupplyWarningDays=3\n");
            var settings = Settings.LoadFromPaths(baseline, userIni);
            Check("settings start with safe baseline", !settings.EnableEconomy &&
                settings.SupplyWarningDays == 3);
            string configError;
            Check("settings in-game write succeeds",
                settings.TrySetMenuValue("EnableSupplyWarnings", "false",
                    userIni, out configError) && !settings.EnableSupplyWarnings);
            Check("settings write persists across new object",
                !Settings.LoadFromPaths(baseline, userIni).EnableSupplyWarnings);
            Check("settings experimental stays opt-in on reload",
                !Settings.LoadFromPaths(baseline, userIni).EnableEconomy);
            Check("settings can enable and persist an opt-in feature",
                settings.TrySetMenuValue("EnableEconomy", "true",
                    userIni, out configError) && settings.EnableEconomy &&
                Settings.LoadFromPaths(baseline, userIni).EnableEconomy);
            Check("settings numeric updates apply immediately",
                settings.TrySetMenuValue("SupplyWarningDays", "5",
                    userIni, out configError) && settings.SupplyWarningDays == 5);
            string previousContents = System.IO.File.ReadAllText(userIni);
            Check("settings invalid input does not mutate disk",
                !settings.TrySetMenuValue("SupplyWarningDays", "99",
                    userIni, out configError) &&
                System.IO.File.ReadAllText(userIni) == previousContents &&
                settings.SupplyWarningDays == 5);
            Check("settings malformed path does not change live state",
                !settings.TrySetMenuValue("EnableEconomy", "false",
                    "", out configError) && settings.EnableEconomy);
            Check("settings preserving unrelated user entries",
                SettingsMenuPolicy.UpdateLines("# keep\nExtraKey=hello\n",
                    "EnableCivicSystems", "false").Contains("ExtraKey=hello"));
        }
        finally
        {
            System.IO.Directory.Delete(tempSettings, true);
        }
        Check("diagnostic CSV escapes quotes and comma",
            DiagnosticJournal.CsvField("castle,\"test\"") == "\"castle,\"\"test\"\"\"");
        Check("diagnostic path redactor accepts empty", DiagnosticJournal.StripPersonalPath("") == "");
        var tempDiagnostic = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "CU-Diagnostic-Test-" + Guid.NewGuid().ToString("N"));
        try
        {
            DiagnosticJournal.OverrideRootForTests = tempDiagnostic;
            DiagnosticJournal.Enabled = true;
            string fakeDesktop=System.IO.Path.Combine(
                tempDiagnostic,"MockDesktop");
            DiagnosticSessionReporter.OverrideDesktopDirectoryForTests=fakeDesktop;
            Check("missing Desktop falls back to local diagnostic folder",
                DiagnosticSessionReporter.ReportsDirectory==
                    System.IO.Path.Combine(tempDiagnostic,"Reports"));
            System.IO.Directory.CreateDirectory(fakeDesktop);
            string desktopReportFolder=System.IO.Path.Combine(
                fakeDesktop,"Calradia Unified Reports");
            Check("automatic reports use a dedicated Desktop folder",
                DiagnosticSessionReporter.ReportsDirectory==
                    desktopReportFolder &&
                System.IO.Directory.Exists(desktopReportFolder));
            var cells = new string[19];
            for (int i = 0; i < cells.Length; i++) cells[i] = "0";
            cells[0] = "castle";
            cells[2] = "castle_test";
            DiagnosticJournal.Finance(cells);
            DiagnosticJournal.Event("test_event", "change=0");
            DiagnosticJournal.Exception("test_crash",
                new InvalidOperationException("Mock crash with comma, and quotes"));
            Check("diagnostic finance CSV writes correct header",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "finance.csv"))
                    .StartsWith(DiagnosticJournal.FinanceHeader));
            Check("diagnostic exception CSV captures managed failure",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "exceptions.csv"))
                    .Contains("InvalidOperationException"));
            Check("diagnostic exception CSV preserves escaped commas",
                System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "exceptions.csv"))
                    .Contains("Mock crash with comma, and quotes"));
            // The report for a native hard crash is recovered on next
            // launch from its active.marker and already-flushed CSV.
            string previousSession = System.IO.Path.Combine(
                tempDiagnostic,"Sessions","20261118T100000_deadbeef");
            System.IO.Directory.CreateDirectory(previousSession);
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                previousSession,"active.marker"),"0");
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                previousSession,"events.csv"),
                "utc,session_id,context,detail\n"+
                "2026-11-18,prior,creator_page,age_to_stats_crashed\n");
            // More recent completed sessions must not conceal an old crash.
            for(int n=0;n<26;n++)
            {
                string completed=System.IO.Path.Combine(
                    tempDiagnostic,"Sessions",
                    "20261118T1001"+n.ToString("D2")+"_completed");
                System.IO.Directory.CreateDirectory(completed);
                System.IO.File.WriteAllText(System.IO.Path.Combine(
                    completed,"completed.marker"),"normal_shutdown");
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                previousSession,"unexpected-private.sav"),"DO NOT EXPORT");
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                previousSession,"unexpected-dump.dmp"),"DO NOT EXPORT");
            // The report must distinguish a known last action from an
            // unproven crash cause. A multiline CSV detail is valid.
            System.IO.File.AppendAllText(System.IO.Path.Combine(
                previousSession,"events.csv"),
                "2026-11-18,prior,cu_stats_page_rows_built,"+
                "\"page=1/9, visible=6\""+"\n");
            string fakeGame=System.IO.Path.Combine(
                tempDiagnostic,"mock-game-root");
            DiagnosticSessionReporter.OverrideGameRootForTests=fakeGame;
            Check("known game path can be set for session report",
                DiagnosticSessionReporter.ResolveGameRoot()==fakeGame);
            DiagnosticSessionReporter.Start();
            string reports=DiagnosticSessionReporter.ReportsDirectory;
            string recoveredZipPath=System.IO.Path.Combine(reports,
                "CU-Session-20261118T100000_deadbeef-INTERRUPTED.zip");
            Check("recovery is not starved by 26 newer completed folders",
                System.IO.File.Exists(recoveredZipPath));
            Check("next launch creates ZIP for interrupted prior session",
                System.IO.File.Exists(recoveredZipPath) &&
                !System.IO.File.Exists(System.IO.Path.Combine(
                    previousSession,"active.marker")) &&
                (System.IO.File.Exists(System.IO.Path.Combine(
                    previousSession,"recovered.marker")) ||
                 !System.IO.Directory.Exists(previousSession)));
            using(var archive=System.IO.Compression.ZipFile.OpenRead(
                recoveredZipPath))
            {
                Check("recovery report contains the prior session journal",
                    archive.Entries.Any(e=>e.FullName=="events.csv") &&
                    archive.Entries.Any(e=>e.FullName=="recovery-info.txt"));
                Check("recovery excludes unexpected saves and dumps",
                    !archive.Entries.Any(e=>e.FullName.EndsWith(".sav") ||
                        e.FullName.EndsWith(".dmp")));
                var summaryEntry=archive.GetEntry("READ_ME_FIRST.txt");
                Check("interrupted report has readable summary in ZIP",
                    summaryEntry!=null);
                using(var reader=new System.IO.StreamReader(
                    summaryEntry.Open()))
                {
                    string summaryText=reader.ReadToEnd();
                    Check("recovery summary identifies last observed stats event",
                        summaryText.Contains("cu_stats_page_rows_built") &&
                        summaryText.Contains("page=1/9, visible=6") &&
                        summaryText.Contains("INTERRUPTED"));
                    Check("recovery summary does not infer native crash cause",
                        summaryText.Contains("A native crash cause is NOT") &&
                        summaryText.Contains("No managed exception recorded."));
                    Check("legacy creator event is identified without claiming cause",
                        summaryText.Contains("Page: All Stats") &&
                        summaryText.Contains("RECORDED ERROR AREA") &&
                        summaryText.Contains("Interruption is unclassified."));
                }
            }
            DiagnosticJournal.Event("gameplay_test_event",
                "after_age_screen=stats_page");
            DiagnosticJournal.Event("creator_csv_quoted_test",
                "field=\"All Stats\", step=1\ncontinuation");
            DiagnosticSessionReporter.Heartbeat(
                "campaign_active=true|test_heartbeat=true");
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                DiagnosticSessionReporter.SessionDirectory,
                "unexpected-private.sav"),"DO NOT EXPORT");
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                DiagnosticSessionReporter.SessionDirectory,
                "unexpected-screenshot.png"),"DO NOT EXPORT");
            DiagnosticSessionReporter.Complete("unit_test_normal_exit");
            string current=System.IO.Path.Combine(reports,
                "CU-Session-"+DiagnosticJournal.CurrentSessionId+
                "-CLOSED.zip");
            Check("normal exit packages current session to private ZIP",
                System.IO.File.Exists(current) &&
                !System.IO.File.Exists(System.IO.Path.Combine(
                    DiagnosticSessionReporter.SessionDirectory,
                    "active.marker")) &&
                System.IO.File.Exists(System.IO.Path.Combine(
                    DiagnosticSessionReporter.SessionDirectory,
                    "completed.marker")));
            using(var archive=System.IO.Compression.ZipFile.OpenRead(
                current))
            {
                Check("normal report includes session events and metadata",
                    archive.Entries.Any(e=>e.FullName=="events.csv") &&
                    archive.Entries.Any(e=>e.FullName=="runtime-info.txt") &&
                    archive.Entries.Any(e=>e.FullName=="README.txt") &&
                    archive.Entries.Any(e=>e.FullName=="READ_ME_FIRST.txt"));
                using(var reader=new System.IO.StreamReader(
                    archive.GetEntry("READ_ME_FIRST.txt").Open()))
                {
                    string summaryText=reader.ReadToEnd();
                    Check("normal summary parses multiline quoted CSV and exception",
                        summaryText.Contains("creator_csv_quoted_test") &&
                        summaryText.Contains("field=\"All Stats\", step=1") &&
                        summaryText.Contains("InvalidOperationException") &&
                        summaryText.Contains("CLOSED"));
                }
                Check("normal report excludes raw memory and saves",
                    !archive.Entries.Any(e=>
                        e.FullName.EndsWith(".dmp",
                            StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.EndsWith(".sav",
                            StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.EndsWith(".png",
                            StringComparison.OrdinalIgnoreCase)));
            }
            Check("local latest report pointer refers to Desktop ZIP",
                System.IO.File.ReadAllText(System.IO.Path.Combine(
                    tempDiagnostic,"latest-report.txt"))==current &&
                System.IO.Path.GetDirectoryName(current)==desktopReportFolder);
            Check("Desktop latest summary opens without extracting ZIP",
                System.IO.File.ReadAllText(System.IO.Path.Combine(
                    desktopReportFolder,"LATEST-REPORT-SUMMARY.txt"))
                    .Contains("creator_csv_quoted_test"));
            // Explicit checkpoints are separate from the requested next
            // page, and error categories derive from real CU contexts.
            string classifier=System.IO.Path.Combine(
                tempDiagnostic,"area-fixtures");
            System.IO.Directory.CreateDirectory(classifier);
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                classifier,"events.csv"),
                "utc,session_id,context,detail\n"+
                "2026-11-20T01:00:00Z,fixture,cu_creator_page_checkpoint,"+
                "\"page=Age|surface=stats_editor|phase=layer_attached\"\n"+
                "2026-11-20T01:00:01Z,fixture,cu_stats_navigation,"+
                "\"direction=next|from=-1|to=0\"\n"+
                // We saw row construction, not that the next page rendered.
                "2026-11-20T01:00:02Z,fixture,cu_stats_page_rows_built,"+
                "\"page=1/9|visible=6\"\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                classifier,"exceptions.csv"),
                "utc,session_id,context,exception_type,message,stack_trace\n"+
                "2026-11-20T01:00:03Z,fixture,cu_native_stats_attach_failed,"+
                "System.InvalidOperationException,UI error,stack\n");
            string classified=DiagnosticSummaryPolicy.Build(classifier,
                "interrupted_previous_process",true);
            Check("reports confirmed Age and requested All Stats separately",
                classified.Contains("Page: Age") &&
                classified.Contains("Later requested transition: Age -> All Stats") &&
                classified.Contains("destination not confirmed visible"));
            Check("distinguishes UI exception evidence from crash causality",
                classified.Contains("Creator / UI=1") &&
                classified.Contains("Campaign / family=0") &&
                classified.Contains("Most recent exception area: Creator / UI") &&
                classified.Contains("NOT A CRASH CAUSE"));
            System.IO.File.AppendAllText(System.IO.Path.Combine(
                classifier,"exceptions.csv"),
                "2026-11-20T01:00:04Z,fixture,campaign_registration,"+
                "System.Exception,Campaign error,stack\n");
            classified=DiagnosticSummaryPolicy.Build(classifier,
                "normal_shutdown:unit_test",false);
            Check("mixed UI and campaign errors remain separately classified",
                classified.Contains("Creator / UI=1") &&
                classified.Contains("Campaign / family=1") &&
                classified.Contains("Both UI and campaign exception contexts"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(
                classifier,"exceptions.csv"),
                "utc,session_id,context,exception_type,message,stack_trace\n"+
                "2026-11-20T01:00:05Z,fixture,unhandled_managed_exception,"+
                "System.Exception,Unknown fault,stack\n");
            classified=DiagnosticSummaryPolicy.Build(classifier,
                "interrupted_previous_process",true);
            Check("unhandled exception without subsystem context stays unknown",
                classified.Contains("Other / unknown=1") &&
                classified.Contains("Most recent exception area: Other / unknown"));
            // Exact frame extraction comes from the saved exception
            // trace, not the last game event or a guessed UI method.
            string stackFixture=System.IO.Path.Combine(
                tempDiagnostic,"managed-stack-fixtures");
            System.IO.Directory.CreateDirectory(stackFixture);
            string stackFile=System.IO.Path.Combine(
                stackFixture,"exceptions.csv");
            string sourcePath=@"C:\Users\PrivatePlayer\Source\CUNativeStatsLayer.cs";
            string normalTrace="System.InvalidOperationException: slider error\n"+
                "   at CalradiaUnified.CUNativeStatsVM.Refresh() in "+
                sourcePath+":line 749\n"+
                "   at CalradiaUnified.CUNativeStatsVM.ExecuteContinue()";
            string frameRow=String.Join(",",new[]{
                "2026-11-20T01:01:00Z","fixture",
                "cu_native_stats_attach_failed",
                "System.InvalidOperationException","slider error",
                normalTrace}.Select(DiagnosticJournal.CsvField));
            System.IO.File.WriteAllText(stackFile,
                DiagnosticJournal.ExceptionHeader+"\n"+frameRow+"\n");
            string frameSummary=DiagnosticSummaryPolicy.Build(
                stackFixture,"normal_shutdown:unit_test",false);
            Check("managed exception summary reports the first method",
                frameSummary.Contains(
                    "Method: CalradiaUnified.CUNativeStatsVM.Refresh") &&
                frameSummary.Contains(
                    "Source (filename only): CUNativeStatsLayer.cs:line 749") &&
                !frameSummary.Contains("PrivatePlayer"));
            Check("managed frame is not presented as proven root cause",
                frameSummary.Contains(
                    "It does not prove the method caused the session to end."));
            string innerTrace="System.Exception: outer ---> "+
                "System.ArgumentException: inner\n"+
                "   at CalradiaUnified.SandboxFamilyCreator.BuildChild()\n"+
                "   --- End of inner exception stack trace ---\n"+
                "   at CalradiaUnified.SandboxFamilyCreator.CreateFamily()";
            frameRow=String.Join(",",new[]{
                "2026-11-20T01:01:01Z","fixture",
                "cu_family_partial_creation",
                "System.Exception","nested",innerTrace}
                .Select(DiagnosticJournal.CsvField));
            System.IO.File.WriteAllText(stackFile,
                DiagnosticJournal.ExceptionHeader+"\n"+frameRow+"\n");
            frameSummary=DiagnosticSummaryPolicy.Build(
                stackFixture,"normal_shutdown:unit_test",false);
            Check("nested exception reports first inner frame with caveat",
                frameSummary.Contains(
                    "Method: CalradiaUnified.SandboxFamilyCreator.BuildChild") &&
                frameSummary.Contains("inner exception"));
            string asyncTrace="System.Exception: async exception\n"+
                "   at CalradiaUnified.CreatorRuntime+"+
                "<NextPageAsync>d__18.MoveNext()";
            frameRow=String.Join(",",new[]{
                "2026-11-20T01:01:02Z","fixture",
                "cu_native_stats_attach_failed",
                "System.Exception","async",asyncTrace}
                .Select(DiagnosticJournal.CsvField));
            System.IO.File.WriteAllText(stackFile,
                DiagnosticJournal.ExceptionHeader+"\n"+frameRow+"\n");
            frameSummary=DiagnosticSummaryPolicy.Build(
                stackFixture,"normal_shutdown:unit_test",false);
            Check("async state machine receives compiler-frame caveat",
                frameSummary.Contains(
                    "Method: CalradiaUnified.CreatorRuntime+") &&
                frameSummary.Contains("async or iterator state-machine frame"));
            frameRow=String.Join(",",new[]{
                "2026-11-20T01:01:03Z","fixture",
                "cu_native_stats_attach_failed",
                "System.Exception","no frames",
                "not a parseable managed stack"}
                .Select(DiagnosticJournal.CsvField));
            System.IO.File.WriteAllText(stackFile,
                DiagnosticJournal.ExceptionHeader+"\n"+frameRow+"\n");
            frameSummary=DiagnosticSummaryPolicy.Build(
                stackFixture,"normal_shutdown:unit_test",false);
            Check("missing managed frame stays unknown",
                frameSummary.Contains(
                    "No parseable managed method in the saved exception stack.") &&
                !frameSummary.Contains("Method:"));
            // A newer handled exception without a usable stack must not
            // conceal the earlier exception that reported a method.
            string firstWithFrame=String.Join(",",new[]{
                "2026-11-20T01:01:04Z","fixture",
                "cu_native_stats_attach_failed",
                "System.InvalidOperationException","frame available",
                normalTrace}.Select(DiagnosticJournal.CsvField));
            string newerWithoutFrame=String.Join(",",new[]{
                "2026-11-20T01:01:05Z","fixture",
                "campaign_registration",
                "System.Exception","no stack here",
                "no stack available"}.Select(DiagnosticJournal.CsvField));
            System.IO.File.WriteAllText(stackFile,
                DiagnosticJournal.ExceptionHeader+"\n"+
                firstWithFrame+"\n"+newerWithoutFrame+"\n");
            frameSummary=DiagnosticSummaryPolicy.Build(
                stackFixture,"normal_shutdown:unit_test",false);
            Check("later stackless exception retains last parseable frame",
                frameSummary.Contains(
                    "Frame source context: cu_native_stats_attach_failed") &&
                frameSummary.Contains(
                    "Method: CalradiaUnified.CUNativeStatsVM.Refresh") &&
                frameSummary.Contains("Context: campaign_registration"));
            System.IO.File.Delete(System.IO.Path.Combine(
                classifier,"exceptions.csv"));
            classified=DiagnosticSummaryPolicy.Build(classifier,
                "interrupted_previous_process",true);
            Check("interrupted process without exception remains unclassified",
                classified.Contains("Interruption is unclassified.") &&
                classified.Contains("Page: Age") &&
                classified.Contains("Later requested transition: Age -> All Stats"));
            DiagnosticSessionReporter.OverrideGameRootForTests=null;
            string autoConfig=System.IO.Path.Combine(tempDiagnostic,
                "auto-report-test.ini");
            System.IO.File.WriteAllText(autoConfig,
                "EnableDiagnostics=true\nAutoSessionReports=false\n");
            Check("automatic session reports default on but can be disabled",
                new Settings().AutoSessionReports &&
                !Settings.LoadFromPaths(
                    System.IO.Path.Combine(tempDiagnostic,"not-there.ini"),
                    autoConfig).AutoSessionReports);
            string exported = DiagnosticJournal.ExportSnapshot();
            Check("diagnostic export contains finance and crash evidence",
                System.IO.File.Exists(System.IO.Path.Combine(exported, "finance.csv")) &&
                System.IO.File.Exists(System.IO.Path.Combine(exported, "exceptions.csv")) &&
                System.IO.File.Exists(System.IO.Path.Combine(exported, "events.csv")));
            DiagnosticJournal.Enabled = false;
            DiagnosticJournal.Event("disabled", "should_not_be_written");
            Check("opt-out stops writing diagnostic journal",
                !System.IO.File.ReadAllText(System.IO.Path.Combine(tempDiagnostic, "events.csv"))
                    .Contains("should_not_be_written"));
        }
        finally
        {
            DiagnosticJournal.Enabled = true;
            DiagnosticJournal.OverrideRootForTests = null;
            DiagnosticSessionReporter.OverrideDesktopDirectoryForTests = null;
            if (System.IO.Directory.Exists(tempDiagnostic))
                System.IO.Directory.Delete(tempDiagnostic, true);
        }
        Console.WriteLine("PASS: " + passed + " gameplay policy checks");
    }
}
