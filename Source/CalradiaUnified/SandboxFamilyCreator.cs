using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Explicitly opted-in creation of real, clan-owned relatives.
    /// Uses native HeroCreator and MarriageAction, not fabricated roster units.
    /// Must pass new-game and save/reload acceptance before production use.
    /// </summary>
    public static class SandboxFamilyCreator
    {
        public static string TryCreate(SandboxOriginProfile profile, Hero player)
        {
            if (profile == null || !profile.RealFamilyEnabled || player == null ||
                Clan.PlayerClan == null) return "not_enabled";
            // Automatic selection avoids a same-sex default when the player's
            // sex is not known until the native appearance stage has finished.
            bool spouseFemale = SandboxFamilyPolicy.ResolveSpouseFemale(
                profile.SpouseGender,player.IsFemale);
            string reason;
            if (!profile.IsValid((int)player.Age, out reason))
                return "invalid_family:" + reason;
            // Bannerlord's marriage model rejects inactive heroes. Check the
            // player before allocating a new person to the campaign world.
            if (profile.SpouseEnabled && !player.CanMarry())
            {
                DiagnosticJournal.Event("cu_family_preflight_refused",
                    "reason=player_cannot_marry" +
                    "|player_id=" + player.StringId +
                    "|age=" + player.Age +
                    "|state=" + player.HeroState +
                    "|clan=" + (player.Clan == null ?
                        "none" : player.Clan.StringId));
                return "invalid_player_cannot_marry";
            }
            string guard = SandboxFamilyPolicy.Guard(profile.SpouseEnabled,
                player.Spouse != null, profile.ChildCount,
                player.Children != null && player.Children.Count > 0,
                profile.SiblingCount,
                player.Father != null || player.Mother != null ||
                    // Native Sandbox often has no registered parents.
                    // We preflight suitable native templates and create
                    // real shared parents before new sibling heroes.
                    profile.SiblingCount > 0,
                spouseFemale == player.IsFemale, true);
            if (guard.Length > 0) return guard;
            for (int i = 0; i < profile.ChildCount; i++)
                if (!SandboxFamilyPolicy.ValidChildAge(
                    profile.Children[i].Age, (int)player.Age,
                    profile.SpouseEnabled, profile.SpouseAge))
                    return "invalid_child_age:" + (i+1);
            // Resolve native birth-clan ancestry before any new Hero is made.
            // This is a NEW spouse, not an existing clan daughter/son recruited.
            Clan birthClan = Clan.PlayerClan;
            Hero birthFather = null;
            Hero birthMother = null;
            string birthError;
            if (profile.SpouseEnabled &&
                !SandboxGeneratedSpouseOrigin.TryResolve(profile,player,
                    out birthClan,out birthFather,out birthMother,
                    out birthError))
            {
                DiagnosticJournal.Event("cu_family_origin_refused",
                    "mode=" + profile.SpouseBackground +
                    "|reason=" + birthError);
                return "invalid_noble_origin:" + birthError;
            }

            // Do not start creating unrelated heroes if the correct source
            // templates cannot be resolved up front.
            var spouseTemplate = profile.SpouseEnabled ?
                // Generated family mode does not reuse stale explicit
                // face-template IDs from earlier versions of the editor.
                FindTemplate(player,profile.SpouseCulture,
                    spouseFemale,"") : null;
            if (profile.SpouseEnabled && spouseTemplate == null)
                return "spouse_template_missing";

            // Native offspring templates depend on BOTH actual parent heroes.
            // Resolve each source after the spouse exists, rather than treating
            // a minor child like a random wanderer/noble from CharacterObject.All.
            // Siblings need the player's actual parents. If Bannerlord has no
            // registered parent heroes, creating strangers and calling them
            // siblings is not acceptable.
            // The policy gate refuses fake siblings with no real native parent.
            var siblingTemplates = new List<CharacterObject>();
            for (int i = 0; i < profile.SiblingCount; i++)
            {
                var sibling = profile.Siblings[i];
                var source = FindTemplate(player,sibling.Culture,
                    sibling.Gender == "Female",sibling.FaceSeed);
                if (source == null)
                    return "sibling_template_missing:" + (i+1);
                siblingTemplates.Add(source);
            }

            // Siblings must have actual parents, not invented relation
            // labels. Generate missing shared parent Heroes from genuine
            // same-culture noble templates before creating any siblings.
            string lineage=EnsureSiblingParents(profile,player);
            if(lineage.StartsWith("refused:",StringComparison.Ordinal) ||
               lineage.StartsWith("failed:",StringComparison.Ordinal))
                return lineage;

            Hero spouseHero = null;
            int madeChildren=0;
            int madeSiblings=0;
            // Track even a newly allocated child whose post-create setup
            // fails before the successful child counter increments.
            var allocatedChildren=new List<Hero>();
            var provisionalSiblings=new List<Hero>();
            try
            {
                if (profile.SpouseEnabled)
                {
                    spouseHero = Create(spouseTemplate,profile.SpouseName,
                        profile.SpouseAge,birthClan);
                    if (birthFather != null && birthMother != null)
                    {
                        spouseHero.Father = birthFather;
                        spouseHero.Mother = birthMother;
                        if (!birthFather.Children.Contains(spouseHero) ||
                            !birthMother.Children.Contains(spouseHero))
                            throw new InvalidOperationException(
                                "Generated noble spouse has incomplete ancestry");
                    }
                    DiagnosticJournal.Event("cu_family_generated_spouse",
                        "id=" + spouseHero.StringId +
                        "|background=" + profile.SpouseBackground +
                        "|origin_clan=" + (spouseHero.OriginClan == null ?
                            "none" : spouseHero.OriginClan.StringId) +
                        "|father=" + (birthFather == null ?
                            "none" : birthFather.StringId) +
                        "|mother=" + (birthMother == null ?
                            "none" : birthMother.StringId) +
                        "|new_hero=true");
                    // A fallback native noble source can have another culture.
                    // The requested culture is applied to the new hero only,
                    // never to the donor character from the campaign.
                    var desiredCulture = ResolveCulture(player,
                        profile.SpouseCulture);
                    if (desiredCulture == null)
                        throw new InvalidOperationException(
                            "Requested spouse culture missing");
                    spouseHero.Culture = desiredCulture;
                    // Apply the game's marriage action. This synchronizes clan,
                    // spouse references and the native marriage event.
                    // Native MarriageAction silently returns for unsuitable
                    // couples. Check that condition and both references, never
                    // claim a decorative spouse was successfully created.
                    bool nativeEligible = Campaign.Current.Models.MarriageModel
                        .IsCoupleSuitableForMarriage(player,spouseHero);
                    DiagnosticJournal.Event("cu_family_marriage_gate",
                        "native_eligible=" + nativeEligible +
                        "|player_age=" + player.Age +
                        "|spouse_age=" + spouseHero.Age +
                        "|player_can_marry=" + player.CanMarry() +
                        "|spouse_can_marry=" + spouseHero.CanMarry() +
                        "|player_clan=" + (player.Clan == null ?
                            "none" : player.Clan.StringId) +
                        "|spouse_clan=" + (spouseHero.Clan == null ?
                            "none" : spouseHero.Clan.StringId));
                    // The vanilla marriage model requires both heroes to be
                    // ACTIVE and clan-owned. Log the exact known prerequisites
                    // before asking the model for the final decision.
                    string prerequisite = SandboxGeneratedMarriagePolicy.Check(
                        player.IsActive, spouseHero.IsActive,
                        player.CanMarry(), spouseHero.CanMarry(),
                        player.Clan != null && Campaign.Current.Models
                            .MarriageModel.IsClanSuitableForMarriage(player.Clan),
                        spouseHero.Clan != null && Campaign.Current.Models
                            .MarriageModel.IsClanSuitableForMarriage(spouseHero.Clan),
                        player.IsFemale != spouseHero.IsFemale);
                    DiagnosticJournal.Event("cu_family_marriage_prerequisites",
                        "result=" + (prerequisite.Length == 0 ? "PASS" : prerequisite) +
                        "|spouse_state=" + spouseHero.HeroState +
                        "|spouse_occupation=" + spouseHero.Occupation +
                        "|spouse_is_template=" + spouseHero.IsTemplate +
                        "|spouse_is_active=" + spouseHero.IsActive +
                        "|spouse_character_age=" + spouseHero.CharacterObject.Age +
                        "|spouse_can_marry=" + spouseHero.CanMarry());
                    if (prerequisite.Length != 0 || !nativeEligible)
                        throw new InvalidOperationException(
                            "Native marriage refused: " +
                            (prerequisite.Length == 0 ?
                                "other_native_marriage_rule" : prerequisite));
                    MarriageAction.Apply(player,spouseHero,false);
                    if (!SandboxFamilyAgePolicy.MatchesRequestedAge(
                        spouseHero.Age,profile.SpouseAge))
                        throw new InvalidOperationException(
                            "Spouse birthday changed during native marriage");
                    DiagnosticJournal.Event("cu_family_spouse_age_verified",
                        "requested=" + profile.SpouseAge +
                        "|actual=" + spouseHero.Age +
                        "|source=native_hero_birthday");
                    if (player.Spouse != spouseHero ||
                        spouseHero.Spouse != player)
                        throw new InvalidOperationException(
                            "Native marriage did not establish both spouse references");
                    if (spouseHero.Clan != Clan.PlayerClan)
                        throw new InvalidOperationException(
                            "Spouse was not moved into the player's clan");
                    if(SandboxFamilyPolicy.NobleFamilyOccupation(profile.Role))
                        VerifyFamilyOccupation(spouseHero,Occupation.Lord,"spouse");
                    // The new Hero now belongs to the real married
                    // family. Train native skills, set a distinct
                    // personality and establish actual starting relations.
                    SandboxFamilyDevelopment.Spouse(
                        profile,player,spouseHero);
                    if((profile.MercenaryGuidedStart ||
                        profile.SoldierGuidedStart) &&
                        profile.SpouseBackground=="Commoner")
                    {
                        // Native marriage action has already verified the
                        // household. A commoner spouse is not granted
                        // aristocratic office by a mercenary contract.
                        spouseHero.SetNewOccupation(Occupation.Wanderer);
                        DiagnosticJournal.Event(
                            "cu_mercenary_commoner_spouse_status",
                            "hero="+spouseHero.StringId+
                            "|native_marriage_verified=true"+
                            "|occupation="+spouseHero.Occupation+
                            "|no_hereditary_lord_title=true");
                    }
                    DiagnosticJournal.Event("cu_family_spouse_created",
                        "id=" + spouseHero.StringId +
                        "|birth_background=" + profile.SpouseBackground +
                        "|birth_clan=" + (spouseHero.OriginClan == null ?
                            "none" : spouseHero.OriginClan.StringId) +
                        "|age=" + spouseHero.Age +
                        "|gender=" + (spouseHero.IsFemale ? "Female" : "Male") +
                        "|clan=" + spouseHero.Clan.StringId);
                }
                for (int i = 0; i < profile.ChildCount; i++)
                {
                    var childProfile=profile.Children[i];
                    Hero mother = player.IsFemale ? player : spouseHero;
                    Hero father = player.IsFemale ? spouseHero : player;
                    if (mother == null || father == null)
                        throw new InvalidOperationException(
                            "Child requires both verified native parent heroes");
                    var childTemplate = Campaign.Current.Models.HeroCreationModel
                        .GetCharacterTemplateForOffspring(mother, father,
                            childProfile.Gender == "Female");
                    if (childTemplate == null)
                        throw new InvalidOperationException(
                            "Native offspring template unavailable for child " + (i+1));
                    Hero child = HeroCreator.CreateChild(childTemplate,
                        player.HomeSettlement, Clan.PlayerClan, childProfile.Age);
                    if (child == null)
                        throw new InvalidOperationException(
                            "Native CreateChild returned null");
                    allocatedChildren.Add(child);
                    child.SetName(new TextObject(childProfile.Name),
                        new TextObject(childProfile.Name));
                    // Native CreateChild can use the template birthday.
                    // The user's requested year is the authoritative plan.
                    float nativeChildAge = child.Age;
                    child.SetBirthDay(CampaignTime.YearsFromNow(
                        -childProfile.Age));
                    if (!SandboxFamilyAgePolicy.MatchesRequestedAge(
                        child.Age,childProfile.Age))
                        throw new InvalidOperationException(
                            "Generated child has the wrong native birthday: " +
                            (i+1));
                    DiagnosticJournal.Event("cu_family_child_age_verified",
                        "index=" + (i+1) +
                        "|requested=" + childProfile.Age +
                        "|native_initial=" + nativeChildAge +
                        "|actual=" + child.Age);
                    // CreateChild initializes with isOffspring=false. Supply
                    // actual parents, then ask the native offspring model to
                    // blend their body properties rather than keeping an
                    // arbitrary cloned adult face.
                    child.IsFemale = childProfile.Gender == "Female";
                    child.Mother = mother;
                    child.Father = father;
                    child.StaticBodyProperties = Campaign.Current.Models
                        .HeroCreationModel.GetStaticBodyProperties(child,true);
                    // Hero.BodyProperties dynamically uses the real Age,
                    // but a parent-derived adult beard can make a teenager
                    // look much older. Keep his inherited hair/face and
                    // use a clean-shaven native FaceGen beard for ages 18-21.
                    if(!child.IsFemale &&
                        SandboxFamilySkillInheritancePolicy.IsYouth(
                            childProfile.Age))
                    {
                        // Pinned Hero 1.5.7 does not expose BeardTags
                        // as a public member. Use the actual FaceGen API
                        // to select its clean-shaven beard index.
                        child.ModifyHair(-1,0,-1);
                        DiagnosticJournal.Event("cu_family_youth_face",
                            "id="+child.StringId+
                            "|actual_age="+child.Age+
                            "|native_facegen=true|beard=clean_shaven"+
                            "|inherited_face_kept=true");
                    }
                    // Minor nobles need the same occupation as adult nobles.
                    // Occupation and activation are independent; never activate a child.
                    if(SandboxFamilyPolicy.NobleFamilyOccupation(profile.Role))
                        VerifyFamilyOccupation(child,Occupation.Lord,"child");
                    // An adult is a usable native clan Hero even if there is
                    // no valid family-owned castle to assign as a residence.
                    // Never activate an underage native Hero.
                    if (!child.IsChild)
                    {
                        if(profile.MercenaryGuidedStart ||
                            profile.SoldierGuidedStart)
                            child.SetNewOccupation(Occupation.Wanderer);
                        else
                            child.SetNewOccupation(Occupation.Lord);
                        if (child.IsNotSpawned)
                            child.ChangeState(Hero.CharacterStates.Active);
                        if (!child.IsActive)
                            throw new InvalidOperationException(
                                "Adult offspring could not be activated: " +
                                (i+1));
                    }
                    DiagnosticJournal.Event("cu_family_child_life_stage",
                        "index=" + (i+1) +
                        "|requested_age=" + childProfile.Age +
                        "|actual_age=" + child.Age +
                        "|is_minor=" + child.IsChild +
                        "|active=" + child.IsActive +
                        "|state=" + child.HeroState +
                        "|clan=" + (child.Clan == null ?
                            "none" : child.Clan.StringId));
                    DiagnosticJournal.Event("cu_family_inherited_appearance",
                        "child=" + child.StringId +
                        "|mother=" + mother.StringId +
                        "|father=" + father.StringId +
                        "|source=native_hero_creation_model");
                    DiagnosticJournal.Event("cu_family_child_source",
                        "index=" + (i+1) +
                        "|template=" + childTemplate.StringId +
                        "|native_offspring_model=true");
                    if (child.Father != father || child.Mother != mother ||
                        child.Clan != Clan.PlayerClan ||
                        !father.Children.Contains(child) ||
                        !mother.Children.Contains(child))
                        throw new InvalidOperationException(
                            "Child was created without complete native family links");
                    SandboxFamilyDevelopment.Child(
                        profile,player,spouseHero,child,childProfile,i);
                    if(SandboxFamilyPolicy.NobleFamilyOccupation(profile.Role))
                        VerifyFamilyOccupation(child,Occupation.Lord,"child_final");
                    madeChildren++;
                    DiagnosticJournal.Event("cu_family_child_created",
                        "id=" + child.StringId + "|age=" + childProfile.Age);
                }
                for (int i = 0; i < profile.SiblingCount; i++)
                {
                    var siblingProfile=profile.Siblings[i];
                    bool birthHouseholdResident=
                        SandboxNobleStartPolicy.IsSonOrigin(
                            profile.NobleBirthPosition) &&
                        siblingProfile.Age>player.Age &&
                        player.Father!=null &&
                        player.Father.Clan!=null &&
                        player.Father.Clan!=Clan.PlayerClan;
                    bool birthHouseholdHeir=birthHouseholdResident &&
                        siblingProfile.Gender=="Male";
                    // An older native noble heir belongs to the father's
                    // house. He does not automatically enlist in a younger
                    // son's party or move into the junior clan.
                    Hero sibling=Create(siblingTemplates[i],siblingProfile.Name,
                        siblingProfile.Age,birthHouseholdResident?
                        player.Father.Clan:null);
                    provisionalSiblings.Add(sibling);
                    sibling.Father=player.Father;
                    sibling.Mother=player.Mother;
                    var siblingCulture=ResolveCulture(player,
                        siblingProfile.Culture);
                    if(siblingCulture==null)
                        throw new InvalidOperationException(
                            "Sibling culture not found");
                    sibling.Culture=siblingCulture;
                    // Merchant, soldier and wandering families do not
                    // automatically become titled lords. Native spouse
                    // marriage setup separately retains game requirements.
                    if(birthHouseholdHeir || SandboxFamilyPolicy.NobleFamilyOccupation(profile.Role))
                        VerifyFamilyOccupation(sibling,Occupation.Lord,"sibling");
                    else
                        sibling.SetNewOccupation(Occupation.Wanderer);
                    if (sibling.Father != null &&
                        !sibling.Father.Children.Contains(sibling))
                        sibling.Father.Children.Add(sibling);
                    if (sibling.Mother != null &&
                        !sibling.Mother.Children.Contains(sibling))
                        sibling.Mother.Children.Add(sibling);
                    if(birthHouseholdResident)
                    {
                        if(sibling.Clan!=player.Father.Clan)
                            throw new InvalidOperationException(
                                "Older noble sibling left birth clan");
                        var placement=SandboxBirthHouseholdPlacement.Place(
                            sibling,player.Father);
                        if(placement.StartsWith("refused:",
                            StringComparison.Ordinal) ||
                           placement.StartsWith("failed:",
                            StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                "Older noble sibling has no native home: "+
                                placement);
                        DiagnosticJournal.Event(
                            "cu_family_older_heir_stays_with_father",
                            "hero="+sibling.StringId+
                            "|father="+player.Father.StringId+
                            "|birth_clan="+sibling.Clan.StringId+
                            "|male_heir="+birthHouseholdHeir+
                            "|residence="+placement+
                            "|player_party_member=false");
                    }
                    else
                    {
                        // The explicitly chosen sibling accompanies the
                        // player as a real adult, not a random wanderer.
                        if(MobileParty.MainParty==null)
                            throw new InvalidOperationException(
                                "Player party missing for sibling");
                        AddHeroToPartyAction.Apply(
                            sibling,MobileParty.MainParty,true);
                        if(MobileParty.MainParty.MemberRoster.GetTroopCount(
                            sibling.CharacterObject)<1)
                            throw new InvalidOperationException(
                                "Sibling absent from native player party");
                    }
                    if(sibling.Father==null || sibling.Mother==null ||
                        sibling.Father!=player.Father ||
                        sibling.Mother!=player.Mother ||
                        !player.Father.Children.Contains(sibling) ||
                        !player.Mother.Children.Contains(sibling))
                        throw new InvalidOperationException(
                            "Sibling missing shared native parent references");
                    madeSiblings++;
                    provisionalSiblings.Remove(sibling);
                    DiagnosticJournal.Event("cu_family_sibling_created",
                        "id="+sibling.StringId+
                        "|father="+sibling.Father.StringId+
                        "|mother="+sibling.Mother.StringId+
                        "|culture="+sibling.Culture.StringId+
                        "|party_member="+(!birthHouseholdResident)+
                        "|birth_household_resident="+birthHouseholdResident+
                        "|birth_household_heir="+birthHouseholdHeir+
                        "|clan="+(sibling.Clan==null?
                            "none":sibling.Clan.StringId));
                }
                if(SandboxNobleStartPolicy.IsSonOrigin(
                    profile.NobleBirthPosition))
                {
                    int older=player.Father.Children.Count(h=>h!=null &&
                        h!=player && h.IsAlive && !h.IsFemale &&
                        h.Mother==player.Mother && h.Age>player.Age);
                    int expected=SandboxNobleStartPolicy.RequiredOlderSons(
                        profile.NobleBirthPosition);
                    if(!SandboxHouseholdLineagePolicy.ExactFinalRank(
                        expected,older))
                        throw new InvalidOperationException(
                            "House siblings altered the native noble birth order");
                    DiagnosticJournal.Event("cu_final_son_birth_rank",
                        "expected_older_sons="+expected+
                        "|actual_older_sons="+older+
                        "|house_siblings_created="+madeSiblings);
                }
            }
            catch (Exception e)
            {
                // One-shot application is committed before creation.
                // Never retry a partial family on another game tick.
                DiagnosticJournal.Exception("cu_family_partial_creation",e);
                // A rejected marriage previously stranded an adult noble in
                // a castle, leaving the user with no spouse and no children.
                // Compensate ONLY while both spouse references are absent and
                // no child has been created. Do not undo a real marriage.
                string cleanup = "native_relationship_preserved";
                if (spouseHero != null &&
                    SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(
                        player.Spouse == spouseHero ||
                        spouseHero.Spouse == player,
                        allocatedChildren.Count,
                        spouseHero.Children!=null &&
                        spouseHero.Children.Count>0))
                    cleanup = CleanupProvisionalHero(spouseHero,player);
                int cleanedUnlinkedSiblings=0;
                foreach(var incompleteSibling in provisionalSiblings)
                {
                    if(CleanupProvisionalHero(incompleteSibling,player)
                        =="disabled_uncommitted_hero")
                        cleanedUnlinkedSiblings++;
                }
                DiagnosticJournal.Event("cu_family_partial_result",
                    "cleanup=" + cleanup +
                    "|allocated_children=" + allocatedChildren.Count +
                    "|provisional_siblings_cleaned=" +
                        cleanedUnlinkedSiblings +
                    "|spouse_hero_created=" + (spouseHero != null) +
                    "|married=" + (player.Spouse == spouseHero && spouseHero != null) +
                    "|created_children=" + madeChildren +
                    "|requested_children=" + profile.ChildCount);
                return "partial:" + e.GetType().Name +
                    ":spouse_hero=" + (spouseHero != null) +
                    ":children=" + madeChildren + "/" + profile.ChildCount;
            }

            bool noble=SandboxFamilyPolicy.NobleFamilyOccupation(profile.Role);
            // Verify again after marriage, placement, development and native party actions.
            // Only newly created family identities from this invocation are touched.
            if(noble)
            {
                try
                {
                    if(spouseHero!=null && player.Spouse==spouseHero && spouseHero.Spouse==player)
                        VerifyFamilyOccupation(spouseHero,Occupation.Lord,"spouse_final");
                    foreach(var child in allocatedChildren.Where(h=>h!=null && h.Clan==Clan.PlayerClan &&
                        (h.Father==player || h.Mother==player)))
                        VerifyFamilyOccupation(child,Occupation.Lord,"child_final_check");
                    foreach(var sibling in provisionalSiblings.Where(h=>h!=null && player.Father!=null &&
                        player.Mother!=null && h.Father==player.Father && h.Mother==player.Mother))
                        VerifyFamilyOccupation(sibling,Occupation.Lord,"sibling_final");
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception("cu_family_occupation_failed",error);
                    return "failed:family_occupation_mismatch";
                }
            }
            bool verified = (!profile.SpouseEnabled ||
                    (player.Spouse == spouseHero && spouseHero != null &&
                     spouseHero.Clan == Clan.PlayerClan)) &&
                (madeChildren == profile.ChildCount) &&
                (madeSiblings == profile.SiblingCount);
            DiagnosticJournal.Event("cu_family_verification",
                "result=" + (verified ? "PASS" : "FAIL") +
                "|requested_spouse=" + profile.SpouseEnabled +
                "|actual_spouse=" + (player.Spouse == null ?
                    "none" : player.Spouse.StringId) +
                "|requested_children=" + profile.ChildCount +
                "|actual_children=" + (player.Children == null ?
                    0 : player.Children.Count) +
                "|created_children=" + madeChildren);
            return verified ?
                "spouse=" + (spouseHero == null ? "none" : spouseHero.StringId) +
                    "|children=" + madeChildren + "|siblings=" + madeSiblings :
                "failed:family_reference_mismatch";
        }

        /// <summary>
        /// Native Sandbox commonly initializes a player without parents.
        /// For explicit sibling starts, create missing parents as real
        /// same-culture clan Heroes and link the player to both. No
        /// pre-existing Hero, spouse or donor is rewritten.
        /// </summary>
        private static string EnsureSiblingParents(
            SandboxOriginProfile profile,Hero player)
        {
            if(profile.SiblingCount==0)return "not_requested";
            Hero father=player.Father;
            Hero mother=player.Mother;
            var eldest=Enumerable.Range(0,profile.SiblingCount)
                .Select(i=>profile.Siblings[i].Age)
                .Concat(new[]{(int)player.Age}).Max();
            if((father!=null && father.Age<eldest+16) ||
               (mother!=null && mother.Age<eldest+16))
                return "refused:native_parent_too_young";
            // Native parentage requires an age gap of at least sixteen.
            // Allow older founders without generating an impossible age 104 parent.
            int fatherAge=Math.Max(55,eldest+20);
            int motherAge=Math.Max(51,eldest+18);
            if((father==null && fatherAge>100) ||
               (mother==null && motherAge>100))
                return "refused:parent_age_over_100";
            CharacterObject fatherTemplate=father==null ?
                FindTemplate(player,"",false,"") : null;
            CharacterObject motherTemplate=mother==null ?
                FindTemplate(player,"",true,"") : null;
            if(father==null && fatherTemplate==null)
                return "refused:father_native_template_missing";
            if(mother==null && motherTemplate==null)
                return "refused:mother_native_template_missing";
            // Reserve the complete planned household before inventing
            // parent names. A generated mother must not share the name
            // of her daughter or the founder's spouse.
            var reservedNames=profile.Siblings.Take(profile.SiblingCount)
                .Select(x=>x.Name)
                .Concat(profile.Children.Take(profile.ChildCount)
                    .Select(x=>x.Name))
                .Concat(new[]{player.Name.ToString(),profile.SpouseName})
                .ToList();
            if(father!=null)reservedNames.Add(father.Name.ToString());
            if(mother!=null)reservedNames.Add(mother.Name.ToString());
            // The independent founding Lord needs real shared parent
            // references only if the player has requested siblings.
            // Do not create two active, living court members who could
            // be mistaken for owners, council officers or senior heirs.
            bool historicalOnly=(profile.Role=="Lord" ||
                profile.Role=="Outlaw" ||
                profile.MercenaryGuidedStart ||
                profile.SoldierGuidedStart) &&
                profile.NobleBirthPosition=="Independent";
            Hero newFather=null;
            Hero newMother=null;
            try
            {
                if(father==null)
                {
                    father=Create(fatherTemplate,SandboxCulturalNames.Suggest(
                        player.Culture.StringId,false,7,
                        reservedNames),fatherAge,null,!historicalOnly);
                    newFather=father;
                    father.Culture=player.Culture;
                    reservedNames.Add(father.Name.ToString());
                }
                if(mother==null)
                {
                    mother=Create(motherTemplate,SandboxCulturalNames.Suggest(
                        player.Culture.StringId,true,7,reservedNames),
                        motherAge,null,!historicalOnly);
                    newMother=mother;
                    mother.Culture=player.Culture;
                }
                if(father==mother)
                    throw new InvalidOperationException(
                        "Native parents must be different Heroes");
                if(player.Father==null)player.Father=father;
                if(player.Mother==null)player.Mother=mother;
                if(!father.Children.Contains(player))
                    father.Children.Add(player);
                if(!mother.Children.Contains(player))
                    mother.Children.Add(player);
                if(player.Father!=father || player.Mother!=mother ||
                   !father.Children.Contains(player) ||
                   !mother.Children.Contains(player))
                    throw new InvalidOperationException(
                        "Native player parent links are incomplete");
                if(historicalOnly &&
                    ((newFather!=null && newFather.IsActive) ||
                     (newMother!=null && newMother.IsActive)))
                    throw new InvalidOperationException(
                        "Independent founder parents unexpectedly active");
                if(historicalOnly)
                    DiagnosticJournal.Event(
                        profile.SoldierGuidedStart ?
                            "cu_independent_soldier_ancestry_recorded" :
                            profile.MercenaryGuidedStart ?
                            "cu_independent_mercenary_ancestry_recorded" :
                            profile.Role=="Outlaw" ?
                            "cu_independent_outlaw_ancestry_recorded" :
                            "cu_independent_lord_ancestry_recorded",
                        "native_sibling_genealogy=true"+
                        "|generated_parents_inactive="+
                            ((newFather==null || !newFather.IsActive) &&
                             (newMother==null || !newMother.IsActive))+
                        "|parents_not_party_officers=true"+
                        "|parents_not_removed_from_family_tree=true"+
                        "|no_inheritance_transfer=true");
                DiagnosticJournal.Event("cu_sibling_parents_created",
                    "father="+father.StringId+
                    "|mother="+mother.StringId+
                    "|father_age="+father.Age+
                    "|mother_age="+mother.Age+
                    "|player_linked=true|same_culture="+
                    (father.Culture==player.Culture &&
                     mother.Culture==player.Culture));
                return "parents_verified";
            }
            catch(Exception e)
            {
                // A native player parent is protected as soon as the real
                // parentage exists. Only fresh, unattached staging Heroes
                // can be disabled. Never rewrite an existing parent.
                string fatherCleanup=CleanupProvisionalHero(
                    newFather,player);
                string motherCleanup=CleanupProvisionalHero(
                    newMother,player);
                DiagnosticJournal.Exception(
                    "cu_sibling_native_parents_failed",e);
                DiagnosticJournal.Event(
                    "cu_sibling_parent_partial_cleanup",
                    "father="+fatherCleanup+"|mother="+motherCleanup+
                    "|native_parentage_preserved=true");
                return "failed:parent_creation_"+e.GetType().Name;
            }
        }

        private static void VerifyFamilyOccupation(Hero person,Occupation expected,string relation)
        {
            if(person==null || person.CharacterObject==null)
                throw new InvalidOperationException("Missing generated family hero");
            var before=person.Occupation;
            if(before!=expected)person.SetNewOccupation(expected);
            DiagnosticJournal.Event("cu_family_occupation",
                "hero="+person.StringId+"|relation="+relation+"|before="+before+
                "|expected="+expected+"|hero_occupation="+person.Occupation+
                "|character_occupation="+person.CharacterObject.Occupation+
                "|minor="+person.IsChild+"|state="+person.HeroState);
            if(person.Occupation!=expected || person.CharacterObject.Occupation!=expected)
                throw new InvalidOperationException("Generated family occupation not confirmed: "+relation);
        }
        private static Hero Create(CharacterObject source,string name,int age,
            Clan originClan=null,bool activate=true)
        {
            Hero result=null;
            try
            {
                result=HeroCreator.CreateSpecialHero(source,null,
                    originClan ?? Clan.PlayerClan,null,age);
                if(result==null)
                    throw new InvalidOperationException("HeroCreator returned null");
            result.SetName(new TextObject(name),new TextObject(name));
            // The native factory can override an explicitly passed age
            // for a wanderer-derived donor template. Setting the NEW
            // hero's birthday fixes that without changing the donor.
            float nativeInitialAge = result.Age;
            result.SetBirthDay(CampaignTime.YearsFromNow(-age));
            DiagnosticJournal.Event("cu_family_hero_exact_age",
                "hero=" + result.StringId +
                "|requested=" + age +
                "|native_initial=" + nativeInitialAge +
                "|actual=" + result.Age);
            if (!SandboxFamilyAgePolicy.MatchesRequestedAge(
                result.Age,age))
                throw new InvalidOperationException(
                    "Generated hero birthday does not match requested age");
            // CreateSpecialHero constructs a NotSpawned Hero by default.
            // Native IsSuitableForMarriage explicitly requires IsActive.
            // Activating must precede MarriageAction, not follow it.
            if (!activate && result.IsActive)
                throw new InvalidOperationException(
                    "Historical parent must not enter the active clan");
            if (age >= 18 && activate)
            {
                result.SetNewOccupation(Occupation.Lord);
                var originalState = result.HeroState;
                if (!result.IsActive)
                    result.ChangeState(Hero.CharacterStates.Active);
                DiagnosticJournal.Event("cu_family_new_adult_activated",
                    "hero=" + result.StringId +
                    "|previous_state=" + originalState +
                    "|state=" + result.HeroState +
                    "|active=" + result.IsActive +
                    "|occupation=" + result.Occupation +
                    "|clan=" + (result.Clan == null ?
                        "none" : result.Clan.StringId));
            }
            if(!activate)
                DiagnosticJournal.Event(
                    "cu_family_parent_historical_only",
                    "hero="+result.StringId+
                    "|active="+result.IsActive+
                    "|state="+result.HeroState+
                    "|native_parent_links_still_required=true");
                return result;
            }
            catch(Exception error)
            {
                // Native factory allocation can succeed before a birthday,
                // occupation or activation setter fails. Reclaim only an
                // unlinked fresh Hero, never a related native person.
                string cleanup=CleanupProvisionalHero(result,Hero.MainHero);
                DiagnosticJournal.Event("cu_family_factory_failure",
                    "stage=post_native_create"+
                    "|cleanup="+cleanup+"|error="+error.GetType().Name);
                throw;
            }
        }

        private static string CleanupProvisionalHero(Hero person,Hero player)
        {
            if(person==null)return "not_allocated";
            bool linkedToPlayer=player!=null &&
                (person==player || player.Father==person ||
                 player.Mother==person || player.Spouse==person ||
                 (player.Children!=null &&
                  player.Children.Contains(person)));
            bool inParty=person.PartyBelongedTo!=null ||
                person.PartyBelongedToAsPrisoner!=null;
            bool inSettlement=person.StayingInSettlement!=null ||
                person.CurrentSettlement!=null;
            if(!SandboxFamilyCreationSafetyPolicy.CanDisableProvisional(
                true,person.Spouse!=null,
                person.Children!=null && person.Children.Count>0,
                inParty,person.CompanionOf!=null,inSettlement,
                linkedToPlayer))
                return "preserved_native_references";
            try
            {
                // Never sever a recorded family relationship before the
                // native disable action has confirmed it succeeded.
                // On failure the person's genealogy stays intact.
                DisableHeroAction.Apply(person);
                if(!person.IsDisabled)
                    return "failed:native_disable_unconfirmed";
                if(person.Father!=null)
                {
                    person.Father.Children.Remove(person);
                    person.Father=null;
                }
                if(person.Mother!=null)
                {
                    person.Mother.Children.Remove(person);
                    person.Mother=null;
                }
                person.Clan=null;
                DiagnosticJournal.Event("cu_family_provisional_cleanup",
                    "hero="+person.StringId+
                    "|disabled_native=true"+
                    "|no_family_or_party_references=true");
                return "disabled_uncommitted_hero";
            }
            catch(Exception cleanupError)
            {
                DiagnosticJournal.Exception(
                    "cu_family_provisional_cleanup_failed",cleanupError);
                return "failed:"+cleanupError.GetType().Name;
            }
        }

        private static CultureObject ResolveCulture(Hero player,string cultureId)
        {
            if (String.IsNullOrEmpty(cultureId)) return player.Culture;
            return MBObjectManager.Instance.GetObject<CultureObject>(
                SandboxFamilyOriginPolicy.NormalizeCultureId(cultureId));
        }

        /// <summary>
        /// Prefer an exact native template. Some supported game versions ship
        /// few or no IsTemplate noble records for a given sex/culture. The
        /// last-resort donor is an existing living noble's CharacterObject.
        /// HeroCreator clones that character into a NEW Hero and leaves the
        /// source person untouched. Never substitute a troop/party member.
        /// </summary>
        private static CharacterObject FindTemplate(Hero player,string cultureId,
            bool female,string requestedTemplateId)
        {
            var culture = ResolveCulture(player,cultureId);
            if (culture == null)
            {
                DiagnosticJournal.Event("cu_family_template_scan",
                    "status=culture_missing|id=" + cultureId);
                return null;
            }

            if (!String.IsNullOrEmpty(requestedTemplateId))
            {
                var requested = MBObjectManager.Instance
                    .GetObject<CharacterObject>(requestedTemplateId);
                bool allowed = requested != null &&
                    requested.IsFemale == female &&
                    !requested.IsBasicTroop &&
                    (requested.IsTemplate || requested.IsHero) &&
                    requested.Culture != null;
                DiagnosticJournal.Event("cu_family_template_scan",
                    "mode=explicit|requested=" + requestedTemplateId +
                    "|valid=" + allowed);
                return allowed ? requested : null;
            }

            var templates = CharacterObject.All.Where(c =>
                c != null && c.IsTemplate && !c.IsBasicTroop &&
                c.IsFemale == female && c.Culture != null).ToArray();
            var local = templates.Where(c => c.Culture == culture).ToArray();
            var preferred = local.Where(c =>
                c.Occupation == Occupation.Lord ||
                c.Occupation == Occupation.Wanderer).ToArray();

            CharacterObject source = preferred
                .OrderBy(c => c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            string method = "local_named_template";
            if (source == null)
            {
                source = local.OrderBy(c => c.StringId,StringComparer.Ordinal)
                    .FirstOrDefault();
                method = "local_any_template";
            }

            if (source == null)
            {
                source = Hero.AllAliveHeroes.Where(h =>
                    h != null && h != player && h.IsActive && h.IsLord &&
                    h.IsFemale == female && h.Culture == culture &&
                    h.CharacterObject != null)
                    .OrderBy(h => h.StringId,StringComparer.Ordinal)
                    .Select(h => h.CharacterObject).FirstOrDefault();
                method = "local_existing_lord_clone";
            }

            if (source == null)
            {
                source = templates.OrderBy(c => c.StringId,StringComparer.Ordinal)
                    .FirstOrDefault();
                method = "foreign_native_template";
            }

            if (source == null)
            {
                source = Hero.AllAliveHeroes.Where(h =>
                    h != null && h != player && h.IsActive && h.IsLord &&
                    h.IsFemale == female && h.CharacterObject != null)
                    .OrderBy(h => h.StringId,StringComparer.Ordinal)
                    .Select(h => h.CharacterObject).FirstOrDefault();
                method = "foreign_existing_lord_clone";
            }

            DiagnosticJournal.Event("cu_family_template_scan",
                "culture=" + culture.StringId +
                "|female=" + female +
                "|total_gender_templates=" + templates.Length +
                "|local_templates=" + local.Length +
                "|preferred_local=" + preferred.Length +
                "|method=" + (source == null ? "none" : method) +
                "|source=" + (source == null ? "none" : source.StringId));
            return source;
        }
    }
}
