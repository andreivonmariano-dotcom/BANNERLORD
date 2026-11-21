# v0.69g | First live gameplay evidence: Younger Brother of Lord denied all requested family

**Evidence received:** 21 November 2026, a user-provided Calradia Unified session diagnostic ZIP. The private ZIP was reviewed in the connected conversation, **not committed, redistributed or linked here**. It included `events.csv`, `runtime-info.txt`, `READ_ME_FIRST.txt` and engine log tails, but no screenshots, native saves or managed exception report.

**User-visible failure:** They selected **Vlandia, Lord, Younger Brother of a Lord**, a spouse, two children and two siblings. On reaching the campaign, neither the older native lord nor the requested family appeared.

## Grounded session timeline

The runtime loaded DLL SHA-256 `80b6f6bc725d8855ad18adf2fabcddccb57ab2a13b59a5e7393558bc083e2740` and internal manifest `v0.38.0`. The session reports no managed exception and a normal shutdown. The DLL SHA matches the immediately prior [Windows CI installer-test run 44864970515](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44864970515), not the separately pinned v0.69f installation candidate from run 44864139228. The two successful builds are not interchangeable by hash.

The actual event sequence was:
- World role/noble-birth selection proceeded into native Sandbox stages.
- **04:24:17 UTC**, House preview recorded `birth=YoungerBrotherOfLord` and **`No eligible native elder found for this background`**. The UI still allowed Continue.
- **04:24:28 UTC**, `cu_guided_house_fortune_committed` recorded `spouse=True|spouse_age=38|children=2|siblings=2|noble_birth=YoungerBrotherOfLord|retinue=RoleBased`, with child ages 9 and 7. This is conclusive evidence that the user's household choices were **saved**.
- **04:24:30 UTC**, native Review/Options finished. **04:24:31 UTC**, after native Advanced Start phase 8, `cu_starting_life_preflight` reported `result=FAIL|reason=noble_sibling:no_eligible_native_elder|role=Lord|noble_birth=YoungerBrotherOfLord`.
- The next event `sandbox_start_contract` was `result=FAIL|reason=preflight_noble_sibling:no_eligible_native_elder|no_custom_grants_applied=true`. No `cu_family_loaded_plan`, `cu_family_creation_status`, `cu_family_spouse_created`, `cu_family_child_created`, or generated sibling events appeared. Family generation was never entered.

The immediate cause was **not a failed save of spouse/children or a late native Hero creation exception**. The upstream noble-lineage availability check failed, and the existing fail-closed policy suppressed **all** custom phase-9 work.

The older-lord search in `SandboxDynasticStart` requires a real existing older native lord with suitable kingdom, culture, living/active clan leadership, appropriate royal rank, and **both registered original parents**. The logs do **not** reveal which of these per-candidate conditions eliminated each Vlandian lord. The House preview established that no candidate passed the combined conditions. There is no evidence that the selected lord can be made a valid native sibling without modifying an existing NPC's ancestry or inventing kin.

## Scoped source repair for this failure mode

**Repair rather than a fabricated noble:** Before House can Save/Continue into the irreversible original native Review/Options, the existing family plan now receives a **read-only projected native elder/father check** using game-owned live Heroes, their true ancestry, sex, culture, age and kingdom. A negative result **blocks House confirmation** and displays an explanation. The player stays in the custom editor. The cursor automatically returns to **Confirm your noble birth**.

**Correctable choice:** House now exposes the **existing NobleBirthPosition selector** after native culture/age have become known. `NobleElderHeroId` remains optional in Advanced. A Lord can explicitly switch to **Independent** before native finalization to keep requested spouse, two children and two siblings. The entire uncommitted House draft remains in memory, including names, ages, wealth and retinue. The screen preview recomputes if the noble option or requested elder changes.

**Safety boundary:** This does **not** create a fictional older native lord, automatically downgrade an invalid younger-brother selection, or bypass phase-9 strict native genealogy validation. A true younger-brother origin only succeeds if a qualified actual game-owned elder exists. If no qualified elder exists for the selected setup, that specific origin cannot honestly be provided. Other family creation steps may still fail for separate native reasons, and require actual testing. An already-ended failed start cannot retroactively be repaired by replacing the DLL: use a disposable new game for QA.

## Regression tests and acceptance

- `scripts/test_v069g_noble_house_preflight.py` asserts House has a recoverable noble-choice field, generated House draft is retained, error focuses the correct field, the availability check runs before `SaveForPlayer` or completing native Review, and the existing strict phase-9 gate remains.
- `Tests/PolicySmoke/Program.cs` executes Vlandian YoungerBrotherOfLord example inputs (age 40, spouse 38, children 9 and 7, two siblings), checks the pure genealogy policy refuses a parentless elder, and verifies switching the *existing* noble position to Independent retains every selected child/spouse/sibling setting across serialization.
- The older `v0.61` test expected NobleBirthPosition to be World-only; it now explicitly allows a House correction. This is a deliberate change based on the failed live-session evidence, not an accidental removal of the World choice.
- The full pinned Bannerlord 1.5.7.125802 compilation, 907+ policy checks, 24 UI XML geometry scenarios, and campaign simulation gates remain required.
- **Actual in-game retest remains required:** same origin should stop at House with an honest message, not start an empty household. Select Independent, confirm spouse, two children and two siblings survive, verify all exact native Heroes, parents and clan references, and repeat save/quit/reload. True older-lord lineage still needs a native eligible elder. Game-overlap, click focus and UI-scale tests remain outstanding.

