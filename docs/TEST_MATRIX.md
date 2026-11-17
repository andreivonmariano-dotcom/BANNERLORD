# Gameplay and engineering acceptance test matrix

**Target:** Mount & Blade II: Bannerlord PC v1.5.7. Record exact installed game/build and official module versions for every test session.

## Test evidence record

Each test report must contain: test ID; build SHA; compiled DLL SHA-256; game version; expansion version if present; active modules and load order; starting save/profile; test steps; expected behavior; observed result; PASS/FAIL/BLOCKED; crash/log path where relevant; tester and date. No result is PASS without an actual run.

## Baseline and CI

- **A-001:** Parse `SubModule.xml` and registered XML; verify module ID, exact files, runtime structure and no unknown third-party dependencies.
- **A-002:** Compare the modlist inventory and migration checklist. Exactly 78 distinct original IDs must remain mapped.
- **B-001:** Run `python verify_source.py` and the .NET PolicySmoke project in GitHub Actions.
- **B-002:** Restore pinned v1.5.7 references, compile `CalradiaUnified.dll`, record actual DLL existence and hash.
- **B-003:** Build a ZIP containing only the `Modules/CalradiaUnified` runtime tree. Verify ZIP CRC, manifest, config and DLL.
- **C-001:** From clean official modules, enable Calradia Unified; launch the exact Bannerlord build to the main menu.
- **C-002:** Start a new campaign, create a save, quit game, restart and load. Check logs for duplicated subscriptions, serialization failure or exceptions.

## Gameplay

- **D-101:** Depot stock / gold transfer conservation, failed transaction atomicity and price bounds.
- **D-102:** Physical map convoy with troops and food, actual map movement, delivery, combat loss and no duplications.
- **D-201:** Six native cultures: local troop, gold/influence costs, ownership transfer, daily limit, party capacity and upgrades.
- **D-301:** Independent player-clan NPC resupply and recruitment: legal order accepted by engine, target ownership, food supply and cooldown.
- **D-302:** Reject movement for the player, army members, battle parties, sieges, settlement-departure transitions and unsupported sea travel.
- **D-303:** Diplomacy goodwill mission: single cost, bounded effect and cooldown.
- **D-401:** Garrison staffing, siege entry and exit, transfer of settlement ownership and castle/town restrictions.
- **D-501:** Field battle, agent spawn, retreat, formation, reinforcement and loot conservation.
- **D-601:** Character/trait event transitions; UI activation and resolution-specific behavior.
- **D-701:** Asset-backed scenes, banners and audio load with verified origin and rights.
- **D-801:** War Sails naval battle, land/sea transition and coast settlement route on pinned expansion version.

## Persistence and regression

- **E-001:** Save, quit and reload during depot transfers and convoys. Reconcile persisted vs actual party state.
- **E-002:** NPC party captured, destroyed, disbanded or enlisted into an army during a pending order.
- **E-003:** Settlement captured during supply movement, recruitment, garrison orders or a siege.
- **E-004:** Repeat historical `Mission.SpawnAgent` and `LeaveSettlementAction.ApplyForParty` transition cases without a reproducible crash.
- **E-005:** Run 30 campaign days with experimental autonomous orders off, then in a supervised test with only one feature switched on.
- **F-001:** Perform at least a 60-minute campaign smoke session covering applicable feature paths, logs, performance and save recovery.
- **G-001:** Verify that the tested artifact hash matches the published artifact, and that release notes state only actually passed features.

## Execution status at adoption

**Gate A/B:** The historical GitHub Actions run [44134919335](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335) passed its configured CI steps for v0.8.0. This does not automatically pass new A/B tests added later.

**Gate C onward:** No evidenced in-game test result is recorded. All gameplay and persistence test IDs are **NOT RUN** until a dated log or direct observation is attached.
