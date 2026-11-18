# v0.17.0 native Sandbox Starting Life integration

**Build target:** Bannerlord PC v1.5.7.125802. **Status:** Experimental, Gate B pending CI. No actual in-game integration or save/load test has been run for this candidate.

## Q1. Native Starting Life pages

The standalone main-menu creator stays removed. A character-creation content handler uses the game's existing `CharacterCreationManager` and `NarrativeMenu` system. With `NativeStageEnabled=true`, it finds the unique native age narrative menu, reconstructs that menu with identical options and visuals, then connects four additional native pages:

- **Starting identity:** Wanderer, Soldier, Knight, Lord, King
- **Household:** alone, married, one child, two children, single parent, siblings
- **Resources:** game default, 500, 2,000, 12,000, 50,000, 250,000 gold presets
- **Equipment:** Vanilla, Traveler, Soldier, Knight, Royal, Custom

These pages use **native narrative Back/Next**, not custom Gauntlet buttons. The code checks for a single compatible age menu and rolls back added menu IDs on registration failure. Its current version of the native graph must be verified on the running target game. The new stages are disabled by default, not yet proven visually.

Other exact skill, trait, spouse name, individual child names, and item IDs remain valid in the existing origin profile. They are not yet all separate interactive native wizard controls.

## Q2. Actual family heroes (opt-in)

`RealFamilyEnabled=true` uses native `HeroCreator.CreateSpecialHero` and `MarriageAction.Apply`, not transient troop rosters. The helper attempts to create a spouse and up to six children in the player's clan with configured names, genders, ages and cultures. Children's real `Father`, `Mother`, and parents' `Children` references are set. An explicit existing CharacterObject template ID, if given in a `FaceSeed` field, selects a matching pre-existing cultural and gendered appearance. This is a template selector, not a facial morph/slider system.

Siblings are created only if the player already has a native father or mother hero. Otherwise the helper refuses to create misleading pseudo-siblings. Prevalidation checks that all requested templates exist before creating any family member. It refuses a pre-existing spouse, pre-existing children, and siblings without actual native parents. Native same-gender marriage is held back until the underlying marriage action is verified with the exact target version. The one-shot new-game flag prevents an attempted partial creation from being repeated by a daily tick or save/load. It **does not provide a transactional rollback** for a partial creation failure. For that reason, real family hero creation must be verified inside the game and remains disabled by default.

## Q3. Real role differences (opt-in)

`RoleAffiliationsEnabled=true` uses native political actions and differing recruit/renown policies.

| Identity | Actual effect attempted |
|---|---|
| Wanderer | Independent clan preserved; no affiliation is created |
| Soldier | Native **mercenary clan** contract with a matching-culture kingdom, not rank-and-file enlistment |
| Knight | Native **vassal clan** affiliation, starting martial reputation and two basic recruits |
| Lord | Native vassal status when possible; a personal fief or estate is **not** fabricated |
| King | Checks the player is already the ruling clan of a native kingdom; does not create a kingdom or bypass locked native Advanced Starting Options |

A soldier contract and knightly vassalage are genuine campaign affiliation mechanics, but neither implements a complete army-rank hierarchy or a personalized feudal estate. All five role selections are represented within the native narrative page, and gameplay effects are isolated behind the second opt-in.

## Acceptance and restrictions

- Do not assume an exact native menu can be safely relinked until a v1.5.7 game test proves it.
- Confirm each parent-child hero survives save/reload and remains in the expected clan.
- Confirm the native political actions fire only for enabled new games and are not repeated.
- Test the native Back button through all custom pages and backward into original age/face/history screens.
- Save/load proof and actual hero visuals require screenshots and native diagnostics from a disposable campaign.

**Production status: NOT RELEASED. No game runtime acceptance has been claimed.**

## Exact experimental setup

Create or edit `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` prior to a new Sandbox with `Version=CU_ORIGIN_1`, `Enabled=false`, `NativeStageEnabled=true`, `RealFamilyEnabled=false`, and `RoleAffiliationsEnabled=false`. The native pages appear only if the v1.5.7 graph exposes exactly one compatible age narrative menu. The game retains the original Sandbox entry, not a separate Calradia Unified main-menu creator. Selecting a role sets Enabled true. Family and role contracts require separate choices in the new pages. Stop at the first UI error and preserve the diagnostic ZIP for analysis.

Individual relative names and detailed attributes are presently editable through the separate profile format, not complete native portrait or text-entry stages. Labeled equipment kits do not currently auto-equip a character. Starting settlement relocation and direct facial sliders remain unfinished. This is an experimental integration milestone, not all requested Sandbox functionality completed.
