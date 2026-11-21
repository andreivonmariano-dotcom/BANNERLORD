# v0.59 | Native-style creator readability, alignment and navigation

**Status:** All Stats first-pass XML implementation. World, Age and House layout changes are specified but not yet implemented. Requires real game screenshots and interaction acceptance on **Bannerlord 1.5.7.125802**. Do not treat static CI success as visual proof.

## Design evidence

The user's four 1650 × 927 screenshots show:
- Guided **World** and **House**: large empty black field, detached small left/right rails, central question card too narrow, tiny instructions and navigation.
- **Age**: useful native drag slider and exact-age controls, but the content and native-history text are undersized.
- **All Stats**: squeezed 718-unit viewport, six cramped 55-unit rows, misaligned name/base/current values, tiny +/-/Exact buttons and loosely placed page navigation.
- Across screens: mixed title alignment, hard rectangular surfaces, very small labels, no consistent column anchors and too much unused space above the footer.

The actual source is native TaleWorlds Gauntlet prefab XML and ViewModels. The guided prefab is `CUGuidedCreator.xml` with a 610×369 central panel. The three stats prefabs are `CUNativeStatsEditor.xml`, `CUNativeStatsEditorPlainFallback.xml`, `CUNativeStatsEditorSafe.xml`. The root safe canvas has `MaxWidth=1820`, `MaxHeight=960`. The existing top band is 91 units, rails are 226 and 338, and footer reserve is 81. These are **Gauntlet UI units**, not guaranteed physical pixels.

## Shared design language

| Role | Target Gauntlet size | Alignment |
| --- | ---: | --- |
| Brand title | 30 | Same left edge, x=22 of inner canvas |
| Phase subtitle | 19 | Left aligned x=28 |
| Primary page/question title | 28–30 | Main stage left edge or centered only inside a choice panel |
| Side rail heading | 21–22 | Left aligned inside a 16–20-unit rail inset |
| Body and help | 18–20 | Left aligned, wrap in a fixed safe-width text box |
| Selected value | 22–32 | Center only inside a single emphasized value well |
| Small metadata | 16 | Avoid smaller values for interactive instructions |
| Main footer control | ≥55 high | Left Back, right Continue with the same inset on every custom page |
| Row action button | ≥52 high | Consistent center text and 8-unit horizontal gap |

Use an 8-unit spacing grid: 8 small, 16 medium, 24 grouping, 32 section. Keep text and controls at least 12 units from textured panel edges. Native-inspired visuals come from the **existing installed** `SPGeneral\EscapeMenu\escape_panel`, `General\CharacterCreation\character_creation_background_gradient`, and `ButtonBrush`, not custom copies of native game assets. Use one dominant panel per main task rather than several raw rectangles. No unsupported corner-radius widgets or unverified brushes. Give all rows one set of column anchors.

Recommended at >=1600 wide: left rail 226, main stage ~900–925, right rail 338; leave breathing room between zones. At **1280×720**, avoid blindly widening fixed prefabs: either use a separate compact arrangement, hide secondary rails by a tested bound policy, or adapt with verified Gauntlet size policies. Do not use an untested runtime translation to move genuine native FaceGen/Banner widgets.

## Q2. All Stats: first implemented pass

Files: all three `CUNativeStatsEditor*.xml` prefabs and exact-layout source regression checks.

**Before:** 718×342 viewport at x251/y276. Six 700×55 rows at offsets 0,55,…275. Labels around 13–16 units, selected values 16. Each row has 41×39 minus/plus and 49×39 Exact, with redundant "EDIT WITH - / + OR EXACT" text. Paging 179×45 starts at y633. The working Age slider is a separate, last-sibling UI control.

**First implemented pass:** 925×410 viewport at x251/y317, framed with installed native escape-panel art. Six 905×66 rows at y offsets 0,68,136,204,272,340. Name 20, native/base value 16, selected value 22. Distinct header labels **STAT / NATIVE**, **CURRENT VALUE**, **ADJUST** at y288. Each row uses minus/plus **76×52**, Exact **132×52**, native `ButtonBrush`, with consistent x offsets **590, 674, 758**. Remove the repeated instructional line from every row. Soften row fills and keep each column aligned. Paging buttons enlarge to 225×55 at y743.

**Still to refine after game screenshots:** Increase main mode-selector button height to ~52, move player/spouse context to a clear segment above or beside the table, align budget/target/help text under a single heading, left-align native-history text, and balance the rail height. The first pass deliberately keeps existing interaction ownership, names, and progression code.

