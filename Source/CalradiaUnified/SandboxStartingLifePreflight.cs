using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only gate after native phase 8 and before any custom grants.
    /// A refusal leaves the native campaign intact and reports a failed
    /// selected start. It cannot roll back native phase 8 mutations.
    /// </summary>
    internal static class SandboxStartingLifePreflight
    {
        internal static string Check(SandboxOriginProfile p,Hero player)
        {
            if(p==null || player==null)return "missing_start_context";
            // Native Advanced Start has already run at phase 8. Check its
            // actual political and fief result BEFORE changing any CU
            // kinship, relation, property, officer or starter resource.
            var campaign=Campaign.Current;
            var clan=Clan.PlayerClan;
            if(campaign==null || campaign.AdvancedStartData==null ||
                clan==null)return "native_start:missing_native_context";
            var kingdom=clan.Kingdom;
            var nativeErrors=SandboxStartContract.Validate(
                SandboxNativeStartOptions.StartTypeForRole(p.Role),
                campaign.AdvancedStartData.GetStartType(),
                p.StartingKingdomId,
                kingdom==null?"":kingdom.StringId,
                kingdom!=null && kingdom.RulingClan==clan,
                clan.Settlements==null?0:clan.Settlements.Count,
                false,false,0,0);
            if(nativeErrors.Length>0)
                return "native_start:"+nativeErrors[0];
            if(SandboxCustomLifeRolePolicy.IsLandless(p.Role) &&
                (Clan.PlayerClan==null ||
                 !SandboxCustomLifeRolePolicy.NativeLandlessContract(
                    Clan.PlayerClan.Kingdom!=null,
                    Clan.PlayerClan.Settlements.Count)))
                return "landless:unexpected_fief_or_kingdom_contract";
            var ages=SandboxHouseholdLineagePolicy.ValidateHousePlan(
                p,(int)player.Age);
            if(ages.Length>0)return "invalid_house_birth_order:"+ages;
            if(SandboxNobleStartPolicy.IsSonOrigin(p.NobleBirthPosition))
            {
                var reason=SandboxDynasticSonStart.Preflight(p,player);
                if(reason.Length>0)return "noble_son:"+reason;
            }
            else if(SandboxNobleStartPolicy.RequiresNobleElder(
                p.NobleBirthPosition) &&
                !SandboxDynasticStart.HasNativeElder(p,player))
                return "noble_sibling:no_eligible_native_elder";
            if(SandboxServicePolicy.NeedsPatron(p.Role) &&
                !SandboxServicePatron.HasNativePatron(p,player))
                return "service:no_eligible_native_patron";
            if(p.SpouseEnabled && !player.CanMarry())
                return "marriage:native_player_not_eligible";
            return "";
        }
    }
}
