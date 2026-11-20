# v0.59c | Four-screen 1650×927 audit, Review Back bridge, official UI art import

**Status (Nov 20, 2026):** New Gauntlet layouts and native Review Back bridge are coded, compiled and statically checked. **Not yet demonstrated in an actual game session.** Original 19 PNGs have passed image decoding, dimensions, alpha range and SHA-256 checks. **No compiled/imported UI atlas (.tpac) is available in this development environment.** The Windows Modding Kit import remains a required local step.

## A. Native Review player name Back → custom House & Fortune

The player's **name** field shown on the native **Review** screen is not the native **Clan Name** stage. CU inserts House & Fortune as an overlay on the game-owned Review stage after Banner and Clan Name. The original implementation marked House completed, then never intercepted a native Review Back, which jumped to Clan Name or Banner.

`CUNativeHouseNavigationPolicy` now detects `Review → ClanName` and `Review → Banner` only after the House overlay was shown and completed. `CUNativeHouseLayer` uses a bounded, one-native-`NextStage()`-per-tick rejoin to the **same** native Review stage. At the next Review tick, it opens the saved House editor again. This keeps all original game stages, hero progress, native banner settings and Review naming logic.

**Non-interception cases:** House's own Back sets `completed=false` and leaves the native Clan stage visible. An ordinary Review Next to Options is not intercepted. No extra `CharacterCreationScreen`, `CharacterCreationReviewStage` or `CharacterCreationClanNamingStage` is created. Failed rejoin, unsupported stage or more than three attempts yields back to native screen and an event rather than looping.

Diagnostics:
- `cu_native_review_back_to_house` includes observed native destination
- `cu_native_review_house_rejoin` records each game-owned `NextStage`
- `cu_native_review_house_reentry` confirms arrival at Review
- `cu_native_review_house_fallback` or `cu_native_review_house_rejoin_failed` identifies a safe fallback

PolicySmoke has 4 new independent checks for Clan/Banner Back, House-originated Back, and Review Next. Static integration is covered by `scripts/test_v059c_review_back_to_house.py`.

## B. 1650×927 exact-source geometry audit

`scripts/test_v059c_layout_1650.py` reads **both native and optional-art prefabs**. It models a 1650×927 screen with the committed fixed dimensions and margins. This is **not actual Bannerlord rendering or mouse interaction**. TaleWorlds scaling, scrollbar behavior and native scene input are separate live tests.

| Screen | Panel width and left edge | Checked region |
|---|---|---|
| World / House | 604 wide, screen x1034 | left 990-pixel World illustration gap 44; navigation wholly inside panel |
| Age / All Stats | 850 wide, screen x790 | 790px left native character/scene area |
| All Stats six rows | 812-wide viewport at panel-local y225 | row content ends y761, pager begins y768 |
| All Stats pager | page controls y768–816 | footer begins y826, **10 UI-unit gap** |
| Age | slider bound to `@PlayerAge` over 18–80 | last-sibling drag target remains outside footer |

**Changes since v0.59b:** Stats ModeDescription, target help, new `@Status` feedback, row headers and 408-high six-row viewport are stacked into distinct bands. Paging moves from y649 to y638 in the Stats group, resolving a 1-unit footer collision at the 1650×927 reference. All three editor commands per stat row and all 54 entries are unchanged. No numeric stat sliders.

**Limitations:** Hardcoded height is targeted at 1650×927. Do not claim 1280×720 support without a compact layout and real QA. Real game screenshots must confirm fonts, title wrapping and the loaded Gauntlet brush sizes.

## C. Official sprite art pipeline

The 19 custom PNG sources in `CalradiaUnified_v059b_Art_Import_Sources.zip` are already generated. The archive contains an SHA-256 manifest, the `ui_calradiaunified` category, and `SpriteParts/Config.xml` with `AlwaysLoad`. All 19 source PNGs were decoded and their SHA-256 hashes and dimensions checked; 18 have alpha transparency, and `cu_world_illustration.png` is opaque.

To finish the *actual engine import* on a Windows game/modding installation:

1. Back up the working `Modules/CalradiaUnified` folder. Place the v0.59c code build into the same module. Use `scripts/Install-CU-UI-SpriteSources.ps1 -GameRoot '<Bannerlord folder>' -SourceZip '<asset ZIP>' -RunGenerator` on the machine with the **matching Bannerlord Modding Kit**. It SHA-checks the 19 images before copying them and starts the official sprite-sheet generator.
2. In the Modding Kit, open the resource console (Alt+grave), run `resource.show_resource_browser`. Under **CalradiaUnified → GauntletUI**, select **Scan new asset files**, choose category `ui_calradiaunified), then **Import**. Close the editor.
3. Use `scripts/Install-CU-UI-SpriteSources.ps1 -GameRoot '<Bannerlord folder>' -VerifyOnly` to check `GUI/CalradiaUnifiedSpriteData.xml` and `Assets/GauntletUI/ui_calradiaunified_*_tex.tpac`. The art prefab variants load only after `CUCreatorArtRegistry` finds the category.

Official procedure: <https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/>.

**Import caveat:** The code branch and CI artifact contain prefab references and the category config, not the 19 PNGs or a compiled `.tpac`. That is deliberate. Without the atlas, the new layouts display installed-game textures. A client with a properly imported atlas may show the approved custom panel and World illustration. No out-of-game graphic simulation can certify the appearance in the real game client.

## D. Live release checklist

- At **1650×927** test World (selection, optional choices, Back/Continue).
- Test native Culture, Face, Family background, Age (drag, exact, +/-), all **nine** All Stats subpages (level, attributes, War Sails skills, focus/traits), editing Player and Spouse.
- Test Banner → Clan Name → House & Fortune → native Review and enter character name. Click **Back on native Review**. It must reopen **House & Fortune** with saved values, not Banner. House Back should still reveal the native predecessor. House Continue should return to Review.
- Confirm no underlying native Banner input can be clicked through the custom right panel. Confirm the native character model remains visible on the left and no overlapping or clipped text.
- Test native Difficulty, Start Game and Save/Reload.
- If the engine fails, attach the newest `Desktop/Calradia Unified Reports` ZIP and the related native failure log. CI model-only campaigns cannot confirm a Bannerlord live editor.

**Do not merge the draft PR or claim fully baked art until the above gates pass in the actual Windows game.**
