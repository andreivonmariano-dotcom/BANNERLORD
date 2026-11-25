using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // A shared campaign-menu exchange, not a native mission conversation.
        // Only actual local adults speak. All session state is transient.
        private string groupParent="",groupPlayerId="",groupSeatId="",groupTopic="";
        private string groupOpening="",groupClosing="",groupOccasion="";
        private bool groupSecondRound,groupPlanned;
        private SandboxGatheringPlan gatheringPlan=new SandboxGatheringPlan();
        private int groupRoundSerial;
        private readonly List<Hero> groupParticipants=new List<Hero>();
        private readonly List<string> groupPreferences=new List<string>();

        private bool GroupLocationValid(string parent)
        {
            var player=Hero.MainHero;
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            if(!applied || !IsStandaloneSandbox() || Campaign.Current==null ||
                player==null || !player.IsAlive || player.IsPrisoner ||
                party==null || party.LeaderHero!=player || party.MemberRoster==null ||
                party.MapEvent!=null || clan==null)return false;
            if(parent=="cu_household_camp")return CanSetUpNativeCamp();
            if(parent!="cu_people_lineage" && parent!="cu_native_royal_council")return false;
            var seat=Settlement.CurrentSettlement;
            if(seat==null || !seat.IsFortification || seat.OwnerClan!=clan ||
                seat.IsUnderSiege)return false;
            return parent!="cu_native_royal_council" || NativeRoyalCouncilPresent(true);
        }

        private bool GroupParticipantPresent(Hero person,string parent)
        {
            var player=Hero.MainHero;
            var party=MobileParty.MainParty;
            if(person==null || person==player || !person.IsAlive || !person.IsActive ||
                person.IsPrisoner || person.Age<18f || party==null ||
                party.MemberRoster==null || person.CharacterObject==null)return false;
            if(person.PartyBelongedTo==party &&
                party.MemberRoster.GetTroopCount(person.CharacterObject)==1)return true;
            if(parent=="cu_household_camp")return false;
            var seat=Settlement.CurrentSettlement;
            if(seat==null)return false;
            if(person.StayingInSettlement==seat && person.PartyBelongedTo==null)return true;
            return person.PartyBelongedTo==null && person.CurrentSettlement==seat &&
                SandboxKingNativeMinistry.VerifiedGovernor(person) &&
                person.GovernorOf.Settlement==seat;
        }

        private bool LocalRoyalGuest(Hero person)
        {
            var clan=Clan.PlayerClan;var seat=Settlement.CurrentSettlement;
            if(person==null || clan==null || clan.Kingdom==null || seat==null ||
                person==Hero.MainHero || !person.IsAlive || !person.IsActive || person.IsPrisoner ||
                person.Age<18f || person.CharacterObject==null || person.Clan==null ||
                person.Clan==clan || person.Clan.Kingdom!=clan.Kingdom || person.Clan.Leader!=person ||
                person.Clan.IsEliminated || person.Clan.IsUnderMercenaryService || person.CurrentSettlement!=seat)return false;
            var visitingParty=person.PartyBelongedTo;
            return visitingParty==null?person.StayingInSettlement==seat:
                visitingParty.LeaderHero==person && visitingParty.CurrentSettlement==seat &&
                visitingParty.MapEvent==null && visitingParty.MemberRoster!=null &&
                visitingParty.MemberRoster.GetTroopCount(person.CharacterObject)==1;
        }
        private List<Hero> AvailableGroupParticipants(string parent,bool includeAll=false)
        {
            var result=new List<Hero>();
            if(!GroupLocationValid(parent))return result;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(parent=="cu_native_royal_council")
            {
                foreach(var officer in new[]{NativeCourtOfficer("Marshal"),
                    NativeCourtOfficer("Royal Minister"),NativeCourtOfficer("Kingsguard")})
                    if(GroupParticipantPresent(officer,parent) && !result.Contains(officer))
                        result.Add(officer);
                if(result.Count!=3)return new List<Hero>();
                // Native visiting vassals only, never an order to travel.
                var visitors=clan.Kingdom.Clans.Where(c=>c!=null && c!=clan)
                    .Select(c=>c.Leader).Where(LocalRoyalGuest).Distinct()
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal);
                foreach(var visitor in visitors)
                {
                    if(!result.Contains(visitor))result.Add(visitor);
                    if(!includeAll && result.Count>=4)break;
                }
                return result;
            }
            var candidates=new List<Hero>();
            if(player.Spouse!=null && player.Spouse.Spouse==player)candidates.Add(player.Spouse);
            candidates.Add(player.Father); candidates.Add(player.Mother);
            if(player.Children!=null)candidates.AddRange(player.Children);
            if(clan.Companions!=null)candidates.AddRange(clan.Companions.Where(h=>
                h!=null && h.Clan==clan && h.CompanionOf==clan));
            if(clan.Heroes!=null)candidates.AddRange(clan.Heroes);
            foreach(var person in candidates.Where(h=>h!=null).Distinct())
            {
                if(GroupParticipantPresent(person,parent))result.Add(person);
                if(!includeAll && result.Count>=4)break;
            }
            return result;
        }

        private bool GroupSessionValid()
        {
            if(groupParticipants.Count<2 || groupParticipants.Count>4 ||
                Hero.MainHero==null || Hero.MainHero.StringId!=groupPlayerId ||
                !GroupLocationValid(groupParent))return false;
            var seat=Settlement.CurrentSettlement;
            if((seat==null?"":seat.StringId)!=groupSeatId)return false;
            // Revalidate identity, membership and presence before each turn.
            var available=AvailableGroupParticipants(groupParent,true);
            return groupParticipants.All(person=>available.Contains(person));
        }

        private void ClearGroupSession()
        {
            groupParent="";groupPlayerId="";groupSeatId="";groupTopic="";
            groupOpening="";groupClosing="";groupOccasion="";groupSecondRound=false;groupRoundSerial=0;groupPlanned=false;
            groupParticipants.Clear();groupPreferences.Clear();
        }

        private void OpenGroupDiscussion(string parent)
        {
            var people=AvailableGroupParticipants(parent);
            if(people.Count<2)return;
            ClearGroupSession();
            groupParent=parent;groupPlayerId=Hero.MainHero.StringId;
            var seat=Settlement.CurrentSettlement;
            groupSeatId=seat==null?"":seat.StringId;
            groupParticipants.AddRange(people);
            GameMenu.SwitchToMenu("cu_group_topics");
        }

        private bool CanScheduleGathering(string occasion)
        {
            return groupParent!="cu_household_camp" && GroupOccasionAllowed(occasion) &&
                gatheringPlan.CanPlan(CurrentHouseholdDay());
        }
        private void ScheduleGathering(string occasion)
        {
            if(!CanScheduleGathering(occasion))return;
            var player=Hero.MainHero;
            var invited=groupParticipants.Select(h=>h.StringId).ToArray();
            var accepted=groupParticipants.Where(h=>SandboxGatheringPlan.AcceptInvitation(
                player.GetRelation(h),PersonalVoice(h),occasion)).Select(h=>h.StringId).ToArray();
            if(gatheringPlan.Schedule(player.StringId,groupParent,groupSeatId,occasion,
                CurrentHouseholdDay(),invited,accepted))
                ShowGatheringPlan();
        }
        private void ShowGatheringPlan()
        {
            if(Hero.MainHero==null || gatheringPlan.Owner!=Hero.MainHero.StringId)return;
            var p=gatheringPlan;
            var lines=new List<string>{p.Occasion+" | "+p.Status,
                "Meet on day "+p.Due+" through day "+(p.Due+3)+". No one is ordered to travel."};
            foreach(var id in p.Invited)
            {
                var hero=ResolveCampaignHero(id);
                lines.Add((hero==null?id:hero.Name.ToString())+": "+
                    (p.Accepted.Contains(id)?"modeled willingness to attend if present":"modeled decline of the invitation")+
                    (p.Status=="Held"?(p.Attended.Contains(id)?" | attended":" | did not attend"):""));
            }
            ShowNativeDetail("Gathering",String.Join("\n",lines));
        }
        private List<Hero> PlannedAttendees(string parent)
        {
            var player=Hero.MainHero;var seat=Settlement.CurrentSettlement;
            if(player==null || seat==null ||
                !gatheringPlan.IsDue(player.StringId,parent,seat.StringId,CurrentHouseholdDay()))
                return new List<Hero>();
            var people=AvailableGroupParticipants(parent,true).Where(h=>gatheringPlan.Accepted.Contains(h.StringId)).ToList();
            if(parent=="cu_native_royal_council" && (people.Count<3 || people.Count>4 ||
                !new[]{NativeCourtOfficer("Marshal"),NativeCourtOfficer("Royal Minister"),NativeCourtOfficer("Kingsguard")}
                    .All(h=>people.Contains(h))))return new List<Hero>();
            return people;
        }
        private void AttendPlannedGathering(string parent)
        {
            var people=PlannedAttendees(parent);
            if(people.Count<2)return;
            ClearGroupSession();
            groupParent=parent;groupPlayerId=Hero.MainHero.StringId;
            groupSeatId=Settlement.CurrentSettlement.StringId;
            groupParticipants.AddRange(people);groupPlanned=true;
            if(!GroupOccasionAllowed(gatheringPlan.Occasion))
            {
                ClearGroupSession();
                ShowNativeDetail("Gathering","Too few guests are present.");
                return;
            }
            BeginGroupOccasion(gatheringPlan.Occasion);
        }

        private void LeaveGroupDiscussion()
        {
            string parent=groupParent;
            bool valid=GroupLocationValid(parent);
            ClearGroupSession();
            if(valid)GameMenu.SwitchToMenu(parent);
            else GameMenu.ExitToLast();
        }

        private Hero RememberedFamilyMember()
        {
            var player=Hero.MainHero;
            if(player==null)return null;
            return new[]{player.Father,player.Mother}.Concat(
                player.Children==null?Enumerable.Empty<Hero>():player.Children)
                .FirstOrDefault(h=>h!=null && !h.IsAlive);
        }

        private bool GroupOccasionAllowed(string occasion)
        {
            if(!GroupSessionValid())return false;
            var player=Hero.MainHero;
            if(occasion=="FamilyGathering")
                return groupParticipants.Any(h=>h==player.Spouse || h==player.Father ||
                    h==player.Mother || h.Father==player || h.Mother==player);
            if(occasion=="MarriageCelebration")return groupParent!="cu_household_camp" &&
                player.Spouse!=null && player.Spouse.Spouse==player &&
                groupParticipants.Contains(player.Spouse);
            if(occasion=="Remembrance")return RememberedFamilyMember()!=null;
            if(occasion=="CourtAssembly")return groupParent=="cu_native_royal_council";
            return occasion=="Fellowship";
        }

        private void BeginGroupOccasion(string occasion)
        {
            if(!GroupOccasionAllowed(occasion))return;
            groupOccasion=occasion;
            BeginGroupTopic(occasion=="CourtAssembly"?"Relations":
                occasion=="Fellowship"?"Livelihood":"Family",true);
        }

        private string GroupOccasionOpening()
        {
            switch(groupOccasion)
            {
                case "FamilyGathering":return "You gather with members of your family to hear their concerns.";
                case "MarriageCelebration":return "You and your spouse gather with those present to speak of the life you share.";
                case "Remembrance":
                    var person=RememberedFamilyMember();
                    return person==null?"You pause to remember departed kin.":"The gathering pauses to remember "+person.Name+".";
                case "CourtAssembly":return "You invite your council to weigh the interests of the house.";
                case "Fellowship":return "You set aside the business of the day for conversation among companions.";
                default:return "";
            }
        }

        private void ShowGroupPeerStanding()
        {
            if(!GroupSessionValid())return;
            var lines=new List<string>();
            for(int i=0;i<groupParticipants.Count;i++)
                for(int j=i+1;j<groupParticipants.Count;j++)
                {
                    var a=groupParticipants[i];var b=groupParticipants[j];
                    var record=peerSocial.For(a.StringId,b.StringId);
                    int nativeRelation=a.GetRelation(b);
                    string standing=nativeRelation<=-20?"Strained relations":
                        nativeRelation>=30?"Good standing":"Standing uncertain";
                    if(record!=null)standing+=". "+SandboxPeerSocialState.Trend(record)+". Last recorded priorities: "+
                        (record.Interest=="CommonInterest"?"similar":"different")+
                        " (day "+record.LastDay+").";
                    lines.Add(a.Name+" and "+b.Name+": "+standing);
                }
            foreach(var person in groupParticipants)
            {
                string reputation=peerSocial.Reputation(person.StringId,CurrentHouseholdDay());
                if(reputation.Length>0)lines.Add(person.Name+": "+reputation+".");
            }
            ShowNativeDetail("Guests",String.Join("\n\n",lines));
        }

        private string GroupOccasionArgument(string priority,Hero speaker,int variant)
        {
            if(groupOccasion=="Remembrance" && variant==1)
                return priority=="Security"?"What they leave behind is a duty to those still living.":priority=="Welfare"?"Grief is not something we can settle for another person. Let each speak as they wish.":"Silence can honor the departed as well as words.";
            if(groupOccasion=="Remembrance" && variant==2)
                return priority=="Security"?"Let us carry forward the care we owe those who remain.":priority=="Welfare"?"Some will wish to speak and others will not. Both deserve our respect.":"We need no grand ceremony to hold a place for them in our thoughts.";
            if(groupOccasion=="Remembrance")
                return priority=="Security"?"Let their memory remind us to protect those still with us.":
                    priority=="Welfare"?"Let us make time for those who grieve, rather than pass over their loss.":
                    "We need not make grand promises in a moment of grief. Quiet remembrance is enough.";
            if(groupOccasion=="MarriageCelebration" && Hero.MainHero!=null && speaker==Hero.MainHero.Spouse && speaker.Spouse==Hero.MainHero)
                return variant==0?"Let us not forget the life we share apart from the duties of this house.":
                    variant==1?"I would rather we kept small promises to each other than made splendid ones before the company.":
                    "We have our own hopes for the years ahead. I wish us time to speak of them together.";
            if(groupOccasion=="MarriageCelebration" && variant==1)
                return priority=="Security"?"May you stand beside each other in hardship, not only in prosperity.":priority=="Welfare"?"Do not let the affairs of the house take every moment you have together.":"Trust grows through small promises kept over many years.";
            if(groupOccasion=="MarriageCelebration" && variant==2)
                return priority=="Security"?"The two of you must be able to rely on one another.":"Let the marriage be shaped by the wishes of those who share it.";
            if(groupOccasion=="MarriageCelebration")
                return priority=="Security"?"A household stands stronger when its members can depend on one another.":
                    priority=="Welfare"?"May the two of you find time for each other amid the duties of this house.":
                    "A life shared is built through patient judgment, not splendid promises.";
            if(groupOccasion=="Fellowship" && variant==1)
                return priority=="Security"?"It is good to know the people who share the road with us.":priority=="Welfare"?"Not every conversation need end in a charge or a request.":"For once, let our talk be worth more than what we spend on it.";
            if(groupOccasion=="Fellowship" && variant==2)
                return priority=="Security"?"A company is more than names upon a muster roll.":priority=="Welfare"?"Let us listen to each other without speaking of rank.":"A quiet evening has its own worth.";
            if(groupOccasion=="Fellowship")
                return priority=="Security"?"Let us keep faith with the people who stand beside us on the road.":
                    priority=="Welfare"?"There is value in sharing a little time without asking anything of one another.":
                    "Let us enjoy the peace we have without forgetting the needs of tomorrow.";
            return SandboxPetitionVoicePolicy.GroupNuance(groupTopic,priority,PersonalVoice(speaker),variant);
        }

        private string GroupContext(string topic)
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var seat=Settlement.CurrentSettlement;
            var party=MobileParty.MainParty;
            if(groupOccasion=="Remembrance" || groupOccasion=="MarriageCelebration" || groupOccasion=="Fellowship")return "";
            if(topic=="Defense")
            {
                var siege=clan.Settlements.FirstOrDefault(s=>s!=null &&
                    s.OwnerClan==clan && s.IsUnderSiege);
                string text=siege==null?"":siege.Name+" is under siege. ";
                if(seat!=null && seat.Town!=null && seat.Town.GarrisonParty!=null &&
                    seat.Town.GarrisonParty.MemberRoster!=null)
                    text+="The garrison here numbers "+seat.Town.GarrisonParty.MemberRoster.TotalManCount+". ";
                return text+"Your party numbers "+party.MemberRoster.TotalManCount+".";
            }
            if(topic=="Family")
            {
                int minors=player.Children==null?0:player.Children.Count(h=>h!=null &&
                    h.IsAlive && h.Age<18f);
                return minors>0?"There are "+minors+" young children in your immediate family to consider.":
                    "The gathering turns to the future of the household.";
            }
            if(topic=="Livelihood")return "The gathering considers how the house will provide for itself in the days ahead.";
            return "The gathering considers obligations to other houses and how best to deal with them.";
        }

        private string GroupPreferenceFor(Hero actor)
        {
            var player=Hero.MainHero;
            var agenda=characterAgendas.Get(actor.StringId);
            var clan=Clan.PlayerClan;
            bool danger=clan!=null && clan.Settlements.Any(x=>x!=null && x.OwnerClan==clan && x.IsUnderSiege);
            bool young=player.Children!=null && player.Children.Any(h=>h!=null && h.IsAlive && h.Age<18f);
            return SandboxPetitionVoicePolicy.GroupTopicPreference(groupTopic,EffectiveHouseholdDuty(actor),
                agenda==null?"":agenda.Goal,PersonalVoice(actor),player.GetRelation(actor),danger,young);
        }

        private void BeginGroupTopic(string topic,bool occasion=false)
        {
            if(!GroupSessionValid() || !SandboxPetitionVoicePolicy.GroupTopic(topic))return;
            if(!occasion)groupOccasion="";
            else if(!GroupOccasionAllowed(groupOccasion))return;
            groupTopic=topic;groupPreferences.Clear();groupClosing="";groupSecondRound=false;
            groupRoundSerial=(groupRoundSerial+1)%300;
            var lines=new List<string>();
            string opening=GroupOccasionOpening();
            if(opening.Length>0)lines.Add(opening);
            string context=GroupContext(topic);if(context.Length>0)lines.Add(context);
            for(int i=0;i<groupParticipants.Count;i++)
            {
                var speaker=groupParticipants[i];
                string preference=GroupPreferenceFor(speaker);
                string reply="";
                if(i>0)
                {
                    var previous=groupParticipants[i-1];
                    reply="To "+previous.Name+": "+SandboxPetitionVoicePolicy.GroupResponse(
                        preference,groupPreferences[i-1],speaker.GetRelation(previous),true);
                    var peer=peerSocial.For(speaker.StringId,previous.StringId);
                    if(peer!=null && peer.Meetings>=2 && peer.Interest=="DifferentPriorities" &&
                        preference!=groupPreferences[i-1])reply+="We have different priorities in this matter. ";
                }
                int variant=NextDialogueVariant(speaker,"Group:"+(groupOccasion.Length>0?groupOccasion:topic),3);
                reply+=GroupOccasionArgument(preference,speaker,variant);
                var memory=livingPersons.LatestGroupDiscussion(groupPlayerId,speaker.StringId);
                if(memory!=null)
                {
                    var fields=memory.Detail.Split('|');
                    bool sameOccasion=groupOccasion.Length==0?fields.Length==3:
                        fields.Length==4 && fields[3]=="occasion="+groupOccasion;
                    if(sameOccasion && fields[1]=="topic="+topic &&
                        fields[2]=="priority="+preference)
                        reply+=" At our earlier gathering you favored this priority.";
                    else if(sameOccasion && fields[1]=="topic="+topic &&
                        fields[2].StartsWith("priority=",StringComparison.Ordinal))
                        reply+=" At our earlier gathering you favored a different priority.";
                }
                lines.Add(speaker.Name+": "+reply);
                groupPreferences.Add(preference);
            }
            groupOpening=String.Join("\n\n",lines);
            GameMenu.SwitchToMenu("cu_group_exchange");
        }

        private bool GroupCanContinue()
        {
            return GroupSessionValid() && SandboxPetitionVoicePolicy.GroupTopic(groupTopic) &&
                (groupOccasion.Length==0 || GroupOccasionAllowed(groupOccasion)) &&
                groupPreferences.Count==groupParticipants.Count;
        }

        private void ChallengeGroup(bool practical)
        {
            if(!GroupCanContinue() || groupSecondRound)return;
            groupSecondRound=true;
            var lines=new List<string>{practical?
                "You: What would that mean in practice?":
                "You: Consider the other side before we settle this."};
            for(int i=0;i<groupParticipants.Count;i++)
            {
                var speaker=groupParticipants[i];
                string reply=groupOccasion=="Remembrance"?
                    (practical?"Let each of us speak of the departed in our own time. We need not settle grief with an order.":"Let those who mourn in another way be heard too."):
                    groupOccasion=="MarriageCelebration"?
                    (practical?"Make time for each other apart from the obligations of the house.":"A marriage is shared by two people. Let their own wishes be heard."):
                    groupOccasion=="Fellowship"?
                    (practical?"Let us spend this time together without a demand or a bargain.":"There is room for different views among companions."):
                    SandboxPetitionVoicePolicy.GroupChallenge(groupTopic,groupPreferences[i],
                        PersonalVoice(speaker),practical);
                if(!practical && i>0)
                {
                    var previous=groupParticipants[i-1];
                    reply="To "+previous.Name+": "+SandboxPetitionVoicePolicy.GroupResponse(
                        groupPreferences[i],groupPreferences[i-1],speaker.GetRelation(previous),true)+reply;
                }
                lines.Add(speaker.Name+": "+reply);
            }
            groupOpening=String.Join("\n\n",lines);
            GameMenu.SwitchToMenu("cu_group_reconsider");
        }

        private void AnswerGroup(string priority)
        {
            if(!GroupSessionValid() || (groupOccasion.Length>0 && !GroupOccasionAllowed(groupOccasion)) ||
                !SandboxPetitionVoicePolicy.GroupPriority(priority) ||
                !SandboxPetitionVoicePolicy.GroupTopic(groupTopic) ||
                groupPreferences.Count!=groupParticipants.Count)return;
            if(groupPlanned && (!gatheringPlan.IsDue(groupPlayerId,groupParent,groupSeatId,CurrentHouseholdDay()) ||
                gatheringPlan.Occasion!=groupOccasion || !gatheringPlan.Finish(groupPlayerId,CurrentHouseholdDay(),
                    groupParticipants.Select(h=>h.StringId).ToArray())))return;
            groupPlanned=false;
            // No native campaign mutation; this is a stated preference only.
            var lines=new List<string>{"You: "+GroupChoiceWords(priority)};
            for(int i=0;i<groupParticipants.Count;i++)
            {
                var speaker=groupParticipants[i];
                lines.Add(speaker.Name+": "+GroupOccasionDecisionReply(groupPreferences[i],priority,PersonalVoice(speaker)));
                livingPersons.RecordGroupDiscussion(groupPlayerId,speaker.StringId,
                    groupTopic,priority,CurrentHouseholdDay(),groupOccasion);
            }
            groupClosing=String.Join("\n\n",lines);
            groupTopic="";groupPreferences.Clear();
            GameMenu.SwitchToMenu("cu_group_reply");
        }

        private string GroupChoiceWords(string priority)
        {
            if(groupOccasion=="Remembrance")return priority=="Security"?"Let their memory guide our care for those still with us.":
                priority=="Welfare"?"Let us hear those who grieve.":"Let us remember them quietly, without grand promises.";
            if(groupOccasion=="MarriageCelebration")return priority=="Security"?"Let us keep faith with one another through hardship.":
                priority=="Welfare"?"Let us make time for the life we share.":"Let us build our life patiently, one day at a time.";
            if(groupOccasion=="Fellowship")return priority=="Security"?"Let us stand by one another on the road.":
                priority=="Welfare"?"Let us enjoy this time together.":"Let us spend this evening simply, without demands.";
            return priority=="Security"?"Our first concern must be security.":
                priority=="Welfare"?"We must put the welfare of those who depend on us first.":
                "We should keep our resources in reserve until we know more.";
        }

        private string GroupOccasionDecisionReply(string preference,string chosen,string tone)
        {
            if(groupOccasion=="Remembrance")return preference==chosen?
                "That is a fitting way to remember them.":tone=="Guarded"?
                "I shall remember them in my own way.":"Let each of us honor their memory as we can.";
            if(groupOccasion=="MarriageCelebration")return preference==chosen?
                "May those words be borne out in the years ahead.":"There are many ways to keep a marriage strong. I wish you both well.";
            if(groupOccasion=="Fellowship")return preference==chosen?
                "Gladly. Let us set our business aside for a time.":"I can agree to spend this time together, though our views differ.";
            return SandboxPetitionVoicePolicy.GroupDecisionReply(preference,chosen,tone);
        }

        private void SetGroupChoiceVariables()
        {
            foreach(string priority in new[]{"Security","Welfare","Reserve"})
                MBTextManager.SetTextVariable("CU_GROUP_CHOICE_"+priority,new TextObject(GroupChoiceWords(priority)));
        }

        private void RegisterGroupDiscussionMenus(CampaignGameStarter starter)
        {
            if(starter==null)return;
            foreach(var source in new[]{"cu_people_lineage","cu_household_camp","cu_native_royal_council"})
            {
                string parent=source;
                starter.AddGameMenuOption(parent,"cu_group_entry_"+parent,
                    parent=="cu_native_royal_council"?"Hear the council's advice":"Gather for a discussion",
                    args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                        return AvailableGroupParticipants(parent).Count>=2;},
                    args=>OpenGroupDiscussion(parent),false);
                if(parent!="cu_household_camp")
                {
                    starter.AddGameMenuOption(parent,"cu_group_plan_view_"+parent,"Gathering plans",
                        args=>{args.optionLeaveType=GameMenuOption.LeaveType.Submenu;return Hero.MainHero!=null && gatheringPlan.Owner==Hero.MainHero.StringId && gatheringPlan.Parent==parent;},
                        args=>ShowGatheringPlan(),false);
                    starter.AddGameMenuOption(parent,"cu_group_plan_attend_"+parent,"Attend gathering",
                        args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;return PlannedAttendees(parent).Count>=2;},
                        args=>AttendPlannedGathering(parent),false);
                    starter.AddGameMenuOption(parent,"cu_group_plan_cancel_"+parent,"Cancel gathering",
                        args=>Hero.MainHero!=null && gatheringPlan.Owner==Hero.MainHero.StringId && gatheringPlan.Parent==parent && gatheringPlan.Status=="Pending",
                        args=>{if(Hero.MainHero!=null)gatheringPlan.Cancel(Hero.MainHero.StringId,CurrentHouseholdDay());},false);
                }
            }
            starter.AddGameMenu("cu_group_topics","{CU_GROUP_TOPICS}",args=>
            {
                string text=GroupSessionValid()?"Guests: "+String.Join(", ",
                    groupParticipants.Select(h=>h.Name.ToString()))+". What shall we discuss?":
                    "The gathering can no longer continue.";
                MBTextManager.SetTextVariable("CU_GROUP_TOPICS",new TextObject(text));
            });
            starter.AddGameMenuOption("cu_group_topics","cu_group_peer_standing",
                "Relations among guests",
                args=>{args.optionLeaveType=GameMenuOption.LeaveType.Submenu;
                    return GroupSessionValid();},
                args=>ShowGroupPeerStanding(),false);
            foreach(var name in new[]{"FamilyGathering","Fellowship","MarriageCelebration","Remembrance","CourtAssembly"})
            {
                string occasion=name;
                string words=occasion=="FamilyGathering"?"Gather with the family":
                    occasion=="Fellowship"?"Spend time in fellowship":
                    occasion=="MarriageCelebration"?"Celebrate our marriage together":
                    occasion=="Remembrance"?"Remember our departed kin":"Convene an assembly of the court";
                starter.AddGameMenuOption("cu_group_topics","cu_group_occasion_"+occasion,words,
                    args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                        return GroupOccasionAllowed(occasion);},
                    args=>BeginGroupOccasion(occasion),false);
                starter.AddGameMenuOption("cu_group_topics","cu_group_schedule_"+occasion,"Arrange: "+words+" (in two days)",
                    args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;return CanScheduleGathering(occasion);},
                    args=>ScheduleGathering(occasion),false);
            }
            foreach(var subject in new[]{"Family","Defense","Livelihood","Relations"})
            {
                string topic=subject;
                starter.AddGameMenuOption("cu_group_topics","cu_group_topic_"+topic,
                    topic=="Family"?"The future of our household":topic=="Defense"?
                    "Our defenses and safety":topic=="Livelihood"?
                    "Our livelihood and obligations":"Our dealings with other houses",
                    args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                        return GroupSessionValid();},
                    args=>BeginGroupTopic(topic),false);
            }
            starter.AddGameMenu("cu_group_exchange","{CU_GROUP_EXCHANGE}",args=>{
                SetGroupChoiceVariables();
                MBTextManager.SetTextVariable("CU_GROUP_EXCHANGE",new TextObject(
                    GroupCanContinue()?groupOpening:"The gathering can no longer continue."));});
            starter.AddGameMenu("cu_group_reconsider","{CU_GROUP_EXCHANGE}",args=>{
                SetGroupChoiceVariables();
                MBTextManager.SetTextVariable("CU_GROUP_EXCHANGE",new TextObject(
                    GroupCanContinue()?groupOpening:"The gathering can no longer continue."));});
            starter.AddGameMenuOption("cu_group_exchange","cu_group_practical","What would that mean in practice?",
                args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                    return GroupCanContinue() && !groupSecondRound;},args=>ChallengeGroup(true),false);
            starter.AddGameMenuOption("cu_group_exchange","cu_group_challenge","Consider the other side before we settle this.",
                args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                    return GroupCanContinue() && !groupSecondRound;},args=>ChallengeGroup(false),false);
            foreach(var source in new[]{"cu_group_exchange","cu_group_reconsider"})
            {
                string menu=source;
                foreach(var value in new[]{"Security","Welfare","Reserve"})
                {
                    string priority=value;
                    starter.AddGameMenuOption(menu,"cu_group_choose_"+menu+"_"+priority,
                        "{CU_GROUP_CHOICE_"+priority+"}",args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                            return GroupCanContinue();},args=>AnswerGroup(priority),false);
                }
            }
            starter.AddGameMenuOption("cu_group_exchange","cu_group_defer","Let us leave this matter undecided.",
                args=>GroupSessionValid() && !groupPlanned,args=>{groupTopic="";groupPreferences.Clear();
                    GameMenu.SwitchToMenu("cu_group_topics");},false);
            starter.AddGameMenu("cu_group_reply","{CU_GROUP_REPLY}",args=>
                MBTextManager.SetTextVariable("CU_GROUP_REPLY",new TextObject(
                    GroupSessionValid()?groupClosing:"The gathering can no longer continue.")));
            starter.AddGameMenuOption("cu_group_reply","cu_group_another","Let us speak of another matter.",
                args=>GroupSessionValid(),args=>GameMenu.SwitchToMenu("cu_group_topics"),false);
            foreach(var source in new[]{"cu_group_topics","cu_group_exchange","cu_group_reconsider","cu_group_reply"})
            {
                string menu=source;
                starter.AddGameMenuOption(menu,"cu_group_leave_"+menu,"That is all for now.",
                    args=>true,args=>LeaveGroupDiscussion(),false);
            }
        }
    }
}
