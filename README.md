# Calradia Unified v0.16.0: Expanded Sandbox Origin Designer

**Target:** Bannerlord PC v1.5.7, one-folder standalone module, no historical DLLs. **Gate B COMPILED_UNTESTED:** [Windows CI #44256086670](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44256086670) passed at commit `04e0da222859c3c7a8b1e58e0cb9be88d66a38fe`, 552 independent policy checks, 40 simulated campaigns (14,600 days), zero compiler warnings/errors, XML and runtime ZIP integrity validation. **No real Bannerlord game test.** The official game's Advanced Starting Options remain authoritative for King, Vassal, Mercenary and other kingdom standing.

## New Sandbox Origin Designer

From an existing town or castle, use **CU: Design my NEXT Sandbox starting life**. Choose a role (Wanderer, Soldier, Knight, Lord or King), set a next-game gold/influence budget, prepare a spouse and child template count, and select a desired starting town from actual settlements. No existing campaign is modified.

The profile is saved to `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`. It is **disabled by default** and can be edited for an entirely new Sandbox launch.

Only on the **OnNewGameCreated** event, when the profile is enabled, the module currently attempts the native resource subset: exact player gold, clan influence where the player has a native kingdom, a bounded count of native-culture basic troops, player skill/attribute/focus/trait overrides, and up to twelve exact item IDs in the party inventory. Skill, focus and attribute effects use the game's progression APIs and must be verified in an actual v1.5.7 campaign. A save key stops repeat application. Roles do not forge rulership. **The spouse, children, siblings, facial appearance, named equipment kits, player age and starting-town relocation are profile-only in this first candidate.** No new relative hero is spawned and no unsafe birth, marriage or map move runs. Full start customization is an ongoing work item, not a completed feature.

All requested profile fields are represented: player age, attributes, 18 skill levels, focus, five personality traits, exact item IDs, equipment kit, chosen settlement ID, spouse name/age/gender/culture/face seed and equipment, and up to six individually named/aged children and four siblings.

The built-in native menu currently edits common choices. Names, ages and appearance seeds for a spouse and six children can be edited through native text dialogs. The same editor offers all six attributes, eighteen skills and focus levels, five personality traits, and twelve item IDs. The file remains available for extended data. An in-game text and appearance editor is planned after target-version UI acceptance.

## Evidence

- [Sandbox profile format, valid relationships and the staged feature matrix](docs/V016_SANDBOX_ORIGINS.md)
- [Game-only startup and regression acceptance](docs/V015_RUNTIME_ACCEPTANCE.md)
- [Existing full 64-setting native controls](docs/V015_NATIVE_SETTINGS_CAPABILITIES.md)
- [Original 78-mod development priorities](Data/Reconstruction_Priority_78.csv)

**No in-game launch/save test has passed.** A hosted Windows C# build is not a runtime claim.

## Pre-game creator entry point (experimental)

An **experimental** `CU: Design Next Sandbox Character` button is registered on the game's initial main menu. It opens the module's native Gauntlet creator. Edit role, spouse templates, six children, siblings, attributes, skills and focus, traits, equipment IDs and starting place **before** launching a new Sandbox. The creator saves the same separate origin profile, not the active game. The in-campaign origin editor remains a fallback. The panel has passed no actual-game rendering or controller tests.

Starting families support a single parent with children, a spouse and children, a spouse without children, no family, or siblings only. Family creation remains profile data until an actual v1.5.7 save-persistent hero creation mechanism has passed in-game tests.

**Compiled v0.16 module:** [CalradiaUnified-v0.16.0-compiled-untested](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44256086670/artifacts/14406421335). DLL SHA-256 `c4844140bf9be56a6caf73610dfeebd128bf9a6dd04fa4f446ef4203f3c005c9`. The release stage remains Gate B until real game startup, profile application, family and UI behavior, and save/load evidence are recorded.

## Not showing in the Bannerlord launcher?

The GitHub Actions download contains a ZIP file inside another ZIP. Extract the outer artifact, then extract `CalradiaUnified_v0.16.0_COMPILED_UNTESTED.zip` into the actual installed Bannerlord game root. The final manifest must be `Modules/CalradiaUnified/SubModule.xml`. The project now includes a checked installer and clear manual steps in [launcher troubleshooting](docs/INSTALLATION_TROUBLESHOOTING.md). Check the Singleplayer > Mods tab after fully restarting the launcher. This addresses file placement, not game runtime testing.

## Module recognized, but DLL cannot load?

The launcher has found the manifest. Verify the DLL is present and unblocked, then compare your installed Native version with the compiled v1.5.7 target. Run scripts/Diagnose-CU-LoadFailure.ps1 against the actual game directory to inspect the exact installed CLR DLL, hash and game engine version. No other framework DLLs are declared. The v0.16 runtime preflight version check is now corrected.
