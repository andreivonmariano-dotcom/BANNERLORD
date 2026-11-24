#!/usr/bin/env python3
"""v0.69h full starting-option source audit.

This is structural and policy coverage. It does not start Bannerlord, create
native Heroes, render Gauntlet, or prove that every option worked in-game.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
src = root / "Source" / "CalradiaUnified"
read = lambda name: (src / name).read_text(encoding="utf-8")
profile = read("SandboxOriginProfile.cs")
world = read("CUGuidedCreatorVM.cs")
native = read("SandboxNativeStartOptions.cs")
role = read("SandboxRoleConsistencyPolicy.cs")
contract = read("SandboxStartContract.cs")
preflight = read("SandboxStartingLifePreflight.cs")
overlay = read("SandboxCreatorOverlay.cs")
behavior = read("SandboxOriginBehavior.cs")
noble = read("SandboxNobleStartPolicy.cs")
fief = read("SandboxFiefPolicy.cs")
house = read("SandboxHouseholdLineagePolicy.cs")
patron = read("SandboxServicePatron.cs")
family = read("SandboxFamilyCreator.cs")

roles = ("Wanderer", "Outlaw", "Mercenary", "Soldier", "Recruit",
         "HouseholdKnight", "Knight", "NobleCadet", "Lord", "King",
         "Admiral", "Trader", "Beggar")
scenarios = ("none", "unitedempire", "LastStand", "twofactionwar",
             "InvasionId", "alternativecalradia")
births = ("Independent", "YoungerBrotherOfLord", "YoungerSisterOfLord",
          "SecondSonOfLord", "ThirdSonOfLord", "YoungerBrotherOfKing",
          "YoungerSisterOfKing", "SecondSonOfKing", "ThirdSonOfKing")

def exact_array(body, name, expected):
    match = re.search(
        r"public static readonly string\[\] " + re.escape(name) +
        r"\s*=\s*\{(.*?)\};", body, re.S)
    assert match is not None, f"missing source array {name}"
    values = re.findall(r'"([^"]+)"', match.group(1))
    assert tuple(values) == expected, (name, values)
    return values

exact_array(profile, "Roles", roles)
# The compatibility parser is broader than the actual guided selector.
life = read("SandboxStartingLifePolicy.cs")
exact_array(life, "SevenPaths", ("Wanderer", "Outlaw", "Mercenary",
    "Soldier", "Knight", "Lord", "King"))
fields = world[world.index("private List<Field> BuildFields()"):
               world.index("private bool Available(")]
assert fields.count("choices:SandboxStartingLifePolicy.SevenPaths")==1
for retired in ("KnightOrigin","SoldierEntryRank","NobleBirthPosition","NobleElderHeroId"):
    assert not re.search(r'new Field\([^)]*?"'+retired+r'"',fields,re.S), retired
assert "choices:SandboxKnightStartPolicy.Origins" not in fields
assert "choices:SandboxSoldierEnlistmentPolicy.EntryChoices" not in fields
assert "choices:SandboxNobleStartPolicy.BirthPositions" not in fields
assert world.count("candidate.NormalizeGuidedCareerDraft()")==2
assert 'if(key=="Role" && Array.IndexOf(SandboxStartingLifePolicy.SevenPaths,value)<0)return false;' in world
normalize=profile[profile.index("public bool NormalizeGuidedCareerDraft()"):
                  profile.index("public void ResetForNewCampaign()")]
for token in ('KnightOrigin="SwornHouse";','SoldierEntryRank="Recruit";',
              'NobleBirthPosition="Independent";','NobleElderHeroId="";'):
    assert token in normalize,token
assert "if(Array.IndexOf(SandboxStartingLifePolicy.SevenPaths,Role)<0)return false;" in normalize
assert "profile.NormalizeGuidedCareerDraft();" in world
assert "profile.NormalizeGuidedCareerDraft()" not in behavior
assert 'profile.SoldierGuidedStart &&' in native
assert 'profile.KnightGuidedStart && profile.Role=="Knight"' in native
assert '"default":StartTypeForRole(profile.Role)' in native
assert world.count("profile.IsFixedGuidedKnightField(key)") == 2
assert 'RetinueMode="RoleBased";' in normalize
assert 'FamilyHomeMode="None";' in normalize
assert 'Gold=-1;Influence=-1;Troops=0;' in normalize
assert 'SandboxKnightStartPolicy.ValidateNativeSwornHouse(' in preflight
assert 'SandboxSoldierEnlistmentPolicy.ValidateIndependentStart(' in preflight
assert 'SandboxKingStartPolicy.ValidateNativeCrown(' in preflight
assert 'SandboxLordStartPolicy.ValidateNativeHouse(' in preflight
assert 'SandboxMercenaryStartPolicy.Validate(' in preflight

exact_array(profile, "Scenarios", scenarios)
exact_array(noble, "BirthPositions", births)
exact_array(fief, "Modes", ("Native", "Castle", "Town", "Specific"))

# The World creator exposes the actual native scenario and player roles.
for key in ("StartingScenario", "StartingKingdomId", "ScenarioFaction1",
            "ScenarioFaction2", "ScenarioVariant", "WorldSeed",
            "StartFastMode", "StartBanditSurge", "StartCivilUnrest",
            "StartRecruitShortage", "StartSwiftTravel", "Role",
            "NobleBirthPosition", "ServicePatronHeroId"):
    assert f'"{key}"' in world, key
    assert f'case "{key}":' in profile, key

# Native ASO, NOT an inferred clan title, owns political starts.
assert "SandboxRoleConsistencyPolicy.NativeType(role)" in native
assert "types.SetValue(type);" in native
assert "options.GetChangedOptions()" in overlay
assert "SandboxStartingLifePreflight.Check(" in behavior
assert "no_custom_grants_applied=true" in behavior
for type_id in ("default", "vassal", "king", "mercenary", "fleetadmiral",
                "trader", "outlaw", "beggar"):
    assert f'"{type_id}"' in role, type_id
for key in ("StartType", "KingdomId", "Scenario",
            "UnitedEmpireUnifierKingdomId", "LastStandKingdomId",
            "InvasionScenarioFactionId", "TwoFactionWarFaction1Id",
            "TwoFactionWarFaction2Id", "AlternativeCalradiaVariantId",
            "EnableFastMode", "RisenBanditsMultiplier", "HighRebellion",
            "RecruitmentRate", "IncreasedGlobalMovementSpeed", "Seed"):
    assert f'"{key}"' in native, key

# Two real sessions showed unrelated phase-9 failures: noble elder missing
# and HouseholdKnight native start type mismatch. The latter must now log
# BOTH sides, and an omitted default only matches an independent, fief-free
# native state. Different explicit native types must still be rejected.
assert 'SandboxRoleConsistencyPolicy.NativeStartTypeMatches(' in contract
assert 'NativeStartTypeMatches(expected,actualStartType,' in role
assert 'expected=="default"' in role
assert 'String.IsNullOrEmpty(observed)' in role
assert '!hasKingdom && !rulingClan && fiefs==0' in role
assert '"cu_native_start_preflight_snapshot"' in preflight
assert 'String.IsNullOrEmpty(actualType)?' in preflight
assert 'return "native_start:"+nativeErrors[0];' in preflight
assert 'SandboxCustomLifeRolePolicy.NativeLandlessContract(' in preflight
assert 'SandboxDynasticStart.HasNativeElder(' in preflight
assert 'SandboxServicePatron.HasNativePatron(' in preflight

# House and grant contracts: independent siblings, controlled fief transfers,
# real family and cultural retinue, and non-repeatable phase-9 registration.
assert 'FirstOlderBrotherInIndependentLordHouse(' in house
assert 'SandboxFamilyCreator.TryCreate(profile,hero)' in behavior
assert 'SandboxStartingFief.TryApply(profile,hero)' in behavior
assert 'SandboxServicePatron.TryAssign(' in behavior
assert 'ChangeOwnerOfSettlementAction.ApplyByGift' in read("SandboxStartingFief.cs")
assert 'MarriageAction.Apply(player,spouseHero,false)' in family
assert 'HeroCreator.CreateChild(' in family
assert 'characterCreationComplete = true' in behavior
assert 'applied = true;' in behavior
assert 'HasNativePatron(' in patron
assert 'RetinueReadyForOtherGrants(' in contract
# The player-side evidence reader must anchor the newest guided session,
# not a stale successful legacy creator event.
reporter = (root / "scripts" / "Test-CU-Playthrough.ps1").read_text(
    encoding="utf-8")
assert "'cu_guided_world_start_committed'" in reporter
assert "'cu_guided_house_fortune_committed'" in reporter
assert "$accepted = @($events | Where-Object" in reporter
assert "creator_source =" in reporter
assert "$null -eq $contract" in reporter

print("PASS v0.69h starting options source audit: "
      "7 single guided careers, 13 legacy-compatible roles, 6 scenarios, 9 legacy noble births, 4 fief modes, "
      "native grants and guarded defaults. GAME STATUS UNTESTED.")
