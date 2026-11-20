# v0.59b | Four custom creator screens, native composition, optional final art

**Status:** Implemented in source and Windows CI only. Native Gauntlet rendering and pointer behavior on the user's game remain unverified. The user-approved PNGs require the matching Bannerlord Modding Kit sprite import before their art variants can render.

## The four custom screens

- **World** and **House and Fortune**: active `CUGuidedCreator.xml` is now a single, 604-unit right-side native-like decision panel with a textured header, larger type, value well, input buttons, one Back and one Continue in a consistent footer. Game-owned underlying 3D scenes are left unblocked on the House overlay. World is created before a native scene exists, so its default left half is deliberately a text/summary stage until the optional original World illustration is imported.
- **Starting Age** and **All Stats**: `CUNativeStatsEditorV059.xml` replaces the old full-screen opaque shell on supported reference-size screens. It overlays an 850-unit native-style panel on the right, revealing the real Bannerlord BannerEditor scene beneath on the left. The game stage is not replaced.
- **Age** retains the live, original 18–80 `CUPlayerAgeSlider` ID, handle, track, events, and 1-year behavior. It gets independent help, status, slider, exact entry, bonus history and footer areas, so messages no longer overlap the slider.
- **All Stats** retains the six named rows, nine pages, all 54 values, +/−/Exact commands, Player/Spouse, Native/Balanced/Unrestricted, and native progression. No `SliderWidget` inside stat rows, no dynamic `ListPanel` or `ScrollablePanel`. Each stat row is 812×66 units with aligned name/base/current/adjust columns.
- **Failure recovery:** first try the v0.59b right-side prefab, then known working v0.59 All Stats prefabs and plain/safe fallbacks. The guided World/House movie uses the new right-side native art, with the existing plain texture fallback.

## Generated art activation

Original art package: `CalradiaUnified_v059b_Art_Import_Sources.zip` available in the conversation. It holds 18 individually cropped PNGs from the approved v0.59a asset kit, one separate World illustration, image hashes, and `Modules/CalradiaUnified/GUI/SpriteParts/Config.xml` specifying `ui_calradiaunified` with `AlwaysLoad`.

The two **art variants** are committed as:
- `CUGuidedCreatorV059Art.xml`: `cu_right_stone_panel`, `cu_long_header_plate`, `cu_text_input_field`, and conditional `cu_world_illustration`.
- `CUNativeStatsEditorV059Art.xml`: `cu_right_stone_panel` behind the same real controls and bindings.

**These variants cannot display unregistered PNGs.** The official workflow requires running `TaleWorlds.TwoDimension.SpriteSheetGenerator.exe` on the matching Windows game/Modding Kit, importing the `ui_calradiaunified` category via its resource browser, verifying the generated `CalradiaUnifiedSpriteData.xml` and `Assets/GauntletUI/ui_calradiaunified_1_tex.tpac`, then packaging those outputs with the module. <https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/>

`CUCreatorArtRegistry` checks for the imported category before requesting either custom art movie. The source build's registered category is absent, so the Windows CI candidate uses installed game textures. It does **not** pretend the image pack has been imported or rendered. The fallback is deliberate and does not require a Modding Kit to play.

## Layout and recovery caveats

- Tested first against the user's 1650×927 screenshots. At a substantially shorter vertical height, the new six-row 850-wide panel needs a compact fallback or an additional adaptive layout pass. Do not claim 1280×720 parity yet.
- The game may draw native BannerEditor elements underneath the Age/Stats overlay. The v0.59b solid right panel covers those existing game-owned controls. The Bannerlord-native left character/scene should remain visible, but no game runtime screenshot verifies this yet.
- Graphic art remains conditional. A successfully compiled DLL does not prove the `.tpac` is registered, the sprite size remains correct, or the pointer is unobstructed.
- The prior user-reported **Name Back → Family/House** navigation problem is a separate stage-routing bug. This visual implementation does **not** claim to fix it.
- Do not alter live gameplay logic, family construction, age birthdate rules, or stat-value policies to compensate for a layout issue. The low-risk fallback can still show the proven button-only stat editor.

## Verification

The Windows workflow now executes `scripts/test_v059b_native_screen_integration.py`, which validates both guided variants and both Age/Stats variants, all 18 stat edit bindings, the real Age slider, category fallback detection, no list templates or stat sliders, and right-side geometry. This is XML/source validation only. Policy tests, campaign model simulation, DLL build and runtime-package ZIP checks still run. **Only live Bannerlord gameplay can certify the final appearance and input hit targets.**

On the next test installation use a disposable Sandbox and confirm:
1. World question and options accept input and Continue opens native creator.
2. Culture/Face/narrative (native) still work and real Age slider drags and responds to +/- and Exact.
3. Age → All Stats opens without hard exit, and all 9 stat pages and spouse/parent editors respond.
4. The right panel covers native banner-side widgets cleanly and the left character/scene remains visible.
5. After Banner and Clan Name, House and Fortune loads, shows family/wealth choices, and returns to native review/difficulty.
6. Recheck Back on name/review separately; it is a known unresolved navigation defect.
