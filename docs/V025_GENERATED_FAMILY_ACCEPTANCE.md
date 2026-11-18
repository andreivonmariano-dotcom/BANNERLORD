# Calradia Unified v0.25 | Generated spouses, noble origins and inheritable child appearance

**Target:** Bannerlord PC v1.5.7.125802. **Status:** reference-compiled candidate only after successful CI. No in-game spouse, natal clan, children, portrait or save/reload evidence yet.

## Why v0.25 exists

The user's v0.24 runtime logs isolated the reproducible family failure: `cu_family_template_scan status=culture_missing|id=Vlandian`. The UI had accepted the English display adjective **Vlandian**, but the game's CultureObject ID is `vlandia`. The family helper exited before any spouse and children existed.

The same live logs proved a real `Lord/vassal` or `King` start, actual Vlandian allegiance and existing fiefs. Two starting wanderers are ordinary native Advanced Start companions, never children. Keep this working behavior.

**v0.25 fixes this input boundary**: `Vlandian → vlandia`, `Sturgian → sturgia`, `Battanian → battania`, `Imperial → empire`, and plain game IDs remain unchanged. The saved profile parser normalizes an existing `SpouseCulture=Vlandian` entry. Quick Setup's spouse-culture control is an advanced choice list of real IDs, not free text.

## An entirely newly generated spouse

The family pipeline does **not** marry an existing native lord or wanderer. It calls `HeroCreator.CreateSpecialHero`, which creates a **new** Hero and copied CharacterObject from a compatible native donor template. The donor is never removed from an existing clan, renamed, or forced to marry the player. Legacy `SpouseFaceSeed` values are ignored in generated mode.

Three background choices in **Household**:

- `Commoner`: generate a new spouse in the player's clan, without noble parentage or a different noble origin clan. The native marriage model requires a Lord-occupation hero and a valid clan, so the generated spouse still uses that technical occupation for native marriage. Commoner is **social origin**, not a special native occupation flag. This distinction is part of the design.
- `NobleRandom`: choose a qualifying real noble clan from the player's **actual starting kingdom**. Find a married native mother/father couple in the same clan, each at least 16 years older than the requested spouse age. Generate a *new adult Hero* as their child, with both native parent-child relationships, a native `OriginClan` pointing to that birth clan, and the user's selected name/age/culture.
- `NobleChosen`: type the exact native birth-clan `StringId` in `SpouseBirthClanId`. That clan must exist in the starting kingdom, not be a bandit/rebel/minor/defunct clan or the player's own, and have a qualified native married parent pair. If not, the creator reports a refusal rather than inventing genealogy or silently choosing another clan.

The chosen name belongs to the newly generated person. After creation, native `MarriageModel.IsCoupleSuitableForMarriage` checks eligibility, then `MarriageAction.Apply(player,spouse,false)` establishes actual spouse references and moves the spouse into `Clan.PlayerClan`. The new Hero's original noble family remains recorded through `OriginClan` and `Father/Mother`. `cu_family_generated_spouse` and `cu_family_noble_origin_selected` record the actual birth, clan and parents; `cu_family_marriage_gate` and `cu_family_verification` record outcomes.

**Chosen-family interface limit:** the pre-campaign CU menu does not yet have an active Kingdom or enumerated native clan data. It currently accepts an *exact clan ID*, not a searchable gallery of clan names or portraits. A later in-campaign picker is required for a convenient user-facing gallery. The validation rejects unknown or inappropriate clan IDs.

## Children really inherit both parents' appearance

Native `HeroCreator.CreateChild(template,...,age)` creates a new Hero, but it initializes with `isOffspring=false`. Merely assigning mother and father afterward keeps the donor's default face.

v0.25 deliberately **changes that order**:

1. Request the offspring template from `Campaign.Current.Models.HeroCreationModel.GetCharacterTemplateForOffspring(mother,father,gender)`.
2. Create a fresh child with the specified age and name via `HeroCreator.CreateChild`.
3. Link `child.Mother` and `child.Father` to the real player/spouse heroes. Native setters register the child in each parent's children collection.
4. Call `HeroCreationModel.GetStaticBodyProperties(child,true)` only **after** both links exist. Bannerlord's default model uses `mother.BodyProperties`, `father.BodyProperties`, the child's sex, cultural hair/beard options and random variation to generate the child's inherited face. Assign the generated static properties back to the child's Hero.
5. Log `cu_family_inherited_appearance`, then verify native parent and child references.

The native blend is probabilistic. It does not promise an exact 50/50 face resemblance, identical eye shape, predictable hairstyle or custom genetics sliders. It reuses Bannerlord's verified-offspring generation rule. The child's current age still affects the rendered model. No family faces are copied from existing, unrelated companions.

## Required runtime acceptance

1. Install the ZIP from a **successful v0.25 Windows CI artifact**, verify its module version and exact compiled DLL hash. Install to `D:\Steam\steamapps\common\Mount & Blade II Bannerlord`, not an old v0.24 file. Only disposable new campaigns.
2. First run: Lord, Vlandia, spouse generated as **Commoner**, spouse gender Auto, spouse age 30, two children ages 7 and 8, Native starting fief. Confirm the former `culture_missing|id=Vlandian` error no longer occurs and both native spouse and children appear in the Clan tab.
3. Confirm `sandbox_start_contract result=PASS`, `cu_family_generated_spouse`, `cu_family_spouse_created`, `cu_family_child_source`, `cu_family_inherited_appearance` and `cu_family_verification result=PASS`. Check the actual hero names, mother/father references and source clan, not just the event labels.
4. Save, quit the entire game, load the exact same save. The correct spouse, two children, ages, bodies and kingdom/fiefs must remain; there must be no duplicate relatives. Save/load remains Gate E and is **unproven** until the player reports it.
5. Second run: Lord, Vlandia, `SpouseBackground=NobleRandom`. Confirm generated spouse's actual `OriginClan` and `Father/Mother` belong to an existing qualified Vlandian clan. Native marriage moves spouse to PlayerClan. If no qualifying couple exists, a clear `cu_family_origin_refused` event is a valid refusal, **not** a pass.
6. Third run: `NobleChosen` with a valid real clan ID known from the game, and an invalid clan ID. The first should create a native child of that clan's valid parent pair. The invalid ID must produce `chosen_noble_clan_not_in_starting_kingdom` without creating any stray Hero.
7. Screenshot at the user's actual display resolution confirms the Household choices and the campaign-plan summary are readable. Native culture, appearance, background, age and difficulty screens **still remain** in this release line.

**Known risks:** Generated spouse native marriage rules may still reject a couple; the existing family helper does not provide transactional rollback after a partial hero creation. The noble family's selected parents can be unavailable or too young. The player must test with a disposable new campaign. Reference compilation alone is not gameplay proof.

## Source responsibility

- `Source/CalradiaUnified/SandboxFamilyOriginPolicy.cs`: culture aliases and generated-family acceptance policy.
- `Source/CalradiaUnified/SandboxGeneratedSpouseOrigin.cs`: chooses a real kingdom clan and qualified married native parent pair.
- `Source/CalradiaUnified/SandboxFamilyCreator.cs`: generates new spouse, native marriage, parent-derived child appearance and verification.
- `Source/CalradiaUnified/SandboxOriginProfile.cs`: serialized background and birth-clan fields; normalizes legacy Vlandian text.
- `Source/CalradiaUnified/SandboxCreatorOverlay.cs`: fields, conditional chosen-clan ID control and summary.
- `scripts/test_generated_family.py`: static contract, not in-game acceptance.
