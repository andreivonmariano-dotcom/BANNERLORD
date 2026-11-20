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
    private static bool ResetAndCheck(SandboxOriginProfile plan)
    {
        plan.ResetForNewCampaign();
        return true;
    }
    private static void Main()
    {
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
            SandboxFamilyHomePolicy.ValidateRequest("None","",true) ==
                "family_home_mode_invalid");
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
            }
            DiagnosticJournal.Event("gameplay_test_event",
                "after_age_screen=stats_page");
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
                    archive.Entries.Any(e=>e.FullName=="README.txt"));
                Check("normal report excludes raw memory and saves",
                    !archive.Entries.Any(e=>
                        e.FullName.EndsWith(".dmp",
                            StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.EndsWith(".sav",
                            StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.EndsWith(".png",
                            StringComparison.OrdinalIgnoreCase)));
            }
            Check("local latest report pointer refers to successful ZIP",
                System.IO.File.ReadAllText(System.IO.Path.Combine(
                    tempDiagnostic,"latest-report.txt"))==current);
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
            if (System.IO.Directory.Exists(tempDiagnostic))
                System.IO.Directory.Delete(tempDiagnostic, true);
        }
        Console.WriteLine("PASS: " + passed + " gameplay policy checks");
    }
}
