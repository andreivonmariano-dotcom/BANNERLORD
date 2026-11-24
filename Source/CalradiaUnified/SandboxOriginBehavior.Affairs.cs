using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ScreenSystem;

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
        private int CountPendingAffairs()
        {
            int day=CurrentHouseholdDay();
            var player=Hero.MainHero;
            int count=agencyDecisions.HasPending?1:0;
            count+=petitionStories.Stories.Count(s=>petitionStories.CanFollowUp(s.HeroId,day) &&
                LivingFollowUp(s.HeroId,day));
            if(IsPersonalChargeActive())count++;
            bool oath=knightOath.HasCurrentOath && knightOath.PlayerId==player.StringId;
            if(oath && knightServiceOrders.Active &&
                knightServiceOrders.PatronId==knightOath.PatronId)count++;
            if(oath && knightOath.CanMuster(day,true))count++;
            if(oath && knightServiceOrders.PatronId==knightOath.PatronId &&
                knightServiceOrders.RecommendationDay>=0 &&
                knightServiceOrders.RoyalHearingDay<0)count++;
            if(OpenAffairsClaim())count++;
            var clan=Clan.PlayerClan;
            if(clan.Settlements!=null)
                count+=clan.Settlements.Count(x=>x!=null && x.OwnerClan==clan && x.IsUnderSiege);
            return count;
        }
        public static bool OpenCampaignAffairs()
        {
            var b=AffairsBehavior();
            if(b==null || Hero.MainHero.IsPrisoner ||
                MobileParty.MainParty==null || MobileParty.MainParty.MapEvent!=null ||
                ScreenManager.TopScreen is CUAffairsScreen)return false;
            try
            {
                ScreenManager.PushScreen(new CUAffairsScreen(b.BuildAffairs()));
                return true;
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_affairs_screen_failed",error);
                b.HearDossierPetition();
                return false;
            }
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
            if(kind=="Audience")return "Requests a private conversation about the standing of your houses.";
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
            if(agencyDecisions.HasPending)ids.Add(agencyDecisions.PendingHeroId);
            if(knightOath.HasCurrentOath)ids.Add(knightOath.PatronId);
            foreach(var office in new[]{"Steward","Captain","Envoy"})
                ids.Add(householdDuties.AssignedId(office));
            if(inheritanceNegotiation.LeaderId.Length>0)ids.Add(inheritanceNegotiation.LeaderId);
            // Resolve current identities once, only on opening the journal.
            var known=Hero.AllAliveHeroes.Where(h=>h!=null && ids.Contains(h.StringId))
                .GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
            Func<string,Hero> find=id=>known.ContainsKey(id)?known[id]:null;
            if(agencyDecisions.HasPending)
            {
                var p=find(agencyDecisions.PendingHeroId);
                var pendingStory=petitionStories.For(agencyDecisions.PendingHeroId);
                rows.Add(new SandboxAffair{Id="request:"+agencyDecisions.PendingHeroId,
                    Category="Letters",HeroId=agencyDecisions.PendingHeroId,Title=AffairPerson(p)+" | Petition",
                    Status=p==null?"Person unavailable":"Awaiting your answer",Pending=true,
                    Day=pendingStory!=null && pendingStory.Kind==agencyDecisions.PendingKind?
                        pendingStory.StartedDay:Math.Max(0,day-(agencyDecisions.Day-agencyDecisions.PendingOpenedDay)),
                    Detail=(pendingStory!=null && pendingStory.Letter.Length>0?pendingStory.Letter:
                        PetitionConcern(agencyDecisions.PendingKind))+"\n\n"+
                        AffairLocation(p)+"\nSpeak with this person about their affairs. The journal cannot answer for you."});
            }
            foreach(var story in petitionStories.Stories)
            {
                if(story.Outcome=="Pending" && agencyDecisions.HasPending &&
                    agencyDecisions.PendingHeroId==story.HeroId)continue;
                var p=find(story.HeroId);
                bool due=p!=null && p.IsAlive &&
                    petitionStories.CanFollowUp(story.HeroId,day);
                bool supported=story.Outcome=="Supported";
                string status=due?"Conversation due":story.Outcome=="Pending"?
                    "No longer awaiting a reply":story.Outcome=="Expired"?"Reply period ended":
                    story.Outcome=="Unavailable"?"Person unavailable":
                    story.Outcome=="Unconfirmed"?"Outcome not confirmed":supported?
                    (story.Kind=="Audience"?"Audience heard":"Help given"):"Request declined";
                if(story.FollowUpDay>=0)status+=" | Revisited";
                rows.Add(new SandboxAffair{Id="story:"+story.HeroId,Category="Letters",HeroId=story.HeroId,
                    Title=AffairPerson(p)+(due?" | Follow-up":" | Earlier matter"),
                    Status=p==null?status+" | Person unavailable":status,Pending=due,Day=story.ResolvedDay>=0?story.ResolvedDay:story.StartedDay,
                    Completed=story.Outcome=="Supported" || story.Outcome=="Declined",
                    Detail=(story.Letter.Length>0?story.Letter:PetitionConcern(story.Kind))+"\n\n"+AffairLocation(p)+"\n"+
                        (due?"Ask how their affairs have fared since your decision.":
                         "An earlier matter from your personal history.")});
            }
            if(IsPersonalChargeActive())
                rows.Add(new SandboxAffair{Id="charge",Category="Service",Title="Your agreed duty",
                    Status=SelectedReviewRole()!=careerCharge.Role?"Calling changed":"Open",Pending=true,
                    Day=careerCharge.StartedDay,Detail=ChargeDescription()+
                        "\nDiscuss the result with a suitable adviser, or set the plan aside in your career chronicle."});
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
                if(knightServiceOrders.PatronId==knightOath.PatronId &&
                    knightServiceOrders.RecommendationDay>=0 && knightServiceOrders.RoyalHearingDay<0)
                    rows.Add(new SandboxAffair{Id="royal-audience",Category="Service",Title="Audience with the ruler",
                        Status="Recommendation recorded",Pending=true,Day=knightServiceOrders.RecommendationDay,
                        Detail="Seek the ruler of your patron's kingdom in person. A hearing grants no land or title."});
            }
            if(OpenAffairsClaim())
            {
                var estate=String.IsNullOrEmpty(inheritanceNegotiation.SettlementId)?null:
                    Settlement.Find(inheritanceNegotiation.SettlementId);
                rows.Add(new SandboxAffair{Id="inheritance",Category="Household",HeroId=inheritanceNegotiation.LeaderId,SettlementId=inheritanceNegotiation.SettlementId,Title="Family estate claim",
                    Status=inheritanceNegotiation.Authorized?"Awaiting lawful consent":"Disputed",
                    Pending=true,Day=inheritanceNegotiation.PetitionDay,Detail=
                        (estate==null?"The claim remains unresolved.":"Claim concerning "+estate.Name+".")+
                        "\nVisit the birth family's hall for the existing claim process. No grant can be made here."});
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
                            "\nUse the ordinary settlement and garrison controls to act."});
                }
            if(clan.Companions!=null)
                foreach(var officer in clan.Companions.Where(h=>h!=null && h.IsAlive && h.Clan==clan && h.CompanionOf==clan))
                {
                    string office=EffectiveHouseholdDuty(officer);
                    if(office.Length==0)continue;
                    rows.Add(new SandboxAffair{Id="officer:"+officer.StringId,Category="Household",
                        HeroId=officer.StringId,Title=officer.Name.ToString(),Status=office,Detail=AffairLocation(officer)+
                            "\nSpeak in person about their office and duties."});
                }
            return rows;
        }
    }
}
