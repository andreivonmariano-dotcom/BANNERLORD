# CU | Moment-to-moment gameplay design
25 November 2026. Companion to the six GDDs. Scope is actual gameplay loops, not a promise that all scenarios are implemented. Source changes d722f7d and subsequent cleanup; CI and native acceptance tracked separately.

## Interaction standard
A player should be able to identify the person, current situation, available decision, cost and observable result. A useful interaction gives at least one of: a meaningful choice, real responsibility, useful information or personal memory. A memory is not a resource reward. Actions that cannot execute must be hidden or explained; no fake NPC work, compulsory chores or competing provider systems. Reuse native interfaces and current handlers.

## S1 | First day in Calradia

**Situation and choices:** Choose independent Wanderer or family-bearing noble; review actual obligations, then confirm.

**Expected consequence:** Real identity, applicable native clan, roster, holdings and contract; no unsupported start.

**Evidence:** S1-1;S1-6;S1-08;S1-09. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R1.

## S2 | Return to your family

**Situation and choices:** Speak to actual spouse about children; visit, choose explicit gift, or leave; review home.

**Expected consequence:** Visit is memory; gift transfers real 100 denars once; native residence and children unchanged by talking.

**Evidence:** S2-1;S2-2;S2-5. Status CONTEXTUAL_CHOICE_SOURCE_CI_PENDING; milestone R3.

## S3 | The company needs a scout

**Situation and choices:** Speak to actual adult household member in party; assign Scout if raw slot vacant; inspect native Party; recheck after reload.

**Expected consequence:** Native raw/effective role confirmed; other slots unchanged; occupied slot hides choice; no new pay.

**Evidence:** S3-2;S3-06;S3-07;S3-10. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R4.

## S3 | A castle needs a governor

**Situation and choices:** Speak to eligible local unassigned relative at owned unbesieged vacant seat; give actual appointment or defer.

**Expected consequence:** Actual Town.Governor and Hero.GovernorOf match; no teleport or incumbent replacement.

**Evidence:** S3-05;S3-06;S3-08. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R4.

## S4 | A character asks for help

**Situation and choices:** Open actual pending concern; ask reason, accept real request if funded, refuse or defer; reopen correspondence.

**Expected consequence:** Transfer/relation proof for applicable petition; no remote payment; memory exact pair; hearing not an order.

**Evidence:** S4-1;S4-2;S4-3;S4-4;S4-5;S4-6. Status CONTEXTUAL_CHOICE_SOURCE_CI_PENDING; milestone R3.

## S4 | A retainer worries about defenses

**Situation and choices:** Ask concern at owned fortification; read current actual garrison; use provider/native controls or defer.

**Expected consequence:** CU does not recruit, train or promise an unissued order; native/provider effect not credited to CU.

**Evidence:** S3-08;S4-1. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R4.

## S5 | A Knight's sworn obligation

**Situation and choices:** Meet named patron; pledge, muster, undertake actual escort/scouting and report; renew or release.

**Expected consequence:** Witnessed task and exact patron; no free vassalage or fabricated service; native rank authoritative.

**Evidence:** S5-1;S5-2;S5-3;S5-4. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R5.

## S6 | The end of a campaign chapter

**Situation and choices:** After native battle, appointment and career transition, open same Chronicle from dossier/local hub; compare readback.

**Expected consequence:** Exact person and dated witnessed events; no additional rewards; successor distinct; no invented cause.

**Evidence:** S6-1;S6-2;S6-3;S6-4. Status COMPILED_BASELINE_NATIVE_PENDING; milestone R6.

## S2/S4 | A child comes of age

**Situation and choices:** Observe actual native adulthood and activation; offer eligible conversation or appointment, not synthetic milestone reward.

**Expected consequence:** Real identity and lifecycle; optional event prompt with dedupe only after future implementation.

**Evidence:** S2-2;S2-3. Status PROPOSED_NOT_IMPLEMENTED; milestone R3.

## Gameplay feedback targets (proposed QA thresholds, not results)
Each actionable concern should expose its feasible next action from the same conversational topic or tell the player who owns the next action. Count CU clicks from topic to action confirmation, actual decision types, repeated no-effect lines, inaccessible choices, wrong actor and native effect errors. Target <=3 CU player choices after entering a relevant topic; no action should need a second independent ledger. Measure enjoyment, clarity, immersion, agency and repetition after each scenario on a 1–5 scale; never average away a crash or incorrect mutation. A below-3 score triggers observed usability issue, not automatic reward tuning. Compare equivalent native control and full-stack play. No fabricated player feedback.

## Implemented source refinement
Family topic now offers visit and the existing explicit 100-denar gift directly; the same guard, cooldown and native gold transfer handler is reused. Gift log now records exact payer/recipient/day and both native balances before/after. Concerns can open the speaker's existing pending petition. Follow-up binds both player and actor IDs to prevent a successor reusing stale transient context. No new relation reward, autonomous family event, task system or save schema. Future actual family milestone encounters remain gated by R1/R2.

[Scenario register](../Data/CU_Moment_To_Moment_Scenarios.csv) | [Seven-career playtest campaign](CU_SEVEN_CAREER_PLAYTEST_CAMPAIGN.md).