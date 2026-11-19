# v0.42 development | Cohesive visual chrome for native character creation

**Target:** Bannerlord PC `v1.5.7.125802`.
**Branch:** `feature/v042-cohesive-native-creator-chrome`, based on v0.41.
**Quality gate:** Source and pinned CI only until a real Windows/Bannerlord session validates rendering, focus, controller navigation, and save/reload. The module manifest intentionally remains `v0.38.0` in this development lineage.

## Requested Q2: make the native screens look like the same CU creator

The **active** creator flow remains the v0.41 identity-first order:

1. **World, starting position:** one choice per CU guided screen, original pre-campaign ASO options committed before SandBoxGameManager.
2. **Character:** real native culture with CU culture chooser; native FaceGen with real model and sliders; native original life-story choices; CU v0.40 stats and traits.
3. **House:** native original banner and clan-name stages; CU guided spouse/children choices after both complete.
4. **Fortune:** CU guided gold, influence, troops, fiefs, and residence.
5. **Begin your legacy:** original native review/difficulty and game-owned finalization.

### What v0.42 adds

- **Native Chrome Policy:** `CUNativeVisualPolicy.cs` identifies all seven game-owned creation stages and maps them to exact CU chapter positions, screen titles, contextual guidance and neutral continuation labels.
- **One visual language:** `CUNativeStateShell.xml` is no longer the earlier small debug ribbon. It reuses the guided screens' **91 px slate top band, gold 3 px divider, parchment/gold headings, understated 2 px bottom rule, and narrow left sidebar styling**. The real native controls remain underneath; there is **no second character creation state**.
- **Responsive and non-invasive:** On screens under **1600 px wide**, native stages get only a thin CU header and bottom line. Wide screens receive a five-chapter read-only rail and 338 px contextual panel. A short native lore/step guide replaces experimental diagnostics. Nothing takes focus, and no buttons, native VM calls or interactive sliders are added to the chrome.
- **Culture:** the existing fullscreen CU culture chooser retains its real native selection/Continue/Back delegation. Its title now agrees with the revised `Your Character` chapter, with simple **Back / Continue / Restore native controls** labels. It already has a two-column dark/slate/gold layout with native choice descriptions.
- **Appearance:** original `BodyGeneratorView`, actual player, FaceGen slider and camera input remain the sole owners. No opaque native shell card is applied during FaceGen. Its minimal CU identity badge remains available. The auto FaceGen right dock stays **F9 reversible**, and no persistent player preview is falsely claimed.
- **Native origins, banner, clan, review, difficulty:** those stages now share the CU top chrome and receive accurate chapter/stage titles, relevant instructions and context panels on wide screens. No game-owned stage is skipped or its controls replaced. The original native review remains the one final review; native difficulty effects remain intact.
- **Live family summary:** the native chrome refreshes the spouse/child summary after the v0.41 guided House/Fortune VM successfully saves, rather than showing its earlier pregame placeholder forever.

## What has NOT been finished

Visual consistency is **not** the same as full pixel-for-pixel re-skinning. The native narrative options, Bannerlord banner editor, clan naming and difficulty control widgets still carry their **game-owned brushes and layout**. v0.42 adds consistent CU framing, readable navigation and context to those native views. It does not clone their private ViewModels into a new full-screen interactive implementation, modify their brushes, or change the hero model lifecycle. Doing that safely requires native control tree audits and observed in-game screen tests.

The 3D model is still game-owned and visible through the real FaceGen stage, not as a persistent model through every chapter. No claim of zero loading, cinematic or final map interruption can be made from CI.

## Acceptance criteria

1. Start Sandbox at **1366×768, 1920×1080 and ultrawide**. Confirm header/rail never covers native controls, banner symbols, face sliders, difficulty controls, or Back/Continue.
2. Observe culture, real 3D FaceGen with camera/mouse, narrative, CU stats, banner, clan, CU House/Fortune, review and difficulty in that order on an actual game session. Check revised chapter text and responsive show/hide behavior.
3. Verify the native culture buttons call original game code; after FaceGen F9 restore, camera, sliders and mouse hit zones remain correct.
4. Change a clan name and banner, choose narrative consequences, configure native difficulty and run the real ASO phase-8 then CU phase-9 effects without duplicates.
5. Validate native Back, Escape, gamepad input, focus and resolution switching. Save, exit and reload. Read diagnostics for `cu_native_state_shell_attached`, `cu_native_culture_confirmed`, `cu_native_face_layers_verified`, `cu_guided_house_fortune_committed` and `sandbox_start_contract`.

`scripts/test_unified_native_chrome.py` checks XML/source bindings, color tokens, full seven-stage policy, focus safety, 1600 px rail threshold, native callback preservation and FaceGen fallback. Independent C# policy checks cover those decisions in `Tests/PolicySmoke`. **Neither static test nor a successful compiler run proves any of the live visual acceptance criteria.**

Keep this work in a draft PR until the actual game acceptance and gameplay reports pass.
