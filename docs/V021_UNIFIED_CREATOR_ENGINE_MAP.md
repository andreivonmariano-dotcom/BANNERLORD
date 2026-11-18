# v0.21 engine map | One Calradia Unified Sandbox creator

**Source of truth:** Actual gameplay must be verified on the user's installed Bannerlord PC v1.5.7 module manifests. The linked decompiled API sources are a working guide, **not proof** that every member exists in the installed binary. Never modify original Bannerlord DLLs. This document separates confirmed code points from engine behavior not yet tested on the player's machine.

**User requirement:** Clicking **Sandbox** opens one attractive CU character creator, with real 3D appearance, culture, clan/kingdom/fief, family, world scenarios and a genuine final Start action. No Default Start / Advanced Start screen and no second vanilla culture/appearance/narrative wizard after Apply.

## Three answers and one implementation path

### Q1: Native 3D face and body editor inside CU

The key rendering machinery belongs to the **campaign-created CharacterCreationState**. The pregame main menu has no Hero.MainHero, character state or player-agent scene yet.

Reference engine sources:
- [SandBoxGameManager](https://github.com/Sartiye/Bannerlord-Taleworlds-Assembly-Decompiled-Archive/blob/main/SandBox/SandBox/SandBoxGameManager.cs) creates a `VideoPlaybackState` and then `CharacterCreationState` through `LaunchSandboxCharacterCreation`. A custom entry action can bypass the Default/Advanced picker, but changing `SandBoxNewGame` alone **cannot** suppress the game's later wizard.
- [CharacterCreationScreen](https://github.com/Sartiye/Bannerlord-Taleworlds-Assembly-Decompiled-Archive/blob/main/SandBox.View/SandBox.View.CharacterCreation/CharacterCreationScreen.cs) constructs `character_menu_new` as a TaleWorlds Scene, creates an agent renderer controller and resolves stage views through `CharacterCreationStageViewAttribute`. Its `ICharacterCreationStateHandler` receives stage-creation and refresh events.
- [CharacterCreationFaceGeneratorView](https://github.com/Sartiye/Bannerlord-Taleworlds-Assembly-Decompiled-Archive/blob/main/SandBox.GauntletUI/SandBox.GauntletUI.CharacterCreation/CharacterCreationFaceGeneratorView.cs) creates `BodyGeneratorView`, tied to `CharacterObject.PlayerCharacter`, game equipment, face history and the native scene/scene layer.
- [CharacterCreationManager](https://github.com/Sartiye/Bannerlord-Taleworlds-Assembly-Decompiled-Archive/blob/main/TaleWorlds.CampaignSystem/TaleWorlds.CampaignSystem.CharacterCreationContent/CharacterCreationManager.cs) owns the stage list, current stage, face history, culture and narrative selections, and `NextStage`/finalization.

**Design rule:** The CU editor must carry a pregame *plan* into this state, then host or wrap the actual native BodyGenerator scene + facial-edit ViewModel with CU frame and navigation controls. A static portrait or a separate Gauntlet text panel does not satisfy the live 3D requirement.

**Integration sequence:**
1. Keep the direct main-menu `SandBoxNewGame` override from v0.19.
2. Store a **pending new-game profile** in memory with a unique run identifier; persist an immutable snapshot only for crash recovery. The on-disk editable preferences are not authoritative once the campaign has started.
3. Launch the native `SandBoxGameManager` using real Advanced Start data. Avoid the intro video only after confirming a version-pinned supported state hook, not by calling a private method guessed from external source.
4. When the real `CharacterCreationState` initializes, install the CU stage/screen bridge after Bannerlord registers its default stages. Reuse `BodyGeneratorView` or its native face stage, not a handwritten model renderer.
5. Bind culture, name, face/body key and age to native `CharacterCreationContent` and `CharacterObject.PlayerCharacter`. The native culture stage currently calls `SetSelectedCulture` and sets the selected player's culture and body properties. A CU replacement must preserve those effects before the face editor opens.
6. Confirm the preview's full lifecycle: native Scene, SceneLayer, GauntletLayer, agent renderer, camera, input focus, hotkeys, cleanup on cancel, and no dangling scene after a new game or exit.

**Not yet built:** A live CU-owned BodyGenerator renderer, native camera control bindings, and a safe way to suppress default stage views. The v0.21 `sandbox_appearance_engine_probe` only records the exposed stages and whether the native face view type is loaded. It changes no stage.

### Q2: Genuine Lord, King, fief, spouse and children

Prefer native `AdvancedStartOptions` over forging a kingdom member/rank after a default start:
- CU `Lord` / `Knight` => `StartType=vassal`
- CU `King` => `StartType=king`
- CU `Soldier` => `StartType=mercenary`
- CU `Wanderer` => `StartType=default`
- CU `Admiral` => `StartType=fleetadmiral` **only** with a provider that actually offers it.
- Native `KingdomId` must be a real start-option item. Never silently substitute a culture ID, or start in a random kingdom.

The engine's `CampaignAdvancedStartingPlayerOptionsCampaignBehavior` applies the selected real King/Vassal/Mercenary start at `OnCharacterCreationIsOverEvent` index **8**. The CU origin behavior runs its additional profile effects at index **9**. An early OnNewGameCreated call is insufficient for observed status.

**Starting fief is not implemented yet.** Native vassal/king starting logic selects from candidate settlements, not a guaranteed user-picked fief. A new fief picker must:
- Show only world-existing settlements with clear kingdom, castle/town status, ownership and compatibility.
- Validate the requested fief after the world scenario has been applied, immediately before transfer.
- Use the v1.5.7 native settlement ownership action and verify the resulting owner, kingdom and clan fief list. Do not assign `Settlement.OwnerClan` directly or silently take an unrelated kingdom's town.
- Record whether a native-start grant was displaced, whether the desired fief was acquired, and recover or refuse on failure. Real multi-object rollback needs its own verified design.

**Family:** Requests must produce real Hero objects and matching genealogical references. Use native HeroCreator + MarriageAction where supported, with age/gender/parent links and clan membership verified, not a decorative roster string. The current v0.20 `SandboxFamilyCreator` may return an explicit failure or partial success. It cannot guarantee rollback. Fresh-start siblings need actual parent heroes, not invented references.

**Acceptance gate introduced in v0.21:** `SandboxStartContract.Validate` inspects the resulting real native type, selected kingdom, ruling/vassal status, fief count, spouse, and children. The real campaign behavior writes `sandbox_start_contract result=PASS/FAIL`. If a user selected Lord and the game made a default Wanderer, the contract reports `native_start_type_mismatch`, not a successful creator. This acceptance logic does not itself grant or alter gameplay.

### Q3: One Bannerlord-style creator, not seven disconnected text menus

**Target layout for 1920x1080, scalable to 1366x768 and ultrawide:**
- Top band: CU Sandbox, progress, active culture/banner and Back/Reset/Exit.
- Left ~18%: numbered pages **Identity**, **Appearance**, **Household**, **Starting Life**, **Kingdom & Fief**, **Resources & Skills**, **World Settings**, **Review**. Disable Finish until all required choices are valid.
- Center ~42%: **live 3D actor** and native camera for orbit/zoom, face/body randomize, culture-dependent equipment and banner.
- Right ~40%: contextual controls, titles with tooltips and real component bounds, sliders, dropdowns and preview of actual bonuses/risks.
- Bottom: warning/validation summary, previous/next page, one **Begin Your Life** action. Escape/cancel returns to the main menu without creating a campaign. No click region can be visually hidden.

Use the installed game's native Gauntlet assets and sprite categories (for example `ui_charactercreation`), font styles, input hotkeys and brushes **only after their actual names and availability are verified**. Placeholder `Frame9`/`ButtonBrush` names and XML syntax tests prove neither visual fidelity nor click behavior. A screenshot, controller test and screen reader focus test must be used.

**One-screen architecture:** pregame UI displays the choices, then a CU-controlled player-character-state editor takes control in the campaign, preserving the same visual shell and profile state. To deliver *literally* one uninterrupted screen, the transition must hide/replace the pregame shell only after the native character scene is ready, with no duplicate native wizard, and hand face/body changes to the actual engine. There is no safe direct BodyGenerator scene in the vanilla main-menu state.

## Gate discipline and status

1. **Gate A:** exact installed v1.5.7 API signatures/scene assets, no unverified assumptions.
2. **Gate B:** CI reference compilation, static command/property tests and pure start-contract policy tests.
3. **Gate C:** disposable game confirms one CU shell plus real preview and live face edits, no residual culture/background wizard, and clean Escape.
4. **Gate D:** Lord/King and world scenarios produce actual kingdom/fief status. Real spouse/children with correct parent and clan links, no partial silent outcomes.
5. **Gate E:** save, exit, reload, confirm body properties, title, fief, spouse, children and resources; no duplicated gifts or relatives.
6. **Gate F:** UI polish: 1080p, 768p, ultrawide, mouse/keyboard/controller, readable brushes, working focus/scroll and localization.
7. **Gate G:** versioned release with exact GitHub build SHA, installed DLL hash, explicit launch evidence and reproducible failures.

**v0.21 source branch is not the completed total conversion.** It introduces actual-start acceptance checks and an engine-surface probe. Do not install/recommend it as a finished one-screen creator. v0.20 Lord/family still need the first real gameplay proof.

## File ownership map

| Function | Source |
|---|---|
| Replace main menu click | `SandboxMainMenuEntry.cs` |
| Save choices before game starts | `SandboxOriginProfile.cs` and `SandboxCreatorOverlay.cs` |
| Native ASO role/kingdom/scenario values | `SandboxNativeStartOptions.cs` |
| Run origin additions after native start | `SandboxOriginBehavior.cs` |
| Real spouse/child attempt | `SandboxFamilyCreator.cs` |
| **Check actual result, never label it success by assumption** | `SandboxStartContract.cs` and `sandbox_start_contract` event |
| Native face scene availability probe | `NativeSandboxStageObserver` in `SandboxOriginBehavior.cs` |
| **Future stage/screen bridge** | *not implemented* |
| **Future fief picker and transfer** | *not implemented* |
| Native-looking Gauntlet visual editor | `Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml`, *incomplete* |

The next code task after v0.21's read-only probe is a **single native face stage prototype**, not another large decorative wizard. Gate it behind an explicit experimental option and test in the user's game before suppressing the default character-creation stages.
