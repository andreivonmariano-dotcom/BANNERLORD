# Calradia Unified v0.13.0: castle finance and manual garrisons

**Target:** Bannerlord PC v1.5.7. **Verified Gate B:** [Windows CI run 44246191394](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44246191394), commit `0f7c194240e0797001abec402127a86357cbc898`, 199 policy checks passed, 14,600 simulated campaign days, zero compiler warnings/errors, ZIP CRC and one-DLL package validated. **Release status: COMPILED_UNTESTED.** Native gameplay and saves still require in-game acceptance. No old mod DLLs, no installer, one `Modules/CalradiaUnified` folder.

## New in v0.13

- Castle estate briefing shows **modeled income required** and **modeled shortfall** against known wage costs. Forecasts are not the game's observed native tax entries.
- Castle staffing report shows real existing roster counts, wage totals and current limits.
- Optional manual deposit or withdrawal of five **existing** native-culture basic recruits between your player party and castle garrison. No autonomous party is sent.
- Optional manual castle wage cap action only if existing garrison wages already fit. Both new gameplay flags default to **false**.
- Complete editorial priority ranking of all original 78 mod IDs, with a validated formula and source traceability.

## Documents and evidence

- [Castle-control rules and game-only acceptance](docs/V013_CASTLE_MANAGEMENT.md)
- [78-module ranking and methodology](docs/RECONSTRUCTION_PRIORITY_v013.md)
- [78-row ranking CSV](Data/Reconstruction_Priority_78.csv)
- [Finance and crash diagnostics](docs/DIAGNOSTICS_v012.md)
- [Baseline start/save procedure](docs/M0_TEST_PROCEDURE.md)
- [Historical registry](Data/Feature_Registry.csv)

## Existing features retained

Includes depot grain transfers, guarded physical convoy candidate, locally cultured recruitment, bounded clan NPC orders, budgeted garrison training, modeled estate harvests, and exportable finance/crash logs. Game-changing features remain disabled by default pending actual v1.5.7 play tests.

Verified v0.13 module ZIP: [CalradiaUnified-v0.13.0-compiled-untested](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44246191394/artifacts/14401771689). DLL SHA-256: `3a342e8d310f46029ab2974c3e01e752875156a64240efeb3998585d4e8da67b`. Inner module ZIP SHA-256: `ffcd046634498e919b5a465d979cf2af5994f63f11a23e3daeb868ee7f560a5a`. Version provenance is the exact commit and associated GitHub Actions run. Do not equate source tests, simulated campaign days or a Windows reference DLL build with a game-tested release.
