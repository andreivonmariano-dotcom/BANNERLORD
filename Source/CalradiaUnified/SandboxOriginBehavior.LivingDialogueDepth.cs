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
        private bool PreparePersonalGreeting()
        {
            MBTextManager.SetTextVariable("CU_PERSONAL_GREETING",
                new TextObject(SandboxPetitionVoicePolicy.Greeting(PersonalVoice(Hero.OneToOneConversationHero))));
            return true;
        }
        private bool CanDiscussPersonalMatters()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            if(!CanSpeakSocially())return false;
            if(SpeakingWithAdultFamily() || CanDiscussHouseholdDuty() ||
                SpeakingWithHouseholdCompanion() ||
                livingPersons.People.Any(p=>p.Id==actor.StringId))return true;
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
            if(topic=="Family")return SpeakingWithAdultFamily();
            if(topic=="Finances")return player.GetRelation(actor)>=10 &&
                (SpeakingWithAdultFamily() || CanDiscussHouseholdDuty());
            if(topic=="Estate")return CanDiscussEstateTopic() && player.GetRelation(actor)>=0;
            if(topic=="Service")return CanCounselCareer();
            if(topic=="Memory")
            {
                var story=petitionStories.For(actor.StringId);
                return story!=null &&
                    petitionStories.CanRecall(actor.StringId,CurrentHouseholdDay());
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
            if(CanDiscussPersonalTopic(topic))
            {
                var agenda=characterAgendas.Get(actor.StringId);
                int relation=player.GetRelation(actor);
                string tone=PersonalVoice(actor);
                if(topic=="Opinions")
                    reply=SandboxPetitionVoicePolicy.Opinion(tone,"Opinions");
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
                    reply=children==0?(actor==player.Spouse?
                        "We have no living children of our own.":"You have no living children of your own."):
                        (actor==player.Spouse?"Our family":"Your family")+" has "+children+
                        " children living. "+(young>0?"The younger ones still need care.":"They have their own lives to lead.");
                    if(actor==player.Spouse)reply+=" Let us discuss how we provide for them.";
                    if(player.Children!=null)
                    {
                        var living=player.Children.Where(h=>h!=null && h.IsAlive)
                            .OrderByDescending(h=>h.Age).Take(3).ToArray();
                        if(living.Length>0)
                            reply+=" We should consider "+String.Join(", ",living.Select(h=>
                                h.Name.ToString()))+".";
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
                    var story=petitionStories.For(actor.StringId);
                    reply=story==null?"There is nothing further to say on that matter.":
                        story.Kind=="Audience"?
                            (story.Outcome=="Supported"?"We have spoken of the matter. Our houses shall each keep their own counsel.":
                             "You gave me your answer. Let us leave the matter there."):
                        story.Outcome=="Supported"?
                            (tone=="Guarded"?"I remember what you did. Deeds carry more weight than words.":
                             tone=="Warm"?"I have not forgotten your kindness to me.":
                             "I remember your help in that matter."):
                        story.Outcome=="Declined"?
                            (tone=="Guarded"?"I remember your answer. I have kept my own counsel.":
                             tone=="Bold"?"You had your reasons. I shall find my own way.":
                             "I remember your answer. We need not quarrel over it."):
                        "There is still a matter awaiting your answer.";
                }
                string opinion=topic=="Opinions"?"":SandboxPetitionVoicePolicy.Opinion(tone,topic);
                if(opinion.Length>0)reply+=" "+opinion;
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
                ()=>CanSpeakSocially(),null,105);
            starter.AddDialogLine("cu_personal_matters_intro","cu_personal_matters_intro",
                "cu_personal_matters_topics","{CU_PERSONAL_GREETING}",()=>PreparePersonalGreeting(),null,105);
            foreach(string topicName in new[]{"Opinions","Concerns","Ambitions","Family","Finances","Estate","Service","Memory"})
            {
                string topic=topicName;
                string question=topic=="Opinions"?"What matters most to you in life?":
                    topic=="Concerns"?"Is there anything that needs our attention?":topic=="Ambitions"?"What do you wish for yourself?":
                    topic=="Family"?"How shall we care for the family?":
                    topic=="Finances"?"How stand our personal finances?":
                    topic=="Estate"?"What concerns our holdings?":
                    topic=="Service"?"What charge lies before us?":
                    "Do you remember our earlier affairs?";
                starter.AddPlayerLine("cu_personal_topic_"+topic,"cu_personal_matters_topics",
                    "cu_personal_topic_reply_"+topic,question,
                    ()=>CanDiscussPersonalTopic(topic),null,100);
                starter.AddDialogLine("cu_personal_topic_reply_"+topic,"cu_personal_topic_reply_"+topic,
                    "cu_personal_matters_topics","{CU_PERSONAL_TOPIC_REPLY}",
                    ()=>PreparePersonalTopic(topic),null,100);
            }
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
            starter.AddPlayerLine("cu_family_education_leave","cu_family_education_choices",
                "cu_personal_matters_topics","Let us leave the matter for now.",null,null,80);
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
            starter.AddPlayerLine("cu_personal_matters_return","cu_personal_matters_topics",
                "hero_main_options","That is all for now.",null,null,80);
        }
    }
}
