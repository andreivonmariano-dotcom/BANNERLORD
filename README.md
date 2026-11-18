# Calradia Unified v0.13.0: castle finance and manual garrisons

**Target:** Bannerlord PC v1.5.7. **Release status:** compiled candidate only after passing the exact v0.13 CI run; native gameplay and saves require in-game acceptance. No old mod DLLs, no installer, one `Modules/CalradiaUnified` folder.

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

Version provenance is the exact commit and associated GitHub Actions run. Do not equate source tests, simulated campaign days or a Windows reference DLL build with a game-tested release.
