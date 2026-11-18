# v0.31: Native 3D face workshop and full-creator parity

**Game target:** Bannerlord PC v1.5.7.125802. **Status:** opt-in engineering prototype, not a completed one-screen Sandbox creator. This document records source/assembly facts, implementation and required live tests.

## Q1: One Calradia Unified screen using the real 3D face editor

The pinned 1.5.7 reference audit (`scripts/Inspect-CU-Creator-API.ps1`) found these public engine surfaces:
- `CharacterCreationState` owns `CharacterCreationManager`.
- `CharacterCreationScreen` owns the shared native scene and creates stage views through `CharacterCreationStageViewAttribute`.
- `CharacterCreationFaceGeneratorView` is registered for native `CharacterCreationFaceGeneratorStage`.
- Native manager exposes `FaceGenHistory` to the face editor. The screen owns stage lifecycle.
- Existing v0.29 `CUNativeOriginReviewStageView` composes `CharacterCreationReviewStageView`.

The new `CUFaceWorkshopStageView` composes the **real** `CharacterCreationFaceGeneratorView` for a unique `CUFaceWorkshopStage`, and delegates scene, tick, navigation, input and finalization directly to it. A small, **nonfocus** `CUFaceWorkshop.xml` ribbon displays the selected role, native culture and planned family without creating Heroes or fake face parameters. The actual native editor keeps focus.

The workshop is **OFF by default** (`NativeFaceWorkshopEnabled=false`). It is available under **Identity → More options** in the CU pregame creator. It cannot run together with the experimental v0.29 CU 3D review. Missing native stages or absent view discovery prevent registration. A failed CU decoration movie leaves native face editing intact.

**Current limitation:** This adds an **experimental end-stage** to the existing dual-creator sequence. It demonstrates the engine face editor inside a CU-owned view, not removal of the earlier native wizard. Do not market this as the finished one-screen feature.

## Q2: Native gameplay parity requirements for a true combined editor

For a future *single* CU-owned character-creation shell, the native APIs must retain all of these functions:

1. **Culture and identity:** Use `CharacterCreationContent.SetSelectedCulture`, `ApplyCulture`, native culture bonuses, starter equipment and generated character name logic. The selected culture must affect the real `CharacterObject.PlayerCharacter` and campaign start.
2. **Editable 3D face and body:** Preserve `CharacterCreationFaceGeneratorView` / `BodyGeneratorView`, `FaceGenHistory`, engine scene and real body properties. Check gender, facial edits, appearance persistence and camera interaction.
3. **Background and age:** Preserve the actual narrative-menu choice chain, `CharacterCreationManager.SelectedOptions`, `NarrativeMenuOption.OnSelect`, `OnConsequence`, and `ApplyFinalEffects`. Age-dependent family and stat effects must not be reimplemented with guessed values.
4. **Banner and clan:** Preserve real `CharacterCreationBannerEditorView` banner colors and `CharacterCreationClanNamingStageView` native naming and character creation effects. Do not overwrite the clan logo with decorative images.
5. **Difficulty and start:** Preserve native `CharacterCreationOptionsStageView` game options. Use `SandboxNativeStartOptions.TryPrepare` for real start type, kingdom, world scenario and seed before campaign construction.
6. **Family and holdings:** Keep v0.27's immediate exact child-count commit and fresh-campaign reset, v0.28's safe spouse/minor home logic and v0.29's real Hero portrait gallery. Apply the native ASO start at phase 8, then CU family/fief effects at phase 9. Never create preview child Heroes.
7. **Finish and save:** Run the real `CharacterCreationManager.ApplyFinalEffects` and `CharacterCreationState.FinalizeCharacterCreationState`. Verify `sandbox_start_contract` and `cu_family_verification`, then save, exit and reload.

`NativeCreatorParityPolicy` now independently checks the presence of the seven required native stage types before adding the face workshop. The C# policy smoke suite tests missing-stage refusal, view-discovery refusal, conflicting opt-ins and the default disabled state. This check proves structural prerequisites only. It cannot establish a working unified UI.

## Q3: The first genuinely unified build and its acceptance protocol

The final screen must be hosted inside the engine's real `CharacterCreationState`. A main-menu-only Gauntlet overlay cannot hold a live player BodyGenerator before that state exists. A safe implementation requires a new compositor that keeps a **single persistent CU Gauntlet shell** and routes pages to the native controllers without registering duplicate native stage views.

### Runtime integration order

1. Pin the exact 1.5.7 assemblies and inspect concrete stage view constructors and lifecycle. The v0.30 reference audit identified 45 related types and all seven native view attributes.
2. Introduce the CU state compositor behind a new opt-in, separate from the workshop. Keep current `SandBoxNewGame` fallback working.
3. Host the native culture, face, narrative, clan, banner, review and options controllers in one CU navigation system. Only remove their separate visible pages when the corresponding native data and callbacks have full parity.
4. Replace the pregame-to-native handoff with one continuous CU visual shell. Retain the real Advanced Start options and phase 8/9 postcreation behavior.
5. Make **Begin Your Life** the only final action. Reject incomplete culture/background/age/banner/difficulty or invalid kingdom/fief requests.
6. Run real game testing with screenshots before enabling the compositor by default.

### v0.31 workshop test (disposable campaign, NOT one-screen acceptance)

1. Back up saves. Install only a matching successful Windows v0.31 CI artifact into a clean game profile. Record the DLL SHA-256.
2. Click **Sandbox** and choose **Lord**, **Vlandia**, generated **NobleRandom** spouse and exactly **one child**. Under **More options**, set **Experimental: edit live 3D face in CU workshop** to Yes. Leave **Preview CU choices inside Bannerlord's 3D review** at No.
3. Confirm the pregame CU plan. Work through the currently separate native culture, face, narrative, banner, naming and difficulty pages. Those pages are expected in v0.31. The experimental workshop should appear **once, at the end**, with the real editable 3D player and the nonfocus CU ribbon.
4. Change a real facial feature or hairstyle, rotate the model, test Back/Next, and finish. Confirm campaign start, actual culture, fief, Lord/vassal status, native family and one child.
5. Check `events.csv` for `cu_face_workshop_native_parity`, `cu_face_workshop_attached`, `cu_face_workshop_view_loaded`, `sandbox_start_contract result=PASS`, and `cu_family_verification result=PASS`. Inspect `exceptions.csv`. A log request without a visible scene is **not** proof of success.
6. Save, fully quit, reload. Compare face, native role, fief, spouse and child to the pre-exit values. Repeat a separate new game with both opt-in flags Off. Verify the v0.29 sequence behaves as it previously did.
7. Negative test: select both experimental modes. The CU creator must refuse to start until exactly one mode is selected.

### Unified screen acceptance (still open)

The later composite build can claim a one-screen creator only after it passes all checks in [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44): a single CU UI from Sandbox click to the map, **no additional native character pages**, live face edits and culture effects, real childhood/adulthood choices, banner and clan, native difficulty, one-shot finalization, correct Lord/King/fief/family effects, persistence, Escape recovery, 1080p/768p/ultrawide, mouse and controller input.

### Evidence levels

- **Gate A:** native API audit and static UI/registration checks.
- **Gate B:** independent policy tests, pinned module compilation and ZIP contents.
- **Gate C:** actual launch, visible controls and real player face editor.
- **Gate D/E:** complete native effects, exact family, actual role/fief, save/load, no duplicate relatives.
- **Gate F/G:** screen usability, long-session regressions and published matching tested ZIP.

A green CI run supports at most **Gate B**. The new face workshop does not yet meet the one-screen Gate C.
