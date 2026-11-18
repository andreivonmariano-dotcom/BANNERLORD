# Calradia Unified v0.23 | Crash recovery, native family checks and guided creator

**Bannerlord target:** v1.5.7.125802.
**Status:** COMPILED_UNTESTED. [Successful Windows CI #44310173930](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44310173930), source SHA `225baa3b26d5331911ac9537026d9822efa674e7`, 591 gameplay policy checks, 40 simulated campaigns, zero compiler warnings/errors.
**Compiled test artifact:** [CalradiaUnified-v0.23.0-compiled-untested](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44310173930/artifacts/14430555683).
**Compiled DLL SHA-256:** `fad4d45b39f07cf1d17982948594908cd87bf84f4f46ebb980499858b7c1ca60`, 199168 bytes.

## Why v0.22 crashed

The user reported a crash on reaching the native **culture** screen. Their one provided event was:
`2026-11-18T12:48:09.9147721Z sandbox_appearance_engine_probe face_stage=True|culture_stage=True|narrative_stage=True|review_stage=True|native_face_view_loaded=False|stage_changes=none`.

This event is **not an exception log**, and `native_face_view_loaded=False` is not proof that native FaceGen is missing. Our old check ran too early.

Source review found the v0.22 experimental class `CUFaceGeneratorStageView` was marked with `[CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))]`. Bannerlord's `CharacterCreationScreen.CollectStagesFromAssembly` scans all attributed stage classes and can override the native stage view regardless of the in-game toggle. That means setting `NativeStageEnabled=false` cannot reliably disable the registration. This is a **credible root-cause hypothesis**, not a confirmed exception stack.

**v0.23 safety fix:** Delete the duplicate stage class and custom header prefab, remove extra view-assembly references, always force the legacy `NativeStageEnabled` profile flag off on Apply, and stop querying whether the native view assembly is loaded during stage content initialization. In v0.23 the original Bannerlord culture/appearance/background stage views remain the only registered providers from this module. Native FaceGen and its live 3D character remain available via the unmodified game sequence. This edition does **not** implement a unified live 3D creator.

## Family system changes

- The spouse gender preference now defaults to `Auto`. It resolves to the opposite gender of the player hero **after native face/sex selection**. Explicit Female or Male values remain available.
- Requested spouse or children activate `RealFamilyEnabled` when the player finishes the pregame editor.
- At new-game character-creation completion phase 9, after native Advanced Start phase 8, `SandboxFamilyCreator` selects culture- and gender-compatible character templates.
- The mod calls `Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage` before native `MarriageAction.Apply`. This detects models that reject a marriage without throwing.
- It verifies both spouse references, spouse clan, children's mother/father references, parental children lists, and child clan, and records `cu_family_verification result=PASS/FAIL` and detailed `cu_family_creation_status`.
- Family creation can still partially fail and leave a new hero created but not married if the game rejects the pair after hero creation. There is **no guaranteed transactional rollback**. On-screen failure and diagnostics identify the problem. Never claim family is fully verified until real new-game and reload evidence exists.
- Initial siblings remain unsupported in the new-campaign workflow without actual registered native parents. Their advanced fields should remain zero.

## Guided creator, six sections

The new `CUSandboxCreator.xml` uses Bannerlord's existing `Frame9` and `ButtonBrush`, with larger fields, separated navigation/content/help regions, active tabs and a full-width bottom action bar. It is still a fixed 1280x790 prototype that needs live screenshot review.

The six stages are:
1. **Starting Life:** role, plus required kingdom only for politically affiliated roles.
2. **Appearance:** culture preference; genuine face/sex controls remain on native character creation after the CU form.
3. **Family:** begin married, then show spouse name/age/gender and child count, plus child names/ages only if requested. Auto spouse gender avoids an unsafe default.
4. **Resources:** gold, influence and starting troops. Detailed skills and exact equipment are hidden until Advanced is explicitly selected.
5. **Kingdom & Fief:** Native/Castle/Town/Specific fief mode, exact ID only for Specific, transfer consent only for non-Native, and native world scenarios. Unavailable options remain hidden unless relevant.
6. **Review:** visible role, kingdom, spouse request, child count, starting fief and scenario. Finish by `Begin your life`.

The **Show advanced settings** button is an opt-in. The Back and Continue buttons progress through visible fields and then the next or previous page, rather than cyclically trapping the player inside long forms. An `@FieldHelp` block tells the player what a selection actually changes. Unimplemented fields are marked accordingly. This is an improved structural layout, not a claim that the game visually matches Bannerlord quality.

## Exact runtime test order

**Never test this on an important existing save.** Install the v0.23 artifact in the user's `D:\Steam\steamapps\common\Mount & Blade II Bannerlord` with the versioned install script and verify the DLL SHA above.

**Gate C1: Culture-screen crash recovery.**
1. Enable only Native, SandBoxCore, Sandbox and CU. Choose a new disposable Sandbox → CU Start Life = Wanderer; Family = unmarried; Fief = Native; leave advanced settings off.
2. Finish Review. Verify native culture appears without a crash. Continue through appearance, narrative/background and enter the campaign map. In logs, `sandbox_appearance_engine_probe` should report `cu_stage_registration=disabled`.
3. If the game still crashes, do not try family or fiefs yet. Export `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv`, `exceptions.csv` if present, and Bannerlord's own crash report/exception trace. The early stage event is insufficient to diagnose a native process crash.

**Gate C2: Spouse creation.**
1. Make another disposable new Sandbox. Choose Lord, kingdom Vlandia, Family → Begin married = Yes, spouse gender = Auto, spouse age = 30, child count = 0. Leave Fief = Native and resources untouched.
2. Finish native character creation. Open Clan and Encyclopedia. Verify actual reciprocal spouse reference, clan membership, and marriage status. Check `cu_family_verification` and `cu_family_creation_status` for an explicit success outcome.
3. Save, fully exit Bannerlord, reload. Verify the spouse persists and no new hero appears a second time. A log `PASS` alone is not save/reload proof.

**Gate C3: One child.**
1. Only after C2 passes: new disposable Lord/Vlandia + same spouse setup, child count = 1, a valid name and age (for example age 6, assuming both parents are at least 22). No fief transfer.
2. Verify the child is a real Hero in Clan/Encyclopedia with both parent references. Save/reload, confirm no duplicate and no broken family tree.

**Gate C4: Starting fief.**
Test the explicit same-kingdom Castle/Town request and save/reload only after C1-C3 pass. It can take a holding from a native allied clan; authorization is a political effect. Refer to `docs/V022_FACE_FIEF_RUNTIME_ACCEPTANCE.md`.

**Gate D: UI review.** Submit a screenshot at the actual screen resolution and a record of whether every Back, Continue, Show Advanced, Edit Text, and Apply action worked without overlapping or invisible click areas.

## Diagnostics to inspect
`sandbox_appearance_engine_probe`, `sandbox_direct_game_start_requested`, `sandbox_origin_creation_complete`, `cu_role_affiliation_status`, `cu_family_spouse_created`, `cu_family_child_created`, `cu_family_partial_result`, `cu_family_creation_status`, `cu_family_verification`, `sandbox_start_contract`, and exception contexts.

**Current acceptance:** Gate B source/package passed. Gates C and D have not been observed. The complete independent 3D CU interface remains planned under [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44). The v0.22 crash is tracked in [issue #48](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/48).
