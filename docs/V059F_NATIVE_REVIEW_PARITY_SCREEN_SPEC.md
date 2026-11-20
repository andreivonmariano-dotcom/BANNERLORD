# Calradia Unified v0.59f | Native Review 1:1 UI parity specification

**Status:** Implementation-ready **design and QA contract**, not a completed UI rewrite. This document is based on the user's native Bannerlord Culture, Face, Family, Banner, Clan Name, Review and Difficulty screenshots and their current World, Age, All Stats and House screenshots, plus the latest source prefabs on `feature/v057-automatic-session-diagnostics`. Do not declare pixel parity until the **native game's XML/brush files and an actual Windows game rendering** have been inspected. All screenshot coordinates are **approximate screen pixels at 1650×927**, not verified Gauntlet logical units.

## Strict visual reference

**Master:** the installed Bannerlord native **Review** page with name entry and culture/family breakdown. The native Family and Difficulty pages corroborate the same outer shell and controls. Reuse the actual installed game's character-creation shell, borders, font brushes, selected/disabled states, Back button and green Next button. Do not imitate those using `SPGeneral\EscapeMenu\escape_panel`, `ButtonBrush` and `BlankWhiteSquare_9` alone. Do not ship copies of proprietary game assets.

Find the actual native prefab and brushes from the user's **installed version 1.5.7.125802**. A known historical path is `Modules/Native/GUI/Prefabs/CharacterCreation/CharacterCreationReviewStage.xml`, but confirm the installed path, child prefabs, resolved brushes and assets directly rather than assuming they are unchanged. The original native XML, its referenced brushes, and game sprite assets are the source of truth for exact layout/typography.

**At the 1650×927 native reference:**

- Outer right native stone column: roughly **x1075–1649 (width 575)**, top **y0**, extending to around **y895**. Its title/stone crown occupies roughly **y0–90**.
- Recessed content region: roughly **x1115–1627 (width 512)**, **y161–828**. Content text is inset from the inner frame, not flush against its dark area.
- Footer navigation: approximately **y834–889**. The small Back button is around **x1142–1230**, and the long native green Next button about **x1285–1609**. Match the game's button heights, cap art, fonts, selected state and hover state from its actual prefab.
- Main hero and scene: **x~275–1075**; original 3D preview should not be overlaid by CU text or large flat rectangles.
- Native Review also has a left attribute/icon rail. **It is native Review content, not a shared creator-shell component.** When the custom House, Age or Stats screen is active, suppress the original native Review/Banner UI widgets and do not duplicate that rail. After closing the custom page, restore the native stage UI exactly.

The current custom Age/All Stats panel visually starts around **x924**, ~151 pixels too far left and is ~710 pixels wide, ~135 wider than native. The current House uses a 604-Gauntlet-unit wrapper around a mostly flat panel with excessive blank area and a generic footer. These must be replaced at the **outer shell**, not scaled by adding arbitrary child positions.

## Shared native shell contract

**One master Gauntlet shell for World, Age, All Stats and House:**

1. Exactly one game-asset native header with **one title** centered like Review. No extra `CALRADIA UNIFIED` heading, chapter headline or step block on the left. Use the source native header's text brush.
2. Native stone outer frame and recessed content panel with measured insets from the installed Review prefab. Do **not** use a full opaque brown rectangle extending beyond the native cavity.
3. Native Back and **native green Next/Continue** buttons at the Review footer anchors. All custom movies must place the footer at one identical Y line.
4. Interior has one content root bounded by the native cavity. It can swap content by ViewModel stage, but outer header/frame/footer **may never change dimensions between custom screens**.
5. Native focus, click hit targets, selection highlight, disabled states, keyboard/controller navigation and scrollbar behavior match the source native widgets. Keep game-owned 3D preview and animated main-menu scene separate from UI widget visibility.
6. No global black/brown tint, alternate frame texture, Escape Menu art, fake texture duplication, arbitrary top-level left text slab, or unregistered `cu_` illustration substituted for real scene rendering.
7. **Do not guess logical font sizes from screenshot pixels.** Copy font, shadow, text scale and alignment from matching native label categories. Record their resolved brushes after examining the native XML. Keep body text within its measured native text box width.
8. Reference resolution 1650×927. The shell must use the game's actual stretch/fixed policies, not raw screen-coordinate offsets. Test 1280×720 and 1920×1080 after reference-resolution parity is achieved.

