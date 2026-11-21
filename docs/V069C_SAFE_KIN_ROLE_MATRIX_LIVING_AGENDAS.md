# v0.69c | Safe provisional kin, all-role native matrix and observed NPC agendas

**Direction:** Q1 → Q2 → Q3, in that order. **Date:** 21 November 2026. **Branch:** `fix/v069c-family-safety-role-matrix-observed-agendas`, [draft PR #91](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/91), stacked on [PR #90](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/90).

## Q1. Family creation failure containment

**Prior source defect:** `madeChildren` increased only after child setup completed. A native `HeroCreator.CreateChild` allocation could have succeeded before a later setter or parent-link check failed. Cleanup then inspected `madeChildren==0` and risked disabling the new spouse even though a real native child had been allocated.

**Change:** Track every returned child allocation immediately, then refuse spouse cleanup if *any* returned child exists. `SandboxFamilyCreationSafetyPolicy.CanDisableProvisional` refuses cleanup for a pre-existing Hero, any native spouse or descendants, a party member or captive, a clan companion, a settled Hero, or a Hero referenced as the player's parent, spouse or child. The helper invokes native `DisableHeroAction` only for a freshly generated, otherwise uncommitted Hero. It must confirm disable before unlinking native parent references. Newly generated parent Heroes are tracked and can be reclaimed if parent binding fails before attachment. New incomplete siblings are tracked separately from already confirmed party or household members. All failures retain the existing no-retry barrier and prevent later grants.

**Open limit:** This is safe containment, not universal atomic rollback. A native marriage, child, or already linked sibling is intentionally retained on a later failure. Do not remove or disable a legitimate native parent, spouse, child or party Hero to make a log appear successful. Phase-8 Advanced Start is outside the scope of rollback. A future native reconciliation flow must account for saved partial starts and fail-visible player recovery.

## Q2. Native game and social-status matrix for every role

A single `SandboxRoleConsistencyPolicy` now maps all twelve selectable roles to the native engine start type. It rejects mismatches and checks ruler/vassal status, clan allegiance and unexpected fiefs in independent starts. `SandboxNativeStartOptions` uses this mapping and `SandboxStartingLifePreflight` applies it before CU grants.

- King: native `king`, ruling clan and at least one actual fief. Monarch duties remain a future gameplay system.
- Lord: native `vassal`, non-ruling kingdom clan and at least one fief. A younger son with his own estate is not landless.
- Knight: native `vassal`, non-ruling kingdom clan and at least one fief. Distinguish this from household service without land.
- Soldier: native `mercenary` in a kingdom, not an NPC troop or enlisted private. Mercenary command is native, not an invented wage.
- Admiral: native `fleetadmiral` in a kingdom. Ship, navy rank and crew duties still need direct game checks.
- Trader: native `trader`, independent and no starting fief or kingdom title. Business contracts and household finances are not yet implemented.
- Outlaw: native `outlaw`, independent and no starting fief. Band membership and outlaw laws are incomplete.
- Beggar: native `beggar`, independent and no starting fief. Household vulnerability and dependence need further gameplay.
- Wanderer: native `default`, independent and no starting fief.
- Recruit: native `default`, independent and no starting fief. An external Serve as Soldier public API is needed for actual NPC party enlistment.
- HouseholdKnight: native `default`, independent and no starting fief. A patron reference is not the same as verified wages or duties.
- NobleCadet: native `default`, independent and no starting fief. Native parentage must be verified, without an automatic inheritance grant.

**Caveat:** The matrix tests coherent *game contracts*, not universal historic inheritance, military law or socioeconomic authenticity. Cultural and service systems must be checked separately. Native asset, fleet and save-game validation remain required.

## Q3. Changing goals from observed life, without fictional memories

`SandboxGeneratedCharacterGoalPolicy` now distinguishes a sibling actually present in the player's party from the same sibling elsewhere. `SandboxCharacterAgendaState.ObserveNativeGoal` updates the modeled ambition, observed occupation and clan, resets uninterrupted comradeship progress on goal change, and persists the results through the unchanged `CU_AGENDA_1` format. `SandboxLivingPersons.ObserveAgenda` reads actual living Hero parentage, shared party, native occupation and clan each day. It records `ObservedGoalReassessment` only on a change, and states that the revised motivation is an inference, not an NPC's spoken intent.

Those observed goals feed `SandboxAgencyDecisionPolicy.Choose`. A sibling currently protecting the shared household can seek HouseholdAid. A sibling whose goal becomes preserving family position can request Recognition on a future eligible turn. The actor must be a real present adult Hero and meet existing bounded request limits. No fabricated past promises, automatic rewards, or imaginary full AI plan are created.

When Bannerlord offers no current residence evidence, previous agenda location and evidence are marked unverified. This does not declare that the person moved or died. Both goal and location status survive existing save serialization.

## Regression gates

- C# policy smoke validates protection of married, parented, settled, companion and active party Heroes and the immediate child-allocation rule.
- All twelve exact role/native type pairs and positive native-status scenarios are covered, plus fake landed cadets, unsupported kingdom affiliations and invented royal/vassal status.
- Native goal and residence changes and their save/reload roundtrips are tested. Goal changes affect eligible request categories but do not bypass physical presence.
- `scripts/test_v069c_family_role_persona_contracts.py` checks source wiring, and Windows CI runs pinned C# smoke, simulations, native reference compilation and ZIP integrity.
- In-game acceptance remains mandatory: successful and deliberately failed generated spouse, children and siblings, full 12-role creator navigation and real map entry, 30-day campaign, native roster/parentage inspection and save/quit/reload.
- Highest game verification gate remains **B at most** until live testing is submitted. Do not claim Q1, Q2 or Q3 complete from policy tests alone.

**CI record:** Pending exact branch-head Windows Actions result as this note is created. Update with executed test count, compile result, SHA-256 and artifact URL after the successful run.
