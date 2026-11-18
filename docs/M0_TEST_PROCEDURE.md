# M0 verification: exact game startup and save/load

**Target:** Bannerlord PC v1.5.7. **Candidate:** Calradia Unified v0.13.0 (compiled untested until observed). This document is an executable test procedure for a real Windows game installation, not a claim of a passed test.

## Test prerequisites

1. Back up save files and move any older `CalradiaUnified` module folder out of `Modules`. Keep a clean official-only profile available as a control.
2. Extract the compiled artifact so the installation contains `Modules/CalradiaUnified/SubModule.xml` and `Modules/CalradiaUnified/bin/Win64_Shipping_Client/CalradiaUnified.dll`.
3. Verify the Native module's version is v1.5.7 and retain a screenshot of launcher module versions and load order.
4. Leave `EnableManualGarrisonTransfers` and `EnableManualGarrisonWageCaps` false. Leave `EnablePhysicalConvoys`, `EnableCivicSystems`, `EnableEconomy`, `EnableRecruitment`, `EnableGarrisonTraining`, `EnableAutonomousClanResupply`, `EnableAutonomousClanRecruitTrips` and `EnableAutonomousClanPatrol` set to `false` for the baseline session.

## M0 test sequence

- **C0.1 Launcher:** Open the launcher with the official modules plus Calradia Unified. Expect the module to be recognized with no missing-DLL dialog. Record PASS/FAIL.
- **C0.2 Startup:** Start the game and create a *new campaign*. Expect map and settlement menu to open without a reproducible crash. Record PASS/FAIL.
- **C0.3 Registration:** Check `%LOCALAPPDATA%\CalradiaUnified\calradia-unified.log` for a matching module initialization and campaign behavior registration event. Logs are corroborating evidence, not substitutes for observation.
- **E0.1 Save:** Save in a friendly town after the campaign reaches the world map. Record save identifier and in-game day.
- **E0.2 Reload:** Quit Bannerlord completely, relaunch the same build, load that save and re-enter a settlement. Expect identical party/inventory state and no exceptions. Record PASS/FAIL.
- **E0.3 Regression:** Enter and leave a friendly town, fight one small battle, return to world map and save/reload again. Check for party movement and mission-loading errors.
- **E0.4 Control:** Reproduce any failure once with official modules only, using a separate fresh save. Do not overwrite the test save.

## Diagnostic collection

Run from PowerShell after the session:

```powershell
./scripts/Collect-M0-Evidence.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord' -OutDir '.\M0-Evidence'
```

The collector only reads the game manifest, DLL identity and the last 250 local mod-log lines. It produces `m0-summary.json` and a log tail. Review the report for private paths or player identifiers before posting to a public issue.

## Result record

| Test | Result | Actual observation | Evidence |
|---|---|---|---|
| C0.1 launcher | NOT RUN | | |
| C0.2 campaign startup | NOT RUN | | |
| C0.3 log events | NOT RUN | | |
| E0.1 saving | NOT RUN | | |
| E0.2 quit/reload | NOT RUN | | |
| E0.3 battle/settlement regression | NOT RUN | | |
| E0.4 official-only control | NOT RUN | | |

**Gate status:** CI compilation alone qualifies for Gate B. M0 Gate C and E stay open until observed game results and the exact tested build are attached to [M0 tracking](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/1).

## v0.12.0 diagnostic gate

After a campaign session, confirm that `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\finance.csv` contains castle and clan-summary rows from different day indices. Export from the in-game menu, then run the module's PowerShell helper to collect existing engine and crash logs. Attach the sanitized ZIP to [diagnostics issue #33](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/33), not the raw memory dump. A managed exception record is not a native crash report. Native game save/load and crash tests remain NOT RUN.

## v0.14 native settings menu smoke test (still unexecuted)