**Shell geometry contract:** at 1650×927, outer frame edges and footer anchors must match the original native Review screenshot to within **±4 screen pixels** after the game renders. Text baseline/line spacing and button cap states require screenshot side-by-side comparisons, not geometry-only XML tests.

## Q1. Exact screen-by-screen target

### World (main menu interstitial)

- Use the same **native right panel** at x~1075, width~575 with header `Chapter I | Your World` or `Your World` styled in the source native header brush. No CU/Chapter information at top-left.
- Keep the **actual moving Bannerlord main-menu scene** fully visible across the left/center. Hide native menu GUI items (War Sails/Bannerlord title and Save/Continue/Sandbox/Options menu widgets) during World, **not its animated scene or entire ScreenBase**. On closing World, restore native menu widgets and their previous visibility/focus.
- Recessed content y~161–828: question y~184–235, explanatory text y~240–290, selectable value plate around y~319–375, `Previous/Next choice` around y~400–447, optional-choices entry around y~505–548. These are **provisional inner coordinates**. Snap exactly to the native choice-row and spacing grid after inspecting installed native Family prefab.
- Footer native Back/green Continue at y~834–889. Long right button advances campaign. No simultaneous native menu Back/Next controls, no global dimmer.
- Existing commands and bindings preserved: `ExecutePreviousChoice`, `ExecuteNextChoice`, `ExecuteAdvanced`, `ExecuteBack`, `ExecuteContinue`, `@SelectedValue`, `@Hint`, `@StepProgress`.
- Open as a GauntletLayer on the existing main-menu ScreenBase, not a pushed `SandboxCreatorScreen`; animation must remain in motion throughout the step.

### Starting Age (native character scene)

- Same native outer shell, title `Starting Age` centered in the Review title position. Hide the underlying Banner Editor's native GUI title, sigil/color controls and footer. Keep the character and banner 3D scene.
- Content y~161–828: Age value and explanation y~188–258; labels `18` and `80` above track around y~309; **single live 18–80 native Age slider** at y~345–380; actions `−1 year / Enter exact age / +1 year` y~400–446; `Keep native age` y~455–500; concise status y~513–548; native background bonus heading, excerpt and paging inside y~560–790.
- Preserve `CUPlayerAgeSlider`, `CUPlayerAgeTrack`, `CUPlayerAgeHandle`, `@PlayerAge`, increment=1, 18–80 range and separate last-sibling drag-hit geometry. Reposition visual and live pointer regions *together* on a real game test.
- Bonus summary must use native Review's text/scroll typography and natural line height; no dark inset container independent of the native recess.
- Footer Back/green `Next: All Stats` in same native positions.

### All Stats (dense native-panel subpage)

- Same native shell/title `All Attributes, Skills, Focus & Traits`. Shorten the displayed title only if needed to fit the exact native title width. **Do not widen the outer stone panel**.
- Inner region ~x1118–1625, width~507. This page is intentionally denser than Review, with its **own compact content organization inside unchanged native chrome**. It must not extend onto the 3D hero.
- Content bands (provisional, reference pixels): Player/Spouse tabs y~174–210; Native/Balanced/Unrestricted modes y~219–254; one short mode/status line y~262–309; column labels y~314–337; six stat rows y~340–676 (each ~54–56 high); Previous / `Stats n/9` / Next y~688–737; optional context/help y~748–799. Footer y~834–889.
- Six-row table x~1123–1615, width~492. Width budget: stat+base text **~154 px**, current value **~75 px**, button cluster **~235 px**, plus column gaps. Use one shared row template and consistent X anchors, no absolute per-row text hacks. Each control must visibly fit and be pointer-accessible.
- Keep nine fixed pages and 54 entries, all Player/Spouse actions, mode actions, budget rules and all six rows' `ExecuteSlot{1..6}Decrease/Increase/Exact` commands. Keep **NO stat SliderWidget**, `ListPanel` or `ScrollablePanel` on the stats table. The *one* native Age SliderWidget remains in Age only.
- The list is editable and compact, but native **selected-state visuals, button caps, fonts, separators and green Continue** must match the installed game. Never turn the six rows into six large unrelated rectangles.

