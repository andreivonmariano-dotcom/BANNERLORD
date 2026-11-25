# S5 | Career and Sworn Service — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
Live a particular station and change it through actual deeds and native status. A Knight is sworn to a person, a Soldier serves a lord, a Lord holds land, and a King actually rules.

## Current contract
Seven starting paths defined in S1. Native and external provider status determine actual occupation. SaS owns enlisted rank, wage, duties and discharge. Native mercenary/vassal/crown status and Diplomacy own contracts and politics. Knight personal oath is the distinctive CU layer: real named patron, in-person pledge/first audience, 30-day muster, field escort, scouting and service order/report, personal recognition, recommendation and royal hearing, release and renewal. Native vassalage inquiry is not automatic estate grant. Founding title is history, not permanent authority.

## Service loop
Meet actual patron → confirm eligibility and personal oath → select feasible service → perform actual witnessed task → report only observed evidence → record outcome/recognition → renew/release or seek native political status. Scout/escort must reflect actual target and movement/participation; no fake battle deed or free influence. On dead/captured/unavailable patron, service loss or changed clan, use existing guard and fail closed. Do not attribute former player's oath or military history to successor. Native/verified provider state controls all transitions and income. Provider unavailable or contradictory snapshot remains unverified, not discharged.

## Next increments
R1 verify each role's actual start and one role action. R5 improve existing Knight obligation clarity and observable task outcome before adding more service types. Later provider-supported tasks may be designed with S3 exact authority and receipts. No custom enlisted rank ladder, second wage system, promotion grind, duplicate crime system or compulsory path from Wanderer to King. Outlaw independent party alone does not prove criminal status; a missing contract alone does not prove a new Wanderer role.

## UI and data
Direct patron conversation, relevant Knight service entry and Correspondence for current actionable duties. Shared Chronicle shows witnessed service history. Avoid separate progress chores, chapter rewards or repeated career prompts. CU_v070i_knight_personal_oath, CU_v070t_knight_native_escort, CU_v071e_native_knight_orders, CU_v065_service_career, CU_v071g_native_military_witness, CU_v073_knight_battle_acknowledgements retained. SandboxOriginBehavior.KnightDialogue.cs, KnightOath.cs, KnightServiceOrders.cs, SandboxKnightEscortState.cs, SandboxKnightStandingPolicy.cs, SandboxSaSBridge.cs.

## Acceptance
S5-1 through S5-6, S1/E1 relevant cases, full provider tests and S6 battle observation. Actual pledge, muster, escort/scouting evidence, report, recommendation/royal hearing without fake title, release/renew, loss of crown/fief, SaS incomplete/disconnected, no duplicate pay or inherited oath. R5 requires native evidence; source compilation is not duty completion.

## Feature-level scope (6 review entries)

- CU-REVIEW-049 | **KEEP** | Patron pledge, first meeting and expectations. Personal oath distinct from vassalage. Source: [SandboxOriginBehavior.KnightDialogue.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KnightDialogue.cs).
- CU-REVIEW-050 | **KEEP** | Knight muster and oath release/renewal. Identity and real presence required. Source: [SandboxOriginBehavior.KnightOath.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KnightOath.cs).
- CU-REVIEW-051 | **KEEP** | Knight field escort. Observed co-location and duty; no duplicate rewards. Source: [SandboxKnightEscortState.cs](../../Source/CalradiaUnified/SandboxKnightEscortState.cs).
- CU-REVIEW-052 | **KEEP** | Knight scouting and muster service order. Real native evidence and direct report. Source: [SandboxOriginBehavior.KnightServiceOrders.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KnightServiceOrders.cs).
- CU-REVIEW-053 | **KEEP** | Knight personal recognition and recommendation. No auto-granted fief, rank or vassalage. Source: [SandboxKnightStandingPolicy.cs](../../Source/CalradiaUnified/SandboxKnightStandingPolicy.cs).
- CU-REVIEW-054 | **KEEP** | Royal hearing and native vassalage inquiry. Native political entry remains authoritative. Source: [SandboxOriginBehavior.KnightServiceOrders.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KnightServiceOrders.cs).

## Current test cases (6, all native PENDING)

- S5-1 (D/E): Knight pledge, defer then actual oath and muster. Expected: Correct named patron and oath holder; no fake vassalage. Evidence: Native state, receipt, screenshots and reload.
- S5-2 (D/E): Complete real escort and scouting/order report. Expected: Only observed actual service evidence, no duplicate reward. Evidence: Native state, receipt, screenshots and reload.
- S5-3 (D/E): Recommendation, royal hearing and native vassalage inquiry. Expected: No unearned fief, rank or title. Evidence: Native state, receipt, screenshots and reload.
- S5-4 (D/E): Release, break and renew under successor. Expected: Exact actor/patron status, no former oath inherited. Evidence: Native state, receipt, screenshots and reload.
- S5-5 (D/E): Mercenary ends contract and Soldier provider disconnects/rejoins. Expected: No fabricated discharge/rank/pay; uncertainty stays unverified. Evidence: Native state, receipt, screenshots and reload.
- S5-6 (D/E): Lord loses fief, King loses crown. Expected: Current native authority revoked; history retained. Evidence: Native state, receipt, screenshots and reload.
