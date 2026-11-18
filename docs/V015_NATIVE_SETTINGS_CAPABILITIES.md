# Calradia Unified v0.15.0: all native settings and experimental Gauntlet screen

**Version source:** `Modules/CalradiaUnified/SubModule.xml`; [Gate B evidence](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44251639925), commit `e4d8ef685c8bd06135fcbe39d86ff65caaa036d5`. This is not real gameplay testing.

The project does **not** contain or claim all of Harmony, ButterLib, UIExtenderEx or MCM. It implements only features needed by Calradia Unified: native event handlers, diagnostic journals, self-contained campaign/Gauntlet menus and profile-backed settings.

## All setting fields

The native menu exposes **64** actual `Settings` fields (24 Boolean feature flags and 40 integers). They are divided into nine pages:

1. General
2. Diagnostics
3. Supply and foraging
4. Depots and economy
5. Recruitment
6. Kingdom and diplomacy
7. Clan AI and reports
8. Physical convoys
9. Castles and estates

Each setting opens a detail menu showing current value and either Enable/Disable or bounded numeric steps. Bounds are taken from the existing INI parser, not UI-only guesses. Flags that can affect gameplay are labeled experimental. Access does not activate them. Both `EnableAutonomousClanPatrol` and `EnablePhysicalConvoys` remain false in shipped defaults.

## Experimental tabbed/slider Gauntlet screen

The optional custom Gauntlet XML prefab `GUI/Prefabs/CUSettingsPanel.xml` and `NativeSettingsPanel.cs` use the official game UI classes. Three tab buttons group the 64 options into Core, Kingdom, and Castles. Previous/Next cycles settings. The `SliderWidget` shows a selected integer value, with explicit Save. The menu fallback continues to work without opening this experimental screen. The compiled screen is **not evidence of rendering, mouse events, viewmodel binding or focus**.

## Persistence

User overrides are written to `%LOCALAPPDATA%\CalradiaUnified\calradia-user-config.ini` with a temp-file replacement and read back over the safe shipped defaults. The current live Settings object updates only after a successful write. No original game save keys change.

## Actual game test still pending

For real v1.5.7 startup, menu navigation, tab/slider usability, crashes, and same-save persistence, use [the concrete runtime matrix](V015_RUNTIME_ACCEPTANCE.md). [Read-only preflight](../scripts/Run-CU-Runtime-Preflight.ps1) checks files and hashes without starting the game, and [runtime evidence validation](../scripts/validate_runtime_evidence.py) reads existing diagnostics without inventing successful runtime results.