### House & Fortune (native Review interstitial)

- Same native shell/title `Chapter III | Your House` or shorter `House & Fortune` if exact Review title width requires it. No text over native attribute icons, hero or horse on the left.
- Native Review scene (character and horse) remains visible, original native Review UI is hidden for the duration, and its prior visibility restored after exit.
- Content y~161–828: current question y~195–255; concise explanatory copy y~265–326; selected value in native choice-row visual around y~345–401; a single clear toggle/previous-next/exact input row y~416–466; optional details y~510–562; summary/status only where the native text/content box allows, not as an independent modal.
- Native Back/green Continue controls y~834–889, aligned with World, Age and All Stats. Preserve `@StepTitle`, `@Hint`, `@SelectedValue`, `@IsBoolean`, `@IsChoice`, `@IsNumber`, `@IsText`, `ExecuteToggle`, `ExecuteExact`, `ExecuteAdvanced` and existing creator flow/state.
- Preserve the separate **native Review/Name Back → House** handoff; house Back must not introduce stage loops or reset spouse/children decisions.

## Q2. Current wrong element → exact native counterpart

| Screen | Current wrong element | Native replacement / mapping |
| --- | --- | --- |
| World | Large left `CALRADIA UNIFIED`, `Your beginning`, tinted summary slab | **No left custom GUI**. Hide native main-menu menu widgets; leave moving scene |
| World | Main-menu logo, Save/Continue/Sandbox items visible beneath CU | Hide only native **menu Gauntlet widgets**, restore after World; never freeze scene |
| World | Broad generic brown right card and EscapeMenu stone cap | Native **Review/Family outer stone frame**, title area and recessed content |
| World | Grey Back / Continue with custom offsets | Exact native **small Back / long green Next** footer |
| Age | 850-wide panel starting ~x924 | Native frame **x~1075, width~575** |
| Age | Floating `Starting Age` below banner title artwork | **One centered title inside native stone header** |
| Age | Flat brown content rectangle + independent bonus region | Native recessed inner panel and native Review text styles |
| Age | Oversized/repositioned slider and scattered buttons | One original Age SliderWidget + hit region, aligned to native content grid |
| All Stats | 812-wide, 66-high rows across a ~710px frame | **~492px inner table, six compact aligned rows** in the native shell |
| All Stats | Detached Player/Spouse/Mode controls, repeated helper text | Two compact native control bands, one helpful status line |
| All Stats | Flat brown row stripes and generic grey controls | Native-selected row states and small native edit buttons |
| House | Big floating question modal and empty brown area | Native **Review-style title/content/choice structure**, consistent internal spacing |
| House | Generic small gray footer controls | Native Back/green Continue, same anchors as all other custom pages |
| All native overlays | Native Banner/Review GUI sometimes visible underneath | Stage-scoped **native Gauntlet widget isolation** only, followed by exact restoration |
| Every page | Different stone frames, title offsets, fonts, footer bars | **One actual shared native shell and its original brushes** |

## Q3. Strict remove / keep / replace checklist

### World

**Remove** left summary rectangle, left CU title and progress block, double logo/menu rendering, static custom World illustration, full-screen tint, generic flat right panel, custom footer.

**Keep** real animated menu scene, Sandbox world decisions/advanced settings, current ViewModel commands, Back/Continue and underlying game initialization.

**Replace** native main-menu GUI visibility with reversible suppression; top-level movie with exact native Review shell; choice/content controls with native styles; footer with native green Continue.

### Age

**Remove** oversized 850-unit custom outer panel, improvised stone cap, extra flat backing rectangles, mismatched bonus section and generic footer.

**Keep** native 3D hero and banner scene, original 18–80 slider and last-sibling hit target, exact-age buttons, native story effects and bonus paging.

