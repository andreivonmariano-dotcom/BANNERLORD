#!/usr/bin/env python3
"""v0.28 family residence: source-only checks. Gameplay/save test required."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
origin=(src/"SandboxOriginBehavior.cs").read_text()
profile=(src/"SandboxOriginProfile.cs").read_text()
overlay=(src/"SandboxCreatorOverlay.cs").read_text()
home=(src/"SandboxFamilyHome.cs").read_text()
policy=(src/"SandboxFamilyHomePolicy.cs").read_text()
family=(src/"SandboxFamilyCreator.cs").read_text()

assert "public string FamilyHomeMode" in profile
assert "public string FamilyHomeId" in profile
assert "FamilyHomeMode=\" + FamilyHomeMode" in profile
assert "FamilyHomeId=\" + FamilyHomeId" in profile
assert "FamilyHomeMode=\"OwnedFief\";" in profile
assert "FamilyHomeId=\"\";" in profile
assert '"FamilyHomeMode"' in overlay
assert '"FamilyHomeId"' in overlay
assert 'SandboxFamilyHomePolicy.Modes' in overlay
assert 'profile.FamilyHomeMode=="SpecificOwned"' in overlay
assert "sandbox_creator_family_plan" in overlay
assert 'string familyHome = family.StartsWith("spouse="' in origin
assert "SandboxFamilyHome.TryPlace(profile,hero,fiefOutcome)" in origin
assert "SandboxFamilyHome.PlaceExistingFamilyAt(" in origin
assert "cu_family_home_status" in origin
assert '"cu_family_residence_here_" + from' in origin
assert "Settlement.CurrentSettlement.OwnerClan ==" in origin

# Settlements must be player-owned fortified locations, never villages.
assert "target.OwnerClan == Clan.PlayerClan" in home
assert "target.IsFortification" in home
assert "target.IsUnderSiege" in home
assert "target.Party.MapEvent" in home
assert '"SpecificOwned"' in home
assert "startingFiefOutcome.StartsWith(\"granted:\"" in home
assert "UpdateLastKnownClosestSettlement(target)" in home
assert 'StringComparer.Ordinal' in home

# Teleport native adult, NEVER teleport a minor or modify its state.
assert 'TeleportHeroAction.ApplyImmediateTeleportToSettlement(' in home
assert 'spouse,target' in home
assert 'child.StayingInSettlement = target;' in home
assert 'var nativeState = child.HeroState;' in home
assert 'if (child.HeroState != nativeState)' in home
assert 'target.HeroesWithoutParty.Contains(child)' in home
assert 'SandboxFamilyHomePolicy.CanPlaceMinor(' in home
assert "child.PartyBelongedTo == null" in home
assert "child.IsChild" in home
assert "AddHeroToPartyAction" not in home
assert "if (child.IsNotSpawned)" in home
assert "child.ChangeState(Hero.CharacterStates.Active)" in home
assert "if (child.IsChild)" in home
assert "child.StayingInSettlement = target;" in home
assert "child.CharacterObject" not in home
assert "HeroCreator.CreateChild(" in family
assert '"cu_family_child_home"' in home
assert '"cu_family_home_result"' in home
assert "PlaceExistingFamilyAt" in home
print("PASS: owned-fief home selection, adult native teleport, persisted minor residence, explicit existing-save option and no baby recruitment (STATIC ONLY)")
