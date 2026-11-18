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
            if (spouse.PartyBelongedTo != null ||
                spouse.PartyBelongedToAsPrisoner != null)
                return "refused:spouse_in_party_or_captive";

            int childrenPlaced = 0;
            int childrenSkipped = 0;
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
                    if (!linkedToPlayer || !noParty || child.IsPrisoner)
                    {
                        childrenSkipped++;
                        continue;
                    }

                    bool placed;
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
                        // StayingInSettlement persists in the game save,
                        // without altering a minor's NotSpawned lifecycle.
                        // Never teleport or recruit a baby into a party.
                        child.StayingInSettlement = target;
                        if (child.HeroState != nativeState)
                            throw new InvalidOperationException(
                                "Minor hero state changed during home placement");
                        placed = child.StayingInSettlement == target &&
                            child.CurrentSettlement == target &&
                            target.HeroesWithoutParty.Contains(child);
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
                        placed = child.StayingInSettlement == target &&
                            child.CurrentSettlement == target;
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
                        "|state=" + child.HeroState +
                        "|settlement=" + target.StringId +
                        "|registered=" + placed +
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
                    "|children_skipped=" + childrenSkipped +
                    "|minor_state_unchanged=true");
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
