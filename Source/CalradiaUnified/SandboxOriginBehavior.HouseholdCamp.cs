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
                "Who is traveling under my banner?",
                args=>CanSetUpNativeCamp(),
                args=>ShowNativeDetail("Camp | The traveling household",
                    CampRollText()),false);
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
                "cu_household_camp_petitions",
                "Read private correspondence",
                args=>CanSetUpNativeCamp(),
                args=>HearDossierPetition(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_leave",
                "Break camp and return to the campaign",
                args=>true,
                args=>GameMenu.ExitToLast(),true);
        }
    }
}
