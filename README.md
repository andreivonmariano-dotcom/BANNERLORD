# Calradia Unified v0.15.0 | Complete settings catalog and experimental Gauntlet UI

**Target:** Bannerlord PC v1.5.7, no installer, one `Modules/CalradiaUnified` folder and one custom DLL. **Highest evidence: Gate B COMPILED_UNTESTED.** The [v0.15 Windows run](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44251639925) verified source, 78 historic module IDs, **530 independent policy checks**, 40 deterministic simulated campaigns (14,600 days and 303,098 checks), game-reference compilation (0 errors, 0 warnings), and packaged ZIP integrity.

## v0.15 changes

- All **64** declared configuration settings from `Settings.cs`: **24 Boolean feature flags and 40 bounded numerical values**, across nine native campaign-menu categories.
- Each setting has a separate detail page. Experimental flags require opening that page before choosing Enable. Defaults are unchanged, and autonomous orders and convoys are still **off** by default.
- New **EXPERIMENTAL** native Gauntlet screen with three tab buttons, selected setting navigation, Boolean toggle, and a native slider with an explicit Save action. Its XML is packaged inside the same module folder and has **not** been verified in the real game.
- Persistent per-user settings at `%LOCALAPPDATA%\CalradiaUnified\calradia-user-config.ini`. It overrides the bundled `ModuleData/calradia-config.ini`, leaves the installed module unchanged and updates active shared settings only after a successful write.
- New runtime-evidence validator and Windows preflight script, with conservative status classifications that do not assert game startup, visual rendering or save/load without observed in-game evidence.

### Opening settings

Enter a town or castle. Choose **CU: Configure Calradia Unified** for the nine-category native settings menu. From its main page choose **EXPERIMENTAL: Open native tabbed slider panel** to try the three-tab screen in a disposable test campaign. The native menu remains the fallback.

## Historical frameworks: external DLLs remain unnecessary

- **Harmony:** native campaign event handlers only, no general method patching API.
- **ButterLib:** local logging, managed exceptions, diagnostic ZIPs, no ButterLib API compatibility.
- **UIExtenderEx:** official native menu and a self-contained Gauntlet prefab, no arbitrary third-party prefab patching.
- **MCM:** the project's own settings controls and persistence, no compatible MCM plugin API.

## Game-only acceptance

- [v0.15 startup, nine-category, native slider and save/load test matrix](docs/V015_RUNTIME_ACCEPTANCE.md)
- [Windows installed-game preflight](scripts/Run-CU-Runtime-Preflight.ps1)
- [Session and settings event analysis](scripts/validate_runtime_evidence.py)
- [Original 78-mod priority register](Data/Reconstruction_Priority_78.csv)
- [Castle management controls and limitations](docs/V013_CASTLE_MANAGEMENT.md)

**Verified compiled ZIP:** [CalradiaUnified-v0.15.0-compiled-untested](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44251639925/artifacts/14403429717). Do not call the module game-tested or released until Bannerlord v1.5.7 launcher, screen rendering, gameplay, and same-save persistence have been verified.