**Preserved behavior:**
- `CUNativeStatsVM` stays unchanged. All 54 level, attributes, skill, focus and trait values appear on nine six-row pages, including War Sails skills if registered.
- Keep `ExecuteSlot1..6Decrease`, `Increase`, `Exact`, `ExecutePreviousGroup`, `ExecuteNextGroup`, `ExecuteEditPlayer`, `ExecuteEditSpouse`, `ExecuteReset`, `ExecuteBack`, `ExecuteContinue`.
- **Do not restore any stat `SliderWidget`, `ListPanel`, `ItemTemplate` or numeric stats binding.** The v0.58 button-only path passed the user's live playthrough after prior Age→All Stats hard exits. The native Age slider, original narrative stages, game state and session reports must remain intact.

## Q1 + Q3. World/House/ Age plan and before→after targets

### World setup / House and Fortune

Source: `CUGuidedCreator.xml` and `CUGuidedCreatorPlainFallback.xml`, existing `CUGuidedCreatorVM` bindings.

**Before:** center card 610×369, main title 27, hint 17, choice value 26, action buttons ~260×47. Step progress and summary are detached in 226/338 side rails, with a large field of blank black pixels.

**Target:** center panel **880×490** (safe on the user's 1650 reference), centered at y145, using the same installed native textured frame. Main question title **30**, description **19**, selected value **32**, choice/toggle buttons **54–58 high**. Increase the central value well to roughly 800×100, with 24-unit section gaps. Keep left progress grouped in one visual block and right summary top-aligned with labeled values rather than a long centered paragraph. Set side-rail body 18–19 and group optional settings into a single secondary button.

**Navigation:** preserve `ExecutePreviousChoice`, `ExecuteNextChoice`, `ExecuteToggle`, `ExecuteDecrease`, `ExecuteIncrease`, `ExecuteExact`, `ExecuteEditText`, `ExecuteNativeDefault`, `ExecuteBack`, `ExecuteContinue`. Use primary Back/Continue only in the footer. Do not confuse Previous/Next **choice** with Back/Continue **step**. Do not change the conditional field sequence or World-before-campaign and House-after-identity boundaries.

### Age

Source: stats editor prefabs and `CUNativeStatsLayer.cs`.

**Before:** center panel 690×373, title/helper 21/15, native slider width 524, +/-/Exact buttons around 150–180×43, and a detached native-effects rail.

**Target:** center panel around **860×475**, same stage origin, age label **28–30** and helper **19**, slider track around **650–680** wide, age actions **54 high**, with larger 18/80 endpoint labels. Keep exact age and native age fallback visible. Native-history rail moves to a left-aligned 18–20 body with consistent paging controls.

**Safety boundary:** the user confirmed the current native Age slider works. A future geometry change must update BOTH the visually backed age panel and the separate **last-sibling** pointer-hit target together. Do not alter slider value range 18–80, `CUPlayerAgeTrack`, `CUPlayerAgeHandle`, or pointer/focus logic without measured in-game evidence. Never place a new transparent widget in front of the live slider.

### All Stats

**Before:** six small 55-high dense rows, cramped values, tiny +/- and Exact controls, a redundant instruction repeated six times, and page nav under a narrow viewport.

**After (first pass committed):** 66-high rows, 925-wide native-framed viewport, larger type and controls, single row-column header, stronger paging. After verifying the game presentation, place mode/target choice in a clear top region and refine the side rails.

## Rollout and acceptance gates

**Gate A: All Stats XML design.** Verify XML parses for native, plain fallback and safe fallback. Confirm all 18 edit commands and six bound text rows, nine pages, mode choices, spouse editing, working age input, and no stat sliders. Run all independent policy tests, model simulations, full Windows compile and package checks. Then take a **1650×927 actual-game screenshot** and test six-row interaction and paging.

**Gate B: Guided World/House.** Apply shared 880×490 panel and typography to BOTH native and fallback prefabs. Test question types (choice/boolean/number/text), advanced fields, optional defaults, footer actions, conditional child/kingdom choices and clean campaign start.

**Gate C: Age.** Rebuild visible panel and hit-test geometry as a paired change. Test actual pressed-handle drag, precise age changes, +/- and Exact, Back/Next, 18 and 80 extremes, native-story bonus preservation, and focus at 1650×927.

**Gate D: Cross-screen polish.** Compare header edge, 30/19 typography, left-aligned summary labels, footer geometry, active/selected button states, compact width at 1280×720, and 1920×1080. No clipped text, overlays blocking native controls, or click regressions.

**Ship gate:** Windows CI green **AND** the actual installed Bannerlord game passes World → native creator → Age → All Stats (all nine pages, spouse controls) → House, save/reload, and complete Back navigation. A successful C# build cannot certify native Gauntlet rendering.
