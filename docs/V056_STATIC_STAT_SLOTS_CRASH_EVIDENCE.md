# v0.56 | Age→Stats hard exit: remove row-template instantiation

## Exact evidence from user-provided report
The read-only `CU-Stats-Exit-20261119T151455Z.zip` (November 19, 2026, 15:14:55 UTC) includes the latest session `20261119T151338_49629482`.

- 15:14:32.690Z: `cu_native_stats_step_attached focused=True` on the working Age page.
- 15:14:32.698Z: `cu_native_age_slider_probe supported=true`.
- 15:14:33.538Z: `cu_stats_navigation direction=next|from=-1|to=0|mode=Native`.
- 15:14:33.539Z: `cu_stats_page_refresh_enter creator_page=0|target=player|row_page=0`.
- 15:14:33.548Z: `cu_stats_page_rows_built page=1/9|visible=6|total=54|scrollable_panel=false`.
- **No later event in the session**. The game exited without a new `exceptions.csv` record or recent Windows Application Error. The engine RGL log ends after transitioning to native Banner scene. The supplied RGL error logs have headers only. This does not prove a particular native stack frame.
- 54 fields confirms the new UI detected all three War Sails skills and their focus-point settings (1 level, 6 attributes, 21 skills, 21 focus values, 5 traits).

The user's previous v0.54 run exited at the same place. The v0.55 fix removed the `ScrollablePanel` but retained a `ListPanel DataSource="{Rows}"` with nested `ItemTemplate` and a `SliderWidget`. Thus the common remaining code was **dynamic Gauntlet item-template cloning and row data context**. This component is the leading fault hypothesis, not an observed crash stack.

## v0.56 correction
- Completely remove `ListPanel`, `ItemTemplate`, `{Rows}`, `ScrollablePanel`, and `Standard.VerticalScrollbar` from **all three** native-styled Stats prefab variants.
- Replace reactive `MBBindingList<CUNativeStatRowVM>` with an ordinary private `List` and eliminate row-level Gauntlet property-change notifications.
- Draw six permanently named native `SliderWidget` controls, each bound directly to `CUNativeStatsVM.Slot1Value` through `Slot6Value`, with distinct text, min/max/increment, plus/minus, and exact-entry commands. Each static widget has a separate ID and top offset.
- One logical All Stats stage retains the Previous/Next six-row pages, all 54 possible fields including registered naval skills, and independent Player/Spouse overrides. Existing stored profile values remain unchanged.
- Keep the user-confirmed Age slider and +/- controls, native narrative, native Banner/House transition unchanged.
- Add `cu_stats_static_slots_notified` and `item_template=false|fixed_slots=6` to distinguish runtime code paths in the next diagnostic.
- Update six older source/visual tests that expected the previous faulty template, plus a strict new test that fails if `ListPanel` or `ItemTemplate` is reintroduced.

## Verification and release boundary
Pinned Windows 1.5.7.125802 CI run:
https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44554505001
- 778 policy checks, 40 simulated campaigns, 303,098 modeled assertions.
- 0 compiler warnings, 0 compiler errors.
- The **only validated fact is that the new source compiles and the static contracts pass**.

In-game acceptance:
1. Back up the known installed module, install the newly packaged v0.56 candidate and verify its DLL SHA-256.
2. Start a disposable Sandbox. Confirm original Age +/- and drag still work. Press Next to All Stats, and check six fixed rows appear without process exit.
3. Click +/- and exact-entry on first page. Switch through all nine stats pages, including naval values and spouse, then return to the first page. Check persisted choices.
4. Finish House and Fortune; check native player/spouse levels, all skills and family inheritance in Clan/Encyclopedia. Save/reload.
5. **If the process exits again, stop testing immediately.** Export the latest game's logs and report; seek native crash stack/minidump details rather than applying another speculative rendering fix.

Do not merge this draft until the user confirms the Age→Stats transition and editor interactions in the installed game. `SubModule.xml` still reports v0.38.0, so identify the development artifact by verified DLL SHA-256.
