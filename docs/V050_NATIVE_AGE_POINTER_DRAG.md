# v0.50 | Native slider mouse-input repair

**Live screenshot:** November 19, 2026 at 1650x927. The Age 50 handle and native sprite rendered, but dragging did not change the selected age. The earlier v0.47 through v0.49 tests covered rendered controls and event binding code, not actual in-game pointer response.

## Repair
Preserve the genuine Bannerlord `SliderWidget` from the native, plain-art and safe age editor prefabs, including `ValueInt` ↔ `PlayerAge` binding. Add a direct pointer path using `Input.IsKeyDown(InputKey.LeftMouseButton)`, `SliderWidget.EventManager.MousePosition`, and the game-measured `SliderArea.GlobalPosition` and `Size`. Accept only mouse-down gestures started inside the visible slider and on the focused creator layer. Moving the mouse calculates a clamped 18–80 integer age and updates both the native slider and the real validated `PlayerAge` ViewModel property. Clicking the thumb preserves its cursor-grab offset, so the thumb does not jump. Clicking the native track directly selects that age.

The native ButtonWidget handle and original slider event handling remain intact. Native +/- and Enter exact age continue to work independently. New events `cu_native_age_pointer_press`, `cu_native_age_pointer_drag`, and `cu_native_age_pointer_release` trace actual pointer gestures and selections. There is no false claim that a mouse drag occurred from a mere ViewModel change.

The same screenshot showed the two numerical endpoints grouped in the middle. Split `18 YEARS` and `80 YEARS` into separately positioned native text widgets in all three prefabs.

## Verification
Pure policy tests cover endpoints, midpoint, grip offset and out-of-bounds clipping; static source and XML checks cover focus and native input. Windows CI is pinned to the game's v1.5.7.125802 reference assemblies. **Only an actual Bannerlord game input session can confirm this mouse behavior.**

## Acceptance test
On the new compiled DLL (verified by SHA-256), open a disposable Sandbox, choose the age page, press the handle at Age 50 and drag to 18, 27, 51 and 80, drag outside both ends, click the track, use exact age 37, test +/- buttons, then confirm Traits and check Hero Age in Encyclopedia and the family age constraints. Upload the next session diagnostics if the slider remains unresponsive. No broader mod work or creator release before passing the live test.
