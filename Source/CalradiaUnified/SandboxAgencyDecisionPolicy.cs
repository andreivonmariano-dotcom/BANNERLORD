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
        /// <summary>
        /// A mod-authored initiative based on actual native Hero traits and
        /// current wealth. No invented speech, memories, or native AI orders.
        /// The pending request becomes real only when offered in a game menu.
        /// </summary>
        public static string ChooseInitiative(string goal,bool adult,
            bool present,int relation,int priorRequests,int daysFromLast,
            int nativeHonor,int nativeGenerosity,int nativeCalculating,
            int nativeNpcGold)
        {
            string baseChoice=Choose(goal,adult,present,relation,
                priorRequests,daysFromLast);
            if(baseChoice.Length==0)return "";
            if(nativeNpcGold<0)return "";
            // A well-funded person does not need a generic money plea.
            // A status-conscious, respected kinsman can instead request
            // acknowledgement. Other personalities defer the appeal.
            if((baseChoice=="HouseholdAid" &&
                nativeNpcGold>=1500 && nativeHonor>0) ||
               (baseChoice=="LivelihoodAid" && nativeNpcGold>=3000))
                baseChoice="Recognition";
            if(baseChoice=="Recognition" &&
                nativeCalculating>=2 && relation<10)
                return "";
            if(baseChoice=="LivelihoodAid" &&
                nativeGenerosity>=2 && nativeNpcGold>=600)
                return "";
            return baseChoice;
        }

        // A conservative modeled response to verified petition history.
        // It never changes the native relation, and reconciliation can
        // reopen requests. Existing quotas and cooldowns still apply.
        public static bool CanReapproach(SandboxSocialBondState.Bond history,
            int nativeRelation)
        {
            if(history==null)return true;
            return !(history.Declined>=2 &&
                history.Declined>history.Supported &&
                history.Negative<0 && nativeRelation<=-10);
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
