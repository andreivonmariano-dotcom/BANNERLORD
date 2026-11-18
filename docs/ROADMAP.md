# Calradia Unified: gameplay-first development roadmap

**Version:** 1.0 (18 November 2026)  
**Target baseline:** Bannerlord PC v1.5.7; War Sails is a separate test track  
**Status:** v0.11.0 hosted DLL build and estate simulator passed (run 44232872459); game launch, campaign function and persistence remain unverified  
**Inventory:** 78 unique historical mod IDs, grouped into nine feature areas. Counts per primary milestone are `M0 7`, `M1 7`, `M2 5`, `M3 3`, `M4 4`, `M5 17`, `M6 18`, `M7 17`. M8 and M9 are shared tracks; no IDs are double-counted.

## Working order

M0 is the release blocker. Complete the first in-game start/save test before activating new autonomous behaviors. Gameplay milestones M1 through M5 precede polish and assets M6 through M8. M9 closes the tested release. Milestones are **acceptance gates**, not promises of calendar completion. Make a single feature releasable without waiting for every feature inspired by the historical inventory.

## Status definitions

`IDENTIFIED`, `SCOPED`, `IMPLEMENTED_SOURCE`, `STATIC_CHECKED`, `COMPILED`, `GAME_TESTED`, `REGRESSION_TESTED`, `RELEASED`, `BLOCKED`. Keep a feature's highest completed status separate from its milestone's open work.

## Milestone acceptance criteria

### M0: Baseline, build reproducibility and campaign safety

