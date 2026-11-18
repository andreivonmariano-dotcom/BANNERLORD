# v0.29.0 runtime acceptance: native origin review + genuine family portraits

**Game:** Mount & Blade II Bannerlord PC v1.5.7.125802. **Current status:** compiled, **not yet tested inside Bannerlord**. Never convert static checks into in-game success.

## Design and crash boundaries

Native Bannerlord retains all seven built-in CharacterCreation stages: culture, 3D face generator, narrative backgrounds, banner, clan naming, native 3D review, and difficulty/options. The separate Calradia Unified main-menu five-chapter starting-conditions creator remains. Its role, spouse, children, clan and fief plans are committed *before* native character creation. The v0.29 experimental mode adds a distinct eighth, CU-only native 3D review after the native difficulty screen. This is a **first integration**, not a claim that the full two-screen sequence has been replaced.

The custom `CUNativeOriginReviewStageView` derives from the engine's real `CharacterCreationReviewStageView`; its 3D player appearance, camera and rotate controls stay native. The CU text overlay is nonfocus and may fail separately, reverting to native review visuals. The previous v0.22 FaceGen-override class **must not be reintroduced**. A stage view preflight compares the custom class and the game's own active-referencing-assembly scan before inserting the extra stage, to avoid an unrenderable stage.

Spouse and children do **not** exist as native Heroes until after final character creation. Pre-game summary contains their planned names/ages/count, not invented 3D characters. The new **CU: View my household portraits** action in any town or castle opens a separate read-only gallery of already existing native Heroes with their actual character-portrait data, parent links, ages and residence. The portrait viewer must not manufacture relatives, activate an infant, recruit children or affect their save data.

## Gate B: automated build checks

- Exact, pinned `v0.29.0` references compile with 0 warnings/errors.
- Unit smoke policy: original 617 plus 3 native-integration checks. New-game reset disables optional review, serialized opt-in roundtrips.
- Static checks: one `CharacterCreationStageView` for the unique `CUNativeOriginReviewStage` only; no attr for `CharacterCreationFaceGeneratorStage` or `CharacterCreationCultureStage`; no native stage deletion. All XML parses, right-side summary bindings and real Hero texture identifier references exist.
- Package contains `CUSandboxCreator.xml`, `CUNativeOriginReview.xml`, `CUFamilyPortraits.xml` and the matching DLL; excludes the crash-prone v0.22 FaceGen prefab.
- PowerShell 5.1 installer rejects a mismatched **successful CI** DLL hash *before touching* an existing installed module. Powershell audit correctly checks current one-child campaign without picking up older logs.

## Gate C: main-menu creator and default safe fallback

1. Close Bannerlord and back up a known-good campaign.
2. Install the exact `v0.29.0` CI artifact via [project PowerShell SOP](POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md), pinning its successful-run DLL SHA-256.
3. Start **Sandbox**, choose Lord + Vlandia, enable generated NobleRandom spouse and **one** daughter. Review her name and age.
4. Leave **More options → Preview CU choices inside Bannerlord's 3D review** OFF. Confirm CU choices.
5. Native culture, appearance (live rotating 3D), narrative, age, difficulty must all work without an extra CU review. Reach campaign map.
6. Compare `sandbox_creator_family_plan`, `cu_family_verification`, `sandbox_start_contract`: actual spouse true, children exactly **1**. Check clan, father/mother and owned fief.
7. Exit, reload save and inspect the actual family. Log-based PASS alone is not persistence evidence.
8. Separate negative test: cancel CU screen with Escape and verify return to main menu with no unwanted new game.

## Gate D: opt-in native stage in a disposable campaign only

1. Create **another disposable campaign**, explicitly set **More options → Preview CU choices inside Bannerlord's 3D review = YES**.
2. Confirm CU configuration. Complete all native culture, face, narrative, banner, naming, review and difficulty screens.
3. Observe **one additional** 3D origin-review screen *after* difficulty. It must show the **same actual 3D player**, native rotation/Next/Previous navigation, with Calradia Unified start/household summary. It must **not** send you back to culture or a second face editor.
4. Press Back once, verify the native difficulty screen works again. Proceed forward and finish once. Confirm only one `sandbox_origin_creation_complete phase=9` in the current run and exact requested family size.
5. If an extra stage does not appear, check `cu_native_review_skipped` for `view_discoverable=False`. If it appears without a text overlay, check `cu_native_review_summary_fallback`. Neither condition is a game success for the extra-stage UI.
6. Crash or blank screen is a **Gate D failure**. Exit the disposable save. In a new game leave the opt-in OFF. Do not disable the entire Calradia Unified family module to bypass this test.
7. Save, fully exit and reload. Confirm native spouse and child count, kingdom, fiefs and age are unmodified.

## Gate E: genuine spouse and child portrait gallery

1. With a campaign containing real player-linked spouse and daughter, enter a town or castle.
2. Select **CU: View my household portraits**.
3. Viewer displays the player, their actual spouse, and the real daughter. Expect **3 rows** in the one-child campaign. Each lists relationship, age, current/last-known settlement and parent names.
4. Where the native engine provides visual data, the portrait must match the Clan portrait. A missing baby image is preferable to a fabricated adult or a crash. Verify the game still reports the daughter as a child with her original age.
5. Close with the **Return to settlement** button, then repeat and close using Escape. No black screen, frozen focus or disappearing native menu.
6. Set a home with **CU: Make this my family's home** in an owned fief. Reopen gallery. It must record a town/castle for spouse and baby without transferring an infant to any party.
7. Save, quit, reload and verify the same parentage and residence. Confirm no new spouse or daughter was created by opening the gallery.

## PowerShell diagnostic evidence

Download the scripts from the same pinned **source commit** as the successful artifact. With `$GameRoot` set to your actual game path, run:

```powershell
$Events = Join-Path $env:LOCALAPPDATA 'CalradiaUnified\Diagnostics\events.csv'
Import-Csv -LiteralPath $Events |
    Where-Object { $_.context -match '^cu_native_review_|^cu_family_portrait|^sandbox_appearance_engine_probe$|^sandbox_start_contract$|^cu_family_verification$' } |
    Select-Object -Last 35 utc, context, detail |
    Format-List

& $Audit -GameRoot $GameRoot -ExpectedVersion 'v0.29.0' -ExpectedDllSha256 $ExpectedHash -ExpectedChildren 1
```

Check `exceptions.csv` separately. The log is cumulative, so distinguish attempts by the timestamp of the newest `sandbox_overhaul_accepted`. A portrait-gallery action in an older save may not match the latest accepted new-game session. Never infer active save identity from CSV timestamps alone.

## Required player evidence before declaring Q2/Q3 complete

- Screen capture: revised CU start UI with role, noble spouse, daughter age and game-world selections readable.
- Screen capture: native 3D player face stage and, with opt-in on, final CU summary with same native rotating model.
- Screen capture: town/castle gallery with all three true family members and matching parent links.
- Logs of `cu_native_review_attached`, `cu_native_review_view_loaded`, `cu_family_portraits_open`, `cu_family_verification result=PASS`, and `sandbox_start_contract result=PASS`.
- A new-save **and** a reload check. No claimed live test until observed.

**Future work:** a fully unified one-screen editor with continuously visible native live player/spouse/children would require engine-backed in-game character creation and a separate character-preview backend. Creating preview Heroes in the main menu is prohibited, as it risks registered adult relatives/duplicate child state. Do not label the v0.29 work as complete Q2/Q3.
