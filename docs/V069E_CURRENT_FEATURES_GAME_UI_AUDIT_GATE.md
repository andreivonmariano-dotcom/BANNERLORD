# v0.69e | Audit-first freeze, existing gameplay and visual acceptance

**Decision: 21 November 2026.** Stop adding features, economic rules, autonomous NPC actions and new creator pages until the CURRENT compiled candidate is reviewed in an installed game. Stabilization priority is **actual operation, readable UI, safe navigation, truthful data and persistence**. This audit-only branch does not add gameplay code. Its parent is v0.69d draft PR #92, stacked on #91, #90 and earlier open PRs. Do not review outdated `main` as though it were the newest module.

## Evidence classification and current baseline

**Latest simulated-audit compiled candidate:** [Windows Actions 44863495218](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44863495218), compiled code SHA `fc17fa8599f103d8136beabc0ab779074b8b2062`, **900 executable C# policy checks passed**, zero C# warnings and errors against Bannerlord 1.5.7.125802 references. The deterministic models completed 1,062,792 medieval-life checks, 180,425 living-affairs checks and 303,098 campaign-policy checks. Those are NOT game-session playthroughs.

[Exact compiled but game-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44863495218/artifacts/14690753378). Installed module manifest/package still reads **v0.38.0**, not v0.69e. Compiled DLL: **588,288 bytes**, SHA-256 `0ceefa4e8d73dea686d28efb79fc6ba98a0d7830dfad273955d4cea788cb523b`. Inner runtime ZIP: **267,119 bytes**, SHA-256 `046c1c3471eed5b28ebf9fb26111b9a22fbbd4e2f2098e020eb07f471e308b46`.

**Gate status:** A/B compiled and policy-tested. For THIS exact DLL, **C (installed startup), D (live feature interaction), E (game save/reload), F (extended game session) and G (release)** are not verified. Keep drafts unmerged. Older v0.18–v0.48 screenshots and game logs document historical regressions but cannot certify the new build.

