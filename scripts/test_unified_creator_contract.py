#!/usr/bin/env python3
"""Gate B only: never report an observed Bannerlord start from static source."""
from pathlib import Path
root = Path(__file__).resolve().parent.parent
s = root / "Source" / "CalradiaUnified"
behavior = (s / "SandboxOriginBehavior.cs").read_text()
contract = (s / "SandboxStartContract.cs").read_text()
csproj = (root / "Tests/PolicySmoke/PolicySmoke.csproj").read_text()
policytests = (root / "Tests/PolicySmoke/Program.cs").read_text()
doc = (root / "docs/V021_UNIFIED_CREATOR_ENGINE_MAP.md").read_text()

assert "SandboxStartContract.Validate(" in behavior
assert 'DiagnosticJournal.Event("sandbox_start_contract"' in behavior
assert 'failures.Length != 0' in behavior
assert 'Notify("Calradia Unified starting conditions failed:' in behavior
assert "SandboxStartContract.cs" in csproj
for rejected in ("native_start_type_mismatch", "starting_kingdom_mismatch",
                 "ruler_status_or_fief_missing", "vassal_status_or_fief_missing",
                 "requested_spouse_missing", "requested_children_missing"):
    assert rejected in contract, rejected
    assert rejected in policytests, rejected

# Preview investigation must not secretly remove native character-creation
# stages before a real game screenshot and input test.
assert 'DiagnosticJournal.Event("sandbox_appearance_engine_probe"' in behavior
assert 'manager.GetStage<CharacterCreationFaceGeneratorStage>()' in behavior
assert 'manager.GetStage<CharacterCreationCultureStage>()' in behavior
assert 'manager.GetStage<CharacterCreationNarrativeStage>()' in behavior
assert 'manager.GetStage<CharacterCreationReviewStage>()' in behavior
assert "RemoveStage<" not in behavior
assert "sandbox_appearance_engine_probe" in doc
assert "not the completed total conversion" in doc
print("PASS: new-start observation contract, pure policy tests and read-only native face-stage probe (STATIC ONLY)")
