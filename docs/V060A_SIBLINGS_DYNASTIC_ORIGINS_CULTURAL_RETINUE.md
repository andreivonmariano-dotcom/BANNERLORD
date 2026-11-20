# Calradia Unified v0.60a | Siblings, native noble origins, culture-specific household officers

**Status:** Experimental code and Windows-CI candidate for Bannerlord 1.5.7.125802. New-game data mutations and UI buttons have not been live-tested in Bannerlord. No assurance of an unchanged save after an untested native action. Draft PR intentionally stays unmerged.

## What is implemented

### Native-style controls and typography
The generator `scripts/generate_v059f_native_review_shell.py` still derives both movies from the **exact native Review** panel and footer. All custom action buttons have larger captions (base font 21) and a taller hit region. House Previous/Next (2 × 262 × 66) and toggle/input (381 × 66) sit within the native 573-unit recess. The six stat edit rows use controls of 66 × 55 for +/−, 101 × 55 for Exact, and larger stat names. Age year buttons are taller. The one Age slider stays intact, with no stat sliders added. Native green Continue and Back are unchanged.

### Siblings in House
The original source had `SiblingCount` and four `OriginRelative` sibling templates but did not display them in the guided House screen, and `CommitHouse` ignored sibling-only requests. House now exposes sibling count **0–4**, each sibling's name, adult age **18–80**, and sex. The selection survives the same validated pre-campaign profile and existing save path. `CommitHouse` activates real family creation for sibling-only households.

If native Sandbox supplied no parents, `SandboxFamilyCreator.EnsureSiblingParents` generates **real same-culture native Hero parents**, connects the player to them, then creates siblings sharing both father and mother. Existing native parents stay intact. Names, parent age and culture are checked and journaled. Adult new sibling Heroes are placed in the player's **actual party** and checked in its troop roster. The mod refuses a request rather than creating unrelated heroes labeled "siblings".

### Younger brother / younger sister of an older native lord or king
The new House `NobleBirthPosition` has Independent, YoungerBrother/YoungerSisterOfLord/King, SecondSon, and ThirdSon noble/royal family variants. Its display text is readable and distinguishes a **royal-house younger son** from a newly created king. The player must select **Lord** as the native starting position, and join the chosen kingdom through Bannerlord's real Advanced Start. The option **does not make the player the king or fake an inheritance rank**.

At the one-shot campaign start, `SandboxDynasticStart` searches real living native noble clan leaders of the same kingdom/culture with both parents known, older than the player. The user can specify an exact elder Hero ID in optional choices. For a third son, the original parents must already have at least two older living sons. The selected Hero is **not cloned or moved between clans**. The player is linked to the real same native mother and father, added to their child lists and gains a native +35 relation change with the elder. The original player vassal clan, kingdom and fiefs remain as the native Advanced Start built them.

**Explicit refusal:** The mod does not invent the elder's parentage, retcon native kings without verified parent Heroes, or silently transfer the player into the ruling clan. If no eligible elder exists, a journal event `cu_dynastic_native_kin_refused` and player-facing failure notice are produced. The character remains a normal lord with no fabricated elder relation.

### Native, culture-specific household officers
House now provides **Native**, **RoleBased**, and **ReplaceNative** retinue modes. `Native` is default and changes no game companions. RoleBased creates officers using **same-culture native lord templates**, falling back only to a local living noble's CharacterObject as a **cloned source**, never recruiting that existing lord or a random wanderer. It uses real `HeroCreator`, `AddCompanionAction` and `AddHeroToPartyAction` actions, checks the officer's clan/culture/party membership, and creates a diagnostic event.

The job list depends on the starting role and whether the player has a **real adult brother in their party**:

- **Lord:** if brother serves in party, Knight + Household Steward. Otherwise Master of Arms + Knight.
- **King:** if brother serves in party, Knight + Chamberlain. Otherwise Marshal + Master of Arms.
- **Knight:** Squire.
- **Admiral:** First Mate + Navigator.
- **Soldier:** Sergeant.
- **Trader:** Factor.
- **Outlaw:** Second in Command.
- **Wanderer/Beggar:** no artificially generated noble retinue.

Office names are deterministic and culture-specific, with surnames/roles written as identity rather than recycled tavern NPC identities. Native models' actual resulting occupation and membership are logged. If no same-culture noble source exists, creation refuses rather than borrowing a mismatched wanderer.

**ReplaceNative:** the player explicitly authorizes firing native starting companions using `RemoveCompanionAction.ApplyByFire`, then replacing them with the selected role's officers. This can affect real in-game relationships and is **experimental**. Native mode preserves every companion. This is only performed after the native start has committed and exactly once per new campaign.

## What is NOT implemented in v0.60a

**Orders from an elder sibling and army commissioning are not yet implemented.** The actual elder Hero ID is written into the campaign save (`CU_v060_dynastic_elder`) for a future, save-scoped commission system. No fake "command army" rank, instant army, AI lord teleport, bypassed kingdom influence or automatic war order is issued in this candidate. A follow-up native campaign behavior will need an elder-generated order, explicit player acceptance, the native game army/influence checks, and cooldown/revocation on elder death, faction change and save/reload. Historical GUI styling remains sourced from the installed game.

## Verification criteria

Windows CI compiles against the pinned assemblies and runs the source-level regression `scripts/test_v060_noble_start_and_retinue.py` and independent `Tests/PolicySmoke` lineage and retinue tests. Static tests check profile persistence, a real shared-parent link, same-culture donor selection, safe fallback/opt-in replacement, native faction gating and larger action hit targets.

Game QA on an expendable new Sandbox save:
1. Try **independent Wanderer** (Native companions preserved), **Lord + sibling count 0–4**, and **Lord + RoleBased retinue**.
2. Check each sibling in the encyclopedia, player clan and player party, both parents and correct culture/age.
3. Compare Lord starts with/without a real brother in the party. Confirm correctly titled Knight / Master of Arms / Steward, identical culture, not a randomly selected tavern wanderer.
4. Try **ReplaceNative** separately, preserving a backup save. Check exactly which original companions were dismissed and that no native character went missing.
5. Choose kingdom, Lord start and an elder king/lord sibling. Verify the existing elder has the required real parents, the player and elder share them, the elder's original clan/kingdom remains unchanged, and the new player remains a proper vassal. Test an invalid ID and confirm a clean refusal.
6. Test all three House Back/Continue paths, final Review/Name Back → House, Save/Reload, and several campaign daily ticks. Check application logs for `cu_dynastic_native_sibling_bound`, `cu_sibling_parents_created`, `cu_family_sibling_created`, `cu_cultural_retinue_officer` and `cu_cultural_retinue_status`.
7. Confirm the larger controls do not overlap, buttons remain clickable, the Age slider continues dragging, and All Stats remains button-only.

**Do not merge or promote until the new campaign actions are verified in-game.**
