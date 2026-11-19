# v0.35: CU controls for the real native culture stage, and genuine FaceGen ownership check

**Game target:** Mount & Blade II: Bannerlord PC **v1.5.7.125802**. **Branch:** `feature/v035-native-culture-chooser-face-dock`. **Status:** opt-in compiled/runtime-untested candidate. Builds on v0.34 native-state shell. **It does not yet remove the overall native wizard or physically relocate the 3D viewport.**

## Q2: What the new CU culture chooser actually changes

Under **CU Sandbox → Starting Life → More options**, select `Experimental: CU culture chooser and real 3D face audit`. The saved `NativeStateShellEnabled` flag resets OFF for each new Sandbox. Never enable this together with the extra CU Face Workshop or CU Native Review options.

The new `CUNativeCultureChooser.xml` is a focused, opaque CU panel **on the real native `CharacterCreationScreen` during the original `CharacterCreationCultureStage`**. It shows the exact cultures that the running native game produced in its `CharacterCreationCultureStageVM.Cultures` collection, preserving each native item name and description. It does **not** use a six-item hard-coded CU culture list. It does **not** add a second character-creation stage or register a conflicting culture-stage view.

`CUNativeCultureController` accesses the **already instantiated** native `CharacterCreationCultureStageView._dataSource` through version-specific reflection, after checking every required member. This is intentional: `CharacterCreationCultureStageVM.OnCultureSelection` updates real player face/body properties, chosen culture and the native stage selection. CU item clicks invoke the original `CharacterCreationCultureVM.ExecuteSelectCulture`. CU Continue calls the **same original** `CharacterCreationCultureStageVM.OnNextStage` that commits `SelectedCulture`, resets native narrative selections and calls native `CharacterCreationCultureStageView.NextStage` to generate the player's first name and advance. No direct `SelectedCulture` property mutation and no guessed culture effects.

The panel provides **Back to main menu**, **Use native culture UI** (instant recovery on the next tick), and **Continue to real 3D face**. Escape mirrors the native culture stage's return-to-menu action. Native CultureStageView and all of its handlers remain alive underneath the CU panel. If reflection fails, a native option is unsupported, or the CU movie fails to load, CU does not conceal the native culture page. The fallback is logged.

**Expected route:** `Sandbox → CU pregame five tabs → CU culture chooser inside the actual native creation state → native 3D face stage → native origins → native banner/clan/difficulty → map`. It is a genuine first native-bound control replacement, but **NOT** the single-window entire creator.

## Q3: Why the native 3D viewport is not yet relocated to the right

`CharacterCreationFaceGeneratorView` already owns a real `BodyGeneratorView` for `CharacterObject.PlayerCharacter`. It creates a **native `SceneLayer`** with the real player model and a **native `GauntletLayer`** with all face/body sliders. The `BodyGeneratorView` owns the face scene, cameras, appearance changes, undo/history, and saving to the actual character.

The inspected native `SceneLayer` and `SceneView` APIs expose `SetScene`, `SetCamera`, `SetRenderWithPostfx` and scene/viewport coordinate transforms, but **no safe public rectangular viewport setter**. The native generated FaceGen prefab uses right-aligned control panels, leaving the underlying model and its camera in the native arrangement. Simply putting a dark rectangular border on the right does not transfer the scene, make the model render there, or relocate the native mouse hit region. Creating a second `BodyGeneratorView` would risk two active character editors and duplicate save/finalizer behavior.

**Implemented safely in v0.35:** `CUNativeFaceInspector` inspects the **current native `CharacterCreationFaceGeneratorView._faceGeneratorView`**. It verifies that one real `BodyGeneratorView` is present, with its `BodyGen`, `SceneLayer` and `GauntletLayer` all active on the real screen. It reports a small nonfocus CU status label at the top-left during the native face stage and logs `cu_native_face_layers_verified` only after observing both real layers. It hides the previous top-right CU ribbon on this stage so the game's face controls remain unobstructed. No second scene or duplicate stage mapping is added.

**Not implemented:** an actual right-hand clipped scene, the native FaceGen controls moved into a left/middle CU pane, face scene remaining present through all five CU chapters, or removing native screens after CU Finish. Those require a deeper native rendering/UI compositor with a measured safe camera and input transformation, and must pass in-game tests before activation.

## Three-column final layout contract, unchanged

At 1920×1080: five-tab rail around **240px**, contextual native-backed controls around **520px**, genuine rotatable player model in the remaining **at least 40%** of screen, and a footer for Back/Continue/one final campaign action. On compact resolutions, collapse secondary summaries before shrinking the face-editing area. Do not add a second Gauntlet editor or fabricated character preview.

## Acceptance in the actual game, still required

1. Install **only** on a disposable new Sandbox game. Verify the pregame CU five-tab flow remains working and the experiment is OFF by default.
2. Switch **CU native culture chooser ON**, both separate CU 3D stages OFF. In the native culture stage, the **CU culture buttons** must appear over the game's normal culture view.
3. Choose Vlandia or Battania. Confirm the culture name and description change on the CU panel. Click **Continue to real 3D face**. On the next page, inspect the real player face and body. Check that the native cultural model changed and the UI remains usable.
4. Return to culture selection and try another culture, confirm native face/body seed and name updates. Test **Use native culture UI** recovery and Escape to menu separately.
5. During native 3D face, rotate and zoom, change face, hair, height and gender where native game permits. The regular native FaceGen controls must remain clickable. The CU status `REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE` should appear. This proves real layers, **not** right-hand viewport relocation.
6. Finish native origins, banner/clan and difficulty. Verify real role/kingdom/fief, spouse and exact child count once on the map, then save/exit/reload. Try the same scenario with shell flag OFF and confirm no CU native overlay.

### Read-only PowerShell checks after one accepted new campaign

Use the updated `scripts/Test-CU-Playthrough.ps1` in this branch, from its actual location on disk. Replace `1` with your **requested actual child count** and use the correct DLL hash from this branch's green Windows CI:

```powershell
$game = "D:\Steam\steamapps\common\Mount & Blade II Bannerlord"
& .\scripts\Test-CU-Playthrough.ps1 `
  -GameRoot $game `
  -ExpectedVersion "v0.35.0" `
  -ExpectedDllSha256 "<CI_DLL_SHA256>" `
  -RequireNativeShell -RequireCUCultureSelection -RequireRealFaceScene `
  -ExpectedChildren 1 -Assert
```

The check requires the native panel, actual CU selection, native confirm and an **observed** `CharacterCreationContent.SelectedCulture` on the real face stage. The committed native culture ID must match CU's selected ID. It requires a native FaceGen `SceneLayer` plus active `GauntletLayer`. This is still **not** visual proof, camera relocation, save/reload proof, or proof that the second native wizard is gone.

**Release safeguard:** keep the known working v0.34 and v0.33 dual-screen route. Do not enable the experimental shell by default until the culture panel and FaceGen audit pass in-game, and do not claim the single-screen requirement until real culture, face, origins, banner, clan and difficulty live under one persistent CU screen and one Finish enters the map.
