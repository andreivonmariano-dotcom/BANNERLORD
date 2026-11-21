# v0.69 step 1 | Honest landless homes and native household acceptance

Target: Bannerlord 1.5.7.125802. Stacked on v0.68 draft PR #89. Draft branch: `fix/v069-starting-life-household-contract`.

## Reproduced source defects

1. The minimal guided House preset chose `FamilyHomeMode=None` for Recruit, HouseholdKnight and NobleCadet. `SandboxFamilyHomePolicy.Modes` did not allow `None`. `SandboxOriginProfile.Parse` ignores unsupported field values and reconstructs the default `OwnedFief`. Result: an intended landless spouse-household home setting could be silently replaced.
2. The final `sandbox_start_contract` checked spouses, children, sibling parentage and fief outcomes, but did not treat requested residence `skipped:/failed:/partial:/refused:` as a failed starting condition.

## Scoped repair

- Support explicit no-fixed-home status `None` and preserve it through profile save/parse and native creator advanced choices.
- Default no-home for unlanded roles, rather than promising an estate the native start does not grant. Retain `OwnedFief` default for roles with native landed King/Vassal starts.
- Refuse `OwnedFief`, `ClanHome` or `SpecificOwned` as a starting spouse-residence promise for landless custom roles. Do not prevent editing intermediate fields.
- For an explicit `None`, emit `not_requested:no_fixed_home` without teleportation or fictional housing.
- A requested home now requires final native placement result `placed:...` to pass the starting contract. This does not undo prior phase-8 or phase-9 native mutations.
- Add deterministic C# policy tests for None preservation, landless rejection, commoner auto plan, partial and omitted placements, and positive native result.

## Explicit non-goals and future work

The v0.69 step-1 patch does not solve phase-8 rollback, full transaction compensation, AI personality, authentic household retainer wages, NPC gold-transfer verification, or live UI crashes. Treat all as open tasks in `docs/PROJECT_DECISIONS_AND_CHAT_HANDOFF.md`.

## Verification gates

CI: C# policy smoke, pure simulations, XML/source regressions, pinned-game assembly compilation, ZIP integrity and exact artifact hashes. In-game: create landless spouse households, verify no fief is invented, check the final contract, create an owned-fief spouse family and verify real settlement residence, replay partial failure, save/quit/reload and complete 30 game days. Source compilation alone meets only gate B.

**Verified Windows CI, step 1:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44857872281 for executable source commit `fdba449c8d5e2e36c91be304edf8b48e7c8398d5`. **872 executed C# gameplay policy checks passed**. Medieval-life model: 96 seeds × 3,650 days, 1,062,792 checks. Living-affairs model: 120 × 730 days, 180,425 checks. Existing campaign model: 40 × 365 days, 303,098 checks. Native 1.5.7.125802 target compilation passed with **0 warnings, 0 errors**; package integrity, hash and PowerShell tests passed. DLL: 563,712 bytes, SHA-256 `698200f99e474faf6eb1f6d132c1384be943a386b3ff116ec5ab6f7ccb22f861`. Runtime ZIP: 260,441 bytes, SHA-256 `30917e819dd18fb12b229c65750763ddb770275f5793ebb696c241ad9a02feaf`. [Compiled and game-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44857872281/artifacts/14688462229). Internal v0.38.0 artifact name is historical. **Gate B only**; no native gameplay or save/reload results yet. [Draft PR #90](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/90).
