#!/usr/bin/env python3
"""v0.64 Starting Life and Living Persons source contract, not gameplay."""
from pathlib import Path
src=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
def read(x):return (src/x).read_text(encoding="utf-8")
vm=read("CUGuidedCreatorVM.cs")
profile=read("SandboxOriginProfile.cs")
policy=read("SandboxStartingLifePolicy.cs")
autohouse=read("SandboxAutoHousePlan.cs")
lineage=read("SandboxHouseholdLineagePolicy.cs")
son=read("SandboxDynasticSonStart.cs")
family=read("SandboxFamilyCreator.cs")
spouse=read("SandboxGeneratedSpouseOrigin.cs")
guard=read("SandboxStartingLifePreflight.cs")
dyn=read("SandboxDynasticStart.cs")
patron=read("SandboxServicePatron.cs")
behavior=read("SandboxOriginBehavior.cs")
ledger=read("SandboxLifeHistoryState.cs")
adapter=read("SandboxLivingPersons.cs")
assert '"EarlyHousehold","The household that raised you"' in vm
assert '"PersonalAmbition","Your personal ambition"' in vm
assert "SandboxStartingLifePolicy.Dossier(" in vm
assert "Social station: " in vm and "Obligations: " in vm
assert '"EarlyHousehold=" + EarlyHousehold' in profile
assert '"PersonalAmbition=" + PersonalAmbition' in profile
assert "SandboxStartingLifePolicy.FamilyScenario(" in autohouse
assert '"MercenaryCompany"' in policy and '"LandedVassalCadet"' in policy
assert "AdditionalOlderBrothers" in lineage
assert "PlannedOlderBrothers" in son
assert "AdditionalOlderBrothers(" in son
assert 'pending_house_older_brothers=' in son
assert "ExactFinalRank(" in family
assert "cu_final_son_birth_rank" in family
assert "birthHouseholdHeir" in family
assert "player.Father.Clan" in family
assert "cu_family_older_heir_stays_with_father" in family
assert "player_party_member=false" in family
assert "cu_dynastic_final_birth_rank" in behavior
assert "noble_birth_rank_changed_after_house" in behavior
assert "requested_native_siblings_missing" in behavior
assert "closeFamilyClans" in spouse and "player.Father" in spouse
assert "SandboxDynasticSonStart.Preflight(p,player)" in guard
assert "SandboxDynasticStart.HasNativeElder(p,player)" in guard
assert "SandboxServicePatron.HasNativePatron(p,player)" in guard
assert "SandboxStartingLifePreflight.Check(" in behavior
assert behavior.index("SandboxStartingLifePreflight.Check(") < behavior.index(
    "SandboxRoleAffiliations.TryApply(")
assert "CU_v064_living_persons" in behavior
assert "SandboxLivingPersons.Capture(profile,hero,patron," in behavior
assert '"CU_LIFE_1"' in ledger
assert "Uri.EscapeDataString" in ledger
assert "VerifiedNativeParent" in adapter
assert "VerifiedNativeMarriage" in adapter
assert "VerifiedNativeLordRelationshipNotEnlistment" in adapter
assert "fabricated_prior_memories=false" in adapter
assert "future_dialogue_hooks_not_active=true" in adapter
print("PASS: Starting Life World dossier, strict son chronology, preflight, kin-safe spouse and save-backed living persons (SOURCE ONLY)")
