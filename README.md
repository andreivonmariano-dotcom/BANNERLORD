# Calradia Unified v0.26.0 | Generated Spouse Activation and Family Cleanup

**Game:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Status:** Gate B COMPILED_UNTESTED. **Not yet proven to create a married spouse or a child in the running game.**

## Confirmed defect in v0.25

The user chose Lord of Vlandia, NobleRandom spouse and one female child. Two adult nobles appeared around the castle, but the player had no spouse or child in the clan. Their attached v0.25 log showed `sandbox_start_contract result=FAIL|native_type=vassal|kingdom=vlandia|fiefs=3|spouse_present=False|children=0`. `exceptions.csv` captured `Native marriage rules rejected the created spouse`. The user's installed DLL hash was the earlier successful v0.25 build `f0f99d3a2433af5dd1dc7cfbf80b65c3dc47af00c07049b25884e6c223ca2c02`.

**Source-confirmed cause:** TaleWorlds `HeroCreator.CreateSpecialHero()` leaves newly created Hero objects in `NotSpawned` state. The native `DefaultMarriageModel.IsSuitableForMarriage` requires `IsActive`. v0.25 gave new spouses Lord occupation but never activated them. The native `MarriageAction` consequently rejected the spouse and the code skipped children.

## v0.26 changes

- The new adult Hero is changed to `Hero.CharacterStates.Active` **before** `MarriageAction` and marriage eligibility testing. Younger children are never forced active.
- Player marriage eligibility is checked **before** creating any new spouse. Pure `SandboxGeneratedMarriagePolicy` and runtime diagnostics identify inactive participants, invalid clans, same-sex pair or a hero whose native marriage eligibility failed.
- Events `cu_family_new_adult_activated` and `cu_family_marriage_prerequisites` record actual before/after state and model requirements.
- On failed marriage **before** any child or spouse relationship exists, the mod compensates by removing links to chosen noble parents and calling native `DisableHeroAction.Apply`, then removing the stray hero from their clan. It does not attempt to undo an established marriage, and compensation is not a guaranteed transactional rollback.
- Generated spouse backgrounds Commoner, NobleRandom and NobleChosen remain available, with correct native `vlandia` culture IDs. Child mother/father links and two-parent body-property blending are unchanged from v0.25.

## Gate B results and download

- [Successful v0.26 Windows CI #44327989195](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44327989195): 604 policy checks, 40 campaign policy simulations, zero warnings/errors, new activation regression, package checks and installer simulation.
- [Compiled-untested v0.26 artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44327989195/artifacts/14437991751), module DLL 215040 bytes, SHA-256 `04abd30e26d87d48f246c08ace3a2f0ee1a3c6ce4011b47c1ee39c8c223faf61`.
- [Exact test and interpretation](docs/V026_ACTIVE_MARRIAGE_ACCEPTANCE.md).

**Runtime proof pending:** in a new disposable Lord/Vlandia game, actual reciprocal wife/husband in PlayerClan, one female minor with correct native parents and appearance, genuine noble natal clan, save/exit/reload with no duplicate or orphan nobles. The player's old v0.25 campaign is not repaired by this module update. Native culture, face, narrative, age and difficulty screens still appear.

To restore the native Sandbox start selector, set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`. Do not modify official game DLLs.
