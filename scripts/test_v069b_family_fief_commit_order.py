"""Source-level regression for CU phase-9 native mutation sequencing.

These checks prove static ordering only. Native campaign behavior still
requires real Bannerlord new-game and save/reload acceptance.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Source" / "CalradiaUnified"
behavior = (root / "SandboxOriginBehavior.cs").read_text(encoding="utf-8")
preflight = (root / "SandboxStartingLifePreflight.cs").read_text(encoding="utf-8")
contract = (root / "SandboxStartContract.cs").read_text(encoding="utf-8")

start = behavior.index("private void TryApplyOnce()")
life = behavior[start:]

ordered = [
    "SandboxStartingLifePreflight.Check(",
    "SandboxRoleAffiliations.TryApply(profile,hero)",
    "SandboxFamilyCreator.TryCreate(profile,hero)",
    "SandboxStartContract.PostFamilyGrantGate(",
    "SandboxServicePatron.TryAssign(",
    "SandboxStartingFief.TryApply(profile,hero)",
    "SandboxFamilyHome.TryPlace(profile,hero,fiefOutcome)",
    "if (profile.Gold >= 0)",
    "SandboxCulturalRetinue.TryCreate(profile,hero)",
]
positions = [life.index(token) for token in ordered]
assert positions == sorted(positions), list(zip(ordered, positions))

assert life.index("if(postFamilyGate.Length>0)") < life.index(
    "SandboxStartingFief.TryApply(profile,hero)"
)
assert 'no_new_fief_gold_items_or_retinue=true' in life
assert 'additional_grants_skipped=true' in life
assert 'reason=commanding_lord_not_bound' in life
assert life.index("RegisterFamilyUpbringings(profile,hero,family)") > life.index(
    "SandboxStartContract.PostFamilyGrantGate("
)

assert preflight.index("SandboxStartContract.Validate(") < preflight.index(
    "SandboxDynasticSonStart.Preflight("
)
assert '"native_start:"+nativeErrors[0]' in preflight
assert 'native_start_type_mismatch' in contract
assert 'starting_kingdom_mismatch' in contract
assert 'ruler_status_or_fief_missing' in contract
assert 'vassal_status_or_fief_missing' in contract
assert 'family_creation_incomplete' in contract

print("PASS v0.69: native start, family, lineage, patron and fief ordered before optional resources (SOURCE ONLY)")
