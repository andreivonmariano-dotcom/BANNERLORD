# v0.59f | Native Review shell extraction, four-screen implementation and screenshot parity checks

**Status:** Implemented and compiled in the v0.59f branch. **Actual game screenshot/parity verification still outstanding.** The source build uses the exact native Review widget structure and brush names from Bannerlord **1.5.7.125802**, not screenshot-estimated XML. No TaleWorlds textures, sprite atlases, or font files have been copied into this mod.

## Source of truth

- Exact version: `Bannerlord.ReferenceAssemblies.GUI.v3 1.5.7.125802`, from the BUTR project's GUI-derived JSON format. Downloaded from the NuGet flat container during Windows CI.
- Exact native prefab: `gui/Native/GUI/Prefabs/CharacterCreation/CharacterCreationReviewStage.json`, matching installed `Modules/Native/GUI/Prefabs/CharacterCreation/CharacterCreationReviewStage.xml`.
- The native Review root contains a `RightPanel` with `WidthSizePolicy=Fixed`, `HeightSizePolicy=Fixed`, **SuggestedWidth=694**, **SuggestedHeight=1080**, **HorizontalAlignment=Right**, **VerticalAlignment=Center**, and sprite `General\CharacterCreation\character_creation_panel`.
- Its exact native title is a **670×55** `TextWidget` with `Brush=Stage.Title.Text`.
- Its native `InnerPanel` is **573×750**, aligned center/center, `PositionXOffset=23`, `PositionYOffset=37`.
- The native footer is a **bottom-right ListPanel**, `MarginRight=40`, `MarginBottom=50`, containing a **125×64** `Standard.BackButton` and **400×64** `Popup.Done.Button.NineGrid` primary button with `Popup.Button.Text` caption brush. The generator rebinds their actions to `ExecuteBack` and `ExecuteContinue`.
- The native UI's constants include brush-derived button dimensions, retained in the generated movie. `Brush="Stage.Description.Text"` supports page content.

These values were extracted from the actual versioned native tree during Windows CI. The earlier v0.59f *planning* document's 575px panel value came from a 1650×927 **screenshot**. That is a rendered-screen measurement, not the Gauntlet logical width. The source width of **694** is now authoritative for the generated shell.

## Shared implementation

`scripts/generate_v059f_native_review_shell.py` downloads the exact GUI.v3 package, verifies source-native panel dimensions/sprite/brushes, and emits **two complete, working movie prefabs from one shared shell builder**:

| Generated movie | Reused screens | CU content |
| --- | --- | --- |
| `CUGuidedCreatorNativeReview.xml` | World, House & Fortune | question, hint, value, number/boolean/text/choice actions, optional settings, step progress |
| `CUNativeStatsEditorNativeReview.xml` | Starting Age, All Stats | original 18–80 discrete Age SliderWidget, bonuses, six-row button-only Stats editor, nine pages, Player/Spouse and modes |

All four share **the same native RightPanel/title/InnerPanel/footer XML**, with only content and footer caption bindings different. The built prefabs contain **references** to the original game assets, not copies of them.

Runtime loaders now try these two movies **first**, then fall back to the previous working v0.59e right-side prefabs and opaque backups on failure. The native review UI/root remains isolated while the custom Age/Stats/House movie is active, and returns on exit.

World remains a Gauntlet layer on the existing animated menu ScreenBase. `SandboxCreatorMenuLayer` now hides native menu GUI widgets (logo, menu buttons) with `CUNativeCreatorUiIsolation`, **not** the menu's game-owned scene. The old background-freeze screen push remains removed. The native UI visibility state is restored on Back/Continue.

The generated stat movie has **no stat SliderWidget**, `ScrollablePanel` or dynamic stat `ListPanel`. The **one** Age SliderWidget is the last sibling hit target and retains its named track/handle and one-year steps.

## What the automated tests prove

- `scripts/test_v059f_shared_native_review_shell.py` runs immediately after prefab generation. Both movies have an identical shared native shell, same native frame and footer brush IDs. Guided commands are complete. Age is 18–80 discrete. All 18 fixed stat-edit actions are present, along with 54 fields across nine modeled pages.
- `scripts/verify_package.py` fails packaging if either generated movie is missing or if native Review sprite/Back/Next controls differ from the pinned shell.
- `scripts/compare_native_creator_screens.py --self-test` creates native/equal/shifted sample screenshots at 1650×927 and confirms a displaced panel is detected. The script then accepts actual native and custom screenshots, outputs an overlay PNG, an edge-difference PNG and JSON metrics.
- `scripts/Check-CU-NativeReview-Installed.ps1` reads the **user's local installed native Review XML** without changing files. It validates the native header/frame/content/footer and locates brush definitions; optionally saves a local diagnostic JSON.

## How to compare real screenshots

Capture the native Review screen and each custom screen at **1650×927**, with identical UI scale, on the same monitor. Then run:

```powershell
python -m pip install Pillow
python scripts/compare_native_creator_screens.py --native .\screenshots\native_review.png --custom .\screenshots\age.png --screen age --out .\BuildResults\age-native-diff --strict
python scripts/compare_native_creator_screens.py --native .\screenshots\native_review.png --custom .\screenshots\house.png --screen house --out .\BuildResults\house-native-diff --strict
.\scripts\Check-CU-NativeReview-Installed.ps1 -GameRoot "C:\Path\To\Mount & Blade II Bannerlord"
```

Look at `native_custom_chrome_overlay.png`, `native_custom_chrome_edge_difference.png` and `native_chrome_comparison.json`. The screenshot geometry gate checks an approximately 4px panel-left offset. It intentionally excludes changing hero/horse artwork, page text and contextual row controls, and **does not** prove 1:1 pixel identity.

## What remains unverified

- That the game actually loads the two generated movies rather than fallback prefabs. The diagnostic journal `cu_native_creator_assets` should identify `layout=review_shell_v059f`. If absent, check `cu_world_review_shell_fallback`, `cu_house_review_shell_fallback`, or `cu_stats_review_shell_fallback`.
- Native menu GUI hiding, 3D character/horse visibility, continued main-menu animation, native button click/hover states, real text measurement and scrolling, hit testing for one-year Age slider and fixed +/-/Exact Stats rows.
- Actual screenshots compared to the native Review reference, and test at 1280×720 / 1920×1080.
- The Review/name Back → House/Fortune return and campaign startup.
- The separate 19-image custom art atlas still needs the matching Modding Kit to import. **v0.59f does not need it for the native stone panel**, as it references installed Bannerlord assets instead.

**Release gate:** Keep PR #88 draft and do not call the UI 1:1 until real game chrome screenshots and full creator interaction tests pass. Windows CI can verify exact source-side shared shell, compiled DLL and ZIP, but cannot run Bannerlord's native rendering engine.
