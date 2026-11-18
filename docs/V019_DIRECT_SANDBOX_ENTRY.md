# Calradia Unified v0.19 | Direct Sandbox entry

**Target:** Bannerlord PC v1.5.7.125802. **Status:** Gate B **COMPILED_UNTESTED**. [Windows CI #44289665260](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44289665260) verified 570 C# policy checks, 40 modeled campaigns, 0 compiler warnings/errors, runtime ZIP checks and installer simulation. Live screen and game testing remain open. This change follows the confirmed v0.18 user screenshot and diagnostic events, tracked in [issue #41](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/41).

## Player-visible contract

1. In the main menu, click **Sandbox** once.
2. Calradia Unified opens its own pre-game creator directly. The game's intervening **Default Start / Advanced Start** selector must not appear.
3. Choose a starting role and optional profile data across Identity, Household, Progression, Equipment, Resources, Location and Review.
4. Select **Apply / continue**. The mod commits the pre-game origin profile, closes its editor and asks the game's own `SandBoxGameManager` to create a **default scenario** campaign. The former Default/Advanced picker stays out of this route.
5. Bannerlord still owns its intro video, face/culture creator, naming and finalization screens. These are *not yet replaced*. The word "complete" does not apply to the entire custom character-creation pipeline until those stages are implemented and game-tested.
6. Escape or **Escape / native** before confirmation returns to the **main menu without starting a campaign**. Clicking Sandbox again opens a new custom screen. The opt-out `OverhauledCreatorEnabled=false` restores the original main-menu Sandbox action and its built-in selection screen.

The existing `Role=King` and `Role=Admiral` profile labels do not create royal rank or a naval command. Native ASO scenarios other than Default are not silently selected. Spouse and clan actions remain disabled unless explicitly opted in, and remain unsafe for existing saves.

## Engine integration

- `SubModule.OnBeforeInitialModuleScreenSetAsRoot` calls `SandboxMainMenuEntry.Install()`.
- `Module.CurrentModule.GetInitialStateOptionWithId("SandBoxNewGame")` finds the official menu item; `OverrideInitialStateOption` replaces its click handler **under the same ID**, preserving the original label, order, disabled predicate and hidden predicate.
- The original `DoAction` is saved for recovery. Repeated hook calls detect the custom click action and do not wrap it twice.
- `SandboxCreatorOverlay.OpenFromMainMenu` creates a `SandboxCreatorScreen` before there is a `Game.Current`; the ViewModel saves profile choices.
- `Apply` queues the launch until after the custom screen is popped. `OnApplicationTick` then runs the same default-options start sequence as the game's ASO Confirm action:
  `AdvancedStartOptionsManager.CreateCampaignStartOptions()`, `GetChangedOptions()`, `MBGameManager.StartNewGame(new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign, ...)))`.
- The `OnCharacterCreationInitializedEvent` observer remains **read-only** and must not reopen the custom pre-game editor inside native character creation.
- `CUSandboxCreator.xml` now requests a background frame and native ButtonBrush on every clickable region. In-game brush resolution and mouse hit testing are **not** established by XML parsing.

No Harmony dependency, patched engine assembly or copied third-party mod DLL is used.

## Acceptance tests on the actual installed game

Use a disposable new Sandbox and **Native + SandBoxCore + Sandbox + Calradia Unified only**.

1. Start with the creator opt-out false (the profile default is true). Click Sandbox. **Expected:** the CU editor is the next screen. No Default/Advanced selector and no initial cinematic prior to the editor.
2. Inspect at 1920x1080 and at your regular resolution. **Expected:** visible button surfaces, panels, readable text, no overlapping controls, pointer hits.
3. Click the seven left-side sections, change starting role with Previous/Next Choice, change a number and save it. **Expected:** state changes, input works, and `sandbox_creator_page_clicked` and `sandbox_overhaul_field_saved` events reflect commands.
4. Escape. **Expected:** return directly to main menu, not to the culture picker. Click Sandbox again. **Expected:** one editor, not duplicate layers.
5. Apply a conservative profile. **Expected:** no Default/Advanced selector; the game proceeds through its own intro/culture/appearance/character-finalization stages, and starts a disposable Sandbox.
6. Save, fully exit, reload; verify gold, recruits, attributes and opt-in effects were not duplicated.
7. Set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`, restart. **Expected:** click Sandbox and see the untouched native Default/Advanced choice.
8. Record `sandbox_main_menu_overridden`, `sandbox_creator_screen_requested`, `sandbox_creator_pre_game_accepted`, `sandbox_direct_game_start_requested` events or relevant exceptions. A request event alone is not successful rendering proof.

## Known limitations and next work

- **Current deliverable goal:** direct pre-game entry and no built-in starting-options selector.
- **Not delivered:** a fully independent culture/face editor and finalization process; non-default ASO scenarios; fully implemented Admiral/King transitions; established mouse/brush and controller behavior in Bannerlord v1.5.7.
- If the custom UI fails, restore the original Sandbox start via the saved profile flag. The user-proven v0.18 overlay had floating text, an unusable interface and working Escape to the native culture picker. Do not label any v0.19 outcome game-tested until an observed session establishes it.

## Exact compiled artifact

- [GitHub Actions compiled-untested ZIP artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44289665260/artifacts/14420293511), a GitHub ZIP containing one runtime module ZIP.
- Compiled DLL size: 174080 bytes. DLL SHA-256: `d351ebd48553bd86a8e456c14ad4202fdbf0fc96d13715b8be9bfbaa2b8070f0`.
- Source review: [draft PR #42](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/42). The actual in-game test must use a file with the exact DLL hash above.
