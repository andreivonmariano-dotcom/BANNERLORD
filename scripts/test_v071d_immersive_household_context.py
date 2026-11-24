#!/usr/bin/env python3
"""v0.71d. UI access and native face-to-face petition safety.

This script checks source contracts only. It cannot validate the
Bannerlord v1.5.7 conversation graph or click the in-game UI.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
origin=read("SandboxOriginBehavior.cs")
menu=read("SandboxOriginBehavior.LivingMenus.cs")
dossier=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
native=read("CUBUTRNativeScreens.cs")
petition=read("SandboxOriginBehavior.PetitionConversations.cs")
proof=read("SandboxNativeEffectProof.cs")
smoke=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text(
    encoding="utf-8")

assert "RegisterPrivatePetitionConversations(starter);" in origin

# Clan and Party still provide portable read-only household access.
assert '[PrefabExtension("Clan",' in native
assert '[PrefabExtension("Party",' in native
assert 'OpenFromNativeBUTRScreen()' in native
assert 'b.ShowStartingHouseholdStatus();' in dossier

# Settlement actions belong in a genuine owned court.
assert '"Attend to local affairs"' in menu
assert 'Settlement.CurrentSettlement.OwnerClan==' in menu
assert 'Clan.PlayerClan' in menu
assert '!Settlement.CurrentSettlement.IsUnderSiege' in menu
assert '"cu_birth_family_council_entry_"' in menu
assert 'RecordedBirthClan()' in menu
assert 'claimReturnMenu=from;' in menu
assert 'GameMenu.SwitchToMenu(claimReturnMenu)' in menu

# Preserve legacy menu ID and a return route, not permanently hidden actions.
assert 'starter.AddGameMenu("cu_people_requests"' in menu
assert '"cu_people_requests_back"' in menu
for key in ("cu_people_accept","cu_people_decline","cu_people_open_requests"):
    assert '"'+key+'"' not in menu,key
assert 'Hero.OneToOneConversationHero!=actor' in menu

# Remote dossier is an index of the ACTUAL native actor's whereabouts.
assert 'ResolveCampaignHero(' in dossier
assert 'cu_petition_ledger_consulted' in dossier
assert 'remote_payment_or_relation_action=false' in dossier
assert 'OfferLocalCharacterRequest(Hero.MainHero)' not in dossier
assert 'AnswerCharacterRequest(true)' not in dossier
assert 'AnswerCharacterRequest(false)' not in dossier
assert 'ConfirmCharacterSupport()' not in dossier
assert 'Text="Correspondence"' in (
    root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"/
    "CUNobleDossier.xml").read_text(encoding="utf-8")

# Every change is made during a real Hero conversation, not by a menu.
assert 'Hero.OneToOneConversationHero' in petition
assert 'agencyDecisions.PendingFor(actor.StringId)!=null' in petition
assert 'RequestedCharacter(actor.StringId)==actor' in petition
assert 'RequestedCharacter(actor.StringId)==actor' in petition
assert 'SamePlace(player,actor)' in petition
assert 'actor.Age<18f' in petition
assert 'SandboxAgencyDecisionPolicy.ChooseInitiative(' in petition
assert 'agencyDecisions.Enqueue(actor.StringId,kind)' in petition
for text in ("cu_petition_hear_pending","cu_petition_invite",
             "cu_petition_recognition","cu_petition_household",
             "cu_petition_livelihood","cu_petition_accept",
             "cu_petition_refuse","cu_petition_defer"):
    assert '"'+text+'"' in petition,text
assert 'Hero.MainHero.Gold>=cost' in petition
assert 'AnswerCharacterRequest(true)' in petition
assert 'AnswerCharacterRequest(false)' in petition
assert 'Hero.MainHero.Gold<cost' in petition

# Engine-modified real relationship gains can exceed requested +1.
assert 'RelationDirectionObserved(' in proof
assert 'bool exactRelation=SandboxNativeEffectProof.RelationConfirmed(' in menu
assert 'SandboxNativeEffectProof.RelationDirectionObserved(' in menu
assert 'relation_exact' in menu
assert 'actual_relation_delta' in menu
assert 'RelationDirectionObserved(20,22,1)' in smoke
assert 'RelationDirectionObserved(20,18,1)' in smoke

print("PASS v0.71d STATIC: court belongs to native owned estates, "+
      "birth-family claims remain reachable, portable native Clan/Party "+
      "dossiers are read-only, petitions use real conversations, and "+
      "modified native relation gains are reported accurately.")

# Live crash containment: map, dossier and settlement routes use native inquiries.
affairs=(src/"SandboxOriginBehavior.Affairs.cs").read_text(encoding="utf-8")
assert 'ScreenManager.PushScreen(new CUAffairsScreen' not in affairs
assert 'b.ShowAffairsNativePage(all,0)' in affairs
assert 'InformationManager.ShowInquiry(new InquiryData(title,body,true,more || unread,' in affairs
assert '"Close",unread?(more?"Mark read & next":"Mark read")' in affairs
assert 'characterAgendas.MarkNewsRead(item.HeroId,Hero.MainHero.StringId,item.Day)' in affairs
assert 'if(RoyalAudienceAvailableToday())count++;' in affairs
assert 'store.SyncData("CU_news_read_1",ref newsReads);' in origin
assert 'if(PendingAffairsGathering(day))count++;' in affairs
assert 'bool pending=PendingAffairsGathering(day);' in affairs
assert 'gatheringPlan.Owner==Hero.MainHero.StringId' in affairs
assert '"cu_people_affairs_journal"' in menu
print("PASS native correspondence containment and shared gathering notification predicate. SOURCE ONLY.")

assert 'agencyDecisions.SelectPending(actor.StringId)' in menu
assert 'agencyDecisions.PendingCount' in affairs
assert 'foreach(var request in agencyDecisions.PendingRequests)' in menu
assert 'CU_petition_recipient_1' in origin
assert 'MBInformationManager.ShowMultiSelectionInquiry' in affairs
assert '"cu_people_affairs_list"' in menu
assert 'new InquiryElement(index,item.Title+" | "+item.Status,null)' in affairs
