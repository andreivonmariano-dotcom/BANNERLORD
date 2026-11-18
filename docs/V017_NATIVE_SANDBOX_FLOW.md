# v0.16.1 Native Sandbox Flow Correction

## User-reported issue

The user tested v0.16.0 in Bannerlord and confirmed the extra **CU: Design Next Sandbox Character** main-menu option opens a faulty Gauntlet screen with stacked vertical labels and inaccessible controls. The chosen entry point was wrong for the product requirement.

The goal is **Main Menu -> Sandbox -> existing native character creation -> embedded extra choices -> native Continue/Back -> campaign**, not a second editor launched from the main menu.

## Immediate correction shipped in v0.16.1

1. Remove the `InitialStateOption` main-menu entry and its launcher-to-Gauntlet handler.
2. Exclude the broken `CUOriginPanel.xml` prefab from the runtime ZIP. The old C# prototype remains source-only for reference, not linked to a user-facing action.
3. Preserve native Sandbox stages and user inputs. Do not replace the game's main menu, face generator, clan naming, age/review, or Advanced Starting Options.
4. Observe `OnCharacterCreationInitializedEvent` and record actual `NarrativeMenus` IDs, `InputMenuId`, `OutputMenuId`, and current stage type to `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv` (if diagnostics are enabled).

## Native-stage integration target

| Current native functionality | Requested additions |
| --- | --- |
| Culture and appearance | Keep native face generator; separate optional family appearances later |
| Parents and early background | Retain history and original skill rewards |
| Childhood and youth | Retain native history and attribute consequences |
| Adult background and age | Add optional role selection linked to native age and narrative sequence |
| Player/clan naming and banner | Keep native character, clan and banner controls |
| Native review and advanced start | Add separate **Starting Life** section before final campaign creation: household, family templates, traits, skills, equipment, money, influence, troop roster and starting town |
| Enter Sandbox world | Apply only compatible confirmed new-game profile mutations once, never on save reload |

The exact insertion point and screen style must follow the real v1.5.7 graph, not newer API assumptions. This release only records the graph. The broken standalone UI must not return.

## Actual acceptance

- Sandbox from main menu opens without any extra Calradia menu item.
- Full original character creation Back/Next works.
- A new disposable Sandbox starts and can exit/reload without duplicate hero bonuses.
- `events.csv` records native menu IDs from actual character creation. Use these IDs to link the next native page.
- No new family heroes or map relocations are assumed ready until controlled in-game save/load tests.

**Status:** corrective implementation with CI and gameplay verification tracked separately.
