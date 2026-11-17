# M0 verification: exact game startup and save/load

**Target:** Bannerlord PC v1.5.7. **Candidate:** Calradia Unified v0.9.0 (compiled untested until observed). This document is an executable test procedure for a real Windows game installation, not a claim of a passed test.

## Test prerequisites

1. Back up save files and move any older `CalradiaUnified` module folder out of `Modules`. Keep a clean official-only profile available as a control.
2. Extract the compiled artifact so the installation contains `Modules/CalradiaUnified/SubModule.xml` and `Modules/CalradiaUnified/bin/Win64_Shipping_Client/CalradiaUnified.dll`.
3. Verify the Native module's version is v1.5.7 and retain a screenshot of launcher module versions and load order.
4. Leave `EnablePhysicalConvoys`, `EnableCivicSystems`, `EnableEconomy`, `EnableAutonomousClanResupply`, `EnableAutonomousClanRecruitTrips` and `EnableAutonomousClanPatrol` set to `false` for the baseline session.

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
