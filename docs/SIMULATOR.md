# Calradia Unified: headless campaign policy simulator

## What this test actually runs

The simulator compiles and executes the **same pure C# policy classes** used by the Calradia Unified module. It creates an independent, seeded world model with four depots, abstract shipments, simulated map-convoy state, player inventories, gold, influence, troop counts, and independent clan-lord command decisions. The simulator runs without Bannerlord or TaleWorlds DLLs.

This is a **policy model**. It does not instantiate Bannerlord MobileParty, spawn an actor in the engine, move on the actual world map, open menus, or invoke TaleWorlds save/load hooks. Its fake parties and travel timing are test doubles, not game-engine simulations. No result from this harness counts as Gate C, D, E, F or G.

## Running it

Install the .NET 8 SDK, then execute:

    dotnet run --project Tests/CampaignSimulator/CampaignSimulator.csproj -- --seed 157 --runs 40 --days 365 --json BuildResults/campaign-simulation.json

GitHub Actions runs this automatically on changes to Tests, Source, Data, or the workflow. Download the **CalradiaUnified-v0.9.1-simulation-report** artifact for the JSON event summary and console output.

## Invariants checked on every simulated day

- Conservation: initial grain plus modeled depot production must equal current depots + player inventory + undelivered abstract shipment amounts + physical party cargo + explicitly counted losses.
- Capacity: no depot exceeds its configured limit, and no player resource becomes negative.
- Money: player gold and influence plus recorded spending equal their initial amounts.
- Identity: every physical shipment has one associated fake party and no orphan parties remain.
- Active-order limits: at most three physical convoys and one pending shipment per source.
- Dispatch: physical convoys require a distinct destination with sufficient space, source stock, and a free slot.
- Arrival: physical unloading only credits grain removed from the fake party. A partly full depot accepts only its free space.
- Loss and timeout: a simulated defeated or expired convoy removes its cargo from active stock and counts it as lost. A full-destination convoy must eventually time out.
- Persistence model: CU4 primitive state is serialized, parsed, and compared every seven days. A fake party registry still holds identities in memory.
- Recruitment: modeled batch levies cannot exceed troop capacity, daily cap, gold or influence.
- NPC decisions: actual strategy and command policies may not issue movements for invalid parties. Patrol requires peace and a safe target. Disabled autonomous movement stays blocked.

A fixed set of deterministic edge examples runs before the seeded campaigns. Every world uses a reproducible random seed, reported in any failing assertion. A failure exits with code 1 and fails CI.

## What remains for M0 and M1

Use [M0_TEST_PROCEDURE.md](M0_TEST_PROCEDURE.md) for launcher, startup and actual save/reload. Use [M1_IMPLEMENTATION_NOTES.md](M1_IMPLEMENTATION_NOTES.md) for real party spawning, travel, arrival, capture, loot, loss and engine save persistence. The simulator cannot certify those game features.

## Evidence and status

A passed simulation demonstrates only that the selected invariants held in the modeled sequences for the source revision and seeds tested. It cannot prove the model accurately represents every game-state transition. Record simulator results beside, not in place of, real Bannerlord test results.

## First verified hosted simulator run

The following report is tied to commit \`3bf3f1d99f828611b829312128c3a7c3374db37e\` and [GitHub Actions run 44227235032](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44227235032).

- **Result:** PASS. All configured build and simulation stages completed.
- **Inputs:** starting seed 157; 40 independent seeded worlds; 365 simulated days per world.
- **Scope:** 14,600 modeled world-days and 154,714 checks, including 10 deterministic edge examples.
- **Prior policy suite:** 143 executable pure C# policy checks passed.
- **Compile:** the v0.9.1 DLL compiled with 0 warnings and 0 errors.
- **Evidence:** [simulation-report artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44227235032/artifacts/14393492219) contains the JSON report and console log. The [compiled-untested module artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44227235032/artifacts/14393591779) is separate.
- **Highest gate reached:** B. No game-engine startup, actual MobileParty, Taleworlds campaign save/load, combat or scene test ran.

**Caveat:** The current model abstracts NPC party routing, vanilla battle events, settlements and cargo ownership. It detects policy and accounting inconsistencies under the modeled conditions. The numerical check count measures executed model assertions, not unique game features.

## v0.11.0 estate budget stress cases

`Tests/CampaignSimulator/FeudalStress.cs` runs beside the original campaign-policy model. For each seed/day it tests a healthy one-castle two-village budget, an 8,500-denar garrison, an 8,000-denar field party, both villages raided, and a depot grain harvest/sale cycle with conserved stock and bounded abstract proceeds. The healthy `+250/day` target refers only to the stated forecast coefficients, not actual Bannerlord tax income. The oversized scenarios intentionally retain negative outcomes.

The first v0.11.0 run, [44232872459](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44232872459), passed **303,098** combined model checks over 40 x 365 days, with **176** independent C# policy checks and a successful compiled DLL. Model evidence cannot pass C/D/E in-game gates.
