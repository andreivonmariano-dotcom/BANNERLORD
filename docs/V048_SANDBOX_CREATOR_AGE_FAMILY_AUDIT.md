# v0.48 | Sandbox creator age and family audit

**Base:** v0.47 development (commit `19df18577e2c7874c7fcbf8c615c96010c137a49`). **Target:** Bannerlord PC v1.5.7.125802. **Gate:** code review and static tests only until CI and a game playthrough.

## User evidence (19 November 2026)
Diagnostic ZIP `CU-Sandbox-Family-Diagnostics-20261119T111419Z.zip` is local evidence, **not committed to GitHub**. The final new Lord/Vlandia run saved PlayerAge=-1 (native age 50), SpouseAge=48, NobleRandom, ChildCount=6, ages 25, 22, 20, 18, 16 and 15. `cu_family_creation_status` returned `invalid_noble_origin:no_valid_parent_couple_in_selected_kingdom_or_clan`; the native contract recorded spouse absent, six requested and zero children. Earlier successful sessions recorded generated spouse age 21–25 and no child-age mismatch guard. The log contained no recorded slider drag and earlier sessions emitted native widget restore errors. The diagnostic collector could not locate the actual game install or hash the installed DLL.

## Changes
- NobleRandom/Chosen spouse ancestry now uses a real native birth clan even if no married native father/mother couple is old enough. Missing parent IDs are explicitly `unknown`, not fictitious. If no eligible birth clan exists, fail with an explicit reason. Existing valid parent couples continue to be linked.
- After native `CreateSpecialHero`, set the generated hero birthday from the requested age. Native donor templates, especially wanderers, can change the resulting age. Compare actual `Hero.Age` against requested age and log it both before and after marriage.
- Apply and verify each child's real native birthday. Promote adults to active clan heroes before residence placement. Keep underage children in the native minor state, with parent links and inherited appearance.
- Detect age or identity mismatch in `sandbox_start_contract`, not just spouse presence and number of children.
- No child ages are silently clamped after a parent-age edit. Display explicit age conflicts and reject invalid profiles at final confirmation. Log all saved parent/child age requests.
- Add **Enter exact age** alongside the slider and year buttons in the native, plain and safe stats prefabs. Instrument button/exact input. Retain native narrative age rewards.
- Keep native culture, FaceGen, narrative, banner, clan, review and difficulty owned by Bannerlord. Do not reenable earlier invasive control skin restoration on accepted CU starts.

## Honest verification boundary
Static source tests, .NET policy tests and game-reference compilation do not confirm Gauntlet mouse dragging, Encyclopedia age, marriage suitability for an older spouse, adult settlement presence, or save/reload. Do not mark these game tested.

## In-game acceptance before wider coverage
Use a disposable new Sandbox after backing up existing saves. Confirm actual installed DLL hash. Check World choice and native culture/FaceGen/narrative age; then Age 50 by slider and exact-age input, Attributes->Skills->Traits, Banner Back->Traits, and final Review/Difficulty. Household test A: Lord 50, NobleRandom spouse 48, six children [25,22,20,18,16,15]. Household test B: Lord 50, spouse 40, six children [24,23,22,21,16,15]. An invalid 25-year-old child with 40-year-old spouse must show an error and preserve the 25 input, never silently change it. Confirm spouse age in Encyclopedia, all six children in Clan with correct names, ages, and parents, adult activity and castle/town residence, and minor residence-only status. Save, exit, reload, verify no duplicated Heroes, wrong ages, missing family, stale profile, or new exceptions. Check a commoner spouse and NobleChosen fallback, starting role/fiefs, and native opt-out. Two clean new-campaign runs plus one save/reload pass are required to advance the gate.

## Review backlog
Exact slider dragging and UI scaling still require player screenshots at 1366x768 and 1650x927. The original native 20/30/40/50 story choice remains visible and controls its bonuses. Independent one-screen replacement remains a separate engine/UI scope, not proven by this patch. Earlier widget skin restore errors came from prior sessions and need a new-session zero-regression check. Wider historical mod coverage remains paused until this flow is accepted.
