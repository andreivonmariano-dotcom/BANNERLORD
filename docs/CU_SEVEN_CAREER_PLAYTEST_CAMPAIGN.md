# CU | Seven-career native playtest campaign
25 November 2026. This is an executable manual QA route, not a completed playthrough. Candidate identity must be recorded at execution; no prior version certifies a changed DLL. Current target Bannerlord 1.5.7.125802 / War Sails 1.3.7.125802; curated 41-module profile. Follow [native acceptance checklist](CU_V074_GAMEPLAY_ACCEPTANCE_CHECKLIST.md) and [56-case matrix](../Data/CU_Six_System_Native_Test_Matrix.csv).

## Prior observed session, not current acceptance
Private diagnostic archive CU-Session-20261125T122817_ca382bca-CLOSED.zip inspected: 1,085 events, one Lord start contract PASS (vassal, three fiefs, spouse and three children), 12 native correspondence-open receipts, one confirmed 100-denar family gift. Normal shutdown callback; no recorded managed exception. The session lasted about 28 minutes; hash 6726da1024f4754cc65fcca9381fe6b44f642b3c05f53bc147b2fd3b7fd7c656 differs from current builds. No separate reload archive, native screenshots, verified current specialist/governor action or 60-minute/30-day evidence. Prior garrison concern cannot count as a completed order. Priority: Lord first, then Soldier provider, Knight sworn service, King authority, Wanderer, Outlaw, Mercenary; independent saves, not seven starts in one logger session.

## Stage 0 | exact preflight
Verify installed DLL against a successful exact artifact and manifest; save the exact game/DLC builds, selected module IDs and order, relevant provider hashes and effective config. Back up old module and saves. Use copied saves and separate test campaigns. Do not install, change profile or enable experimental flags without authorization. Start control profile (official dependencies + four required BUTR frameworks + CU; SaS for Soldier), then actual full profile. Do not silently change difficulty, cultures or resources between comparable runs. Record resolution, build, test day and player ID.

## Stage 1 | Wanderer: Independent road company

**Play route:** Choose one companion; enter native Sandbox, recruit or trade through native UI; appoint a vacant party specialist if eligible; return to town.

**Pass observation:** No fief, no contract, real companion/native wages; actual assignment; no duplicate grants.

**Native matrix:** S1-1;E1-1;S3-1;S3-06;S4-1;S6-1. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 2 | Outlaw: Band under pressure

**Play route:** Start real six-bandit company and lieutenant; inspect food/wage reserve; use native/Fourberie crime action only where legal in game; speak to lieutenant, assign eligible role or defer.

**Pass observation:** Actual roster and native costs; no CU crime or foraging mutation, no free replacement troops.

**Native matrix:** S1-2;E1-2;S3-2;S4-1;S6-3. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 3 | Mercenary: Contract and survival

**Play route:** Accept actual native employer; fight or perform native employer service; inspect pay and contract; end contract in copied branch; reopen Chronicle.

**Pass observation:** Actual employer/native wages; no CU salary; real end state and history.

**Native matrix:** S1-3;E1-3;S5-5;S6-2;S6-3. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 4 | Soldier: Service under a lord

**Play route:** Use exact compatible SaS Recruit path; verify host, lord, rank and actual duty; witness pay/service; try household command during enlistment, then reload; provider absent on separate control branch.

**Pass observation:** Actual SaS authority; no duplicate wages or party-role mutation while enlisted; unavailable status not fabricated discharge.

**Native matrix:** S1-4;E1-4;S3-07;S5-5;S6-3. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 5 | Knight: Oath in action

**Play route:** Start independent landless with patron and Squire; visit patron, muster, actual escort/scout, report; seek recommendation or release in copied branch.

**Pass observation:** Actual sworn agreement and evidence; no unearned fief, crown or vassalage.

**Native matrix:** S1-5;E1-5;S5-1;S5-2;S5-3;S5-4. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 6 | Lord: A house to protect

**Play route:** Start real vassal with family and fief; visit spouse, gift once, revisit concern; use vacant specialist or governor; inspect garrison via provider; save/reload.

**Pass observation:** Native kin and ownership; exact gold transfer, actual assignment, no CU recruiter; native correspondence closes safely.

**Native matrix:** S1-6;E1-6;S2-1;S2-5;S3-05;S3-08;S4-2;S6-5. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Stage 7 | King: The burdens of the crown

**Play route:** Start native ruling clan and real court; inspect minister governorship; convene actual eligible council; assign vacant specialist; test loss of seat/crown in copied branch.

**Pass observation:** Native authority and court identities, no repeated grant or held marshal transfer; loss recorded honestly.

**Native matrix:** S1-7;E1-7;S3-09;S3-04;S5-6;S6-2. Save immediately before any mutation, then a separate named after-save. Quit to desktop, relaunch and load that exact after-save. Verify native identity/role/kin/roster/holdings and repeat or attempt invalid action. Record visual and native evidence; do not manufacture a missing provider, screenshot or result.

## Shared stress pass
Test scene/map transitions, siege, army, naval state, stale actor, missing funds, wrong owner, occupied raw slot including absent holder, duplicate clicks, native removal of current duty, death/capture/authority loss, unread archive and safe exit. Full stack: 30 campaign days and at least 60 minutes continuous native play with comparable CU-on/off timing, memory, frame pacing and save-size samples. Test copied historical saves containing depot/convoy, old gatherings, peer/story records and former camp menu; no automatic native party destruction or replay. Record every FAIL/BLOCKED/NOT_APPLICABLE with reason. Do not mark a test passed based on absence of a log.

## Evidence folder and checker
For each career and candidate/profile, retain actual original session ZIPs (start.zip and reload.zip or distinct filenames), screenshots (native before and after reload), private save label and actual observed actor/party/clan IDs, role-specific before/after facts, steps, expected/actual result and reviewer observations. No saves or private logs in the repository. Use [evidence template](playtest/role-evidence-template.json), copy to <role>.json and fill with actual evidence. The checker scripts/audit_cu_current_playtest.py accepts a folder with seven role JSON files and their real closed session archives and screenshots. Supply --dll-sha256 for the exact candidate and --profile for the exact test profile. It checks identity, session uniqueness, normal close, real start-contract markers, role-specific receipts, native correspondence fallback and separate reload evidence fields. It emits REVIEW_READY only for a complete mechanically consistent record; manual review remains mandatory. It cannot authenticate gameplay, screenshot content, operator honesty, role ownership or save identity solely from metadata. Missing or contradictory evidence blocks readiness. Old audit_v070 remains historical; do not require retired graphical or garrison-transfer receipts.

## Enjoyment review
After each career record 1–5 clarity, agency, immersion, repetition (5 = low repetition) and enjoyment plus concrete examples, minutes played, decision/action counts, clicks to action, failed actions and UI exits. Use actual observed evidence to prioritize P0 native safety, P1 functional and usability problems, then P2 polish. Scores stay blank before play. No invented playtest findings.

[Seven routes](../Data/CU_Seven_Career_Playtest_Campaign.csv) | [Scenario register](../Data/CU_Moment_To_Moment_Scenarios.csv) | [Development tracker](CU_DEVELOPMENT_TRACKER.md).