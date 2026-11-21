#!/usr/bin/env python3
"""v0.71b: seven-role portrait dossier and native-backed decisions.

Static source gate only. Native gameplay still requires in-game clicks,
load/save and screen-resolution evidence.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
movie=root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"/"CUNobleDossier.xml"
read=lambda name:(src/name).read_text(encoding="utf-8")
screen=read("CUNobleDossierScreen.cs")
gallery=read("CUFamilyPortraitGallery.cs")
house=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
menu=read("SandboxOriginBehavior.LivingMenus.cs")
ceremony=read("SandboxOriginBehavior.KnightDialogue.cs")
state=read("SandboxKnightOathState.cs")
prefab=ET.parse(movie).getroot()
assert prefab.tag=="Prefab"
lists={e.get("DataSource"):e for e in prefab.iter("ListPanel")
       if e.get("DataSource")}
for name in ("{FamilyMembers}","{OfficerMembers}"):
    assert name in lists,name
    assert lists[name].find("ItemTemplate/"+
        "ButtonWidget[@Command.Click='ExecuteInspect']") is not None
for action in ("ExecuteOpenFamily","ExecuteHearPetition",
               "ExecuteRoleAction","ExecuteClose"):
    assert any(e.get("Command.Click")==action for e in
               prefab.iter("ButtonWidget")),action
assert 'MBBindingList<CUFamilyPortraitVM> FamilyMembers' in screen
assert 'MBBindingList<CUFamilyPortraitVM> OfficerMembers' in screen
assert 'new CUFamilyPortraitVM(' in screen
assert 'person.IsAlive' in gallery
assert 'public void ExecuteInspect()' in gallery
assert 'CharacterCode.CreateFrom(hero.CharacterObject)' in gallery
assert 'InspectDossierHero' in house
assert 'person.CompanionOf==clan' in house
assert 'player.Children.Contains(person)' in house
assert 'OpenDossierFamilyPortraits' in house
assert 'ScreenManager.PushScreen(new CUFamilyPortraitScreen())' in house
assert 'HearDossierPetition' in house
assert 'DeclineDossierPetition' in house
assert 'OfferLocalCharacterRequest(Hero.MainHero)' in house
assert 'RequestedCharacter()' in house
assert 'ConfirmCharacterSupport()' in house
assert 'AnswerCharacterRequest(false)' in house
assert 'ShowNativeDetail(' in house
assert 'GiveGoldAction.ApplyBetweenCharacters(' in menu
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in menu
assert 'roleEconomy.Started' in menu
assert '"My household and present calling"' in menu
for role in ("Wanderer","Outlaw","Mercenary",
             "Soldier","Knight","Lord","King"):
    assert 'case "'+role+'":' in screen,role
    assert 'case "'+role+'":return' in house,role
for role in ("Wanderer","Outlaw","Mercenary","Soldier"):
    assert 'case "'+role+'":' in house,role
assert 'roleEconomy.CareerStatus' in house
assert 'ReviewDossierCalling' in house
assert 'RoleActionLabel' in screen
assert 'ExecuteRoleAction()' in screen
assert 'clan.IsUnderMercenaryService' in house
assert 'VerifiedExternalEnlistment' in house
assert 'knightOath.LastMusterDay<0' in house
assert 'royalCouncil.IsRecorded' in house
assert 'lordInheritance.PrimaryHeirId' in house
assert 'SpeechOnly' not in ceremony
opening=ceremony.split('"cu_knight_patron_first_audience"',1)[1]
opening=opening.split('starter.AddDialogLine(',1)[0]
assert 'RecordNativePatronAudience()' not in opening
pledge=ceremony.split('"cu_knight_ceremony_pledge"',1)[1]
pledge=pledge.split('starter.AddDialogLine(',1)[0]
assert 'RecordNativePatronAudience()' in pledge
assert 'knightOath.RecordMuster(day,true)' in ceremony
assert 'day>LastMusterDay' in state
assert 'hero_main_options' in ceremony
for banned in ("ChangeKingdomAction","ChangeOwnerOfSettlementAction",
               "ChangeClanLeaderAction","HeroCreator.Create"):
    assert banned not in screen+gallery
print("PASS v0.71b STATIC: seven native-role ledgers, clickable real "+
      "Hero portraits, grounded petition decisions and pledge-only "+
      "first-audience reward. Bannerlord runtime remains unverified.")
