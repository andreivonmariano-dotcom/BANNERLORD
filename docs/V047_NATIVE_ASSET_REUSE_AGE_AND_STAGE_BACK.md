# v0.47 | Native-first creator art, real age-slider input, and Banner Back return

Target: Bannerlord PC **v1.5.7.125802**, `net472`.

Status: **compiled and source-tested; no new in-game proof yet**. This branch is based on v0.46. The module manifest retains `v0.38.0` during development.

## What the player asked for

- Use Bannerlord's own artwork and interaction patterns, rather than enlarging or recoloring opaque rectangular CU widgets.
- Remove duplicate native-character-creation overlays, keeping original cultures, 3D FaceGen, life story, banner, clan, review and difficulty.
- Make the one-year age slider respond to actual mouse dragging.
- Return to CU Traits after clicking **Back** on the *original native* banner editor.
- Skip the opening movie on **accepted new CU Sandbox starts**.

## Native-first drawing contract

The accepted-Sandbox branch in `CUNativeStateShell.Tick` no longer attaches its trial CU chapter ribbon, native culture chooser, FaceGen dock offset, control-skin overlays or layout adjustment. The original native culture, FaceGen, narrative, banner, clan, review and difficulty movies retain the engine's controllers and game-owned input.

The custom World/House and Stats prefab files now **reference** Bannerlord's existing sprite IDs:

- `General\CharacterCreation\character_creation_background_gradient`
- `SPGeneral\EscapeMenu\escape_panel`

These IDs appear in public native GUI extension examples. Their availability and final rendering in the *player's exact installed build* have **not** been inspected. No copyrighted game textures are shipped in Calradia Unified. The previous standalone prefabs `CUGuidedCreatorPlainFallback.xml` and `CUNativeStatsEditorPlainFallback.xml` are preserved as fallback loads if a native resource fails. The stats editor retains `CUNativeStatsEditorSafe.xml` as the last-resort per-year button-only editor.

This is reuse of actual game artwork, not a guarantee of identical native layout. The World/House and Stats screens still have their own layout. Both their UI spacing and runtime sprite availability require new screenshots.

## Age: input, original narrative bonuses, and the unresolved native age menu

The CU Age page spans 18–80 with integer steps. Its `SliderWidget` has a `ButtonWidget` handle. The layer samples its actual `ValueInt` and writes it to the same validated `PlayerAge` ViewModel property as the 1-year buttons. The selected birthday is applied to the real `Hero.MainHero` only on the final Traits confirmation. Reverting with Back retains draft data. The original Hero birthday is restored if saving the profile fails.

**The original native age menu is still shown.** It contains the game's 20/30/40/50 narrative options, whose effects differ from a plain Hero birthday change. Auto-selecting a guessed option would silently change the game's original background and skill bonuses, so v0.47 does not yet remove those choices.

New `CUNativeAgeMenuProbe` checks the game-owned final narrative menu without changing it. It records its actual option IDs and labels in the player's `DiagnosticJournal` event `cu_native_final_narrative_menu`. The future native-menu replacement needs those live IDs and confirmed engine callbacks, then a slider selection can map to game-authored rewards without blindly skipping content. This read-only probe has **no player-visible overlay**.

## Banner Back and fixed chapter navigation

The previous CU Stats panel lived *over* the native Banner stage. After confirming Traits, clicking Banner's real Back went to the native narrative instead of CU Traits. The layer now detects the Banner -> Narrative stage transition **after completion**, reopens the draft at Traits, and lets the native stage's own Back remain the owner of the transition. Confirming returned Traits calls `CharacterCreationManager.NextStage()`. Going Back from the reopened age page only reveals the already-active narrative, without calling `PreviousStage()` twice.

The internal four-step chain remains Age -> Attributes -> Skills -> Traits, regardless of Native, Balanced or Unrestricted budgets. Every page has an actual Back/Continue path and the original narrative bonuses are not invented or replaced.

This source-level path has been compiled and statically tested but must be checked in-game at Banner -> Back -> Traits -> Skills -> Attributes -> Age -> Banner. In particular, inspect whether Bannerlord restores narrative menu selection when it reenters the original Narrative stage.

## Opening cinematic

`CUSandboxNoIntroGameManager` invokes the same native `CharacterCreationState` for accepted **new** CU Sandbox campaigns instead of showing the initial new-campaign intro movie. Saved games and non-CU game paths defer to the normal `SandBoxGameManager` implementation. Later cinematics remain game-owned. The original state initialization and the fallback path still need a live game test.

## Runtime acceptance checklist

1. Start a new game using CU and confirm the initial intro movie is skipped, with the real native culture and FaceGen screens usable.
2. Verify native menu art is visible. Confirm the CU World/House and Stats pages load correctly, without flat fallback or broken textures. Check screen size 1366×768 and 1650×927.
3. During native narrative, record `cu_native_final_narrative_menu` from the diagnostics export. The original age options should remain available in this build.
4. On the custom Age page, **drag the age slider** to 18, 27, 51 and 80. Observe the displayed exact age and log `cu_native_age_slider_drag`. Check one-year +/- and Keep native age.
5. Navigate Age -> Attributes -> Skills -> Traits, switch modes and go Back to each preceding custom page. Confirm once, enter original Banner, press the **original Banner Back**, verify CU Traits reappears, and return to Banner again.
6. Finish the native character creator. Check the actual Hero age, stats, background effects and campaign start, then save/load. Confirm that the two-step native-age story reward remains the one you chose.
7. For the separate child residence report, open the existing CU household portrait gallery. A minor's recorded castle home does not imply an adult-style spawned hero or party recall.

## Verification boundaries

Windows CI compiles the C# module with pinned reference assemblies, runs pure policy tests, static XML/VM checks, native API metadata inspections and installer simulation. It **does not launch Bannerlord**, load the installed 3D scenes or test mouse dragging. The installed game's sprite registries and the user's past run logs are not present in CI. Do not promote this branch from development without a full playthrough.
