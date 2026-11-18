# Native framework capabilities and settings UI for v0.14

**Target:** Bannerlord PC v1.5.7. **Candidate status:** Gate B COMPILED_UNTESTED, verified by [Windows workflow 44249156647](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44249156647) at commit `3606ba5faa2c69ba7065624d40002d787f74e970`. It passed 217 policy checks, 40 deterministic modeled campaigns, 78-module coverage, v1.5.7 reference compilation, and runtime ZIP checks with 0 compiler errors/warnings. Game runtime and save/reload unverified.

## Why no framework DLLs

The historical 78-module inventory includes Harmony, ButterLib, UIExtenderEx, and MCM (Mod Configuration Menu). Calradia Unified implements the **narrow functions currently required by its own features**, not all APIs or assets of those frameworks. It does not load their DLLs, implement their public extension contracts, claim that dependent external mods work, or ship a third-party binary.

| Framework | Present capability | Not implemented |
| --- | --- | --- |
| Harmony | Original native campaign events and bounded commands | Arbitrary runtime patching, prefixes/postfixes, transpilers |
| ButterLib | Local diagnostic journal, managed exception capture, portable finance ZIP exporter | ButterLib public APIs, full diagnostics services or exception overlay |
| UIExtenderEx | Native Bannerlord campaign menus and menu entry points | Gauntlet interface interception, prefab injection or external extension API |
| MCM | Native in-game menu, configurable values and safe profile persistence | MCM-compatible API, sliders, GUI tab widgets, mod interoperability |

## Where to open the settings menu

From the **town** or **castle** game menu, choose **CU: Configure Calradia Unified**. Select Reports and Warnings, Economy and Recruitment, or Castle Management. Disabled options present an Enable action; enabled options present a Disable action. The selected setting is changed in the one shared `Settings` instance used by the campaign behaviors.

The supply warning threshold supports 0–15 days. The local recruitment daily cap supports 1–20 recruits. These numeric settings change by one per click.

## Persistence and precedence

1. `Modules/CalradiaUnified/ModuleData/calradia-config.ini` supplies the shipped defaults and original advanced options.
2. `%LOCALAPPDATA%/CalradiaUnified/calradia-user-config.ini` stores only user overrides. Values in this file take precedence on next campaign creation/load.
3. UI save writes a temporary file in the local settings directory and commits it by replace/rename. The memory value updates only after disk success. A failed write does not change the current setting.
4. A user can remove the local override file to return to the bundled defaults. No campaign save data keys were changed.

**Safety:** native menus expose the audited catalog of all 64 existing public settings with strict type and range constraints. Autonomous NPC movement and physical convoy toggles are marked experimental with a detail page before activation. Experimental economy, recruitment and castle actions remain disabled until the user explicitly turns one on.

## Verification still required

1. Verify v1.5.7 launcher loads the single module and the new menu opens from both town and castle.
2. Exercise menu entry, submenus and return paths, including changed option visibility.
3. Toggle a report option in-game, confirm it changes activity without relaunch, save and restart, and verify the user override wins.
4. Confirm missing, malformed and read-only profile file behavior. The mod must remain loadable and report write errors without activating a failed option.
5. Exercise experimental controls only in a fresh test campaign. Save/reload, siege, ownership and mission exit remain unverified.
