# v0.37: Full-screen native-inspired Calradia Unified Sandbox UI

**Target runtime:** Bannerlord PC v1.5.7.125802. **Baseline:** user-reported working experimental v0.36. **Scope:** visual and layout-only refresh. The current two-step creator and engine state are unchanged.

## User decision

Replace the old floating gold-framed `1280×700` custom creation dialog with a **full-screen Bannerlord-style screen**. Keep the five CU pages and all of their functional fields, role/kingdom/children/fiefs/world validations and native start effects.

Native visual conventions applied:
- A dark charcoal/slate full-display background, rather than a semitransparent veil behind a bordered modal.
- Full-width upper and lower UI bands, with subtle brass/gold rules.
- Three interior regions: numbered chapter navigation, contextual settings, and house/holding summary.
- The original engine `ButtonBrush` for clickable buttons, and `BlankWhiteSquare_9` for native-supported UI planes, without external texture dependencies.
- Consistent brass accents, readable cream labels and blue-grey inactive explanatory text.
- A single persistent command bar with Return to menu, Previous, Continue, or truthful NEXT: NATIVE CREATOR.

The content stays centered within a **1280×700 maximum control area**. That fits 1366×768. On larger displays, the screen-sized backdrop and bands continue to the display edges. The control area is intentionally width-limited for reading and is not a floating framed modal. This is a full-screen visual shell with a centered readable content region, **not** a dynamically reflowing ultrawide UI.

The **CU native culture chooser** uses the same full-display visual system. Its original `CharacterCreationCultureStageVM` and click/commit/back callbacks are unchanged. The other Bannerlord-native stages already use the native game's own UI; this patch does not replace their input controllers or scene.

## Files

- `Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml`: remove the framed dialog backing. Use full-screen slate canvas and screen-wide header/footer. Retheme interior navigation/field/chronicle panels, gold rules, text and footer.
- `Modules/CalradiaUnified/GUI/Prefabs/CUNativeCultureChooser.xml`: match native-inspired full-screen bars and color palette, retain native game data bindings.
- `scripts/test_fullscreen_native_style.py`: assert stretch-to-screen backdrops and screen-wide bands in both prefabs, five exact tabs, all primary controls, native culture delegates, role/ASO handoff and truthful native wizard boundary.
- Manifest, installer, CI workflow and runtime preflight now target v0.37.0. No new C# gameplay or stage registration.

## What this version does not change

**Do not claim a single-screen creator or live pregame 3D model.** The pregame CU pages still collect start options before `SandBoxGameManager` builds its `Campaign`. Then the game advances to its genuine culture, face, origins, banner/clan and difficulty controllers. The optional experimental CU culture panel is still controlled by `NativeStateShellEnabled` under Starting Life > More options and is off by default. The native `BodyGeneratorView` still owns the real 3D player scene and appearance slider input.

True one-window creation requires the Advanced Start plan to be committed before campaign construction and a proven native-stage composition scheme. The permanent right-hand 3D dock needs tested render clipping, camera framing, scene lifetime and input mapping, not a decorative frame.

## Acceptance gates

**Static/CI:** both XML files parse and have a full-screen background, header/footer bands, five exact chapter actions, unchanged field controls and final confirmation, native culture callback parity, zero unexpected gameplay code changes, and a pinned Windows DLL/ZIP build.

**In-game (pending):** inspect at 1366×768 and 1920×1080. The custom screen should be edge-to-edge without a visible bordered popup. All fields/buttons must be readable and clickable, More options must expose experimental modes, exact spouse/child/role/fief requests must save, native culture stage must work, native face controls must remain usable, and campaign role/fief/family effects must match the selected plan after save/reload. Older known working builds remain the fallback until this passes.

**Status:** a native-inspired fullscreen visual refresh, not complete unified native character creation.
