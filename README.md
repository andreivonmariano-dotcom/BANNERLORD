# Calradia Unified v0.20.0 | Real Native Starts & Family Candidate

**Game target:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Status:** compiled-source candidate until exact Windows Actions and live-game validation are recorded. Do not call this a final release.

## Why v0.20 exists

A user successfully launched v0.19 and entered **Lord** plus family details in the custom creator, yet the game launched an ordinary native Default Start with no family. The source explanation is concrete: v0.19 never set the native `StartType` or `KingdomId` fields and attempted profile effects too early.

v0.20 keeps the direct **Sandbox → CU creator** main-menu action and adds true native start values:
- **Lord / Knight → native Vassal**, **King → native Ruler**, **Soldier → native Mercenary**, Wanderer → Default, Trader/Outlaw/Beggar → their native options. Admiral is refused unless a native provider offers its start type.
- Native political starts require a valid **Starting Kingdom** choice. The creator refuses the Apply action with a clear message if it is missing. Selecting Lord now means requesting the actual Vassal game start, not a descriptive label.
- The Location page contains native world Scenario, faction IDs, modifiers and optional seed, passed to the engine's `AdvancedStartOptions` instead of displaying the Default/Advanced picker.
- **Spouse/children requests** are treated as family creation requests. Actual `HeroCreator` / `MarriageAction` runs only after the engine's native start effects, at `OnCharacterCreationIsOverEvent` phase 9. It rejects children without spouse and fresh-start siblings without native parents. Runtime failures are recorded explicitly.
- The custom screen has category navigation, a native-brush frame, buttons and separate panels. Rendering and input are still owed a screenshot and game test.

## Completion boundary

This fixes the functional start-data handoff, but **does not yet replace** Bannerlord's video, culture, native face/appearance, background and finalization screens. A true single-screen total conversion with a live 3D character preview, full family setup, and no native wizard is tracked in [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44). Do not label v0.20 as that finished feature.

The equipment-theme preset, age request, starting-town preference and some family appearance fields are not fully implemented and are labeled accordingly in the editor. Actual spouse and children depend on native templates, ages, sex restrictions and successful engine references. Use disposable new campaigns.

## Build, installation and tests

- [Windows build workflow](.github/workflows/bannerlord-build.yml), including 570 policy checks, campaign simulation and a regression guard for actual Vassal/King/Family startup wiring.
- [v0.20 runtime acceptance test](docs/V020_NATIVE_START_FAMILY_ACCEPTANCE.md).
- [v0.19 underlying direct-main-menu feature](docs/V019_DIRECT_SANDBOX_ENTRY.md).
- [v0.19 player-reported gameplay failure](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/43).
- [Full Sandbox overhaul acceptance](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).
- [Feature registry](Data/Feature_Registry.csv).

The compiled release candidate should be installed as a single `Modules/CalradiaUnified` module using `scripts/Install-CU-From-Artifact.ps1`, which backs up the previous installed module before replacement. The current compiled candidate URL, ZIP hash and DLL identity must come from a successful *v0.20 branch* GitHub Actions run. The installer and source checks never prove in-game behavior.

Setting `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` restores the original Default/Advanced selector.
