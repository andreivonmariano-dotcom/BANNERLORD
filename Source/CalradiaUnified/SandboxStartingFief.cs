using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// Apply one explicitly selected same-kingdom fief after native ASO phase 8.
    /// Transfers are one-shot, consent-gated and verified by native owner state.
    /// There is no cross-kingdom seizure, siege transfer or silent fallback.
    /// </summary>
    internal static class SandboxStartingFief
    {
        internal static string TryApply(SandboxOriginProfile profile, Hero hero)
        {
            if (profile == null || profile.StartingFiefMode == "Native")
                return "native_unchanged";
            string issue = SandboxFiefPolicy.ValidateRequest(profile.Role,
                profile.StartingFiefMode, profile.StartingFiefId,
                profile.AllowStartingFiefTransfer);
            if (issue.Length != 0) return "refused:" + issue;
            if (hero == null || Clan.PlayerClan == null ||
                Clan.PlayerClan.Kingdom == null)
                return "refused:player_has_no_starting_kingdom";

            try
            {
                var kingdom = Clan.PlayerClan.Kingdom;
                Settlement target;
                if (profile.StartingFiefMode == "Specific")
                {
                    target = Settlement.Find(profile.StartingFiefId);
                    if (target == null)
                        return "refused:unknown_settlement_id";
                }
                else
                {
                    // Sort for reproducible selection. The actual game world,
                    // not an out-of-date hardcoded castle list, is authoritative.
                    bool castle = profile.StartingFiefMode == "Castle";
                    target = Settlement.All
                        .Where(s => s != null && s.OwnerClan != null &&
                            s.OwnerClan.Kingdom == kingdom &&
                            (castle ? s.IsCastle : s.IsTown) &&
                            !s.IsUnderSiege &&
                            (s.Party == null || s.Party.MapEvent == null))
                        .OrderBy(s => s.OwnerClan == Clan.PlayerClan ? 0 : 1)
                        .ThenBy(s => s.StringId, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (target == null)
                        return "refused:no_safe_kingdom_fief_of_requested_type";
                }

                bool own = target.OwnerClan == Clan.PlayerClan;
                issue = SandboxFiefPolicy.ValidateTarget(
                    target.IsFortification,
                    target.OwnerClan != null &&
                        target.OwnerClan.Kingdom == kingdom,
                    target.IsUnderSiege,
                    target.Party != null && target.Party.MapEvent != null,
                    own);
                if (issue.Length != 0) return "refused:" + issue;
                if (own) return "already_owned:" + target.StringId;

                // A real gift moves a same-kingdom fortification from its
                // current clan to the player. User must opt in to this
                // politically disruptive new-game action in the creator.
                string formerClan = target.OwnerClan == null ?
                    "none" : target.OwnerClan.StringId;
                ChangeOwnerOfSettlementAction.ApplyByGift(target, hero);
                if (target.OwnerClan != Clan.PlayerClan)
                    return "failed:owner_unchanged:" + target.StringId;

                DiagnosticJournal.Event("sandbox_start_fief_granted",
                    "id=" + target.StringId +
                    "|former_clan=" + formerClan +
                    "|new_clan=" + Clan.PlayerClan.StringId +
                    "|kingdom=" + kingdom.StringId +
                    "|action=ApplyByGift");
                return "granted:" + target.StringId;
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception("sandbox_start_fief_failed", error);
                return "failed:exception:" + error.GetType().Name;
            }
        }
    }
}
