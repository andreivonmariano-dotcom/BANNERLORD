# v0.58 | Age to All Stats hard-exit isolation after real Windows report

Target: Bannerlord PC **1.5.7.125802**, one `CalradiaUnified` module. This is a **diagnostic candidate**, not a proven crash fix.

## Evidence from the user's November 20, 2026 uploads

- Uploaded ZIP: `CU-Session-20261120T044641_5e9af432-INTERRUPTED.zip`. The reporter confirms the installed DLL SHA-256 was `cec1cf99947d202117b3803c8392a61f48926e6e11487fff79d634c285f6bd6c`, exactly the preceding compiled branch artifact.
- Session `events.csv` shows the native campaign, 3 guided World questions, Culture, Face, and original narrative completed. The working real Age slider recorded its final chosen value **47**.
- At **2026-11-20 04:48:18.7693417 UTC**: `cu_stats_navigation direction=next|from=-1|to=0|mode=Native`.
- At **04:48:18.7727575 UTC**: `cu_stats_page_rows_built page=1/9|visible=6|total=54|scrollable_panel=false|item_template=false|fixed_slots=6|game_native_slider=true`.
- At **04:48:18.7787574 UTC**: `cu_stats_static_slots_notified visible=6|template=false|static_widgets=6|row_page=1`.
- At **04:48:18.7797634 UTC**: `cu_creator_page_checkpoint page=All Stats|phase=bindings_refreshed|target=player|row_page=1`. **No later CU game event is present**, and the previous process left an active marker. The checkpoint is **not** proof that a screen visibly rendered.
- One `session_heartbeat_failed` managed `NullReferenceException` appeared much earlier at **04:47:31.5127469 UTC** in `SubModule.OnApplicationTick`. It predates game-start events and is **not evidence for the All Stats exit**.
- The included prior-session RGL log ends at about **12:48:15 local time** after banner-scene loading, without a faulting stack. A separate RGL tail beginning at **12:49:28** belongs to the **recovery launch**, not the interrupted game. Neither contains a proven native faulting function.
- The other uploaded ZIP, `CU-Session-20261120T044940_f39457a0-CLOSED.zip`, describes a **separate, later clean shutdown** at 04:50 UTC. Its final event is a menu override. It cannot invalidate the interrupted run.

## Isolated experimental change

The v0.55 hypothesis that dynamic `ListPanel` item cloning caused the exit was not sufficient. Version v0.56 removed the template, but the user's v0.57 runtime still exited immediately after six stat row bindings refreshed. The remaining **six bound `SliderWidget` instances and their numeric update notifications** are a plausible failure path, **not a confirmed native stack root cause**.

- Remove `CUFixedStatSlider1` through `CUFixedStatSlider6`, their tracks and handles, and the `@SlotNValue`, `@SlotNMin`, `@SlotNMax`, and `@SlotNIncrement` bindings from all three Stats prefabs.
- Retain six distinct stat rows with labels, current values, **minus, plus, Exact**, Player/Spouse selection, and Previous/Next page controls. All **54** level, attribute, skills, focus, and trait entries remain available through nine six-row pages.
- Keep the working **Age SliderWidget** and its live native mouse-path diagnostics unchanged in native and plain-fallback prefabs. The safe fallback remains button-only for age.
- Stop emitting 24 unused numeric stat-binding notifications in `NotifyStaticSlots`. Retain text/visibility updates and exact +/- callbacks; the underlying progression and save policies remain unchanged.
- Add `cu_stats_button_only_bindings_refreshed` after the six text/visibility refresh callbacks to distinguish this candidate from the crashed stat-slider build.
- Guard `Hero.MainHero` heartbeat access when `Campaign.Current` is null; the user's earlier menu-time exception is a separate, evidenced issue.

## Verification boundary

Windows CI tests inspect the exact three XML prefabs, continued working age binding, six non-slider stat row controls, and the normal regression suite. Simulations and compilation **do not** run Bannerlord's real Gauntlet UI.

**Real-game acceptance is still required:** after backing up the previously installed module, install the v0.58 compiled candidate (identify by the CI run's DLL SHA-256). On a disposable Sandbox, choose Age, then open All Stats. If it stays open, test **all nine stat pages**, +/- and Exact, Player and Spouse, Back to Age, then finish Banner and House. Only after observing successful play may this become a verified fix. Any hard exit on this build warrants a native stack/minidump investigation, not another assumption that a particular source method crashed.

## Reports

New reports continue saving to the real Windows Desktop's **Calradia Unified Reports** folder. Keep the `-INTERRUPTED.zip`, any matching native Windows crash records, and the exact DLL SHA for diagnostics. Reporting remains local and never uploads saves or dumps.
