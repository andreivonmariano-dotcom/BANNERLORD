# CU feature-by-feature source audit and retirement pass

25 November 2026. Audited source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df; cleanup source 47cfd78f4f512a7027f49530bb7150fbcc769c62. The 143-row canonical/supplemental review was rechecked against the current Git tree: 143 unique rows, all source paths exist, all 70 canonical CU IDs included; 90 distinct source/reference paths. This is registry-to-path and feature-family reconciliation, not a claim that every line of every class or external binary was manually inspected. The new development tracker carries all 143 dispositions without inflating historical acceptance. Native gameplay remains unverified.

## Concrete runtime cleanup

Eight formerly compiled, disconnected adapters excluded by exact Compile Remove entries: CivicBehavior.cs, CivicBehavior.CastleManagement.cs, CivicBehavior.Convoys.cs, CivicBehavior.Feudal.cs, CivicBehavior.Garrisons.cs, CivicBehavior.Kingdom.cs, CivicBehavior.Trade.cs, KingdomAutonomyBehavior.cs. Their historical source remains for provenance and historic source-policy regressions. New CivicMigrationBehavior.cs and KingdomAutonomyMigrationBehavior.cs keep the exact original runtime type names, constructor signatures, registration boundary and serialization functions. CivicState remains unchanged; CU_v030_civic_state, CU_v060_kingdom_orders, CU_v060_kingdom_day and four legacy civic exits preserved. SupplyBehavior retains read-only food/army warnings and CU_v020_days_since_forage; actual grain mutation removed. No active convoy spawning, depots, harvest/sales, wage cap, levy, advisory scan, autonomous travel or patrol adapter in game assembly. Pure historical policy types/test files stay until cross-reference and migration review.

The new test_cu_retired_runtime_migration.py checks the actual compiled set, identical save-codec bodies and exit menu body, one registered civic exit listener, no autonomy listener, no supply mutation and same registered type names. Historical tests remain for old behavior, not current native claims. Existing source inventory enumerates all compiled/excluded paths, registrations and literal save keys. Actual old saves with existing physical convoy parties, outstanding shipments or funded depots still require controlled migration; no destructive cleanup or compensation guessed here. The exact save-key identity and codec equality reduce risk but do not prove save compatibility.

## Audit classifications

- COMPILED_SOURCE_PATH: 82 review rows.
- RUNTIME_EXCLUDED_MIGRATION_SHIM: 17 review rows.
- RETIRED_ROUTE_OR_LEGACY_DATA: 14 review rows.
- MUTATOR_REMOVED_WARNINGS_ONLY: 1 review rows.
- RUNTIME_EXCLUDED_HISTORICAL: 3 review rows.
- DEFERRED_OR_PROBE: 5 review rows.
- REFERENCE_OR_ASSET_PATH: 5 review rows.
- OBSERVER_DISCONNECTED: 6 review rows.
- ARCHIVED_STATE: 2 review rows.
- DESIGN_ONLY: 5 review rows.
- ENTRY_REMOVED: 3 review rows.

Rows count subfeatures, not independent systems or tests. Source status and native evidence are separate. 18 rows directly affected by the examined adapter/migration or supply mutator removal; other rows retain existing decisions after path and family-contract reconciliation. Remaining dead code inside large partials (legacy retainer/education/peer/story/grant methods), legacy settings and experimental assets require a second targeted reachability/migration pass, not indiscriminate deletion. No independent mods modified.

## References

[All 143 tracked entries](../Data/CU_Feature_Development_Tracker.csv), [design library](CU_GAME_DESIGN_DOCUMENTS.md), [test matrix](../Data/CU_Six_System_Native_Test_Matrix.csv), [native execution ledger](../Data/CU_Native_Test_Execution_Tracker.csv), [bug tracker](../Data/CU_Bug_Tracker.csv), [release evidence](../Data/CU_Release_Evidence_Tracker.csv).

## Verified cleanup candidate update

Source 47cfd78f4f512a7027f49530bb7150fbcc769c62, [CI 45632715404](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45632715404), [artifact 15031393532](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45632715404/artifacts/15031393532). Both workflows passed; 138 steps and 4,359 independent policy checks, zero compiler warnings/errors. Source inventory: 193 files, 182 compiled candidates, 22 prefabs, 65 settings, 18 literal campaign listeners and 58 literal save keys. Before cleanup: 191 files, 188 compiled candidates, same 58 keys. DLL 1,139,712 bytes (39,936 bytes smaller), SHA-256 166b7843d99baf7853cd2a5d6a4961116d326e94172ee24b2221da2679d19ad9. Runtime ZIP 448,474 bytes, SHA-256 349e36af8280fada0c351876395b2aa6a4c9ab4df3a6279c72833360c8e62487. Both archive CRCs, one module, one DLL, v0.74.0 manifest and embedded build identity verified. Gate B only. No native career, old-save migration or performance certification. Candidate-specific execution rows now cover both verified binaries; each retains separate evidence.
