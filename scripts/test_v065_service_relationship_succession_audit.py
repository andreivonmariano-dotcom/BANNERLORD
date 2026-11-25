#!/usr/bin/env python3
"""v0.65 flow audit and policy wiring. Cannot replace real Bannerlord testing."""
from pathlib import Path
src=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
def text(name):return (src/name).read_text(encoding="utf-8")
vm=text("CUGuidedCreatorVM.cs")
native=text("SandboxNativeStartOptions.cs")
role_matrix=text("SandboxRoleConsistencyPolicy.cs")
profile=text("SandboxOriginProfile.cs")
bh=text("SandboxOriginBehavior.cs")
people=text("SandboxLivingPersons.cs")
family=text("SandboxFamilyCreator.cs")
retinue=text("SandboxCulturalRetinue.cs")
ledger=text("SandboxLifeHistoryState.cs")
career=text("SandboxServiceCareerState.cs")
social=text("SandboxSocialObservationState.cs")
succession=text("SandboxSuccessionInterestState.cs")
start=text("SandboxStartingLifePolicy.cs")
assert 'profile.StartingScenario=="twofactionwar"' in vm
assert 'SandboxRoleConsistencyPolicy.NativeType(role)' in native
assert 'case "Soldier":' in role_matrix
assert 'case "Mercenary":return "mercenary";' in role_matrix
assert 'case "Outlaw":return "outlaw";' in role_matrix
assert 'case "Knight":' in role_matrix and 'case "Lord":return "vassal";' in role_matrix
assert '"Independent mercenary captain, not an enlisted soldier"' in start
assert '"Knight holding a native vassal fief"' in start
assert "Occupation.Wanderer" in retinue
assert "nobleFieldOffice" in retinue
assert 'sibling.SetNewOccupation(Occupation.Wanderer)' in family
assert 'status="Active"' not in bh or 'serviceCareer.Start(' in bh
assert 'CU_v065_service_career' in bh
assert 'CU_v065_social_observations' in bh
assert 'CU_v065_succession_interest' in bh
assert 'SandboxLivingPersons.ObserveDaily(' in bh
assert 'cu_personal_service_career_started' in bh
assert 'enlisted_in_npc_party=false' in bh
assert 'automatic_promotion=false' in bh
assert 'native_wages_authoritative=true' in bh
assert 'PotentialSuccessionReview' in bh
assert 'native_fief_ownership_unchanged' in bh
assert 'native_fiefs_transferred=0' in bh
assert 'NativeRelationChangedObserved' in people
assert 'state.ObserveMarriage(hero.StringId,' in people
assert 'hero.Spouse.Spouse==hero' in people
assert 'NativeMarriageObserved' in ledger
assert 'if(!reciprocal' in ledger
assert 'NativeClanChangedObserved' in people
assert 'cause=unverified' in people
assert 'Uri.EscapeDataString' in social
assert 'CU_SOCIAL_1' in social
assert 'CU_SERVICE_1' in career
assert 'PayAuthority="NativeGame"' in career
assert 'CU_SUCCESSION_1' in succession
assert '"ReviewNativeSuccession"' in succession
assert 'Events.RemoveAt(0)' in ledger
assert 'requested_spouse_identity_mismatch' in bh
assert 'family_creation_incomplete' in bh
assert 'starting_fief_request_unfulfilled' in bh
assert 'cultural_retinue_not_applied' in bh
assert 'family_only_retinue_not_applied' in bh
assert bh.index("SandboxLivingPersons.Capture(") > bh.index(
    'DiagnosticJournal.Event("sandbox_start_contract",')
# v0.67 groups the old status action under the native People & Lineage menu.
native_affairs=text("SandboxOriginBehavior.LivingMenus.cs")
assert '"Attend to local affairs"' in native_affairs
assert 'RegisterLocalAdministrationEntry(starter)' not in native_affairs
assert 'Settlement.CurrentSettlement.OwnerClan==' in native_affairs
assert '"cu_people_household"' in native_affairs
assert 'RegisterPeopleAndLineageMenus(starter);' in bh
print("PASS: World visibility, native career tracking, observed social changes, succession interests and no false start PASS (SOURCE ONLY)")
