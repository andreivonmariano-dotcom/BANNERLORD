# v0.40 development: post-origins native stats, focus, and personality

**Target:** Bannerlord PC `v1.5.7.125802`.
**Source branch:** `feature/v040-post-origins-stats-traits`, derived from the v0.39 automatic creator bridge.
**Release grade:** gate B until actual in-game input, visuals, stage order, saves and reloads pass.
**Manifest:** retains `v0.38.0` for the current development lineage.

## Final player intention, and what this milestone actually changes

The long-term Calradia Unified start remains a short-screen, five-chapter character creator with consistent native-style UI:
1. **Your World:** world scenario, role, and starting affiliation. This must precede campaign construction.
2. **Your Character:** culture, identity/age, real 3D face, family origins, childhood, youth/adulthood, **starting stats and traits**.
3. **Your House:** banner, clan identity, spouse, and dependent children.
4. **Your Fortune:** resources, fiefs and family residence.
5. **Begin Your Legacy:** native difficulty and final review, then one campaign start action.

The 5-chapter UX reorganization, native appearance/age bridge, separate family pages and uninterrupted map entry are **not yet fully implemented**. In the current branch the pregame five-tab front end and native stages still run. Do not confuse this stats work with the complete replacement.

**Implemented in v0.40:** one additional native-themed, full-screen `CUNativeStatsEditor.xml` overlay. It opens automatically **after the original native narrative completes and when the native banner stage first activates**, before showing the original banner controls. The real Bannerlord `CharacterCreationManager` remains the sole owner of all stages. The overlay never calls `RemoveStage`, creates an extra CharacterCreationState, or alters the live Hero during drafting.

## The three requested features

### Q1: default, balanced, unrestricted

- **Native (default):** never apply a stat override. Current life-story bonuses remain game-owned. Every new campaign resets to Native and clears old overrides.
- **Balanced:** fixed **30-point bonus budget** using the real Hero values available at the editor boundary as the initial reference. Raising an attribute by 1 costs 4 points; raising focus by 1 costs 3; raising a skill by up to 10 costs 1; changing a personality trait by one level in either direction costs 3. Lowering skills or attributes cannot create refund points. The UI rejects plans that exceed the cap. A fresh check runs at phase 9 against the final native Hero.
- **Unrestricted:** customize the same supported six attributes, 18 skills, focus points and five traits without a bonus budget. Legal native value bounds still apply. Exact-number entry is available for all fields.

Any untouched field keeps its original native value. Reset All clears drafts; Native mode clears all overrides. Changes are not granted inside the Gauntlet movie. The profile saves after final confirmation, then `SandboxOriginBehavior.ApplyPlayerProgression` applies the validated plan once **after native ASO phase 8**, at character creation completion phase 9.

### Q2: show real native origin bonuses before edits

The actual pinned v1.5.7 assembly metadata exposes `CharacterCreationManager.SelectedOptions` as a private `Dictionary<NarrativeMenu,NarrativeMenuOption>`; the option type exposes `PositiveEffectText`. `NativeNarrativeBonusReader` **reads this exact game-owned dictionary without altering it** and offers paginated effect text in the right-hand column. No hardcoded cultural/childhood reward tables are fabricated.

`CUNativeStatsSnapshot.Capture(Hero.MainHero)` reads current attribute, skill, focus and trait values from the actual Hero. These are the values visible at the narrative-to-banner boundary. **Important:** late native character-creation consequences may change the values again. The phase-9 budget check uses the actual final Hero values; the overlay must not claim that a mid-creator snapshot is the final saved character.

If the private field or Hero is unavailable at runtime, the UI either clearly states the missing bonus history or leaves the native banner flow intact. It never invents bonuses.

### Q3: personality trait help and NPC relationship effects

Five recognized native trait keys: **Mercy, Valor, Honor, Generosity, Calculating**, each in `-2..2`. The trait editor shows plain-language information about the character quality and says when the engine may check it. Trait and NPC relationship effects depend on individual native dialogue choices and events. There is **no universal numeric relationship change** for setting a trait, and the UI makes no such promise.

## Short-screen and input rules

The overlay has three small sections: **Attributes**, **Skills & Focus** (six groups, by related attribute), and **Traits**. Each row displays `NATIVE`, `CHOSEN`, and the delta, with decrease, increase and exact edit. The right rail shows two actual native narrative effect texts at a time and allows paging without truncation.

Back navigates internally, then returns to the actual preceding native narrative stage through `CharacterCreationManager.PreviousStage()`. Next confirms the final trait page, persists only a valid plan and restores the original native banner input. Escape is mapped to Back. No experimental opt-in is required. Failed CU overlay binding falls back to the original native stage.

The Gauntlet content uses the previous native-inspired charcoal, slate and brass color system. An explicit compact layout targets **1366x768** without six-row lists covering the bottom action bar; a centered max-size layout covers wider screens. In-game screen tests must still verify actual widget placement and mouse/controller input.

**Pinned Windows CI:** [run 44459027527](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44459027527), source SHA `095d79d3001f38b9e1ec5d7f491c42878a20c5d8`, **677 policy checks passed**, zero compiler warnings/errors. The DLL SHA-256 is `352cd1a169cd63965d2fb85a1bb41110c361a6465f6923f76f72841a09a9f7a9`. ZIP CRC and installer simulations passed. **Gate B only: compiled, not observed in-game.**

## Tests and evidence

- `scripts/test_post_origin_stats.py`: stage transition, original input ownership, selected-option dictionary access, Hero snapshot, XML commands and no redundant pregame stat fields.
- `Tests/PolicySmoke/Program.cs`: independent native default, balanced costs, restricted native ranges, invalid plan refusal, unrestricted mode, profile serialization and trait help.
- `scripts/Inspect-CU-Creator-API.ps1`: exact 1.5.7 private field audit. Verified the native `SelectedOptions` dictionary type in the pinned assembly.
- GitHub Actions builds module `net472` against exact reference assemblies and validates installable ZIP format. Source, API and policy gates are **not proof of an observed Bannerlord session**.

### Live acceptance still required

Verify two fresh campaigns (one Native, one Balanced/Unrestricted). Check actual life-story bonus text, edit every stat category, confirm the 30-point cap, test exact numeric entry, Return/Next/Escape focus, 1366x768 and 1920x1080, same-family and fief start, actual trait effects where native events reference them, and save-exit-reload persistence. An invalid edit must never silently grant partial changes or block the stock banner editor.

**Do not merge this branch into the main release or report the whole new creator finished until the relevant live gates pass.**
