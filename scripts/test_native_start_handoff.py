#!/usr/bin/env python3
"""v0.20 static native-start contract audit. Does NOT claim a Bannerlord game test."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
source = root / "Source/CalradiaUnified"
overlay = (source / "SandboxCreatorOverlay.cs").read_text()
roles = (source / "SandboxNativeStartOptions.cs").read_text()
role_matrix = (source / "SandboxRoleConsistencyPolicy.cs").read_text()
profile = (source / "SandboxOriginProfile.cs").read_text()
behavior = (source / "SandboxOriginBehavior.cs").read_text()
family = (source / "SandboxFamilyCreator.cs").read_text()

# Regression #43: v0.19 always started with unchanged native options.
assert 'SandboxNativeStartOptions.TryPrepare(profile,out options,out reason)' in overlay
assert 'options.GetChangedOptions()' in overlay
assert 'MBGameManager.StartNewGame(new CUSandboxNoIntroGameManager(' in overlay
assert 'SandboxRoleConsistencyPolicy.NativeType(role)' in roles
assert 'case "Knight":' in role_matrix and 'case "Lord":return "vassal";' in role_matrix
assert 'case "King":return "king";' in role_matrix
assert 'case "Soldier":return "mercenary";' in role_matrix
assert 'StartingKingdomId' in profile
assert 'StartingKingdomId' in overlay
assert 'KingdomId' in roles and 'StartType' in roles
assert 'if (RequiresKingdom(type))' in roles
assert 'Select a starting kingdom' in roles
assert 'Admiral' in role_matrix and 'fleetadmiral' in role_matrix

# Advanced start settings are part of the CU editor, not a hidden vanilla picker.
for key in ('StartingScenario', 'ScenarioFaction1', 'ScenarioFaction2',
            'ScenarioVariant', 'StartBanditSurge', 'StartCivilUnrest',
            'StartRecruitShortage', 'StartSwiftTravel', 'StartFastMode',
            'WorldSeed'):
    assert key in profile, key
    assert key in overlay, key
for native in ('Scenario', 'LastStandKingdomId', 'InvasionScenarioFactionId',
               'TwoFactionWarFaction1Id', 'TwoFactionWarFaction2Id',
               'UnitedEmpireUnifierKingdomId', 'AlternativeCalradiaVariantId'):
    assert native in roles, native

# Native ASO applies player start at phase 8. CU must wait for the last phase.
assert 'OnCharacterCreationIsOverEvent.AddNonSerializedListener' in behavior
assert 'if (phase != 9' in behavior
assert 'characterCreationComplete = true' in behavior
assert 'if (!characterCreationComplete || !startedNewGame' in behavior
early = behavior.split('private void NewGameCreated(', 1)[1].split('private void CharacterCreationCompleted', 1)[0]
assert 'TryApplyOnce();' not in early
assert 'SandboxFamilyCreator.TryCreate(profile,hero)' in behavior

# Family fields now arm real family creation, but incomplete configurations fail.
assert 'candidate.RealFamilyEnabled = candidate.SpouseEnabled ||' in overlay
assert 'candidate.ChildCount > 0 && !candidate.SpouseEnabled' in overlay
assert 'candidate.SiblingCount > 0' in overlay
assert 'MarriageAction.Apply(player,spouseHero,false)' in family
assert 'HeroCreator.CreateSpecialHero' in family
assert 'cu_family_creation_status' in behavior
print('PASS: native scenario routing, kingdom checks, post-phase-8 application and family creation contract (STATIC ONLY)')
