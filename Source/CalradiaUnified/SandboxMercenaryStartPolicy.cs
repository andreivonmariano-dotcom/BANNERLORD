using System;

namespace CalradiaUnified
{
    /// <summary>
    /// A mercenary is a hired, independent company rather than a troop
    /// enlisted inside another lord's party or a landed vassal.
    /// Bannerlord's native mercenary-clan status, not a role label,
    /// establishes this contract. No invented salary or fixed term.
    /// </summary>
    public static class SandboxMercenaryStartPolicy
    {
        public static string Validate(
            string role,string requestedKingdom,string actualKingdom,
            bool nativeMercenaryContract,bool playerHeadsClan,
            bool playerHeadsOwnParty,bool isRulingClan,int fiefs)
        {
            if(role!="Mercenary")return "not_guided_mercenary";
            if(String.IsNullOrEmpty(requestedKingdom))
                return "contract_kingdom_missing";
            if(String.IsNullOrEmpty(actualKingdom))
                return "native_kingdom_missing";
            if(!String.Equals(requestedKingdom,actualKingdom,
                StringComparison.Ordinal))
                return "native_contract_kingdom_mismatch";
            if(!nativeMercenaryContract)
                return "native_mercenary_contract_missing";
            if(isRulingClan || fiefs!=0)
                return "mercenary_land_or_rulership_invalid";
            if(!playerHeadsClan)
                return "player_does_not_lead_company_clan";
            if(!playerHeadsOwnParty)
                return "player_not_leading_independent_party";
            return "";
        }
    }
}