**Tracking issue:** [#1](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/1)

**Objective:** Establish a reliably loadable single-module baseline before expanding gameplay.

**State:** Open. Existing v0.8.0 compile is a prior baseline, not a completed M0 gate.

**Acceptance tests**

- [ ] A0.1: Source structure, XML manifest and dependency scan pass; no old mod DLLs are distributed.
- [ ] B0.1: Windows GitHub Actions compiles the current commit and executes independent tests.
- [ ] C0.1: Launcher detects the module; a new campaign launches on the pinned Bannerlord build.
- [ ] E0.1: Save, exit, relaunch and reload on the same build without lost data or event duplication.
- [ ] E0.2: Regress Mission.SpawnAgent and LeaveSettlementAction.ApplyForParty transitions without reproducible mod faults.

**Exit:** Gate E for baseline startup and persistence, with exact game build and logs. Historical v0.8.0 CI only reached Gate B.

### M1: Economy, depots, logistics and physical convoys

**Tracking issue:** [#2](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/2)

**Objective:** Real settlement stock, bounded prices, atomic resource transfers, ownership changes, and observable map convoys.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A1.1: Independent policy tests reject negative grain or gold and prevent partial transactions.
- [ ] D1.1: Depot deposit, purchase and withdrawal change matching game inventories exactly once.
- [ ] D1.2: A physical convoy appears on the campaign map, moves, and delivers at arrival, never by simulated teleport.
- [ ] D1.3: Convoy loss, attack, or destination ownership change cannot duplicate cargo.
- [ ] E1.1: Pending convoys and depot balances persist across save and reload.

**Exit:** Gate E for stock conservation, real convoy life cycle and persistent transactions.

### M2: Cultural recruitment and troop progression

**Tracking issue:** [#3](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/3)

**Objective:** Culture-specific recruit selection, real troop costs, gold/influence checks, daily limits, and unit capacity.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A2.1: Culture and cost policies pass deterministic tests and reject invalid costs.
- [ ] D2.1: Six culture-specific settlements recruit their own basic troop after conquest.
- [ ] D2.2: Capacity, insufficient gold, insufficient influence and daily-cap rejections preserve state.
- [ ] D2.3: Troop IDs and upgrade paths remain valid in combat and roster UI.
- [ ] E2.1: Daily limits and recruitment state persist through reload.

**Exit:** Gate E for six-culture roster and recruitment economy.

### M3: Independent clan AI and bounded diplomacy

**Tracking issue:** [#4](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/4)

**Objective:** Safe NPC party goals and goodwill missions, respecting the native army system.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A3.1: Policy tests cover defense priority, food thresholds and order cooldown.
- [ ] D3.1: Only independent player-clan NPC parties accept automated resupply or recruiting destinations.
- [ ] D3.2: Orders refuse battle, siege, army membership, hostile destinations and unsupported sea travel.
- [ ] D3.3: Goodwill missions apply documented costs, effects and cooldowns once.
- [ ] E3.1: Reload restores cooldowns; no destination thrashing over 30 campaign days.

**Exit:** Gate E for bounded NPC order execution and goodwill state. Full kingdom warfare remains a separate scope decision.

### M4: Garrisons, sieges and campaign transitions

**Tracking issue:** [#5](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/5)

**Objective:** Garrison staffing, ownership transfers and legal siege transitions.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A4.1: Garrison and siege state policies reject destroyed or captured settlement references.
- [ ] D4.1: Garrison deposits and withdrawals preserve troop count and capacity.
- [ ] D4.2: Enter or leave a siege and complete a settlement ownership transfer without illegal movement orders.
- [ ] E4.1: Save/reload during siege and after capture restores state consistently.
- [ ] E4.2: Campaign events do not register duplicate handlers after reload.

**Exit:** Gate E for ownership, siege transitions, garrison state and regression.

### M5: Combat, equipment and battle behavior

**Tracking issue:** [#6](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/6)

**Objective:** Independent tactical/roster improvements without destabilizing agent spawn or mission transitions.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A5.1: Equipment and upgrade references resolve against current game data.
- [ ] D5.1: Field battle, retreat, reinforcement and autocalc scenarios behave as specified.
- [ ] D5.2: Formation and spear/pike behavior changes have reproducible test conditions.
- [ ] E5.1: Repeated mission starts and agent spawns have no reproducible crash.
- [ ] E5.2: Battle exit and campaign re-entry maintain troop and loot counts.

**Exit:** Gate E for named combat features and regression; report measured balance results.

### M6: Characters, interfaces and immersion

**Tracking issue:** [#7](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/7)

**Objective:** Character state and interface changes with safe event lifetimes, identity data and save migration.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A6.1: UI and character identifiers and save keys are unique and stable.
- [ ] D6.1: Each new menu and screen opens, closes and refreshes without duplicate actions.
- [ ] D6.2: Traits and relations change only through supported events.
- [ ] E6.1: Death, marriage, reload, different resolutions and localization paths retain valid state.
- [ ] E6.2: Clean profile launch includes no missing file or interface exception.

**Exit:** Gate E for each implemented character or UI behavior.

### M7: Scenes, visuals, heraldry and audio

**Tracking issue:** [#8](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/8)

**Objective:** Version-correct scene and visual assets that are licensed for distribution.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A7.1: Every asset has an origin, permission and load-path record.
- [ ] D7.1: Relevant town, battlefield and siege scenes load without missing assets.
- [ ] D7.2: Banners, sounds and animations trigger in the intended missions.
- [ ] E7.1: Long-session performance measurements include before/after frame timing.
- [ ] E7.2: Scene transitions, reload and removal do not break official module assets.

**Exit:** Gate E and asset-rights check for each shipped visual feature.

### M8: War Sails compatibility track

**Tracking issue:** [#9](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/9)

**Objective:** Test the expansion separately, including sea routes, ship states and naval-specific data.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] A8.1: Record the exact installed War Sails version and expansion dependencies.
- [ ] B8.1: Compile any expansion-specific code against matching references.
- [ ] D8.1: Ship travel, coastal settlement arrival and naval battle scenarios execute with relevant features.
- [ ] E8.1: Save/load at sea and transit to land preserve party state.
- [ ] E8.2: Regression checks cover naval-specific recruitment, equipment and cargo.

**Exit:** Gate E on the named War Sails version; no expansion support claim from base-game CI.

### M9: Verified one-folder release

**Tracking issue:** [#10](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/10)

**Objective:** Publish only the actual tested module with precise feature coverage.

**State:** Open. No milestone-wide in-game acceptance test is recorded.

**Acceptance tests**

- [ ] F9.1: Complete 60-minute campaign smoke session on the exact game version.
- [ ] F9.2: Compile and artifact hashes match the candidate that underwent gameplay testing.
- [ ] F9.3: ZIP contains one Modules/CalradiaUnified tree, runtime-only files and real DLL, no installer.
- [ ] F9.4: Accept/reject each reconstructed feature in the 78-mod acceptance checklist.
- [ ] G9.1: Publish changelog, dependency list, load order, save compatibility, source SHA and test evidence.

**Exit:** Gate G release after all shipped features meet their individual gates; unimplemented inspirations remain explicitly pending.

## Coverage rule

See [78_MOD_COVERAGE.md](78_MOD_COVERAGE.md) for the original IDs assigned to primary workstreams. Grouping is planning only. For each feature, record acceptance criterion, source path, exact commit, test artifact, game build, highest passed gate, rights, and remaining scope before claiming a historical feature replaced.

## Dependencies and risks

- A signed or compiled DLL is only Gate B evidence. Gate C through Gate F require a real Bannerlord installation.
- Keep high-risk party movement, siege transitions and save-state changes behind disabled-by-default flags.
- Recheck TaleWorlds game and expansion versions and original mod licenses at each integration.
- Release artifacts have no installer and contain runtime resources only.

## Working links

- [Project rules](SOURCE_OF_TRUTH.md) and [full handbook](../PROJECT_INSTRUCTIONS.md)
- [Acceptance protocol](TEST_MATRIX.md) and [release checklist](RELEASE_CHECKLIST.md)
- [Original migration inventory](../Data/Migration_78.csv) and [78-mod acceptance CSV](../Data/CalradiaUnified_78_Mod_Acceptance_Checklist.csv)
- [First successful v0.8.0 compile](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335)
