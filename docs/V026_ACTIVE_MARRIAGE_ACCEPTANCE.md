# v0.26 | Activate generated spouse before native marriage

**Game target:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Status:** Compile and source-level candidate; game, marriage, save and Clan-panel acceptance must be established separately.

## The player's exact observed defect

The user selected Lord → Vlandia → generated random noble spouse → one daughter. They reported two adult female nobles in their castle but no wife in their clan and no daughter. Their uploaded diagnostic report included:

- Installed v0.25.0 DLL hash `f0f99d3a2433af5dd1dc7cfbf80b65c3dc47af00c07049b25884e6c223ca2c02` (earlier v0.25 CI candidate).
- `sandbox_start_contract result=FAIL|role=Lord|native_type=vassal|kingdom=vlandia|fiefs=3|fief_request=already_owned:castle_V3|spouse_present=False|children=0|failures=requested_spouse_missing,requested_children_missing`.
- `exceptions.csv`: `cu_family_partial_creation System.InvalidOperationException: Native marriage rules rejected the created spouse`.

This establishes **a failed marriage**, not two created children. The complete family event stream was not displayed because PowerShell sent `else` clauses as independent commands. Do not assume the exact source of both observed adult nobles from that abbreviated log. The candidate spouse was created, marriage refused, then children were not attempted.

## Verified engine cause and code correction

The version-reference native `HeroCreator.CreateSpecialHero` calls the native `Hero` constructor and initializes the character's clan, name, occupation, body, traits and equipment. The Hero constructor assigns `Hero.CharacterStates.NotSpawned` initially. The native `DefaultMarriageModel.IsSuitableForMarriage` explicitly requires **`hero.IsActive`** along with adult age, `IsLord`, no existing spouse, and other conditions. `MarriageAction.Apply` silently returns without assigning spouse refs for unsuitable couples.

v0.25 `SandboxFamilyCreator.Create` set `Occupation.Lord` but failed to call `ChangeState(Active)`. This is a **source-established cause** of the marriage rejection. A freshly registered adult in NotSpawned state cannot pass the native marriage model.

v0.26:
1. Preflight `player.CanMarry()` **before** generating any hero, rejecting unsupported player marital state without leaving a new spouse.
2. Create a brand-new spouse using the requested Commoner/NobleRandom/NobleChosen birth origin, native source template/clone, requested sex, age and name.
3. For every **adult newly created Hero**, set Lord occupation, call `hero.ChangeState(Hero.CharacterStates.Active)` if needed, and record `cu_family_new_adult_activated` with before/after states. Do **not** activate minor children.
4. Verify explicit native marriage prerequisites: both active, each clan suitable, opposite gender, both eligible. Report `cu_family_marriage_prerequisites` and `cu_family_marriage_gate`, then call native `MarriageAction.Apply`.
5. Require real reciprocal `player.Spouse/spouse.Spouse` references and generated spouse membership in `Clan.PlayerClan` before creating a child.
6. A native rejection **before any marriage or children** now calls `CleanupUnmarriedGeneratedSpouse`: removes the new hero from the chosen natal father/mother's child lists, clears parent references, calls native `DisableHeroAction.Apply`, removes the hero from its clan and records `cu_family_orphan_cleanup`. This is limited **compensating cleanup**, not complete rollback of arbitrary engine side effects.
7. Once married, create requested minor children via native offspring template, set actual Mother/Father, then compute `GetStaticBodyProperties(child,true)` using both parents' face data. Children are left in their normal minor state.
8. Report the final `cu_family_verification`, `cu_family_creation_status`, and `sandbox_start_contract`. **Game-test before declaring PASS**.

## Required runtime proof

1. Close Bannerlord. Install the exact successful v0.26 GitHub compiled artifact. Verify the DLL SHA against that workflow. The earlier user install hash `f0f99...` is **not** this v0.26 DLL.
2. Start a **new disposable Sandbox**, choose Lord, Vlandia, Fief Mode Native, and one generated spouse with `NobleRandom`. Set spouse age at least 21 and one female child aged 7 or younger, subject to final player/spouse ages.
3. Reach the campaign map. In `events.csv`, check `cu_family_new_adult_activated` shows `previous_state=NotSpawned|state=Active`. `cu_family_marriage_gate` must show `native_eligible=True`. `cu_family_spouse_created` must have the new spouse's ID and player's clan. `cu_family_inherited_appearance` should show mother and father IDs.
4. Open Clan and Encyclopedia: wife/husband is **a newly created active Hero** in the player's Clan with reciprocal spouse refs. Exactly **one minor daughter** has both actual parent links, correct age and inherited face. The native vassal-start wanderer companions are **not children**.
5. Save, quit Bannerlord, reload. Verify spouse, child, natal father/mother and birth-clan relationships all persist and no family member duplicates appear.
6. Test `Commoner` separately to confirm same-clan generated spouse can marry. Test chosen noble clan only with a known exact clan ID and eligible parent pair.
7. If failure, export the newest `cu_family_*` events and exceptions. `cu_family_orphan_cleanup` should show whether a rejected generated adult was disabled/detached. Do not keep testing a partially modified save.

**Previous saves:** A v0.25 save already containing unattached nobles is **not repaired** by installing v0.26. A deliberate migration/cleanup tool would require locating the exact generated Hero IDs and verifying their references. Do not mass-delete people from the user's save.

**Acceptance limits:** Compilation only proves source compatibility. A daughter who remains an adult, spouse who is still NotSpawned, missing clan references or any rejected marriage means the feature is not accepted. Native culture/appearance/background screens still appear after the CU pregame editor.
