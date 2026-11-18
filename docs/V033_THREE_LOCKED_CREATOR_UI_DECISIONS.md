# v0.33: Three UX decisions for the locked Calradia Unified Sandbox creator

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Flow baseline:** [v0.32 five-page contract](V032_LOCKED_SANDBOX_CREATOR_FLOW.md). **Status:** v0.33 improves truthful unavailable controls; persistent real 3D preview is a design requirement, not implemented here.

## Decision 1: Freeze five chapters, improve the contents, then integrate the engine

The order is **Starting Life → Bloodline → Fortune → Land & Home → Review Your House**. Maintain the existing internal page keys `Identity, Household, Resources, Location, Review`. Do not add an extra top-level native appearance wizard or native-only difficulty page. Direct tab access, Previous/Continue field navigation, preserved draft values, and one final confirmation stay in place.

A page's native functions can become subpanels *inside* the same CU shell. Engine integration should not wait for every decorative flourish. First stabilize selection, active/disabled states, keyboard focus, data persistence and clear invalid-request messages. Keep the current functional v0.31 dual-screen route as recovery until the new compositor passes live-game acceptance.

**Acceptance:** five tabs and all existing real starting role/kingdom, spouse/children, resources, fiefs and world options survive. `NativeFaceWorkshopEnabled` and `NativeIntegrationReviewEnabled` remain opt-in, off by default and mutually exclusive.

## Decision 2: Persistent **real** 3D player model on every CU chapter

**Yes, in the final native-state editor.** The right-side context region becomes a persistent player 3D viewport. It should remain visible during Starting Life, Bloodline, Fortune, Land & Home and Review. The active chapter can show different UI and camera framing without creating a second player Hero or reopening a second wizard.

On Starting Life, the user edits the real face, body, sex/gender and native culture, and sees a live result. On Bloodline, the **player model** stays live, but spouse/children remain text plans until real Heroes exist. On Fortune, the model shows the actual character/equipment state after legitimate native narrative choices. On Land & Home, the same model stays available next to the fief/world summary. On Review, the model and summary remain visible before final confirmation. Face controls are only active in the appropriate appearance subpanel, not over every screen.

**Native ownership:** the actual scene, `BodyGeneratorView`, character body properties, `FaceGenHistory` and native stage callbacks stay engine-owned. One CU compositor must coordinate scene, camera, stage transitions and input focus. The current **pre-campaign** `CUSandboxCreator` screen cannot show an actual newly-created player Agent yet, so v0.33 does **not** add a fake portrait, duplicate face renderer or placeholder 3D scene. The persistent viewport is a future requirement for a shell hosted in `CharacterCreationState`.

At 1920×1080, provide the live viewport enough space to rotate the full character, keeping active controls in a separate unobscured region. At 1366×768, the viewport may be smaller, but must remain usable and the editor controls must not overlap it. The right-side household/world text summary can collapse to a compact strip while the 3D dock is present. Preserve controller and keyboard navigation.

## Decision 3: Hide irrelevant inputs, show unavailable requested features as read-only

There are three distinct states:

- **Relevant and supported:** show an editable control and persist its committed value. Example: spouse name, age and child count after `Begin married = Yes`. Hide spouse-specific inputs when not married, and show only the requested number of children.
- **Relevant but unsupported:** show a visible **NOT AVAILABLE IN THIS BUILD** status and a short reason, **without an editable control**. The v0.33 pregame creator marks `PlayerAge` (actual age is native), `SiblingCount` (native parent Heroes are missing), `EquipmentKit` (equipment themes are not applied) and `StartingTown` (relocation is not active) read-only under More options. Hide the individual sibling detail editor until actual native sibling creation works.
- **Experimental, genuinely implemented as a prototype:** show under **More options**, off by default, label its scope. The native CU Face Workshop is a separate experimental stage, not evidence of a one-screen creator.

A requested feature should never look fully editable only to be rejected at the final confirmation. When the native-state composer adds age, culture, appearance and genuine birth family links, replace the corresponding read-only explanation with real native-bound controls. Do not delete the user's planned feature from the contract.

## Code changes in this branch

- `Source/CalradiaUnified/SandboxCreatorOverlay.cs`: use `Field.Kind="unavailable"`, expose `IsUnavailable`, display a truthful selected value and reason, and never show value-edit buttons for those fields.
- `Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml`: show the read-only unavailable indicator in the center pane without adding a button or input capture.
- `Source/CalradiaUnified/SandboxOriginProfile.cs`: clear old unsupported age/starting-town/equipment-theme requests on every new Sandbox, alongside child/sibling consent resets.
- `scripts/test_cu_creator_unavailable_fields.py`: block regression to editable unsupported options or a fake 3D viewport.

## What is not delivered by v0.33

This build **still uses the separate native character creation after CU review**, intentionally. It is an interface honesty and flow-stability candidate only. Face, culture, narrative bonuses, banner, clan and difficulty are not yet embedded in one CU screen. A successful CI compile or a valid ZIP does not verify live 3D scenes, family creation or save/reload.

**Next native compositor acceptance:** click Sandbox once, see the same CU shell for all five pages, edit a genuine live player 3D character, choose real native culture and narrative consequences, set the true banner/clan/difficulty, accept once, arrive directly at the map with real Advanced Start role/fief and correct family Heroes. Save, exit, reload and verify every result. Keep the old working mode as rollback until the user verifies these in-game.
