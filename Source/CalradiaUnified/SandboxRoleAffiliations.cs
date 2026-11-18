using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaUnified
{
    /// <summary>Native affiliation and starting reputation. No fake titles.</summary>
    public static class SandboxRoleAffiliations
    {
        public static string TryApply(SandboxOriginProfile profile,Hero player)
        {
            if (profile == null || player == null || Clan.PlayerClan == null)
                return "not_available";
            var clan=Clan.PlayerClan;
            if (!profile.RoleAffiliationsEnabled)
                return "native_affiliation_unchanged";
            if (profile.Role == "King")
                return clan.Kingdom != null &&
                    clan.Kingdom.RulingClan == clan
                    ? "native_king_confirmed" :
                    "king_requires_official_advanced_start";
            if (profile.Role == "Wanderer")
                return clan.Kingdom == null ? "independent_wanderer" :
                    "native_affiliation_retained";
            if (clan.Kingdom != null)
                return "native_affiliation_retained";
            if (!SandboxRolePolicy.CanRequestAffiliation(profile.Role,false,true))
                return "no_role_contract";
            var kingdom=Kingdom.All.FirstOrDefault(k => k != null &&
                k.Culture == player.Culture &&
                k.RulingClan != null);
            if (kingdom == null) return "matching_native_kingdom_missing";
            try
            {
                if (profile.Role == "Soldier")
                {
                    // This is a genuine MERCENARY CLAN contract, not an
                    // unimplemented rank-and-file enlistment.
                    ChangeKingdomAction.ApplyByJoinFactionAsMercenary(
                        clan,kingdom,50,false);
                }
                else
                {
                    // Knight and Lord use the game's genuine vassal action.
                    ChangeKingdomAction.ApplyByJoinToKingdom(
                        clan,kingdom,false);
                }
                return clan.Kingdom == kingdom
                    ? (profile.Role == "Soldier" ?
                        "native_mercenary_contract" : "native_vassal_contract")
                    : "native_action_did_not_change_kingdom";
            }
            catch (Exception e)
            {
                DiagnosticJournal.Exception("cu_role_affiliation",e);
                return "native_affiliation_failed";
            }
        }
    }
}
