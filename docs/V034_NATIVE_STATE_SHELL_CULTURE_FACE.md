# v0.34 First native-state CU compositor: culture + real 3D face

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Baseline:** v0.33 locked five-page CU flow. **Scope:** opt-in native-state read-only CU ribbon across real native character stages. **Not a completed one-screen creator.**

## Q1: What was built in this slice

`CUNativeStateShell` observes the engine-owned `CharacterCreationState` and its real `SandBox.View.CharacterCreation.CharacterCreationScreen` from `SubModule.OnApplicationTick`. It adds an independent, nonfocus `GauntletLayer` to **the existing** `ScreenBase.AddLayer` surface. The native screen's normal layer refreshes remove only its managed native stage layers, leaving our persistent ribbon attached as native culture, face, origins, banner, naming, review and difficulty advance. On state exit, it removes and releases the ribbon when the host is still live, or lets `ScreenBase.HandleFinalize` own its layers if the engine already destroyed the screen.

The new `NativeStateShellPolicy` maps real stage class names to the frozen five CU chapters. It reports a **native stage label and real selected culture** directly from `CharacterCreationManager.CurrentStage` and `CharacterCreationContent.SelectedCulture`. It never changes stage indices, calls `NextStage` automatically, creates fake Heroes, replaces `GameStateScreen` or binds a duplicate native FaceGen view attribute.

To trial, under the pregame CU **Starting Life → More options**, set **Experimental: show CU ribbon throughout native creator** to Yes. Both extra native 3D stages must remain No. The new setting `NativeStateShellEnabled` resets to **false** for every new Sandbox. This trial only runs for an explicitly accepted active CU profile and only in a live native `CharacterCreationState`. If the ribbon fails, the native stage and its inputs remain available and the failure is logged.

**Current route:** `Sandbox → CU five-page pregame wizard → native stages (with CU shell ribbon when opted in) → map`. We do not suppress native pages yet. A persistent *branding ribbon* is the first working lifecycle proof, not a completed compositor replacing culture/face/origin controls.

## Q2: Target 3D screen layout

The final unified shell should reserve three adaptable zones:

- **Left navigation:** approximately 240px at 1920×1080, with five frozen chapters: Starting Life, Bloodline, Fortune, Land & Home, Review Your House.
- **Middle contextual controls:** approximately 520px for the active chapter's current native-bound fields, validation, native experience choices and actual clan/fief information.
- **Right live 3D player viewport:** all remaining width, at least about 40% of the screen at 1920×1080. Keep a real native camera orbit/zoom target visible on all five chapters. The model uses the real `CharacterObject.PlayerCharacter` body properties, `FaceGenHistory`, native equipment and engine scene. Do not manufacture a static portrait, parallel BodyGenerator or pre-creation spouse/child Hero.
- **Footer:** Back, progress, Continue, and exactly one final Begin Campaign after native finalization works.

At 1366×768, narrow the navigation to an icon/short-label rail and trim summaries before sacrificing camera controls or minimum readable field widths. At ultrawide, limit text column width and grant the extra space to the native 3D player. Only the actual appearance subpanel should consume mouse dragging for facial editing. Keyboard focus must stay on the active native controller.

**Current v0.34 only paints a small read-only upper-right ribbon inside the native screen.** It does not relocate the engine's actual native face panel, camera or culture layer into the final three-zone layout.

## Q3: Native integration order and why

1. **Culture + face ownership first:** confirm that `CUNativeStateShell` enters the same real `CharacterCreationState`, tracks the culture stage, updates `SelectedCulture`, advances to `CharacterCreationFaceGeneratorStage`, and retains the native live 3D editor. This is the new opt-in slice. Preserve `CharacterCreationCultureStageView.NextStage` name generation and `BodyGeneratorView` ownership, camera, rotation and face properties.
2. **Actual page compositor:** the next implementation must instantiate the CU shell inside the native character creation state with an editable native culture control and real face/body editor exposed in the Starting Life section, not merely summarized. It must keep native stage effects and their callbacks and present only one visible CU UI through a single campaign start.
3. **Origins → Fortune:** connect the real `NarrativeMenu`, `SelectedOptions`, `OnSelect`, `OnConsequence` and final bonuses. Do not re-create skill/attribute effects using guessed values.
4. **Banner and clan → Bloodline:** connect native `CharacterCreationBannerEditorView`, `CharacterCreationClanNamingStageView` and actual banner, colors, names and party/clan data. The postcreation spouse and child systems remain the existing CU implementation.
5. **Difficulty and review:** connect native Options/Review VMs and true settings to Review Your House. Only then switch the sole final button to **Begin Campaign**, execute the native finalizers once, and enter the map with no second native wizard.

Do not remove any stock native stage or overwrite `[GameStateScreen(typeof(CharacterCreationState))]` just to make the original UI disappear. The earlier v0.22 duplicate FaceGen registration crashed. Custom stage maps and a nonfocus layer are safer first proof surfaces but cannot produce the final desired UI alone.

## Tests and release gates

- **Static:** `scripts/test_cu_native_state_shell.py` checks stage/state identity, read-only XML binding, native lifecycle, explicit opt-in, no native view override. Existing flow/family tests run unchanged.
- **Executable policy:** `Tests/PolicySmoke` checks exact stage-to-chapter mapping, non-face culture, explicit state/screen/opt-in and conflict refusals.
- **Compile and package:** pinned Windows v1.5.7.125802 `Bannerlord.ReferenceAssemblies`, one mod DLL, one module ZIP, hash-pinned installer, default-off fallback.
- **In-game (NOT provided by CI):** v0.34 must be tested on a **disposable** new Sandbox with the shell flag On and both extra stages Off. Confirm native culture screen remains fully clickable, selected culture appears correctly in the CU ribbon, 3D face opens and rotates and edits, ribbon persists through background/banner/options, no duplicate layers after Back/Next, Escape still works, map role/fief/spouse/child state correct, then save/quit/reload. Repeat with all experimental flags Off to confirm exact v0.33 behavior.
- **True one-screen acceptance:** only claim completion after **one CU visual shell** from Sandbox click to map, real 3D character maintained through all chapters, real native culture/face/origins/banner/clan/difficulty values, and *no visible native wizard after the final button*. This gate is **open**.

### User-side Windows proof after a disposable game session

Use the standard read-only playthrough checker from this branch after completing a new Sandbox with **Experimental: show CU ribbon throughout native creator = Yes** and both optional extra 3D stages = No. The script does not change your save.

```powershell
$game = "D:\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord"
& .\\scripts\\Test-CU-Playthrough.ps1 `
    -GameRoot $game `
    -ExpectedVersion "v0.34.0" `
    -RequireNativeShell `
    -ExpectedChildren 1 `
    -Assert
```

Run from a checkout of the v0.34 branch or invoke the script's absolute downloaded location. Add `-ExpectedDllSha256` with the exact value from the matching successful CI run. The native shell must log `cu_native_state_shell_attached` and no `cu_native_state_shell_failed`. **The check cannot establish that the ribbon looked right or that face editing worked.** Verify those visually and retain the game-side logs/screenshots.

### Expected native diagnostics

`cu_native_state_shell_attached` means the layer was mounted to the actual `CharacterCreationScreen`, not that its rendering and mouse behavior were game-tested. `cu_native_state_shell_failed` and `cu_native_shell_detach` point to lifecycle issues. The module still collects `cu_native_sandbox_stage_completed`, `sandbox_start_contract`, `cu_family_verification` and family-home evidence separately.
