# v0.30: One Calradia Unified Sandbox creator, no second native wizard

**Target game:** Bannerlord PC v1.5.7.125802. **Status:** engineering contract and exact-reference API audit. This document does not claim the merged UI exists.

## Observed current route

1. \`SandBoxNewGame\` is overridden by \`SandboxMainMenuEntry.Install\`.
2. \`SandboxCreatorOverlay\` opens the pregame \`CUSandboxCreator\` Gauntlet movie. It edits a saved CU origin profile (role, spouse, children, fief, world, resources).
3. **Finish** calls \`MBGameManager.StartNewGame(new SandBoxGameManager(...))\`.
4. The native game loads its own character-creation state. Culture, 3D face/body, background choices, banner, clan details, review, and difficulty are still separate native pages. An optional CU review in v0.29 adds a further page, not a replacement.
5. The native start applies its ASO changes before CU's phase-9 family additions. This order must be preserved.

The issue is not the first Default/Advanced selector, which was bypassed in v0.19. The remaining issue is the **second native creation sequence**.

## Player-facing contract

Click **Sandbox** once and interact with one continuous CU-branded editor, using one back/next/review/finish navigation system. Its parts should be:

- **Identity and Culture:** real native culture selection, identity, career/role and kingdom.
- **Appearance:** the existing native live 3D player model and actual native face/body editor, including camera and equipment preview. No static portrait substitute.
- **Origins and Age:** real native culture-dependent childhood/adolescence/adulthood effects and applicable age choices.
- **Household:** spouse, children and siblings, with genuine native family links only after character finalization. Show pregame relatives as plans, not fake existing Hero portraits.
- **Banner and Clan:** native banner/clan name values and a visible result.
- **Starting World and Resources:** real ASO scenarios, controlled fief choice, gold, troops, items and skills. Mark unavailable or unsupported choices clearly.
- **Difficulty and Review:** bind to native difficulty settings, summarize actual selected values and refuse Finish on invalid required choices.

There must be **no native culture, appearance, origin, age, banner, or difficulty screens after the CU Finish action**. One Finish must enter the campaign map. Back and Escape must not start a campaign or partially create family Heroes.

## Engine ownership and source tasks

\`SandboxCreatorOverlay\` is currently constructed **before** the campaign exists. The real 3D player scene and \`CharacterCreationManager\` exist **inside** the campaign's \`CharacterCreationState\`. A cosmetic reskin of the pregame XML cannot transfer the engine-managed face editor into it.

The native-state bridge must:

1. Inspect the **exact v1.5.7.125802 reference DLLs** for \`CharacterCreationState\`, \`CharacterCreationManager\`, \`CharacterCreationScreen\`, native stage views, the view attribute, \`SandboxCharacterCreationContent\`, \`SandBoxGameManager\`, and their constructor/event signatures. \`scripts/Inspect-CU-Creator-API.ps1\` produces this metadata from pinned references in Windows CI.
2. Keep ASO initialization and engine finalization intact. Never reimplement culture/narrative XP bonuses using guessed arithmetic.
3. Reuse the native \`BodyGeneratorView\` or equivalent engine-owned live renderer with its correct scene, camera, refresh lifecycle and input focus.
4. Adopt one CU Gauntlet screen shell inside \`CharacterCreationState\`. Handle page transitions **within the CU shell**, not through visible duplicated native stage screens.
5. Eliminate or hide the game's native stage visuals **only after** corresponding CU controls have proven parity. Never install a duplicate \`CharacterCreationFaceGeneratorStage\` or \`CharacterCreationCultureStage\` view attribute. That route previously crashed in v0.22.
6. Bind native culture, face/body, age, banner and options through version-verified game adapters. Preserve phase-8 ASO and phase-9 CU family processing and the existing one-child contract.
7. Keep a safe rollback route to the v0.29 dual-screen sequence until one-screen gameplay tests pass. No implicit conversion of old saves or activation of infant Heroes.

## Readability constraints

At 1920x1080, 1366x768 and ultrawide, keep active controls and their descriptions inside visible bounded panels. Use a single heading, short labels, one contextual control group, clear numerical units, and a persistent right-side household/role/world summary. Do not put \`not applied yet\` fields alongside applied values without an explicit status. Numeric changes must write the stored plan immediately, as fixed in v0.27.

The portrait gallery in v0.29 is **post-creation only**. It is not a substitute for the live pregame character preview.

## Acceptance gates

- **A:** machine-readable pinned API audit and source-only checks establish where native view/data ownership lives.
- **B:** compilation and independent tests against the pinned reference package, no duplicate view registration, one-shot start and family contracts.
- **C:** actual installed game displays only CU pages from Sandbox click through character editing. Live player model changes face/body and culture. Native pages do not reappear.
- **D:** all native origin bonuses, age, banner, clan/kingdom, role, fief, spouse/child count, difficulty and ASO options match the chosen review.
- **E:** back, Escape, restarting Sandbox, save/quit/reload and 30-day campaign check. No duplicate Heroes, phase-8/9 processing, stage handlers or screen input layers.
- **F:** readable focus, hover/click, controller navigation, scale and input checks at three resolutions. Real screenshots and a recorded run.
- **G:** publish a matching tested DLL and one-folder module ZIP. A green compiler alone is not proof of a one-screen creator.

**Current blocker:** inspected engine metadata and a safe native scene/state compositor. The v0.30 audit only addresses the first; native page removal is prohibited until the replacement works in the game.
