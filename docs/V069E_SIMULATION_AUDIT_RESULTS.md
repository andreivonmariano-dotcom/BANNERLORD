# v0.69e | Q1 Q2 Q3 | Existing features simulated QA

**Decision, 21 November 2026:** User requested all three audit tracks by **simulations** before adding new gameplay. This is a test-only continuation of the [current feature and visual acceptance freeze](V069E_CURRENT_FEATURES_GAME_UI_AUDIT_GATE.md). No new C# mod behavior, Gauntlet prefab, game start, character feature, native world modifier or module manifest change is requested or implemented here.

## Evidence classes

- **Direct policy execution:** Compiles actual game-independent C# policy source files into a separate net8.0 `Tests/AuditSimulator` executable. Its simulated Heroes, wallets, family counts and requests are fake input fixtures. They are **not TaleWorlds objects**.
- **Creator XML model:** `scripts/simulate_v069e_creator_hitboxes.py` loads the **generated version-pinned native Review shell prefabs**, evaluates mutually exclusive ViewModel visibility flags, checks command uniqueness and logical rectangles, and detects overlaps. This is not Gauntlet rendering.
- **Campaign policy model:** `Tests/CampaignSimulator` runs the existing depot/grain, convoy, recruitment, garrison, kingdom order, trade and feudal stress model with more seeds and modeled days.
- **Windows reference compilation:** Existing pinned `Bannerlord 1.5.7.125802` DLL packaging gate is retained. Compile result cannot certify in-game UI, mouse/pointer routing, 3D world, pathfinding, native SaveGame serialization or an installed client.

## Q1. Current creator, UI and navigation audit

The XML simulation exercises **World/House** choice, boolean, numeric, numeric-with-native-default and text states and **Age/All Stats** Age state, six-row and partial-row Stats states. It verifies all visible action bindings are unique, that their modeled clickable rectangles stay within the 573×750 native Review inner panel, and that mutually exclusive actions do not collide. The same logical controls are checked against the declared viewport list **1650×927, 1280×720, 1920×1080**. The shell is 694×1080 native **logical** units; the test intentionally emits an `unscaledPanelHeightExceedsScreen` flag instead of claiming real Bannerlord scaling or pixel clipping. Outer Back/Continue, hover, font metrics, scene rendering and input must be checked in game.

The C# audit simulator runs 10,000 deterministic Age→All Stats→Back cycles, validates the native Review/House Back gating policy, tests all nine six-row groups for 54 stats, drives the 18–80 age pointer-to-year policy, and rejects child ages that breach the 16-year parent-to-child interval. These are pure decision-path checks, not end-to-end input events or an attached character creation state.

## Q2. Existing settlement and campaign systems

Run the existing campaign simulator **96 seeds × 730 modeled days** with deterministic seed **20261121**, in addition to the standard workflow's **40 seeds × 365 modeled days**. It tests modeled depot limits, grain conservation, convoys, recruitment, daily caps, training, expenditures, fiscal and strategy policies. It cannot create a real `MobileParty`, enter an encounter or verify a native settlement menu. The matching `BuildResults/audit-campaign-stress.json` is attached to the Actions simulation artifact, not copied from gameplay.

## Q3. Family, NPC agenda, denar and save-state stability

`Tests/AuditSimulator` runs **12 role labels × 48 seeds × 365 modeled days** (210,240 modeled days). Across modeled days, it verifies native-*number* balance arithmetic, bounded request/relationship/wealth-based offers, 35-day expiry and 70-day request cooldown, role descriptions that never invent wages or automatic jobs, partial family counts that never auto-certify identities, and two-sided gold-delivery evidence. At each simulated checkpoint it serializes and parses **economic, family recovery and agency** state, rejecting lost data or duplicated requests. These are string `Serialize/Parse` tests. They do **not** load Bannerlord's own campaign save format or test a character surviving game reload.

## Required reports and acceptance

Expected distinct CI outputs:
- `BuildResults/v069e_creator_hitbox_simulation.json` with visibility/hitbox cases, viewport flags and `nativeScreensRendered=false`
- `BuildResults/audit-campaign-stress.json` with `engineExecuted=false`
- `BuildResults/audit-personal-stress.json` with `nativeUiRendered=false`, `gameSaveReloadExecuted=false`, `realNativeTransactionsExecuted=false`
- existing `BuildResults/policy-tests.log` and compiled DLL/package reports

A passing simulation means **no policy or logical hit-box invariant failed in the tested model**. It is not a certificate that all features work in the game. An XML-model success does not prove visible text, real hover state, working controller/mouse input or no overlap in the actual rendered client. The latest-version Windows game screenshot/input gate and genuine 30-day/save-reload tests from v0.69e remain **NOT RUN**.

**CI evidence:** Await current exact branch-head successful Actions run. Record passing counts, run commit and report artifacts only after the job completes. Do not replace prior `v0.69d` verified runtime hash with a guessed one.
