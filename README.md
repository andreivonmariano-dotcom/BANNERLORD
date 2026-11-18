# Calradia Unified v0.19.0 | Direct Sandbox start candidate

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Development branch:** [feature/v019-direct-sandbox-entry](https://github.com/andreivonmariano-dotcom/BANNERLORD/tree/feature/v019-direct-sandbox-entry).
**Status:** source candidate, pending compilation and in-game validation. Not a certified playable release.

## Change requested after the confirmed v0.18 screen failure

Clicking **Sandbox** in the Bannerlord main menu must open **Calradia Unified's own creator immediately**. The built-in **Default Start / Advanced Start** selection page must not appear during this path.

The replacement intercepts the existing `SandBoxNewGame` menu action rather than adding another option or opening over a native character creation state. The custom editor collects identity, household, progression, equipment, starting resources, starting town and review data.

Pressing **Apply / continue** commits an origin profile and starts a new *default-scenario* Sandbox through the v1.5.7 engine API, without showing the built-in start-type chooser. Pressing **Escape** cancels back to the main menu. An explicit opt-out, `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`, restores Bannerlord's original Sandbox action.

**Completion boundary:** This bypasses the Default/Advanced choice, but **does not yet replace** the game's intro cinematic, native culture/face controls, clan naming or final confirmation screens. Custom royal status, naval command, family heroes and non-default ASO scenarios remain experimental or unimplemented. The v0.18 floating-text user screenshot is real evidence of UI failure. The new native-brush controls must pass an observed click and screenshot test before they can be called functional.

## Installation and verification

The goal is still one paste-ready `Modules/CalradiaUnified` folder containing the module manifest, one `CalradiaUnified.dll`, configuration and the native UI prefab. The build runs static XML/policy checks, a Windows v1.5.7 reference compile, runtime-archive verification and an installer simulation.

Read [v0.19 direct Sandbox entry and acceptance procedure](docs/V019_DIRECT_SANDBOX_ENTRY.md), [the v0.18 user-reported failure](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/41), and [the full feature registry](Data/Feature_Registry.csv). Exact compiled artifact links and hashes belong in the verified CI run, not in this pre-build document.

## Existing systems

The earlier independent supply, depots, economy, recruitment, garrisons, NPC orders, native settings, origin profile and diagnostics remain in the source tree. Most gameplay-changing settings are disabled by default. No source or CI success proves that these features are game-tested. The historical 78 mods are tracked as reconstruction references rather than complete replacements.

- [Build workflow](.github/workflows/bannerlord-build.yml)
- [Installed game preflight](scripts/Run-CU-Runtime-Preflight.ps1)
- [Feature registry](Data/Feature_Registry.csv)
- [Native test procedure](docs/M0_TEST_PROCEDURE.md)
- [78-mod coverage](docs/78_MOD_COVERAGE.md)
