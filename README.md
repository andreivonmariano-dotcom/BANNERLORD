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
