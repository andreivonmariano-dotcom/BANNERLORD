# v0.44 development: native control positions and spacing

**Target game:** Bannerlord PC `v1.5.7.125802`.
**Source:** `feature/v044-native-content-layout-alignment`, based on v0.43's actual native-control palette styling.
**Release classification:** Source implementation with pinned Windows CI, **not yet an observed in-game layout**. The module manifest retains `v0.38.0` during development.

## Q2: from matching colors to matching usable control positions

The Calradia Unified guided World, Starting Stats, House and Fortune screens share a 91 px top header, dark/slate/gold accents, a compact main content area, optional 226 px left chapter rail and 338 px right information panel, and Back/Continue near the bottom. v0.43 styles the **real native character-creation controls** but previously kept their game-default positions and internal spacing.

v0.44 introduces `CUNativeLayoutAlignment.cs` on the **same real `CharacterCreationScreen`**. It reads the original native stage's `GauntletMovieIdentifier.Movie.RootWidget` and **measures the real Gauntlet widget rectangles** to move only controls that safely fit the CU layout.

### Exact pinned Widget geometry

A dependency-free PE metadata audit `scripts/Inspect-CU-Widget-Layout-Metadata.ps1` verifies native `TaleWorlds.GauntletUI.BaseTypes.Widget` on game version `1.5.7.125802`. The real API includes `Size`, `GlobalPosition`, writable `PositionXOffset` / `PositionYOffset`, `ParentWidget`, and writable `MarginLeft` / `MarginRight`.

**Critical correction:** Bannerlord does **not** expose WPF-style `ActualWidth` or `ActualHeight` on this Widget. The layout code reads the pinned native `Widget.Size` and `GlobalPosition` Vector2 values. It never assumes a layout can be changed solely from a widget name.

### Responsive safe area

- The full-screen CU header is 91 px high. Native content begins at least **116 px** from the top.
- At **1366×768**, the content's side insets are **28 px**, and the maximum bottom edge is at **675 px**.
- At **1920×1080**, the content begins at **260 px**, leaving room for the 226 px chapter rail. The content ends at **1562 px** so it does not hide behind the 338 px context panel. The bottom edge is **987 px**.
- A candidate original native content panel is repositioned only if its *measured rectangle* fits. Content translations are bounded to 256 px horizontally and 112 px vertically. Ambiguous, very large, clipped or unmeasured panels remain in their original game locations.
- Native Back and Continue buttons near the screen edges can align to the CU footer's **29 px left**, **31 px right**, and **24 px bottom** gaps. Movement is limited to 64 px horizontally and 35 px vertically. The original button and its click handler move together.
- Native buttons with short enough real labels can receive **12 px symmetric text margins**, keeping the click rectangle and native handler intact. Long localized text or a native larger margin is never forcibly reduced.
- The root's measured size must match the actual screen resolution within 7.5%. Screen/UI scaling, missing geometry or unsupported resolutions skip movement rather than mixing logical and physical coordinates.

### Stage-specific rules

- **Native life story:** Reposition the measured content panel and safe footer buttons, leaving original narrative choice callbacks untouched.
- **Native clan naming:** Reposition eligible measured content panels and the real Back/Continue controls. The editable field retains its actual game-owned input handler.
- **Native review and difficulty:** Fit measured content to the CU side panels and footer. Original difficulty slider values, movement rules and finalization remain native.
- **Native banner and heraldry:** Adjust eligible *text-button labels and footer buttons only*. The scene, character, shield, color swatches, heraldry thumbnails and symbol geometry are never repositioned.
- **Native culture, FaceGen, CU stats, and guided World/House/Fortune:** These screens already have their own dedicated CU/native presentation. v0.44 does not alter the game-owned 3D scene, FaceGen sliders or their input handling.

**The wide CU chapter rail appears only after the original native content area has been measured and verified clear.** When the native tree lacks safe geometry or an exact panel match, the wide rail and context pane stay hidden. The header remains available.

### No duplicate wizard or new native stage

The adapter only changes existing widget offsets and margins. It neither registers stages nor creates a second `CharacterCreationScreen`, `BodyGeneratorView`, character ViewModel, banner, spouse or difficulty controller. There is no re-parenting, fake option choice, synthetic Back/Continue callback or new game-state transition. It shares the original stage view targeting introduced in v0.43.

The original offset and margin values are kept for restoration. **F10 restores both v0.43 control colors and v0.44 positions**, including all label margins. Back, native stage changes, native screen disposal, CU fallback and screen-resolution changes restore original geometry. **F9 remains the separate reversible FaceGen right-dock control.**

### Failure and diagnostics

A missing or scaled root, ambiguous panel, large native tree, position setter mismatch or boundary violation preserves the original native screen and records a diagnostic event. Relevant event names:

- `cu_native_layout_content_aligned`: the measured native panel moved within the CU bounds
- `cu_native_layout_content_verified`: the native panel already fit the CU bounds
- `cu_native_layout_footer_aligned`: real Back/Continue button moved
- `cu_native_layout_button_padding`: native button labels received 12 px spacing
- `cu_native_layout_native_preserved`: a panel was too large, unclear or unmeasurable
- `cu_native_layout_restored`: original offsets and margins restored
- `cu_native_layout_toggle`: F10 enabled or disabled the position changes

**An automated build cannot prove any of these runtime events actually occurred.** Stage-specific logs and screenshots are necessary to claim matching layout in an observed game session.

**Pinned Windows CI:** [run 44471172868](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44471172868), implementation/test source SHA `44d8f2eddc53bcadff0a9b98ca0d3fc389800fa5`. **730 gameplay policy checks passed**, zero C# warnings and errors. The 1.5.7 widget and geometry audits passed, with ZIP CRC and installer/hash checks. Compiled DLL SHA-256: `b41f980e899a2bc898dd2f4c05db2661b271ec5b7234293b331d50bb1ce4c44f`. Verified direct-install ZIP SHA-256: `a836d8abf4fdfcf598d7706a02b8c125d58a9f775f197b0bf4d8312cb22c32d4`. **Gate B only, compiled and not tested in-game.**

## Checks and acceptance

`Tests/PolicySmoke/Program.cs` now tests layout calculations at 1366×768 and 1920×1080, root scaling, hard movement bounds, footer position, banner/FaceGen exclusion, long translated labels and nonintrusive padding. `scripts/test_native_layout_alignment.py` verifies that alignment uses the game-owned movie root, the pinned `Size` and `GlobalPosition` API, original button callbacks, F10 rollback and safe wide-panel gating.

**Live acceptance remains open:** at 1366×768, 1920×1080 and ultrawide, check actual native life-story choices, clan-name caret/focus, heraldry preview and swatches, native review, difficulty slider drag, gamepad/controller traversal, Back/Continue input hitboxes, F10 toggling and F9 real FaceGen restoration. Test UI scaling settings, campaign finalization, family/fief phase 9, and save/reload.

This is a bounded **source-level layout adaptation** for native game-owned controls, not a claim of pixel-perfect native rearrangement or an uninterrupted final campaign launch. Do not merge into the release until the in-game layout and gameplay acceptance passes.
