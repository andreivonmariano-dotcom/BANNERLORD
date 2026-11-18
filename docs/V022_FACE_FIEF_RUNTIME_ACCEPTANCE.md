# v0.22 | Live native FaceGen stage and explicit starting fief prototype

**Game target:** Bannerlord PC v1.5.7.125802. **Status:** Windows reference-compiled, not tested in the running game. This is a controlled prototype, not the finished single-screen creator.

## Q1: Real 3D model during Calradia Unified appearance creation

- New `CUFaceGeneratorStageView` is a `CharacterCreationStageView` replacement for `CharacterCreationFaceGeneratorStage` that derives from the game's original `CharacterCreationFaceGeneratorView`. Its base constructor keeps the actual `BodyGeneratorView`, `FaceGenVM`, agent renderer, 3D scene, face-history, camera and native body key.
- The CU header prefab `CUFaceStageHeader.xml` is shown on a separate **non-focus** Gauntlet layer. It has no buttons. If it cannot load, the native editor still operates and `cu_face_stage_heading_failed` records the failure.
- The Appearance tab's `NativeStageEnabled` boolean is **off by default**. Set it to true to test the CU header during Bannerlord's actual FaceGen stage. The rest of the native face editor remains intact. The wrapper only affects a real campaign with `profile.Enabled=true`.
- **This does not suppress the native culture, face, narrative, clan naming or review wizard.** Those must not be removed until an integrated 3D UI has actual click, focus, save and state-finalization evidence.
- Also verify the wrapper is actually selected by Bannerlord's stage-view resolver. The custom attribute can coexist with the native view, so compilation does not establish which wins. Look for `cu_face_stage_native_wrapped`. No such event means resolver integration is not confirmed.

## Q2: Real starting fief, with explicit consent and kingdom boundaries

New `StartingFiefMode`: `Native`, `Castle`, `Town`, `Specific`. The first three options require no hardcoded settlement IDs. Castle/Town resolve a deterministic safe settlement from the actual in-game kingdom world after native Advanced Start has run. `Specific` requires a real exact settlement ID in `StartingFiefId`.

The `AllowStartingFiefTransfer` toggle must be true for any non-Native mode. Consent is explicit: granting the requested settlement can move it from an existing allied clan to the player's clan and can change political ownership. It is **not a cosmetic option or free bonus without consequences**.

Guards:
1. Only King, Lord and Knight starts are eligible. The native Advanced Start must give an actual kingdom first.
2. The candidate must be an actual castle or town in the **same kingdom** as the player's current clan after the world scenario has been applied.
3. Besieged or battle-contested holdings are rejected. Other-kingdom fiefs, villages and unknown IDs fail.
4. If the player already owns the settlement, the action is idempotent.
5. Otherwise the mod calls the native `ChangeOwnerOfSettlementAction.ApplyByGift(target, Hero.MainHero)`. It then checks `target.OwnerClan == Clan.PlayerClan` and reports success or failure.
6. The action runs only once as part of the new-game origin pipeline, after the native start at phase 8. No existing-save edit is done.

**Important limitation:** `Castle`/`Town` pick the first safe eligible settlement in a reproducible sort, preferring one already owned by the player. `Specific` is a typed exact ID, not yet a browseable in-game settlement gallery. A transfer is not automatically rolled back if a later unrelated origin effect fails. Test only in disposable new games.

## Q3: Native-style creator interface, page by page

The CU pre-game Gauntlet prefab now has **eight** navigation categories: Identity, Appearance, Household, Progression, Equipment, Resources, Fief & World, and Review. The Appearance section exposes the experimental native 3D header opt-in. The Fief & World section exposes requested fief mode, exact ID, consent and world-scenario controls. The button footer says `Begin your life`, and cancel returns to the main menu.

Changes use the available native `Frame9` and `ButtonBrush` references, with a clear header, separate navigation and form panels, a status area and action footer. This is **not yet a polished Bannerlord UI**. Font brushes, scaling, different resolutions, keyboard/controller focus and the new face-stage header need real screenshots and interaction checks. A fixed 1280×790 form is a prototype.

## Game test 1: Native face-stage wrapper

1. Install a v0.22 artifact with the exact recorded compiled DLL hash. Enable only Native, SandBoxCore, Sandbox and Calradia Unified.
2. From Sandbox, set **Appearance → Experimental CU header = true**. Select Wanderer, no family, Native fief mode, and Begin Your Life.
3. Complete native culture selection and reach the real face/body editor. Verify the model renders in 3D and can rotate, zoom, change hair/face/body. Look for a CU header that does not block any native controls.
4. Exit to the main menu and repeat once with the appearance toggle false. Confirm face editor still works with no extra CU header.
5. Check `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv`: `sandbox_appearance_engine_probe`, `cu_face_stage_native_wrapped`. Record any `cu_face_stage_heading_failed` exception.

## Game test 2: Fief ownership effect

1. With a fresh disposable Lord start, select `StartingKingdomId=vlandia`.
2. In Fief & World set `StartingFiefMode=Castle` and `AllowStartingFiefTransfer=true`. Leave StartingFiefId blank for Castle mode.
3. Finish native character creation. Open Clan → Fiefs. Inspect the actual owned settlement, its current owner and kingdom.
4. Check `sandbox_start_fief_result` and `sandbox_start_contract` in `events.csv`. Look for `granted:<id>` or `already_owned:<id>`. A result beginning with `refused:` or `failed:` is a failed test.
5. Save, fully close Bannerlord, reload. Verify the settlement remains in the clan, there is no second transfer, spouse and children are correct if later tested.
6. Repeat a new disposable start with `StartingFiefMode=Specific`, a valid exact castle or town ID from your installed game, and explicit consent. Record errors for wrong-kingdom, missing ID and siege cases.

## Release gate

A successful C# build validates source compatibility and package structure. It does **not** prove the game selected the CU stage override, accepted mouse input, granted a settlement or persisted a save. The full replacement of the native background/culture wizard remains tracked under [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44). Keep the PR draft until live evidence is supplied.
