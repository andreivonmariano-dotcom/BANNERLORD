# v0.39 development milestone: zero-toggle native creator bridge

**Target:** Bannerlord PC v1.5.7.125802. **Branch:** `feature/v039-automatic-native-creator-bridge`, based on the v0.38 face-dock trial. **Manifest:** still v0.38.0 until the complete creator meets its release gates.

**User's locked final contract:** click Sandbox, see ONE full-screen Calradia Unified creator, complete Starting Life / Bloodline / Fortune / Land & Home / Review Your House, use ONE Begin Campaign action, and arrive on the campaign map. Native culture, 3D body/face, origin narrative bonuses, age, clan/banner, difficulty, Advanced Starting Options, real spouse/children and fiefs must match the chosen settings. The real 3D player model stays visible in the right pane throughout the five chapters. The native creator must not reappear after confirmation.

## Code implemented in this milestone

- Intercept the original `SandBoxNewGame` option with `SandboxMainMenuEntry`; this was already implemented in previous builds.
- When a new CU Sandbox plan passes validation, `SandboxCreatorViewModel.ExecuteApplyAndContinue` automatically sets `NativeStateShellEnabled=true` and `NativeFaceDockTrialEnabled=true`. The old extra native 3D review and Face Workshop stages are disabled so no duplicate stage view is registered.
- `IsAvailable(Field)` hides all four old experimental creator controls. No player must open More options or turn an experimental mode on.
- The native-state shell delegates culture selection, Back and Continue to Bannerlord's actual culture VM. It preserves original native stage callbacks, face/body data and finalization. The live FaceGen right-dock attempt operates on the original engine-owned scene and widgets. It reverts to stock layout on a failed binding or with F9.
- A fresh or cancelled campaign still resets family/fief authorization. The original vanilla Sandbox menu action remains a failure recovery path.
- `scripts/test_auto_native_creator_bridge.py` guards the zero-toggle acceptance path, no extra stages, native callbacks and truthful user-facing handoff. The Windows workflow now runs this gate.

## Q1: two phases but one uninterrupted screen (NOT complete)

Native Advanced Start options for role, kingdom and scenario are passed to `new SandBoxGameManager(() => new Campaign(...gameOptions))` before `CharacterCreationState` exists. The existing `CUSandboxCreator` screen is pre-campaign and still closes before the game-owned culture/face/narrative stages. A seamless final compositor must preserve one persistent visual shell or equivalent visually identical state transition across this barrier. The pre-campaign UI must not imply an instantiated Hero/FaceGen scene exists before the campaign creates it.

## Q2: native stages behind our five tabs (NOT complete)

The real culture stage already has a CU-bound controller. FaceGen remains native, with a reversible right-dock trial. Narrative menu `OnSelect`/`OnConsequence`/`ApplyFinalEffects`, age, native banner editor, clan naming, review and difficulty still use stock visible stage views. To remove their visible pages, each CU subpanel must bind to the existing runtime VM and call its actual stage callbacks exactly once, preserving Back/Next, option-specific state, cancellation and finalization. Merely hiding or deleting native stage registrations would lose gameplay effects.

## Q3: persistent real player 3D across every chapter (NOT complete)

The current scene exists during `CharacterCreationFaceGeneratorStage`. Exiting that native stage finalizes its `BodyGeneratorView`, its scene and input layers. The v0.38 dock trial only moves that scene during FaceGen. A persistent right-side player needs a tested engine-owned scene lifetime or game-backed preview that follows the actual `BodyProperties`, with proven clipping, camera framing, zoom/orbit input and mouse hit regions across stages. Do not clone the actual player Hero, fabricate faces or preserve a finalized native render target.

## Verification and release gate

Windows CI run [44454724380](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44454724380), source SHA `0a6b0702b0825b62893cf9555f5a56591e4c80f6`, passed the new gate, 656 independent gameplay policy checks and v1.5.7 net472 module compilation, with zero compiler warnings and errors. The exact compiled DLL SHA-256 is `f2ccb4422a300889411042b8dcc741435d591c68f930b68a141d79e739b7a5d5`. The inner runtime module ZIP passes CRC and has one module DLL. **Verification level: Gate B, COMPILED_UNTESTED.**

A final production release still requires both of the following:
1. A real game session proving the complete visual flow `Sandbox → one CU creator → Begin Campaign → map` without a visible stock creator, at 1366×768 and 1920×1080. Confirm 3D preview, camera input, controls, native narrative bonuses, clan/banner/difficulty and all five tabs.
2. Two new campaigns with actual roles, spouse/children and fiefs. Save, quit, reload, verify exact gameplay state and no duplicate character creation, and check diagnostic errors and performance.

**Do not label this branch as a completed single-window creator.** Its achievement is the removal of the manual opt-in and safe compilation of the automatically attached native bridge. Keep the known-good compiled branch for recovery until the game-only tests and remaining engineering work pass.
