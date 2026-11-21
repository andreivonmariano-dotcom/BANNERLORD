using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaUnified
{
    /// <summary>
    /// Short-lived live scene for a real traveling household. Campaign
    /// Heroes stay with their native parties. Their scene agents are
    /// temporary visual representations, never new campaign records.
    /// No combat, no inventory mutation and no invented children.
    /// </summary>
    public sealed class CUHouseholdCampMissionLogic : MissionLogic
    {
        private readonly List<Hero> participants;
        private const BodyFlags GroundFlags=(BodyFlags)544321929;
        public CUHouseholdCampMissionLogic(List<Hero> actualPartyHeroes)
        {
            participants=actualPartyHeroes??new List<Hero>();
        }
        public override void AfterStart()
        {
            base.AfterStart();
            try
            {
                Mission.SetMissionMode(MissionMode.Stealth,true);
                Vec3 min,max;
                Mission.Scene.GetBoundingBox(out min,out max);
                Vec3 center=(min+max)*0.5f;
                center.z=Mission.Scene.GetGroundHeightAtPosition(
                    center,GroundFlags);
                var player=Hero.MainHero;
                if(player==null || player.CharacterObject==null)
                {
                    DiagnosticJournal.Event("cu_household_camp_scene",
                        "result=NO_NATIVE_MAIN_HERO");
                    return;
                }
                var party=PartyBase.MainParty;
                var playerAgent=SpawnActualHero(player,party,center,
                    Vec2.Forward,AgentControllerType.Player);
                if(playerAgent==null)
                {
                    DiagnosticJournal.Event("cu_household_camp_scene",
                        "result=PLAYER_AGENT_FAILED");
                    return;
                }
                int count=0;
                foreach(var person in participants)
                {
                    if(person==null || person==player ||
                        person.CharacterObject==null ||
                        !person.IsAlive || !person.IsActive ||
                        person.IsPrisoner || person.Age<18f ||
                        count>=8)continue;
                    var position=center;
                    double angle=(count+1)*Math.PI/4.0;
                    position.x+=(float)(5.0*Math.Cos(angle));
                    position.y+=(float)(5.0*Math.Sin(angle));
                    position.z=Mission.Scene.GetGroundHeightAtPosition(
                        position,GroundFlags);
                    var toward=new Vec2(center.x-position.x,
                        center.y-position.y);
                    toward.Normalize();
                    if(SpawnActualHero(person,party,position,toward,
                        AgentControllerType.AI)!=null)count++;
                }
                DiagnosticJournal.Event("cu_household_camp_scene",
                    "result=REAL_MISSION_SPAWNED"+
                    "|companions="+count+
                    "|native_party_roster_only=true"+
                    "|campaign_characters_not_created=true");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_household_camp_mission_spawn_failed",error);
            }
        }

        private Agent SpawnActualHero(Hero person,PartyBase party,
            Vec3 position,Vec2 direction,AgentControllerType controller)
        {
            if(person==null || party==null)return null;
            try
            {
                var data=new AgentBuildData(person.CharacterObject)
                    .InitialPosition(in position)
                    .InitialDirection(in direction)
                    .NoHorses(true)
                    .CivilianEquipment(true)
                    .TroopOrigin(new PartyAgentOrigin(party,
                        person.CharacterObject,-1,default,false,false))
                    .Controller(controller);
                var agent=Mission.SpawnAgent(data,false);
                if(agent!=null && controller==AgentControllerType.AI)
                {
                    agent.AddComponent(new CommonAIComponent(agent));
                    agent.AddComponent(new HumanAIComponent(agent));
                }
                return agent;
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_household_camp_native_agent_failed",error);
                return null;
            }
        }
        public override InquiryData OnEndMissionRequest(
            out bool canPlayerLeave)
        {
            canPlayerLeave=true;
            return null;
        }
    }
}
