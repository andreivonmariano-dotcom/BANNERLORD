using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxHouseholdDutyState householdDuties=
            new SandboxHouseholdDutyState();

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
            string id=Hero.OneToOneConversationHero.StringId;
            if(householdDuties.AssignedId(office)==id)return false;
            return new[]{"Steward","Captain","Envoy"}
                .Where(x=>x!=office).All(x=>
                    householdDuties.AssignedId(x)!=id);
        }
        private void AppointHouseholdCompanion(string office)
        {
            if(!CanAppointHouseholdCompanion(office))return;
            var speaker=Hero.OneToOneConversationHero;
            string status=householdDuties.Assign(office,speaker.StringId);
            DiagnosticJournal.Event("cu_household_officer_appointed",
                "result="+status+"|office="+office+
                "|native_hero="+speaker.StringId+
                "|native_party_role_unchanged=true"+
                "|native_governorship_unchanged=true");
            if(status=="personal_duty_recorded")
                Notify(speaker.Name+" now serves as your household "+
                    office+" in your personal household ledger.");
        }
        private bool CanReportHouseholdDuty()
        {
            if(!SpeakingWithHouseholdCompanion())return false;
            var speaker=Hero.OneToOneConversationHero;
            var party=MobileParty.MainParty;
            var settlement=Settlement.CurrentSettlement;
            string office=HouseholdOfficeFor(speaker.StringId);
            if(office=="Steward")
                return settlement!=null &&
                    settlement.OwnerClan==Clan.PlayerClan &&
                    settlement.IsFortification;
            if(office=="Captain")
                return party.MemberRoster.TotalManCount>=8;
            if(office=="Envoy")
                return settlement!=null && settlement.IsTown &&
                    settlement.OwnerClan!=null &&
                    settlement.OwnerClan!=Clan.PlayerClan;
            return false;
        }
        private string HouseholdOfficeFor(string id)
        {
            foreach(var office in new[]{"Steward","Captain","Envoy"})
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
            if(result=="native_duty_report_recorded")
                Notify("A verified "+office+" duty was recorded for "+
                    speaker.Name+". No salary, troops or native office "+
                    "was granted.");
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
                 (player.Children!=null &&
                  player.Children.Contains(speaker) &&
                  (speaker.Mother==player || speaker.Father==player)));
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
            // Save the audience first, before any native relation change,
            // so reloading cannot duplicate earned relation.
            if(!householdDuties.RecordAudience(person.StringId,day))
                return;
            int before=player.GetRelation(person);
            try
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    player,person,1,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_audience_native_relation_failed",error);
            }
            int after=player.GetRelation(person);
            DiagnosticJournal.Event("cu_family_in_person_audience",
                "native_hero="+person.StringId+
                "|day="+day+
                "|relation_before="+before+
                "|relation_after="+after+
                "|native_action_verified="+(after>before)+
                "|personal_visit_recorded=true");
        }
        private bool CanFamilyGift()
        {
            return SpeakingWithAdultFamily() &&
                Hero.MainHero.Gold>=100 &&
                householdDuties.CanGift(
                    Hero.OneToOneConversationHero.StringId,
                    CurrentHouseholdDay());
        }
        private void GiveFamilyGift()
        {
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
            Notify(proof=="confirmed"?
                "You have given "+recipient.Name+
                    " 100 denars from your own purse.":
                "The native gift could not be verified. "+
                    "No gift success was recorded.");
        }
        private void RegisterHouseholdConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine(
                "cu_family_house_audience","hero_main_options",
                "cu_family_house_audience_reply",
                "Let us speak together of our family's affairs.",
                ()=>CanFamilyAudience(),
                ()=>HoldFamilyAudience(),115);
            starter.AddDialogLine(
                "cu_family_house_audience_reply",
                "cu_family_house_audience_reply","hero_main_options",
                "It is good to speak face to face. Let us keep "+
                "faith with our family, and do what we can.",
                null,null,110);
            starter.AddPlayerLine(
                "cu_family_house_gift","hero_main_options",
                "cu_family_house_gift_reply",
                "I have brought 100 denars for your household.",
                ()=>CanFamilyGift(),()=>GiveFamilyGift(),105);
            starter.AddDialogLine(
                "cu_family_house_gift_reply",
                "cu_family_house_gift_reply","hero_main_options",
                "Your offer is received. May our house stand firm.",
                null,null,100);

            foreach(var officeName in new[]{"Steward","Captain","Envoy"})
            {
                string office=officeName;
                starter.AddPlayerLine(
                    "cu_household_appoint_"+office.ToLowerInvariant(),
                    "hero_main_options","cu_household_appoint_reply_"+
                    office.ToLowerInvariant(),
                    "I appoint you "+office+
                    " of my household. Will you serve?",
                    ()=>CanAppointHouseholdCompanion(office),
                    ()=>AppointHouseholdCompanion(office),100);
                starter.AddDialogLine(
                    "cu_household_appoint_reply_"+
                        office.ToLowerInvariant(),
                    "cu_household_appoint_reply_"+
                        office.ToLowerInvariant(),
                    "hero_main_options",
                    "I accept this duty to our house. "+
                    "Your own clan command remains unchanged.",
                    null,null,100);
            }
            starter.AddPlayerLine(
                "cu_household_duty_report","hero_main_options",
                "cu_household_duty_report_reply",
                "Give me your report on the duty I assigned.",
                ()=>CanReportHouseholdDuty(),
                ()=>RecordHouseholdDuty(),110);
            starter.AddDialogLine(
                "cu_household_duty_report_reply",
                "cu_household_duty_report_reply","hero_main_options",
                "The work is witnessed. Our household ledger "+
                "shall carry this report.",
                null,null,100);
        }
        private void ShowHouseholdDutyLedger()
        {
            var clan=Clan.PlayerClan;
            string text="";
            foreach(var office in new[]{"Steward","Captain","Envoy"})
            {
                string id=householdDuties.AssignedId(office);
                var hero=ResolveCampaignHero(id);
                bool present=hero!=null && hero.IsAlive &&
                    clan!=null && hero.CompanionOf==clan &&
                    hero.Clan==clan;
                text+=office+": "+(id.Length==0?"vacant":
                    present?hero.Name.ToString():
                    "former holder unavailable")+
                    " | witnessed reports: "+
                    householdDuties.ReportCount(office)+"\n";
            }
            text+="\nSpeak with one of your actual companions "+
                "in person to appoint an officer. "+
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
