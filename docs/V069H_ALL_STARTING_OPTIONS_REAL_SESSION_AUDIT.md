# v0.69h | All starting options: evidence audit and live-game acceptance

**Date:** 21 November 2026
**Branch:** `audit/v069e-current-gameplay-and-visual-acceptance` (draft PR #93, unmerged)
**Scope:** Audit existing options. Stabilization only. No new starting role, title, native character action, or inheritance system.

## Evidence and first P0/P1 results

Two *different* real sessions have demonstrated that entering the native campaign does not prove the selected Starting Life completed.

1. **Earlier 04:23 session (Vlandian Lord, YoungerBrotherOfLord):** House saved spouse age 38, two children ages 9 and 7, and two siblings. House preview reported no eligible native older clan leader. Phase 9 reported `noble_sibling:no_eligible_native_elder`; no CU household was created. v0.69g blocks that unsupported choice in House. The corrected binary has not been verified in-game.
2. **New 04:54 session (Vlandian HouseholdKnight, Independent):** The World creator committed `role=HouseholdKnight|kingdom=vlandia|scenario=none`; direct native start requested `type=default`. Native culture, FaceGen, narrative, Age (selected 25), Banner, Clan, House, Review and Options progressed. House committed `spouse=False|children=0|siblings=0|retinue=FamilyOnly|fief=Native`. At **04:55:24 UTC**, phase 9 refused `native_start:native_start_type_mismatch`, then `no_custom_grants_applied=true`. A campaign heartbeat followed, and the game closed normally. There was no recorded managed exception. **This was not a successful HouseholdKnight start.**
3. **Exact evidence boundary:** The second ZIP has `events.csv`, engine log tails and `runtime-info.txt`, but **no save, screenshots, actual native clan/party roster export or native start-type value**. It ran older DLL SHA-256 `80b6f6bc725d8855ad18adf2fabcddccb57ab2a13b59a5e7393558bc083e2740`, not the newer v0.69g compiled candidate. The ZIP is private and is not committed to GitHub.

## v0.69h bounded source repair

`SandboxCreatorOverlay` passes `options.GetChangedOptions()` to `new Campaign(...)`. `SandboxNativeStartOptions` explicitly sets `StartType=default` for four independent roles. The real session proves a native start-type mismatch, **but did not record the actual returned value**. A missing/default option is a plausible cause, not a proven native-return string.

- `SandboxRoleConsistencyPolicy.NativeStartTypeMatches` accepts an absent native start type **only** for requested `default`, with no kingdom affiliation, ruling-clan status or fiefs. A nonempty wrong type is still refused. King, vassal, mercenary and fleet-admiral starts still require exact native IDs.
- The same constrained comparison is used in both `SandboxStartingLifePreflight` and `SandboxStartContract`/role station checks.
- The new `cu_native_start_preflight_snapshot` event logs the **raw** requested and actual type, actual kingdom, fiefs and ruling-clan status before any grants. Its purpose is to identify the exact native value in the next session. It does not invent a patron, fief or marriage and does not bypass required `HasNativePatron`.
- The old fail-closed start barrier remains: a rejected start does not execute optional phase-9 grants or retry on load.

**Until a new in-game HouseholdKnight session verifies the actual native receipt, this is a candidate repair only.**

## Full starting option inventory

### World and native ASO

- **Six scenarios:** Native Sandbox (`none`), United Empire, Last Stand, Two-Faction War, Invasion and Alternate Calradia.
- **Scenario parameters:** First faction, second faction where appropriate, and Alternate Calradia variant (`default`, `fractured`, `shattered` variants in game-native identifier form).
- **Five native modifiers:** Fast Mode, Bandit Surge, Civil Unrest, Recruit Shortage and Swift Travel. Seed accepts native random or a chosen integer.
- **Kingdoms:** Vlandia, Sturgia, Battania, Northern/Western/Southern Empire, Khuzait and Aserai. The empty id is a separate independent/default option. Actual native provider existence is checked in `TryPrepare`.
- **Direct handoff:** `TryPrepare` validates the native `StartType`, `KingdomId`, `Scenario` and scenario-specific IDs before starting a campaign. The native runtime must still prove that the selected options took effect.

### Twelve player roles

These are **source-to-native expectations**, not live success claims:

- `Wanderer`: independent native `default`, no fief or oath. **Not game-tested in this audit.**
- `Recruit`: independent `default`, real patron context, landless. World confirmation requires the supported Serve as Soldier public API; actual enlistment must happen through that provider, not fabricated by CU. **Not game-tested.**
- `Soldier`: native `mercenary` with a real kingdom, plus a verified superior. Not a native enlisted troop. **Not game-tested.**
- `HouseholdKnight`: independent `default`, landless, plus a real patron from the selected kingdom. **Old-build live FAIL at native start-type preflight; new conditional fix not retested.**
- `Knight`: native `vassal`, an actual kingdom and estate, plus a personal commanding lord. **Not game-tested.**
- `NobleCadet`: independent `default`, landless, requires real eligible native family ties rather than an imaginary parent/clan. **Not game-tested.**
- `Lord`: native `vassal` with actual kingdom/holding. Independent House forbids a player-created older brother but permits older sisters. **YoungerBrotherOfLord old-build live FAIL; the Independent variant remains untested.**
- `King`: native `king`, actual ruling clan and at least one fief. **Not game-tested.**
- `Admiral`: native `fleetadmiral` with required kingdom context and the actual DLC-provided start option. **Not game-tested.**
- `Trader`: native `trader`; no fictional title or kingdom accession. **Not game-tested.**
- `Outlaw`: native `outlaw`; no fictional kingdom accession. **Not game-tested.**
- `Beggar`: native `beggar`; no fictional kingdom accession. **Not game-tested.**

### Noble birth, family and fortune

- **Nine noble positions:** Independent, younger brother/sister of an existing lord or king, second son of a lord/king, third son of a lord/king. A real native older lord or parent is required for all non-Independent options. An older created sibling is not a previously existing clan leader.
- **House:** spouse request, spouse name/age/sex/culture/background, 0 to 6 children with ages/genders/upbringing, 0 to 4 siblings, parental age limits and actual native genealogy. All family grants require native Hero/Marriage/child proof, not just a saved draft.
- **Independent Lord succession constraint:** The player is the eldest male of the created household. An older sister, younger brother or same-age brother is allowed. An older brother is blocked without changing their chosen sex or age automatically. This is a creation contract, not native clan leadership or inheritance transfer.
- **Retinue choices:** Native existing companions, RoleBased cultural officers, or FamilyOnly. The legacy `ReplaceNative` value remains supported in saved profiles. Verify actual party roster, same culture and removal of unrelated companions rather than trusting text alone.
- **Fortune:** Native or explicit gold and influence, additional recruits, native holding or an authorized Castle/Town/Specific same-kingdom gift, and optional household residence. Landless starts cannot promise owned estates. No unrelated fief may be silently seized.
- **Native editor:** Actual culture, FaceGen, origin narrative, age 18–80, nine stats pages, banner, clan name, native review and difficulty. Logs about page checkpoints are not screenshots or proof of mouse behavior.

## Acceptance gates and test order

**Source/CI gates:** The v0.69h Python regression inventories 12 roles, 6 scenarios, 9 births, four fief modes and critical handoff. The C# policy suite explicitly tests guarded omitted-default receipts and all 12 native-type mappings. The pinned Windows reference compilation, scenario static tests, simulation suite and installer hash gates remain mandatory. Passing these gates does not certify live Bannerlord play.

**Native game gates, all currently outstanding for the new binary:**

1. Start a disposable new Sandbox on the **exact CI artifact** and record the DLL SHA and generated session ZIP. Check `cu_native_start_preflight_snapshot` against the actual native type/kingdom/fief state.
2. First reproduce **HouseholdKnight/Vlandia/Independent**, age 25 and no family. Require `cu_starting_life_preflight|result=PASS` and final `sandbox_start_contract|result=PASS`, then verify a real independent clan, no owned fief, and verified eligible native patron. If the native actual type is a nonempty wrong value, preserve the failure and analyze that exact value.
3. Then cover each of the other eleven roles, verifying native political affiliation, actual fiefs, patron, service, retinue and starting resources. A rejected unsupported role is **not a pass**.
4. For Lord, test Independent with older sister and younger brother, then negative older brother, then each eligible native noble family origin. The latter requires a named real parent/elder and true genealogy, not generated titled relatives.
5. Run each world scenario and variant with appropriate faction IDs. Verify the real campaign changes; do not infer success from the picker.
6. Repeat family, retinue, home and land transfers as separate cases. Inspect the exact Hero, spouse, children, parents, party membership, clan/fief ownership, dates and receipts.
7. Save/quit/reload and verify no loss or duplicate grants. Advance 30 real campaign days for systems whose effects occur over time. Check 1650×927, 1280×720 and 1920×1080 visual captures for obscured controls.

**Release status:** Retain PR #93 as draft, the feature freeze, and no release claim until all P0/P1 cases are proven inside the actual game. Do not mark failed or untested variants as working.

## Player-side evidence-reader correction

The existing `scripts/Test-CU-Playthrough.ps1` had another audit gap: it searched for the old `sandbox_overhaul_accepted` editor event when deciding which campaign to inspect. The current guided creator records `cu_guided_world_start_committed` and `cu_guided_house_fortune_committed` instead. A genuine guided test could be reported as **INCOMPLETE** or the wrong historical campaign could be selected. The reader now anchors the latest event from **either** creator, reads the guided House child count, distinguishes the creator source in JSON, and requires the selected start's phase-9 contract. A synthetic CI regression confirms a recent guided **FAIL** overrides an earlier legacy **PASS**, and a guided start without a contract remains **INCOMPLETE**. These tests evaluate the reader only, not a real playable campaign.

## First compiled v0.69h audit candidate

[Successful Windows CI run 44869209844](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44869209844) built source `84807350fe6292dee073fca7c511e462f57f2f6e` prior to the evidence-reader refinement. **949 C# gameplay policy checks passed**, including the role/native-type matrix and an omitted-default start restricted to a verified landless independent clan. The 96×730-day simulation passed with 1,441,129 checks; 210,240 modeled audit days passed with 1,159,203 assertions. Native reference compilation: **0 warnings, 0 errors**. Compiled DLL SHA-256 `95929a3cacea278f7ddaab3a3b979265d2708dc475969d4a46059ff2fa607521`, runtime ZIP SHA-256 `beaec8b4d0108bdc975f45a52f8fa92452d53a915f8ec08f85bca0d29f45a4d5`. [Exact artifact 14692919174](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44869209844/artifacts/14692919174). The later evidence-reader changes require a separate CI pass before a new artifact is selected. The manifest continues to read historical `v0.38.0`.

## Final combined audit and guided-reader CI

[Windows Actions run 44869433822](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44869433822) **PASSED** for compiled source `31118824bb1e68cb2674f6b05a0ecb6663e3d589`. The v0.69h source audit, guided-session negative fixtures, **949 executable C# gameplay policy tests**, 96×730-day model simulation (**1,441,129 checks**) and 210,240 modeled audit days (**1,159,203 assertions**) all passed. The pinned Bannerlord 1.5.7.125802 compilation reported **0 warnings, 0 errors**. The guided reader correctly classified the synthetic later `HouseholdKnight` native-start mismatch as **FAIL** despite an earlier legacy **PASS**, and an incomplete guided session as **INCOMPLETE**.

**Exact new compiled, game-untested module:** [artifact 14692874756](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44869433822/artifacts/14692874756), DLL **593,920 bytes**, SHA-256 `249a5e61ec1993247735f059773c4419b0aea163ae2fd5c3c1488b5a6627a55f`, inner runtime ZIP **268,517 bytes**, SHA-256 `28d4326f4d6453fe5bb162683c5a84a1794d35b2821224bdbfd6fe9f0255c67a`. Historical manifest still says `v0.38.0`. The latest pinned v0.69g installer catalog refers to an *older* artifact. Do not select `-LatestVerified` with the new ZIP; install only with its exact manual DLL SHA-256 and back up the current module.

**Current verdict:** Existing source and model gates passed. The earlier live noble start and landless knight start both failed. Neither new fix has received a passing game retest. New binary remains **not accepted for release**. The next upload should include `cu_native_start_preflight_snapshot` and an actual native campaign inspection.
