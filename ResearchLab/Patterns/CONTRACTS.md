# PAT-001 | Implementation patterns
Design guidance from [native reference](../NATIVE_ENGINE_1_5_7.md) and [external source studies](../MOD_IMPLEMENTATION_STUDIES.md), not universal runtime guarantees.
- Observe actual events; pre-event is not completion. Verify exact actor/target, native post-state and failure.
- Separate pure policy, adapter, UI and persistence; distinguish policy tests from game execution.
- Explicit authorization, revalidate before action, verify before/after resources and all role slots; partial failure requires inspection rather than blind retries.
- Bounded event-driven state, stable IDs and migration; no competing provider mechanics.
- History must affect a concrete decision and outcome. Study Diplomacy event-to-decision, Banner Kings competence and costs, Distinguished Service earned promotion without copying code.
- Keep receipts in diagnostic logs; offer practical player-facing feedback and test repeated use. Revalidate every pattern against current engine and provider.