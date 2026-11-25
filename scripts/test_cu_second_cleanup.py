#!/usr/bin/env python3
"""Current retirement and contextual gameplay gates (source only)."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda n:(src/n).read_text(encoding="utf-8-sig")
excluded={x.get("Remove") for x in ET.parse(src/"CalradiaUnified.csproj").getroot().iter("Compile")}
retired={"SandboxOriginBehavior.PeerSocial.cs","SandboxOriginBehavior.StoryDirector.cs","SandboxOriginBehavior.HouseholdCamp.cs","CUHouseholdCampScreen.cs"}
assert retired <= excluded
compiled={p.name:read(p.name) for p in src.glob("*.cs") if p.name not in excluded}
blob="\n".join(compiled.values())
for symbol in ("ObservePeerSocialDaily","ObserveStoryDirector","ObservePeerStory","AnswerStory","RegisterStoryDetailMenus","ProbeNativeGroupConversationApi","CUHouseholdCampScreen","OpenIllustratedHouseholdCamp","EnterCampFromPartyDossier"):
    assert not re.search(r"\b"+symbol+r"\b",blob),symbol
origin=read("SandboxOriginBehavior.cs")
shim=read("SandboxOriginBehavior.LegacySocialMigration.cs")
for key in ("CU_peer_social_1","CU_story_director_1","CU_peer_initiative_1","CU_gathering_plan_1"):
    assert 'SyncData("'+key+'"' in origin
assert "peerSocial.Serialize()" in origin and "SandboxPeerSocialState.Parse(peerRecords)" in origin
assert "storyDirector.Serialize()" in origin and "SandboxStoryDirectorState.Parse(story)" in origin
assert "RegisterHouseholdCampMenus(starter)" in origin and 'GameMenu.ExitToLast()' in shim
assert "CanSetUpNativeCamp(){return false;}" in shim and shim.count("AddGameMenuOption(")==1
assert "CUHouseholdCamp.xml" in (root/"scripts"/"Package-Compiled.ps1").read_text()
assert 'LoadMovie("CUHouseholdCamp"' not in blob
assert "SettingsCatalog.IsRuntimeEditable(key)" in read("Settings.cs")
assert "public static bool IsRuntimeEditable" in read("SettingsCatalog.cs")
assert "public bool EnableArmyWarnings" in read("CUBUTRMCMSettings.cs")
depth=read("SandboxOriginBehavior.LivingDialogueDepth.cs")
house=read("SandboxOriginBehavior.HouseholdDuties.cs")
pet=read("SandboxOriginBehavior.PetitionConversations.cs")
assert "personalTopicPlayerId==Hero.MainHero.StringId" in depth
for ident in ("cu_family_visit_from_topic","cu_family_gift_from_topic"):
    assert ident in house
assert 'personalTopicName=="Family" && CanFamilyGift()' in house
assert '"|payer_before="+payerBefore' in house
assert '"|recipient_after="+recipient.Gold' in house
assert '"cu_petition_hear_concern","cu_personal_followup_choices"' in pet
assert 'personalTopicName=="Concerns" && SpeakingWithPendingPetitioner()' in pet
print("PASS SOURCE ONLY: 4 retired social/camp adapters excluded, historical state and safe exit retained, settings write allowlist and exact-pair contextual choices.")
