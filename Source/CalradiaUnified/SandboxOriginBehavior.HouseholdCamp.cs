using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using SandBox.GauntletUI.Missions;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        public static bool EnterCampFromPartyDossier()
        {
            var campaign=Campaign.Current;
            var behavior=campaign==null?null:
                campaign.GetCampaignBehavior<SandboxOriginBehavior>();
            if(behavior==null || !behavior.CanSetUpNativeCamp())
            {
                Notify("You can set up camp only with your "+
                    "own party on safe land, outside any settlement.");
                return false;
            }
            // Leave the native Party UI before changing map menus.
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
                !Hero.MainHero.IsWounded &&
                Hero.MainHero.CurrentSettlement==null &&
                Settlement.CurrentSettlement==null &&
                party!=null && party.LeaderHero==Hero.MainHero &&
                party.MemberRoster!=null &&
                !party.IsCurrentlyAtSea &&
                Mission.Current==null &&
                party.MapEvent==null;
        }
        private List<Hero> PresentCampHeroes()
        {
            var party=MobileParty.MainParty;
            var result=new List<Hero>();
            if(party==null || party.MemberRoster==null)
                return result;
            foreach(var e in party.MemberRoster.GetTroopRoster())
            {
                if(!e.Character.IsHero || e.Number<=0)continue;
                var hero=e.Character.HeroObject;
                if(hero==null || hero==Hero.MainHero ||
                    !hero.IsAlive || !hero.IsActive ||
                    hero.IsPrisoner || hero.Age<18f ||
                    hero.PartyBelongedTo!=party ||
                    result.Contains(hero))continue;
                result.Add(hero);
                if(result.Count>=8)break;
            }
            return result;
        }
        private static string NativeCampSceneName(MobileParty party)
        {
            if(party==null || Campaign.Current==null)return "";
            var model=Campaign.Current.Models.SceneModel;
            if(model==null)return "";
            try
            {
                var method=model.GetType().GetMethod(
                    "GetBattleSceneForMapPosition",
                    new[]{party.Position.GetType()});
                if(method!=null)
                {
                    var result=method.Invoke(model,
                        new object[]{party.Position}) as string;
                    if(!String.IsNullOrWhiteSpace(result))
                        return result;
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_camp_battle_scene_lookup_failed",error);
            }
            // The fallback is a REAL game-selected conversation scene,
            // not a fabricated or hard-coded scene path.
            return model.GetConversationSceneForMapPosition(
                party.Position)??"";
        }
        private void OpenNativeHouseholdCamp()
        {
            if(!CanSetUpNativeCamp())return;
            var party=MobileParty.MainParty;
            string scene=NativeCampSceneName(party);
            if(String.IsNullOrWhiteSpace(scene))
            {
                Notify("No native outdoor scene is available here. "+
                    "Your campaign party remains unchanged.");
                return;
            }
            var people=PresentCampHeroes();
            try
            {
                var rec=new MissionInitializerRecord(scene)
                {
                    PlayingInCampaignMode=true,
                    AtmosphereOnCampaign=Campaign.Current.Models
                        .MapWeatherModel.GetAtmosphereModel(party.Position),
                    SceneLevels="",
                    DecalAtlasGroup=2
                };
                DiagnosticJournal.Event("cu_household_camp_entry",
                    "scene="+scene+
                    "|native_roster_heroes="+people.Count+
                    "|player_party_unchanged=true"+
                    "|physical_mission_attempted=true");
                MissionState.OpenNew("CUHouseholdCamp",rec,
                    _=>CampMissionBehaviors(people),true,true);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_household_camp_scene_entry_failed",error);
                Notify("This terrain could not host the household "+
                    "camp. Your campaign remains on the map.");
            }
        }
        private MissionBehavior[] CampMissionBehaviors(
            List<Hero> people)
        {
            return new MissionBehavior[]{
                new MissionOptionsComponent(),
                new CampaignMissionComponent(),
                new MissionConversationLogic(),
                new MissionMainAgentController(),
                new CUHouseholdCampMissionLogic(people),
                new MissionGauntletSingleplayerEscapeMenu(false),
                new MissionGauntletOptionsUIHandler(),
                new MissionGauntletPhotoMode(),
                new MissionGauntletLeaveView(),
                new MissionSingleplayerViewHandler(),
                new MissionGauntletAgentStatus(),
                new MissionMainAgentEquipDropView(),
                new MissionGauntletMainAgentEquipmentControllerView(),
                new MissionGauntletConversationView(),
                new MissionGauntletNameMarkerView(),
                new MissionGauntletMainAgentControlModeView()
            };
        }
        private void RegisterHouseholdCampMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_household_camp",
                "Your household gathers beside your own "+
                "banner. Only companions traveling in your "+
                "actual party are present. Children and "+
                "relatives staying elsewhere are not recalled.",
                args=>{});
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_walk",
                "Walk among my traveling household",
                args=>{
                    args.optionLeaveType=
                        GameMenuOption.LeaveType.Mission;
                    return CanSetUpNativeCamp();
                },
                args=>OpenNativeHouseholdCamp(),false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_roll",
                "Ask who is gathered beneath my banner",
                args=>CanSetUpNativeCamp(),
                args=>{
                    var people=PresentCampHeroes();
                    ShowNativeDetail("Your traveling household",
                        people.Count==0?
                        "No adult companion Heroes are physically "+
                        "traveling in your current party.":
                        String.Join("\n",people.Select(p=>
                            p.Name+" | "+DossierOfficerLabel(p)))+
                        "\nThese are native party members, "+
                        "not summoned family or new NPCs.");
                },false);
            starter.AddGameMenuOption("cu_household_camp",
                "cu_household_camp_leave",
                "Break camp and return to the campaign",
                args=>true,
                args=>GameMenu.ExitToLast(),true);
        }
    }
}
