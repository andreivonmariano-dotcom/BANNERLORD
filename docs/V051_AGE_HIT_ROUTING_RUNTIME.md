# v0.51 | Age input failure: evidence-driven control hit-test repair

**Real user report, Nov 19, 2026**: slider, -1 and +1 buttons all unresponsive. The read-only uploaded `CU-Age-Input-20261119-125114.txt` identifies latest run session `20261119T124930_27aebea3`.

## Confirmed
- `cu_native_stats_step_attached`: age page really appeared.
- `cu_native_age_slider_probe supported=true`: game located the native SliderWidget and handle.
- No `cu_native_age_button` events: age +/- handlers did not execute in that session.
- No native slider click/move events.
- `cu_native_age_slider_probe_failed` is a real `System.MissingMethodException`: `TaleWorlds.GauntletUI.EventManager.get_MousePosition()` returning `System.Numerics.Vector2` does not match the user's installed game.
- Existing running module DLL hash could not be verified in the supplied report, so do not claim binary identity.

## Corrective changes
1. Remove the direct EventManager.MousePosition getter introduced by v0.50. Retain the real game-native SliderWidget and its ValueInt/PlayerAge bridge. Do not invoke the incompatible method even as an optional fallback.
2. Under each age prefab, move the entire slider and +/-/exact/native buttons into the **last direct sibling** of the main Gauntlet canvas. That layer matches the old 690×373 age panel geometry and is visible only on the Age page. The parent refuses events but permits events to children. It wins reverse-order hit tests over passive backgrounds, right-side summary and status text.
3. Log an input edge and resolved `LatestMouseDownWidget` using safe reflection, including focus status. This distinguishes a backdrop intercept, absent mouse events, wrong target, or a handler not executing. Never access the mismatch-prone mouse-position getter in diagnostics.
4. Attempt to restore CU focus only when the current focus is null or a **lower-priority** native layer. Never steal focus from a higher-priority game modal such as the exact-age text inquiry.
5. Static tests verify actual tree order and that each prefab has exactly one +/- and exact age command, plus pinned compile checks. They do not prove in-game click success.

## Release evidence required
The new installed DLL hash must match its pinned Windows CI artifact. With the game running on a disposable Sandbox, click -1, +1, Enter exact age 37 and drag from 50 to 18, 27, 51, 80. The latest-session events must show `cu_native_age_hit_trace`, `cu_native_age_button`, `cu_native_age_exact_entry`, `cu_native_age_slider_value_changed`, and `cu_native_player_age_committed`. The game's actual Hero Age must match the committed age.

**Status is a compiled development candidate only.** Source/XML checks and the previous v0.50 pointer implementation failed in game. Do not claim repaired without live input evidence.
