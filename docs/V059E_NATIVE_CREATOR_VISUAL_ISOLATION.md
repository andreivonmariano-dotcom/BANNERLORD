# Calradia Unified v0.59e | Native creator visual isolation

**Status:** GitHub development candidate. Static prefab/source contracts and Windows C# compile are not a substitute for the actual Bannerlord 1.5.7.125802 client. No screenshots from the new implementation are yet available. Keep the development PR draft until the installed game confirms the intended behavior.

## The user's verified defects

The latest 1650×927 gameplay screenshots showed two interfaces at once. The game-owned Banner Editor/Review Gauntlet panels and titles remained visible behind CU's custom Age, All Stats and House panels. The top title overlapped and underlying native attribute icons showed through left-side text. The custom prefabs drew full-screen translucent tint over both the native 3D scene and the animated main-menu background, reducing readability. The user previously reported that pushing World as a separate `ScreenBase` stopped the animated native main-menu scene, so v0.59d already moved World to a Gauntlet layer on the original menu.

## v0.59e changes

- **Age and All Stats:** `CUNativeCreatorUiIsolation` finds the existing game-owned Gauntlet layers on `CharacterCreationScreen` and, before the CU movie is added, hides their UIContext root widget using `IsHidden`. It saves each prior state and restores it when the CU movie exits or attachment fails. It never removes a native stage, native screen, native SceneLayer, character or horse. CU focus and all original game stage transitions remain. It detects only Gauntlet layers and skips CU layers. It logs `cu_native_ui_isolated` and `cu_native_ui_restored`.
- **House & Fortune:** applies the same temporary suppression to the underlying native Review interface. Left-side CU headings and summary are now World-only, leaving the real native character and horse scene unobstructed. The right panel displays House's current question, value, controls and footer, with a separate status area.
- **Fallback gate:** `UIContext.Root` is discovered via a version-guarded read-only reflection lookup, then `IsHidden` is applied to the real widget. If no root is available or a change fails, restore any modified widgets. Stats uses its prior fully opaque known-working `CUNativeStatsEditor`, and House uses `CUGuidedCreatorLegacyOpaque` from the previously tested full-screen XML. That fallback sacrifices the exposed character scene but avoids two simultaneously visible native/custom UI trees. Never leave a partially hidden native UI.
- **World:** removes all global full-screen brown/black tint and character-creator gradient widgets. Removes the static `cu_world_illustration` from the active custom-art variant, as the actual main menu is already animated. A localized 704-unit translucent summary backing plate and World-only text remain on the left. The 604-unit interactive right panel stays. The existing main-menu `ScreenBase` keeps rendering throughout the menu step from v0.59d.
- **Typography:** increases Age primary title, short story text, bonus text, and Stats row names/base values. Removes old 'drag a slider' and 'scrollable page' copy from the six-row button-only editor. The six All Stats sliders remain absent, all 18 fixed edit callbacks remain, and the original 18–80 native Age slider stays.

## Technical boundaries

This deliberately **does not** change progression logic, family rules, Banner/Clan Name stages, the v0.59d animated menu ownership, the v0.59c Review Back→House reentry path, the sandbox campaign startup manager or the actual 3D camera. The native Gauntlet root tree is hidden, not removed. The restored value is the original `IsHidden`, not an assumed `false`.

The custom 19-image PNG art pack remains optional and requires import into `ui_calradiaunified` with a matching Bannerlord Modding Kit. The runtime package is allowed to use existing TaleWorlds brushes and textures until those assets are registered. Do not claim the proprietary engine atlas was generated in CI.

## Verification gate

`scripts/test_v059e_native_scene_isolation.py` checks both guided and both v059 right-panel prefabs, no global tint/static scene substitution, visibility bindings for World-only left content, required callbacks, native Gauntlet root suppression/restore and safe fallbacks. Existing `scripts/test_v059d_animated_main_menu.py` verifies no separate World screen is pushed. The rest of the Windows workflow still compiles the DLL, checks ZIP layout and executes policy/model-only simulations.

### Real game tests required

At **1650×927**:
1. Press Sandbox from the animated native main menu. Leave World open for several seconds, change Native Sandbox selection and confirm background troops/clouds keep moving. World helper text should be readable against its localized backplate, with no full-screen dark filter.
2. Enter native Culture, Face, origin and story, then Age. Confirm the native Banner title and color/sigil controls are not visible beneath the custom Age editor, but the character and banner/scene are visible on the left. Drag the age handle, use +/- and Exact, preserve story bonuses.
3. Open All Stats. Check all 9 pages, 6 rows per page, Player/Spouse, Native/Balanced/Unrestricted, +/- and Exact. No crash, clipped rows or superimposed native UI.
4. Enter Banner and Clan Name, then House. Confirm native Review text, skill icons and native Back/Next controls are absent during House, but the hero and horse model remain on the left. Test marriage toggle, optional choices, spouse and child input, House Back and Continue.
5. When House closes, confirm the **original** native Review UI returns. Back from Review must reenter House via the v0.59c stage bridge without native-stage duplication. Then check Difficulty, campaign start, save/reload.
6. If any native UI remains visible, inspect `cu_native_ui_isolation_unavailable`, `cu_native_ui_isolation_failed` or `cu_native_creator_safe_fallback` in the latest diagnostics ZIP. Do not merge on static checks alone.

Screenshots and native-input evidence are required to declare the redesign complete. Static tests cannot prove where the game draws its separate 3D and UI layers.
