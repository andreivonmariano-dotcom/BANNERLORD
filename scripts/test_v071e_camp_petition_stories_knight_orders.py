#!/usr/bin/env python3
"""v0.71e source gates for 2D household camp, petitioner arcs, and Knight orders.

No custom mission, cutscene, generated agent, or physical scene is
permitted for the household interface.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
source=root/"Source"/"CalradiaUnified"
read=lambda filename:(source/filename).read_text(encoding="utf-8")
origin=read("SandboxOriginBehavior.cs")
native=read("CUBUTRNativeScreens.cs")
camp=read("SandboxOriginBehavior.HouseholdCamp.cs")
stories=read("SandboxOriginBehavior.PetitionStories.cs")
petitions=read("SandboxOriginBehavior.PetitionConversations.cs")
agency=read("SandboxOriginBehavior.LivingMenus.cs")
orders=read("SandboxOriginBehavior.KnightServiceOrders.cs")
knight=read("SandboxOriginBehavior.KnightDialogue.cs")
states=[read("SandboxPetitionStoryState.cs"),
        read("SandboxKnightServiceOrderState.cs")]
project=read("CalradiaUnified.csproj")
smoke=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text()
assert 'RegisterHouseholdCampMenus(starter);' in origin
assert 'RegisterKnightServiceOrders(starter);' in origin
assert 'RegisterPetitionStoryConversations(starter);' in origin
assert 'this, ObserveKnightServiceOrderDaily);' in origin
assert 'CU_v071e_personal_petition_stories' in origin
assert 'CU_v071e_native_knight_orders' in origin

assert '[PrefabExtension("Party",' in native
assert 'ExecuteCUCamp()' in native
assert 'Command.Click=\\\"ExecuteCUCamp' in native
assert 'GameMenu.ActivateGameMenu("cu_household_camp")' in camp
assert 'ScreenManager.PopScreen();' in camp
assert 'MobileParty.MainParty' in camp
assert 'CanSetUpNativeCamp()' in camp
assert '!party.IsCurrentlyAtSea' in camp
assert 'party.MapEvent==null' in camp
assert 'IsStandaloneSandbox()' in camp
assert 'party.MemberRoster.GetTroopRoster()' in camp
assert 'person.PartyBelongedTo!=party' in camp
assert 'person.Age<18f' in camp
assert 'GameMenu.ExitToLast()' in camp
assert 'ShowHouseholdDutyLedger()' in camp
assert 'CareerChronicleDescription()' in camp
assert 'HearDossierPetition()' in camp
assert not (source/"CUHouseholdCampMissionLogic.cs").exists()
for forbidden in (
    'MissionState.OpenNew','Mission.SpawnAgent',
    'new AgentBuildData(', 'MissionInitializerRecord',
    'GetBattleSceneForMapPosition', 'GetConversationSceneForMapPosition',
    'HeroCreator.Create', 'ChangeClanLeaderAction',
    'ChangeKingdomAction', 'ChangeOwnerOfSettlementAction',
    'GiveGoldAction', 'ChangeRelationAction'):
    assert forbidden not in camp,forbidden

assert 'RememberPetitionStory(actor,kind);' in petitions
assert 'RememberPetitionStory(hero,kind);' in agency
assert 'ResolvePetitionStory(actor,accept,kind,' in agency
assert 'petitions' not in stories or 'petitionStories.For' in stories
assert 'Hero.OneToOneConversationHero' in stories
assert 'SamePlace(Hero.MainHero,person)' in stories
assert 'petitionStories.RecordFollowUp(' in stories
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in stories
assert 'FollowUpDays=7' in states[0]
assert 'return "face_to_face_story_followup_recorded";' in states[0]

assert 'KnightOathPatron()' in orders
assert 'SpeakingWithSwornPatron()' in orders
assert 'native_patron_service_report_completed' in orders
assert 'KnightOathNativeRelation(patron,2,' in orders
assert 'town.OwnerClan.Kingdom==patron.Clan.Kingdom' in orders
assert 'town.IsTown' in orders
assert 'Hero.MainHero.CurrentSettlement==town' in orders
assert 'MobileParty.MainParty.MemberRoster' in orders
assert '"cu_knight_order_scout"' in orders
assert '"cu_knight_order_muster"' in orders
assert '"cu_knight_order_report"' in orders
assert 'KnightConversationMusterAvailable()' in knight
assert 'PersonalStanding()' in states[1]
assert 'DueDay=day+14' in states[1]
assert 'CompletedOrders' in states[1]
assert 'MissionState.OpenNew' not in orders
assert 'SandboxPetitionStoryState.cs' in (root/"Tests"/"PolicySmoke"/
    "PolicySmoke.csproj").read_text()
assert 'SandboxKnightServiceOrderState.cs' in (root/"Tests"/"PolicySmoke"/
    "PolicySmoke.csproj").read_text()
assert 'petition stories require actual supported actor/kind' in smoke
assert 'scouting needs two distinct native kingdom towns' in smoke
print("PASS v0.71e STATIC: native 2D camp menu for real roster "+
      "Heroes, petitioner face-to-face follow-ups and sworn patron "+
      "scouting/muster orders. Game acceptance remains pending.")
