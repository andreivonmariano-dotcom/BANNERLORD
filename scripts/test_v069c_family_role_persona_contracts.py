"""Static wiring regressions for the v0.69c Q1/Q2/Q3 integrity work.

This is source-only evidence. It does not launch Bannerlord or verify
that a native action succeeded in a running game or a persisted save.
"""
from pathlib import Path

src = Path(__file__).resolve().parents[1] / "Source" / "CalradiaUnified"
def read(name):
    return (src / name).read_text(encoding="utf-8")

family=read("SandboxFamilyCreator.cs")
safety=read("SandboxFamilyCreationSafetyPolicy.cs")
roles=read("SandboxRoleConsistencyPolicy.cs")
native=read("SandboxNativeStartOptions.cs")
preflight=read("SandboxStartingLifePreflight.cs")
agenda=read("SandboxCharacterAgendaState.cs")
living=read("SandboxLivingPersons.cs")

assert "allocatedChildren.Add(child);" in family
assert family.index("allocatedChildren.Add(child);") < family.index(
    'child.SetName(new TextObject(childProfile.Name)'
)
assert "allocatedChildren.Count" in family
assert "CleanupProvisionalHero(spouseHero,player)" in family
assert "CleanupProvisionalHero(incompleteSibling,player)" in family
assert "CleanupProvisionalHero(newFather,player)" in family
assert "CleanupProvisionalHero(newMother,player)" in family
assert "person.Children!=null && person.Children.Count>0" in family
assert "person.PartyBelongedToAsPrisoner!=null" in family
assert "person.StayingInSettlement!=null" in family
assert "player.Father==person" in family and "player.Mother==person" in family
assert "DisableHeroAction.Apply(person);" in family
assert family.index("DisableHeroAction.Apply(person);") < family.index(
    "person.Father.Children.Remove(person);"
)
assert "hasNativeChildren" in safety and "playerReferencesHero" in safety

all_roles = ["Wanderer","Recruit","Soldier","HouseholdKnight",
    "Knight","NobleCadet","Lord","King","Admiral","Trader",
    "Outlaw","Beggar"]
for role in all_roles:
    assert ('case "'+role+'"') in roles
assert "SandboxRoleConsistencyPolicy.NativeType(role)" in native
assert "SandboxRoleConsistencyPolicy.CheckNativeStation(" in preflight
assert '"independent_start_has_kingdom_or_fief"' in roles
assert '"ruler_status_or_fief_missing"' in roles
assert '"vassal_status_or_fief_missing"' in roles

assert "residence_no_longer_verified" in agenda
assert "goal_reassessed_from_native_context" in agenda
assert "native_home_changed" in agenda
assert "ObservedGoalReassessment" in living
assert "NativeResidenceEvidenceLost" in living
assert "SandboxGeneratedCharacterGoalPolicy.Goal(" in living
assert "goal_is_inferred_not_self_reported" in living

print("PASS v0.69c: provisional Hero safety, twelve native roles and observed NPC goal changes (SOURCE ONLY)")