**Registry snapshot, not a completion claim:** `Data/Feature_Registry.csv` currently contains **55 CU feature rows**: 44 COMPILED, 4 BLOCKED, 3 GAME_TESTED, 3 IMPLEMENTED_SOURCE and 1 SCOPED. The three GAME_TESTED rows reference [issue #55](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/55), which describes a **v0.26** game session and still-open v0.27 regression. Those prior observations remain historically true but are not recent validation. It contains **78 HIST reference rows**, all IDENTIFIED, not rebuilt from original mods. Nothing in this audit upgrades a feature's gameplay gate.

## Findings from reviewing the current source and prior game evidence

- **P0 | Missing current in-game evidence.** The CI workload compiles against pinned references and runs source-level UI assertions. The screenshot comparator is called with `--self-test`, generating synthetic 1650×927 images, not current native/Calradia Unified captures. No repository screenshot assets were found. This build has not passed real screenshot, focus, hover, click, drag, animation, audio or save verification.
- **P0 | Unverified core creator.** `SandboxCreatorMenuLayer` keeps the native animated main-menu scene and hides only its UI roots; it logs `cu_world_native_menu_not_isolated` when suppression fails. `CUNativeStatsLayer` and `CUNativeHouseLayer` use `CUNativeCreatorUiIsolation`, with older opaque fallback prefabs. Whether the correct layer, model and native controls actually appear needs the installed client. Native Back/Continue, original 3D FaceGen, culture choice and campaign launch must be exercised together.
- **P1 | Resolution and text geometry.** Earlier real screenshots at **1650×927** showed overlapping native/CU UI, dimmed scenes, empty panels, undersized text and cramped Stats. Source repairs were made in v0.59e/f but have not been re-photographed. Existing docs warn **1280×720** parity is not proven. At minimum check **1650×927, 1280×720 and 1920×1080** at an explicitly recorded UI scale. Confirm the entire footer, nine Stats pages, long labels, tooltips and all hit targets without clipping.
- **P1 | Generated Review prefabs and assets.** The current Age/Stats and House/World flow requests `CUNativeStatsEditorNativeReview` and `CUGuidedCreatorNativeReview` first. CI generates both from the pinned native Review shell and checks bindings. If a movie fails, code falls back to older right-panel or opaque prefabs; identify the one the GAME actually loaded from `cu_native_creator_assets` and fallback events. The optional 19-image custom sprite atlas is not packaged as generated game assets, so native textures are the intended default, not evidence that custom art rendered.
- **P1 | Creator input reliability.** Old live regressions included an Age slider not responding to dragging, Age/Stats hard exits, a selected child count not being committed, and native Review Back not returning to House. Source repairs and static tests exist, but they need repeat mouse, keyboard, Back, escape and save tests on the present artifact. Do not infer UI success from XML parsing.
- **P1 | Family, retinue and genuine native starts.** The selected role, fief, spouse, children, siblings, party, castle/town residence and retinue must match actual clan, encyclopedia, party and map state. Phase-8 Advanced Start is not reversible. `SandboxFamilyRecoveryState` is read-only; it does not repair a failed partial family. `SandboxCulturalRetinue` deliberately refuses unsafe multi-companion replacement. A refusal with no discretionary CU rewards is a protective result, not a fully honored custom roster.
- **P1 | Native decisions and finance.** NPC requests and mediation now verify intended gold debits against BOTH Heroes and inspect actual relations. The saved `SandboxRoleEconomyState` ledger describes unattributed native money movements, not wages or taxes. Inferred `ClanHeadConsents` is not real NPC permission, so inheritance/estate transfer remains intentionally gated off. Test each live menu label for accuracy: never present disabled or inferred action as completed.
- **P2 | Historical documentation drift.** The module manifest and workflow name still read v0.38.0, the README begins at v0.47, and the PowerShell install-standard page retains an old v0.29 exemplar. Pin the SHA-256 and Actions run rather than selecting ZIPs by version label. Update cosmetic labels only after the runtime gate and save compatibility are reviewed.
- **P1 | Broad features are still compilation-only.** Depot grain, convoy parties, village harvest, recruitment, fiscal caps, garrison training, kingdom advisors, clan movement and independent NPC orders have many source-level tests but no current D/E evidence. Avoid turning them all on during the first creator smoke test.

## In-game acceptance sequence, no feature expansion

**Session A | Start, world, native identity, age and editor [C-001 / UI-101–109].** Start from the pinned binary with official Native, SandBoxCore, Sandbox and CalradiaUnified modules, exact active game version and recorded UI scaling. Open Sandbox from the genuine animated main menu. Record whether background animation continues, old logo/buttons are hidden, and Back/Escape restores the exact menu. Advance through native culture, genuine 3D FaceGen, original family/background and narrative choices. Use mouse and keyboard on each screen. Confirm native character/horse visibility, model rotation, no doubled headers, no missing images. Open custom Age: test drag from 18 to a chosen year, +/- and Exact, confirm *real* Hero age and story bonuses. Traverse **all nine All Stats pages, six fields per page** with Player/Spouse and Native/Balanced/Unrestricted modes. Check all 54 controls, values, clickable buttons, vertical spacing and focus. Go Back to Age and native narrative, then Continue to the real Banner stage.

**Session B | Banner, clan, House, Review and difficulty [UI-110–119].** Change banner colors and sigils, complete native clan naming. Open House & Fortune. Test spouse Yes/No, names, requested ages, child count 0/1/6, siblings, advanced sections, retinue mode, land and family-home validity messages. Invalid parental ages must refuse without silent resets. House Back returns to the legitimate previous stage. House Continue restores the ORIGINAL native Review. Change name and press native Review Back: it must re-open House with its saved inputs, then return without a loop. Finish the actual Difficulty and start map. Check for missing footer buttons, incorrect focus, underlying native click-through, invisible fields or duplicate reviews.

**Session C | Real starting contracts and family [D-101–114].** Make a disposable, small positive Lord/Vlandia native-vassal start with one requested spouse and one named child. Verify exactly one spouse, exactly one matching native child, exact ages, both parent links, home/party status, gold/fief and clan. Save and reload, check no duplicates. Then test a landless Recruit/NobleCadet with NO invented kingdom or fief, and one Soldier/Mercenary and one King native-start scenario. Audit all twelve roles individually before calling the role matrix game-tested. Test a real castle/town home and underage children separately from adult recall. Review family portraits. The remaining 12-role cases belong to the same acceptance gate, not a new-feature phase.

**Session D | Existing campaign systems [D-201–299].** Use a SEPARATE disposable campaign and turn on one currently compiled system at a time. Check the real menu, disabled states, cost, action, precise native result and log for: settings/diagnostics (64 settings); player supply and depot deposit/withdrawal; production and trade; physical convoys; six-culture recruitment; garrison training and wage cap; fiscal reports and clan gold; kingdom advisor, diplomacy, peacetime patrol and independent player-clan orders. Include a battle/mission, settlement entry/exit, army and siege boundary for any feature that mutates parties. An unavailable DLC, original mod, prefab or unsupported scenario must be reported as BLOCKED/SKIPPED with cause rather than silently PASS.

**Session E | Current living people and role features [D-301–399].** In a valid saved start, review People & Lineage, family history, origin station, service and livelihoods. Compare denar ledger against the game's wallet. With a real co-located adult NPC, accept and decline permitted requests and compare both Heroes' actual money and relation. Check that a high clan-leader relation alone does NOT transfer an estate. Revisit after advancing days and after death/kingdom change. Do not claim that the NPC has its own game AI merely because a modeled request was displayed.

**Session F | Persistence and safety [E-001–099].** Save at creator entry, after successful start, after resource transfer, after companion/family changes and during a pending request. Quit the GAME process, reload, then compare the actual Hero IDs, clan family, roster, fiefs, party, gold, relationship, settings, requests, economy ledger, residence and logs. Check no repeated awards, orphan Heroes, broken input or duplicate menu options. Test at least **30 campaign days** and one **60-minute** smoke playthrough. Use copies/disposable saves for deliberately invalid or failed native scenarios. Never delete or forcibly repair existing relatives to make a test pass.

## Screen-level acceptance

For each screen, the TESTER records the actual game capture and these separate results: **drawn** (correct textures/model/one panel); **legible** (labels fit, no clipped translations or overlapping text); **interactive** (hover and selected state, click target, sliders, Back/Escape and mouse focus); **native-continuity** (original stage, game 3D and audio continue, underlying controls do not accept click-through); **state-true** (selected values, real status, costs and eventual Hero changes). A screen is PASS only if ALL five are observed. A source test or model simulation is not a substitute for a screenshot plus input check.

Capture the same native Review reference and World, Culture, Face, Age, Stats (first/middle/last), Banner, Clan, House (simple and advanced), Review, Difficulty, map, Clan/Family, portrait gallery, People & Lineage, settings, recruitment, depot, garrison and finances. At **1650×927** hold UI scale constant to run `scripts/compare_native_creator_screens.py` with real screenshots. Its JSON `geometry_gate` checks an estimated left edge; `parity_certified=false` is deliberate, as image edge alignment does not verify text, click targets, 3D layers or actual Bannerlord runtime state.

Suggested evidence filenames: `A01_main_menu.png`, `A02_world.png`, `A03_culture.png`, `A04_face.png`, `A05_age.png`, `A06_stats_first.png`, `A07_stats_last.png`, `B01_banner.png`, `B02_clan.png`, `B03_house.png`, `B04_native_review.png`, `B05_difficulty.png`, `C01_clan_family.png`, `C02_map.png`, `D01_settlement_menu.png`, `E01_people_lineage.png`. Take additional captures for each error or input failure.

## Player-side pinned validation, PowerShell 5.1

From [exact artifact 14690753378](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44863495218/artifacts/14690753378), download the ZIP **manually**, then install using the pre-existing, source-audited PowerShell helper. The installer backs up the old CalradiaUnified module and does not touch saves. Verify the game root and downloaded filename on the player's machine.

```powershell
$GameRoot = 'D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$SourceCommit = 'fc17fa8599f103d8136beabc0ab779074b8b2062'
$ExpectedVersion = 'v0.38.0'
$ExpectedHash = '0ceefa4e8d73dea686d28efb79fc6ba98a0d7830dfad273955d4cea788cb523b'
$Zip = Join-Path $env:USERPROFILE 'Downloads\CalradiaUnified-v0.38.0-compiled-untested.zip'
if (-not (Test-Path -LiteralPath $Zip -PathType Leaf)) {
    throw "First download the pinned Actions artifact and set the actual ZIP path."
}
Set-ExecutionPolicy -Scope Process Bypass -Force
$ScriptBase = "https://raw.githubusercontent.com/andreivonmariano-dotcom/BANNERLORD/$SourceCommit/scripts"
$Installer = Join-Path $env:TEMP 'Install-CU-From-Artifact.ps1'
$Audit = Join-Path $env:TEMP 'Test-CU-Playthrough.ps1'
$NativeReviewAudit = Join-Path $env:TEMP 'Check-CU-NativeReview-Installed.ps1'
Invoke-WebRequest -UseBasicParsing -Uri "$ScriptBase/Install-CU-From-Artifact.ps1" -OutFile $Installer
Invoke-WebRequest -UseBasicParsing -Uri "$ScriptBase/Test-CU-Playthrough.ps1" -OutFile $Audit
Invoke-WebRequest -UseBasicParsing -Uri "$ScriptBase/Check-CU-NativeReview-Installed.ps1" -OutFile $NativeReviewAudit
& $Installer -GameRoot $GameRoot -ArtifactZip $Zip -ExpectedVersion $ExpectedVersion -ExpectedDllSha256 $ExpectedHash -ReplaceExisting
& $NativeReviewAudit -GameRoot $GameRoot
# AFTER playing, saving, quitting and reloading the game:
& $Audit -GameRoot $GameRoot -ExpectedVersion $ExpectedVersion -ExpectedDllSha256 $ExpectedHash -ExpectedChildren 1 -RequireNativeShell -RequireCUCultureSelection -RequireRealFaceScene -ReportJson (Join-Path $env:TEMP 'CU-current-in-game-audit.json') -Assert
```

Note: the `-ExpectedChildren 1` audit is valid for the one-child positive test, not for a zero- or six-child test. The native Review exactness script validates the installed game's original source files, not the screen's rendered pixels. The Playthrough report only reads current-session diagnostics, does not drive the game and cannot certify actual screenshots or save/reload by itself.

For crashes, invoke `scripts/Export-CU-Diagnostics.ps1` from the same pinned source revision. Review a ZIP for personal paths before sharing; no saves are bundled by default.

## Reporting and stop gates

Log each case with: **case ID, exact DLL hash, native game version, active module list/load order, test resolution/UI scaling, starting profile, screenshot/video path, real observed result, related event or exception, PASS/FAIL/BLOCKED/NOT RUN, date**. Preserve failed screenshots, not only passes. Treat a native menu that loads a fallback as a valid recovered UI only after the fallback itself meets all five screen criteria.

**Block merge/release** if any of these occurs: current artifact fails to boot, a creator page cannot be completed with mouse and keyboard, original native stage cannot be returned to, UI text/controls are unreadable or unreachable at the tested resolution, a selected family/role differs materially from actual Hero/Clan state, a native transaction double charges or reports a false success, save/reload duplicates a character/resource, or an exception corrupts a campaign.

**Existing feature freeze:** No new roles, systems, visual re-skins, simulated wages, AI goals or content migrations until the audit report lists direct PASS for the core creator and representative campaign gameplay plus an explicit unresolved-defect queue. Fix verified P0/P1 defects in isolated reviewable patches, replay their exact in-game case, then update the feature registry's status per feature only with corresponding evidence.

## Audit outcome at creation

**Source inventory completed. Compiled baseline grounded. IN-GAME VISUAL AND GAMEPLAY ACCEPTANCE STILL NOT RUN FOR v0.69d.** This is a test protocol and review record, not a claim that the player's installed Bannerlord currently runs and displays everything correctly. Previous screenshots prove defects in older versions; they cannot establish their presence or absence in the current code.


## Simulated QA update, 21 November 2026

User-directed Q1/Q2/Q3 stress tests now pass Windows CI without adding gameplay code. [Latest simulation details](V069E_SIMULATION_AUDIT_RESULTS.md): 24 logical creator hitbox/visibility cases across three declared viewports, 10,000 modeled navigation cycles, 96×730 campaign model days and 1,441,129 checks, plus 12×48×365 modeled role-days and 979,198 assertions with 7,488 multi-state serializer checkpoint groups. The 900 independent C# gameplay policy checks and pinned module compile passed with zero errors/warnings. The installed-game protocol above now identifies the **exact same-run CI artifact, scripts SHA and DLL SHA** from 44862791276. Previous run hashes remain correct only for their own artifacts.

This simulation success does not advance the installed-game acceptance gate. Real screen rendering, actual UI click/drag focus, first game launch, native 30-day campaign and save/quit/reload remain unverified.


## Verified source-defect correction and latest build

The latest [run 44863495218](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44863495218) contains a single bounded runtime repair: `SandboxStartContract.RetinueReadyForOtherGrants` refuses incomplete or malformed success receipts. Existing native retinue creation, class features and Gauntlet views were not changed. Policy smoke passed **900** checks, and the separate Q1–Q3 simulator passed **999,201** assertions, including 20,000 receipt stress cases. Module compilation had zero warnings and errors. The PowerShell validation recipe above is pinned to **this new run's code SHA and DLL hash**, not earlier candidates. [More detailed result](V069E_SIMULATION_AUDIT_RESULTS.md). In-game Gate C–G remains not run.
