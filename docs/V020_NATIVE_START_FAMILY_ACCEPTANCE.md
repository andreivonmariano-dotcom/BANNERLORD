# Calradia Unified v0.20 | Actual native starting roles and family application

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Status:** Compiled-untested candidate until the exact workflow artifact and user gameplay evidence establish Gate C/D/E. Player report: [#43](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/43).

## Correcting the v0.19 user-reported failure

The v0.19 pre-game editor accepted a Lord and family profile, then constructed the game from *unchanged* AdvancedStartOptions. Bannerlord therefore chose the default start. The v0.19 behavior also ran too early in the new-game lifecycle, and its family-creation toggle remained disabled by default even after a player chose a spouse or children.

In v0.20 `SandboxNativeStartOptions.TryPrepare` translates the editor selections into the engine's real v1.5.7 Advanced Start identifiers **before constructing the campaign**:

| CU role | Native StartType |
|---|---|
| Wanderer | default |
| Soldier | mercenary |
| Knight | vassal |
| Lord | vassal |
| King | king |
| Admiral | fleetadmiral (only if an active game module provides it) |
| Trader | trader |
| Outlaw | outlaw |
| Beggar | beggar |

King/Lord/Knight/Soldier/Admiral require a valid `StartingKingdomId` from the native kingdom picker. The player has to pick one on the Identity page. An unavailable type (such as Admiral without an installed provider), kingdom or unsupported world scenario displays a refusal and does not start the game. Roles map directly to the native start data rather than forging gameplay status from a label. The normal vanilla ASO UI lock predicate is not used: this total-conversion creator directly passes a recognized option string to native Advanced Start.

**Other Advanced Start choices** are under Location, even though the Location tab label is temporary: native Scenario (Default, United Empire, Last Stand, Invasion, Two-Faction War, Alternative Calradia), matching scenario-faction selectors, Alternate Calradia variant, fast mode, bandit surge, civil unrest, recruit shortage, swift travel, and an optional numeric seed. These use engine-provided option keys. No scenario or native item is hardcoded as an in-game side effect. A chosen world-scenario faction must exist and pass validation.

**Actual character creation order** (Bannerlord 1.5.7):

1. CU opens before Default/Advanced selection. Player chooses Role, Kingdom and profile.
2. CU creates `AdvancedStartOptions` with selected values, then constructs the real `SandBoxGameManager`.
3. Bannerlord runs its intro and native culture/appearance/background character creation.
4. Native `CampaignAdvancedStartingPlayerOptionsCampaignBehavior` applies political start at `OnCharacterCreationIsOverEvent` index 8.
5. CU's `SandboxOriginBehavior` runs its remaining one-shot origin changes only at event index 9, with an optional post-completion daily retry.
6. A requested spouse or children set `RealFamilyEnabled=true` when Apply is clicked. The native `HeroCreator` and `MarriageAction` produce real heroes, subject to native template and age checks. Children require a spouse. Siblings require existing native parents and are refused from a new custom profile in this iteration. The code never silently discards a requested sibling count.

## Known missing systems

- Native **culture, appearance, background and finalization screens still appear**. A complete custom replacement of these screens is NOT implemented by the v0.20 start fix.
- `StartingCulture` still expresses a preference and does not replace choosing culture in the native wizard.
- `PlayerAge`, `StartingTown`, `EquipmentKit`, and specific family appearance presets are not guaranteed as game effects. Item IDs, gold, troops, skills and focus are attempted at phase 9. Every one requires runtime verification.
- Family creation can fail on native template resolution, marriage/gender restrictions, actual age or hero ownership. The event log records a reason and on-screen notice. Partial creation is not rolled back. Use disposable new campaigns until verified.
- The custom editor has readable clickable controls but a primitive visual treatment. A polished Bannerlord-style UI with a live character preview is distinct work. Do not treat compiling XML as proof of rendering.
- Real Rank/King and scenario options have not been tested inside Bannerlord. Save/load persistence remains unverified.

## Required v1.5.7 runtime test

1. Close the game. Install *only* the actual v0.20 compiled artifact in `D:\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\CalradiaUnified`. Verify version, DLL hash and prefab using v0.20 preflight.
2. In a disposable new Sandbox, select **Lord** and **Starting kingdom: vlandia**. On Household, select **Begin married: true**, choose an opposite-gender spouse and name/age. Keep child count 0 initially. Go to Review and Apply.
3. Observe the native culture/appearance/background flow. At the campaign map, open Kingdom/Clan. Verify membership in Vlandia, real vassal status and native startup property. Verify a real spouse in Clan or Encyclopedia. Character class/name alone is not proof.
4. Close the game, re-open the same save and repeat the checks. Verify no duplicate relatives or troops. Repeat with 1 valid child only after spouse passes.
5. Read `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv` and `exceptions.csv`. Expected events include `sandbox_overhaul_accepted` (role, kingdom, family request), `sandbox_direct_game_start_requested` (actual ASO type), `sandbox_origin_creation_complete` (phase 9), `cu_role_affiliation_status` (actual native status), `cu_family_creation_status` (created or explicit failure), and `sandbox_origin_applied`.
6. Test King and world-scenario options separately using disposable saves. Compare native results, report any missing assets or early exception.
7. Test `OverhauledCreatorEnabled=false` as opt-out to the untouched native Default/Advanced picker.

**Acceptance:** The user must genuinely start as a vassal or king when selected, a requested spouse must appear in the actual clan family, and saved campaigns must reload. A build or a log message alone does not meet this condition.

## Source references

- `Source/CalradiaUnified/SandboxNativeStartOptions.cs`: validates/marshals role, faction and native world options.
- `Source/CalradiaUnified/SandboxCreatorOverlay.cs`: pre-game form, rejection messages, configured launch.
- `Source/CalradiaUnified/SandboxOriginBehavior.cs`: finalization phase gating and one-shot results.
- `Source/CalradiaUnified/SandboxFamilyCreator.cs`: native hero creation and marriage.
- `scripts/test_native_start_handoff.py`: static regression audit, not gameplay proof.
