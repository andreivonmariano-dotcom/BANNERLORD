using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxCareerChargeState careerCharge=new SandboxCareerChargeState();
        private string chargeReply="";

        private string PersonalVoice(Hero actor)
        {
            var player=Hero.MainHero;
            if(actor==null || player==null)return "Plain";
            return SandboxPetitionVoicePolicy.Tone(
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Generosity),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Mercy),
                player.GetRelation(actor));
        }
        private bool CanSpeakSocially()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            return applied && IsStandaloneSandbox() && actor!=null && player!=null &&
                actor!=player && actor.IsAlive && actor.IsActive && !actor.IsPrisoner &&
                actor.Age>=18f;
        }
        private SandboxDialogueVarietyState dialogueVariety=new SandboxDialogueVarietyState();
        private string personalOpeningActor="",personalOpeningPlayer="",personalInitiative="";
        private int personalOpeningVariant,personalTopicVariant;
        private void EnsureDialogueSeed()
        {
            if(dialogueVariety.Seed.Length==0)
                dialogueVariety.Initialize(Guid.NewGuid().ToString("N"));
        }
        private int NextDialogueVariant(Hero speaker,string topic,int count)
        {
            var player=Hero.MainHero;
            if(player==null || speaker==null || speaker==player)return 0;
            EnsureDialogueSeed();
            int value=dialogueVariety.Variant(player.StringId,speaker.StringId,topic,count);
            dialogueVariety.Advance(player.StringId,speaker.StringId,topic,CurrentHouseholdDay());
            return value;
        }
        private void BeginPersonalConversation()
        {
            if(!CanSpeakSocially())return;
            var actor=Hero.OneToOneConversationHero;
            personalOpeningActor=actor.StringId;personalOpeningPlayer=Hero.MainHero.StringId;
            personalTopicActorId="";personalTopicName="";personalReply="";personalTopicVariant=0;
            personalOpeningVariant=NextDialogueVariant(actor,"Greeting",3);
            personalInitiative=PersonalInitiativeTopic();
            if(personalInitiative.Length>0)NextDialogueVariant(actor,"Initiative",3);
        }
        private bool OpeningMatches()
        {
            return CanSpeakSocially() && personalOpeningActor==Hero.OneToOneConversationHero.StringId &&
                personalOpeningPlayer==Hero.MainHero.StringId;
        }
        private string SelectedInitiative()
        {
            if(!OpeningMatches() || SpeakingWithPendingPetitioner() ||
                personalInitiative.Length==0 || !CanDiscussPersonalTopic(personalInitiative) ||
                livingPersons.HasRecentConversationChoice(personalOpeningPlayer,personalOpeningActor,
                    personalInitiative,CurrentHouseholdDay()))return "";
            return personalInitiative;
        }
        private bool PreparePersonalGreeting()
        {
            MBTextManager.SetTextVariable("CU_PERSONAL_GREETING",
                new TextObject(PersonalGreetingText()+PersonalInitiativeGreeting()));
            return true;
        }
        // Transient conversation context only. Never saved or used for rewards.
        private string personalTopicActorId="";
        private string personalTopicName="";
        private string personalReply="";

        private string PersonalGreetingText()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(!CanSpeakSocially())return "Let us speak another time.";
            int relation=player.GetRelation(actor);
            string tone=PersonalVoice(actor);
            string role=actor==player.Spouse && actor.Spouse==player?"Spouse":
                SpeakingWithSwornPatron()?"Patron":SpeakingWithAdultFamily()?"Kin":
                SpeakingWithHouseholdCompanion()?"Companion":"Other";
            string alternate=SandboxPetitionVoicePolicy.PersonalOpening(role,tone,relation,
                OpeningMatches()?personalOpeningVariant:0);
            if(alternate.Length>0)return alternate;
            if(actor==player.Spouse && actor.Spouse==player)
            {
                if(relation<0)return "We have not been of one mind lately. What is it?";
                if(relation>=20)return tone=="Prudent"?
                    "Come, sit with me. Is this a matter for our house, or for the two of us?":
                    tone=="Bold"?"It is good to have a moment to ourselves. Speak freely.":
                    "I am glad we have a moment together. What is on your mind?";
                return "Of course. What would you like us to talk about?";
            }
            if(SpeakingWithSwornPatron())return "Come, speak freely. Is this a matter of your service?";
            if(SpeakingWithAdultFamily())return relation>=10?
                "We are kin. You need not stand on ceremony with me.":
                "What matter brings you to me?";
            if(SpeakingWithHouseholdCompanion())return relation>=10?
                "Of course. Is there something you need my counsel on?":
                "I am listening. What do you need?";
            if(relation<=-10)return "We have not always seen eye to eye. Speak plainly.";
            if(relation>=50)return "I am glad to speak with you away from the business of the day.";
            return SandboxPetitionVoicePolicy.Greeting(tone);
        }

        // NPC chooses a real subject within a conversation the player opens.
        // No forced dialogue, map interruption, task creation or native AI order.
        private int ObservedStandingShift(Hero player,Hero actor)
        {
            if(player==null || actor==null || player==actor)return 0;
            var e=livingPersons.Events.LastOrDefault(x=>x.Kind=="NativeRelationChangedObserved" &&
                x.Actor==player.StringId && x.Target==actor.StringId);
            if(e==null)return 0;
            var fields=e.Detail.Split('|');
            int before,after;
            if(fields.Length<2 || !fields[0].StartsWith("before=",StringComparison.Ordinal) ||
                !fields[1].StartsWith("after=",StringComparison.Ordinal) ||
                !Int32.TryParse(fields[0].Substring(7),out before) ||
                !Int32.TryParse(fields[1].Substring(6),out after) ||
                before< -100 || before>100 || after< -100 || after>100 ||
                after!=player.GetRelation(actor))return 0;
            return after-before>=10?1:before-after>=10?-1:0;
        }
        private string PersonalInitiativeTopic()
        {
            if(!CanSpeakSocially() || !CanDiscussPersonalMatters() ||
                SpeakingWithPendingPetitioner())return "";
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            var clan=Clan.PlayerClan;
            var candidates=new System.Collections.Generic.List<string>();
            if(clan!=null && CanDiscussEstateTopic() &&
                clan.Settlements.Any(x=>x!=null && x.OwnerClan==clan && x.IsUnderSiege))
                candidates.Add("Concerns");
            if(player.GetRelation(actor)<0 || ObservedStandingShift(player,actor)!=0)candidates.Add("Standing");
            if(player.GetRelation(actor)>=50 && livingPersons.SharedConversationMemory(player.StringId,actor.StringId)!=null)
                candidates.Add("Standing");
            if(CanCounselCareer() && IsPersonalChargeActive())candidates.Add("Service");
            if(actor==player.Spouse && actor.Spouse==player && player.Children!=null &&
                player.Children.Any(h=>h!=null && h.IsAlive && h.Age<18f &&
                    (h.Father==actor || h.Mother==actor)))candidates.Add("Family");
            if(characterAgendas.Get(actor.StringId)!=null)candidates.Add("Ambitions");
            if(PersonalMemoryText().Length>0)candidates.Add("Memory");
            var eligible=candidates.Distinct().Where(topic=>CanDiscussPersonalTopic(topic) &&
                !livingPersons.HasRecentConversationChoice(player.StringId,actor.StringId,topic,day)).ToArray();
            if(eligible.Length==0)return "";
            if(eligible.Contains("Concerns"))return "Concerns"; // Actual siege is urgent.
            if(CanDiscussStory(StoryTopic()))return StoryTopic();
            return eligible[dialogueVariety.Variant(player.StringId,actor.StringId,"Initiative",eligible.Length)];
        }

        private string PersonalInitiativeGreeting()
        {
            if(!CanSpeakSocially())return "";
            if(SpeakingWithPendingPetitioner())return " There is something I have been meaning to ask you.";
            string topic=SelectedInitiative();
            switch(topic)
            {
                case "Concerns":return " Before we part, I would speak of the danger to our holdings.";
                case "Standing":
                    int shift=ObservedStandingShift(Hero.MainHero,Hero.OneToOneConversationHero);
                    return shift>0?" There is something about our improved standing I would like to discuss.":
                        shift<0?" I would rather speak of the strain between us.":" I would rather not leave matters between us unspoken.";
                case "Service":return " There is the duty we agreed upon, too.";
                case "Family":return " I have been thinking about the children.";
                case "Ambitions":return " There is something I wish to tell you about my own plans.";
                case "Memory":return " There is something from our earlier dealings I would like to revisit.";
                default:return "";
            }
        }

        private string PersonalMemoryText()
        {
            if(!CanSpeakSocially())return "";
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            var story=petitionStories.For(actor.StringId);
            // Old petition stories are keyed by petitioner alone. A matching
            // player/speaker receipt prevents a successor claiming the old choice.
            bool sharedPetition=story!=null && livingPersons.Events.Any(e=>
                e.Actor==actor.StringId && e.Target==player.StringId &&
                ((story.Kind=="Audience" && e.Kind=="PersonalAudienceAnswered") ||
                 ((e.Kind=="CharacterRequestAccepted" || e.Kind=="CharacterRequestDeclined" ||
                   e.Kind=="CharacterRequestRelationUnverified") &&
                  e.Detail.Split('|').Contains("kind="+story.Kind) &&
                  e.Detail.Split('|').Contains("choice="+(story.Outcome=="Supported"?"accept":"decline")))));
            if(sharedPetition && story!=null && petitionStories.CanRecall(actor.StringId,CurrentHouseholdDay()))
            {
                if(story.Outcome=="Declined")return "I remember the request you declined. I will not pretend you made a promise.";
                if(story.NativePaymentVerified)return "I remember the silver you gave me when I asked for help. That matter is settled.";
                if(story.Kind=="Audience")return "We spoke of our houses before. I valued the chance to hear your views.";
                if(story.Kind=="Recognition")return "You acknowledged my work when I asked. I have not forgotten.";
            }
            var intention=livingPersons.LatestRelationshipIntention(player.StringId,actor.StringId);
            if(intention!=null)
            {
                if(intention.Detail.EndsWith("|intention=Repair",StringComparison.Ordinal))
                    return "I remember that you wished to mend matters between us. I judge our standing by how we deal with one another now.";
                if(intention.Detail.EndsWith("|intention=Care",StringComparison.Ordinal))
                    return "I remember that you spoke of valuing our relationship. I have not taken those words as a bargain.";
            }
            var group=livingPersons.LatestGroupDiscussion(player.StringId,actor.StringId);
            if(group!=null)
            {
                var fields=group.Detail.Split('|');
                if((fields.Length==3 || fields.Length==4) && fields[1].StartsWith("topic=",StringComparison.Ordinal) &&
                    fields[2].StartsWith("priority=",StringComparison.Ordinal))
                {
                    string subject=fields[1].Substring(6),priority=fields[2].Substring(9);
                    if(SandboxPetitionVoicePolicy.GroupTopic(subject) &&
                        SandboxPetitionVoicePolicy.GroupPriority(priority))
                    {
                        if(fields.Length==4 && fields[3].StartsWith("occasion=",StringComparison.Ordinal))
                        {
                            string occasion=fields[3].Substring(9);
                            if(occasion=="Remembrance")return "I remember our time honoring the departed. There was room for each of us to grieve in our own way.";
                            if(occasion=="MarriageCelebration")return "I remember the gathering to celebrate your marriage. Those words were about the life you share.";
                            if(occasion=="Fellowship")return "I remember the time we set business aside and simply spoke as companions.";
                            if(occasion=="FamilyGathering")return "I remember our gathering to hear the concerns of the family.";
                            if(occasion=="CourtAssembly")return "I remember the assembly where we weighed the interests of the house. We did not settle every matter there.";
                        }
                        string topicWords=subject=="Family"?"the future of our household":
                            subject=="Defense"?"our defenses":subject=="Livelihood"?"our livelihood":"our dealings with other houses";
                        return "At our gathering we spoke of "+topicWords+". You favored "+
                            (priority=="Security"?"security":priority=="Welfare"?"the welfare of those who depend on us":"keeping our resources in reserve")+
                            ". I remember your counsel, not a promise that the work was done.";
                    }
                }
            }
            var sharedEvent=livingPersons.SharedConversationMemory(player.StringId,actor.StringId);
            if(sharedEvent!=null)
            {
                switch(sharedEvent.Kind)
                {
                    case "FamilyVisit":return "I remember the last time we made time for our family affairs.";
                    case "FamilyGiftConfirmed":return "I remember the gift you placed in my hands. You owe me nothing further for it.";
                    case "HouseholdDutyAppointed":return "We spoke of the office you entrusted to me. I know what was asked of me then.";
                    case "HouseholdDutyReleased":return "We spoke when you released me from my former duty.";
                    case "HouseholdDutyReviewed":return "We reviewed my responsibilities together before.";
                    case "NativeMarriageObserved":
                        if(actor==player.Spouse && actor.Spouse==player)return "We are married, and I value the chance to speak without ceremony.";
                        break;
                    case "VerifiedSiblingComradeship":return "We have marched together.";
                    case "PersonalAudienceAnswered":return "We have had a private audience before. We need not settle everything in one sitting.";
                    case "PersonalDialogueChoice":
                        var fields=sharedEvent.Detail.Split('|');
                        if(fields.Length==3 && fields[1].StartsWith("topic=",StringComparison.Ordinal))
                        {
                            string subject=fields[1].Substring(6);
                            if(SandboxLifeHistoryState.IsConversationTopic(subject))
                            {
                                string words=subject=="Family"?"our family":subject=="Estate"?"the holdings":
                                    subject=="Finances"?"money":subject=="Service"?"our duties":
                                    subject=="Standing"?"our standing":subject=="Ambitions"?"my ambitions":
                                    subject=="Concerns"?"our concerns":subject=="Memory"?"our earlier dealings":"our beliefs";
                                if(fields[2]=="stance=disagreed")return "Last time we spoke of "+words+
                                    ", you saw things differently. I am willing to hear you again.";
                                if(fields[2]=="stance=asked_advice")return "You asked my counsel about "+words+
                                    ". I would be glad to revisit it.";
                            }
                        }
                        break;
                }
            }
            // Only the named wounded Hero or a recorded patron/host is a
            // witness; mere current membership is not proof of past presence.
            var battle=battleHistory.Entries.LastOrDefault(b=>
                b.PatronWitnessId==actor.StringId ||
                (b.Source=="ServeAsSoldier" && b.LordId==actor.StringId) ||
                b.WoundedHeroIds.Split(',').Contains(actor.StringId));
            if(battle!=null)
            {
                if(battle.WoundedHeroIds.Split(',').Contains(actor.StringId))
                    return "I remember being wounded in that campaign. Let us not treat the cost of battle lightly.";
                return "I remember that encounter in our campaign records. "+
                    (battle.Outcome=="Victory"?"Our side prevailed.":
                    battle.Outcome=="Defeat"?"Our side did not prevail.":"Its outcome was not settled.");
            }
            return "";
        }

        private void RecordPersonalFollowUp(bool disagreed)
        {
            if(!CanFollowPersonalTopic())return;
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(CanDiscussPersonalMatters())
                livingPersons.RecordConversationChoice(player.StringId,actor.StringId,
                    personalTopicName,disagreed,CurrentHouseholdDay());
        }

        private bool CanExpressRelationshipIntention(bool repair)
        {
            if(!CanFollowPersonalTopic() || personalTopicName!="Standing")return false;
            var player=Hero.MainHero;
            var actor=Hero.OneToOneConversationHero;
            if(!HasPersonalStandingContext(player,actor))return false;
            int relation=player.GetRelation(actor);
            return repair?relation<0:relation>=20;
        }

        private void ExpressRelationshipIntention(bool repair)
        {
            personalReply="Let us speak of this another time.";
            if(!CanExpressRelationshipIntention(repair))return;
            var player=Hero.MainHero;
            var actor=Hero.OneToOneConversationHero;
            string tone=PersonalVoice(actor);
            int relation=player.GetRelation(actor);
            livingPersons.RecordRelationshipIntention(player.StringId,actor.StringId,
                repair?"Repair":"Care",CurrentHouseholdDay());
            if(repair)
                personalReply=relation<=-30 || tone=="Guarded"?
                    "I have heard you. I will not say that trust has returned simply from these words.":
                    "I am willing to hear you. Let our dealings from this day show what we mean.";
            else
                personalReply=actor==player.Spouse && actor.Spouse==player?
                    "I value our life together, too. Let us make time for each other.":
                    tone=="Guarded"?"I value deeds more than assurances. Let us keep faith.":
                    "I value the regard we have for one another. Let us not take it for granted.";
        }

        private void SelectPersonalTopic(string topic)
        {
            var actor=Hero.OneToOneConversationHero;
            personalTopicActorId=actor==null?"":actor.StringId;
            personalTopicName=topic;
            personalTopicVariant=actor!=null && CanDiscussPersonalTopic(topic)?NextDialogueVariant(actor,topic,3):0;
            personalReply="";
        }

        private bool CanFollowPersonalTopic()
        {
            var actor=Hero.OneToOneConversationHero;
            return actor!=null && personalTopicActorId==actor.StringId &&
                personalTopicName.Length>0 && CanDiscussPersonalTopic(personalTopicName);
        }

        private string PersonalFollowUpText(bool disagree)
        {
            if(!CanFollowPersonalTopic())return "We can leave that subject for another time.";
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            string tone=PersonalVoice(actor);
            int relation=player.GetRelation(actor);
            string answer;
            switch(personalTopicName)
            {
                case "Family":
                    var children=player.Children==null?null:player.Children.Where(h=>
                        h!=null && h.IsAlive && h.Age<18f &&
                        (actor!=player.Spouse || h.Father==actor || h.Mother==actor))
                        .OrderBy(h=>h.Age).ToArray();
                    answer=children!=null && children.Length>0?
                        "Think of "+children[0].Name+". At "+(int)children[0].Age+
                        ", there is still time to shape the habits that will carry into adulthood. "+
                        (tone=="Bold"?"Courage can be taught without courting danger.":
                        tone=="Prudent"?"I would teach sound judgment before giving them great responsibilities.":
                        "I would rather we gave our time than tried to settle everything with gifts."):
                        "Kinship is not a duty we can discharge once and forget. I would keep the door open to our family.";
                    break;
                case "Estate":
                case "Concerns":
                    var clan=Clan.PlayerClan;
                    var siege=clan==null?null:clan.Settlements.FirstOrDefault(x=>
                        x!=null && x.OwnerClan==clan && x.IsUnderSiege);
                    var seat=Settlement.CurrentSettlement;
                    if(SpeakingWithPendingPetitioner())answer=
                        "I would rather explain the matter itself. Ask me about my petition, and judge it for yourself.";
                    else if(siege!=null && CanDiscussEstateTopic())answer=siege.Name+
                        " cannot be treated like any other holding just now. "+
                        (tone=="Bold"?"We should think about relieving the siege before other business.":
                        "I would put the safety of its people ahead of any new undertaking.");
                    else if(CanDiscussEstateTopic() && seat!=null && clan!=null &&
                        seat.OwnerClan==clan && seat.IsFortification)
                    {
                        var g=seat.Town==null?null:seat.Town.GarrisonParty;
                        answer=g!=null && g.MemberRoster!=null?
                            "There are "+g.MemberRoster.TotalManCount+" soldiers in the garrison of "+seat.Name+
                            ". That is a beginning, not a measure of every threat. Consider what you can spare before moving any men.":
                            "I would speak to the castellan before committing men we have not counted.";
                    }
                    else if(IsPersonalChargeActive() && CanCounselCareer())answer=
                        ChargeDescription()+" I would finish what we have undertaken before taking on more.";
                    else answer="I have no particular danger to point to. Let us not invent trouble where there is none.";
                    break;
                case "Finances":
                    answer="Your purse holds "+player.Gold+" denars. "+
                        (tone=="Prudent"?"I would count the next expenses before promising any of it.":
                        tone=="Bold"?"Spend for a clear purpose, not to impress someone at court.":
                        "I would keep a reserve for the people who rely on you. A gift should not leave them wanting.");
                    break;
                case "Service":
                    answer=IsPersonalChargeActive()?ChargeDescription()+
                        " Judge us by our deeds.":
                        "We have agreed no particular charge. I would rather choose a duty we can carry out than make a grand promise.";
                    break;
                case "Standing":
                    answer=actor==player.Spouse && actor.Spouse==player?
                        (relation<0?"I would rather hear what troubles you than accept another promise of affection. We can speak without agreeing.":
                         "Make time for us, not merely for our duties. I would rather we made decisions together than heard of them afterward."):
                        relation<0?"Trust is not restored by asking for it. Give me reason to see matters differently.":
                        relation>=20?"I value our good standing. I would not put it at risk with a promise neither of us can keep.":
                        "We need time and honest dealings before I can speak of deeper trust.";
                    break;
                case "Memory":
                    answer=PersonalMemoryText();
                    if(answer.Length==0)answer="There is nothing more I can recall with certainty.";
                    else answer+=" We can take account of that without making another bargain.";
                    break;
                case "Ambitions":
                    string office=RecordedRetinueOffice(actor);
                    var ambition=characterAgendas.Get(actor.StringId);
                    string goal=ambition==null?"":ambition.Goal;
                    answer=office=="MasterOfArms" || office=="Marshal" || office=="Kingsguard"?
                        "Let my deeds speak for me, not my title.":
                        goal=="BuildTradingContacts" || goal=="EstablishLivelihood"?
                        "I want to build a livelihood of my own. I would start with a modest undertaking, not put our whole household's silver at risk.":
                        goal=="GainMilitaryTraining"?
                        "I would learn from those who have served longer than I have, and prove myself through useful work.":
                        goal=="ProtectSharedHousehold" || goal=="MaintainHousehold" || goal=="SupportHousehold"?
                        "I would settle our family's needs before chasing honors. A flourishing household is an ambition in its own right.":
                        goal=="DefendClanStanding" || goal=="PreserveFamilyPosition"?
                        "I want our house to be respected for keeping its word. I would not purchase a reputation with empty promises.":
                        goal=="MapSafePassages"?
                        "I would see the roads for myself before trusting rumor.":
                        goal=="KeepCompanyTogether"?
                        "I want our company to remain together. I would rather hear what our people need than demand blind obedience.":
                        tone=="Prudent"?"I would take a small step I can sustain rather than make a promise beyond my means.":
                        tone=="Bold"?"I would rather attempt something difficult than wait for fortune to find me.":
                        "I want the chance to make my own choices, not have every matter settled for me.";
                    break;
                default:
                    answer=SandboxPetitionVoicePolicy.Opinion(tone,"Opinions");
                    answer+=" I do not expect every person to see things as I do.";
                    break;
            }
            if(!disagree)return answer;
            string preface=relation<0?"I expected you to differ. ":
                tone=="Guarded"?"You need not take my word for it. ":
                tone=="Bold"?"Then speak plainly. I stand by my judgment. ":
                tone=="Prudent"?"There is room to weigh both views. ":
                tone=="Dutiful"?"We can differ without breaking faith. ":
                "I hear you. We need not think alike. ";
            return preface+answer;
        }

        private bool PreparePersonalFollowUp()
        {
            MBTextManager.SetTextVariable("CU_PERSONAL_FOLLOWUP",new TextObject(
                CanFollowPersonalTopic()?personalReply:"Let us leave that subject for another time."));
            return true;
        }

        private bool HasPersonalStandingContext(Hero player,Hero actor)
        {
            if(player==null || actor==null || player==actor)return false;
            int relation=player.GetRelation(actor);
            return relation>=20 || relation<=-10 ||
                livingPersons.People.Any(p=>p.Id==actor.StringId) ||
                livingPersons.SharedConversationMemory(player.StringId,actor.StringId)!=null;
        }

        private bool CanDiscussPersonalMatters()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(!CanSpeakSocially())return false;
            if(SpeakingWithAdultFamily() || CanDiscussHouseholdDuty() ||
                SpeakingWithHouseholdCompanion() ||
                HasPersonalStandingContext(player,actor))return true;
            if(knightOath.PlayerId==player.StringId && knightOath.HasCurrentOath &&
                actor==KnightOathPatron())return true;
            if(SelectedReviewRole()!="Soldier")return false;
            var service=SandboxSaSBridge.Read();
            return service.Available && service.Enlisted && service.LordId==actor.StringId;
        }

        // Acquaintances can discuss opinions, but cannot issue commands for
        // a household, army or oath they do not belong to.
        private bool CanCounselCareer()
        {
            if(!CanSpeakSocially())return false;
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            string role=SelectedReviewRole();
            if(role=="Knight")return SpeakingWithSwornPatron();
            if(role=="Soldier")
            {
                var service=SandboxSaSBridge.Read();
                return service.Available && service.Enlisted && service.InsideHostParty &&
                    service.LordId==actor.StringId;
            }
            return (SpeakingWithAdultFamily() || CanDiscussHouseholdDuty() ||
                SpeakingWithHouseholdCompanion()) && actor.Clan==Clan.PlayerClan &&
                player.GetRelation(actor)>=-10;
        }
        private bool CanOfferCharge(string kind)
        {
            return CanCounselCareer() && CanBeginCareerCharge(kind);
        }
        private bool CanBeginCareerCharge(string kind)
        {
            // The sworn patron already owns knight service orders and musters.
            // Keep old saved muster charges readable, but offer no parallel task.
            string target;int baseline;
            return kind!="Muster" && !IsPersonalChargeActive() &&
                TryChargeStartEvidence(kind,out target,out baseline);
        }
        private bool CanDiscussPersonalTopic(string topic)
        {
            if(!CanSpeakSocially())return false;
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(topic=="Opinions")return true;
            if(topic=="Concerns")return SpeakingWithAdultFamily() ||
                CanDiscussHouseholdDuty() || SpeakingWithPendingPetitioner() || CanCounselCareer();
            if(!CanDiscussPersonalMatters())return false;
            if(topic=="Standing")return HasPersonalStandingContext(player,actor);
            if(topic=="Family")return SpeakingWithAdultFamily();
            if(topic=="Finances")return player.GetRelation(actor)>=10 &&
                (SpeakingWithAdultFamily() || CanDiscussHouseholdDuty());
            if(topic=="Estate")return CanDiscussEstateTopic() && player.GetRelation(actor)>=0;
            if(topic=="Service")return CanCounselCareer();
            if(topic=="Memory")
            {
                return PersonalMemoryText().Length>0;
            }
            return topic=="Ambitions" && characterAgendas.Get(actor.StringId)!=null;
        }
        private bool IsPersonalChargeActive()
        {
            return Hero.MainHero!=null && careerCharge.Active &&
                careerCharge.HeroId==Hero.MainHero.StringId;
        }

        private bool TryChargeStartEvidence(string kind,out string target,out int baseline)
        {
            target="";baseline=-1;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            var place=Settlement.CurrentSettlement;
            string role=SelectedReviewRole();
            if(!CanRecordLifetimeRole(role) || player==null || clan==null || party==null ||
                !SandboxCareerChargeState.IsKindForRole(role,kind))return false;
            switch(kind)
            {
                case "VisitTown":
                    if(place==null || !place.IsTown)return false;
                    target=place.StringId;return true;
                case "BuildBand":
                    if(party.MemberRoster==null)return false;
                    target=clan.StringId;baseline=party.MemberRoster.TotalManCount;return true;
                case "CompanyBattle":
                    if(clan.Kingdom==null)return false;
                    target=clan.Kingdom.StringId;return true;
                case "ServeWeek":
                    var service=SandboxSaSBridge.Read();
                    if(!service.Available || !service.Enlisted || !service.InsideHostParty)return false;
                    target=service.LordId;baseline=service.Days;return baseline>=0;
                case "Muster":
                    target=knightOath.PatronId;baseline=knightOath.LastMusterDay;return true;
                case "VisitEstate":
                    var estate=clan.Settlements.Where(x=>x!=null && x.IsFortification &&
                        x.OwnerClan==clan && !x.IsUnderSiege && x!=place)
                        .OrderBy(x=>x.StringId,StringComparer.Ordinal).FirstOrDefault();
                    if(estate==null)return false;
                    target=estate.StringId;return true;
                case "StrengthenGarrison":
                    if(place==null || !place.IsFortification || place.OwnerClan!=clan ||
                        place.IsUnderSiege || place.Town==null || place.Town.GarrisonParty==null ||
                        place.Town.GarrisonParty.MemberRoster==null)return false;
                    target=place.StringId;baseline=place.Town.GarrisonParty.MemberRoster.TotalManCount;return true;
                case "Council":
                    var seat=RecordedRoyalSeat();
                    if(seat==null || royalCouncil.KingId!=player.StringId)return false;
                    target=seat.StringId;baseline=royalCouncil.LastMeetingDay;return true;
                default:return false;
            }
        }

        private string ChargeDescription()
        {
            if(!IsPersonalChargeActive())return "Nothing binds us to a particular duty at present.";
            string target=careerCharge.TargetId;
            if(careerCharge.Kind=="VisitTown" || careerCharge.Kind=="VisitEstate" ||
                careerCharge.Kind=="StrengthenGarrison" || careerCharge.Kind=="Council")
            {
                var place=Settlement.Find(target);
                if(place!=null)target=place.Name.ToString();
            }
            else if(careerCharge.Kind=="Muster" || careerCharge.Kind=="ServeWeek")
                target=NativeHeroDisplayName(target);
            else if(careerCharge.Kind=="BuildBand" && Clan.PlayerClan!=null)
                target=Clan.PlayerClan.Name.ToString();
            else if(careerCharge.Kind=="CompanyBattle")
                target=Clan.PlayerClan!=null && Clan.PlayerClan.Kingdom!=null &&
                    Clan.PlayerClan.Kingdom.StringId==careerCharge.TargetId?
                    Clan.PlayerClan.Kingdom.Name.ToString():"our former employer";
            switch(careerCharge.Kind)
            {
                case "VisitTown":return "There is little reason to stay in "+target+
                    ". Let us seek our fortunes in another town and speak again after the journey.";
                case "BuildBand":return "Let us build our company to at least "+
                    (careerCharge.Baseline+3)+" souls. We shall speak again once the band is stronger.";
                case "CompanyBattle":return "Let us earn our keep under the banner of "+target+
                    ". We shall have more to discuss after a victory.";
                case "ServeWeek":return "Seven more days of service to "+target+
                    " lie before us. Let us speak of the matter after that.";
                case "Muster":return "We must attend the next muster of "+target+".";
                case "VisitEstate":return "We have been meaning to visit "+target+
                    ". Let us see the holding for ourselves.";
                case "StrengthenGarrison":return "Let us bring the garrison of "+target+
                    " to at least "+(careerCharge.Baseline+5)+" soldiers.";
                case "Council":return "We should gather the council at "+target+" on another day.";
                default:return "There is nothing further to arrange.";
            }
        }

        private string AgreeCareerCharge(string kind)
        {
            string target;int baseline;
            if(kind=="Muster")return "Speak with your sworn lord about your next service.";
            if(!TryChargeStartEvidence(kind,out target,out baseline))return
                "We cannot undertake that duty in our present circumstances.";
            var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            string role=SelectedReviewRole();
            if(!careerCharge.Start(player.StringId,role,kind,target,day,baseline,true))
                return "Let us settle the charge already agreed, or return on another day.";
            DiagnosticJournal.Event("cu_career_charge_agreed","hero="+player.StringId+"|role="+role+
                "|kind="+kind+"|target="+target+"|baseline="+baseline+"|day="+day);
            return "Then this shall be our charge. "+ChargeDescription();
        }

        private bool ObserveChargeCompletion()
        {
            if(!IsPersonalChargeActive() || !CanRecordLifetimeRole(careerCharge.Role) ||
                SelectedReviewRole()!=careerCharge.Role)return false;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            var place=Settlement.CurrentSettlement;
            string target="";int value=-1;
            switch(careerCharge.Kind)
            {
                case "VisitTown":
                    if(place!=null && place.IsTown)target=place.StringId;
                    break;
                case "BuildBand":
                    target=clan.StringId;
                    if(party.MemberRoster!=null)value=party.MemberRoster.TotalManCount;
                    break;
                case "CompanyBattle":
                    if(clan.Kingdom==null)return false;
                    target=clan.Kingdom.StringId;
                    value=battleHistory.LatestMercenaryVictory(player.StringId,
                        careerCharge.TargetId,careerCharge.StartedDay);
                    break;
                case "ServeWeek":
                    var service=SandboxSaSBridge.Read();
                    if(!service.Available || !service.Enlisted || !service.InsideHostParty)return false;
                    target=service.LordId;value=service.Days;break;
                case "Muster":
                    target=knightOath.PatronId;value=knightOath.LastMusterDay;break;
                case "VisitEstate":
                case "StrengthenGarrison":
                    if(place==null || !place.IsFortification || place.OwnerClan!=clan ||
                        place.IsUnderSiege)return false;
                    target=place.StringId;
                    if(careerCharge.Kind=="VisitEstate")value=1;
                    else if(place.Town!=null && place.Town.GarrisonParty!=null &&
                        place.Town.GarrisonParty.MemberRoster!=null)
                        value=place.Town.GarrisonParty.MemberRoster.TotalManCount;
                    break;
                case "Council":
                    if(!NativeRoyalCouncilPresent(true))return false;
                    target=royalCouncil.SeatId;value=royalCouncil.LastMeetingDay;break;
            }
            return careerCharge.CanComplete(player.StringId,careerCharge.Role,
                CurrentHouseholdDay(),target,value,true);
        }

        private string DiscussChargeProgress()
        {
            if(!IsPersonalChargeActive())return ChargeDescription();
            if(SelectedReviewRole()!=careerCharge.Role)
                return "Our calling has changed. Let us set the former charge aside.";
            if(careerCharge.Kind=="CompanyBattle" && (Clan.PlayerClan==null ||
                !Clan.PlayerClan.IsUnderMercenaryService || Clan.PlayerClan.Kingdom==null ||
                Clan.PlayerClan.Kingdom.StringId!=careerCharge.TargetId))
                return "That promise was made to another employer. We can set it aside.";
            if(!ObserveChargeCompletion())return "The charge is not yet fulfilled. "+ChargeDescription();
            var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            string role=careerCharge.Role,kind=careerCharge.Kind,target=careerCharge.TargetId;
            // Existing career receipt is the only progression record.
            if(!careerCharge.Close(player.StringId,day))
                return "We have already settled that matter.";
            bool formalReview=recurringCareerReviews.Record(player.StringId,role,day,true);
            livingPersons.Remember("CareerReviewObserved",player.StringId,target,
                "role="+role+"|charge="+kind+"|day="+day+"|native_evidence=true");
            DiagnosticJournal.Event("cu_career_charge_completed","hero="+player.StringId+
                "|role="+role+"|kind="+kind+"|target="+target+"|day="+day+
                "|weekly_review="+formalReview+"|extra_reward=false");
            return "You have carried out what we agreed. There is no more to be done on that matter.";
        }

        private string WithdrawCareerCharge()
        {
            if(!IsPersonalChargeActive() || !careerCharge.Close(Hero.MainHero.StringId,CurrentHouseholdDay()))
                return "There is no charge to withdraw.";
            DiagnosticJournal.Event("cu_career_charge_withdrawn","hero="+Hero.MainHero.StringId+"|reward=false");
            return "Then we set that plan aside. Let us attend to what lies before us.";
        }

        private string educationChildId="";
        private string educationReply="";
        private Hero EducationChild(int index)
        {
            var player=Hero.MainHero;
            var spouse=Hero.OneToOneConversationHero;
            if(!CanSpeakSocially() || spouse!=player.Spouse ||
                spouse.Spouse!=player || player.GetRelation(spouse)<0 ||
                player.Children==null)return null;
            return player.Children.Where(h=>h!=null && h.IsAlive && h.Age<18f &&
                (h.Father==player || h.Mother==player) &&
                (h.Father==spouse || h.Mother==spouse) &&
                familyGrowth.UpbringingFor(h.StringId).Length>0)
                .OrderBy(h=>h.StringId,StringComparer.Ordinal).Skip(index).FirstOrDefault();
        }
        private Hero SelectedEducationChild()
        {
            for(int i=0;i<6;i++)
            {
                var child=EducationChild(i);
                if(child!=null && child.StringId==educationChildId)return child;
            }
            return null;
        }
        private bool PrepareEducationOption(int index)
        {
            var child=EducationChild(index);
            if(child==null)return false;
            MBTextManager.SetTextVariable("CU_EDUCATION_CHILD_"+index,new TextObject(child.Name.ToString()));
            return true;
        }
        private bool PrepareEducationDiscussion()
        {
            var child=SelectedEducationChild();
            string reply=child==null?"Let us speak of this another time.":
                "What shall we encourage in "+child.Name+"? "+
                SandboxPetitionVoicePolicy.Opinion(PersonalVoice(Hero.OneToOneConversationHero),"Family");
            if(child!=null)
            {
                string current=familyGrowth.UpbringingFor(child.StringId);
                string emphasis=current=="Kind"?"kindness":current=="Martial"?"the practice of arms":
                    current=="Scholarly"?"study":current=="Mercantile"?"the ways of trade":
                    current=="Seafaring"?"knowledge of the sea":current=="Independent"?"independent judgment":"";
                if(emphasis.Length>0)reply+=" We have been encouraging "+emphasis+". Do you wish to change course?";
            }
            MBTextManager.SetTextVariable("CU_EDUCATION_DISCUSSION",new TextObject(reply));
            return true;
        }
        private void ChooseEducation(string plan)
        {
            var child=SelectedEducationChild();
            educationReply="There is nothing to change just now.";
            if(child==null)return;
            if(familyGrowth.ReviseUpbringing(child.StringId,plan,(int)child.Age))
            {
                educationReply=SandboxPetitionVoicePolicy.DecisionReply(
                    PersonalVoice(Hero.OneToOneConversationHero),true,true)+
                    " We shall keep this in mind as "+child.Name+" grows.";
                DiagnosticJournal.Event("cu_family_upbringing_revised","hero="+child.StringId+
                    "|plan="+plan+"|no_immediate_skill_grant=true|prior_milestones_preserved=true");
            }
        }
        private bool PrepareEducationReply()
        {
            MBTextManager.SetTextVariable("CU_EDUCATION_REPLY",new TextObject(educationReply));
            return true;
        }
        private bool PreparePersonalTopic(string topic)
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            string reply="Let us speak again when we are together.";
            if(SandboxLifeHistoryState.IsConversationTopic(topic) && CanDiscussPersonalTopic(topic))
            {
                var agenda=characterAgendas.Get(actor.StringId);
                int relation=player.GetRelation(actor);
                string tone=PersonalVoice(actor);
                if(topic=="Opinions")
                {
                    reply=SandboxPetitionVoicePolicy.PersonalReflection(topic,tone,personalTopicVariant);
                    if(reply.Length==0)reply=SandboxPetitionVoicePolicy.Opinion(tone,"Opinions");
                }
                else if(topic=="Concerns")
                {
                    var clan=Clan.PlayerClan;
                    var siege=clan==null?null:clan.Settlements.FirstOrDefault(
                        x=>x!=null && x.OwnerClan==clan && x.IsUnderSiege);
                    if(SpeakingWithPendingPetitioner())
                        reply="There is still the matter I wished to put before you.";
                    else if(siege!=null && (SpeakingWithAdultFamily() || CanDiscussHouseholdDuty()))
                        reply=siege.Name+" is under siege. "+
                            SandboxPetitionVoicePolicy.Opinion(tone,"Estate");
                    else if(IsPersonalChargeActive() && CanCounselCareer())
                        reply=ChargeDescription()+" "+SandboxPetitionVoicePolicy.Opinion(tone,"Service");
                    else if(SpeakingWithAdultFamily() && player.Children!=null &&
                        player.Children.Any(h=>h!=null && h.IsAlive && h.Age<18f))
                        reply="The young ones are growing. "+
                            SandboxPetitionVoicePolicy.Opinion(tone,"Family");
                    else reply="There is nothing urgent I can tell you just now.";
                }
                else if(topic=="Standing")
                {
                    reply=actor==player.Spouse && actor.Spouse==player?
                        (relation<0?"We share a household, but I do not want us to pretend that everything between us is well.":
                         relation>=50?"I value the life we share. I would rather we spoke honestly than agreed for the sake of peace.":
                         relation>=20?"I am glad we can speak as husband and wife. Let us make time for one another.":
                         "I would like us to understand each other better, not merely share the same household."):
                        SpeakingWithHouseholdCompanion()?
                        (relation<0?"I serve in your company, but I have my doubts about our dealings.":
                         relation>=50?"I value your confidence. I would still speak against a course I think unwise.":
                         "I would judge our companionship by what we do for one another."):
                        SandboxPetitionVoicePolicy.StandingReply(relation);
                    var bond=socialBonds.For(actor.StringId);
                    bool knownSupport=livingPersons.Events.Any(e=>e.Actor==actor.StringId &&
                        e.Target==player.StringId && e.Kind=="CharacterRequestAccepted");
                    bool knownDecline=livingPersons.Events.Any(e=>e.Actor==actor.StringId &&
                        e.Target==player.StringId && e.Kind=="CharacterRequestDeclined");
                    if(knownSupport && bond!=null && bond.Supported>0 && bond.Positive>0 && relation>=20)
                        reply+=" I remember the help you gave in earlier matters.";
                    else if(knownDecline && bond!=null && bond.Declined>0 && bond.Negative<0 && relation<0)
                        reply+=" I remember that we did not agree on earlier matters.";
                    int shift=ObservedStandingShift(player,actor);
                    if(shift>0)reply+=" Our standing has improved since the earlier record. I would see that progress continue.";
                    else if(shift<0)reply+=" Matters between us have grown more strained. I would not dismiss that change.";
                    if(agenda!=null && agenda.NewsRecipient==player.StringId && agenda.NewsDay>=0 &&
                        SandboxCharacterAgendaState.StandingBand(relation)!=agenda.NewsStanding)
                        reply+=" Our standing is not quite as it was when last I wrote.";
                }
                else if(topic=="Ambitions")
                {
                    string goal=agenda==null?"":agenda.Goal;
                    switch(goal)
                    {
                        case "ProtectSharedHousehold":case "SupportHousehold":case "MaintainHousehold":
                            reply="My concern is the security of our household. I would see our kin provided for.";break;
                        case "GainMilitaryTraining":reply="I would earn my place through service, not merely a title.";break;
                        case "PreserveFamilyPosition":reply="I wish to preserve the standing of my family.";break;
                        case "EstablishLivelihood":case "BuildTradingContacts":
                            reply="I want the means to establish a livelihood of my own.";break;
                        case "MapSafePassages":reply="I would learn the roads and passages we can travel safely.";break;
                        case "ProtectDescendants":reply="I wish to see my children and their children secure.";break;
                        case "CompleteUpbringing":reply="I have much yet to learn before I find my own place.";break;
                        case "KeepCompanyTogether":reply="I would see the company stand together through hardship.";break;
                        case "DefendClanStanding":reply="I wish to preserve the name and standing of my house.";break;
                        default:reply="I have not settled on a particular ambition to share.";break;
                    }
                    string office=RecordedRetinueOffice(actor);
                    if(office=="Squire")reply="I would learn to carry myself well in the saddle and earn my place through service.";
                    else if(office=="MasterOfArms")reply="I would see the household's fighters well drilled and ready to stand together.";
                    else if(office=="Marshal")reply="I would keep our forces well ordered and bring our people safely home.";
                    else if(office=="Kingsguard")reply="My first concern is the safety of the sovereign. I mean to be worthy of that trust.";
                    else if(office=="RoyalMinister")reply="I would keep the affairs of the household in good order and preserve its holdings.";
                    else if(office=="Knight")reply="I would prove worthy of the service entrusted to me.";
                    var changed=livingPersons.Events.LastOrDefault(e=>e.Kind=="ObservedGoalReassessment" &&
                        e.Actor==actor.StringId && e.Target==player.StringId &&
                        e.Detail.Split('|').Contains("current="+goal));
                    if(changed!=null)reply+=" My circumstances have changed, and so have the plans I make.";
                    if(relation<0)reply+=" I am not ready to entrust all my concerns to you.";
                    else if(actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor)>0)
                        reply+=" I would rather earn it fairly than take what is not mine.";
                    else if(actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating)>0)
                        reply+=" We must weigh the cost before committing ourselves.";
                    string motive=SandboxStoryDirectorState.MotiveWords(goal,tone,relation,
                        SpeakingWithSwornPatron() || RecordedRetinueOffice(actor).Length>0);
                    if(motive.Length>0)reply+=" "+motive;
                }
                else if(topic=="Family")
                {
                    var kin=player.Children==null?new Hero[0]:player.Children.Where(h=>h!=null && h.IsAlive &&
                        (actor!=player.Spouse || h.Father==actor || h.Mother==actor)).ToArray();
                    int children=kin.Length;
                    int young=kin.Count(h=>h.Age<18f);
                    reply=children==0?(actor==player.Spouse?
                        "We have no living children of our own.":"You have no living children of your own."):
                        (actor==player.Spouse?"Our family":"Your family")+" has "+children+
                        " children living. "+(young>0?"The younger ones still need care.":"They have their own lives to lead.");
                    if(actor==player.Spouse && children>0)reply+=" Let us discuss how we provide for them.";
                    if(player.Children!=null)
                    {
                        var living=kin.OrderByDescending(h=>h.Age).Take(3).ToArray();
                        if(living.Length>0)
                        {
                            var child=living.FirstOrDefault(h=>h.Age<18f)??living[0];
                            reply+=" I think of "+child.Name+" in particular. "+(child.Age<18f?
                                "We still have a chance to guide the years ahead.":"It is time to respect the choices of our grown children.");
                        }
                    }
                }
                else if(topic=="Finances")
                    reply="You have "+player.Gold+" denars in your purse. I hold "+actor.Gold+
                        ". Let us keep enough for what lies ahead.";
                else if(topic=="Estate")
                {
                    var clan=Clan.PlayerClan;
                    var seat=Settlement.CurrentSettlement;
                    reply=clan==null?"I cannot speak for a house without a clan.":
                        "Our house holds "+clan.Settlements.Count+" settlements. ";
                    if(clan!=null && seat!=null && seat.OwnerClan==clan && seat.IsFortification)
                    {
                        var garrison=seat.Town==null?null:seat.Town.GarrisonParty;
                        reply+="Here at "+seat.Name+", "+
                            (garrison==null||garrison.MemberRoster==null?"the garrison count is not known.":
                             "the garrison numbers "+garrison.MemberRoster.TotalManCount+".");
                        if(seat.IsUnderSiege)reply+=" The siege must take precedence.";
                        else reply+=" We should consider the defenses before leaving.";
                    }
                }
                else if(topic=="Service")
                {
                    reply=ChargeDescription();
                    var recent=battleHistory.Entries.LastOrDefault();
                    if(recent!=null && recent.Day>=0 &&
                        (long)CurrentHouseholdDay()-recent.Day>=0 &&
                        (long)CurrentHouseholdDay()-recent.Day<=14 && recent.PartyDead>=0)
                        reply+=" Our campaign record recently counted "+recent.PartyDead+
                            " fallen and "+recent.PartyWounded+" wounded among those who fought.";
                }
                else if(topic=="Memory")
                {
                    reply=PersonalMemoryText();
                    if(reply.Length==0)reply="There is nothing further I can recall with certainty.";
                }
                if(CanDiscussStory(topic))reply+=" "+StoryWords();
                string opinion=topic=="Opinions" || topic=="Memory"?"":SandboxPetitionVoicePolicy.PersonalReflection(topic,tone,personalTopicVariant);
                if(opinion.Length==0 && topic!="Opinions" && topic!="Memory")opinion=SandboxPetitionVoicePolicy.Opinion(tone,topic);
                if(opinion.Length>0 && topic!="Concerns" && !reply.Contains(opinion))reply+=" "+opinion;
            }
            MBTextManager.SetTextVariable("CU_PERSONAL_TOPIC_REPLY",new TextObject(reply));
            return true;
        }

        private bool CanDiscussEstateTopic()
        {
            return (SpeakingWithAdultFamily() || CanDiscussHouseholdDuty()) &&
                Clan.PlayerClan!=null && Clan.PlayerClan.Settlements.Count>0;
        }

        private bool PrepareChargeReply()
        {
            MBTextManager.SetTextVariable("CU_CHARGE_REPLY",new TextObject(chargeReply));
            return true;
        }

        private void RegisterLivingDialogueDepth(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("cu_personal_matters_open","hero_main_options",
                "cu_personal_matters_intro","Let us speak privately for a moment.",
                ()=>CanSpeakSocially(),()=>BeginPersonalConversation(),105);
            starter.AddDialogLine("cu_personal_matters_intro","cu_personal_matters_intro",
                "cu_personal_matters_topics","{CU_PERSONAL_GREETING}",()=>PreparePersonalGreeting(),null,105);
            starter.AddDialogLine("cu_personal_matters_intro_cu_safe_exit","cu_personal_matters_intro",
                "close_window","We can speak again another time.",null,null,1);
            foreach(string topicName in new[]{"Opinions","Concerns","Ambitions","Family","Finances","Estate","Service","Standing","Memory"})
            {
                string topic=topicName;
                string question=topic=="Opinions"?"What matters most to you in life?":
                    topic=="Concerns"?"Is there anything that needs our attention?":topic=="Ambitions"?"What do you wish for yourself?":
                    topic=="Family"?"How shall we care for the family?":
                    topic=="Finances"?"How stand our personal finances?":
                    topic=="Estate"?"What concerns our holdings?":
                    topic=="Service"?"What charge lies before us?":
                    topic=="Standing"?"How do matters stand between us?":
                    "Do you remember our earlier affairs?";
                starter.AddPlayerLine("cu_personal_topic_"+topic,"cu_personal_matters_topics",
                    "cu_personal_topic_reply_"+topic,question,
                    ()=>CanDiscussPersonalTopic(topic),()=>SelectPersonalTopic(topic),100);
                starter.AddDialogLine("cu_personal_topic_reply_"+topic,"cu_personal_topic_reply_"+topic,
                    "cu_personal_followup_choices","{CU_PERSONAL_TOPIC_REPLY}",
                    ()=>PreparePersonalTopic(topic),null,100);
            starter.AddDialogLine("cu_personal_topic_reply__cu_safe_exit"+topic,"cu_personal_topic_reply_"+topic,
                "close_window","We can speak again another time.",null,null,1);
            }
            starter.AddPlayerLine("cu_personal_initiative","cu_personal_matters_topics",
                "cu_personal_initiative_reply","You had something in mind. Tell me.",
                ()=>SelectedInitiative().Length>0,()=>SelectPersonalTopic(SelectedInitiative()),112);
            starter.AddDialogLine("cu_personal_initiative_reply","cu_personal_initiative_reply",
                "cu_personal_followup_choices","{CU_PERSONAL_TOPIC_REPLY}",
                ()=>PreparePersonalTopic(CanFollowPersonalTopic()?personalTopicName:""),null,100);
            starter.AddDialogLine("cu_personal_initiative_safe","cu_personal_initiative_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine("cu_personal_followup_ask","cu_personal_followup_choices",
                "cu_personal_followup_reply","What would you do in my place?",
                ()=>CanFollowPersonalTopic(),()=>{personalReply=PersonalFollowUpText(false);RecordPersonalFollowUp(false);},100);
            starter.AddPlayerLine("cu_personal_followup_disagree","cu_personal_followup_choices",
                "cu_personal_followup_reply","I see the matter differently.",
                ()=>CanFollowPersonalTopic(),()=>{personalReply=PersonalFollowUpText(true);RecordPersonalFollowUp(true);},95);
            starter.AddDialogLine("cu_personal_followup_reply","cu_personal_followup_reply",
                "cu_personal_matters_topics","{CU_PERSONAL_FOLLOWUP}",()=>PreparePersonalFollowUp(),null,100);
            starter.AddDialogLine("cu_personal_followup_reply_safe","cu_personal_followup_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine("cu_personal_relationship_repair","cu_personal_followup_choices",
                "cu_personal_followup_reply","I wish to mend matters between us.",
                ()=>CanExpressRelationshipIntention(true),()=>ExpressRelationshipIntention(true),96);
            starter.AddPlayerLine("cu_personal_relationship_care","cu_personal_followup_choices",
                "cu_personal_followup_reply","I value what we share.",
                ()=>CanExpressRelationshipIntention(false),()=>ExpressRelationshipIntention(false),96);
            foreach(string choice in new[]{"Listen","Caution","Defer","Finish"})
            {
                string response=choice;
                string words=response=="Listen"?"Tell me more. I would hear your concerns.":
                    response=="Caution"?"Take care before deciding what to do.":
                    response=="Defer"?"Let us leave this matter here.":"How do you see the matter now?";
                starter.AddPlayerLine("cu_personal_story_"+response,"cu_personal_followup_choices",
                    "cu_personal_followup_reply",words,()=>CanAnswerStory(response=="Finish"),
                    ()=>AnswerStory(response),110);
            }
            starter.AddPlayerLine("cu_personal_followup_back","cu_personal_followup_choices",
                "cu_personal_followup_back_reply","Let us speak of something else.",null,null,85);
            starter.AddDialogLine("cu_personal_followup_back_reply","cu_personal_followup_back_reply",
                "cu_personal_matters_topics","Of course. What else is on your mind?",null,null,85);
            starter.AddPlayerLine("cu_personal_followup_exit","cu_personal_followup_choices",
                "close_window","That is all for now.",null,null,80);
            for(int i=0;i<6;i++)
            {
                int slot=i;
                starter.AddPlayerLine("cu_family_education_child_"+slot,"cu_personal_matters_topics",
                    "cu_family_education_discussion","Let us speak of {CU_EDUCATION_CHILD_"+slot+"}'s upbringing.",
                    ()=>PrepareEducationOption(slot),()=>
                    {
                        var child=EducationChild(slot);
                        educationChildId=child==null?"":child.StringId;
                    },95);
            }
            starter.AddDialogLine("cu_family_education_discussion","cu_family_education_discussion",
                "cu_family_education_choices","{CU_EDUCATION_DISCUSSION}",()=>PrepareEducationDiscussion(),null,100);
            starter.AddDialogLine("cu_family_education_discussion_cu_safe_exit","cu_family_education_discussion",
                "close_window","We can speak again another time.",null,null,1);
            foreach(string choice in new[]{"Kind","Martial","Scholarly","Mercantile","Seafaring","Independent"})
            {
                string plan=choice;
                string words=plan=="Kind"?"Let us teach kindness and generosity.":
                    plan=="Martial"?"Let us encourage the practice of arms.":
                    plan=="Scholarly"?"Let us encourage study and learning.":
                    plan=="Mercantile"?"Let us teach the ways of trade.":
                    plan=="Seafaring"?"Let us encourage knowledge of the sea.":
                    "Let us encourage independent judgment.";
                starter.AddPlayerLine("cu_family_education_choose_"+plan,"cu_family_education_choices",
                    "cu_family_education_reply",words,
                    ()=>SelectedEducationChild()!=null &&
                        familyGrowth.UpbringingFor(educationChildId)!=plan,
                    ()=>ChooseEducation(plan),100);
            }
            starter.AddDialogLine("cu_family_education_reply","cu_family_education_reply",
                "cu_personal_matters_topics","{CU_EDUCATION_REPLY}",()=>PrepareEducationReply(),null,100);
            starter.AddDialogLine("cu_family_education_reply_cu_safe_exit","cu_family_education_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine("cu_family_education_leave","cu_family_education_choices",
                "cu_family_education_back_reply","Let us leave the matter for now.",null,null,80);
            starter.AddDialogLine("cu_family_education_back_reply","cu_family_education_back_reply",
                "cu_personal_matters_topics","Very well. What else is on your mind?",null,null,80);
            starter.AddPlayerLine("cu_family_education_end","cu_family_education_choices",
                "close_window","That is all for now.",null,null,70);
            starter.AddPlayerLine("cu_personal_charge_agree","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us agree on our next duty.",
                ()=>CanOfferCharge(SandboxCareerChargeState.DefaultKind(SelectedReviewRole())),
                ()=>chargeReply=AgreeCareerCharge(SandboxCareerChargeState.DefaultKind(SelectedReviewRole())),100);
            starter.AddPlayerLine("cu_personal_charge_defend","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us strengthen the garrison here.",
                ()=>CanDiscussEstateTopic() && CanOfferCharge("StrengthenGarrison"),
                ()=>chargeReply=AgreeCareerCharge("StrengthenGarrison"),100);
            starter.AddPlayerLine("cu_personal_charge_royal_estate","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us visit another royal holding.",
                ()=>CanDiscussEstateTopic() && SelectedReviewRole()=="King" && CanOfferCharge("VisitEstate"),
                ()=>chargeReply=AgreeCareerCharge("VisitEstate"),100);
            starter.AddPlayerLine("cu_personal_charge_progress","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us review what we have accomplished.",
                ()=>CanCounselCareer() && IsPersonalChargeActive(),
                ()=>chargeReply=DiscussChargeProgress(),100);
            starter.AddPlayerLine("cu_personal_charge_withdraw","cu_personal_matters_topics",
                "cu_personal_charge_reply","We must set our charge aside.",
                ()=>CanCounselCareer() && IsPersonalChargeActive(),
                ()=>chargeReply=WithdrawCareerCharge(),90);
            starter.AddDialogLine("cu_personal_charge_reply","cu_personal_charge_reply",
                "cu_personal_matters_topics","{CU_CHARGE_REPLY}",()=>PrepareChargeReply(),null,100);
            starter.AddDialogLine("cu_personal_charge_reply_cu_safe_exit","cu_personal_charge_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine("cu_personal_matters_return","cu_personal_matters_topics",
                "close_window","That is all for now.",null,null,80);
        }
    }
}
