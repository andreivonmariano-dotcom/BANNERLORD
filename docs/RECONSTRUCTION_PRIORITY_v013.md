# Calradia Unified: all 78 original mod priorities (v0.13 planning)

This is a source-grounded **engineering priority register**, not evidence of historical functionality being ported. Every archive name and version comes from `Data/Original_78_Module_Inventory.csv`. The proposed focus is our design interpretation, not a claim about the old DLL's exact internal behavior.

## Reproducible scoring

All values are editorial planning scores. `gameplay_value` is 1–5 (higher is better). `engineering_effort` and `compatibility_risk` are 1–5 (lower is better). `existing_source_reuse` is 0–3 (higher is better). Score is **5 × value + 3 × reuse − 2 × effort − 2 × risk**. Ties are sorted by value descending, then module ID. Candidate scores use the existing v0.12 source and 78-mod inventory. They are not performance measurements or runtime compatibility results. A mod name alone is not enough to claim full replacement.

Phase: EARLY_AFTER_M0 at score ≥16, MID at ≥8, LATER at ≥0, and DEFER below 0. M0 first requires the v1.5.7 launcher, save/load and mission smoke test. Source rebuild must respect asset rights and exact pinned engine APIs.

## Highest-ranked original modules

1. `RealisticTownIncome` (28): Observed castle expenses versus estimated income and affordability
2. `ImprovedGarrisons` (24): Player-directed local troop transfers, garrison wages, and defense
3. `RealisticRecruitment` (24): Existing native-culture recruiting and daily limits
4. `ClanRankings` (20): Read-only clan comparisons and campaign information
5. `ImmersiveRecruitCommoners` (16): Commoner recruitment and progression after native roster testing
6. `TavernkeeperReach` (16): Existing aid and logistics ledgers without invented carriers
7. `RealisticTroopCosts` (15): Transparent troop recruitment and wage costs
8. `AfterTheBattle` (14): Post-battle handling after mission transition regressions
9. `CampaignImprovements` (11): Broad AI and diplomacy behavior requires isolated testing
10. `Promoted` (11): NPC advancement and safe campaign-event timing
11. `RealisticBattleLoot` (11): Loot changes after mission and inventory regression
12. `RealisticPrisoner` (11): Prisoner economy and release actions
13. `BetterTime` (7): UI speed controls after pause and save/load checks
14. `SpearPreference` (6): Test mission entry, damage and battle transitions before acceptance
15. `RBM` (5): Large mission, combat and equipment rewrite

The full ordered, machine-readable 78-row register is `Data/Reconstruction_Priority_78.csv`. A CI check compares every original ID, score and rank.

## Deliberate exceptions and release gates

- Source already includes partial, default-disabled estate, recruitment, garrison, and clan-party code. The ranking rewards reuse, but no historical module is marked as wholly replaced.
- Large mission hooks (for example RBM, ReinforcementSystem, LivingPersonasAI and SiegeCommandMenu) remain lower priority until reproducible native startup and regression evidence exists.
- War Sails integration requires separate, version-pinned checks and cannot be inferred from a v1.5.7 base-game DLL.
- The v0.13 estate shortfall is **modeled**. Actual player gold differences are observed but do not isolate tax receipts. The native clan-finance breakdown still needs exact matching game APIs and in-game validation.
- Existing native garrison troops are moved manually with an opt-in switch. The mod creates no independent recruiter or patrol party for these controls. This is the castle-management core, not a full replacement of Improved Garrisons.
