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