1. Keep all experimental settings false. Enter one town and one castle and open **CU: Configure Calradia Unified** from each.
2. Visit Reports and Warnings, Economy and Recruitment, and Castle Management. Go back to the current settlement without leaving it unexpectedly.
3. Disable and re-enable Supply warnings. The opposite option should replace the prior option. Set the low supply threshold to four days.
4. Verify `%LOCALAPPDATA%\CalradiaUnified\calradia-user-config.ini` contains the changed keys and original comments/unknown settings remain intact.
5. Exit and relaunch the same campaign. Confirm the values reload and old campaign save-state keys are undisturbed.
6. Make the override file read-only and attempt another setting change. The menu should warn and leave the live setting unchanged. Restore permissions.
7. Repeat menu entry after settlement ownership changes, a siege, a save/reload cycle and a new campaign. Check exported diagnostic events for exceptions.
8. Enable each gameplay-changing control only in a disposable test campaign after proving the vanilla-only baseline. Report exact module version, game version, observed behavior and logs.

## v0.15 full catalog and optional Gauntlet screen

See [V015_RUNTIME_ACCEPTANCE.md](V015_RUNTIME_ACCEPTANCE.md) for the installed-game preflight, nine-category coverage, three-tab Gauntlet panel, slider and save/reload matrix. As of this source revision every M0 in-game item remains **NOT RUN**. A GitHub Actions build does not perform a native Bannerlord gameplay test.

## v0.16 Expanded Sandbox origin game acceptance (not yet run)

1. In the official Bannerlord v1.5.7 launcher, use the compiled v0.16 candidate with the three official modules. Confirm the title-screen **CU: Design Next Sandbox Character** entry appears without duplicates after returning from a campaign.
2. Open the five-tab experimental origin panel. Change the role, family profile, gold, one attribute, one trait, and an exact known game item ID. Close and reopen. Check `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` for correct persisted values. Record screen images and input results.
3. Start a separate **new Sandbox**, not Story Mode. Test first with `Enabled=false`, then with an enabled profile in another disposable new game. Compare the native hero gold, influence, troop roster, attributes, skills, focus, traits and item inventory to requested profile values.
4. Save, exit and reload the enabled campaign. Compare the inventory and gold before and after reload, and confirm the origin is not applied twice. Check the `CU_v016_origin_applied` save flag through event evidence, not assumed success.
5. Choose an official King/Vassal Advanced Start and verify its native kingdom status remains correct. Selecting a "King" profile alone must never create a fake kingdom.
6. Verify any spouse/child profile fields do not claim to create real family heroes. Check a single-parent profile, spouse and children, and invalid ages. Actual family creation and relocation remain unimplemented.
7. Repeat the previous M0 mission-entry, siege, ownership-change and scene-departure regressions, comparing against an official-only control and collecting sanitized diagnostic ZIPs.

**Gate C/D/E status: NOT RUN.** Native UI XML and reference DLL compilation are Gate B only.

## v0.17 native Starting Life experimental acceptance

Use a disposable new Sandbox on Bannerlord PC **v1.5.7.125802**. The profile at
`%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` has three new Boolean
flags, all false by default: `NativeStageEnabled`, `RealFamilyEnabled`,
and `RoleAffiliationsEnabled`. Turn on only one experiment at a time.

1. Native stage test: Set `NativeStageEnabled=true` and keep the other two false. Start native Sandbox. Confirm culture, appearance, family background, early childhood, adolescence, youth, adulthood, starting age, banner, clan naming, review and difficulty still work. Test Back and Next through every added Starting Life page. Verify role/family/resources/equipment selections persist in the profile. Record any change to the original final narrative menu.
2. Disable all flags, then start a new control Sandbox. Confirm no custom pages appear and normal start and save/load still work.
3. Family test: Prepare legal spouse/child names and ages. In a disposable new game set `Enabled=true` and `RealFamilyEnabled=true`. Confirm the hero is in the player's clan, native spouse pointers are reciprocal, every child's parent pointers match, ages and genders match, and the family appears correctly in the encyclopedia. Save, quit and reload. Compare IDs before and after.
4. Role test: In a separate new game set `RoleAffiliationsEnabled=true`. Verify Soldier receives a real **mercenary clan** contract, Knight and Lord receive real **vassal clan** status, and King works **only if** the native Advanced Start established a ruling clan. A label alone must never grant a crown.
5. Check a single-parent start, existing native spouse, missing character template, unsupported native parents for siblings, and a mismatched King role. All must refuse the dangerous operation without creating false relatives or titles.
6. Inspect diagnostics and compare renown, recruits, faction map, clan UI, hero biographies and the exact same save after restart. Do not promote Gate C, D or E from compilation alone.
