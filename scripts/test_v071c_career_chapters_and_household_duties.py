#!/usr/bin/env python3
"""v0.71c source wiring gate: native meetings and saved three-part careers.

Pure-state behavior is exercised by Tests/PolicySmoke/Program.cs.
A green source gate is not in-game proof of dialogue or UI behavior.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
origin=read("SandboxOriginBehavior.cs")
duties=read("SandboxOriginBehavior.HouseholdDuties.cs")
chapter=read("SandboxOriginBehavior.CareerChronicle.cs")
office=read("SandboxHouseholdDutyState.cs")
career=read("SandboxCareerChronicleState.cs")
menu=read("SandboxOriginBehavior.LivingMenus.cs")
house=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
screen=read("CUNobleDossierScreen.cs")
project=(root/"Tests"/"PolicySmoke"/"PolicySmoke.csproj").read_text()
for field in ("CU_v071c_household_duties","CU_v071c_career_chronicle"):
    assert field in origin,field
for name in ("RegisterHouseholdConversations(starter);",
             "RegisterCareerChronicleMenus(starter);",
             "this, ObserveCareerChronicleDaily);"):
    assert name in origin,name
for p in ("SandboxHouseholdDutyState.cs",
          "SandboxCareerChronicleState.cs"):
    assert 'Link="'+p+'"' in project,p
assert '"cu_career_chronicle"' in chapter
assert '"cu_career_chronicle_entry"' in chapter
assert 'NativeHeroDisplayName(' not in office
assert 'ChangeKingdomAction' not in office+career+duties+chapter
assert 'ChangeOwnerOfSettlementAction' not in office+career+duties+chapter
assert 'ChangeClanLeaderAction' not in office+career+duties+chapter
assert 'HeroCreator.Create' not in office+career+duties+chapter
assert 'CurrentCareerRole()' in chapter
assert 'knightEscort.CompletedEscorts>=2' in chapter
assert 'serviceCareer.VerifiedExternalEnlistment' in chapter
assert 'serviceCareer.ExternalInsideHostParty' in chapter
assert 'clan.IsUnderMercenaryService' in chapter
assert 'royalCouncil.Meetings>=3' in chapter
assert 'clan.Settlements.Count>=2' in chapter
assert 'native_state_verified=true' in chapter
assert 'ChapterObjective' in career
for role in ("Wanderer","Outlaw","Mercenary","Soldier",
             "Knight","Lord","King"):
    assert 'case "'+role+'":' in chapter
    assert 'case "'+role+'":' in career
assert 'familyAudience' in office and 'familyGift' in office
assert 'FamilyCooldownDays=30' in office
assert 'RecordGiftAttempt' in duties
assert 'GiftResult(' in duties
assert 'GiveGoldAction.ApplyBetweenCharacters(' in duties
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in duties
assert 'ReportCount(office)' in duties
assert 'party.MemberRoster.GetTroopCount(' in duties
assert 'speaker.CompanionOf==clan' in duties
assert 'speaker.Age>=18f' in duties
assert 'speaker.Spouse==player' in duties
assert 'speaker.Mother==player || speaker.Father==player' in duties
assert 'householdDuties.RecordAudience' in duties
assert 'householdDuties.RecordGiftAttempt' in duties
assert 'HouseholdOfficeFor(person.StringId)' in house
assert 'officerLabelAction(officer)' in screen
assert '"cu_household_duty_ledger"' in menu
assert '"cu_family_native_audiences"' in menu
assert 'CareerChronicleDescription()' in house
assert 'ReviewDossierCalling' in house
print("PASS v0.71c STATIC: officer appointments, real family meetings, "+
      "save-backed seven-role chapters, native campaign evidence and "+
      "no counterfeit salary, clan or kingdom actions.")
