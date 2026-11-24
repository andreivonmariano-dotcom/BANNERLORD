using System;

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
        // A group discussion is advice, not an executable native order.
        public static bool GroupTopic(string topic)
        {
            return topic=="Family" || topic=="Defense" || topic=="Livelihood" || topic=="Relations";
        }
        public static bool GroupPriority(string priority)
        {
            return priority=="Security" || priority=="Welfare" || priority=="Reserve";
        }
        public static string GroupPreference(string office,string goal,string tone,int relation)
        {
            if(relation< -10)return "Reserve";
            if(office=="Marshal" || office=="MasterOfArms" || office=="Kingsguard" ||
                office=="Captain" || office=="Master of Arms" || office=="Knight" ||
                goal=="GainMilitaryTraining" || goal=="KeepCompanyTogether")return "Security";
            if(office=="RoyalMinister" || office=="Royal Minister" || office=="Steward" ||
                office=="HouseholdSteward" || office=="Factor" || office=="Chamberlain" ||
                goal=="BuildTradingContacts" || goal=="EstablishLivelihood")return "Reserve";
            if(goal=="SupportHousehold" || goal=="ProtectSharedHousehold" ||
                goal=="ProtectDescendants" || goal=="MaintainHousehold" || tone=="Warm")
                return "Welfare";
            return tone=="Bold"?"Security":tone=="Prudent" || tone=="Guarded"?"Reserve":"Welfare";
        }
        public static string GroupArgument(string topic,string priority)
        {
            if(!GroupTopic(topic) || !GroupPriority(priority))return "";
            switch(topic)
            {
                case "Family":
                    return priority=="Security"?"Teach our kin how to stand on their own feet. A house must be able to defend itself.":
                        priority=="Welfare"?"Our kin need our time and care. Let us hear what they want for their own futures.":
                        "We should not make promises to every member of the house before we know what we can provide.";
                case "Defense":
                    return priority=="Security"?"Put the defense of our people first. Count the men we can spare before any other undertaking.":
                        priority=="Welfare"?"Defenses matter, but not at the cost of leaving families and dependants without support.":
                        "Inspect the rolls and the cost before committing any men. A larger force is not always a stronger one.";
                case "Livelihood":
                    return priority=="Security"?"Our livelihood will not endure without a company able to protect it.":
                        priority=="Welfare"?"A prosperous house should improve the lives of those who depend on it.":
                        "Keep a reserve for lean days. We should know the cost before making another promise.";
                default:
                    return priority=="Security"?"Keep faith with reliable allies and be prepared to defend our interests.":
                        priority=="Welfare"?"Hear the other house before judging it. Good relations are worth preserving.":
                        "I would promise little until we know what the other house is willing to undertake.";
            }
        }
        public static string GroupResponse(string preference,string previous,int peerRelation,bool hasPrevious)
        {
            if(!hasPrevious || !GroupPriority(preference) || !GroupPriority(previous))return "";
            if(preference==previous)return peerRelation< -10?
                "On this point, at least, I agree. ":"I agree with that counsel. ";
            return peerRelation< -10?"I cannot support that course. ":
                peerRelation>=20?"I respect that view, but see another difficulty. ":
                "I would weigh another concern. ";
        }
        public static string GroupDecisionReply(string preference,string chosen,string tone)
        {
            if(!GroupPriority(preference) || !GroupPriority(chosen))return "";
            if(preference==chosen)return tone=="Guarded"?"I shall judge that intention by your deeds.":
                "That is the course I hoped you would favor.";
            return tone=="Bold"?"I still hold to my counsel. I ask that you do not dismiss the risk.":
                tone=="Prudent"?"I hear your decision. Keep the other costs in mind before acting.":
                "I understand your choice, though my concern remains.";
        }
        // Stable variation per participant and topic; no random state in UI
        // conditions. Adjacent days rotate through distinct authored lines.
        public static int DialogueVariant(string speaker,string topic,int day,int count)
        {
            if(count<=0)return 0;
            uint hash=2166136261u;
            unchecked { foreach(char c in (speaker??"")+"|"+(topic??""))
                hash=(hash^c)*16777619u; }
            return (int)(((ulong)hash+(ulong)Math.Max(0,day))%(ulong)count);
        }
        public static string GroupTopicPreference(string topic,string office,string goal,
            string tone,int relation,bool urgentDefense,bool youngFamily)
        {
            if(!GroupTopic(topic))return "";
            if(topic=="Defense" && urgentDefense && tone!="Guarded" &&
                (tone=="Bold" || tone=="Dutiful" || office=="Marshal" || office=="Captain"))
                return "Security";
            if(topic=="Family" && youngFamily &&
                (goal=="SupportHousehold" || goal=="ProtectDescendants" || tone=="Warm"))
                return "Welfare";
            if(topic=="Livelihood" && (tone=="Prudent" || goal=="BuildTradingContacts"))
                return "Reserve";
            if(topic=="Relations" && tone=="Dutiful" && relation>=0)return "Security";
            return GroupPreference(office,goal,tone,relation);
        }
        public static string GroupNuance(string topic,string priority,string tone,int variant)
        {
            if(!GroupTopic(topic) || !GroupPriority(priority))return "";
            if(variant%3==0)return GroupArgument(topic,priority);
            string[] lines;
            switch(topic)
            {
                case "Family":
                    lines=priority=="Security"?new[]{"Our children should learn to judge danger, not seek it.","Let the grown members of this house carry their share of its duties."}:
                        priority=="Welfare"?new[]{"Ask what each member of our family needs before deciding for them.","I would rather hear our kin speak freely than see them obey in silence."}:
                        new[]{"Settle what we can provide before promising more to our kin.","Let us distinguish the needs of the young from the wishes of the grown."};break;
                case "Defense":
                    lines=priority=="Security"?new[]{"Make certain our company can answer danger without abandoning those at home.","A threat to one holding can leave another exposed. Consider both."}:
                        priority=="Welfare"?new[]{"The people beneath our banner must not bear every cost of preparedness.","Our soldiers have families too. Keep their needs in mind."}:
                        new[]{"Before sending men anywhere, count the garrison and what must remain.","I would know which defenses need work before paying for more."};break;
                case "Livelihood":
                    lines=priority=="Security"?new[]{"A purse is little comfort if the roads we travel are unsafe.","Keep enough strength to protect our means of living."}:
                        priority=="Welfare"?new[]{"The house prospers through people, not coin alone.","See that those doing the work have reason to continue it."}:
                        new[]{"Distinguish what must be paid now from what can wait.","Do not spend tomorrow's silver on today's display."};break;
                default:
                    lines=priority=="Security"?new[]{"An ally must know what we can actually undertake.","We should keep our word and be ready to defend it."}:
                        priority=="Welfare"?new[]{"Hear the grievance of another house before it becomes a feud.","There is room to settle differences without humiliating either house."}:
                        new[]{"Find out what another house seeks before giving our word.","A useful bargain begins with knowing what each side can afford."};break;
            }
            return lines[(variant%3+3)%3==1?0:1];
        }
        public static string GroupChallenge(string topic,string priority,string tone,bool practical)
        {
            if(!GroupTopic(topic) || !GroupPriority(priority))return "";
            if(practical)
            {
                switch(topic)
                {
                    case "Family":return priority=="Welfare"?"Begin by hearing the children and kin who are old enough to speak for themselves.":priority=="Security"?"Agree which duties belong to which grown members, rather than demanding the same of everyone.":"Count the obligations we already have before offering anything new.";
                    case "Defense":return priority=="Security"?"Review the actual garrison and party strength, then decide what can be spared.":priority=="Welfare"?"Ask what the proposed defense leaves for the people who rely on us.":"Inspect the defenses and compare the cost of each proposal.";
                    case "Livelihood":return priority=="Reserve"?"Review the existing obligations and cash before authorizing expenditure.":priority=="Security"?"Review the safety of the routes on which our livelihood depends.":"Ask what our people need and distinguish that from needless display.";
                    default:return priority=="Welfare"?"Hear the other party's actual grievance in person.":priority=="Security"?"Check the commitments already made before offering new ones.":"Ask what each party offers, and keep the question open until we know.";
                }
            }
            return tone=="Guarded"?"I will reconsider if the facts change, not merely to please the room.":
                tone=="Prudent"?"The objection is fair. We should compare the costs of both courses.":
                tone=="Bold"?"I will hear the objection, but I shall not pretend the danger has vanished.":
                tone=="Dutiful"?"Let us weigh our obligations to those who will bear the cost.":
                "Let us hear the other view before we settle on a course.";
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
        // Authored cultural registers, not claims about individual native customs.
        // The request content and cost stay identical across all voices.
        public static string Letter(string kind,string tone,string culture,
            string contact,bool recipientRuler,bool recipientNoble,int relation)
        {
            string concern;
            switch(kind)
            {
                case "HouseholdAid":concern="I ask for 150 denars to help my household.";break;
                case "LivelihoodAid":concern="I ask for 200 denars to make a start in my livelihood.";break;
                case "Recognition":concern="I ask that my service be acknowledged.";break;
                case "Audience":concern="I ask for a private audience concerning the standing of our houses.";break;
                default:return "";
            }
            string greeting=contact=="Spouse" && relation>=20?"My dear companion,":
                contact=="Kin" && relation>=10?"To my kin,":
                recipientRuler?"To the sovereign,":
                contact=="Patron"?"To my sworn companion,":
                recipientNoble?"To the head of your house,":"Greetings,";
            string voice;
            switch(tone)
            {
                case "Guarded":voice="I shall state my purpose plainly. I ask for no promises in writing.";break;
                case "Prudent":voice="Let us weigh the matter carefully when we meet.";break;
                case "Bold":voice="I would rather speak plainly than leave this matter unsettled.";break;
                case "Dutiful":voice="I mean to bear my own share of our obligations.";break;
                case "Warm":voice="I value the chance to speak with you in trust.";break;
                default:voice="I would welcome a word with you in person.";break;
            }
            string closing;
            switch((culture??"").ToLowerInvariant())
            {
                case "empire":closing="With due respect.";break;
                case "vlandia":closing="In good faith.";break;
                case "sturgia":closing="May your road be safe.";break;
                case "battania":closing="May your hearth be well kept.";break;
                case "aserai":closing="Peace upon your household.";break;
                case "khuzait":closing="May your travels be fortunate.";break;
                default:closing="Until we meet.";break;
            }
            return greeting+"\n\n"+concern+" "+voice+"\n\n"+closing;
        }
        // Ordinary correspondence conveys a modeled thought, not a
        // new event, quest, request, reward or claim of actual native dispatch.
        public static string OrdinaryLetter(string goal,string tone,string culture,
            string contact,int relation)
        {
            string greeting=contact=="Spouse" && relation>=20?"My dear companion,":
                contact=="Kin"?"To my kin,":contact=="Patron"?"To my sworn companion,":"Greetings,";
            string thought;
            switch(goal)
            {
                case "ProtectSharedHousehold":case "SupportHousehold":case "MaintainHousehold":
                    thought="I keep the household's welfare in mind.";break;
                case "GainMilitaryTraining":thought="I mean to learn more of the soldier's craft.";break;
                case "BuildTradingContacts":case "EstablishLivelihood":
                    thought="I still hope to make a living through my own efforts.";break;
                case "DefendClanStanding":case "PreserveFamilyPosition":
                    thought="I hold the name of my house dear.";break;
                case "ProtectDescendants":thought="I think often of the years ahead for my kin.";break;
                case "MapSafePassages":thought="I wish to learn more of the roads between our towns.";break;
                case "KeepCompanyTogether":thought="I value the company that shares our hardships.";break;
                default:thought="I have been thinking of the years ahead.";break;
            }
            string voice=tone=="Warm"?"I think of you kindly.":
                tone=="Dutiful"?"May we keep faith.":
                tone=="Prudent"?"I shall take care in my affairs.":
                tone=="Bold"?"I shall face what comes.":
                tone=="Guarded"?"I ask nothing of you.":"I send my regards.";
            string closing=(culture??"").ToLowerInvariant()=="aserai"?"Peace to you.":
                (culture??"").ToLowerInvariant()=="sturgia"?"Safe roads.":
                (culture??"").ToLowerInvariant()=="battania"?"A warm hearth.":
                (culture??"").ToLowerInvariant()=="khuzait"?"Fair travels.":
                (culture??"").ToLowerInvariant()=="empire"?"With respect.":"In good faith.";
            string text=greeting+" "+thought+" "+voice+" "+closing;
            return text.Length<=180?text:greeting+" "+thought+" "+closing;
        }
        public static string StandingReply(int nativeRelation)
        {
            // Describes today's native relation. It does not label a
            // person a native friend/enemy or manufacture a grievance.
            int band=SandboxCharacterAgendaState.StandingBand(nativeRelation);
            return band==-2?"There is little trust between us.":
                band==-1?"Our dealings have not been easy.":
                band==2?"I regard you with considerable goodwill.":
                band==1?"We stand on friendly terms.":
                "We have no particular cause for ill will or great trust.";
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
