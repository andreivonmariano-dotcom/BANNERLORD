using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Character-driven opportunities must be grounded in real adult
    /// Heroes, real presence and bounded social or financial effects.
    /// A request is authored by the NPC, not an invented past event.
    /// </summary>
    public static class SandboxAgencyDecisionPolicy
    {
        public const int MaxRequestsPerHero=3;
        public const int CooldownDays=70;
        public const int ExpiryDays=35;
        public static string Choose(string goal,bool adult,bool present,
            int relation,int priorRequests,int daysFromLast)
        {
            if(!adult || !present || relation< -35 ||
                priorRequests>=MaxRequestsPerHero ||
                daysFromLast<CooldownDays)return "";
            if(goal=="ProtectSharedHousehold" ||
               goal=="SupportHousehold" ||
               goal=="MaintainHousehold")
                return "HouseholdAid";
            if(goal=="EstablishLivelihood" ||
               goal=="BuildTradingContacts")
                return "LivelihoodAid";
            if(goal=="PreserveFamilyPosition" ||
               goal=="GainMilitaryTraining" ||
               goal=="MapSafePassages")
                return "Recognition";
            return "";
        }
        public static int Amount(string kind)
        {
            if(kind=="HouseholdAid")return 150;
            if(kind=="LivelihoodAid")return 200;
            return kind=="Recognition"?0:-1;
        }
        public static int RelationDelta(string kind,bool accepted)
        {
            if(Amount(kind)<0)return 0;
            return accepted?(kind=="Recognition"?1:2):
                (kind=="Recognition"?0:-1);
        }
        public static bool NativeTransferPermitted(string kind,
            bool accepted,int playerGold,bool sameNativePerson)
        {
            int amount=Amount(kind);
            return accepted && sameNativePerson &&
                amount>0 && playerGold>=amount;
        }
    }
}
