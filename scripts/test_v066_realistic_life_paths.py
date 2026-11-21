#!/usr/bin/env python3
"""v0.66 source checks. Independent C# stress simulation runs in PolicySmoke."""
from pathlib import Path
src=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
def read(name):return (src/name).read_text(encoding="utf-8")
profile=read("SandboxOriginProfile.cs")
native=read("SandboxNativeStartOptions.cs")
role_matrix=read("SandboxRoleConsistencyPolicy.cs")
life=read("SandboxStartingLifePolicy.cs")
world=read("CUGuidedCreatorVM.cs")
policy=read("SandboxCustomLifeRolePolicy.cs")
guard=read("SandboxStartingLifePreflight.cs")
father=read("SandboxDynasticSonStart.cs")
elder=read("SandboxDynasticStart.cs")
patron=read("SandboxServicePatron.cs")
sa=read("SandboxSaSBridge.cs")
state=read("SandboxServiceCareerState.cs")
culture=read("SandboxCulturalInheritancePolicy.cs")
claim=read("SandboxSuccessionInterestState.cs")
goals=read("SandboxGeneratedCharacterGoalPolicy.cs")
agenda=read("SandboxCharacterAgendaState.cs")
living=read("SandboxLivingPersons.cs")
house=read("SandboxFamilyCreator.cs")
home=read("SandboxBirthHouseholdPlacement.cs")
auto=read("SandboxAutoHousePlan.cs")
behavior=read("SandboxOriginBehavior.cs")
simulation=read("SandboxMedievalLifeSimulation.cs")
assert ' "Recruit", "Soldier", "HouseholdKnight"' in profile
assert '"Knight", "NobleCadet", "Lord"' in profile
assert 'case "NobleCadet":return "default";' in role_matrix
assert 'SandboxRoleConsistencyPolicy.NativeType(role)' in native
assert 'SandboxSaSBridge.HasSupportedProvider()' in native
assert 'SandboxCustomLifeRolePolicy.NativeLandlessContract(' in guard
assert 'SandboxCustomLifeRolePolicy.ValidBirth(' in profile
assert 'SandboxCustomLifeRolePolicy.IsNobleFamilyRole' in world
assert 'SandboxCustomLifeRolePolicy.NeedsKingdomContext(' in world
assert '"UnlandedDynasticCadet"' in life
assert '"ProspectiveEnlistedRecruit"' in life
assert 'p.Role=="NobleCadet"' in father
assert 'profile.Role=="NobleCadet"' in elder
assert 'SandboxCustomLifeRolePolicy.IsLandless(p.Role)' in patron
assert 'ServeAsSoldier.Compat.SaSPublicApi' in sa
assert '"ApiVersion"' in sa and '"IsPlayerEnlisted"' in sa
assert '"GetSnapshot"' in sa and '"DailyWage"' in sa
assert '"CU_SERVICE_2"' in state and '"CU_SERVICE_1"' in state
assert '"PendingEnlistment"' in state
assert '"EnlistedActive"' in state
assert 'PayAuthority="ServeAsSoldier"' in state
assert 'SandboxSaSBridge.Read()' in behavior
assert 'cu_sas_service_observed' in behavior
assert 'SandboxBirthHouseholdPlacement.CanPlace(father)' in father
assert 'SandboxBirthHouseholdPlacement.Place(' in father
assert 'birthHouseholdResident' in house
assert 'EnterSettlementAction.ApplyForCharacterOnly' in home
assert 'AddHeroToPartyAction.Apply' in home
assert 'plan.FamilyHomeMode="None"' in auto
assert 'SandboxCulturalInheritancePolicy.Model(' in claim
assert '"CU_SUCCESSION_2"' in claim
assert '"CU_SUCCESSION_1"' in claim
assert 'CanTransferEstateWithoutNativeAction()' in culture
assert 'sandbox' not in culture.lower() or 'Native' in culture
assert '"ProtectSharedHousehold"' in goals
assert 'e.Goal!="ProtectSharedHousehold"' in agenda
assert 'ComradeshipAwards' in agenda
assert 'SandboxLivingPersons.CaptureAgenda(' in behavior
assert 'SandboxLivingPersons.ObserveAgenda(' in behavior
assert 'CU_v066_character_agendas' in behavior
assert 'NativeResidenceChangedObserved' in living
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in living
assert 'Run(int seeds,int days)' in simulation
assert 'NativeFiefsTransferred' in simulation
assert 'NativeWagesGranted' in simulation
assert '96 seeds through 3650 days' in (
    Path(__file__).resolve().parents[1]/
    'Tests/PolicySmoke/Program.cs').read_text(encoding='utf-8')
print("PASS: independent landless starts, documented SaS API, real household placements, culture claims, native goals and 350400-day policy simulation (SOURCE ONLY)")
