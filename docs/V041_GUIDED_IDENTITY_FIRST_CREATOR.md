# v0.41 development: identity-first guided Sandbox creator and unified chapter sequence

**Target:** Bannerlord PC `v1.5.7.125802`. **Branch:** `feature/v041-identity-first-guided-creator`. **Status:** source implementation and pinned CI only, no observed game session. The existing module manifest remains `v0.38.0` until production gates pass.

## Corrected player-first decision flow

The old five-tab CU pregame wizard was logically reversed. It asked about spouse, children, fortune and residence **before culture, character age, appearance, native origins and clan**. This development branch replaces the **active entrypoint**, not merely the page labels:

1. **Your World, pre-campaign:** the original main-menu Sandbox action opens `CUGuidedCreator`. One field per short screen covers scenario, optional scenario factions/variant, starting career/role, required kingdom. Advanced seed/world modifiers are optional. This is the only part that must happen before campaign initialization. The old `CUSandboxCreator` pregame five-tab implementation remains in source as a legacy reference and is **not loaded** by `SandboxCreatorScreen`.
2. **Your Character, engine-owned:** native culture and real FaceGen appearance, native life-story background/childhood/youth/adulthood. v0.40's automatic three-mode stats editor opens after narrative, before banner. Native callbacks and actual game-derived effects remain authoritative.
3. **Your House, after banner and clan:** Bannerlord's original banner and clan naming stages run first. A new full-screen CU guided layer then asks about spouse, spouse age/name/background, native family background, child count and names/ages/sex only for each requested child.
4. **Your Fortune, still post-clan:** starting gold, influence, extra native recruits, optional eligible fief, explicit fief transfer consent, spouse/family home. A last CU review confirms the **household and estate request**, not the entire native character a second time.
5. **Begin Your Legacy:** resume the original game-owned character review and difficulty, preserving its proper effects, then let Bannerlord finalize the campaign. The final map/cinematic behavior is still game-owned and **not yet verified as interruption-free**.

The shared `CUGuidedCreator.xml` is used for **both pre-campaign World and post-clan House/Fortune**. It uses the same full-screen charcoal/slate/brass visual system and footer. There is one active field per screen. Unneeded spouse, child, kingdom, fief, scenario and family-home questions are hidden. All native choices remain within their own existing game stages and native-focused Gauntlet controls.

## Source implementation

- `Source/CalradiaUnified/CUGuidedCreatorVM.cs`: same ViewModel for pregame and post-clan, conditional field sequence, optional extras, numeric bounds, exact text/number inquiry, Back/Continue, one-shot confirm, right-hand live summary. `CommitWorld` calls the real `SandboxNativeStartOptions.TryPrepare` **before** starting the campaign. `CommitHouse` uses the actual `Hero.MainHero.Age`, validates spouse/child and fief/home requests, then saves for phase-9 one-shot effects.
- `Source/CalradiaUnified/CUNativeHouseLayer.cs`: attaches the **same movie** on the existing `CharacterCreationScreen` after observing both the real `CharacterCreationBannerEditorStage` and `CharacterCreationClanNamingStage`. The editor overlays original Review or Options stages; it neither constructs a second stage/screen nor skips native callbacks. On confirmation it detaches and restores original input focus. Back delegates to `CharacterCreationManager.PreviousStage()`. A missing native view or failure returns to the original game editor and records diagnostics.
- `Source/CalradiaUnified/SandboxCreatorOverlay.cs`: `SandboxCreatorScreen` now loads `CUGuidedCreatorVM(true)` and `CUGuidedCreator`, NOT its retired five-tab screen. Accepted plans auto-enable the native shell and face dock, without user experimental controls.
- `Source/CalradiaUnified/CUNativeStateShell.cs`: retains native culture, FaceGen and v0.40 post-story stats step and integrates House/Fortune layer on active and safe-fallback native screens. Same five-chapter stage rail, revised to `Your World → Your Character → Your House → Your Fortune → Begin Your Legacy`.
- `Source/CalradiaUnified/SandboxOriginProfile.cs`: new-campaign reset removes stale item grants, stat edits, resources, seed, and world modifiers; `SaveForPlayer` validates children against the **actual native player's age** at the post-clan stage. Load verifies structural adult eligibility without pretending every founder is age 30.
- `scripts/test_guided_creator_flow.py`: new source contract proves the **active** movie route, two temporal phases, one-question UI, no early family questions, real native stage ordering and the fallback. Windows CI runs it before compilation. The old v0.32 five-tab test remains for legacy source reference, not actual routing.

## Boundaries and risks

This is a working **source-level architecture**, not a verified complete player-facing release. These conditions remain:

- The exact two-stage timing and visual attachment of the House overlay require real game observation. If v1.5.7 does not visit both native Banner and Clan stages, the overlay deliberately leaves the original native stage intact. It does not claim the family request was completed.
- Native face, narrative, clan, banner, review and difficulty controls remain Bannerlord-owned. They share a compatible dark/gold visual direction and CU chapter rail, but **full pixel-identical layouts across every native stage have not yet been proven or implemented**.
- Native transitions, startup cinematic/loading, native final review, and playable map arrival remain Bannerlord-controlled. The current code does not bypass them or invent a direct-to-map action.
- The real FaceGen 3D dock is still native FaceGen-stage-only; it is not a persistent 3D preview through every chapter. The reversible F9 fallback stays active.
- There is no implemented source-driven controller for native identity/age editing outside existing Bannerlord stages. The original native choices must remain available.

## Required acceptance

For Gate C through Gate G, run actual fresh Sandbox starts at 1366×768, 1920×1080 and ultrawide, check original role/kingdom/scenario/fief and native real culture/face, complete background and native stats, verify the new House/Fortune step appears **only after both banner and clan**, test spouse and 0/1/6 children with valid age rules, fief transfer consent, input focus and Escape/Back, native difficulty, final map and save/quit/reload. Check diagnostic `cu_guided_world_start_committed`, `cu_guided_house_attached`, `cu_guided_house_fortune_committed`, `sandbox_start_contract` and `cu_family_verification`. No duplicate grants, native stage skipping or double prompts.

**Do not label this build as finished one-screen character creation or as a proven interruption-free experience.** The source has the corrected logical order and the same styled guided movie in both phases. Live observation remains the release blocker. See [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).
