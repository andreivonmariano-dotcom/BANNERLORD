# v0.68 | Confirm native sibling comradeship before consuming its lifetime reward

**Target:** Bannerlord PC 1.5.7.125802, Calradia Unified. Stacked on draft PR #88 at commit `5d054a512f5980698c75ddc112d7247850a38ea6`.

## Observed source defect

Previously `SandboxCharacterAgendaState.ObserveComradeship` increased `ComradeshipAwards` as soon as the 30-day policy proposed a +1 relation award. The later `ChangeRelationAction.ApplyRelationChangeBetweenHeroes` might throw or fail to change Bannerlord's native relationship. The ledger would still record a consumed lifetime award. This could deny a real sibling benefit, despite no successful native effect.

## Narrow implementation

- `ObserveComradeship` now proposes a point without counting it. A transient `AwardProposed` flag prevents repeated confirmation of the same daily proposal.
- The native adapter reads the actual relation before and after the synchronous game action. It confirms a reward only after the observed value increases. A rejected or throwing action consumes no award and produces no invented life-history event.
- Both the player and sibling must be adults. They must have the same actual mother and father and occupy the same native party. The existing +1 per 30 consecutive days, +5 lifetime limit, and native relation ceiling of 80 remain.
- The next 30-day eligible milestone can retry after an unconfirmed award. No new pay, rank, fief, or fabricated relationship is created.
- `CU_AGENDA_1` save format remains unchanged. The proposal flag is deliberately transient, and only confirmed rewards are persistent.

## Checks

`Tests/PolicySmoke/Program.cs` tests the successful five-point limit, a simulated native action that does not change the relation, a retry that succeeds at the next milestone, a rejected duplicate confirmation, and save/reload of confirmed and unconfirmed awards.

`SandboxMedievalLifeSimulation` inserts rejected native relation actions into the 96-seed, 3,650-day pure-policy run. It checks that lifetime awards change only on confirmations and that relation scores remain bounded.

**These checks simulate policy behavior only.** Windows CI results must be read from the actual workflow run. They do not establish that `ChangeRelationAction` works in a live campaign.

## In-game acceptance

1. Start a fresh Sandbox with two genuinely adult full siblings. Verify both Heroes share real parents and occupy the same real player party.
2. Travel together for 30 continuous days. Check the native relation before and after the recorded award. It should rise only once, and the stored counter should rise by one.
3. Separate the siblings and reunite them. Check the 30-day continuous-service rule and confirm no point is given merely for kinship.
4. Save, quit and reload after confirmed awards. Inspect diagnostics and verify no duplicate point or reset of the lifetime counter.
5. Test a player under 18 or a sibling under 18 to confirm that service awards are refused. Keep draft status until the game observations pass.

## Verified Windows CI

**Run:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44856889654

**Exact compiled source commit:** `caa897e645001afff2bdb3db81d5265b2f5b3574`. The subsequent document-only commit does not alter runtime source.

- 867 executed C# gameplay policy checks passed, including the new failed-action and duplicate-confirmation tests.
- Medieval life: 96 seeds × 3,650 modeled days, 1,062,792 checks, no reported invariant failures.
- Living affairs: 120 seeds × 730 modeled days, 180,425 checks, no reported invariant failures.
- Existing campaign policy simulation: 40 seeds, 14,600 modeled days, 303,098 checks.
- Bannerlord 1.5.7.125802 module compilation: **zero warnings, zero errors**. Runtime ZIP structure, CRC, DLL hash, and installer test passed.
- Compiled DLL: 563,200 bytes, SHA-256 `a7a42fe8490dbb5b26ff53e6d2e57cb63b2eb5d97732b6cc9b3a3b6d6c11a200`.
- Inner runtime module ZIP: 260,171 bytes, SHA-256 `88a711e035b50e3390f8ce8ed6badf0ab2f9e7999c92c09d73e1275617535562`.
- [Compiled, **game-untested** GitHub artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44856889654/artifacts/14688157069). The legacy `v0.38.0` package label is historical. Identify this candidate by CI run and DLL hash.

**Highest completed gate: B (Compiled).** Live Bannerlord campaign, native relations, save/reload and game-regression gates C–G remain unverified.
