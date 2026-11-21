#!/usr/bin/env python3
"""v0.71f: illustrated 2D camp, grounded NPC voices, earned Knight standing.

Static wiring, prefab geometry and pure-state build gates only.
A green script cannot establish live Bannerlord UI click acceptance.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
ui=root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"
read=lambda p:(src/p).read_text(encoding="utf-8")

camp=read("SandboxOriginBehavior.HouseholdCamp.cs")
screen=read("CUHouseholdCampScreen.cs")
portrait=read("CUFamilyPortraitGallery.cs")
talk=read("SandboxOriginBehavior.PetitionConversations.cs")
story=read("SandboxOriginBehavior.PetitionStories.cs")
voice=read("SandboxPetitionVoicePolicy.cs")
orders=read("SandboxOriginBehavior.KnightServiceOrders.cs")
standing=read("SandboxKnightStandingPolicy.cs")
order_state=read("SandboxKnightServiceOrderState.cs")
house=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
smoke=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text()
policy_project=(root/"Tests"/"PolicySmoke"/
    "PolicySmoke.csproj").read_text()

x=ET.parse(ui/"CUHouseholdCamp.xml").getroot()
assert x.tag=="Prefab"
panels=[w for w in x.iter("Widget") if
    w.get("HorizontalAlignment")=="Center" and
    w.get("VerticalAlignment")=="Center" and
    w.get("WidthSizePolicy")=="Fixed" and
    w.get("HeightSizePolicy")=="Fixed"]
assert len(panels)==1
panel=panels[0]
assert int(panel.get("SuggestedWidth"))<=1216
assert int(panel.get("SuggestedHeight"))<=656
for element in panel.find("Children"):
    if element.get("WidthSizePolicy")=="Fixed" and (
       element.get("HeightSizePolicy")=="Fixed"):
        assert int(element.get("PositionXOffset","0"))+int(
            element.get("SuggestedWidth","0"))<=970
        assert int(element.get("PositionYOffset","0"))+int(
            element.get("SuggestedHeight","0"))<=618

commands={w.get("Command.Click") for w in
    x.iter("ButtonWidget") if w.get("Command.Click")}
assert commands=={"ExecutePeople","ExecuteOfficers","ExecuteLetters",
                  "ExecuteService","ExecuteInspect","ExecuteClose"}
attendees=x.find(".//ListPanel[@DataSource='{Attendees}']")
assert attendees is not None
assert attendees.find("./ItemTemplate/ButtonWidget[@Command.Click='ExecuteInspect']") is not None
assert attendees.find(".//ImageIdentifierWidget[@DataSource='{Portrait}']") is not None
assert int(attendees.get("SuggestedHeight"))>=6*42
assert "MBBindingList<CUFamilyPortraitVM> Attendees" in screen
assert 'new CUFamilyPortraitVM(person,' in screen
assert 'OnPropertyChangedWithValue(SectionBody,' in screen
assert 'layer.LoadMovie("CUHouseholdCamp",data)' in screen
assert 'Input.IsKeyReleased(InputKey.Escape)' in screen
assert 'CharacterCode.CreateFrom(hero.CharacterObject)' in portrait
assert 'ScreenManager.PushScreen(new CUHouseholdCampScreen(' in camp
assert 'person.PartyBelongedTo!=party' in camp
assert 'person.Age<18f' in camp
assert 'InspectTravelingHouseholdMember' in camp
assert 'PresentCampHeroes().Contains(person)' in camp
assert 'OpenIllustratedHouseholdCamp()' in camp
assert 'CampCorrespondenceText()' in camp
assert 'CampOfficerText()' in camp
assert 'CampServiceText()' in camp
assert 'GameMenu.ExitToLast()' in camp
for forbidden in ("MissionState.OpenNew","Mission.SpawnAgent",
                  "AgentBuildData","MissionInitializerRecord",
                  "GiveGoldAction","ChangeKingdomAction",
                  "ChangeOwnerOfSettlementAction"):
    assert forbidden not in camp+screen

for style in ("Guarded","Duty","Ambitious","Warm","Direct"):
    assert '"'+style+'"' in voice
    assert 'PetitionerStyle()=="'+style+'"' in talk or (
        style=="Direct" and 'PetitionerStyle()=="Direct"' in talk)
assert "GetTraitLevel(DefaultTraits.Honor)" in talk
assert "GetTraitLevel(DefaultTraits.Generosity)" in talk
assert "GetTraitLevel(DefaultTraits.Calculating)" in talk
assert "player.GetRelation(speaker)" in talk
assert "characterAgendas.Get(speaker.StringId)" in talk
assert '"cu_petition_personal_reason"' in talk
for kind in ("Recognition","HouseholdAid","LivelihoodAid"):
    assert 'PetitionStoryKind("'+kind+'")' in story
assert 'petitionStories.RecordFollowUp(' in story
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in story

assert 'knightServiceOrders.CompletedOrders' in orders
assert 'knightEscort.CompletedEscorts' in orders
assert 'clan.Renown' in orders
assert 'player.GetRelation(patron)' in orders
assert 'SandboxKnightStandingPolicy.Standing(' in orders
assert 'SandboxKnightStandingPolicy.CanRequestRoyalHearing(' in orders
assert 'patron.Clan.Kingdom.RulingClan' in orders
assert 'lord==rulingClan.Leader' in orders
assert 'knightServiceOrders.RecordRecommendation(' in orders
assert 'knightServiceOrders.RecordRoyalHearing(' in orders
assert 'no_fief_or_royal_promotion_granted=true' in orders
assert 'kingdom_membership_unchanged=true' in orders
assert 'RecommendationDay' in order_state
assert 'RoyalHearingDay' in order_state
assert 'f.Length!=11 && f.Length!=13' in order_state
assert 'KnightVerifiedStanding()' in house
assert 'KnightVerifiedStanding()' in camp

for file in ("SandboxPetitionVoicePolicy.cs",
             "SandboxKnightStandingPolicy.cs"):
    assert 'Link="'+file+'"' in policy_project
assert 'old eleven-field knight saves remain loadable' in smoke
assert 'native-trait dialogue' in smoke
assert 'political recommendation needs actual renown and service' in smoke

print("PASS v0.71f STATIC: 720p-safe Gauntlet camp with real portrait "+
      "tabs, native Hero trait dialogue, renown-and-service gated "+
      "Knight audience and backward-compatible political receipts. "+
      "No scripted 3D cutscenes.")
