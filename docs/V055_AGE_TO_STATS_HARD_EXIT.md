# v0.55 | Age-to-Stats hard exit, bounded native UI

**Player report, November 19, 2026:** Bannerlord unexpectedly exited immediately after selecting Next on the known-working Age screen, on entry to the new Attributes / All Stats page of the v0.54 compiled candidate.

## Verified source boundary
- The v0.51 original Age slider/buttons were explicitly confirmed working in-game.
- v0.53/v0.54 introduced a new `ScrollablePanel` with `Standard.VerticalScrollbar`, `ListPanel DataSource="{Rows}"`, and a `SliderWidget` template, with 54+ row VMs created on every page refresh. These new widget combinations were checked for XML shape, **never for in-game Windows rendering**.
- Bannerlord's native `ScrollbarWidget.OnLateUpdate` accesses `Handle.IsPressed` without a null guard in the public decompiled version. A missing game scrollbar brush/handle would be dangerous. This is **a plausible failure path, not a proven crash stack**. The user's game exception log was not available at patch time.

## Targeted crash guard
- Remove the unverified `ScrollablePanel` and `Standard.VerticalScrollbar` from the native-art, plain-art, and safe prefabs. Keep the pinned Bannerlord `SliderWidget` template and `ListPanel` bound to `Rows`.
- Display **at most six** live slider rows in a clipped `Widget` within the same native-styled All Stats stage. `Previous Stats` and `Next Stats` browse the remaining 55+ values. Attribute, skill, focus, trait, naval, spouse and character-level changes remain in their respective `profile.Overrides` and `profile.SpouseOverrides`. Changing pages does not finalize or lose values.
- Preserve the Age native slider and +/- buttons, FaceGen and native stage continuity.
- Emit `cu_stats_page_refresh_enter`, `cu_stats_page_rows_built`, and `cu_stats_rows_page_button` to separate a view construction failure, a navigation input failure, and gameplay application failure. Do not call anything "fixed" from the presence of log lines alone.
- Data-only `SandboxStatsPagePolicy` verifies pagination bounds, including the final partially filled page, with C# executable policy checks.

## Critical acceptance test
1. Back up the working module and confirm the candidate DLL SHA-256 matches its successful Windows CI artifact. The displayed module version may still say v0.38.0.
2. Start a fresh disposable Sandbox. Complete the same native story and select Age 50. Test both age +/- buttons, then **Next: All Stats**. The game must remain open, six or fewer stat rows appear, and the page indicator must display.
3. Press Previous Stats and Next Stats across every page, editing a value on the first, middle, and last page. Switch Player/Spouse; verify each retains independent entries. Check War Sails Mariner, Boatswain, Shipmaster only when registered. Set the player and spouse levels.
4. Back from All Stats to Age, go forward, confirm edits. Complete the creator; check real Clan/Encyclopedia levels, skills and family inheritance. Save, exit and reload, confirm persistence.
5. Repeat with War Sails disabled. The naval fields should vanish without a game crash.
6. **If the process exits again**, collect its latest CalradiaUnified diagnostic events and Windows application crash log. Without these, the precise runtime failure remains unknown.

**Status:** Static tests + pinned reference compilation are only a preflight. User's actual mouse and screen test decides whether the crash is resolved.