**Gate status:** **D failure evidence received** for the old build. This is not a D pass for the repair. Do not merge or release based solely on policy-source and reference-compilation evidence.


## Verified compiled repair and new-test contract

[Successful Windows CI run 44866582823](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44866582823) built exact revision `63a82b14bde2ccc07eea9a15a9745aa6ae3693fb` with **910 executable C# gameplay checks passed**, **1,159,203 audit policy assertions**, 70,080 modeled campaign days with **1,441,129 additional policy checks**, and **0 compiler warnings or errors**. The v0.69g House preflight source regression passed and the native Review XML model retained 24 logical visibility/hitbox cases.

- [Exact new compiled game-untested ZIP](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44866582823/artifacts/14692466071), inner runtime ZIP **267,668 bytes**, SHA-256 `0d6c1ed8882dc89457b85d2e9e17ecc2949b8691cd96b6498c93e1397a2c7368`.
- DLL **590,848 bytes**, SHA-256 `2f1cad7fd25359fe5eef0f25e5ef8dde91a4f3e334f6c0a694f55849a9381ab0`.
- Updated [PowerShell `-ShowLatest` / `-LatestVerified` installer](../scripts/Install-CU-From-Artifact.ps1) now pins **v0.69g** to this actual successful source+artifact SHA, retaining historical in-game manifest `v0.38.0`.
- The earlier **v0.69f** session had different DLL SHA `80b6f6bc725d8855ad18adf2fabcddccb57ab2a13b59a5e7393558bc083e2740`, so do not mix files across CI runs.

**Required direct game verification:** Create a disposable Vlandian Lord/YoungerBrotherOfLord start, select a spouse (38), children (9, 7), and two siblings. On an ineligible elder, verify native Review never begins and the House screen focuses the noble-birth choice with all selections retained. Explicitly choose **Independent**, then finish the same House plan and verify one actual native spouse, two actual children and two native siblings in the real game. Check individual parent links, clan, age, menu, real save/quit/reload and that no duplicates appear. **This v0.69g source repair is not a successful native-elder-binding test.** It makes an unsupported start recoverable rather than creating a false lord or quietly discarding family choices.


## Follow-up clarification: an older House sibling does not become the native lord

**User's question:** After selecting **Younger Brother of a Lord**, will creating an **older brother** in House automatically make the new brother the lord? **No.** The two selections refer to different native Hero workflows.

`SandboxDynasticStart.FindElder` searches `Hero.AllAliveHeroes` for an **already present** eligible older native lord and demands that he lead a qualifying native clan, share proper living/native parentage, and fit culture, kingdom and royal-rank rules. `SandboxFamilyCreator` later makes any requested sibling through `HeroCreator.CreateSpecialHero`, links them to native parents, and for the younger-brother origin places them in the **player's party**, not as a separate existing native clan leader or fief owner. The generated adult may have TaleWorlds `Occupation.Lord`, but that does not imply control of a kingdom clan, a land grant or eligibility as the pre-existing elder.

**UI refinement:** World noble-birth hint, House `Confirm your noble birth`, House `Brothers and sisters` and its dynamic explanatory text now state that a newly generated older sibling cannot qualify as the pre-existing older lord. The v0.69g source-only regression checks this wording, without changing native kinship, clan leadership, property or family creation order. This is a communication repair, not proof that a compatible native older lord exists in the user's save.

**Bounded design principle:** Do not silently promote a newly created sibling into another native clan or move settlements as a workaround. That would be a distinct gameplay feature with native political and save implications, not a repair of the reported failed starting-life contract. The qualified-elder check remains before native finalization.


## Verified v0.69g UI clarification recompile

The user asked whether a newly generated older brother in House automatically becomes the lord required by `YoungerBrotherOfLord`. The answer is **no**, and the House/World hints were refined to state this before the player finishes. The next successful [Windows CI run 44867186334](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44867186334) compiled source `8c2382461522907db63d5890e2204864a336e056` and passed **910 executable C# policy checks**, 1,159,203 policy model assertions, 1,441,129 extended campaign model checks, 24 modeled UI hitbox cases and 0 compiler warnings/errors. The expanded v0.69g source test verifies the explicit existing-native-lord versus generated-older-sibling distinction.

**Latest compiled v0.69g with corrected creator wording:** [artifact 14691813274](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44867186334/artifacts/14691813274). DLL **590,848 bytes**, SHA-256 `d6d9ad9a1ec217d5482a46442597461944c1cf2fa52f185c87309b654dba6f10`; inner runtime ZIP **267,725 bytes**, SHA-256 `694d28c2e8b2f43177492b4dcd56422fe969ca7d48097984cf6e688042adfd4a`. The preceding successful run 44866582823 is an older compiled v0.69g revision without this extra UI explanation. **Both binaries remain untested in an actual new native campaign.** The PowerShell latest-candidate pin must use this new run's matching DLL, never mix its hash with the prior artifact.
