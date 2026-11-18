# Calradia Unified v0.23.0 | Crash-safe Guided Sandbox Test Candidate

**Game:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Status:** **COMPILED_UNTESTED**, not certified as a game-tested fix. [Windows CI #44310173930](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44310173930) passed 591 policy checks, 40 simulated campaigns and zero C# warnings/errors. That cannot prove the native culture screen, marriage, or saving work.

## Why v0.23 replaces v0.22

The player reported a crash entering Bannerlord's native culture screen. The only supplied diagnostic showed the culture/face/narrative/review *stages* existed; it contained no exception stack. The experimental v0.22 face view was registered globally even when its UI option was false. This is a plausible cause of the crash, not a proven root cause.

**v0.23 removes the competing `CharacterCreationStageView` implementation completely**, deletes the custom FaceGen header prefab and extra assembly references, and disables the obsolete appearance override flag. The native game remains in charge of culture, 3D face/body, story background, clan naming and finalization.

## Family reliability changes

The beginning-married request now defaults spouse gender to `Auto`, based on the player's actual character sex. Before calling native `MarriageAction`, the mod checks the game's marriage suitability model. It then verifies both spouse references, spouse-clan membership and reciprocal mother/father/children relationships. Events report `cu_family_verification=PASS/FAIL` and partial-creation failures.

**Family is not yet independently proven in-game.** The game can reject a requested marriage or hero template, and partial hero creation has no automatic rollback. The next disposable campaign plus save/reload test determines whether this source change works on the user's v1.5.7 installation.

## Six guided pages instead of a long settings form

- Starting Life: actual native career and kingdom, only if the role requires one.
- Appearance: culture preference and a clear explanation that native face controls follow.
- Family: spouse request, then relevant spouse and child fields.
- Resources: gold, influence, recruits. Skills and exact equipment appear under Advanced.
- Kingdom & Fief: native or consented same-kingdom castle/town/fief options and world scenario settings.
- Review: starting role, kingdom, family, fief and scenario, then **Begin your life**.

A **Show advanced settings** button reveals less common settings only on request. Back/Continue advances through the relevant fields and pages. New contextual explanations state what each option actually affects. The XML uses native frame/button brushes, selected navigation states and larger content regions. Game screenshots at more than one resolution are still required to verify readability and appearance.

## Installation and test evidence

- [Download the v0.23 compiled-untested module](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44310173930/artifacts/14430555683). Exact DLL SHA-256: `fad4d45b39f07cf1d17982948594908cd87bf84f4f46ebb980499858b7c1ca60`. Installation backs up the old module.
- [Crash recovery and step-by-step family test](docs/V023_CRASH_RECOVERY_FAMILY_GUIDED_UI.md)
- [Underlying crash report #48](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/48)
- [Full unified character-creator goal #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44)
- [Windows CI](.github/workflows/bannerlord-build.yml)
- [New game preflight](scripts/Run-CU-Runtime-Preflight.ps1)

**Do not test on an existing save.** First confirm Wanderer with no family and no fief reaches the native culture screen without crashing. Next test a married Lord and save/reload. Add children only after the spouse test passes. Test fief transfer last.

Set `OverhauledCreatorEnabled=false` under `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` to restore Bannerlord's original Default/Advanced Sandbox start. This does not affect old installed DLLs; replace v0.22 completely.
