# Calradia Unified v0.18.0 | Custom Sandbox creator front-end

**Target:** Bannerlord PC v1.5.7.125802. **Verification:** Gate B **COMPILED_UNTESTED**, [Windows build #44278537058](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44278537058) passed 570 independent policy checks, 40 modeled campaigns (14,600 days), source/GUI XML verification, an installed-artifact smoke test, and reference compilation with zero warnings/errors. No real user-interface or save-load test has been performed. Real UI and save/load still untested.

The game's normal **Sandbox** button now prepares a custom full-screen Calradia Unified character-creation interface directly on the native Sandbox character-creation state. There is no separate main-menu entry. The front-end uses dedicated pages for identity, family, progression, equipment, resources, starting location and review.

- Configurable role: Wanderer, Soldier, Knight, Lord, King or Admiral. The Admiral selection is only a profile role until War Sails integration is tested. A King label does not bypass native ruling-clan creation.
- Family: spouse, children and siblings with names, ages, gender and optional appearance template IDs. Actual hero creation remains a separate explicit opt-in.
- Six attributes, 18 skills, focus points, five traits, existing item IDs, equipment themes, gold, influence, troops and a preferred town.
- A native Escape-key exit and always-present **Return to native creator** action. The previous locked-screen prototype is absent.
- **Apply and continue** saves the profile for the new Sandbox and returns to Bannerlord's underlying appearance/finalization screens. This safe hand-off is still necessary. It is not yet a complete independent replacement for game-provided culture/face/finish logic.

The custom creator launches once per Sandbox character-creation session. To disable it after a rendering failure, set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`. The profile is **not automatically activated on page selection**. Cancelling keeps the native start. The experimental family and kingdom actions remain disabled by default.

See [v0.18 visual flow and acceptance](docs/V018_SANDBOX_OVERHAUL.md). Previous native Starting Life age-menu injection has been retired to avoid the post-age failure.

**v0.18 compiled candidate:** [Download the GitHub artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44278537058/artifacts/14416545228). The GitHub download wraps the module ZIP inside another ZIP, so extract both layers. Compiled DLL SHA-256: `893af9a64e6e8d49e0ab7745a7c344aeefcb63e358e9e34203877c96eadf87c3`.
