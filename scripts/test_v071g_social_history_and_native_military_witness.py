#!/usr/bin/env python3
"""v0.71g native authority audit for the household chronicle,
real social history and native kingdom/army appointment witnesses.
This is a static source gate, not a game-played acceptance test.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
origin=read("SandboxOriginBehavior.cs")
camp=read("SandboxOriginBehavior.HouseholdCamp.cs")
camp_ui=read("CUHouseholdCampScreen.cs")
social=read("SandboxOriginBehavior.SocialRecords.cs")
social_state=read("SandboxSocialBondState.cs")
personal=read("SandboxOriginBehavior.PetitionStories.cs")
military=read("SandboxOriginBehavior.NativeMilitaryWitness.cs")
military_state=read("SandboxNativeMilitaryWitnessState.cs")
orders=read("SandboxOriginBehavior.KnightServiceOrders.cs")
tests=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text()
project=(root/"Tests"/"PolicySmoke"/"PolicySmoke.csproj").read_text()
movie=ET.parse(root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"/
    "CUHouseholdCamp.xml").getroot()
buttons={x.get("Command.Click") for x in movie.iter("ButtonWidget")
         if x.get("Command.Click")}
assert "ExecuteChronicle" in buttons
assert 'public void ExecuteChronicle()' in camp_ui
assert 'SetSection("Chronicle"' in camp_ui
assert 'CampChronicleText()' in camp
assert 'SocialBondDescription(person)' in camp
assert 'CampChronicleText(),InspectTravelingHouseholdMember' in camp
assert 'CareerChronicleDescription()' in camp
assert 'petitionStories.Stories.Where(' in social
assert 'militaryWitness.ArmyCommandDay>=0' in social

assert 'CU_v071g_social_bonds' in origin
assert 'CU_v071g_native_military_witness' in origin
assert 'this, ObserveNativeMilitaryAppointmentsDaily);' in origin
assert 'sandbox' not in social_state.lower() or (
    'native' in social_state.lower())
assert 'nativeRelation<=-30' in social_state
assert 'bond.Declined>=2' in social_state
assert 'bond.Negative<0' in social_state
assert 'nativeRelation>=45' in social_state
assert 'bond.Supported>=2' in social_state
assert 'socialBonds.RecordPetition(' in personal
assert 'socialBonds.RecordFollowUp(' in personal
assert 'SandboxSocialBondState.Parse(bonds)' in origin
assert 'ActualRelation' not in social_state or (
    'RecordPetition' in social_state)
assert 'HeroCreator.Create' not in social+social_state
assert 'ChangeRelationAction' not in social+social_state

assert 'knightServiceOrders.RoyalHearingDay<0' in military
assert 'clan.Kingdom!=patron.Clan.Kingdom' in military
assert 'clan.IsUnderMercenaryService' in military
assert 'party.Army.LeaderParty==party' in military
assert 'party.LeaderHero==Hero.MainHero' in military
assert 'militaryWitness.Observe(' in military
assert 'militaryWitness=SandboxNativeMilitaryWitnessState.Parse(' in origin
assert 'nativeVassal' in military_state
assert 'nativeArmyCommander' in military_state
assert 'KingdomId' in military_state
assert 'ObserveNativeMilitaryAppointmentsDaily()' in military
assert '"cu_knight_lawful_vassalage_inquiry"' in orders
assert 'KnightCanDiscussNativeVassalage()' in orders
assert 'ordinary terms for joining the realm' in orders
for forbidden in ("ChangeKingdomAction","ChangeClanLeaderAction",
                  "ChangeOwnerOfSettlementAction","Army.Create",
                  "HeroCreator.Create","MissionState.OpenNew"):
    assert forbidden not in social+social_state+military+military_state+orders,forbidden

for name in ("SandboxSocialBondState.cs",
             "SandboxNativeMilitaryWitnessState.cs"):
    assert 'Link="'+name+'"' in project
assert 'social trust needs repeated help and actual relations' in tests
assert 'real native vassalage precedes acknowledged army command' in tests
assert 'native appointment receipts persist and reject tampering' in tests
print("PASS v0.71g STATIC: five-folio 2D camp, grounded native "+
      "social records, native vassal and army leadership witness, "+
      "lawful kingdom entry advice and no fabricated appointment.")
