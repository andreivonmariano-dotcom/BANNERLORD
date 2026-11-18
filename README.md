# Calradia Unified v0.11.0: early-vassal budget, villages and garrison economics

**Target game:** Mount & Blade II: Bannerlord PC v1.5.7  
**Release status:** source candidate pending the v0.11.0 GitHub Actions build. Neither actual game startup nor game save/load is verified.  
**Package:** one `Modules/CalradiaUnified` folder and one compiled DLL, no installer or old third-party mod DLLs.

## Early-vassal balance problem

A castle and two villages should finance a **reasonable starting force** in a balanced starting scenario. An inherited 70-plus-mod load order gave the user daily deficits of **5,000 to 10,000 denars**. This is a reported experience, not a verified calculation on the current v1.5.7 configuration.

The new fiscal system uses the castle's prosperity and the hearths in its non-raided bound villages to forecast an indicative income. It compares the forecast against **actual clan lord-party wages and garrison wages** and recommends a wage cap that leaves a configured reserve. **The forecast is not Bannerlord's native tax calculation.** It deliberately avoids pretending that all oversized armies must produce positive income.

The model's illustrative reference case: castle prosperity **1,500**, two villages with **900 combined hearths**, forecast income **1,450/day**, clan field wage share **500**, other-cost allowance **150**, and garrison cap **550**. The resulting model balance is **+250/day**. An 8,500-denar garrison in that example would instead yield **−7,700/day** before any cap. These are example inputs, **not observed v1.5.7 revenue values**.

## New v0.11.0 gameplay functions

- **Fief fiscal briefing:** Every owned castle has an estimated tax/wage report in its menu. It reads the castle and its bound villages and prints actual game garrison and field-party wages alongside the *forecast*.
- **Opt-in garrison wage budgeting:** `EnableAutoGarrisonWageCaps=false` by default. If enabled, it can **lower** an existing wage limit for a player-owned castle using the game's `SetGarrisonWagePaymentLimit` API. It skips sieges, preserves manually lower caps, and refuses a result below the configured minimum defensive wage. Lower limits can trigger unpaid-wage desertion, so test on a new campaign.
- **Opt-in budgeted garrison training:** `EnableAutoGarrisonTraining=false`. Only an existing player-owned castle garrison is considered. Requires the previous `EnableGarrisonTraining` toggle, a safe settlement, adequate gold reserves, depot grain, a troop deficit, and a positive model budget after added wages. Creates at most two basic local soldiers per castle/day.
- **Opt-in abstract village harvest:** `EnableVillageHarvest=false`. Healthy bound villages add bounded **virtual** grain to the castle depot according to hearths. It never takes goods from the vanilla village inventory. No harvest is granted for raided villages.
- **Opt-in abstract estate wholesale:** `EnableSurplusEstateSales=false`. The estate sells only depot stock above a grain reserve and credits at most eight grain x 18 denars per day. Grain is subtracted from the saved depot ledger. No physical buyer or real caravan is spawned.

These additions do not rewrite native castle/village tax income, vanilla garrison wage formulas, or the kingdom's AI army command system. The existing M1 guarded map convoys remain separate.

## Build, test and traceability

The pinned [GitHub Actions workflow](.github/workflows/bannerlord-build.yml) runs independent C# policy tests and the headless simulator, compiles against v1.5.7 references and packages a `COMPILED_UNTESTED` module. See [Feudal budget design and scenarios](docs/FEUDAL_BUDGET_v011.md), [simulator](docs/SIMULATOR.md), [M0 startup procedure](docs/M0_TEST_PROCEDURE.md), [feature registry](Data/Feature_Registry.csv) and [78-mod tracker](Data/Migration_78.csv).

**Test ladder:** simulation = modeled policy evidence. Compilation = Gate B. Real startup, settlement menus, wage-cap effects and game save/load remain Gates C–E and are not passed by CI.

## Installation after successful build

Open the Actions run and download its `CalradiaUnified-v0.11.0-compiled-untested` artifact. Extract the inner module ZIP into the Bannerlord installation root, preserving:

```
Modules/CalradiaUnified/SubModule.xml
Modules/CalradiaUnified/ModuleData/calradia-config.ini
Modules/CalradiaUnified/bin/Win64_Shipping_Client/CalradiaUnified.dll
```

Back up the old module folder and saves. Start with all mutating fiscal switches disabled. Read the castle budget briefing, then activate one experimental feature at a time on a fresh campaign.
