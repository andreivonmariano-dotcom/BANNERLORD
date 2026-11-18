# Feature-level audit and GitHub acceptance workflow

**Canonical registry:** [Data/Feature_Registry.csv](../Data/Feature_Registry.csv). The registry has one row per tracked feature or historical mod placeholder. The separate historical inventory retains original metadata and licensing notes.

## How to use the registry

- **Current C# features:** `CU-*` records. Each has its own feature ID, milestone, primary source path, optional original-mod reference, evidence stage and open test coverage.
- **Historical source references:** `HIST-001` through `HIST-078`. These describe individual old mod folders, remain `IDENTIFIED`, and never imply that all functionality from that mod was recreated.
- **Issue tracking:** Create or update a GitHub issue with the feature ID in the title, and link it to a milestone issue [M0](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/1) through [M9](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/10). Record exact gameplay reproduction and evidence there.
- **Status transition:** `IDENTIFIED` → `SCOPED` → `IMPLEMENTED_SOURCE` → `STATIC_CHECKED` → `COMPILED` → `GAME_TESTED` → `REGRESSION_TESTED` → `RELEASED`, or `BLOCKED` with cause. A feature's status describes the highest observed gate for that feature, not the health of every system.
- **Verification:** Build evidence is mandatory for `COMPILED` and later. A game-session report is mandatory for `GAME_TESTED`, `REGRESSION_TESTED` and `RELEASED`. `RELEASED` requires Gate G.

## CI check

GitHub Actions runs `python scripts/validate_feature_registry.py`. This checks the 78 distinct historical IDs, exact one-to-one historical placeholder coverage, unique IDs, source paths, allowed statuses and evidence requirements. It does not assert gameplay compatibility. The independent C# smoke project validates game-independent policy logic. A Windows build and a real game session are separate gates.

## M1 examples

- `CU-M1-001`: depot deposit and withdrawal, with new capacity rules.
- `CU-M1-003`: abstract shipment settlement and bounded refunds.
- `CU-M1-004`: actual guarded convoy map party, with partial unloading.

These are three independently testable features. The historical mods remain unmatched at full parity until their own acceptance criteria have passed.

## Review checklist per feature

1. Describe one player-visible action and identify the source revision that implements it.
2. Record normal, edge, disabled-state and failure-path policy tests.
3. Link the exact successful build and compiled DLL identity before setting `COMPILED`.
4. For gameplay gates, provide game version, toggles, save ID, steps, observed outcome and log.
5. Test save/load and destructive transitions before setting `REGRESSION_TESTED`.
6. Only update historical replacement status after the defined feature coverage is accepted.

## v0.10.0 recruitment and garrison entries

- `CU-M2-002`: Daily settlement-specific limit on local cultural recruitment.
- `CU-M4-001`: Gold-and-grain-funded defenders added to an existing garrison.

These features are new source work with independent C# policy tests and simulation scenarios. They must pass a pinned build and actual supervised game tests before their runtime status can be advanced beyond compilation.

## v0.11.0 fiscal work

New source feature IDs: `CU-M1-006` fiscal estimate, `CU-M1-007` virtual village harvest, `CU-M1-008` surplus wholesale sale, and `CU-M4-002` budgeted garrison wage caps and automatic training. These are not full replacements for their historical mod inspirations. A policy simulator result counts only as modeled acceptance, not real native tax or garrison wage behavior.
