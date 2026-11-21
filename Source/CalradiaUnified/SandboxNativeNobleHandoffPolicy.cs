namespace CalradiaUnified
{
    /// <summary>
    /// The actual native King/vassal contract, clan and party leadership,
    /// real kingdom and holding are authoritative at character-creation
    /// handoff. Hero.IsLord and IsMinorFaction can lag a new native start.
    /// The role-specific gates also validate exact kingdom and eliminated
    /// status. This gate never changes clan ownership or native rank.
    /// </summary>
    public static class SandboxNativeNobleHandoffPolicy
    {
        public static bool HasVerifiedNativeNobleAuthority(
            string nativeType,bool inNativeKingdom,
            bool playerLeadsClan,bool playerLeadsParty,
            bool rulingClan,bool mercenary,bool bandit,
            bool rebel,int nativeFiefs)
        {
            if(nativeType!="king" && nativeType!="vassal")
                return false;
            if(!inNativeKingdom || !playerLeadsClan ||
               !playerLeadsParty || mercenary || bandit ||
               rebel || nativeFiefs<1)return false;
            return nativeType=="king"?rulingClan:!rulingClan;
        }
    }
