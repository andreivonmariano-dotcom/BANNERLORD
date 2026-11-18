using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
                profile.SiblingCount, player.Father != null ||
                    player.Mother != null,
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

            Hero spouseHero = null;
            int madeChildren=0;
            int madeSiblings=0;
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
                    if (player.Spouse != spouseHero ||
                        spouseHero.Spouse != player)
                        throw new InvalidOperationException(
                            "Native marriage did not establish both spouse references");
                    if (spouseHero.Clan != Clan.PlayerClan)
                        throw new InvalidOperationException(
                            "Spouse was not moved into the player's clan");
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
                    child.SetName(new TextObject(childProfile.Name),
                        new TextObject(childProfile.Name));
                    // CreateChild initializes with isOffspring=false. Supply
                    // actual parents, then ask the native offspring model to
                    // blend their body properties rather than keeping an
                    // arbitrary cloned adult face.
                    child.IsFemale = childProfile.Gender == "Female";
                    child.Mother = mother;
                    child.Father = father;
                    child.StaticBodyProperties = Campaign.Current.Models
                        .HeroCreationModel.GetStaticBodyProperties(child,true);
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
                    madeChildren++;
                    DiagnosticJournal.Event("cu_family_child_created",
                        "id=" + child.StringId + "|age=" + childProfile.Age);
                }
                for (int i = 0; i < profile.SiblingCount; i++)
                {
                    var siblingProfile=profile.Siblings[i];
                    Hero sibling=Create(siblingTemplates[i],siblingProfile.Name,
                        siblingProfile.Age);
                    sibling.Father=player.Father;
                    sibling.Mother=player.Mother;
                    if (sibling.Father != null &&
                        !sibling.Father.Children.Contains(sibling))
                        sibling.Father.Children.Add(sibling);
                    if (sibling.Mother != null &&
                        !sibling.Mother.Children.Contains(sibling))
                        sibling.Mother.Children.Add(sibling);
                    madeSiblings++;
                    DiagnosticJournal.Event("cu_family_sibling_created",
                        "id=" + sibling.StringId);
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
                string cleanup = "not_required";
                if (spouseHero != null && madeChildren == 0 &&
                    player.Spouse != spouseHero && spouseHero.Spouse != player)
                    cleanup = CleanupUnmarriedGeneratedSpouse(spouseHero);
                DiagnosticJournal.Event("cu_family_partial_result",
                    "cleanup=" + cleanup +
                    "|spouse_hero_created=" + (spouseHero != null) +
                    "|married=" + (player.Spouse == spouseHero && spouseHero != null) +
                    "|created_children=" + madeChildren +
                    "|requested_children=" + profile.ChildCount);
                return "partial:" + e.GetType().Name +
                    ":spouse_hero=" + (spouseHero != null) +
                    ":children=" + madeChildren + "/" + profile.ChildCount;
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

        private static Hero Create(CharacterObject source,string name,int age,
            Clan originClan=null)
        {
            var result=HeroCreator.CreateSpecialHero(source,null,
                originClan ?? Clan.PlayerClan,null,age);
            if (result == null)
                throw new InvalidOperationException("HeroCreator returned null");
            result.SetName(new TextObject(name),new TextObject(name));
            // CreateSpecialHero constructs a NotSpawned Hero by default.
            // Native IsSuitableForMarriage explicitly requires IsActive.
            // Activating must precede MarriageAction, not follow it.
            if (age >= 18)
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
            return result;
        }

        private static string CleanupUnmarriedGeneratedSpouse(Hero spouse)
        {
            try
            {
                if (spouse.Spouse != null)
                    return "refused:hero_has_spouse";
                if (spouse.Father != null)
                {
                    spouse.Father.Children.Remove(spouse);
                    spouse.Father = null;
                }
                if (spouse.Mother != null)
                {
                    spouse.Mother.Children.Remove(spouse);
                    spouse.Mother = null;
                }
                DisableHeroAction.Apply(spouse);
                spouse.Clan = null;
                DiagnosticJournal.Event("cu_family_orphan_cleanup",
                    "hero=" + spouse.StringId +
                    "|state=" + spouse.HeroState +
                    "|clan=" + (spouse.Clan == null ?
                        "none" : spouse.Clan.StringId));
                return "disabled_unmarried_hero";
            }
            catch (Exception cleanupError)
            {
                DiagnosticJournal.Exception("cu_family_orphan_cleanup_failed",
                    cleanupError);
                return "failed:" + cleanupError.GetType().Name;
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
