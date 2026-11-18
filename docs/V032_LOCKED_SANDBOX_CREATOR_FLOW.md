# v0.32 Sandbox flow lock: one five-page CU creator

**Status:** UX and navigation contract, not a native-screen replacement. **Baseline:** compiled v0.31 at `646364c676563b4e149c8f1d56d49750bdbf9079`. **Target:** Bannerlord PC v1.5.7.125802. This flow is the agreed engineering baseline for issue #44. No game behavior is changed by this document.

## Why this freeze exists

The observed working v0.31 route is **Sandbox main menu → CU five-page pregame creator → separate native culture, face, origins, banner/clan, review and difficulty screens → campaign map**. The extra CU 3D workshop is a default-off separate stage, not a replacement. The UI itself openly says `PART 1 OF 2: STARTING CONDITIONS` and `NEXT: NATIVE CREATOR`. A DLL compiling or working in the game does not prove one-screen integration.

**Final user contract:** **Sandbox → one continuous CU creator → Begin Campaign → map**. The native game systems must run under the CU interface. There must never be a second *visible* native character-creation wizard after final confirmation.

## Frozen five top-level tabs

The CU five-tab **order** and the role of each page are locked. Internally the view model currently stores these as `Identity, Household, Resources, Location, Review`. The existing visual labels are `Starting life, Bloodline, Fortune, Land & home, Review your house`. Do not add an extra top-level page just to wrap a native screen.

1. **Starting Life** (`Identity`): choose the player's actual Sandbox starting role and, for roles that require it, a kingdom. The real native `StartType` and `KingdomId` must be valid for the installed game. The future CU integration belongs here for native culture, character gender, face/body and appearance preview. The 3D scene must be the actual `BodyGeneratorView`, not a portrait substitute. Age must be driven by native age selection, not the v0.31 profile-only `PlayerAge` field.
2. **Bloodline** (`Household`): choose real starting spouse, origin/background, spouse name, adult age, gender, and exactly the requested number of children with names, ages and genders. A family choice is a **plan** until native creation finalizes. No pre-creation fake Hero or portrait. Existing post-creation spouse, minor residency and real family portrait code remains intact. Current siblings cannot be honored and must continue to be rejected at final validation. Future clan naming/banner subviews may be presented within CU when the native stage manager reaches those real stages, without adding a sixth top-level tab.
3. **Fortune** (`Resources`): gold, influence, recruit count, equipment kits, exact items and optional character progression. Default/native sentinel values must keep the game's grants. The future native background/narrative choice chain belongs here, using native `SelectedOptions` and final effect handlers. Do not claim profile-only options are live effects.
4. **Land & Home** (`Location`): starting fief policy, exact fief ID, explicit ownership-transfer permission, family home, starting scenario, scenario factions, and advanced world settings. The installed native ASO options must validate role, kingdom and scenario. A specific family home must already be a safe owned town/castle. Never activate infants or move them as troops.
5. **Review Your House** (`Review`): comprehensive read-only preview of role, culture, character, origin bonuses, actual clan/banner, spouse and exact children, resources, fief, family home, world, and native difficulty. Invalid/unavailable requests must be visibly identified. There is exactly **one** user-facing final action. In v0.31 it is truthfully labelled `NEXT: NATIVE CREATOR`. Rename it to **Begin Campaign** only **after** a native-state compositor proves that all native choices and the real difficulty screen are embedded within CU, and that this action enters the map without a second wizard.

## Frozen navigation and persistence

- **Entry:** `SandBoxNewGame` replaces the old Default/Advanced selector with the CU screen. Maintain the original native click as an explicit opt-out/recovery path; do not create another Sandbox menu entry.
- **Page ownership:** all five tabs can be selected directly. The current Back/Continue buttons traverse individual visible fields, then the previous/next page in the fixed order. This interaction can be refined for clarity without changing tab order or dropping fields.
- **Quick/Advanced:** More options only expands optional settings. It must never force an experimental face stage on. Experimental native 3D review and face workshop stay off by default and cannot both be selected.
- **Edits:** a confirmed field change must survive tab switching and review. Numeric and child-count choices must write committed values, not only a temporary slider readout. Review summarizes the saved draft.
- **Fresh campaign:** opening the CU creator resets transient approvals, requested child count and unsafe fief grants. Do not silently copy the previous game's spouse/children authorization.
- **Exit/Escape:** exit returns to the main menu, disables the active CU profile and queues no new campaign. No native screen, Hero creation or resource grant occurs as a side effect.
- **Validation:** final acceptance refuses unsupported siblings, missing spouse for children, inconsistent family home/fief grants, unavailable native start types or kingdoms, incomplete scenario variants and conflicting experimental options. On failure, remain in the CU creator and identify the correction.
- **Single launch:** accepting a valid configuration queues one start. Native ASO creates actual kingdom/role/fief state. CU phase-9 grants/family run once after native phase 8, with one-shot guard, then verify observed game state and persistence.

## Where the existing route ends and the desired route starts

**Current v0.31 behavior, intentionally not changed by this lock:**

`Sandbox → Starting Life → Bloodline → Fortune → Land & Home → Review → NEXT: NATIVE CREATOR → native pages → map`

**Future required behavior, after native-state compositor passes real playtests:**

`Sandbox → persistent CU five-tab shell inside CharacterCreationState → Begin Campaign → map`

Native controllers still own culture, body generation, `CharacterCreationManager`, narrative effects, banner, clan naming, review, difficulty and finalization. Their *visible UI* must be integrated into the five-tab CU shell without duplicate views or skipped effects. A main-menu-only Gauntlet overlay cannot create a live native player character or scene by itself.

## Acceptance and no-regression checks

**Contract gate (this branch):** `scripts/test_locked_sandbox_flow.py` verifies five pages in order, five distinct XML tab buttons and page bindings, no duplicate native FaceGen stage override, disabled unsafe options, one validated launch, review-only confirmation and Escape cancellation. This is source inspection, not proof of visual behavior.

**Runtime gate (later branch):** use a disposable game and confirm all five tabs, readable controls at 1366x768 and 1920x1080, role and spouse persistence between pages, exact one-child count, correct actual Lord/King/fief state, proper starting wealth, successful face edits and narrative bonuses, native banner/clan/difficulty values, save/quit/reload, and no second native wizard. Confirm the map appears directly after the single CU final action.

**Release rule:** keep v0.31's working dual-creator route available until the actual single-screen build completes runtime acceptance. Do not remove or hide native stages merely to make the second UI disappear. Do not claim this flow-lock branch is a completed one-screen build.
