# Calradia Unified v0.14.0: native in-game settings, no third-party frameworks

**Target:** Bannerlord PC v1.5.7. **Status:** source candidate pending the exact v0.14.0 Windows GitHub Actions Gate B run and real in-game launch. This includes all v0.13 castle finance, staff transfer, and 78-mod priority work.

## New in v0.14

Enter a **town or castle** and choose **CU: Configure Calradia Unified**. The new native Bannerlord campaign menu contains three pages: Reports and Warnings, Economy and Recruitment, and Castle Management. Each flag has an Enable or Disable choice with the opposite state automatically hidden. Two supported numeric controls can be increased or decreased one step at a time.

Changes write to a user-owned file at `%LOCALAPPDATA%\\CalradiaUnified\\calradia-user-config.ini` and take effect within the same campaign. This **does not write to the installed module directory**. Old `ModuleData/calradia-config.ini` values remain the baseline and are overridden only by saved user settings. Unknown user-edited keys and comments are preserved. Failed saves do not change live settings.

Existing dangerous automated orders, experimental convoys, auto garrison training, and other advanced controls remain off by default and are not exposed through the native menu. The native menu, profile persistence and startup still need game testing.

## Internal equivalents rather than copied frameworks

- **Harmony:** existing safe native campaign event hooks, no arbitrary method patcher.
- **ButterLib:** existing local log, managed exceptions and diagnostic export.
- **UIExtenderEx:** add campaign UI through official native game menu registration. No Gauntlet widget patcher.
- **MCM:** new native in-game settings categories, opt-in toggles, numeric controls and persistent user overrides. No general third-party settings extension API.

No Harmony, ButterLib, UIExtenderEx or MCM DLL, package or separate module is needed.

## Existing documentation

- [Native settings and framework capability limits](docs/NATIVE_FRAMEWORK_CAPABILITIES_v014.md)
- [Castle management v0.13 acceptance](docs/V013_CASTLE_MANAGEMENT.md)
- [Full 78-mod priority register](Data/Reconstruction_Priority_78.csv)
- [M0 real-game test procedure](docs/M0_TEST_PROCEDURE.md)

**Release status:** COMPILED_UNTESTED only after a successful exact v0.14 workflow. Do not claim a real game launch, save/reload or MCM compatibility from reference-assembly compilation.
