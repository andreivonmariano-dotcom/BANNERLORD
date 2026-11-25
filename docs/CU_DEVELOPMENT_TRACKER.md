# Calradia Unified | Permanent development tracker

25 November 2026. Active draft PR #100, stacked on PR #99. This index is the living dashboard. Update records after every scoped milestone; never infer a PASS from source, policy tests or a different DLL. Verify live head and CI before reporting. The latest verified runtime at creation is 9e5aaf18e17afa767c42e3b7245db6f7620fd9df, run [45630836856](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45630836856), artifact [15029908935](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45630836856/artifacts/15029908935). Gate B only: 138 CI steps, 4,359 policy checks, zero warnings/errors, verified ZIP and DLL hash 81e240cdb7eee56ef19320be7920ff456002133bd1fa9da667f0d9298194d8ed. Cleanup source 47cfd78f4f512a7027f49530bb7150fbcc769c62 subsequently passed CI; see verified candidate update below. Native acceptance remains pending. Native careers certified 0/7. Installed DLL was previously older; verify again before testing.

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

## Verified cleanup candidate update

Source 47cfd78f4f512a7027f49530bb7150fbcc769c62, [CI 45632715404](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45632715404), [artifact 15031393532](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45632715404/artifacts/15031393532). Both workflows passed; 138 steps and 4,359 independent policy checks, zero compiler warnings/errors. Source inventory: 193 files, 182 compiled candidates, 22 prefabs, 65 settings, 18 literal campaign listeners and 58 literal save keys. Before cleanup: 191 files, 188 compiled candidates, same 58 keys. DLL 1,139,712 bytes (39,936 bytes smaller), SHA-256 166b7843d99baf7853cd2a5d6a4961116d326e94172ee24b2221da2679d19ad9. Runtime ZIP 448,474 bytes, SHA-256 349e36af8280fada0c351876395b2aa6a4c9ab4df3a6279c72833360c8e62487. Both archive CRCs, one module, one DLL, v0.74.0 manifest and embedded build identity verified. Gate B only. No native career, old-save migration or performance certification. Candidate-specific execution rows now cover both verified binaries; each retains separate evidence.


## Current playtest campaign and second cleanup

[Moment-to-moment scenarios](CU_MOMENT_TO_MOMENT_GAMEPLAY.md), [seven-career campaign](CU_SEVEN_CAREER_PLAYTEST_CAMPAIGN.md), [second retirement pass](CU_SECOND_CLEANUP_PASS.md). Source f49e9242 CI/native status pending at creation. Current evidence intake script audits separate exact-hash native archives and manual observations, returning REVIEW_READY, not native PASS. No live tests executed. Prior Lord session remains historical. All seven career routes and enjoyment fields start pending.


## Verified second cleanup candidate

Source f49e924240a67ff0be0fe26b302c9a80faf78955; [CI 45634616203](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45634616203), [artifact 15031528871](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45634616203/artifacts/15031528871). Both workflows succeeded. 138 CI steps, 4,361 independent policy checks, zero warnings/errors. Conversation graph 147 registrations, 52 states, static exits pass. Inventory 194 source files, 179 compiled candidates, 58 literal save keys, same 18 literal listeners. DLL 1,115,648 bytes SHA256 c14cdaa16a65c1def2c0e5ccfd96f5f2ae427cf3aa148e848765ac6ec98bb4c3; runtime ZIP 439,392 bytes SHA256 7dafe05f4be79c245bee2ff06bf8e3168a4126166dc3023a0e8c2e149c511690. Verified actual bytes, one module, one DLL, manifest/embedded source, both CRCs and retired camp prefab absent. Gate B, not installed or native-certified. 56 additional candidate-specific rows; all native cases PENDING. Earlier records remain immutable.


## Family observation and practical dialogue follow-up
Source work following exact green tracker head 9a722f24 (both workflows: 45635430604 and 45635414648). Existing bounded life ledger stores age-band and verified spouse-presence samples, without new literal save keys, rewards, or native mutations. First observations are baselines, not invented milestones; recent confirmed transitions inform co-parent dialogue. Native household member observation plus current infant age supports a present infant concern, not a birth date. Native duty-specific concerns use current readback. Six independent policy checks authored; exact new CI and native tests pending. No actual Lord playtest performed. See [focused Lord acceptance](CU_LORD_FAMILY_OBSERVATION_PLAYTEST.md).
