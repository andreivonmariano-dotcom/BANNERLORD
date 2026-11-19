# v0.49 | Game-native Sandbox creator UI parity

**Target:** Bannerlord PC v1.5.7.125802. **Source base:** v0.48 creator-family fixes. **Status:** source/UI tests and pinned Windows compilation, then real game verification. The module manifest still reports development v0.38.0.

## Actual screen ownership

**Already native (pixel identity follows the installed game's own files):** culture selection, 3D FaceGen and camera, life story and age narrative rewards, banner editor, clan name, original review, and difficulty/options. All are the genuine game-owned stages and controllers.

**Unique Calradia Unified content:** starting world/role choices before game creation, starting household/fief/fortune between native clan and review, and the 18–80 age/attributes/skills/traits editor between narrative and banner. There are no vanilla stages containing these custom features. The CU Gauntlet prefabs therefore **reference native Bannerlord sprites and native ButtonBrush**, but are not pixel-identical vanilla templates. No original game textures are shipped.

## UI and slider hardening

- Use Bannerlord's actual `SliderWidget`, `Widget.FindChild`, `GetChild`, `Handle`, `SliderArea` and `ValueInt` instead of reflection guesses. Validate actual slider range (18–80) and native input handle, retry the movie tree for up to 60 ticks, then provide honest fallback status.
- Keep the native two-way ViewModel binding, but avoid reverting a button/exact-entry age change with a stale widget value. Separate `cu_native_age_slider_value_changed` from `cu_native_age_slider_drag`. A logged mouse drag requires a pressed game handle.
- Every stats prefab includes `Enter exact age`, 1-year +/- buttons and Keep native age. The original native 20/30/40/50 narrative options remain game-owned and retain their original background/reward side effects.
- Household now falls back from the native-sprite CU movie to its plain prefab. If both fail, log a clear household-unavailable event rather than pretending the family request was confirmed.
- `scripts/Audit-CU-Native-Creator-Assets.ps1` reads only installed native XML reference **metadata**, never copies proprietary textures or prefabs. It reports `UNKNOWN_COMPILED_OR_MISSING` when an asset cannot be proved from readable XML, not a false claim of absence.

## Run installed asset audit
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Audit-CU-Native-Creator-Assets.ps1 -GameDir "D:\Games\Mount & Blade II Bannerlord" -OutFile "$env:USERPROFILE\Desktop\CU-Native-Creator-Art-Audit.json"
```
Use the **actual** installed game directory in place of the illustrative path.

## Release blockers

1. Game screenshot comparisons at 1366x768 and 1650x927 for World, Age/Stats, House and original native screens. The native game's original screens need no cloned replacement. Exact 100% pixel identity for the *new* CU-only panels cannot be claimed until a pinned asset/layout audit and real image comparison succeeds.
2. Slider drag pointer input from 18 to 27 to 51 to 80, age +/- and exact entry, round-trip after Back, real Hero age in Encyclopedia and save/reload. Verify logs show `cu_native_age_slider_value_changed` and any genuinely pressed drag, not only a model setter.
3. Single native stage handoff, native age bonuses intact, family ages and six children correctly spawned, adult residence and minor home, no layer/focus exceptions.
4. Two new campaigns, one save/reload, hash-verified installed DLL, and no new-session `cu_native_widget_skin_restore_error`.

**Do not claim in-game acceptance from source compilation or the static art-reference audit.** The native asset audit is a preparation step, not a replacement for a screenshot and click test.
