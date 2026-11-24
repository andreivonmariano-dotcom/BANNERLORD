# Calradia Unified | Current development and verified build identity

**24 November 2026 — feature freeze.** The active consolidated development source is [draft PR #99](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/99), branch `integration/cu-living-society-acceptance`, based on the active v0.69e acceptance branch. Always resolve its current head SHA, rather than assuming the README or manifest version identifies the current DLL.

**Last successful build located at this audit:** [run 45355054393](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45355054393), source [1f71b00a72d71717135d3c55e4e6da50a8bfae3f](https://github.com/andreivonmariano-dotcom/BANNERLORD/commit/1f71b00a72d71717135d3c55e4e6da50a8bfae3f), [compiled-untested artifact 14901409008](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45355054393/artifacts/14901409008), DLL SHA-256 `becf8046622242743373c3b04ddea24e2ea6f3da919ca2ea01772c750925b615`. This is an earlier seven-career foundation build, NOT the consolidated cleanup. No current installed DLL or runtime acceptance established. The manifest remains v0.38.0; target Bannerlord is 1.5.7.125802.

The v0.72 and v0.73 battle-history branches are separate, diverged feature candidates, not the consolidated source. Do not overwrite either branch or merge unfinished battle features during the freeze. The current default main branch is not the advanced development baseline. See [acceptance ownership audit](docs/CU_ACCEPTANCE_FREEZE_OWNERSHIP_AUDIT.md).

**Historical README follows.** Its old download/installer references are historical, not current delivery instructions.

---

# Calradia Unified: latest compiled developer candidate (v0.69g)

**v0.69g** addresses the user's first actual gameplay failure: **Younger Brother of a Lord** had no eligible real native elder. That stopped the spouse, two children and two siblings from being created. The new House safeguard blocks an unsupported noble lineage **before native finalization**, returns to the noble-birth selector and preserves all household choices. It does not fabricate a native lord.

[Successful Windows build 44867186334](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44867186334) passed **910 C# policy checks**, 0 compilation warnings/errors and the existing simulations. [Download the exact compiled, still game-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44867186334/artifacts/14691813274). The game's historical `SubModule.xml` continues to say **v0.38.0**.

**PowerShell 5.1, show the exact v0.69g candidate and its SHA-256:**

```powershell
$Installer = Join-Path $env:TEMP 'Install-CU-From-Artifact.ps1'
Invoke-WebRequest -UseBasicParsing -Uri 'https://raw.githubusercontent.com/andreivonmariano-dotcom/BANNERLORD/5602fbd745a4c3249750dc9882d58b9be5ec4c01/scripts/Install-CU-From-Artifact.ps1' -OutFile $Installer
& $Installer -ShowLatest
```

Download that exact Actions ZIP, then install with `-LatestVerified -GameRoot "YOUR_GAME" -ArtifactZip "YOUR_DOWNLOADED_ZIP" -ReplaceExisting`. Pinned DLL SHA-256: **`d6d9ad9a1ec217d5482a46442597461944c1cf2fa52f185c87309b654dba6f10`**. The script backs up the existing module before replacement and leaves saves untouched. [Full current installer instructions](docs/POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). [Evidence-based defect report](docs/V069G_REAL_GAME_NO_NOBLE_ELDER_HOUSEHOLD_GATE.md).

**Acceptance:** This repair is compiled and simulation-tested, not yet verified in a new actual Bannerlord session. The old session confirms a gameplay failure, not a passing new-game test. The correction is on [draft PR #93](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/93), not merged as a release.

---

# Calradia Unified v0.47 development: native artwork and original creator screens

**Native-first:** Original culture, appearance/3D, life story, banner, clan, review and difficulty screens retain Bannerlord's graphics and callbacks. The extra CU chapter rail, replacement culture chooser and native 3D dock are suppressed on accepted Sandbox starts.

**Original game artwork:** Custom World/House and Stats prefabs reference Bannerlord's `General\\CharacterCreation\\character_creation_background_gradient` and `SPGeneral\\EscapeMenu\\escape_panel`, without shipping copies. Standalone prefabs remain ready if a game resource cannot load.

**Real exact-age control:** The custom 18–80 SliderWidget uses an interactive native ButtonWidget handle and reads the live slider value through the pinned game widget interface. The 1-year buttons remain. The **original native age choices are still present**. A read-only last-narrative-menu probe records genuine 20/30/40/50 option IDs for a later faithful replacement. We do not silently choose native age rewards.

**Back into Traits:** Clicking Back in the original banner editor returns through the real narrative stage, where the CU Stats panel reopens at Traits. Completing Traits returns to Banner through the native manager. The four internal steps remain Age, Attributes, Skills, Traits, independent of stat mode.

**Intro:** The intro movie is bypassed only for accepted new CU Sandbox starts. Saved games and other campaign cinematics use native behavior.

**Build acceptance:** Windows compilation, XML tests and static navigation contracts cannot prove runtime sprite availability, slider dragging or actual Banner Back. The build remains **COMPILED_UNTESTED** until an in-game playthrough. Existing internal module manifest version is still v0.38.0.

[Implementation and live verification plan](docs/V047_NATIVE_ASSET_REUSE_AGE_AND_STAGE_BACK.md)

---

# Calradia Unified v0.46 development: final page order, native family presence truth, cleaner creator UI

**Player-screenshot repairs:** World and House now have a compact gold-edged 610px choice card, shorter side panels and no `Selected: SpouseEnabled = true` internal debug string. Age is a focused separate page; stats rows are taller and more readable. Culture has a framed lore section. Native FaceGen, Narrative, Banner, Clan, Review and Difficulty keep their original active game control surfaces.

**Deterministic Back and Continue:** `Starting Age → Attributes → Skills → Traits → Confirm` regardless of Native, Balanced or Unrestricted mode. Back from Attributes returns to Age. Only the age page's Back returns to native life story. Conditional guided questions now keep their selected field after spouse, child count, role or fief changes.

**Why Alden is recorded at Caleus Castle but not recallable:** The game records a minor Hero's home without spawning that child as an adult hero or granting native party recall. This affects male and female children, irrespective of which portraits the settlement UI shows. The family home path no longer reports minor residence as failed due to absence from `HeroesWithoutParty`. **CU: View my household portraits** now gives every actual son/daughter a separate residence and eligibility status. Adult relatives retain native teleport/rehome handling.

**Pinned Windows build:** [run 44485361161](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44485361161), source SHA `decdf9b6d3e9ca5b2a7be178627ca2791256df71`. **747 C# policy checks**, zero compiler warnings/errors, pinned native API reflection, ZIP integrity, DLL identity and installer simulation passed. DLL SHA-256 `e71ce307ac40e04b084e58474e7d2392974887fea7b9680e728dc90095f77085`.

**Release gate: COMPILED_UNTESTED.** The supplied prior screenshots were of v0.45. The v0.46 layout, all Back/Next sequences, Alden's live `HeroState` and actual adult castle scene placement require a new game test. A minor cannot be forced into adult recall without violating native Bannerlord life-stage rules. Module manifest remains `v0.38.0` in development.

[Detailed v0.46 implementation](docs/V046_CREATOR_NAVIGATION_FAMILY_UI_HARDENING.md) · [Draft PR #77](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/77)

---

# Calradia Unified v0.45 development: screenshot-driven creator repair

**Player-observed defects addressed:** The v0.44 guided World/Stats/Household and Culture screenshots showed flat navy panels, crowded text, raw culture IDs colliding with culture names, a duplicate Calradia Unified header over native Family/Banner/Review/Difficulty, and a black experimental FaceGen dock. v0.45 changes active custom GUI prefabs to native-inspired dark brown, stone, brass and parchment with measured font sizes, compact stat rows, centered responsive question controls and clear button labels. It removes the overlaid CU native header, preserves the original native layouts and defaults to the original game-owned 3D FaceGen camera.

**Stats and age:** Skill level is still **0–330**, separate from **Focus** (native cap 5). A new age step after the game-owned life story offers **18–80 in one-year increments** through a native Gauntlet slider with a year-button fallback, committing the chosen birthday to the real Hero and preserving story rewards. Child-age limits are now computed from the younger actual parent minus 16 years, displayed before editing, and validated/clamped instead of failing late.

**Build evidence:** [Windows run 44477842670](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44477842670), source commit `4a28300d407309719a12274f62e52468be0c165d`. **735 gameplay policy checks**, zero warnings/errors, native widget/age-slider metadata audit, source/XML creator checks, ZIP CRC and simulated installer passed. Compiled DLL SHA-256 `ab630c5ce3d9d3881dfba804c6c05990879ac0390493330fbef68a81d2980772`. Verified direct-install ZIP SHA-256 `68d19377f31e394322aa7ce937cf2cbc58f5fd8fd37ff80cca8a343f0176025a`.

**Gate B, COMPILED_UNTESTED:** The provided screenshots refer to old v0.44 behavior. This new design and age slider have not been observed in a game session. Exact slider dragging, font clipping, FaceGen reappearance and child-age Hero persistence must be checked in a new Sandbox. The module manifest retains its prior development version number.

[Detailed v0.45 changes and live acceptance](docs/V045_NATIVE_CREATOR_UI_AND_AGE_REPAIR.md) · [Draft pull request](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/76)

---

# Calradia Unified v0.44 development: aligned native creator control positions

**Native layout alignment:** The original Bannerlord narrative, clan naming, review and difficulty controls now have a measured, bounded layout adjustment path compatible with the CU guided creator's header, 226px wide chapter rail, right context panel and footer. Native banner editor content, real 3D FaceGen and heraldry symbols remain game-owned and unshifted; original native Back/Continue footer controls receive measured alignment only where safe. Text-bearing native buttons can receive symmetric 12px interior label spacing, preserving long localized labels and click targets.

**Version-pinned API:** Native widget geometry is read from `Widget.Size` and `GlobalPosition` on Bannerlord PC `1.5.7.125802`. The new Windows PE metadata audit checks writable offsets and margins, and large CU sidebars hide unless actual native content clearance is verified. Original offsets, margins and v0.43 native colors revert with **F10**, no duplicate GameState or native callbacks.

**Pinned Windows CI:** [run 44471172868](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44471172868), implementation/test source SHA `44d8f2eddc53bcadff0a9b98ca0d3fc389800fa5`. **730 gameplay policy checks passed**, zero C# warnings and errors. The 1.5.7 widget and geometry audits passed, with ZIP CRC and installer/hash checks. Compiled DLL SHA-256: `b41f980e899a2bc898dd2f4c05db2661b271ec5b7234293b331d50bb1ce4c44f`. Verified direct-install ZIP SHA-256: `a836d8abf4fdfcf598d7706a02b8c125d58a9f775f197b0bf4d8312cb22c32d4`. **Gate B only, compiled and not tested in-game.**

**Engineering status:** The system is conditionally applied in the existing source. No observed Bannerlord session has yet proven that every native stage exposes movable panels or that in-game control placement is pixel-identical. Full game input, resolution, FaceGen, fief/family and save/reload tests remain required. The development module manifest remains `v0.38.0`.

[Measured alignment and live acceptance checklist](docs/V044_MEASURED_NATIVE_CONTROL_LAYOUT.md) · [Draft PR #75](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/75)

---

# Calradia Unified v0.43 development: original native creator control styling

**Q1 implemented at source level:** Unlike v0.42's header-only framing, v0.43 now alters the actual game-owned `CharacterCreationNarrativeStageView`, `CharacterCreationBannerEditorView`, `CharacterCreationClanNamingStageView`, `CharacterCreationReviewStageView` and `CharacterCreationOptionsStageView` **control brushes and text colors in place**. It uses the exact native `GauntletMovieIdentifier.Movie.RootWidget`, a per-widget `Brush.Clone()` and a saved original appearance. Heraldry color swatches and native 3D FaceGen are untouched. Native clan input and difficulty sliders get guarded private brush styling. Dynamic new life-story choices receive the same colors; **F10 restores native control styling**, and **F9 remains the native face-dock restore**.

**Pinned Windows build:** [run 44467336043](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44467336043), source commit `fb9d6969bf5ef64d0d648bada0cbd426ba27adbf`. **708 C# gameplay policy checks passed**, zero compiler warnings/errors, exact 1.5.7 Gauntlet and character-stage metadata audits, ZIP integrity and simulated installer passed. DLL SHA-256 `61fce6458651c3e1f6462cff43e9a363c5cafeed9d77684165b7eecac171624a`.

**Gate B only, COMPILED_UNTESTED:** This is true native control palette re-skinning without replacing the game-owned ViewModels or widgets. It does **not** reposition original controls to a new pixel-identical layout, or certify the in-game hover, input, mouse, controller, cinematic and final map transitions. Native widgets that cannot be re-skinned safely keep the original style and are logged as partial/unavailable. The manifest intentionally remains `v0.38.0` in this dev lineage.

[Exact native control re-skin and live acceptance](docs/V043_NATIVE_WIDGET_RESKIN.md) · [Draft PR #74](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/74)

---

# Calradia Unified v0.42 development: coherent styling across native creator screens

**Q2 native visual match:** Every real native stage now receives an appropriate CU presentation: native-bound Culture keeps its full-screen CU chooser; FaceGen retains its genuine 3D renderer and native sliders; origins, banner, clan, final review and difficulty use the shared **91 px slate header, gold rule, five-chapter rail on wide screens, matching stage titles and context**. At 1366 px, large context panels are hidden to protect original controls. Native character creation remains game-owned. The visual chrome is read-only, requires no experimental switch, and cannot skip native callbacks. CU-guided World, stats, House and Fortune keep their existing native-inspired full-screen layout.

**Build evidence:** [v0.42 Windows run 44463786087](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44463786087), source SHA `02e2de047b8416436e3fbf2976daffbfa7825b6e`, **693 gameplay policy checks**, zero compiler warnings/errors, pinned Bannerlord 1.5.7 API audit, CRC and installer simulation success. Compiled DLL SHA-256 `384b81812cc631306e1fc25c9c66ee6120aea9a0566c611424fc15ed41742d07`.

**Gate B, COMPILED_UNTESTED:** Native narrative, banner, clan naming and difficulty buttons still use their original game-owned widget brushes and placement. The UI **shares CU framing, not pixel-identical controls**. Actual in-game resolution, focus, mouse and final map tests remain required. No claim of a persistent 3D preview or a new uninterrupted game-start cinematic path. Manifest remains `v0.38.0` for this development lineage.

[Detailed change and visual acceptance contract](docs/V042_COHESIVE_NATIVE_CREATOR_CHROME.md) · [Q2 draft pull request](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/73)

---

# Calradia Unified v0.41 development: identity-first guided character creator

The active main-menu **Sandbox** creator now begins with short **World and Starting Position** screens only. Bannerlord then owns the real **culture, appearance, life story, post-origins stats, banner and clan**. A second set of short guided CU screens runs **after banner and clan** for **spouse, children, fortune, fiefs and household residence**. Native review and difficulty remain the proper finalization path. The World and House/Fortune screens load the **same** `CUGuidedCreator.xml` full-screen charcoal/slate/gold layout with one relevant field at a time, automatic conditional screens, and Back/Continue/Escape navigation.

There are **no experimental toggles** for this guided flow. Native stats mode remains optional, and choosing Native now skips the extra skill/trait edit pages. Fresh starts clear inherited item grants, stats, gold/influence/troops, seed and hidden world modifiers. House validation uses the live Hero's age before any family grant at phase 9.

**Engineering boundary:** this is the source-level Q1/Q3 redesign, not a game-tested full replacement. Native culture/FaceGen/narrative/banner/clan/review/difficulty stages still display original engine-owned controls with CU chapter context. Identical layouts on all native stages, uninterrupted cinematics/loading and direct playable map entry have **not** been verified. The genuine 3D scene is still FaceGen-stage-only. Manifest retains `v0.38.0` in this development candidate.

**Pinned Windows CI:** [run 44461909073](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44461909073), source commit `697517cd1251afb2ddea86ef04f99eb2aef622f9`. **681 C# gameplay policy checks passed**; net472 v1.5.7 build completed with **zero warnings and zero errors**. ZIP contents, CRC, DLL identity and simulated installer passed. Compiled DLL SHA-256: `6c641c2d5602fc11dc3ec222f2322cc4cf1627f84181ac4ba7e3135ed2d137c0`. **Gate B: compiled, not observed in-game.**

[Implementation and live acceptance checklist](docs/V041_GUIDED_IDENTITY_FIRST_CREATOR.md) · [Draft PR #72](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/72)

---

# Calradia Unified v0.40 development: native post-origins stats and personality

**New Chapter II editor:** after completing Bannerlord's actual life-story narrative, one native-styled full-screen CU progression step opens before the original banner editor. Choose **Native** (default, no overrides), **Balanced** (30-point budget), or **Unrestricted** (legal native stat ranges). Three compact pages cover Attributes, grouped Skills & Focus, and five Personality Traits. Rows show actual current Hero values and any draft differences. Exact entry and Reset are available. All selected native narrative positive-effect texts are paginated from Bannerlord 1.5.7's verified `SelectedOptions` field. Trait help explains that NPC relationships depend on the native event, not a guaranteed flat modifier.

**No experiment toggles.** A current accepted CU Sandbox session is required. The original game-owned narrative, banner and finalization stages remain intact. The one-shot phase-9 behavior applies only a valid saved plan, after the native character and starting role exist.

**Scope limit:** this is a new post-origins editor within the existing two-part startup. The full revised five-chapter, multi-screen creator, unbroken final map entry and persistent live 3D model have **not** been completed. The game manifest remains `v0.38.0`. A green CI job proves compilation and static contracts, not live visual or gameplay success.

**Pinned Windows CI:** [run 44459027527](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44459027527), source SHA `095d79d3001f38b9e1ec5d7f491c42878a20c5d8`, **677 policy checks passed**, zero compiler warnings/errors. The DLL SHA-256 is `352cd1a169cd63965d2fb85a1bb41110c361a6465f6923f76f72841a09a9f7a9`. ZIP CRC and installer simulations passed. **Gate B only: compiled, not observed in-game.**

[Implementation, point costs and live acceptance tests](docs/V040_POST_ORIGINS_STATS_TRAITS.md) · [Draft PR #71](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/71)

---

# Calradia Unified v0.39 development: automatic native creator bridge

**New Sandbox starts no longer require experimental toggles.** After accepting the CU starting conditions, the native-state CU shell, native-bound culture chooser and reversible original FaceGen right-dock attempt activate automatically. FaceGen's F9 recovery and native fallback remain available. The historical optional extra Face Workshop and Review stages are suppressed for this path.

**Not final:** this remains the two-part pregame CU setup followed by the native character wizard. The full replacement, persistent live 3D viewport through all five chapters, and Begin Campaign directly to map have not been delivered. The module manifest stays `v0.38.0` in this development candidate. Do not confuse automatic activation with completed single-window creation.

**CI evidence:** [Windows run 44454724380](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44454724380), SHA `0a6b0702b0825b62893cf9555f5a56591e4c80f6`, 656 policy checks passed, 0 compiler warnings and errors, compile/ZIP integrity and installer checks passed. DLL SHA-256 `f2ccb4422a300889411042b8dcc741435d591c68f930b68a141d79e739b7a5d5`. **Gate B: COMPILED_UNTESTED**, no in-game creator or saved campaign verification.

[Exact remaining Q1/Q2/Q3 engineering and acceptance gates](docs/V039_AUTOMATIC_NATIVE_CREATOR_BRIDGE.md).

---

# Calradia Unified v0.38.0 | Responsive Native-Style Creator and Original FaceGen Right-Dock Trial

**Target:** Bannerlord PC v1.5.7.125802. **Status:** experimental, compiled candidate only after pinned Windows CI succeeds. Gameplay and visual tests are required.

**Q1, native-style responsive UI:** pregame CU and CU native culture now grow beyond the fixed 1280px desktop dialog. At 1920×1080 their context fields expand, chapter rail and house details stay anchored, and native-style header/footer and actions follow the display. Compact 1366×768 retains all five real chapters, children, ASO role and fief effects.

**Q2, original 3D FaceGen right dock:** an extra **OFF-by-default** advanced flag uses the game's existing `View.SetScale`/`View.SetOffset` to shift its actual native FaceGen renderer right, and moves the same game-owned native sliders left. No duplicate `BodyGeneratorView`, Hero, scene or camera. Press **F9** during the face editor to restore or retry the stock layout. You must have **Experimental CU native-state shell = Yes** and **Experimental right FaceGen dock = Yes**, with the separate workshop and review stages off. This is **not yet verified in-game** and is strictly a real FaceGen-stage trial, not a persistent 3D scene across all five chapters.

**Q3, one creator:** still **not completed**. The pregame advanced-start settings must be applied before the Campaign is constructed, and the actual native origin/banner/clan/difficulty stages must keep their callbacks. We retain the safe native wizard instead of suppressing it to fake a seamless result.

[Full technical design and test gates](docs/V038_RESPONSIVE_FACE_DOCK_ONE_CREATOR_GATE.md) · [v0.37 fullscreen visual baseline](docs/V037_FULLSCREEN_NATIVE_SANDBOX_UI.md)

---

## Previous v0.37 fullscreen candidate

# Calradia Unified v0.37.0 | Full-Screen Native-Styled Sandbox Creator

**Target:** Bannerlord PC v1.5.7.125802. **Status:** compiled visual prototype, game playtest required.

The custom Sandbox creator now has a **full-display charcoal/slate backdrop, native-style gold rules and full-width header/footer**, instead of a bordered popup. The five numbered chapters, native-style action buttons, contextual settings, and house/holdings summary retain their bindings and gameplay meaning. At 1366×768 the 1280×700 centered control area nearly fills the screen. At larger sizes the background and screen chrome cover the entire display, with the readable controls centered.

The optional native-bound CU culture chooser uses the same full-screen design. Its culture selection, continue and back handlers still operate through Bannerlord's real character-creation VM. The genuine 3D appearance scene and camera remain untouched and game-owned. **This is a visual change, not the completed one-screen character creator.** A native wizard still follows the pregame options, and a permanently docked right-hand 3D viewport is not delivered here.

[Design and runtime acceptance checks](docs/V037_FULLSCREEN_NATIVE_SANDBOX_UI.md) · [Five-chapter native shell contract](docs/V036_NATIVE_FIVE_CHAPTER_SHELL_VIEWPORT_CONTRACT.md)

---

## Previous v0.36 experimental native shell

# Calradia Unified v0.36.0 | Five Native Chapters and Safe 3D Viewport Contract

**Game target:** Bannerlord PC v1.5.7.125802. **Status:** compiled candidate only after Windows CI verifies the code. The five original CU pregame selection pages and native screens are **still separate**. The complete one-window creator and a persistent cropped right-side 3D scene are **not yet implemented**.

This candidate builds on the native-bound CU culture chooser and original real 3D FaceGen from v0.35. With **Starting Life → More options → Experimental: CU culture chooser and real 3D face audit = Yes**, its persistent `CharacterCreationState` shell now displays the five frozen chapter labels and highlights the game-owned native stage in progress. Land & Home displays `START OPTIONS SET`, since King/Lord, kingdom, fief and scenario are already supplied to `SandBoxGameManager` **before** native `CharacterCreationState` starts. This rail is read-only. It does not let the user jump over Bannerlord narrative or difficulty callbacks.

The face page uses the same genuine game-owned `BodyGeneratorView`, native 3D camera, FaceGenHistory and slider controls. The CU shell identifies the native scene and displays **NATIVE 3D SCENE: FULL SCREEN; DOCK NOT YET VERIFIED**. Layout target geometry is defined for both 1920×1080 and 1366×768. No fake portrait, duplicate scene or falsely claimed right-hand 3D crop is shipped. The correct technical work left is a verified render/viewport transfer with intact input and scene ownership.

See [v0.36 technical contract](docs/V036_NATIVE_FIVE_CHAPTER_SHELL_VIEWPORT_CONTRACT.md) and [v0.35 culture/face contract](docs/V035_NATIVE_CULTURE_CHOOSER_AND_FACE_DOCK_GATE.md). Keep the known working build as fallback and test new native-layer changes only in a disposable campaign.

---

## Previous v0.35 candidate

# Calradia Unified v0.35.0 | Real Native-Bound CU Culture Chooser

**Target:** Bannerlord PC v1.5.7.125802. **Release status:** opt-in compiled, game-untested candidate. The requested final single-window creator and persistent right-hand 3D character viewport are **not yet complete**.

This is the first CU culture UI that invokes the actual game-owned culture selection and native stage finalizer. When you turn on **Starting Life → More options → Experimental: CU culture chooser and real 3D face audit**, CU displays the cultures and descriptions from the native `CharacterCreationCultureStageVM`. Each choice delegates to the native `ExecuteSelectCulture` handler, and Continue delegates to `OnNextStage`, preserving culture-dependent appearance, player name generation and origin reset. The original native stage remains active underneath. The player can press **Use native culture UI** at any time for immediate recovery. The flag remains **OFF by default**.

The next real 3D face page still uses **only** Bannerlord's original `BodyGeneratorView` and FaceGen scene, camera, body controls and history. A CU top-left diagnostic confirms its actual `SceneLayer` and `GauntletLayer` are present. **No right-hand viewport relocation has been performed**, since the exact native `SceneView` API does not supply a safe viewport rectangle setter. The original face stage and later native origin/banner/clan/difficulty pages remain visible.

The five CU pregame chapters, actual Advanced Start roles and kingdom/fiefs, spouse/children and one-shot runtime grants remain intact. Never combine this native-state experiment with the separate CU Face Workshop or 3D Review experimental stages.

[Technical v0.35 contract and in-game checks](docs/V035_CU_NATIVE_CULTURE_AND_FACE_DOCK.md) · [v0.34 native-state shell](docs/V034_NATIVE_STATE_SHELL_CULTURE_FACE.md) · [five-page CU flow lock](docs/V032_LOCKED_SANDBOX_CREATOR_FLOW.md)

---

## Previous v0.34 pilot

# Calradia Unified v0.34.0 | Real Native-State Culture and Face Shell Pilot

**Game:** Bannerlord PC v1.5.7.125802. **Status:** an opt-in, read-only CU stage ribbon that attaches to the real engine `CharacterCreationScreen`. **The native wizard still appears.** This is a vertical integration prototype, not the requested complete one-screen creator.

A new `NativeStateShellEnabled` option under **Starting Life → More options** lets the game overlay a nonfocus CU ribbon on the existing native character-creation screen. It reads actual `CharacterCreationManager.CurrentStage` and `CharacterCreationContent.SelectedCulture` as the native Culture, 3D FaceGen, origins, banner, clan and difficulty stages advance. It does not replace game views, intercept native controls, add fake Heroes or instantiate another face editor. It is **OFF by default** for each new campaign and cannot be combined with the old separate CU Face Workshop or 3D Review experiments.

This build follows the locked five-page CU design and its future three-region viewport plan. Full native culture and face controls must later be shown within one persistent CU screen, followed by origins, banner/clan and difficulty. **Never advertise a second-screen removal until the actual game opens directly to the map from one CU Finish action.**

[Technical v0.34 contract and runtime checks](docs/V034_NATIVE_STATE_SHELL_CULTURE_FACE.md) · [v0.33 UI contract](docs/V033_THREE_LOCKED_CREATOR_UI_DECISIONS.md)

---

## Previous v0.33 candidate

# Calradia Unified v0.33.0 | Locked Sandbox Flow and Honest Feature Availability

**Target:** Bannerlord PC v1.5.7.125802. **Status:** new UI candidate, native screens still follow the CU pregame creator. The real one-screen integration is not delivered by v0.33.

The five chapters remain **Starting Life → Bloodline → Fortune → Land & Home → Review Your House**. The read-only controls for **starting age, siblings, equipment theme and preferred starting town** now say **Unavailable in this build** with a reason. None of these can be edited in the pregame CU screen. Old unsupported requests are cleared on every new Sandbox. Supported spouse/children, native role/kingdom, resources and fief options remain editable. The 3D preview remains the **real engine-owned player renderer**, and the final requirement is a persistent player model across the future native-state CU shell. No fake pregame portrait was added.

See [v0.33 UX decisions](docs/V033_THREE_LOCKED_CREATOR_UI_DECISIONS.md) and [v0.32 frozen flow contract](docs/V032_LOCKED_SANDBOX_CREATOR_FLOW.md). This branch changes the CU pregame field UI, but it does not remove the separate native character creation that currently follows review.

---

## Previous v0.31 prototype

# Calradia Unified v0.31.0 | Experimental Native 3D Face Workshop

**Target:** Bannerlord PC v1.5.7.125802. **Status:** [Windows CI #44362408521](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44362408521) passed at commit `f7574effec028ac4d52de2ac8397666ee28b9c76`, **Gate B COMPILED_UNTESTED**. The game-facing workshop and save/reload remain untested. **The second native character wizard still appears**.

In **Sandbox → Starting Life → More options**, an optional **Experimental: edit live 3D face in CU workshop** choice adds a distinct CU stage after native difficulty. It composes Bannerlord's real `CharacterCreationFaceGeneratorView`, preserving the native BodyGenerator, model, face history, camera and face controls. A CU read-only context ribbon remains unfocused so the editor owns user input. The option is **off by default**, and the previous experimental CU review cannot be selected at the same time.

The v0.31 DLL is 242,176 bytes, SHA-256 `23321aa2d6a0c049965d37e31496389500ca53f3c24821381290296855048be6`. The inner runtime ZIP passes CRC and one-DLL checks, SHA-256 `8916d60f93eedab9145f16082d265bf482e167db7d02429f14c06d62ac4c208c`. The verified CI run executed **627 independent C# policy checks** and compiled with **0 warnings / 0 errors**. These are build results, not native screen-rendering or gameplay test evidence.

The new `NativeCreatorParityPolicy` guards all seven native stage prerequisites before the optional workshop can load. No native stage view is replaced, no child Heroes are created during preview, and the native character-creation sequence stays unchanged when the mode is disabled. The goal of a single CU creator from **Sandbox click to campaign map** remains open under [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).

**Technical and test protocol:** [v0.31 Face Workshop and Unified Creator Parity](docs/V031_FACE_WORKSHOP_AND_UNIFIED_CREATOR_PARITY.md). Keep the original v0.29 review and family features intact.

---

## Previous v0.29 candidate and established mechanics


**Target game:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Status:** COMPILED_UNTESTED for v0.29 UI and the v0.28 family-home feature. Previous in-game evidence validated genuine spouse and children, but the new stage, portrait gallery and save persistence still need separate Bannerlord playtests.

### New in v0.29

The starting-conditions creator uses clearer five-chapter navigation, explicit spouse and child summary text and a handoff cue to **the game's own live 3D face editor**. It never replaces native culture, face, background, banner, clan naming, review or difficulty screens. The fully unified single-screen creator is **not complete**.

Under **More options** on Starting Life, the experimental setting `Preview CU choices inside Bannerlord's 3D review` can append one distinct origin-review stage **after native difficulty**. It wraps Bannerlord's real rotatable 3D player review through its native stage-listener contract, has a nonfocus CU summary layer, and is **OFF by default**. The CU stage has no inherited native Review-stage registration. The original v0.22 duplicate FaceGen view remains banned. If engine view discovery fails, the extra page is skipped and logged. If the CU summary prefab fails, the base 3D review remains available.

After an actual family exists, native town/castle menus show **CU: View my household portraits**. The gallery reads the real main character, native spouse and player-linked children, plus age, parents and recorded home. Images use native Hero body data. It never creates preview relatives, ages children, or recruits infants. Missing baby visuals must not become fake adult heroes.

Read [v0.29 native UI and portrait runtime acceptance](docs/V029_NATIVE_CREATOR_AND_FAMILY_UI_ACCEPTANCE.md) and the [permanent PowerShell installation and playthrough standard](docs/POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Both the standard gameplay audit and its optional `-RequireNativeReview` / `-ExpectedPortraits` checks run under Windows PowerShell 5.1.


## Standard Windows PowerShell install and playthrough report

The project's **standing player workflow** is documented in [PowerShell Installation and Playthrough Standard](docs/POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Future Calradia Unified coding chats should read that file plus [AGENTS.md](AGENTS.md) and [PROJECT_INSTRUCTIONS.md](PROJECT_INSTRUCTIONS.md) before suggesting a download.

Install the exact successful Windows CI artifact through [`scripts/Install-CU-From-Artifact.ps1`](scripts/Install-CU-From-Artifact.ps1) with **`-ExpectedDllSha256`** from that same run. It rejects another same-version DLL before changing the installed module and backs up the previous folder.

After entering the campaign map, run the read-only [`scripts/Test-CU-Playthrough.ps1`](scripts/Test-CU-Playthrough.ps1) with your expected child count. This reports the latest accepted start, native family verification, actual children, home settlement events and exceptions. Check save, full exit and reload separately. For crash evidence, use [`scripts/Export-CU-Diagnostics.ps1`](scripts/Export-CU-Diagnostics.ps1).

[Successful standard-workflow CI run #44345749188](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44345749188) verified the CI hash reject-before-install test and a synthetic latest-campaign diagnostic, alongside 617 game-policy checks. The downloaded ZIP remains a **COMPILED_UNTESTED** candidate, not a proven game release.

## Observed player state and actual cause

Previous runtime logs proved a newly generated `NobleRandom` Vlandian spouse joined the player's clan and children received real parent links with inherited body properties. The user's first v0.26 family test created two children from a saved count of two; v0.27 corrected the separate numeric confirmation issue and now reports exactly one child as selected.

The player now reports spouse and baby shown as **Holding** in the Clan screen. Bannerlord's native underage heroes are not ordinary party members. Infants cannot be recruited as troops just to move them. `Holding` does not invalidate parentage.

## New v0.28 family residence

The five-step CU starting-conditions creator now includes **Where should your family live?** on Kingdom & World:

- `OwnedFief` (default): an owned town or castle, preferring your safe clan home and then a town before a castle.
- `ClanHome`: use the clan's owned home fortification if safe, then another owned fief.
- `SpecificOwned`: choose the exact internal settlement ID for an **already owned** castle/town. No cross-kingdom ownership changes. Unknown, besieged, contested or village locations are refused.

After native character creation, fief assignment, real spouse marriage and child creation are complete, `SandboxFamilyHome`:
- Places the **adult spouse** into the owned settlement with native `TeleportHeroAction.ApplyImmediateTeleportToSettlement`.
- Assigns a **minor child** `StayingInSettlement` directly, which is a saveable Hero field registered in the settlement's non-party resident list. Does not activate, age, enlist or teleport a baby to a party.
- For an adult offspring not assigned to a party, uses native active-Hero relocation. Only an adult still in the native `NotSpawned` state is activated first.
- Verifies recorded settlement and residents, logs `cu_family_home_result`, `cu_family_child_home`, `cu_family_home_status`.

**Important:** The vanilla Clan screen might still show **Holding** for a child under 18 even after residence is saved. A family-home location is not the same as a physical baby NPC walking around a keep. This candidate does not replace child lifecycle or game scenes.

## Use your existing saved family

You do **not** need to create another spouse or child. Load your existing Sandbox save and enter any town/castle **owned by your clan**. In the settlement's native menu, choose:

**CU: Make this my family's home**

This explicit in-campaign action places your already-existing spouse and minors at the current settlement. It does not spawn relatives again or modify anyone else's clan. Save, quit, reload, and verify the location and parent links remain. A spouse who is in an active party, captive or otherwise unavailable is not forcibly removed.

## Creator and native appearance

CU remains five pages (Starting Life, Household, Resources, Kingdom & World, Review), with a dark Gauntlet-style panel and family-home choice. The native Bannerlord culture, 3D appearance, origins, age and difficulty screens follow as **Part 2**. Existing native character creation has not been replaced, so v0.22's competing FaceGen handler crash is not reintroduced.

## Testing and evidence

- [Full v0.28 family-residence acceptance checklist](docs/V028_FAMILY_RESIDENCE_ACCEPTANCE.md)
- [Windows build and package gate](.github/workflows/bannerlord-build.yml)
- [Original one-child count correction](docs/V027_CHILD_COUNT_NATIVE_HANDOFF.md)
- [Long-term unified 3D creator issue](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44)

The compiled module is **not yet game-verified for family home placement**, and save/reload of the residence is still a required acceptance step. Test on a backed-up save or a disposable new campaign before relying on settlement residency.
