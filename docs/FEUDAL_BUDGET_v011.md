# Calradia Unified v0.11: Feudal income and affordability model

**Target:** Bannerlord PC v1.5.7  
**System:** M1 economy + M4 affordable garrison management  
**Status:** Source-level experimental features. Compilation and in-game results must be recorded separately.

## Why this exists

The user reported a negative 5,000–10,000 denars per day after beginning as a vassal owning a castle and two villages with 70-plus historical mods. That is a reported balance problem, not measured causality from specific mods. A balanced estate should plausibly sustain a modest starting field force and garrison, but bankruptcies remain valid for expensive elite armies, multiple clan parties, lost village income and sieges.

The game has its own clan finance models, garrison wage caps and village production. We do not replace those models. The fiscal adviser offers a transparent, conservative **estimate**, not a full reconstruction of the game's tax ledger.

## Deterministic budgeting rules

\`FeudalBudgetPolicy.EstimateEstateIncome(prosperity, healthyHearths)\` uses:

\`\`\`
estimated_income = 100 + floor(0.45 * castle_prosperity)
                      + floor(0.75 * healthy_bound_village_hearths)
\`\`\`

Inputs are capped to avoid runaway totals. Raided or looted villages contribute zero hearths to this **forecast**. The coefficients are gameplay-balancing assumptions, not the native clan finance model. The forecast must be recalibrated from actual game income readouts after in-game evidence is available.

The cap follows:

\`\`\`
affordable_garrison_wage = clamp(
  estimated_income
    - clan_lord_party_wage_share
    - reserved_other_costs
    - target_daily_surplus,
  0, configured_max_garrison_wage)
\`\`\`

Field-party wages use Bannerlord \`MobileParty.TotalWage\` for active player-clan lords. With several player-owned castles, their combined field wages are divided across those castles for the *forecast*. The comparison omits certain non-lord expenses and other properties, making it a planning aid only. The wage cap mutation uses \`Settlement.SetGarrisonWagePaymentLimit\`. It never raises a lower existing cap and skips besieged castles. A recommended cap below the defensive minimum is advisory-only. Enabling this action may lead the engine to reduce the garrison, including desertion. It does not instantly dismiss troops or promise a positive in-game net income.

### Example: one castle + two villages

| Item | Model denars/day |
| --- | ---: |
| Castle prosperity, 1,500 | Input |
| Two unraided villages, 900 hearths total | Input |
| Estimated gross fief income | 1,450 |
| Player clan field-party wage share | -500 |
| Other expenses allowance | -150 |
| Maximum budgeted garrison wages | -550 |
| **Forecast surplus** | **+250** |

An uncontrolled garrison costing 8,500/day produces a **−7,700/day modeled balance** using the exact same inputs. The model does not assume that every castle can support an 8,500-denar elite garrison.

## Economic mechanics

**Virtual village harvest:** Each 150 healthy hearths can add one abstract grain to the player's **saved castle depot**, up to ten per day and remaining depot capacity. This does not debit actual village production or alter the native village economy. A raided village produces no new harvest for this system.

**Surplus wholesale sale:** If at least 31 stored grain are available, the system sells at most eight per day and leaves a minimum reserve of 30. Revenue is \`soldGrain * 18\` denars. The actual saved depot grain is consumed. This is an *abstract sale*, not a physical market trade or map party.

**Auto garrison training:** Opt-in training adds two culture-basic troops to the **existing** garrison. It needs the prior training flag, 250 gold, five depot grain, a safe castle, at least 5,000 player gold, and space under both garrison size and wage budgets. It does not train elite troops or create new garrison parties.

## Simulator acceptance examples

- Healthy vassal: 1,500 prosperity, 900 total healthy hearths, 500 field wage, 150 other costs, 250 target surplus, max 1,800 garrison limit -> forecast +250.
- Pathological garrison: 8,500 wage on same castle -> forecast −7,700. The proposed cap is no higher than 550. Automatic cap changes require explicit opt-in.
- Oversized field army: 8,000 wage -> no affordable garrison budget. A negative clan balance remains legitimate.
- Both villages raided: zero healthy hearth contributions and zero added village harvest.
- Full depot: no new harvest. Protected grain reserve: no surplus sale.
- Training rejected for siege, unowned settlement, wage cap, troop cap, missing grain or insufficient cash.

The headless simulator runs these cases over seeded campaigns. It checks stock balance, revenue derived from actual sold depot units, projected surplus under healthy assumptions, and expected deficits under stress. A simulation pass cannot validate native tax receipts or how Bannerlord actually enforces a wage limit.

## Release safety

All four modifying settings default to \`false\`. The council fiscal report is read-only. Do not claim that this patch cures the user's specific 70-mod balance issue without collecting a v1.5.7 daily income breakdown and testing a campaign with the relevant historical modifications. Keep Gates C, D and E open until real play evidence exists.

## First executed validation

[GitHub Actions run 44232872459](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44232872459) completed the v0.11 source and model checks. It executed **176 C# policy checks** and **303,098 simulated checks** across **40 seeded model campaigns, totaling 14,600 model days**. Both the compilation and model stages passed. Compiler warnings: **0**. Compiler errors: **0**.

The model confirmed the exact **+250** healthy illustrative daily budget and identified expected losses under oversized garrison or field wages and village raids. It did not calculate actual v1.5.7 settlement tax receipts or perform a game save/load. In-game comparison is recorded as [issue #28](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/28) and the original deficit report as [issue #32](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/32).

Four source features `CU-M1-006`, `CU-M1-007`, `CU-M1-008`, `CU-M4-002` reached **Gate B** on this run. They are not full reconstructions of the referenced third-party mods.