**Replace** outer shell, fonts, labels, bonus text layout and footer with the game's native Review geometry. The slider's visual and input regions must move together.

### All Stats

**Remove** 812-wide rows, broad 850-unit shell, empty brown rectangle, drifting mode/status/help labels, duplicated column descriptions and generic footer.

**Keep** button-only +/-/Exact editing, all 54 fields and nine pages, Player/Spouse, three modes, original native stats/gameplay rules and no stat sliders.

**Replace** each six-row page with a compact ~492px native-table layout, consistent column widths, native button/selection styles and shared Review footer.

### House & Fortune

**Remove** floating modal treatment, excess brown backing, non-native title treatment, redundant left summary/skill text and generic footer.

**Keep** hero and horse model, native Review stage, spouse/marriage/children/wealth/holding choices, Back→House bridge and native review/name continuity.

**Replace** visible native Review widgets temporarily with the CU native-shell content; restore the exact original widgets on exit. Use native choice rows and native Back/green Continue.

## Implementation sequence and acceptance criteria

1. **Native template extraction:** inspect the user's pinned game `CharacterCreationReviewStage.xml`, its nested prefabs and brushes. Record exact native widget and sprite IDs for outer stone frame, content recess, header, text brushes, active/hover controls and two footer buttons. Do not invent these names. The user's screenshots alone cannot identify them.
2. **Shared native shell:** replace the 604- and 850-unit CU custom outer shells with the single copied native structure. Keep the new shell stable for all four movies.
3. **World native-menu isolation:** hide only native main-menu widgets while World is active; preserve the existing animated ScreenBase and its scene. Restore prior visibility and focus on Back/Continue.
4. **Age/House stage isolation:** hide original native Banner/Review Gauntlet roots without altering the game scene. Reattach/restore original UI and input state after the CU overlay. Preserve safe fallbacks for unsupported game assets.
5. **All Stats compact reflow:** fit six rows and pager inside the real 512px native recess. Preserve native Age SliderWidget, stats button-only safety, 54 field coverage, exact inputs and nine-page navigation.
6. **Side-by-side image gate:** at 1650×927 compare each custom screenshot with the native Review/Family reference. Header and footer edges **within ±4 pixels**, one visible title, matching button sizes/skins and no clipping. Verify type brushes by source, not eyeballing guessed numbers.
7. **Interaction gate:** test full World → native Culture/Face/Family → Age → All Stats (9 pages + spouse) → Banner → Clan Name → House → native Review/Name → Difficulty. Check all Back paths, native menu restoration and animated backdrop. Confirm 1280×720 and 1920×1080 separately.
8. **Build + real game gate:** 814+ independent policy checks, 40 model-only campaign simulations, Windows compile 0 warnings/errors, and **actual live gameplay/screenshots**. Static CI alone never certifies 1:1 appearance or mouse routing.

**Definition of done:** no side-by-side observer can distinguish the **native chrome** (stone frame, title, content recess, Back/green Next, text brushes, controls and margins) from the installed game's Reference Review page. Only each screen's **content** and the naturally different character/scene vary. The real animated World menu continues moving. No duplicate native GUI is visible and no crash regression occurs.

**Known implementation files:**

- `Modules/CalradiaUnified/GUI/Prefabs/CUGuidedCreator.xml`, `CUGuidedCreatorPlainFallback.xml`, `CUGuidedCreatorV059Art.xml`
- `Modules/CalradiaUnified/GUI/Prefabs/CUNativeStatsEditorV059.xml`, `CUNativeStatsEditorV059Art.xml`
- `Source/CalradiaUnified/SandboxCreatorMenuLayer.cs`, `CUNativeCreatorUiIsolation.cs`, `CUNativeStatsLayer.cs`, `CUNativeHouseLayer.cs`, `CUGuidedCreatorVM.cs`

**Scope note:** This document intentionally defines **what must be changed** and the acceptance test. It does not falsely claim these exact native assets were identified in the installed game, that new Gauntlet XML has been built, or that the screenshots are already pixel-perfect.
