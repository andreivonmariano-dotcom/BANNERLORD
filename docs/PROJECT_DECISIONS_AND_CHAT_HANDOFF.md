# BANNERLORD | Living project direction and cross-chat handoff

**Established:** 21 November 2026. **Canonical source Google Doc:** https://docs.google.com/document/d/1A6AZpYpCyY6PoI1J6hn1rC2NQqnvPt9dsvZIZ0vwJpk/edit . The Google Doc contains the controlling “CURRENT MASTER PRIORITY” section near its beginning. Treat this file as a dated engineering handoff, not a substitute for inspecting live source or CI.

## User decision: Q1 → Q2 → Q3, in that order

**Q1, v0.69, highest priority:** resolve Starting Life's native consistency and failure-containment defects before extending the simulation. Native start status, actual parentage, birth rank, spouse/children/siblings, real fiefs, household residences, patron bonds, gifts and native relation scores must agree with the pre-start contract. Check status only after game-owned actions. Stage creation and use safe compensation for partial failures. Check every role's start, and test after save/reload. Do not present source validation, simulation, or a compiler pass as live game acceptance.

**Q2, v0.70+:** design 12 socially credible medieval starts: King, Lord, Knight, Soldier, Admiral, Trader, Outlaw, Beggar, Wanderer, Recruit, HouseholdKnight, NobleCadet. Distinguish birth rank, title, job, clan membership, fiefs, financial means, native patron, service term, real wages, homes, family obligations, legal/political standing and changing social mobility. Keep native Bannerlord status authoritative. A landless cadet does not receive an inherited fief, a household knight is not automatically a landed vassal, and a Recruit is an applicant until real external service.

**Q3, v0.71+:** build persistent NPC identity, stable traits, evolving goals, loyalties, boundaries, verified memories, changing residences and independent actions. A recorded favor or consent requires observed native effect, not an intent label or a relationship threshold. Different people must be able to accept, refuse, negotiate, leave service, improve livelihoods, react to losses and develop personal bonds. Use bounded save data and deterministic policy tests plus live in-game checks.

Do not advance the feature scope ahead of Q1 integrity gates.

## Record of our decisions and development events

- **18 November:** initial project source handbook v1.0, Bannerlord PC 1.5.7.125802 reference target, official-module-only baseline, source-first review, Windows PowerShell 5.1 verification, single module ZIP with no installer. Source: [PROJECT_INSTRUCTIONS.md](../PROJECT_INSTRUCTIONS.md), [PowerShell standard](POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Historical 78-mod inventory remains a target, not a claim of completeness.
- **19–20 November:** native Sandbox UI and age/stats/house family design expanded through v0.60+. Native character creation and family relationships were integrated. Earlier live screenshots and tests found menu-background freeze, overlays, navigation, sliders, and stats-screen reliability problems. Compiles do not prove these have passed in the game.
- **21 November, v0.64–v0.67:** Starting Life preflight/final contract, native father/mother and elder-son binding, controlled creation of siblings and family, three landless roles, bounded social observations, service tracking, persistent character ambitions, local requests and optional family-estate mediation. These are targeted mechanics, not full medieval-life simulation.
- **21 November, v0.68:** [draft PR #89](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/89), stacked on [draft PR #88](https://github.com/andreivonmariano-dotcom/BANNERLORD/pull/88). Sibling comradeship rewards count only after actual native +1 relation. Runtime source compiled at caa897e645001afff2bdb3db81d5265b2f5b3574. [Actions 44856889654](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44856889654) passed 867 executed C# policy checks, large model simulations, and compilation with no errors/warnings. Highest verified gate: B. No v0.68 live-game acceptance recorded.
- **21 November, latest source audit:** P0 phase-8 native start is irreversible before phase-9 CU preflight. P0 `SandboxAutoHousePlan`'s landless `FamilyHomeMode=None` was not a valid profile option and was silently lost on parsing. P1 final start contract omitted failed family-home placement, cultural consent was derived from a relation threshold rather than a native NPC choice, gift bookkeeping did not confirm both sides of the transfer, location history could become stale, created family/officers lacked complete compensation, agenda goals were static, and roles remained incomplete socially. Internal manifest still reads v0.38.0.
- **21 November, current user directive:** implement Q1, Q2 and Q3 stepwise, store priority in source Google Doc, and maintain a cross-chat record. This file records the material engineering decisions and outcomes, not a verbatim transcript.

## Current milestone: v0.69 step 1, source pending CI

**Branch:** `fix/v069-starting-life-household-contract`, based on v0.68 head `83061e77dee4804efa758f625e03bdef84e9753a`.

**Step 1 changes:**
- `SandboxFamilyHomePolicy` admits `None`, rejects a promised owned home for a landless married start, and checks that an explicitly requested native household placement really ended in `placed:...`.
- `SandboxAutoHousePlan` chooses `None` by default for every non-landed role, not just the three custom landless roles.
- `SandboxOriginProfile.IsValid` and `CUGuidedCreatorVM.CommitHouse` reject contradictory landless household requests without preventing intermediate advanced-field edits.
- `SandboxFamilyHome.TryPlace` explicitly records `not_requested:no_fixed_home` for `None` instead of quietly making up a residence.
- `SandboxOriginBehavior.TryApplyOnce` includes a requested family-home failure in final `sandbox_start_contract` and logs observed home outcome.
- New C# policy tests exercise `None` roundtrip, nonlanded auto home, impossible landless home, and failed/partial/confirmed placement.
- This is only a **partial Q1 repair**. Do not mark all v0.69 P0/P1 audit items resolved.

**Verification:** CI and live-game testing have not yet been established for v0.69 in this handoff. Read the latest Actions run and update this paragraph before representing this version as compiled.

## Unresolved Q1 blocker queue

1. Fully transactional or carefully compensating phase-9 initialization. Native Advanced Start phase 8 cannot be undone by current preflight. Prevent impossible choices before handoff, then abort without partial custom resources.
2. Verify exact native payer AND recipient gold transfer, all native relation effects, and consistent event counts in local NPC requests and inheritance mediation.
3. Update NPC location records when residence is lost, and distinguish verified home, actual position and last known location.
4. Define compensation for failed family, retinue and kinship creation. Never delete existing native Heroes or disband native parties during a failed retry.
5. Make sibling/elder binding, birth-order, spouse birth clan, home and retinue acceptance evidence-based across all 12 roles.
6. Run the actual installed Bannerlord new-game, UI, 30-day and save/reload tests. Validate compatibility with the exact compiled artifact.

## Rules for future BANNERLORD chats

Read this record, the source Google Doc, `AGENTS.md`, `PROJECT_INSTRUCTIONS.md`, the relevant current source, latest open PRs and Actions runs at the start of a development request. Do not assume `main` is newest: recent features are a stacked chain of draft PRs. Distinguish **requested**, **planned**, **committed**, **compiled**, and **verified live in game**. Update this record after each concrete milestone, including SHA, changed files, C# check counts, Actions ID, artifact hash, remaining blockers and next action. Keep all verified evidence links. Never insert untested claims into release notes.

**Primary references:** [repo](https://github.com/andreivonmariano-dotcom/BANNERLORD), [latest v0.68 CI](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44856889654), [v0.68 notes](V068_NATIVE_CONFIRMED_SIBLING_COMRADESHIP.md).
