# v0.36 Native five-chapter shell and real 3D viewport feasibility

**Target runtime:** Mount & Blade II: Bannerlord PC **1.5.7.125802**. **Base:** compiled but game-untested v0.35, with its native culture selection panel and unmodified native FaceGen editor. **Status:** opt-in shell layout/progress and viewport safety contract, not a single-screen creator.

## Q1. Move all five CU creator chapters inside real CharacterCreationState

**Desired:** Sandbox → one CU shell inside the engine-owned `CharacterCreationState` → Starting Life, Bloodline, Fortune, Land & Home, Review Your House → one Begin Campaign → map. Native culture, true player face, origin consequences, clan/banner, difficulty and campaign finalization must still run exactly once. No second visible wizard.

**Verified engine barrier:** `SandboxCreatorOverlay.Tick` builds `SandBox.AdvancedStartOptions.AdvancedStartOptions` and immediately sends `options.GetChangedOptions()` to `new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign,gameOptions))`. These King/Lord/kingdom/scenario values must be supplied **before** the subsequent live `CharacterCreationState` exists. A native-state-only CU page that changes role or fief *after* construction would be dishonest. Do not silently apply profile-only values, fake a King grant, or ignore the ASO phase-8 contract to remove a visible screen.

**What v0.36 implements:**

- A persistent read-only **five-chapter status rail** is now hosted on the **actual native CharacterCreationScreen**, not on the old pregame screen. It reports Starting Life for native Culture/FaceGen, Bloodline for Banner/Clan Naming, Fortune for native Narrative, and Review for native review/difficulty. **Land & Home reads START OPTIONS SET** since the native start options were committed before the game-state transition.
- The same UI remains mounted as the existing native stage controller advances. It does not register duplicate stages, alter the actual screen handler, or expose clickable fake tabs that would skip required native `OnConsequence`/stage finalize callbacks.
- The existing **opt-in** CU native culture chooser remains the actual functional culture-selection UI for its native stage. Existing family/fief/ASO behavior stays unchanged and **NativeStateShellEnabled remains off by default**.
- The **five entire editable pages have NOT moved**. The pregame five-page creator is still required for King/Lord, scenario and other ASO inputs, and the user still sees the native stages afterward. The native chapter rail is a safe first lifecycle/UX surface, not the completed unified screen.

**Required architecture for genuine single-screen launch:** split the current UI into a *pre-campaign start request* and a *native-state continuation* with one continuous CU visual shell. Either provide game-start options before `SandBoxGameManager` construction using a validated, visually continuous first-page bootstrap, or implement a rigorously tested engine-supported deferred-start mechanism. Do **not** claim all five pages are entirely inside the live native state without resolving this startup order. Use only one final user-facing confirmation after native options and effects. The old route must remain an opt-out until live tests show native King/fief/family parity.

## Q2. Persistent right-hand **real** 3D viewport

**Desired visual contract:** on 1920×1080, five-page navigation about 240px wide, context controls about 520px wide, and the remaining **~1132px** for the live player. On 1366×768, compact navigation 172px, controls 395px, leaving **~771px**. The model remains rotatable and zoomable across all five chapters, with face-edit controls in Starting Life. The geometry is deliberately a target, not the game renderer's actual cropped viewport.

**Pinned API examination:** Bannerlord 1.5.7's `BodyGeneratorView` owns `SceneLayer`, `GauntletLayer`, `FaceGenVM`, native camera and `FaceGenHistory`. `CharacterCreationFaceGeneratorView` creates it once for the native FaceGen stage and finalizes it when that stage exits. The publicly exposed v1.5.7 `SceneLayer.HitTest` tests the **entire screen**, and `SceneView` has camera and render-target operations but no verified, public rectangle crop + matching input-coordinate transform. Simply drawing a CU border around a scene or moving a portrait is **not** a persistent working right-hand viewport. Manually reusing a finalized FaceGen SceneLayer on later stages risks a destroyed render target and broken input.

**What v0.36 implements:** `CUNativeViewportPolicy` fixes the proposed responsive region dimensions, clearly exposes `HasVerifiedRectangularSceneDock=false`, and renders an explicit native-state face status: **NATIVE 3D SCENE: FULL SCREEN; DOCK NOT YET VERIFIED**. The original, genuinely editable FaceGen continues untouched, with its scene and controls inspected by reference identity. The native 3D scene is **not relocated or duplicated**.

**Required proof before attempting a live dock:** native or test-backed render-to-texture+Gauntlet transport, stable viewport ownership across stage transitions, accurate scene-to-Gauntlet mouse transform, full orbit/zoom and slider input, and a campaign save after face editing. A new scene may be created only if it renders the actual engine-backed player attributes and does not compete with the active FaceGen view. Never destroy the original native face view until a proven replacement preserves `FaceGenHistory` and effect finalization.

## Test gates and recovery

- `Tests/PolicySmoke` tests the five titles and chapter mappings, Land & Home start-time barrier, non-skipping statuses, two common screen-size targets and the explicit unverified dock capability.
- `scripts/test_cu_native_five_chapter_rail.py` verifies XML list binding, all five page VM titles, lack of stage-navigation buttons or a new BodyGen, and the one real native `CharacterCreationState`.
- The full existing v0.35 culture/face, family/ASO, installer, manifest and pinned 1.5.7 compiler tests still run.
- In-game checks must confirm the rail on real native stages, that native stage transitions preserve the real face/body state, the native culture chooser works and any UI panel does not obstruct rotation/zoom. Test a disposable campaign and verify all real start values after save/quit/reload. **CI compilation does not supply that evidence**.
- The build is experimental and opt-in only. Set `NativeStateShellEnabled=false` for the previous route, or reinstall the known working v0.35 ZIP. Never advertise the complete one-screen editor or permanent 3D viewport until those tests pass.

## Final acceptance checklist

**Q1 final:** choose King, kingdom, spouse with an exact child count, fief/home, native culture, actual body/age, narrative bonuses, banner/clan and difficulty within *one* CU shell. Only one confirmation enters the map. No second visible creator. Start type and phase-8/phase-9 effects verified after loading the save.

**Q2 final:** one genuine editable player Agent persists visually across all five chapter pages, right viewport remains at least 40% of 1920×1080, 1366×768 controls do not overlap, body and camera edits persist to the player, native hotkeys work, and no additional BodyGenerator or broken native FaceGen attributes exist.

**Both final acceptance gates remain open in v0.36.**
