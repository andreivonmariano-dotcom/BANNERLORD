using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxHouseholdDutyState householdDuties=
            new SandboxHouseholdDutyState();
        private SandboxRetainerServiceState retainerService=new SandboxRetainerServiceState();
        private bool ActiveRetainerService(Hero person)
        {
            var player=Hero.MainHero;var clan=Clan.PlayerClan;
            return person!=null && player!=null && clan!=null && person.IsAlive &&
                person.Clan==clan && person.CompanionOf==clan && clan.Companions!=null &&
                clan.Companions.Contains(person) && householdDuties.AssignedId("ManAtArms")==person.StringId &&
                retainerService.Matches(player.StringId,person.StringId);
        }
        private static int RetainerSkill(Hero h,string name)
        {
            var skill=SandboxNativeSkillCatalog.Find(name);
            return h==null || skill==null?0:h.GetSkillValue(skill);
        }
        private static int RetainerMartial(Hero h)
        {return new[]{"OneHanded","TwoHanded","Polearm","Bow","Crossbow","Throwing"}.Max(x=>RetainerSkill(h,x));}
        private bool CanEnrollRetainer()
        {
            var h=Hero.OneToOneConversationHero;
            return SpeakingWithHouseholdCompanion() && HouseholdOfficeFor(h.StringId)=="ManAtArms" &&
                !ActiveRetainerService(h) && h.Age>=18;
        }
        private void EnrollRetainer()
        {
            appointmentReply="We shall speak of this another time.";
            if(!CanEnrollRetainer())return;
            var h=Hero.OneToOneConversationHero;
            if(retainerService.Begin(Hero.MainHero.StringId,h.StringId,CurrentHouseholdDay()))
                appointmentReply="I am ready to serve.";
        }
        private bool CanPromoteRetainer(string rank)
        {
            var h=Hero.OneToOneConversationHero;
            return SpeakingWithHouseholdCompanion() && ActiveRetainerService(h) && h.Age>=18 &&
                retainerService.CanPromote(Hero.MainHero.StringId,h.StringId,rank,CurrentHouseholdDay(),
                    RetainerMartial(h),RetainerSkill(h,"Riding"),RetainerSkill(h,"Leadership"),
                    CurrentNobleAuthority(true)||CurrentNobleAuthority(false));
        }
        private void PromoteRetainer(string rank)
        {
            appointmentReply="Let us leave this for now.";
            if(!CanPromoteRetainer(rank))return;
            var h=Hero.OneToOneConversationHero;
            if(!SandboxPetitionVoicePolicy.AcceptsHouseholdDuty(Hero.MainHero.GetRelation(h),
                h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor),
                h.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating)))
            {appointmentReply="I am not ready for that charge.";return;}
            if(!retainerService.Promote(Hero.MainHero.StringId,h.StringId,rank,CurrentHouseholdDay(),
                RetainerMartial(h),RetainerSkill(h,"Riding"),RetainerSkill(h,"Leadership"),
                CurrentNobleAuthority(true)||CurrentNobleAuthority(false)))return;
            livingPersons.Remember("RetainerPromoted",h.StringId,Hero.MainHero.StringId,"rank="+rank);
            DiagnosticJournal.Event("cu_retainer_promoted","hero="+h.StringId+"|rank="+rank+"|native_rank_unchanged=true");
            appointmentReply="I shall strive to deserve the honor.";
        }
        private bool ReadyRetainerMarshal(Hero h)
        {
            return ActiveRetainerService(h) && CurrentNobleAuthority(true) &&
                retainerService.MarshalReady(Hero.MainHero.StringId,h.StringId,CurrentHouseholdDay(),
                    RetainerSkill(h,"Leadership"),RetainerSkill(h,"Tactics"));
        }
        private bool CanPayRetainer()
        {
            var h=Hero.OneToOneConversationHero;
            return SpeakingWithHouseholdCompanion() && ActiveRetainerService(h) &&
                retainerService.PayDue(Hero.MainHero.StringId,h.StringId,CurrentHouseholdDay()) &&
                Hero.MainHero.Gold>=SandboxRetainerServiceState.Wage(retainerService.Rank);
        }
        private void PayRetainer()
        {
            appointmentReply="No payment was confirmed.";
            if(!CanPayRetainer())return;
            var h=Hero.OneToOneConversationHero;var player=Hero.MainHero;
            int cost=SandboxRetainerServiceState.Wage(retainerService.Rank);
            int before=player.Gold,recipient=h.Gold;
            if(!retainerService.RecordPayAttempt(player.StringId,h.StringId,CurrentHouseholdDay()))return;
            try{GiveGoldAction.ApplyBetweenCharacters(player,h,cost,false);}
            catch(Exception error){DiagnosticJournal.Exception("cu_retainer_pay_failed",error);}
            string result=SandboxNativeEffectProof.GiftResult(before,player.Gold,recipient,h.Gold,cost);
            if(result=="confirmed")
            {
                livingPersons.Remember("RetainerPaid",h.StringId,player.StringId,"denars="+cost);
                appointmentReply="My thanks. I have received "+cost+" denars.";
            }
            DiagnosticJournal.Event("cu_retainer_pay","hero="+h.StringId+"|denars="+cost+"|result="+result);
        }
        private string RetainerServiceSummary(Hero h)
        {
            if(!ActiveRetainerService(h))return "";
            return SandboxRetinueOfficePolicy.Profession(retainerService.Rank)+
                " | Reports: "+retainerService.Reports+" | Days in rank: "+
                Math.Max(0,CurrentHouseholdDay()-retainerService.Since)+
                " | Weekly pay: "+SandboxRetainerServiceState.Wage(retainerService.Rank)+
                " denars (voluntary)"+(ReadyRetainerMarshal(h)?" | Qualified for Marshal":"");
        }


        // Appointment is only a personal, save-backed household duty.
        // It never changes the native party role, Clan or GovernorOf.
        private bool SpeakingWithHouseholdCompanion()
        {
            var speaker=Hero.OneToOneConversationHero;
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            return applied && Hero.MainHero!=null &&
                clan!=null && party!=null &&
                speaker!=null && speaker!=Hero.MainHero &&
                speaker.IsAlive && speaker.IsActive &&
                !speaker.IsPrisoner &&
                speaker.Clan==clan && speaker.CompanionOf==clan &&
                clan.Companions!=null &&
                clan.Companions.Contains(speaker) &&
                party.MemberRoster!=null &&
                speaker.CharacterObject!=null &&
                party.MemberRoster.GetTroopCount(
                    speaker.CharacterObject)==1;
        }
        private bool CanAppointHouseholdCompanion(string office)
        {
            if(!SpeakingWithHouseholdCompanion())return false;
            var speaker=Hero.OneToOneConversationHero;
            if(ExistingCourtDuty(speaker).Length>0)return false;
            string id=speaker.StringId;
            string incumbent=householdDuties.AssignedId(office);
            if(incumbent==id)return false;
            if(incumbent.Length>0)
            {
                var old=ResolveCampaignHero(incumbent);
                // Unknown identity is not proof of a vacancy.
                if(old==null || (old.IsAlive && old.Clan==Clan.PlayerClan &&
                    old.CompanionOf==Clan.PlayerClan))return false;
            }
            return new[]{"Steward","Captain","Envoy","ManAtArms"}
                .Where(x=>x!=office).All(x=>
                    householdDuties.AssignedId(x)!=id);
        }
        private string appointmentReply="";
        private bool PrepareAppointmentReply()
        {
            MBTextManager.SetTextVariable("CU_APPOINTMENT_REPLY",new TextObject(appointmentReply));
            return true;
        }
        private void AppointHouseholdCompanion(string office)
        {
            appointmentReply="Let us leave the matter for now.";
            if(!CanAppointHouseholdCompanion(office))return;
            var speaker=Hero.OneToOneConversationHero;
            string tone=PersonalVoice(speaker);
            if(!SandboxPetitionVoicePolicy.AcceptsHouseholdDuty(
                Hero.MainHero.GetRelation(speaker),
                speaker.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor),
                speaker.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating)))
            {
                appointmentReply="I would rather not take on that duty just now.";
                return;
            }
            string previous=householdDuties.AssignedId(office);
            if(previous.Length>0 && !householdDuties.Release(office,previous))return;
            string status=householdDuties.Assign(office,speaker.StringId);
            DiagnosticJournal.Event("cu_household_officer_appointed",
                "result="+status+"|office="+office+
                "|native_hero="+speaker.StringId+
                "|native_party_role_unchanged=true"+
                "|native_governorship_unchanged=true");
            if(status=="personal_duty_recorded")
            {
                if(office=="ManAtArms")retainerService.Begin(Hero.MainHero.StringId,speaker.StringId,CurrentHouseholdDay());
                appointmentReply=SandboxPetitionVoicePolicy.DecisionReply(tone,true,true)+
                    " I shall attend to my duties as "+SandboxRetinueOfficePolicy.Profession(office)+".";
                livingPersons.Remember("HouseholdDutyAppointed",speaker.StringId,
                    Hero.MainHero.StringId,"office="+office+"|previous="+previous);
            }
        }
        private bool CanReleaseHouseholdDuty()
        {
            return SpeakingWithHouseholdCompanion() &&
                ExistingCourtDuty(Hero.OneToOneConversationHero).Length==0 &&
                HouseholdOfficeFor(Hero.OneToOneConversationHero.StringId).Length>0;
        }
        private void ReleaseHouseholdDuty()
        {
            appointmentReply="Let us leave the matter for now.";
            if(!CanReleaseHouseholdDuty())return;
            var speaker=Hero.OneToOneConversationHero;
            string office=HouseholdOfficeFor(speaker.StringId);
            if(!householdDuties.Release(office,speaker.StringId))return;
            if(office=="ManAtArms" && retainerService.HeroId==speaker.StringId)retainerService.Clear();
            livingPersons.Remember("HouseholdDutyReleased",speaker.StringId,
                Hero.MainHero.StringId,"office="+office);
            appointmentReply=Hero.MainHero.GetRelation(speaker)<0?
                "Very well. I shall be glad to be free of that charge.":
                "Then I lay that duty aside. I remain in your company.";
            DiagnosticJournal.Event("cu_household_duty_released",
                "hero="+speaker.StringId+"|office="+office+"|native_role_unchanged=true");
        }
        private static string ExistingCourtDuty(Hero person)
        {
            if(person==null || Clan.PlayerClan==null ||
                person.Clan!=Clan.PlayerClan || person.CompanionOf!=Clan.PlayerClan)return "";
            return SandboxRetinueOfficePolicy.Label(RecordedRetinueOffice(person));
        }

        private string EffectiveHouseholdDuty(Hero person)
        {
            if(person==null)return "";
            string court=ExistingCourtDuty(person);
            return court.Length>0?court:HouseholdOfficeFor(person.StringId);
        }

        private bool CanDiscussHouseholdDuty()
        {
            var speaker=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            bool residentMinister=applied && player!=null && speaker!=null &&
                clan!=null && speaker.IsAlive && speaker.IsActive && !speaker.IsPrisoner &&
                speaker.Clan==clan && ExistingCourtDuty(speaker)=="Royal Minister" &&
                SandboxKingNativeMinistry.VerifiedGovernor(speaker) &&
                speaker.GovernorOf.Settlement==Settlement.CurrentSettlement;
            return CanReviewNativeHouseholdAssignment() ||
                ((SpeakingWithHouseholdCompanion() || residentMinister) &&
                EffectiveHouseholdDuty(speaker).Length>0);
        }

        private static string NativeOfficerSkills(Hero officer,string office)
        {
            if(officer==null)return "";
            var values=SandboxRetinueOfficePolicy.Skills(SandboxRetinueOfficePolicy.Key(office))
                .Select(name=>new {Name=name,Skill=SandboxNativeSkillCatalog.Find(name)})
                .Where(x=>x.Skill!=null)
                .Select(x=>x.Name+"="+officer.GetSkillValue(x.Skill));
            return String.Join(", ",values);
        }
        private string dutyReplySpeaker="",dutyReplyPlayer="";
        private int dutyReplyVariant;
        private bool PrepareHouseholdDutyReply()
        {
            var speaker=Hero.OneToOneConversationHero;
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            var seat=Settlement.CurrentSettlement;
            string office=EffectiveHouseholdDuty(speaker);
            string reply="Let us speak again when we are together.";
            if(CanReviewNativeHouseholdAssignment())
            {
                reply="My current appointment is "+NativeHouseholdAssignment(speaker)+
                    ". That is my present duty, not a new promise.";
                MBTextManager.SetTextVariable("CU_HOUSEHOLD_DUTY_REPLY",new TextObject(reply));
                return true;
            }
            if(CanDiscussHouseholdDuty() && speaker!=null && party!=null && clan!=null && office.Length>0)
            {
                int strength=party.MemberRoster==null?-1:party.MemberRoster.TotalManCount;
                string company=strength<0?"I cannot confirm our company strength.":
                    "Our company numbers "+strength+" souls.";
                string holdings="Our house holds "+(clan.Settlements==null?0:clan.Settlements.Count)+" settlements.";
                bool owned=seat!=null && seat.OwnerClan==clan && seat.IsFortification;
                if(office=="Steward" || office=="Royal Minister" ||
                    office=="Household Steward" || office=="Chamberlain" || office=="Factor")
                    reply=holdings+" Your purse holds "+Hero.MainHero.Gold+" denars. "+
                        (owned?"We can inspect the stores and defenses here.":
                        "Let us inspect our own holdings when we return.");
                else if(office=="Navigator")
                    reply="My concern is the road ahead. We should travel with care and keep sight of our route.";
                else if(office=="Envoy")
                    reply=clan.Kingdom==null?"We owe allegiance to no realm at present.":
                        "Our allegiance is to "+clan.Kingdom.Name+".";
                else
                {
                    reply=company+" ";
                    if(owned && seat.Town!=null)
                    {
                        var garrison=seat.Town.GarrisonParty;
                        reply+=garrison==null || garrison.MemberRoster==null?
                            "I cannot confirm the garrison at "+seat.Name+".":
                            "At "+seat.Name+", the garrison numbers "+
                            garrison.MemberRoster.TotalManCount+".";
                        if(seat.IsUnderSiege)reply+=" The holding is under siege.";
                    }
                    else reply+="We are away from our own fortifications.";
                    reply+=" Let us review the troops before our next march.";
                }
                int variant=dutyReplySpeaker==speaker.StringId && dutyReplyPlayer==Hero.MainHero.StringId?
                    dutyReplyVariant:0;
                string focus=SandboxRetinueOfficePolicy.DutyFocus(ActiveRetainerService(speaker)?retainerService.Rank:office,owned,
                    owned && seat.IsUnderSiege,SandboxKingNativeMinistry.VerifiedGovernor(speaker),variant);
                if(focus.Length>0)reply+=" "+focus;
                // Skill numbers belong in the character record, not spoken dialogue.
                if(ExistingCourtDuty(speaker).Length==0)
                {
                    int reports=householdDuties.ReportCount(office);
                    reply+=reports==0?" We have yet to go over these matters together.":" We have spoken of these matters before.";
                }
            }
            reply+=" "+SandboxPetitionVoicePolicy.Opinion(PersonalVoice(speaker),
                office=="Steward" || office=="Royal Minister" || office=="Household Steward" ||
                office=="Chamberlain" || office=="Factor"?"Finances":"Service");
            MBTextManager.SetTextVariable("CU_HOUSEHOLD_DUTY_REPLY",new TextObject(reply));
            return true;
        }

        private void DiscussHouseholdDuty()
        {
            if(!CanDiscussHouseholdDuty())return;
            var person=Hero.OneToOneConversationHero;
            dutyReplySpeaker=person.StringId;dutyReplyPlayer=Hero.MainHero.StringId;
            if(CanReviewNativeHouseholdAssignment())return; // A live native report, not another custom office receipt.
            dutyReplyVariant=NextDialogueVariant(person,"OfficerDuty",2);
            string recorded=ExistingCourtDuty(person);
            if(recorded.Length>0)
            {
                string dayKey="office="+recorded+"|day="+CurrentHouseholdDay();
                string detail=dayKey+"|level="+person.Level+"|skills="+NativeOfficerSkills(person,recorded);
                if(!livingPersons.Events.Any(e=>e.Kind=="CourtDutyConsulted" &&
                    e.Actor==person.StringId && e.Target==Hero.MainHero.StringId &&
                    (e.Detail==dayKey || e.Detail.StartsWith(dayKey+"|",StringComparison.Ordinal))))
                    livingPersons.Remember("CourtDutyConsulted",person.StringId,Hero.MainHero.StringId,detail);
            }
            // Generated court titles own the role; old private appointments
            // remain readable but cannot produce a second formal duty receipt.
            if(ExistingCourtDuty(person).Length==0 && CanReportHouseholdDuty())
                RecordHouseholdDuty();
            DiagnosticJournal.Event("cu_household_repeat_conversation",
                "hero="+person.StringId+"|office="+EffectiveHouseholdDuty(person)+
                "|read_only_report_available=true|no_repeat_reward=true");
        }

        private bool CanReportHouseholdDuty()
        {
            if(!SpeakingWithHouseholdCompanion())return false;
            var speaker=Hero.OneToOneConversationHero;
            var party=MobileParty.MainParty;
            var settlement=Settlement.CurrentSettlement;
            string office=HouseholdOfficeFor(speaker.StringId);
            if(!householdDuties.CanReport(office,speaker.StringId,
                CurrentHouseholdDay()))return false;
            if(office=="Steward")
                return settlement!=null &&
                    settlement.OwnerClan==Clan.PlayerClan &&
                    settlement.IsFortification;
            if(office=="Captain")
                return party.MemberRoster.TotalManCount>=8;
            if(office=="ManAtArms")
                return party.MemberRoster.GetTroopCount(speaker.CharacterObject)>0;
            if(office=="Envoy")
                return settlement!=null && settlement.IsTown &&
                    settlement.OwnerClan!=null &&
                    settlement.OwnerClan!=Clan.PlayerClan;
            return false;
        }
        private string HouseholdOfficeFor(string id)
        {
            foreach(var office in new[]{"Steward","Captain","Envoy","ManAtArms"})
                if(householdDuties.AssignedId(office)==id)
                    return office;
            return "";
        }
        private void RecordHouseholdDuty()
        {
            if(!CanReportHouseholdDuty())return;
            var speaker=Hero.OneToOneConversationHero;
            string office=HouseholdOfficeFor(speaker.StringId);
            string result=householdDuties.Report(office,
                speaker.StringId,CurrentHouseholdDay());
            DiagnosticJournal.Event("cu_household_native_duty_report",
                "result="+result+"|office="+office+
                "|companion="+speaker.StringId+
                "|reports="+householdDuties.ReportCount(office)+
                "|actual_native_evidence=true|free_rewards=0");
            // Record the witnessed consultation, not a new economic or military action.
            if(result=="native_duty_report_recorded")
            {
                if(office=="ManAtArms" && ActiveRetainerService(speaker))
                    retainerService.Record(Hero.MainHero.StringId,speaker.StringId,CurrentHouseholdDay());
                livingPersons.Remember("HouseholdDutyReviewed",speaker.StringId,
                    Hero.MainHero.StringId,"office="+office+"|day="+CurrentHouseholdDay());
            }
            // The conversation itself reports the state; no receipt popup.
        }
        private static int CurrentHouseholdDay()
        {
            return (int)Math.Floor(CampaignTime.Now.ToDays);
        }
        private bool SpeakingWithAdultFamily()
        {
            var player=Hero.MainHero;
            var speaker=Hero.OneToOneConversationHero;
            return applied && player!=null && speaker!=null &&
                speaker!=player && speaker.IsAlive &&
                speaker.IsActive && !speaker.IsPrisoner &&
                speaker.Age>=18f &&
                ((speaker==player.Spouse &&
                  speaker.Spouse==player) ||
                 speaker==player.Father || speaker==player.Mother ||
                 (player.Children!=null &&
                  player.Children.Contains(speaker) &&
                  (speaker.Mother==player || speaker.Father==player)) ||
                 (player.Father!=null && player.Mother!=null &&
                  speaker.Father==player.Father && speaker.Mother==player.Mother));
        }
        private bool PrepareFamilyConversationReply()
        {
            var person=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            string reply="Let us speak again when we can.";
            if(person!=null && player!=null && SpeakingWithAdultFamily())
            {
                var home=person.StayingInSettlement ?? person.CurrentSettlement;
                var shared=player.Children==null?new Hero[0]:player.Children.Where(h=>h!=null && h.IsAlive &&
                    (h.Father==person || h.Mother==person)).ToArray();
                int children=shared.Length;
                int minors=shared.Count(h=>h.Age<18f);
                int relation=player.GetRelation(person);
                reply=relation<0?"We have matters between us yet to settle. ":
                    relation>=50?"I am glad to have you here. ":
                    "It is good that we have time to speak. ";
                if(person==player.Spouse)
                    reply+="Our family has "+children+" living children, "+minors+
                        " still young. ";
                if(home!=null)reply+="I am presently at "+home.Name+". ";
                // The actual native appointment is useful family context.
                string assignment=NativeHouseholdAssignment(person);
                if(assignment.Length>0)reply+="My present duty is "+assignment+". ";
                // A gift-attempt cooldown is not proof of payment.
                reply+="Let us talk of our house again whenever you return.";
            }
            reply+=" "+SandboxPetitionVoicePolicy.Opinion(PersonalVoice(person),"Family");
            MBTextManager.SetTextVariable("CU_FAMILY_CONVERSATION_REPLY",new TextObject(reply));
            return true;
        }

        private void DiscussFamilyAffairs()
        {
            if(!SpeakingWithAdultFamily())return;
            if(CanFamilyAudience())HoldFamilyAudience();
            DiagnosticJournal.Event("cu_family_repeat_conversation",
                "hero="+Hero.OneToOneConversationHero.StringId+
                "|audience_days_remaining="+householdDuties.AudienceDaysRemaining(
                    Hero.OneToOneConversationHero.StringId,CurrentHouseholdDay())+
                "|conversation_not_reward_gated=true");
        }

        private bool CanFamilyAudience()
        {
            return SpeakingWithAdultFamily() &&
                householdDuties.CanAudience(
                    Hero.OneToOneConversationHero.StringId,
                    CurrentHouseholdDay());
        }
        private void HoldFamilyAudience()
        {
            if(!CanFamilyAudience())return;
            var person=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            int day=CurrentHouseholdDay();
            // A visit is a memory, not a repeatable relation bonus.
            if(!householdDuties.RecordAudience(person.StringId,day))return;
            livingPersons.Remember("FamilyVisit",person.StringId,player.StringId,"day="+day);
            DiagnosticJournal.Event("cu_family_in_person_audience",
                "native_hero="+person.StringId+"|day="+day+
                "|personal_visit_recorded=true|extra_relation=false");
        }
        private bool CanFamilyGift()
        {
            return SpeakingWithAdultFamily() &&
                Hero.MainHero.Gold>=100 &&
                householdDuties.CanGift(
                    Hero.OneToOneConversationHero.StringId,
                    CurrentHouseholdDay());
        }
        private string familyGiftReply="";
        private bool PrepareFamilyGiftReply()
        {
            MBTextManager.SetTextVariable("CU_FAMILY_GIFT_REPLY",new TextObject(familyGiftReply));
            return true;
        }
        private void GiveFamilyGift()
        {
            familyGiftReply="Let us leave that matter for now.";
            if(!CanFamilyGift())return;
            var player=Hero.MainHero;
            var recipient=Hero.OneToOneConversationHero;
            int day=CurrentHouseholdDay();
            if(!householdDuties.RecordGiftAttempt(
                recipient.StringId,day))return;
            int payerBefore=player.Gold;
            int recipientBefore=recipient.Gold;
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(
                    player,recipient,100,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_gift_native_transfer_failed",error);
            }
            string proof=SandboxNativeEffectProof.GiftResult(
                payerBefore,player.Gold,
                recipientBefore,recipient.Gold,100);
            DiagnosticJournal.Event("cu_family_native_household_gift",
                "recipient="+recipient.StringId+
                "|amount_requested=100"+
                "|native_result="+proof+
                "|no_automatic_salary_or_free_gold=true");
            if(proof=="confirmed")livingPersons.Remember("FamilyGiftConfirmed",player.StringId,
                recipient.StringId,"day="+day+"|denars=100|native_transfer_confirmed=true");
            familyGiftReply=SandboxPetitionVoicePolicy.DecisionReply(
                PersonalVoice(recipient),true,proof=="confirmed");
            if(proof!="confirmed")Notify("The transfer could not be confirmed.");
        }
        private void RegisterHouseholdConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_family_house_audience","cu_personal_matters_topics",
                "cu_family_house_audience_reply",
                "Let us spend some time together.",
                ()=>SpeakingWithAdultFamily(),
                ()=>DiscussFamilyAffairs(),115);
            starter.AddDialogLine(
                "cu_family_house_audience_reply",
                "cu_family_house_audience_reply","cu_personal_matters_topics",
                "{CU_FAMILY_CONVERSATION_REPLY}",
                ()=>PrepareFamilyConversationReply(),null,110);
            starter.AddDialogLine("cu_family_house_audience_reply_cu_safe_exit","cu_family_house_audience_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine(
                "cu_family_house_gift","cu_personal_matters_topics",
                "cu_family_house_gift_reply",
                "I have brought 100 denars for your household.",
                ()=>CanFamilyGift(),()=>GiveFamilyGift(),105);
            starter.AddDialogLine(
                "cu_family_house_gift_reply",
                "cu_family_house_gift_reply","cu_personal_matters_topics",
                "{CU_FAMILY_GIFT_REPLY}",
                ()=>PrepareFamilyGiftReply(),null,100);
            starter.AddDialogLine("cu_family_house_gift_reply_cu_safe_exit","cu_family_house_gift_reply",
                "close_window","We can speak again another time.",null,null,1);

            starter.AddPlayerLine("cu_household_duty_release","cu_personal_matters_topics",
                "cu_household_duty_release_reply","I release you from your household duty.",
                ()=>CanReleaseHouseholdDuty(),()=>ReleaseHouseholdDuty(),100);
            starter.AddDialogLine("cu_household_duty_release_reply","cu_household_duty_release_reply",
                "cu_personal_matters_topics","{CU_APPOINTMENT_REPLY}",()=>PrepareAppointmentReply(),null,100);
            starter.AddDialogLine("cu_household_duty_release_reply_cu_safe_exit","cu_household_duty_release_reply",
                "close_window","We can speak again another time.",null,null,1);
            starter.AddPlayerLine(
                "cu_household_duty_report","cu_personal_matters_topics",
                "cu_household_duty_report_reply",
                "Report your current assignment.",
                ()=>CanDiscussHouseholdDuty(),
                ()=>DiscussHouseholdDuty(),110);
            starter.AddDialogLine(
                "cu_household_duty_report_reply",
                "cu_household_duty_report_reply","cu_personal_matters_topics",
                "{CU_HOUSEHOLD_DUTY_REPLY}",
                ()=>PrepareHouseholdDutyReply(),null,100);
            starter.AddDialogLine("cu_household_duty_report_reply_cu_safe_exit","cu_household_duty_report_reply",
                "close_window","We can speak again another time.",null,null,1);
        }
        private void ShowHouseholdDutyLedger()
        {
            var clan=Clan.PlayerClan;
            string text="";
            foreach(var office in new[]{"Steward","Captain","Envoy","ManAtArms"})
            {
                string id=householdDuties.AssignedId(office);
                var hero=ResolveCampaignHero(id);
                bool present=hero!=null && hero.IsAlive &&
                    clan!=null && hero.CompanionOf==clan &&
                    hero.Clan==clan;
                text+=(office=="ManAtArms"?"Man-at-Arms":office)+": "+(id.Length==0?"vacant":
                    present?hero.Name.ToString():
                    "former holder unavailable")+
                    " | witnessed reports: "+
                    householdDuties.ReportCount(office)+"\n";
                if(present && office=="ManAtArms" && ActiveRetainerService(hero))
                    text+=RetainerServiceSummary(hero)+"\n";
            }
            if(clan!=null && clan.Companions!=null)
                foreach(var officer in clan.Companions.Where(h=>h!=null && h.IsAlive && h.Clan==clan && h.CompanionOf==clan).Take(32))
                {
                    string title=ExistingCourtDuty(officer);
                    if(title.Length==0)continue;
                    int consultations=livingPersons.Events.Count(e=>e.Kind=="CourtDutyConsulted" &&
                        e.Actor==officer.StringId && e.Target==Hero.MainHero.StringId &&
                        e.Detail.StartsWith("office="+title+"|day=",StringComparison.Ordinal));
                    text+="\n"+title+": "+officer.Name+" | recent consultations: "+consultations;
                }
            text+="\nSpeak to a companion to appoint an officer. "+
                "Reports can be discussed repeatedly; formal receipts are limited by day. "+
                "The Steward reports from a real owned "+
                "fortification. The Captain reports with at least "+
                "eight living party members. The Envoy reports "+
                "from a real town outside your clan. "+
                "Appointments are household duties, not "+
                "native party roles or governorships.";
            ShowNativeDetail("Household | Officers and duties",text);
        }
    }
}
