# Calradia Unified v0.21.0 | Native start verification candidate

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Current status:** Gate B COMPILED_UNTESTED. [Successful Windows CI #44300627835](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44300627835): 578 pure gameplay-policy checks passed, 40 simulated campaigns completed, and 0 compiler warnings/errors. This is **not** a completed single-screen Sandbox overhaul.

## Intended complete creator

**Sandbox → one Calradia Unified editor → Begin Your Life**, with no Default/Advanced popup and no second culture, face or background wizard. It must include real native 3D appearance, culture, clan, kingdom, fief, spouse, children, skills, gear, resources and world scenarios.

Exact architecture and acceptance criteria: [v0.21 engine map](docs/V021_UNIFIED_CREATOR_ENGINE_MAP.md).
Full end-state tracking: [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).

## What this candidate actually changes

- Uses the v0.20 native Advanced Start mapping for genuine vassal, king, mercenary and other role choices.
- **Checks the observed game state after character creation.** New `SandboxStartContract.Validate` rejects mismatches between the chosen role and actual native StartType, kingdom, ruling status or fief count. It checks real spouse and child references, too.
- After the v1.5.7 native start runs, the live campaign behavior records `sandbox_start_contract result=PASS/FAIL` with status and failure codes. A requested Lord who starts as a default Wanderer now produces a logged failure and visible message instead of an unqualified success.
- **Reads the actual native appearance-stage surface** at creation time: `sandbox_appearance_engine_probe` records the face, culture, narrative and review stages and whether the native face view type is loaded.
- Adds executable pure-policy tests for native King/Lord/fief/family acceptance. The probe is observational and does **not** replace the current native screen or alter face data.

## Still unimplemented

The integrated 3D face preview, native character-editor skin, selected-fief ownership transfer, full native-wizard suppression and verified family creation remain open. The installed v0.20's gameplay outcome was not yet reported. These features must be tested against the real game engine before shipment.

Read [v0.20 actual start/family test procedure](docs/V020_NATIVE_START_FAMILY_ACCEPTANCE.md) first. Previous user-proven failures: [v0.18 UI](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/41), [v0.19 role/family](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/43).

## Build and test rules

- [Windows CI](.github/workflows/bannerlord-build.yml) checks source structure, policy tests, engine-reference compilation, package integrity and nested-ZIP installation.
- [Compiled, untested v0.21 module](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44300627835/artifacts/14426620218), intended for diagnostic testing only. It does not implement the integrated face editor.
- [Pure acceptance logic](Source/CalradiaUnified/SandboxStartContract.cs) runs in `Tests/PolicySmoke` with no Bannerlord game process.
- [In-game diagnostics](Source/CalradiaUnified/SandboxOriginBehavior.cs) observe the actual v1.5.7 campaign and creator stages.
- [Acceptance specification](docs/V021_UNIFIED_CREATOR_ENGINE_MAP.md) separates compile, 3D-preview, native-start, family, save/reload and UI polish checks.
- The installed native module versions, exact DLL hash and in-game output must all match before any feature is declared game-tested.

Set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` to restore the original Default/Advanced chooser if the experimental creator fails.
