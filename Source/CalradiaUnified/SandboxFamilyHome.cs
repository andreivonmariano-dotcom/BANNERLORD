using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// Place real generated relatives at an owned castle or town. Adult
    /// spouse uses the native teleport action. Children retain their native
    /// IsChild and NotSpawned state, but are recorded as staying in the fief.
    /// Never enlist infants in a MobileParty or make them adult heroes.
    /// </summary>
    internal static class SandboxFamilyHome
    {
        internal static string TryPlace(SandboxOriginProfile profile,Hero player,
            string startingFiefOutcome)
        {
            if(profile!=null && profile.FamilyHomeMode=="None")
            {
                DiagnosticJournal.Event("cu_family_home_result",
                    "result=NOT_REQUESTED|mode=None"+
                    "|no_fixed_residence_promised=true");
                return "not_requested:no_fixed_home";
            }
            if (profile == null || player == null || player.Spouse == null ||
                Clan.PlayerClan == null)
                return "skipped:no_registered_player_family";

            string refusal;
            var target = Resolve(profile.FamilyHomeMode,
                profile.FamilyHomeId, startingFiefOutcome, out refusal);
            if (target == null)
            {
                DiagnosticJournal.Event("cu_family_home_result",
                    "result=SKIP|reason=" + refusal +
                    "|mode=" + profile.FamilyHomeMode);
                return "skipped:" + refusal;
            }
            return PlaceAt(player, target, "generated_start");
        }

        // Deliberately explicit in-campaign action, not a sweep of every
        // hero on game load. This can safely home a family from an older save.
        internal static string PlaceExistingFamilyAt(Hero player,
            Settlement target)
        {
            if (player == null || player.Spouse == null ||
                Clan.PlayerClan == null || target == null)
                return "refused:no_player_family_or_target";
            return PlaceAt(player, target, "explicit_settlement_menu");
        }

        // Read-only startup preflight after native ownership exists.
        // A bad residence must not be discovered after creating a family.
        internal static string PreflightNativeHome(SandboxOriginProfile profile)
        {
            if(profile==null)return "missing_profile";
            if(!profile.SpouseEnabled || profile.FamilyHomeMode=="None")return "";
            string refusal;
            return Resolve(profile.FamilyHomeMode,profile.FamilyHomeId,"",out refusal)!=null?
                "":refusal;
        }

        private static Settlement Resolve(string mode,string id,
            string startingFiefOutcome, out string refusal)
        {
            refusal = "";
            var clan = Clan.PlayerClan;
            if (clan == null)
            {
                refusal = "missing_player_clan";
                return null;
            }
            if (mode == "SpecificOwned")
            {
                var exact = Settlement.Find(id);
                if (exact == null)
                {
                    refusal = "unknown_family_home_id";
                    return null;
                }
                refusal = Check(exact);
                return refusal.Length == 0 ? exact : null;
            }

            // Respect the exact native fief awarded in this startup, when
            // the family-home mode is automatic. Do not use other clans'
            // holdings merely because they are in the same kingdom.
            if (mode == "OwnedFief" &&
                !String.IsNullOrEmpty(startingFiefOutcome) &&
                (startingFiefOutcome.StartsWith("granted:",
                    StringComparison.Ordinal) ||
                 startingFiefOutcome.StartsWith("already_owned:",
                    StringComparison.Ordinal)))
            {
                int colon = startingFiefOutcome.IndexOf(':');
                var newFief = Settlement.Find(
                    startingFiefOutcome.Substring(colon+1));
                if (newFief != null && Check(newFief).Length == 0)
                    return newFief;
            }

            if(clan.Settlements==null)
            {refusal="no_safe_owned_town_or_castle";return null;}
            var owned = clan.Settlements.Where(s =>
                s != null && Check(s).Length == 0)
                .OrderBy(s => s.IsTown ? 0 : 1)
                .ThenBy(s => s.StringId,StringComparer.Ordinal)
                .ToArray();

            if (mode == "ClanHome" &&
                clan.HomeSettlement != null &&
                Check(clan.HomeSettlement).Length == 0)
                return clan.HomeSettlement;

            if (clan.HomeSettlement != null &&
                Check(clan.HomeSettlement).Length == 0 &&
                mode == "OwnedFief")
                return clan.HomeSettlement;

            if (owned.Length > 0) return owned[0];
            refusal = "no_safe_owned_town_or_castle";
            return null;
        }

        private static string Check(Settlement target)
        {
            if (target == null) return "missing_settlement";
            return SandboxFamilyHomePolicy.ValidateSettlement(
                target.OwnerClan == Clan.PlayerClan,
                target.IsFortification, target.IsUnderSiege,
                target.Party != null && target.Party.MapEvent != null);
        }

        private static string PlaceAt(Hero player,Settlement target,
            string source)
        {
            string refusal = Check(target);
            if (refusal.Length != 0)
                return "refused:" + refusal;
            Hero spouse = player.Spouse;
            if (spouse == null || spouse.Clan != Clan.PlayerClan ||
                spouse.Spouse != player || spouse.IsPrisoner)
                return "refused:spouse_not_player_clan_or_in_captivity";
            if (!spouse.IsAlive || !spouse.IsActive || spouse.IsDisabled)
                return "refused:spouse_not_active";
            if (spouse.PartyBelongedTo != null ||
                spouse.PartyBelongedToAsPrisoner != null)
                return "refused:spouse_in_party_or_captive";

            int childrenPlaced = 0;
            int childrenSkipped = 0;
            int minorsWithResidence = 0;
            int adultsSceneRegistered = 0;
            try
            {
                // TeleportHeroAction owns adult location and settlement events.
                // Only relocate a non-party spouse, not the player or army.
                if (spouse.StayingInSettlement != target)
                    TeleportHeroAction.ApplyImmediateTeleportToSettlement(
                        spouse,target);

                bool spouseAtHome = spouse.StayingInSettlement == target &&
                    spouse.CurrentSettlement == target;
                if (spouseAtHome)
                    spouse.UpdateLastKnownClosestSettlement(target);

                foreach (Hero child in player.Children)
                {
                    if (child == null) continue;
                    bool correctParents = child.Mother == player ||
                        child.Father == player;
                    correctParents = correctParents &&
                        (child.Mother == spouse || child.Father == spouse);
                    bool linkedToPlayer = child.Clan == Clan.PlayerClan &&
                        correctParents;
                    bool noParty = child.PartyBelongedTo == null &&
                        child.PartyBelongedToAsPrisoner == null;
                    if (!linkedToPlayer || !noParty || child.IsPrisoner ||
                        !child.IsAlive || child.IsDisabled)
                    {
                        childrenSkipped++;
                        continue;
                    }

                    bool placed;
                    bool sceneRegistered=false;
                    if (child.IsChild)
                    {
                        if (!SandboxFamilyHomePolicy.CanPlaceMinor(
                            child.Clan == Clan.PlayerClan,correctParents,
                            noParty,child.IsChild))
                        {
                            childrenSkipped++;
                            continue;
                        }
                        var nativeState = child.HeroState;
                        // Native underage Heroes are NotSpawned. Assign
                        // their persistent home, but do not demand a
                        // HeroesWithoutParty scene entry or pretend they can
                        // be recalled like an adult. This is independent of
                        // male/female Hero templates.
                        child.StayingInSettlement = target;
                        if (child.HeroState != nativeState)
                            throw new InvalidOperationException(
                                "Minor hero state changed during home placement");
                        placed = child.StayingInSettlement == target;
                        sceneRegistered =
                            target.HeroesWithoutParty.Contains(child);
                        if(placed)minorsWithResidence++;
                    }
                    else
                    {
                        // An adult offspring is a real Hero and can reside in
                        // the castle as an active character. Only promote a
                        // NotSpawned ADULT, never a child or disabled hero.
                        if (child.IsNotSpawned)
                            child.ChangeState(Hero.CharacterStates.Active);
                        if (!child.IsActive)
                        {
                            childrenSkipped++;
                            continue;
                        }
                        if (child.StayingInSettlement != target)
                            TeleportHeroAction.ApplyImmediateTeleportToSettlement(
                                child,target);
                        sceneRegistered =
                            target.HeroesWithoutParty.Contains(child);
                        placed = child.StayingInSettlement == target &&
                            child.CurrentSettlement == target;
                        if(placed && sceneRegistered)
                            adultsSceneRegistered++;
                    }
                    if (placed)
                    {
                        childrenPlaced++;
                        child.UpdateLastKnownClosestSettlement(target);
                    }
                    else childrenSkipped++;
                    DiagnosticJournal.Event("cu_family_child_home",
                        "child=" + child.StringId +
                        "|age=" + child.Age +
                        "|is_minor=" + child.IsChild +
                        "|gender=" + (child.IsFemale ? "Female":"Male") +
                        "|state=" + child.HeroState +
                        "|settlement=" + target.StringId +
                        "|residence_recorded=" + placed +
                        "|scene_registered=" + sceneRegistered +
                        "|status=" + SandboxFamilyResidencePolicy.Classify(
                            child.IsChild,placed,sceneRegistered) +
                        "|native_child_recall=" +
                            SandboxFamilyResidencePolicy.EligibleForAdultRecall(
                                child.IsChild,child.IsAlive,child.IsActive,
                                child.IsDisabled,child.IsPrisoner) +
                        "|party=none");
                }

                bool success = spouseAtHome && childrenSkipped == 0;
                DiagnosticJournal.Event("cu_family_home_result",
                    "result=" + (success ? "PASS" : "PARTIAL") +
                    "|source=" + source +
                    "|settlement=" + target.StringId +
                    "|spouse=" + spouse.StringId +
                    "|spouse_at_home=" + spouseAtHome +
                    "|children_homed=" + childrenPlaced +
                    "|minor_residence_only=" + minorsWithResidence +
                    "|adults_scene_registered=" + adultsSceneRegistered +
                    "|children_skipped=" + childrenSkipped +
                    "|minor_state_unchanged=true" +
                    "|underage_not_party_recallable=true");
                return (success ? "placed:" : "partial:") +
                    target.StringId + ":children=" + childrenPlaced;
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception("cu_family_home_exception",error);
                return "failed:" + error.GetType().Name;
            }
        }
    }
}
