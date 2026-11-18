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

## Policy simulation: separate from in-game acceptance

- **SIM-001:** Execute the seeded headless C# campaign simulator for 40 runs of 365 model days with fixed starting seed 157. Preserve the JSON summary and the failing seed on error.
- **SIM-002:** Validate grain conservation, depot bounds, convoy cargo and expiration, gold/influence balances, recruitment caps, NPC safety decisions and CU4 primitive round trips.
- **SIM-003:** Treat SIM-001 and SIM-002 as model evidence only. Retain all real C0, D1 and E1 game tests as NOT RUN until an actual Bannerlord session is observed.

See [SIMULATOR.md](SIMULATOR.md).

## v0.10.0 additional gameplay and migration tests

- **D-202:** In an owned town, recruit two local culture troops at 200 gold each. The third attempt on the same day must reject, and a new campaign day must reset the settlement counter.
- **D-203:** In a captured town, recruit units from the town's native culture, not the current faction of its owner. Refuse a purchase that would exceed party capacity or available gold.
- **D-402:** In a player-owned, unbesieged town or castle with an existing garrison, train exactly two native basic troops using 250 gold and five depot grain. Confirm the roster receives two actual soldiers and both resources decrease exactly once.
- **D-403:** Reject garrison training during a siege, in an unowned settlement, without an existing garrison, with insufficient gold or grain, at troop cap, or after daily training cap. Check that rejection changes no resource.
- **E-203:** Save and reload with nonzero local and garrison daily counters, then attempt additional recruitment. Test older CU3 and CU4 saves separately for safe counter defaults.
- **SIM-010:** Use the headless policy simulation to test new recruitment and training transaction limits across repeatable campaigns. Simulated save round-trips do not count as game save/load evidence.

## v0.11 fiscal and garrison regression cases

- **D-110:** In an owned castle with two healthy bound villages, inspect the CU budget briefing; record prosperity, hearths, **actual** lord-party and garrison wages, the proposed cap, and native clan-income breakdown. Do not report the forecast as real tax.
- **D-111:** With `EnableAutoGarrisonWageCaps=true`, verify an unaffordable castle wage limit is *lowered* only when the result meets the defensive minimum. Confirm that a preexisting lower manual cap and an active siege are untouched. Record troop desertion and timing, if any.
- **D-112:** Train an existing castle garrison automatically only under adequate gold, grain, budgeted troop costs, daily limits and no siege. Check the real roster and exact gold and grain debits.
- **D-113:** With virtual harvest on, compare healthy and raided bound villages. Confirm added grain appears only in the CU depot and is limited by its capacity.
- **D-114:** With abstract surplus sale on, verify gold gains equal grain sold at the configured price, no sale occurs below the reserve, and no physical caravan is claimed.
- **E-110:** Save/reload with a fiscal cap, partially spent training quota, depot stock and sales. Check actual game side effects and don't claim M0 based on model round trips.
- **SIM-110:** Seed the fiscal stress model with one castle, two villages, oversized garrison, oversized field party, and dual raids. Healthy affordable example must be positive, crisis cases must preserve negative-balance possibility.
