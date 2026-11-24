using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // The camp is a native, flat campaign menu. It never opens a 3D
        // mission, spawns Agents, moves family, or interrupts the map
        // with a scripted cinematic.
        public static bool EnterCampFromPartyDossier()
        {
            var campaign=Campaign.Current;
            var behavior=campaign==null?null:
                campaign.GetCampaignBehavior<SandboxOriginBehavior>();
            if(behavior==null || !behavior.CanSetUpNativeCamp())
            {
                Notify("You may review your traveling household "+
                    "only with your party on safe land outside a settlement.");
                return false;
            }
            if(ScreenManager.TopScreen!=null)
                ScreenManager.PopScreen();
            GameMenu.ActivateGameMenu("cu_household_camp");
            return true;
        }
        private bool CanSetUpNativeCamp()
        {
            var party=MobileParty.MainParty;
            return applied && IsStandaloneSandbox() &&
                Campaign.Current!=null &&
                Hero.MainHero!=null && !Hero.MainHero.IsPrisoner &&
                Hero.MainHero.CurrentSettlement==null &&
                Settlement.CurrentSettlement==null &&
                party!=null && party.LeaderHero==Hero.MainHero &&
                party.MemberRoster!=null &&
                !party.IsCurrentlyAtSea &&
                party.MapEvent==null;
        }
        private List<Hero> PresentCampHeroes()
        {
            var party=MobileParty.MainParty;
            var result=new List<Hero>();
            if(party==null || party.MemberRoster==null)
                return result;
            foreach(var entry in party.MemberRoster.GetTroopRoster())
            {
                if(!entry.Character.IsHero || entry.Number<=0)
                    continue;
                var person=entry.Character.HeroObject;
                if(person==null || person==Hero.MainHero ||
                    !person.IsAlive || !person.IsActive ||
                    person.IsPrisoner || person.Age<18f ||
                    person.PartyBelongedTo!=party ||
                    result.Contains(person))continue;
                result.Add(person);
                if(result.Count>=8)break;
            }
            return result;
        }
        private string CampRollText()
        {
            var people=PresentCampHeroes();
            if(people.Count==0)
                return "There are no adult companion Heroes "+
                    "traveling with this party at present.";
            return String.Join("\n",people.Select(person=>
                person.Name+" | "+DossierOfficerLabel(person)))+
                "\n\nThe names above come from your actual party roster. "+
                "No relatives or officers are summoned from elsewhere.";
        }
        private string CampCorrespondenceText()
        {
            if(!agencyDecisions.HasPending)
                return "No one has a pending private petition. "+
                    "Speak to your actual companions and grown family "+
                    "members to learn what matters to them.";
            var person=ResolveCampaignHero(
                agencyDecisions.PendingHeroId);
            if(person==null || !person.IsAlive)
                return "The recorded petitioner cannot presently "+
                    "be found. No remote answer can be given.";
            var place=person.StayingInSettlement ??
                person.CurrentSettlement;
            string where=place!=null?place.Name.ToString():
                person.PartyBelongedTo!=null?
                    person.PartyBelongedTo.Name.ToString():
                    "location not verified";
            var story=petitionStories.For(person.StringId);
            return person.Name+" seeks "+agencyDecisions.PendingKind+
                ".\nLast known location: "+where+
                "\nPersonal history: "+(story==null?
                    "no earlier outcome":story.Outcome)+
                "\nAnswer only by speaking with this person.";
        }
        private string CampOfficerText()
        {
            var clan=Clan.PlayerClan;
            string summary="";
            foreach(var office in new[]{"Steward","Captain","Envoy"})
            {
                string id=householdDuties.AssignedId(office);
                var person=ResolveCampaignHero(id);
                bool verified=person!=null && person.IsAlive &&
                    clan!=null && person.CompanionOf==clan &&
                    person.Clan==clan;
                summary+=office+": "+(id.Length==0?"vacant":
                    verified?person.Name.ToString():
                    "previous holder unavailable")+
                    " | reports "+householdDuties.ReportCount(office)+
                    "\n";
            }
            return summary+
                "\nMeet your companion in person to grant "+
                "a duty or hear a witnessed report.";
        }
        private string CampServiceText()
        {
            string overview=CareerChronicleDescription();
            if(!knightOath.IsConfigured)return overview;
            var patron=KnightOathPatron();
            return "Sworn patron: "+NativePersonName(patron)+
                "\nRelationship: "+(patron==null?
                    "unavailable":Hero.MainHero.GetRelation(patron)
                        .ToString())+
                " | Escorts: "+knightEscort.CompletedEscorts+
                "\nOrders completed: "+knightServiceOrders.CompletedOrders+
                " | Order: "+knightServiceOrders.Status+
                (knightServiceOrders.Active?
                    " (due day "+knightServiceOrders.DueDay+")":"")+
                "\nHousehold standing: "+
                KnightVerifiedStanding()+
                "\n\n"+overview;
        }
        private void InspectTravelingHouseholdMember(Hero person)
        {
            if(person==null || !PresentCampHeroes().Contains(person))
                return;
            var place=person.StayingInSettlement ??
                person.CurrentSettlement;
            ShowNativeDetail(person.Name+" | Traveling household",
                "Age "+(int)person.Age+
                "\nCurrent duty: "+DossierOfficerLabel(person)+
                "\nNative party: "+MobileParty.MainParty.Name+
                "\nStanding with you: "+
                Hero.MainHero.GetRelation(person)+
                "\n"+SocialBondDescription(person)+
                "\n"+PersonalHistoryText(person)+
                "\nKnown settlement: "+(place==null?
                    "not recorded":place.Name.ToString())+
                "\nThis is an actual living companion in your "+
                "party, not a summoned or fabricated NPC.");
        }
        private void OpenIllustratedHouseholdCamp()
        {
            if(!CanSetUpNativeCamp())return;
            var clan=Clan.PlayerClan;
            var people=PresentCampHeroes();
            try
            {
                ScreenManager.PushScreen(new CUHouseholdCampScreen(
                    people,
                    clan==null?"Under your own banner":
                        "Beneath the banner of "+clan.Name,
                    CampRollText(),CampOfficerText(),
                    CampCorrespondenceText(),CampServiceText(),
                    CampChronicleText(),InspectTravelingHouseholdMember));
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_household_camp_illustrated_fallback",error);
                ShowNativeDetail("The traveling household",
                    CampRollText()+"\n\n"+CampOfficerText());
            }
        }

        private void RegisterHouseholdCampMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_household_camp",
                "At camp, your company gathers around its own banner. "+
                "Consult the traveling roll, consider your service "+
                "and make plans. Meet individual people through "+
                "the game's real conversations.",
                args=>{});
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_roll",
                "Open my illustrated household register",
                args=>CanSetUpNativeCamp(),
                args=>OpenIllustratedHouseholdCamp(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_officers",
                "Review the duties of my companions",
                args=>CanSetUpNativeCamp(),
                args=>ShowHouseholdDutyLedger(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_service",
                "Consider my next duty and service",
                args=>CanSetUpNativeCamp() &&
                    CurrentCareerRole().Length>0,
                args=>ShowNativeDetail("Camp | Present duties",
                    CareerChronicleDescription()),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_agree_charge","Agree on my next duty",
                args=>CanSetUpNativeCamp() &&
                    CanBeginCareerCharge(SandboxCareerChargeState.DefaultKind(SelectedReviewRole())),
                args=>ShowNativeDetail("Career | New charge",
                    AgreeCareerCharge(SandboxCareerChargeState.DefaultKind(SelectedReviewRole()))),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_withdraw_charge","Set aside my current charge",
                args=>CanSetUpNativeCamp() && IsPersonalChargeActive(),
                args=>ShowNativeDetail("Career | Charge",WithdrawCareerCharge()),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_recurring_review",
                "Review recurring career duties",
                args=>CanSetUpNativeCamp() && SelectedReviewRole().Length>0,
                args=>ReviewRecurringCareer(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_petitions",
                "Read private correspondence",
                args=>CanSetUpNativeCamp(),
                args=>OpenCampaignAffairs(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_leave",
                "Break camp and return to the campaign",
                args=>true,
                args=>GameMenu.ExitToLast(),true);
        }
    }
}
