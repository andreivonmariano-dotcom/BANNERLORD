# Calradia Unified v0.18 | Overhauled Sandbox front-end

**Target:** Bannerlord PC v1.5.7.125802. **Status:** Gate B COMPILED_UNTESTED. [Windows CI #44278537058](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44278537058) passed 570 independent policy checks, the static layout/escape audit, v1.5.7 reference compilation with zero errors and warnings, and runtime ZIP checks. New Gauntlet rendering and mouse/controller input still require an actual in-game test.

## New flow

1. Click the **normal Sandbox** button in the Bannerlord main menu.
2. The game creates its real Sandbox `CharacterCreationState`. The mod detects this state and opens **CUSandboxCreator**, a fully separate full-screen Gauntlet wizard, on top.
3. Configure seven pages: **Identity**, **Household**, **Progression**, **Equipment**, **Resources**, **Location**, and **Review**. Use the large side navigation and Previous/Next Field controls.
4. At Review, select **Apply / continue**. This commits an opt-in `sandbox-origin.ini` profile and closes the custom screen. The **native game screen resumes for its appearance, naming and finalization work**.
5. Press **Escape** or select **Escape / native** to dismiss the custom wizard without activating an origin. This is the explicit recovery route.
6. The game does not have a second CU menu entry.

This release begins the redesign immediately after selecting Sandbox. It does **not** yet replace Bannerlord's complete face-generation/game-finalization internals. Its culture preference, equipment kit, starting-town choice and Admiral role are profile choices. They do not guarantee an actual culture switch, mounted kit, physical relocation or naval rank. The previously requested full independent ending sequence remains future development.

## Safety and bug prevention

- No native age-menu links are rewritten, eliminating the v0.17 post-age dependency.
- Both `RealFamilyEnabled` and `RoleAffiliationsEnabled` are reset to false on entry and must be explicitly re-selected on Review.
- `OverhauledCreatorEnabled=true` by default. In `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`, set `OverhauledCreatorEnabled=false` to return to the untouched native wizard.
- A physical Escape-key handler calls the screen's Close path. The UI has a separate, wide Return to native button, and all button-label TextWidgets fill their parent widths. CI checks this exact layout structure to stop the vertical text bug from recurring.
- Origin effects are applied once on `OnNewGameCreatedEvent`, after game hero and clan availability. Cancelling the new front-end forbids applying an active profile in the same session.
- The existing v0.17 optional family hero creation and kingdom-affiliation routines are still **experimental and save/load untested**.

## Test on the installed game

1. Replace the old module folder, unblock the new DLL, and use a disposable new Sandbox with official modules only.
2. Confirm the **normal Sandbox button** opens the CU screen as the first visible character-creation page. Record the top header and the seven left-side categories.
3. Inspect buttons at 1920x1080 and your usual resolution. No letters should stack vertically. All text must be readable.
4. Try Escape immediately, then start another Sandbox. No second overlay appears in one creation state, and native creation is accessible after Escape.
5. Select Wanderer, spouse/children names, one attribute, an equipment theme, 12,000 gold, and starting town ID. Reopen each category and confirm values persist. Exit and inspect the user profile.
6. Select Review > Apply / continue. Verify the native character-creation screen appears under the overlay and remains navigable through appearance, clan name, review and start.
7. Start the new disposable campaign. Confirm only validated profile effects are applied once and are unchanged after save/load.
8. Keep the dangerous family and political switches false for the first complete pass. Test them in separate campaigns only after the UI and save/reload baseline works.

If the screen crashes or blocks exit, disable by writing:

```ini
OverhauledCreatorEnabled=false
Enabled=false
NativeStageEnabled=false
RealFamilyEnabled=false
RoleAffiliationsEnabled=false
```

to the user profile file. The original native Sandbox flow then runs without this custom screen.

### Evidence boundaries

A 0-error Windows reference build proves C# compilation. XML binding checks prove names and dimensions match the source. Neither proves the runtime screen shows at the right moment, draws usable fonts, accepts mouse input, or survives save/load. These are Gate C–E requirements.
