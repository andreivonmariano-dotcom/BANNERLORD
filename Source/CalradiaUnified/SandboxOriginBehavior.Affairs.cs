using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private static SandboxOriginBehavior AffairsBehavior()
        {
            var c=Campaign.Current;
            if(c==null)return null;
            var b=c.GetCampaignBehavior<SandboxOriginBehavior>();
            return b!=null && b.applied && IsStandaloneSandbox() &&
                Hero.MainHero!=null && Hero.MainHero.IsAlive &&
                Clan.PlayerClan!=null?b:null;
        }
        // This count reads only bounded existing state, not a world Hero scan.
        public static int PendingAffairsCount()
        {
            var b=AffairsBehavior();
            return b==null?0:b.CountPendingAffairs();
        }
        public static int UnreadAffairsCount()
        {
            var b=AffairsBehavior();
            return b==null?0:b.characterAgendas.Entries.Count(e=>
                b.characterAgendas.IsNewsUnread(e.Id,Hero.MainHero.StringId,e.NewsDay));
        }
        private bool RoyalAudienceAvailableToday()
        {
            return royalCouncil.IsRecorded && royalCouncil.LastMeetingDay<CurrentLordInheritanceDay() &&
                NativeRoyalCouncilPresent(true);
        }
        private int affairFollowUpCacheDay=-1;
        private HashSet<string> livingFollowUpPeople=new HashSet<string>(StringComparer.Ordinal);
        private bool LivingFollowUp(string id,int day)
        {
            // At most one bounded identity observation per campaign day.
            // A newly resolved petition cannot become due until seven days pass.
            if(affairFollowUpCacheDay!=day)
            {
                affairFollowUpCacheDay=day;
                var dueIds=new HashSet<string>(petitionStories.Stories
                    .Where(s=>petitionStories.CanFollowUp(s.HeroId,day)).Select(s=>s.HeroId),
                    StringComparer.Ordinal);
                livingFollowUpPeople=new HashSet<string>(dueIds.Count==0?new string[0]:
                    Hero.AllAliveHeroes.Where(h=>h!=null && h.IsAlive && dueIds.Contains(h.StringId))
                        .Select(h=>h.StringId),StringComparer.Ordinal);
            }
            return livingFollowUpPeople.Contains(id);
        }
        private bool OpenAffairsClaim()
        {
            return successionInterest.IsRecorded && Hero.MainHero!=null &&
                successionInterest.ClaimantId==Hero.MainHero.StringId &&
                (inheritanceNegotiation.Status=="ClaimContested" || inheritanceNegotiation.Authorized);
        }
        private bool PendingAffairsGathering(int day)
        {
            return Hero.MainHero!=null && gatheringPlan.Owner==Hero.MainHero.StringId &&
                gatheringPlan.Status=="Pending" && day>=gatheringPlan.Created && day<=gatheringPlan.Due+3;
        }
        private int CountPendingAffairs()
        {
            int day=CurrentHouseholdDay();
            var player=Hero.MainHero;
            int count=PetitionsBelongToCurrentPlayer()?agencyDecisions.PendingCount:0;
            count+=petitionStories.Stories.Count(s=>petitionStories.CanFollowUp(s.HeroId,day) &&
                LivingFollowUp(s.HeroId,day));
            bool oath=knightOath.HasCurrentOath && knightOath.PlayerId==player.StringId;
            if(oath && knightServiceOrders.Active &&
                knightServiceOrders.PatronId==knightOath.PatronId)count++;
            if(oath && knightOath.CanMuster(day,true))count++;
            if(oath && Clan.PlayerClan.Kingdom==null &&
                knightServiceOrders.PatronId==knightOath.PatronId &&
                knightServiceOrders.CanHearKing(CurrentOathDay()))count++;
            if(oath && knightEscort.IsActive && knightEscort.PatronId==knightOath.PatronId)count++;
            if(PendingAffairsGathering(day))count++;
            if(RoyalAudienceAvailableToday())count++;
            var clan=Clan.PlayerClan;
            if(clan.Settlements!=null)
                count+=clan.Settlements.Count(x=>x!=null && x.OwnerClan==clan && x.IsUnderSiege);
            return count;
        }
        private bool CanReadAffairs(string expectedOwner)
        {
            return AffairsBehavior()==this && Hero.MainHero.StringId==expectedOwner &&
                !Hero.MainHero.IsPrisoner && MobileParty.MainParty!=null &&
                MobileParty.MainParty.MapEvent==null && TaleWorlds.MountAndBlade.Mission.Current==null;
        }
        public static bool OpenCampaignAffairs()
        {
            var b=AffairsBehavior();
            if(b==null || !b.CanReadAffairs(Hero.MainHero.StringId))return false;
            try
            {
                // Live War Sails log terminated after CUAffairsScreen resumed.
                // Keep the normal route on native inquiries. Only a deliberate
                // process-local map trial may open the diagnostic movie.
                var all=b.BuildAffairs().Where(x=>x!=null).GroupBy(x=>x.Id,StringComparer.Ordinal)
                    .Select(g=>g.First()).OrderByDescending(x=>x.Pending)
                    .ThenByDescending(x=>x.Unread).ThenByDescending(x=>x.Day).ThenBy(x=>x.Id,StringComparer.Ordinal).ToArray();
                if(CUAffairsScreen.TryOpenDiagnosticProbe(all.ToList()))return true;
                DiagnosticJournal.Event("cu_campaign_affairs_native_open","records="+all.Length+
                    "|pending="+all.Count(x=>x.Pending)+"|custom_movie=false");
                b.ShowAffairsNativePage(all,0);
                return true;
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_affairs_native_failed",error);
                return false;
            }
        }
        // Optional game-owned list from a settlement; the known inquiry route
        // stays available from all original buttons during live crash recovery.
        private void OpenNativeAffairsList()
        {
            if(AffairsBehavior()!=this || Hero.MainHero.IsPrisoner ||
                MobileParty.MainParty==null || MobileParty.MainParty.MapEvent!=null)return;
            string owner=Hero.MainHero.StringId;
            try
            {
                var all=BuildAffairs().Where(x=>x!=null).GroupBy(x=>x.Id,StringComparer.Ordinal)
                    .Select(g=>g.First()).OrderByDescending(x=>x.Pending).ThenByDescending(x=>x.Unread)
                    .ThenByDescending(x=>x.Day).ThenBy(x=>x.Id,StringComparer.Ordinal).ToArray();
                if(all.Length==0){ShowAffairsNativePage(all,0);return;}
                var entries=all.Select((item,index)=>new InquiryElement(index,
                    item.Title+" | "+item.Status+(item.Day>=0?" | Day "+item.Day:""),null)).ToList();
                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Correspondence","Select a letter or request.",
                    entries,true,1,1,"Read","Close",selected=>
                    {
                        if(!CanReadAffairs(owner) || selected==null ||
                            selected.Count!=1 || !(selected[0].Identifier is int))return;
                        ShowAffairsNativePage(all,(int)selected[0].Identifier);
                    },selected=>{}),true);
                DiagnosticJournal.Event("cu_affairs_native_list_open","records="+all.Length+"|custom_movie=false|images=false");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_affairs_native_list_failed",error);
                OpenCampaignAffairs();
            }
        }
        private void ShowAffairsNativePage(SandboxAffair[] records,int index)
        {
            if(AffairsBehavior()!=this)return;
            string owner=Hero.MainHero.StringId;
            if(!CanReadAffairs(owner))return;
            if(records.Length==0)
            {
                ShowNativeDetail("Correspondence","No correspondence.");
                return;
            }
            if(index<0 || index>=records.Length)return;
            var item=records[index];
            bool more=index+1<records.Length;
            string title="Correspondence | "+(index+1)+" / "+records.Length;
            string body=item.Category+" | "+item.Title+"\n"+item.Status+
                (item.Pending?" | Attention required":"")+
                (item.Day>=0?" | Day "+item.Day:"")+"\n\n"+item.Detail;
            bool unread=item.Unread && characterAgendas.IsNewsUnread(item.HeroId,Hero.MainHero.StringId,item.Day);
            InformationManager.ShowInquiry(new InquiryData(title,body,true,more || unread,
                "Close",unread?(more?"Mark read & next":"Mark read"):(more?"Next matter":null),
                ()=>{},(more || unread)?(Action)(()=>
                {
                    if(!CanReadAffairs(owner))return;
                    if(unread && characterAgendas.MarkNewsRead(item.HeroId,owner,item.Day))
                        DiagnosticJournal.Event("cu_correspondence_read","sender="+item.HeroId+"|recipient="+Hero.MainHero.StringId+"|day="+item.Day);
                    if(more)ShowAffairsNativePage(records,index+1);
                }):null),true);
        }
        internal static Hero ResolveAffairHero(string id)
        {
            return String.IsNullOrEmpty(id)?null:ResolveCampaignHero(id);
        }
        // A distant person uses the native last-known settlement, never secret live party coordinates.
        internal static Settlement ResolveAffairPlace(SandboxAffair affair)
        {
            if(affair==null)return null;
            if(!String.IsNullOrEmpty(affair.SettlementId))return Settlement.Find(affair.SettlementId);
            var person=ResolveAffairHero(affair.HeroId);
            if(person==null || !person.IsAlive)return null;
            return SamePlace(Hero.MainHero,person)?(person.StayingInSettlement ?? person.CurrentSettlement ?? person.LastKnownClosestSettlement):person.LastKnownClosestSettlement;
        }
        private static string AffairLocation(Hero person)
        {
            if(person==null || !person.IsAlive)return "Person no longer available.";
            if(person.IsPrisoner)return "Currently held prisoner.";
            if(!person.IsActive)return "Not currently available.";
            if(SamePlace(Hero.MainHero,person))return "Nearby. Speak with them in person.";
            var place=person.LastKnownClosestSettlement;
            if(place!=null)return "Last known near "+place.Name+".";
            return "Present whereabouts unknown.";
        }
        private static string AffairPerson(Hero person)
        {
            return person==null?"Unidentified person":person.Name.ToString();
        }
        private static string PetitionConcern(string kind)
        {
            if(SandboxAgencyDecisionPolicy.IsAudience(kind))return SandboxAgencyDecisionPolicy.AudienceConcern(kind);
            if(kind=="Recognition")return "Wishes their service to be acknowledged.";
            int amount=SandboxAgencyDecisionPolicy.Amount(kind);
            return amount>0?"Asks for "+amount+" denars of support.":"Wishes to discuss a private matter.";
        }
        private List<SandboxAffair> BuildAffairs()
        {
            var rows=new List<SandboxAffair>();
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            int day=CurrentHouseholdDay();
            var ids=new HashSet<string>(petitionStories.Stories.Select(s=>s.HeroId),StringComparer.Ordinal);
            foreach(var news in characterAgendas.Entries.Where(e=>e.NewsRecipient==player.StringId && e.NewsDay>=0))
                ids.Add(news.Id);
            if(PetitionsBelongToCurrentPlayer())foreach(var request in agencyDecisions.PendingRequests)ids.Add(request.HeroId);
            if(knightOath.HasCurrentOath)ids.Add(knightOath.PatronId);
            foreach(var office in new[]{"Steward","Captain","Envoy"})
                ids.Add(householdDuties.AssignedId(office));
            if(inheritanceNegotiation.LeaderId.Length>0)ids.Add(inheritanceNegotiation.LeaderId);
            // Resolve current identities once, only on opening the journal.
            var known=Hero.AllAliveHeroes.Where(h=>h!=null && ids.Contains(h.StringId))
                .GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
            Func<string,Hero> find=id=>!String.IsNullOrEmpty(id) && known.ContainsKey(id)?known[id]:null;
            foreach(var request in PetitionsBelongToCurrentPlayer()?agencyDecisions.PendingRequests:new SandboxAgencyDecisionState.PendingRequest[0])
            {
                var p=find(request.HeroId);
                var pendingStory=petitionStories.For(request.HeroId);
                rows.Add(new SandboxAffair{Id="request:"+request.HeroId,
                    Category="Letters",HeroId=request.HeroId,Title=AffairPerson(p)+" | "+SandboxAgencyDecisionPolicy.Category(request.Kind)+" petition",
                    Status=p==null?"Person unavailable":"Awaiting your answer",Pending=true,
                    Day=pendingStory!=null && pendingStory.Kind==request.Kind?
                        pendingStory.StartedDay:Math.Max(0,day-(agencyDecisions.Day-request.OpenedDay)),
                    Detail=(pendingStory!=null && pendingStory.Letter.Length>0?pendingStory.Letter:
                        PetitionConcern(request.Kind))+"\n\n"+
                        AffairLocation(p)+"\nReply within "+Math.Max(0,SandboxAgencyDecisionPolicy.ExpiryDays-(agencyDecisions.Day-request.OpenedDay))+
                        " days. Reply in person."});
            }
            foreach(var story in petitionStories.Stories)
            {
                if(story.Outcome=="Pending" && agencyDecisions.PendingFor(story.HeroId)!=null)continue;
                var p=find(story.HeroId);
                bool due=p!=null && p.IsAlive &&
                    petitionStories.CanFollowUp(story.HeroId,day);
                bool supported=story.Outcome=="Supported";
                string status=due?"Conversation due":story.Outcome=="Pending"?
                    "No longer awaiting a reply":story.Outcome=="Expired"?"Reply period ended":
                    story.Outcome=="Unavailable"?"Person unavailable":
                    story.Outcome=="Unconfirmed"?"Outcome not confirmed":supported?
                    (SandboxAgencyDecisionPolicy.IsAudience(story.Kind)?"Audience heard":"Help given"):"Request declined";
                if(story.FollowUpDay>=0)status+=" | Revisited";
                rows.Add(new SandboxAffair{Id="story:"+story.HeroId,Category="Letters",HeroId=story.HeroId,
                    Title=AffairPerson(p)+(due?" | Follow-up":" | Earlier matter"),
                    Status=p==null?status+" | Person unavailable":status,Pending=due,Day=story.ResolvedDay>=0?story.ResolvedDay:story.StartedDay,
                    Completed=story.Outcome=="Supported" || story.Outcome=="Declined",
                    Detail=(story.Letter.Length>0?story.Letter:PetitionConcern(story.Kind))+"\n\n"+AffairLocation(p)+"\n"+
                        (due?"Ask how they have fared.":
                         "Past matter.")});
            }
            foreach(var news in characterAgendas.Entries.Where(e=>e.NewsRecipient==player.StringId &&
                e.NewsDay>=0 && e.NewsText.Length>0))
            {
                var sender=find(news.Id);
                var historical=livingPersons.People.FirstOrDefault(x=>x.Id==news.Id);
                string name=sender!=null?AffairPerson(sender):historical==null?"Recorded correspondent":historical.Name;
                rows.Add(new SandboxAffair{Id="news:"+news.Id,Category="Letters",HeroId=news.Id,
                    Title=name+" | Personal letter",Status=characterAgendas.IsNewsUnread(news.Id,player.StringId,news.NewsDay)?
                        "Unread | No reply required":"Read | No reply required",Day=news.NewsDay,
                    Pending=false,Unread=characterAgendas.IsNewsUnread(news.Id,player.StringId,news.NewsDay),Detail=news.NewsText+"\n\n"+AffairLocation(sender)});
            }
            if(IsPersonalChargeActive())
                rows.Add(new SandboxAffair{Id="charge",Category="Service",Title="Your agreed duty",
                    Status="Legacy personal goal",Pending=false,
                    Day=careerCharge.StartedDay,Detail=ChargeDescription()+
                        "\nOptional old goal. Set it aside in Family and records > Career history."});
            bool oath=knightOath.HasCurrentOath && knightOath.PlayerId==player.StringId;
            if(oath)
            {
                var patron=find(knightOath.PatronId);
                string location=AffairLocation(patron);
                if(knightServiceOrders.Active && knightServiceOrders.PatronId==knightOath.PatronId)
                    rows.Add(new SandboxAffair{Id="knight-order",Category="Service",
                        HeroId=knightOath.PatronId,Title="Duty for "+AffairPerson(patron),Status="Due day "+knightServiceOrders.DueDay,
                        Pending=true,Day=knightServiceOrders.StartDay,
                        Detail=(knightServiceOrders.Order=="Scout"?"Survey two towns of the realm.":"Ready your sworn fighters.")+
                            "\n"+location+"\nReturn to your patron to report."});
                if(knightOath.CanMuster(day,true))
                    rows.Add(new SandboxAffair{Id="muster",Category="Service",
                        HeroId=knightOath.PatronId,Title="Audience with "+AffairPerson(patron),Status="Muster due",
                        Pending=true,Day=knightOath.NextMusterDay,Detail=location+
                            "\nMeet your sworn lord to review your service."});
                if(knightEscort.IsActive && knightEscort.PatronId==knightOath.PatronId)
                    rows.Add(new SandboxAffair{Id="knight-escort",Category="Service",HeroId=knightOath.PatronId,
                        Title="Escort "+AffairPerson(patron),Status="Field escort",
                        Pending=true,Day=knightEscort.StartDay,Detail="Keep company with your sworn lord on the campaign map. "+
                        knightEscort.ConsecutiveDays+" of "+SandboxKnightEscortState.RequiredDays+" consecutive days recorded.\n"+location});
                if(clan.Kingdom==null && knightServiceOrders.PatronId==knightOath.PatronId &&
                    knightServiceOrders.CanHearKing(CurrentOathDay()))
                {
                    var kingdom=patron==null || patron.Clan==null?null:patron.Clan.Kingdom;
                    var ruler=kingdom==null || kingdom.RulingClan==null?null:kingdom.RulingClan.Leader;
                    rows.Add(new SandboxAffair{Id="royal-audience",Category="Service",Title="Audience with the ruler",
                        HeroId=ruler==null?"":ruler.StringId,
                        Status="Recommendation recorded",Pending=true,Day=knightServiceOrders.RecommendationDay,
                        Detail="Seek an audience with your patron's ruler.\n"+AffairLocation(ruler)});
                }
            }
            if(RoyalAudienceAvailableToday())
                rows.Add(new SandboxAffair{Id="royal-council-today",Category="Household",
                    SettlementId=royalCouncil.SeatId,Title="Royal court available",
                    Status="Optional audience today",Pending=true,Day=day,
                    Detail="Your court is ready to meet."});
            if(gatheringPlan.Owner==player.StringId && gatheringPlan.Status.Length>0)
            {
                var place=Settlement.Find(gatheringPlan.Seat);
                bool pending=PendingAffairsGathering(day);
                rows.Add(new SandboxAffair{Id="gathering",Category="Household",SettlementId=gatheringPlan.Seat,
                    Title="Arranged gathering",Status=pending?(day<gatheringPlan.Due?"Scheduled":"Attendance window open"):
                        gatheringPlan.Status=="Pending"?"Attendance window ended":gatheringPlan.Status,
                    Pending=pending,Completed=gatheringPlan.Status=="Held",Day=gatheringPlan.Created,
                    Detail="Meeting at "+(place==null?"the recorded holding":place.Name.ToString())+
                        ". Due day "+gatheringPlan.Due+"; attendance window closes after day "+(gatheringPlan.Due+3)+
                        ". "+gatheringPlan.Accepted.Length+" accepted of "+gatheringPlan.Invited.Length+" invited. "+
                        "Attend the gathering at your holding."});
            }
            if(OpenAffairsClaim())
            {
                var estate=String.IsNullOrEmpty(inheritanceNegotiation.SettlementId)?null:
                    Settlement.Find(inheritanceNegotiation.SettlementId);
                rows.Add(new SandboxAffair{Id="inheritance",Category="Household",HeroId=inheritanceNegotiation.LeaderId,SettlementId=inheritanceNegotiation.SettlementId,Title="Family estate claim",
                    Status="Historical claim (native ownership only)",
                    Pending=false,Day=inheritanceNegotiation.PetitionDay,Detail=
                        (estate==null?"The claim remains unresolved.":"Claim concerning "+estate.Name+".")+
                        "\nCU estate negotiation is retired. Use native political processes."});
            }
            if(clan.Settlements!=null)
                foreach(var estate in clan.Settlements.Where(s=>s!=null && s.OwnerClan==clan))
                {
                    var garrison=estate.Town==null?null:estate.Town.GarrisonParty;
                    rows.Add(new SandboxAffair{Id="estate:"+estate.StringId,Category="Household",
                        SettlementId=estate.StringId,Title=estate.Name.ToString(),Status=estate.IsUnderSiege?"Under siege":"Holding",
                        Pending=estate.IsUnderSiege,Day=estate.IsUnderSiege?day:-1,
                        Detail=(garrison==null || garrison.MemberRoster==null?"Garrison not confirmed.":
                            "Garrison: "+garrison.MemberRoster.TotalManCount+" soldiers.")+
                            "\nManage the garrison at the settlement."});
                }
            if(clan.Companions!=null)
                foreach(var officer in clan.Companions.Where(h=>h!=null && h.IsAlive && h.Clan==clan && h.CompanionOf==clan))
                {
                    string office=EffectiveHouseholdDuty(officer);
                    if(office.Length==0)continue;
                    rows.Add(new SandboxAffair{Id="officer:"+officer.StringId,Category="Household",
                        HeroId=officer.StringId,Title=officer.Name.ToString(),
                        Status=ContextualHouseholdTitle(officer),Detail=AffairLocation(officer)+
                            (ActiveRetainerService(officer)?"\n"+RetainerServiceSummary(officer):"")+"\nSpeak to your officer."});
                }
            return rows;
        }
    }
}
