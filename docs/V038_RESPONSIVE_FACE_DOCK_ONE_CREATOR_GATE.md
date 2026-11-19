# v0.38: Responsive CU creator and reversible REAL native FaceGen right-dock experiment

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Base:** v0.37 full-screen CU styles and v0.36 user-tested gameplay. **Release state:** development, compile and playtest required. **Both the single-window creator and persistent five-chapter 3D scene remain unfinished.**

## Q1. Real responsive 1920×1080 layout

Both the pregame `CUSandboxCreator.xml` and native-state `CUNativeCultureChooser.xml` now use a **StretchToParent** central content container bounded at maximum **1820×960** with 24px side and 20px vertical margins. The former fixed 1280×700 box is gone. Five real chapter actions, exact role/fief/family fields and the native culture VM remain unchanged.

Native Gauntlet `MarginLeft`/`MarginRight` let wide middle fields and description panels grow; `HorizontalAlignment=Right` keeps the House/Holdings summary and final action at the right; `VerticalAlignment=Bottom` keeps return/continue buttons near the screen foot. Full-screen dark slate and native gold header/footer stay visible at every resolution.

**Geometry:** at 1366×768, up to ~1318×728 usable content after margins. At 1920×1080, the content caps near 1820×960, leaving 50px side gutters. Native-style cards and text widths expand with the screen. The game must still prove the controls are readable, clickable and nonoverlapping at both sizes, including custom UI scale.

## Q2. Original live 3D FaceGen relocated RIGHT, stock sliders LEFT

The exact game's public `TaleWorlds.Engine.View` exposes `SetScale(Vec2)` and `SetOffset(Vec2)`. Native `MBInitialScreenBase` itself uses those calls to size and position rendered video. The v1.5.7 generated `FaceGen` UI contains a **694px right-side control panel** and **83px extension toolbar**. The new `CUNativeFaceDockTrial` attempts a conservative in-place rearrangement **only during the genuine native `CharacterCreationFaceGeneratorStage`**:

1. Identify the *current engine-owned* `CharacterCreationFaceGeneratorView._faceGeneratorView` by reference, then its own `BodyGeneratorView.SceneLayer` and `GauntletLayer`. Require both layers mounted on the active `CharacterCreationScreen`, and find the unique 694px/1080px panel and 83px extension toolbar inside the original `FaceGen` movie.
2. Save native panel positions. Move those same native Gauntlet controls left. Call `SceneLayer.SceneView.SetScale(new Vec2(1 - 694 / screenWidth, 1))` and `SetOffset(new Vec2(694 / screenWidth, 0))` on the **existing** renderer. This makes a candidate true right-side rendered scene, not a separate static image.
3. Preserve original `BodyGenerator`, player body properties, `FaceGenVM`, `FaceGenHistory`, slider callbacks and scene camera. Never create a second game stage or cloned character.
4. Press **F9** during the face editor to restore **both** native Gauntlet control positions and the original full-screen scene scale/offset. Press F9 again to retry. Cleanup restores first if the same native layers are still alive. Native FaceGen retains Back/Next and finalization.
5. If reflection, native control identities, screen resolution or render calls fail, do not attach the candidate dock. Revert to the original native layout and log an explicit fallback.

**Activation:** under **Starting Life → More options**, set both **Experimental: CU culture chooser and real 3D face audit** and **Experimental: move real 3D face to RIGHT (F9 restores)** to Yes. Set extra 3D Workshop and Review to No. The new FaceDockTrial flag defaults OFF for each fresh Sandbox. It is independent of the earlier separate workshop, but requires the native-state shell. Try in a disposable new campaign, and validate first in the face editor.

**Important limitations:** v0.38 has an engine-supported rendering transform, but **no actual in-game evidence yet** that mouse hit regions and camera FOV line up with the new viewport. The native SceneLayer still declares a full-screen hit test, though the game's normal Gauntlet widgets handle native input priority. Do not call the dock production-ready until mouse rotation/zoom, slider access, model proportions, F9 restore, Back/Next, save/reload and both target resolutions pass. The 3D scene still exists only during the native face stage. Leaving that stage destroys its original BodyGeneratorView. A persistent player across *all five chapters* remains a separate native scene lifetime task.

## Q3. One seamless CU creator, not a fake shortcut

The user wants **Sandbox → one CU creator → Begin Campaign → map**, with real King/Lord and ASO effects, family, face, origins, banner/clan and difficulty in one continuous visual system. The current game engine requires `AdvancedStartOptions` to be committed into `new SandBoxGameManager(() => new Campaign(...gameOptions))` *before* native `CharacterCreationState` exists. The current CU five-page pregame screen records those values, then native stages still run. v0.38 keeps the native culture panel and five-chapter rail but **does not hide or skip** native origins, banner, clan naming and difficulty.

To complete one visual wizard safely, future native compositor work must preserve a visually continuous shell through the pre-campaign ASO bootstrap and subsequent native stage ownership, replace each native view's visible controls by calling its own real VM callbacks, and keep a player 3D scene alive across native stage transitions. Do not call one-screen integration complete by drawing a fullscreen Gauntlet backdrop or removing native stages without running their effects.

## Automated and manual acceptance

- `scripts/test_responsive_native_face_trial.py`: native XML stretch/anchoring, default-off trial flag, mutually required shell, original FaceGen scene/move/restore API and preserved "NEXT: NATIVE CREATOR" boundary.
- Executable `Tests/PolicySmoke` tests the 694px dock ratios for 1366 and 1920, exact default-off profile flag and strict save/reload.
- CI: native v1.5.7.125802 API check, full C# compilation, all prior culture/face/role/family/fief tests, manifest CRC, hash-pinned installer rollback tests.
- **Actual game:** start fresh Sandbox with both trials Yes. Enter culture, then face editor. Verify native sliders moved left and the actual 3D model to the right; rotate/zoom/edit; press F9 and verify the stock layout returns; press F9 again; continue through origins/banner/difficulty; verify real King/Lord/family/fief, save/quit/reload. Repeat at 1366×768 and 1920×1080. Check diagnostics for `cu_real_face_dock_trial_applied`, `cu_real_face_dock_toggle` and absence of `cu_real_face_dock_failed`.

**Failure rule:** keep v0.37/v0.36 installers as known-good rollback; no silent automatic activation on an existing profile or new campaign. `HasVerifiedRectangularSceneDock` remains **false** for the final persistent creator, even if the native face-stage trial renders successfully.
