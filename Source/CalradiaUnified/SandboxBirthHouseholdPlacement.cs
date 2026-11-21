using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// House members who remain in their parent's clan get an ACTUAL
    /// native residence or party assignment, never a location label alone.
    /// A refusal is preferable to an invisible unspawned "heir".
    /// </summary>
    internal static class SandboxBirthHouseholdPlacement
    {
        internal static bool CanPlace(Hero father)
        {
            return father!=null && father.Clan!=null &&
                (father.PartyBelongedTo!=null ||
                 father.Clan.Settlements.Any(s=>s!=null &&
                    s.OwnerClan==father.Clan && s.IsFortification));
        }
        internal static string Place(Hero child,Hero father)
        {
            if(child==null || father==null || father.Clan==null ||
               child.Clan!=father.Clan)
                return "refused:birth_clan_mismatch";
            if(child.PartyBelongedTo!=null)
                return "native_party:"+child.PartyBelongedTo.StringId;
            if(child.StayingInSettlement!=null)
                return "native_settlement:"+
                    child.StayingInSettlement.StringId;
            try
            {
                var home=father.Clan.Settlements.Where(s=>s!=null &&
                    s.OwnerClan==father.Clan && s.IsFortification)
                    .OrderBy(s=>s.StringId,StringComparer.Ordinal)
                    .FirstOrDefault();
                if(home!=null)
                {
                    EnterSettlementAction.ApplyForCharacterOnly(
                        child,home);
                    if(child.StayingInSettlement==home)
                        return "native_settlement:"+home.StringId;
                }
                if(father.PartyBelongedTo!=null)
                {
                    AddHeroToPartyAction.Apply(child,
                        father.PartyBelongedTo,false);
                    if(child.PartyBelongedTo==father.PartyBelongedTo)
                        return "native_father_party:"+
                            father.PartyBelongedTo.StringId;
                }
                return "refused:no_verified_native_residence";
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_birth_household_placement_failed",e);
                return "failed:"+e.GetType().Name;
            }
        }
    }
}
