# v0.43 development: style the actual native creator controls

**Target:** Bannerlord PC `v1.5.7.125802`.
**Branch:** `feature/v043-native-widget-reskin`, following the v0.42 unified CU chrome.
**Quality gate:** Source build and Windows CI, **not a real in-game visual acceptance**. Current development module manifest stays `v0.38.0`.

## Why this is not another decorative overlay

v0.42 matched the header, color bands, chapter context and navigation direction. Bannerlord's native narrative option buttons, banner controls, clan-name input, review labels and difficulty widgets still used their original colors. v0.43 now targets the **live, game-owned Gauntlet widget tree itself**:

- `CUNativeControlSkin.cs` reads the pinned `CharacterCreationScreen._currentStageView` and confirms the exact expected 1.5.7 stage view class.
- It obtains an actual native `GauntletMovieIdentifier.Movie.RootWidget` from the original stage view. Narrative/Review/Options use game-owned `_movie`, ClanNaming uses `_clanNamingStageMovie`, and Banner opens through `_bannerEditorView`'s owned movie. It will **not** search all layers, rebind another mod's controls or create a new character-creation state.
- It visits only the actual rooted controls. Native option buttons with visible text receive **locally cloned** native `Brush` objects with CU brass/parchment colors. TextWidgets receive private `Brush.FontColor` copies or a writable local Color. The button click delegates, native ViewModel data bindings, selection ownership, localized strings and native stage callbacks stay untouched.
- Original brush and Color objects are saved for restoration. F10 **toggles the in-place styling on/off** for visual comparison. Native stage changes, screen teardown and failure restore originals.
- The native life-story controller can replace menu option widgets within the same stage. Every 18 native UI ticks, the skin looks for **new widgets only** and colors them, avoiding repeated modifications to existing buttons.
- **Heraldry swatches and icons are protected**. An original native button must have a text-bearing child to receive the parchment/brass button tint. In-game banner color/symbol geometry and all 3D scenes are excluded.
- On **native ClanNaming**, the original text-input brush is cloned for a matching slate background and parchment foreground, without changing editing, caret, focus, allowed names or the actual clan-name callback.
- On **native Options**, difficulty slider brushes are locally cloned for brass accents, without changing drag handlers, slider range or campaign difficulty data.
- Actual **native narrative, banner, clan, review and difficulty** stages have strict allowlisting. The native CU culture chooser and the genuine native FaceGen remain separate and unchanged by the skin. FaceGen's F9 reversible scene dock is untouched.

## What does this change to the layout?

Control styling is **real, in-place widget styling**. The elements keep their original native size, placement, click target and keyboard/gamepad navigation. The CU header, stage title, wide-screen chapter rail and responsive context from v0.42 still provide the common frame. The short CU World/House/Fortune and stats screens retain their identical XML layout.

It is **not** a new clone of the entire Bannerlord native creator or a pixel-identical relocation of every button. Moving native input widgets and scenes to fixed positions without an observed screen would risk blocked inputs or missing original effects. That remains a live acceptance and potential later layout milestone.

## Safety and native ownership

1. **Original game state only:** no synthetic `CharacterCreationScreen`, no additional `CharacterCreationStageView`, no `RemoveStage`, and no direct native transition command in the skin.
2. **Private rather than global brushes:** the exact v1.5.7 pinned assembly audit confirms `Brush.Clone()`, writable `Brush.Color`, `Brush.FontColor` and `GauntletMovieIdentifier.Movie`. No shared native brush is directly edited.
3. **Fail closed:** an absent or ambiguous native movie root, unsupported view type, large widget tree or nonwritable brush leaves the original UI untouched, records `cu_native_widget_skin_unavailable` and does not interrupt character creation.
4. **Visible proof required:** positive counts in `cu_native_widget_skin_applied` for actual TextWidgets, native buttons, clan inputs and difficulty sliders. `cu_native_widget_skin_refreshed` reports newly created narrative option controls. `cu_native_widget_skin_restored` reports rollback. Automated builds can only assert the source path and policies, not game rendering.

## Pinned Windows compile and static verification

[Successful CI run 44467336043](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44467336043) at source SHA `fb9d6969bf5ef64d0d648bada0cbd426ba27adbf` ran **708 gameplay policy checks**, including the new stage-view/style safeguards. The pinned v1.5.7 widget audit succeeded (513 metadata lines), with zero C# compiler warnings or errors. GitHub's generated artifact contains an exact built DLL with SHA-256 `61fce6458651c3e1f6462cff43e9a363c5cafeed9d77684165b7eecac171624a`. Local artifact re-verification found one direct-install zip, passed both ZIP CRC checks, checked expected prefab files and the exact DLL hash. The re-packed inner direct-install ZIP SHA-256 is `f952de9670741df355c1e775a71ba1bdaa929cbbeb823810f72f1867a3406b91`.

This proves compiled and static acceptance only. Positive `cu_native_widget_skin_applied` runtime counts are required before stating that any native button is visibly recolored in a live game session.

## Live acceptance before merge

Run fresh CU Sandbox on 1366x768 and 1920x1080. Verify the original button label, native icon, dark/slate/gold tint, selection border, hover/press colors, focus and gamepad traversal. During the complete native origins narrative, inspect later menu options for consistent newly created button styles. Change banner symbols and exact banner colors to catch incorrectly tinted swatches. Enter a clan name, confirm the field's caret and focus, change a difficulty slider, and compare F10 on/off at all five native stages. FaceGen's 3D scene and F9 recovery must remain intact. Complete House/Fortune, verify the single final native review, start the campaign and reload. Capture diagnostic `applied`, `refreshed` and `restored` counters for every stage.

All original v0.41 family/fief contracts, native background bonuses, balanced/unrestricted/native stat overrides, game-owned finalization, and save/reload effects remain independent of decorative widget colors.

**This document reports implemented source-level re-skinning, not a claim that every native widget has visibly changed in an observed Bannerlord session.**
