# Calradia Unified v0.25.0 | Generated Sandbox families and inherited appearance

**Game target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Status:** **COMPILED_UNTESTED**. Do not merge or release based only on a reference build.

## Why this version exists

The user's installed v0.24 logs exposed an exact culture-name mismatch: `cu_family_template_scan status=culture_missing|id=Vlandian`. The engine expects `vlandia`, not the adjectival label. That stopped the wife and children before hero creation. Existing Vlandian King/Lord starts, factions and fiefs worked.

v0.25 corrects this alias (plus other native culture adjectives), lets players **generate an entirely new spouse**, and gives children their appearance from the native mother/father blend. Two random wanderers appearing on Lord starts remain vanilla Advanced Start companions, not family.

## Quick Setup: generated spouse

Choose Household → Begin Married → spouse name, age and gender (Auto or explicit), then **Generated spouse's family background**:

- **Commoner**: a fresh Hero with no noble ancestral family. A Lord-type occupation and player's clan are still used internally so Bannerlord's native marriage action can recognize and marry them.
- **NobleRandom**: select a real noble clan in the player's actual starting kingdom that has a qualifying married parent couple. A new spouse is generated as their child and records their birth clan, father and mother. Marriage moves the new Hero into the player clan.
- **NobleChosen**: type a valid exact `SpouseBirthClanId` in the same kingdom. That family must have real eligible parents. Missing/unknown IDs are refused. A searchable clan-name picker is not implemented at this pre-campaign stage.

The new spouse is **never an existing hero renamed or married**. Native donor CharacterObjects provide valid race/sex/appearance data, but `HeroCreator.CreateSpecialHero` registers a unique new hero. Previous `SpouseFaceSeed` fields are not used in generated-family mode.

## Children inherit both parents' looks

After both parent heroes exist, each new child receives genuine father/mother links and then `HeroCreationModel.GetStaticBodyProperties(child,true)`, which blends native parental body properties with random variation and cultural appearance rules. This explicitly corrects the prior `HeroCreator.CreateChild` path that left a non-inherited donor face.

The native 3D culture/appearance/background/age/difficulty screens still appear after the pregame CU form. A full single-screen creator remains issue #44.

## Build and test status

- [Successful Windows build #44322044399](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44322044399): **599 policy checks**, **40 simulated campaigns**, **0 compiler errors/warnings**, package integrity and nested installer tests passed.
- [Compiled v0.25 test module](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44322044399/artifacts/14434014831), DLL SHA-256 `f0f99d3a2433af5dd1dc7cfbf80b65c3dc47af00c07049b25884e6c223ca2c02` (212480 bytes). This is a **test build**, not a game-verified release.
- [v0.25 exact in-game acceptance procedure](docs/V025_GENERATED_FAMILY_ACCEPTANCE.md).
- [Underlying family culture mismatch](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/50).
- [Source and CI](.github/workflows/bannerlord-build.yml).

**Important:** Test only on disposable new campaigns. Verify actual spouses, parent links, origin clan, visually inherited child faces and saving/reloading. Noble clan selection and the native marriage action can still refuse a request; no automatic rollback exists for partially registered heroes. Default and Advanced entry can be restored using `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`.
