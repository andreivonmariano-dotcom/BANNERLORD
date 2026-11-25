# Calradia Unified | Permanent development tracker

25 November 2026. Active draft PR #100, stacked on PR #99. This index is the living dashboard. Update records after every scoped milestone; never infer a PASS from source, policy tests or a different DLL. Verify live head and CI before reporting. The latest verified runtime at creation is 9e5aaf18e17afa767c42e3b7245db6f7620fd9df, run [45630836856](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45630836856), artifact [15029908935](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45630836856/artifacts/15029908935). Gate B only: 138 CI steps, 4,359 policy checks, zero warnings/errors, verified ZIP and DLL hash 81e240cdb7eee56ef19320be7920ff456002133bd1fa9da667f0d9298194d8ed. Cleanup source 47cfd78f4f512a7027f49530bb7150fbcc769c62 requires its own CI and native acceptance. Native careers certified 0/7. Installed DLL was previously older; verify again before testing.

## Persistent records

- [Feature development tracker](../Data/CU_Feature_Development_Tracker.csv) — 143 source-linked entries, disposition, current runtime scope, milestone, test group, action and evidence limit.
- [Milestone tracker](../Data/CU_Milestone_Tracker.csv) — 9 dependencies and status.
- [Native test execution tracker](../Data/CU_Native_Test_Execution_Tracker.csv) — 56 test IDs; one Gate B package record passed for exact earlier candidate, 55 other cases pending. The matrix is procedure; ledger is actual results. Add a separate candidate/profile row for each executed test, not overwrite previous candidate proof.
- [Compatibility tracker](../Data/CU_Compatibility_Tracker.csv) — 41 module ownership records; no claimed pairwise compatibility.
- [Bug tracker](../Data/CU_Bug_Tracker.csv) — 13 current risks/defects, priority, status, evidence and next action.
- [Release evidence tracker](../Data/CU_Release_Evidence_Tracker.csv) — immutable source/run/artifact/hash records per candidate; no release claim.
- [Six design documents](CU_GAME_DESIGN_DOCUMENTS.md), [source audit](CU_FEATURE_SOURCE_AUDIT_V2.md), [master roadmap](CU_MASTER_ROADMAP.md).

## Maintenance rules

Feature tracker: maintain stable IDs and canonical coverage. Keep disposition separate from implementation and native evidence. Add no feature without a core system or explicit technical contract, native/provider owner, source path, acceptance test and save implications. Retired and archived features remain in history, no duplicate entries.

Native test execution: use PENDING, PASS_GATE_B_ONLY, PASS, FAIL, BLOCKED or NOT_APPLICABLE; PASS requires native evidence for C–F, exact candidate/profile and actual post-state; record reason for blocked/not applicable. Record private screenshot/session identifiers but never publish private logs, saves, personal data or game DLLs. Gate E requires actual copied save/quit/relaunch/reload. Gate F requires 30 campaign days, 60-minute full profile, performance and transition checks. Release requires exact tested artifact.

Bug tracker: P0 crash/corruption/authority/evidence/release blocker; P1 broken gameplay, overlap or unusable UI; P2 cleanup/polish. OPEN, BLOCKED_PROVIDER, SOURCE_REFACTORED_CI_PENDING, COMPILED_NATIVE_PENDING, MITIGATED_SOURCE_NATIVE_PENDING, DESIGN_READY_GATED, VERIFIED_FIXED, CLOSED. Do not close on compile alone. Update evidence and next action, link PR/commit/run. Historical bug evidence remains. Milestones close by acceptance, not date. Every coding response should identify current gate and blocker.

## Current priority

R0 exact candidate and installed configuration; R1 seven-career native acceptance; R2 repair observed blockers; R3–R6 deepen only accepted baseline systems; R7 copied-save migration and full-stack performance; R8 release. No branch merge, release or local install without appropriate evidence and explicit authorization.