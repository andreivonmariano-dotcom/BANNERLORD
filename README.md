# Calradia Unified v0.24.0 | Family source recovery and readable Sandbox creator

**Game target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Release status:** Development candidate. Reference compilation, policy checks and packaging are not proof of in-game family or visual acceptance. [User-reported failure #50](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/50).

## What the real game has proved

v0.23 entered a working Lord/Vassal start, Kingdom Vlandia, with three reported fiefs. The user had chosen a wife and two children, but `cu_family_creation_status requested=True|result=spouse_template_missing` occurred. The family pipeline returned before creating the wife or any children. The start contract correctly reported both failures.

**Two starting wanderer companions are normal for a Vassal start**, not failed children. Bannerlord's own `CampaignAdvancedStartingPlayerOptionsCampaignBehavior.StartGameAsVassal` calls `GiveStartingCompanions(2, random)`. The custom family feature must create relatives as real Hero family links, not party companions.

A user-provided v0.23 screenshot shows the old interface text floating directly over a bright cinematic, invisible frame backgrounds, unclear choices and misleading `family: off` status.

## v0.24 corrective code

### Spouse and children
- Broader native source lookup: prefer matching culture/sex Lord/Wanderer templates, any matching template, then clone an existing native same-culture Lord's CharacterObject. If unavailable, use a gender-matched native template or cloned Lord of another culture, then assign the intended new spouse's culture. The donor is never reassigned or removed.
- `cu_family_template_scan` records culture, gender, template counts and exact fallback. `cu_family_marriage_gate` records the native marriage eligibility, player and spouse ages and clans. `MarriageAction` still owns actual marriage.
- The requested children now use `HeroCreationModel.GetCharacterTemplateForOffspring(mother,father,sex)` and `HeroCreator.CreateChild` with selected ages. Native mother/father/child/clan references are verified after creation.
- `cu_native_companion_snapshot` records ordinary native companions **before** the family attempt, separating the two Vassal recruits from real relatives.
- **No claimed successful in-game marriage or save/reload.** Native marriage rules can reject a created spouse; partial results are logged and the game should be tested only in a disposable new campaign.

### Readable six-step layout
- Real tinted native `BlankWhiteSquare_9` backing planes dim the cinematic. Solid dark-blue/steel panels, bronze highlights, a left navigation bar, wide value/help panel and separate right-hand campaign summary replace the floating text.
- The right summary displays chosen role, kingdom, spouse requested and children count, land and world setup. It does not wrongly say `family: off` before Apply.
- Back, Continue and Begin Campaign are explicit and consistent. `More options` hides detailed controls by default.
- The Appearance step is informational in Quick Setup; the native 3D culture/face editor still follows, so no option falsely promises a working independent face editor.
- Actual Gauntlet rendering and click behavior must be tested at the player's game resolution.

## Test and installation

- [Detailed v0.24 runtime acceptance procedure](docs/V024_FAMILY_UI_ACCEPTANCE.md)
- [Windows v1.5.7 CI workflow](.github/workflows/bannerlord-build.yml)
- [Package installer, backs up existing CalradiaUnified module](scripts/Install-CU-From-Artifact.ps1)
- [User-reported family and UI bug](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/50)
- [Long-term full one-screen character creator](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44)

**Do not load an important existing save.** First test new Lord/Vlandia with a spouse and two validly aged children. The native culture/appearance/background/age/difficulty sequence is still present. Confirm a real spouse, real parent-linked children and persistence after saving/reloading.

To fall back to the native Default/Advanced Start selector, set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`.
