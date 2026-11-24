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

        private bool CanDiscussPersonalMatters()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(!applied || !IsStandaloneSandbox() || actor==null || player==null ||
                actor==player || !actor.IsAlive || !actor.IsActive || actor.IsPrisoner ||
                actor.Age<18f)return false;
            if(SpeakingWithAdultFamily() || CanDiscussHouseholdDuty() ||
                livingPersons.People.Any(p=>p.Id==actor.StringId))return true;
            if(knightOath.PlayerId==player.StringId && knightOath.HasCurrentOath &&
                actor==KnightOathPatron())return true;
            var service=SandboxSaSBridge.Read();
            return service.Available && service.Enlisted && service.LordId==actor.StringId;
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
                (kind!=SandboxCareerChargeState.DefaultKind(role) &&
                 !(role=="Lord" && kind=="StrengthenGarrison")))return false;
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
            if(!IsPersonalChargeActive())return "We have no unfinished charge agreed for you. Let us decide what needs doing next.";
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
            else if(careerCharge.Kind=="CompanyBattle" && Clan.PlayerClan!=null &&
                Clan.PlayerClan.Kingdom!=null)target=Clan.PlayerClan.Kingdom.Name.ToString();
            string task=SandboxCareerChargeState.Label(careerCharge.Kind);
            if(careerCharge.Kind=="VisitTown")task+=" (depart from "+target+")";
            else task+=" ("+target+")";
            if(careerCharge.Kind=="StrengthenGarrison" || careerCharge.Kind=="BuildBand" ||
                careerCharge.Kind=="ServeWeek")task+=". Starting count: "+careerCharge.Baseline;
            return task+". Return to discuss the result on a later day. Use the game's own actions to carry it out.";
        }

        private string AgreeCareerCharge(string kind)
        {
            string target;int baseline;
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
                    value=battleHistory.Entries.Where(e=>e.Source=="PlayerParty" &&
                        e.Outcome=="Victory" && e.Day>careerCharge.StartedDay)
                        .Select(e=>e.Day).DefaultIfEmpty(-1).Max();
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
            if(!ObserveChargeCompletion())return "The charge is not yet fulfilled. "+ChargeDescription();
            var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            string role=careerCharge.Role,kind=careerCharge.Kind,target=careerCharge.TargetId;
            // Existing career receipt is the only progression record.
            if(!recurringCareerReviews.Record(player.StringId,role,day,true))
                return "The deed is witnessed. Let us enter it in the chronicle when the next weekly review is due.";
            careerCharge.Close(player.StringId,day);
            livingPersons.Remember("CareerReviewObserved",player.StringId,target,
                "role="+role+"|charge="+kind+"|day="+day+"|native_evidence=true");
            DiagnosticJournal.Event("cu_career_charge_completed","hero="+player.StringId+
                "|role="+role+"|kind="+kind+"|target="+target+"|day="+day+"|extra_reward=false");
            return "You have carried out the charge. It stands in the chronicle. We shall consider another duty on a later day.";
        }

        private string WithdrawCareerCharge()
        {
            if(!IsPersonalChargeActive() || !careerCharge.Close(Hero.MainHero.StringId,CurrentHouseholdDay()))
                return "There is no charge to withdraw.";
            DiagnosticJournal.Event("cu_career_charge_withdrawn","hero="+Hero.MainHero.StringId+"|reward=false");
            return "Then we set that plan aside. No deed has been entered as accomplished.";
        }

        private bool PreparePersonalTopic(string topic)
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            string reply="Let us speak again when we are together.";
            if(CanDiscussPersonalMatters())
            {
                var agenda=characterAgendas.Get(actor.StringId);
                int relation=player.GetRelation(actor);
                if(topic=="Ambitions")
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
                        default:reply="I have not settled on a particular ambition to share.";break;
                    }
                    if(relation<0)reply+=" I am not ready to entrust all my concerns to you.";
                    else if(actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor)>0)
                        reply+=" I would rather earn it fairly than take what is not mine.";
                    else if(actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating)>0)
                        reply+=" We must weigh the cost before committing ourselves.";
                }
                else if(topic=="Family")
                {
                    int children=player.Children==null?0:player.Children.Count(h=>h!=null && h.IsAlive);
                    int young=player.Children==null?0:player.Children.Count(h=>h!=null && h.IsAlive && h.Age<18f);
                    reply="Your house has "+children+" living children; "+young+" are still young. "+
                        (young>0?"Their upbringing deserves our attention.":"We must think of their own places in the world.");
                    if(actor==player.Spouse)reply+=" Let us discuss how we provide for them.";
                    if(player.Children!=null)
                    {
                        var living=player.Children.Where(h=>h!=null && h.IsAlive)
                            .OrderByDescending(h=>h.Age).Take(3).ToArray();
                        if(living.Length>0)
                            reply+=" We should consider "+String.Join(", ",living.Select(h=>
                                h.Name.ToString()+" ("+(int)h.Age+")"))+".";
                    }
                }
                else if(topic=="Finances")
                    reply="You have "+player.Gold+" denars in your purse. I hold "+actor.Gold+
                        ". The clan's lands are a separate concern. Let us keep enough for our actual obligations.";
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
                        else reply+=" Shall we visit another holding or strengthen these defenses?";
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
                            " fallen and "+recent.PartyWounded+" wounded in an involved party.";
                }
                else if(topic=="Memory")
                {
                    var story=petitionStories.For(actor.StringId);
                    reply=story==null?"No earlier petition between us stands in my record.":
                        story.Outcome=="Supported"?"I remember your support for my earlier concern.":
                        story.Outcome=="Declined"?"I remember your refusal. We can still speak of our affairs.":
                        "There is still a matter awaiting your answer.";
                }
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
                ()=>CanDiscussPersonalMatters(),null,105);
            starter.AddDialogLine("cu_personal_matters_intro","cu_personal_matters_intro",
                "cu_personal_matters_topics","Of course. What is on your mind?",null,null,105);
            foreach(string topicName in new[]{"Ambitions","Family","Finances","Estate","Service","Memory"})
            {
                string topic=topicName;
                string question=topic=="Ambitions"?"What do you wish for yourself?":
                    topic=="Family"?"How shall we care for the family?":
                    topic=="Finances"?"How stand our personal finances?":
                    topic=="Estate"?"What concerns our holdings?":
                    topic=="Service"?"What charge lies before us?":
                    "Do you remember our earlier affairs?";
                starter.AddPlayerLine("cu_personal_topic_"+topic,"cu_personal_matters_topics",
                    "cu_personal_topic_reply_"+topic,question,
                    ()=>CanDiscussPersonalMatters() &&
                        (topic!="Family" || SpeakingWithAdultFamily()) &&
                        (topic!="Finances" || SpeakingWithAdultFamily() || CanDiscussHouseholdDuty()) &&
                        (topic!="Estate" || CanDiscussEstateTopic()),null,100);
                starter.AddDialogLine("cu_personal_topic_reply_"+topic,"cu_personal_topic_reply_"+topic,
                    "cu_personal_matters_topics","{CU_PERSONAL_TOPIC_REPLY}",
                    ()=>PreparePersonalTopic(topic),null,100);
            }
            starter.AddPlayerLine("cu_personal_charge_agree","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us agree on our next duty.",
                ()=>CanDiscussPersonalMatters() && !IsPersonalChargeActive() && SelectedReviewRole().Length>0,
                ()=>chargeReply=AgreeCareerCharge(SandboxCareerChargeState.DefaultKind(SelectedReviewRole())),100);
            starter.AddPlayerLine("cu_personal_charge_defend","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us strengthen the garrison here.",
                ()=>CanDiscussEstateTopic() && SelectedReviewRole()=="Lord" && !IsPersonalChargeActive(),
                ()=>chargeReply=AgreeCareerCharge("StrengthenGarrison"),100);
            starter.AddPlayerLine("cu_personal_charge_progress","cu_personal_matters_topics",
                "cu_personal_charge_reply","Let us review what we have accomplished.",
                ()=>CanDiscussPersonalMatters() && IsPersonalChargeActive(),
                ()=>chargeReply=DiscussChargeProgress(),100);
            starter.AddPlayerLine("cu_personal_charge_withdraw","cu_personal_matters_topics",
                "cu_personal_charge_reply","We must set our charge aside.",
                ()=>CanDiscussPersonalMatters() && IsPersonalChargeActive(),
                ()=>chargeReply=WithdrawCareerCharge(),90);
            starter.AddDialogLine("cu_personal_charge_reply","cu_personal_charge_reply",
                "cu_personal_matters_topics","{CU_CHARGE_REPLY}",()=>PrepareChargeReply(),null,100);
            starter.AddPlayerLine("cu_personal_matters_return","cu_personal_matters_topics",
                "hero_main_options","That is all for now.",null,null,80);
        }
    }
}
