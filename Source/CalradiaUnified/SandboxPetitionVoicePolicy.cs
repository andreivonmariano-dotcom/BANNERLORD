namespace CalradiaUnified
{
    /// <summary>
    /// Dialogue tone is grounded in actual native Hero traits,
    /// current relation and the already observed character goal.
    /// No invented family ties, memories or accomplishments.
    /// </summary>
    public static class SandboxPetitionVoicePolicy
    {
        public static string Style(string goal,int nativeHonor,
            int nativeGenerosity,int nativeCalculating,int relation)
        {
            if(relation<0 ||
                (nativeCalculating>=2 && relation<20))
                return "Guarded";
            if((goal=="ProtectSharedHousehold" ||
                goal=="SupportHousehold" ||
                goal=="MaintainHousehold") && nativeHonor>=1)
                return "Duty";
            if((goal=="EstablishLivelihood" ||
                goal=="BuildTradingContacts") &&
                nativeCalculating>=1)
                return "Ambitious";
            if(nativeGenerosity>=1 && relation>=10)
                return "Warm";
            return "Direct";
        }
        // Shared dialogue voice: no new trait, random personality or relationship system.
        public static string Tone(int honor,int generosity,int calculating,
            int valor,int mercy,int relation)
        {
            if(relation< -10)return "Guarded";
            if(calculating>0 && calculating>=honor && calculating>=generosity &&
                calculating>=valor && calculating>=mercy)return "Prudent";
            if(valor>0 && valor>=honor && valor>=generosity && valor>=mercy)return "Bold";
            if(honor>0 && honor>=generosity && honor>=mercy)return "Dutiful";
            if(mercy>0 || generosity>0 || relation>=50)return "Warm";
            return "Plain";
        }
        public static string Greeting(string tone)
        {
            switch(tone)
            {
                case "Guarded":return "Speak your mind. I will hear you out.";
                case "Prudent":return "What matter requires our attention?";
                case "Bold":return "Tell me what is to be done.";
                case "Dutiful":return "You have my attention.";
                case "Warm":return "Come, tell me what is troubling you.";
                default:return "What would you speak of?";
            }
        }
        public static string Opinion(string tone,string topic)
        {
            switch(topic)
            {
                case "Opinions":
                    switch(tone)
                    {
                        case "Guarded":return "I have learned to judge a person by deeds, not words.";
                        case "Prudent":return "A sound judgment is worth more than a promise made in haste.";
                        case "Bold":return "I would rather face hardship than live in fear of it.";
                        case "Dutiful":return "A person's word should be worth more than silver.";
                        case "Warm":return "A life is worth little without those we care for.";
                        default:return "A decent living and the freedom to choose one's own way.";
                    }
                case "Family":
                    switch(tone)
                    {
                        case "Guarded":return "We need not agree on every matter to look after our kin.";
                        case "Prudent":return "We must think of what they will need in years to come.";
                        case "Bold":return "Let them learn courage and stand on their own feet.";
                        case "Dutiful":return "They must learn to keep their word.";
                        case "Warm":return "They deserve our time, not merely our silver.";
                        default:return "Let us hear what each of them wants.";
                    }
                case "Estate":
                    switch(tone)
                    {
                        case "Guarded":return "I would see the matter for myself before giving counsel.";
                        case "Prudent":return "Count the cost before committing more men.";
                        case "Bold":return "A strong defense is worth more than promises.";
                        case "Dutiful":return "Those who depend on us must be protected.";
                        case "Warm":return "Do not forget the people who live beneath our banner.";
                        default:return "We should see to the weakest holding first.";
                    }
                case "Finances":
                    switch(tone)
                    {
                        case "Guarded":return "I prefer to keep my purse and yours separate.";
                        case "Prudent":return "Set aside enough for the next levy of expenses.";
                        case "Bold":return "Silver is of little use if we cannot feed our company.";
                        case "Dutiful":return "Pay what is owed before seeking more.";
                        case "Warm":return "Let us provide for those who depend on us.";
                        default:return "Spend no more than we can afford.";
                    }
                case "Service":
                    switch(tone)
                    {
                        case "Guarded":return "I would not promise what I cannot deliver.";
                        case "Prudent":return "Make certain we have the means before we act.";
                        case "Bold":return "Deeds will tell more than talk.";
                        case "Dutiful":return "A promise given must be kept.";
                        case "Warm":return "Let us bring our people safely home.";
                        default:return "One matter at a time.";
                    }
                default:return "";
            }
        }
        public static bool AcceptsHouseholdDuty(int relation,int honor,int calculating)
        {
            return relation>=0 && (relation>=20 || honor>0 || calculating<=0);
        }
        public static string DecisionReply(string tone,bool accepted,bool confirmed)
        {
            if(!confirmed)return "Let us settle the matter properly before we speak of it as done.";
            if(accepted)
            {
                if(tone=="Guarded")return "You have kept your word on this matter. I shall not forget it.";
                if(tone=="Prudent")return "That gives me something to work with.";
                if(tone=="Dutiful")return "I will remember the obligation.";
                if(tone=="Bold")return "Then I shall make the most of it.";
                if(tone=="Warm")return "You have been kind to me.";
                return "You have my thanks.";
            }
            if(tone=="Guarded")return "I have your answer. I will trouble you no further today.";
            if(tone=="Prudent")return "Then I must find another way.";
            if(tone=="Bold")return "Very well. I shall see what I can do myself.";
            return "I understand. I shall make other plans.";
        }
    }
}
