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

## Verified Windows CI outcome

[Successful Windows Actions run 44862791276](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44862791276) compiled and executed exact source SHA `2b220bf94d15a98e94342bdb163f883c370615df`. Documentation-only updates after the run do not change its executable source. The new creator XML model passed **24 visibility/hitbox cases at three declared viewports**, with zero modeled button collisions. The new campaign stress simulation passed **96 seeds × 730 modeled days = 70,080 modeled days and 1,441,129 policy checks**. The older baseline 40-seed × 365-day simulator independently passed **303,098 checks**. The twelve-role audit completed **12 × 48 × 365 = 210,240 modeled days**, with **979,198 policy assertions, 7,488 serializer checkpoint round-trips, and 10,000 modeled creator-navigation cycles**. All **899 independent gameplay policy smoke checks** passed.

Compiled pinned Bannerlord 1.5.7.125802 module: **0 compiler warnings, 0 errors**. Exact DLL **588,288 bytes, SHA-256 `4047d5eda6c4bc191a7a50345be4ef3bd8e67d1a073a1a64cd07d70b79d01461`**. Inner runtime ZIP **267,062 bytes, SHA-256 `b0ae5826fb57380775de85a611405d7d7881f3ab890398cf46f8b27ad32f8350`**. The DLL hash differs from the earlier v0.69d compiled run despite no gameplay source edits in this audit branch. For any installation, use the **same run's exact artifact and DLL hash**, never the prior v0.69d hash. Historical module manifest remains `v0.38.0`.

- [Current compiled, game-untested module artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44862791276/artifacts/14691045274)
- [Actual JSON model reports and policy-test evidence](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44862791276/artifacts/14691035373)
- [Full run and job steps](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44862791276)

**Audit verdict:** All configured models, policy assertions, generated XML hitbox cases, compilation and Windows packaging tests passed. Highest completed actual-gate level remains **B**. No live game boot, actual rendered screenshot, Gauntlet focus/click/drag, native party traversal, in-game 30-day run or game save/quit/reload was exercised by CI. All applicable gate C–G in-game tests remain **NOT RUN**. Keep PR #93 draft, preserve the user-requested feature freeze and gather current-version actual game evidence before repairing unconfirmed visual problems or resuming Q1/Q2/Q3 feature growth.
