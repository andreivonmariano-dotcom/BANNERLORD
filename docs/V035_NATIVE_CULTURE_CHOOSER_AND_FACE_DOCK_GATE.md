# v0.35 CU native culture chooser and real FaceGen retention gate

**Game:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Branch:** `feature/v035-native-culture-chooser-face-dock`, stacked on v0.34.
**Status:** optional experimental bridge. Native creation stages remain present. True single-screen CU creator is **not** implemented by this release.

## Q2: Replace native culture controls with a CU-owned panel without changing native effects

When the player chooses **Sandbox**, accepts the existing CU five-page Starting Life/Bloodline/Fortune/Land & Home/Review sequence, and opts in to `NativeStateShellEnabled`, the same true `CharacterCreationState` and `CharacterCreationScreen` are launched as before. During `CharacterCreationCultureStage`, `CUNativeCultureLayer` places one focused CU culture panel over the existing native culture view. It presents the **native runtime** list, names and text descriptions directly from `CharacterCreationCultureStageVM.Cultures`.

The bridge binds through pinned-version reflection to **the existing** `CharacterCreationCultureStageView._dataSource`, never constructs a second `CharacterCreationCultureStageVM` and never fills a guessed culture list. Each CU culture click invokes its actual `CharacterCreationCultureVM.ExecuteSelectCulture()`. This calls the native `CharacterCreationCultureStageVM.OnCultureSelection`, updating `CharacterObject.PlayerCharacter` body properties and `Hero.MainHero.Culture` and calling the native sound-parameter handler.

The CU Continue button invokes the original `CharacterCreationCultureStageVM.OnNextStage()`. The native VM then calls `CharacterCreationContent.SetSelectedCulture(culture, manager)`, resetting dependent native narrative options and generating the initial clan name, before passing through the **original** `CharacterCreationCultureStageView.NextStage()` for the native first-name generator and `CharacterCreationManager.NextStage()`. It proceeds to the normal face-editing stage. Back calls the native VM's `OnPreviousStage()`.

The original native culture view and its controller remain below the CU overlay. A visible **Use native culture UI** button removes the CU layer at the following application tick, restoring focus to `CharacterCreationCulture`. If the game's pinned private fields, VM signatures or culture list do not match, the CU panel **refuses to attach** and native culture remains usable. The script logs `cu_native_culture_fallback`, not a false success. The bridge never registers a competing `[CharacterCreationStageView]` handler or a second `[GameStateScreen]`.

**Status:** this is a genuine native-bound culture UI at the culture stage, but it still runs after the five-page CU pregame setup. It does not yet bring culture selection into the pregame Starting Life page. The next compositor must move the CU shell into the live native state before all five selections.

## Q3: The single real 3D face editor, with no duplicate renderer

After native culture confirmation, `CUNativeFaceInspector` inspects the engine's *existing* `CharacterCreationFaceGeneratorView._faceGeneratorView`. It requires the actual `BodyGeneratorView.SceneLayer`, `BodyGeneratorView.GauntletLayer`, and `BodyGen` to exist, then confirms that **those same layer objects** are mounted in the live native screen. This is proof of runtime object ownership, **not** a visual or input test.

During the face stage, the CU ribbon switches from its upper-right position to a small top-left status panel, leaving the genuine native right-side appearance sliders and 3D scene unobscured. The native `BodyGeneratorView` owns the `FaceGenVM`, zoom/rotate input, `FaceGenHistory`, body properties, camera and scene, and native finalization. CU does **not** construct another 3D face editor, clone player appearance, move the camera, or create another stage. A real face editing session should operate as the stock game does.

**Outstanding right-hand viewport limitation:** the pinned public `SceneLayer`/`SceneView` API exposes full-screen hit-testing and camera/scene ownership but no proven, safe rectangular 3D viewport crop or Gauntlet embedding primitive. `BodyGeneratorView` positions the native Gauntlet controls near the right edge. The v0.35 prototype does **not** claim that the live 3D scene has been moved into a persistent CU viewport across all five chapters. Do not infer that a small CU overlay makes this feature complete.

### Locked future 3D layout

On the final *live-state* CU screen, reserve a five-chapter left navigation rail, center native-bound contextual controls, and a large right-side player viewport with real camera orbit and zoom. Keep **one** player Agent and native FaceGen history. Use a native scene projection or original native render target supported by the exact v1.5.7 runtime; test crop, focus, controls, model framing, stage transitions and save/quit/reload before hiding stock UI. The current game-native stages cannot all render a live player viewport through a pregame-only screen.

## Runtime behavior and escape routes

- Both features require the existing explicit **Starting Life → More options → Experimental CU native-state shell** flag. It resets OFF for every fresh Sandbox. Do not enable the separate CU 3D Review or Face Workshop at the same time.
- A failed culture reflection/member match keeps the native culture view. The **Use native culture UI** button opts out for the active game screen without losing the selected culture.
- A native face layer lookup failure leaves the stock native face editor untouched and changes the status text. Do not skip face editing or auto-advance.
- The five pregame CU pages, direct role/kingdom ASO selection, exact child count, spouse generation, real fiefs, family residence and one-shot phase-9 effects stay unchanged.
- This branch must not remove `CharacterCreationCultureStage`, `CharacterCreationFaceGeneratorStage`, `CharacterCreationNarrativeStage`, banner, clan, review or options from `CharacterCreationManager`.

## Verification

`scripts/test_native_culture_and_face_dock.py` tests source and prefab contracts for live native controller selection/confirmation/back, safe failure, scene/slider layer identities and no duplicate face view registration. `scripts/test_cu_native_state_shell.py` protects the earlier full native stage sequence. The pinned Windows CI must compile one DLL with 0 errors, pass 600+ C# policy tests, package the new `CUNativeCultureChooser.xml`, verify ZIP CRC and module path, and exercise installer rollback and SHA-256 identity. Those are **not game tests**.

To examine real runtime evidence after a disposable game session, run the branch's read-only PowerShell diagnostic with:

```powershell
$game = "D:\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord"
& .\\scripts\\Test-CU-Playthrough.ps1 `
    -GameRoot $game `
    -ExpectedVersion "v0.35.0" `
    -RequireNativeShell `
    -RequireCuCulture `
    -RequireNativeFaceLayers `
    -Assert
```

Run this from the project checkout, or use the absolute path to the current version's `Test-CU-Playthrough.ps1`. Add `-ExpectedDllSha256` with the successful v0.35 CI hash after it is known. The accepted session must log `cu_native_culture_composer_attached`, `cu_native_culture_selected`, `cu_native_culture_confirmed`, `cu_native_culture_commit_observed` and `cu_native_face_layers_verified`. The checker reports failures and missing events rather than creating synthetic pass evidence.

**Still required before promoting:** boot v1.5.7, select culture from CU, visually verify the real culture and appearance change, use CU Continue, interact with every native face slider, rotate and zoom, go Back and forward again, verify native origin bonuses and actual role/fief/family after starting, then save/quit/reload. The **second native wizard remains visible**, even with both features working. Keep the previous tested module available as rollback.
