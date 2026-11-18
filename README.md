# Calradia Unified v0.16.0: Expanded Sandbox Origin Designer

**Target:** Bannerlord PC v1.5.7, one-folder standalone module, no historical DLLs. **Gate B pending on this branch.** The official game's Advanced Starting Options remain authoritative for King, Vassal, Mercenary and other kingdom standing.

## New Sandbox Origin Designer

From an existing town or castle, use **CU: Design my NEXT Sandbox starting life**. Choose a role (Wanderer, Soldier, Knight, Lord or King), set a next-game gold/influence budget, prepare a spouse and child template count, and select a desired starting town from actual settlements. No existing campaign is modified.

The profile is saved to `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`. It is **disabled by default** and can be edited for an entirely new Sandbox launch.

Only on the **OnNewGameCreated** event, when the profile is enabled, the module currently attempts the safe resource subset: exact player gold, clan influence where the player has a native kingdom, and a bounded count of native-culture basic troops. A save key stops repeat application. Roles do not forge rulership. **The spouse, children, siblings, face models, stat overrides, equipment kits and custom relocation are schema/profile-only in this first candidate.** No new relative hero is spawned and no unsafe birth, marriage or map move runs. Full start customization is an ongoing work item, not a completed feature.

All requested profile fields are represented: player age, attributes, 18 skill levels, focus, five personality traits, exact item IDs, equipment kit, chosen settlement ID, spouse name/age/gender/culture/face seed and equipment, and up to six individually named/aged children and four siblings.

The built-in native menu currently edits common choices. Detailed names, appearance seeds, attributes, skills, traits and equipment IDs are editable in the saved profile file. An in-game text and appearance editor is planned after target-version UI acceptance.

## Evidence

- [Sandbox profile format, valid relationships and the staged feature matrix](docs/V016_SANDBOX_ORIGINS.md)
- [Game-only startup and regression acceptance](docs/V015_RUNTIME_ACCEPTANCE.md)
- [Existing full 64-setting native controls](docs/V015_NATIVE_SETTINGS_CAPABILITIES.md)
- [Original 78-mod development priorities](Data/Reconstruction_Priority_78.csv)

**No in-game launch/save test has passed.** A hosted Windows C# build is not a runtime claim.
