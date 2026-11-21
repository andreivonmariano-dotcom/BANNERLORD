# v0.69f | Existing creator-state, NPC-save and family-recovery stabilization

**Date:** 21 November 2026. **Base:** active audit branch `audit/v069e-current-gameplay-and-visual-acceptance`, [draft PR #93](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/93). **User directive:** run all three follow-up tracks, and continue **audit-first with a feature freeze**. This repair adds no social role, interface page, wage, game action, NPC profession, artifact, or Gauntlet prefab.

## Source audit and repaired P1/P2 defects

### Q1. Creator-state reliability

The real `CUGuidedCreatorVM` builds edited profiles by `SandboxOriginProfile.Parse(profile.Serialize())`. Changes to spouse, children, siblings, age and optional fields alter which questions are visible. The VM anchors its cursor to the field **key** rather than the old numeric index and holds an uncommitted House draft across native Review Back. `CommitHouse` saves to the player's starting profile before clearing that draft, and the Stats editor keeps independent player/spouse override dictionaries. Earlier C# checks covered examples but did not stress the exact hidden/visible cases.

New C# checks now edit spouse name/age, two named children and one sibling, hide them by setting counts and spouse choice to zero, serialize/reparse, and reopen them. The expected behavior is **no lost names, ages, gold or retinue mode**. Invalid name and age input must not mutate the selected draft. The source-only regression checks the VM's navigation and snapshot wiring, native Review reentry, and Stats age rollback/save-before-continue order.

**Boundary:** This is executable profile serialization and static ViewModel inspection, not actual Gauntlet input, Back/Continue focus, text clipping or native character creation.

### Q2. Existing campaign request/cooldown defect

**Source-confirmed defect:** `SandboxAgencyDecisionState.Parse` could restore an active NPC request from a saved header without a corresponding record of that person's proposal or prior request count. It accepted malformed negative `LastDay` values and did not cross-check that the request opening day matched the saved quota record. `Propose` also subtracted the day counter using 32-bit integers, risking overflow on a very large campaign day.

**Bounded fix:** Rehydrate a pending request only when its saved Hero has a matching record, at least one recorded request, and `LastDay == PendingOpenedDay`. Malformed or nonexistent records never authorize a pending request after reload. Negative historical day values and zero-request quota records are rejected. Compute cooldown intervals with `long` arithmetic. Preserve the existing `CU_AGENCY_1` save format, the 70-day cooldown, 35-day expiry and three-request lifetime maximum. Normal legitimate saved pending requests must continue to reload exactly.

**Boundary:** A corrupted save can still erase historical records. This repair prevents a forged *pending* request from displaying, but it cannot reconstruct a missing prior history or a native action that was never recorded. No live NPC dialogue or native game save has been run.

### Q3. Invalid family-recovery observations and adverse saved-state fixtures

**Source-confirmed defect:** `SandboxFamilyRecoveryState.Observe` compared native count inputs before clamping them to 99 and used the raw values again for count-match status. A negative count could be accepted as an observation, and a status change with unchanged counts could be reported as `same` instead of a new event.

**Bounded fix:** Reject negative child or sibling counts without mutating the original saved dossier. Compare and evaluate counts after clamping. Return an observation-change marker if the review status itself changes. Continue to label any count match as `NativeCountsMatchReviewStillRequired`; matching counts are not proof of native identity or family age.

The expanded `Tests/AuditSimulator` exercises **10,000 adverse serialized snapshots**, including missing and mismatched NPC quota records, invalid historic timestamps, expired requests, negative family counts, large count inputs, unrelated player IDs, malformed family status and negative native money balances. C# policy smoke covers valid and malformed requests and conditional creator fields. These are deterministic pure-policy models, not game-native save files.

## Regression gates

- `Tests/PolicySmoke/Program.cs`: executable profile hidden-field/Review-back roundtrips, saved NPC quota evidence, negative day and maximum-day arithmetic
- `Tests/AuditSimulator/Program.cs`: 12 roles × 48 seeds × 365 modeled days plus 10,000 corrupted snapshot permutations and prior 20,000 retinue-receipt cases
- `scripts/test_v069f_creator_back_state_and_agency_save.py`: source-only ViewModel and save-evidence wiring checks
- `.github/workflows/bannerlord-build.yml`: all legacy source regressions, 24 modeled UI hitbox cases, 96 × 730 modeled campaign days, and pinned native Bannerlord 1.5.7.125802 module compile with no errors or warnings
- Actual installed-game UI, native spouse/companion/party effects, current-session screenshots, 30-day campaign, game save/quit/reload acceptance remain **NOT RUN** until real evidence is available

**Verification pending at this document's creation.** Await a successful exact branch-head Windows Actions run. Record the policy count, model assertions, executable commit, DLL/ZIP SHA-256, and compiled artifact only from that run. Keep draft PR #93 unmerged and the current feature freeze intact.
