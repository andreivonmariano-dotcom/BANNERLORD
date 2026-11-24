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
        private string groupOpening="",groupClosing="";
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

        private List<Hero> AvailableGroupParticipants(string parent)
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
                return result.Count==3?result:new List<Hero>();
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
                if(result.Count>=4)break;
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
            var available=AvailableGroupParticipants(groupParent);
            return groupParticipants.All(person=>available.Contains(person));
        }

        private void ClearGroupSession()
        {
            groupParent="";groupPlayerId="";groupSeatId="";groupTopic="";
            groupOpening="";groupClosing="";
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

        private void LeaveGroupDiscussion()
        {
            string parent=groupParent;
            bool valid=GroupLocationValid(parent);
            ClearGroupSession();
            if(valid)GameMenu.SwitchToMenu(parent);
            else GameMenu.ExitToLast();
        }

        private string GroupContext(string topic)
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var seat=Settlement.CurrentSettlement;
            var party=MobileParty.MainParty;
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
            return SandboxPetitionVoicePolicy.GroupPreference(EffectiveHouseholdDuty(actor),
                agenda==null?"":agenda.Goal,PersonalVoice(actor),player.GetRelation(actor));
        }

        private void BeginGroupTopic(string topic)
        {
            if(!GroupSessionValid() || !SandboxPetitionVoicePolicy.GroupTopic(topic))return;
            groupTopic=topic;groupPreferences.Clear();groupClosing="";
            var lines=new List<string>{GroupContext(topic)};
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
                }
                reply+=SandboxPetitionVoicePolicy.GroupArgument(topic,preference);
                var memory=livingPersons.LatestGroupDiscussion(groupPlayerId,speaker.StringId);
                if(memory!=null)
                {
                    var fields=memory.Detail.Split('|');
                    if(fields.Length==3 && fields[1]=="topic="+topic &&
                        fields[2]=="priority="+preference)
                        reply+=" At our earlier gathering you favored this priority.";
                    else if(fields.Length==3 && fields[1]=="topic="+topic &&
                        fields[2].StartsWith("priority=",StringComparison.Ordinal))
                        reply+=" At our earlier gathering you favored a different priority.";
                }
                lines.Add(speaker.Name+": "+reply);
                groupPreferences.Add(preference);
            }
            groupOpening=String.Join("\n\n",lines);
            GameMenu.SwitchToMenu("cu_group_exchange");
        }

        private void AnswerGroup(string priority)
        {
            if(!GroupSessionValid() || !SandboxPetitionVoicePolicy.GroupPriority(priority) ||
                !SandboxPetitionVoicePolicy.GroupTopic(groupTopic) ||
                groupPreferences.Count!=groupParticipants.Count)return;
            // No native campaign mutation; this is a stated preference only.
            var lines=new List<string>{"You: "+GroupChoiceWords(priority)};
            for(int i=0;i<groupParticipants.Count;i++)
            {
                var speaker=groupParticipants[i];
                lines.Add(speaker.Name+": "+SandboxPetitionVoicePolicy.GroupDecisionReply(
                    groupPreferences[i],priority,PersonalVoice(speaker)));
                livingPersons.RecordGroupDiscussion(groupPlayerId,speaker.StringId,
                    groupTopic,priority,CurrentHouseholdDay());
            }
            groupClosing=String.Join("\n\n",lines);
            groupTopic="";groupPreferences.Clear();
            GameMenu.SwitchToMenu("cu_group_reply");
        }

        private static string GroupChoiceWords(string priority)
        {
            return priority=="Security"?"Our first concern must be security.":
                priority=="Welfare"?"We must put the welfare of those who depend on us first.":
                "We should keep our resources in reserve until we know more.";
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
            }
            starter.AddGameMenu("cu_group_topics","{CU_GROUP_TOPICS}",args=>
            {
                string text=GroupSessionValid()?"Those gathered: "+String.Join(", ",
                    groupParticipants.Select(h=>h.Name.ToString()))+". What shall we discuss?":
                    "The gathering can no longer continue.";
                MBTextManager.SetTextVariable("CU_GROUP_TOPICS",new TextObject(text));
            });
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
            starter.AddGameMenu("cu_group_exchange","{CU_GROUP_EXCHANGE}",args=>
                MBTextManager.SetTextVariable("CU_GROUP_EXCHANGE",new TextObject(
                    GroupSessionValid()?groupOpening:"The gathering can no longer continue.")));
            foreach(var value in new[]{"Security","Welfare","Reserve"})
            {
                string priority=value;
                starter.AddGameMenuOption("cu_group_exchange","cu_group_choose_"+priority,
                    GroupChoiceWords(priority),args=>{args.optionLeaveType=GameMenuOption.LeaveType.Conversation;
                        return GroupSessionValid() && SandboxPetitionVoicePolicy.GroupTopic(groupTopic);},
                    args=>AnswerGroup(priority),false);
            }
            starter.AddGameMenuOption("cu_group_exchange","cu_group_defer","Let us leave this matter undecided.",
                args=>GroupSessionValid(),args=>{groupTopic="";groupPreferences.Clear();
                    GameMenu.SwitchToMenu("cu_group_topics");},false);
            starter.AddGameMenu("cu_group_reply","{CU_GROUP_REPLY}",args=>
                MBTextManager.SetTextVariable("CU_GROUP_REPLY",new TextObject(
                    GroupSessionValid()?groupClosing:"The gathering can no longer continue.")));
            starter.AddGameMenuOption("cu_group_reply","cu_group_another","Let us speak of another matter.",
                args=>GroupSessionValid(),args=>GameMenu.SwitchToMenu("cu_group_topics"),false);
            foreach(var source in new[]{"cu_group_topics","cu_group_exchange","cu_group_reply"})
            {
                string menu=source;
                starter.AddGameMenuOption(menu,"cu_group_leave_"+menu,"That is all for now.",
                    args=>true,args=>LeaveGroupDiscussion(),false);
            }
        }
    }
}
