# S3 | Household and Retainers — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
Ask real people to take responsibilities they can actually perform. Their replies identify what they now do, and reports reflect native state. A title alone is not a job.

## Current contract
Generated cultural companions retain actual native IDs, gear, skills and historical personal office labels. The shared responsibility submenu is available from direct conversation, private matters and appropriate Concerns/Family follow-ups. It exposes only executable native vacant appointments: Governor, Quartermaster, Scout, Surgeon and Engineer. Current duty report uses NativeHouseholdAssignment and does not grant another office. Real court replacement/restoration remains separate and conditional.

## Native command rules
Actor: reciprocal adult spouse, adult child or registered clan companion, alive/active, same clan, not prisoner/fugitive/traveling, relation >=0. Player: active leader of own clan and relevant party, no incompatible SaS status. Local governor: owned unbesieged fortification, actual actor/player at target, no existing party or governorship, Town.Governor null, no army/sea/map event; invoke ChangeGovernorAction.Apply and verify reciprocal Town.Governor/Hero.GovernorOf. Party specialist: actual actor in main roster exactly once, raw slot vacant, no existing specialist role, CanAssignMoreRolesToHero, no unsafe army/sea/battle/service; invoke SetHeroPartyRole and verify raw holder, GetHeroPartyRoles, effective holder, other five land/naval slots unchanged. Appointment costs no extra CU gold, rank or relationship. CU-authored reply is not independent consent. Failure or unconfirmed effect must not be reported as complete. Do not silently overwrite incumbent role or teleport.

## Practical command workflow
Ask → show available exact responsibility → explicit choice → recheck eligibility → native action → compare post-state → confirmed or unconfirmed response → view actual duty in native Party/Clan and dossier. Same handler for all entrances. Reading a report is read-only. Release/reassign uses native interface unless a separately tested native command exists. Remember only exact player/actor confirmed assignments, and re-read duty on subsequent conversation.

## Deferred delegation
When a retainer mentions garrison weakness, show actual garrison count and direct player to native/Improved Garrisons controls. No promise of recruiting from dialogue. Improved Garrisons owns recruitment, training, patrols and guards. The guarded five-recruit manual transfer is a diagnostic experiment, disabled with provider present; no autonomous task. Future provider-supported jobs only after documented public API contract, actual authority and cost. Proposed states Offered → Rejected or Accepted → In progress → Completed/Failed/Cancelled; persistent unique ID, exact actor/target/provider, budget, timeout, no double charge, actual result receipt. No general remote job engine or parallel economy yet.

## UI and source
One My Household dossier (Clan/Party), face-to-face responsibilities, actual duty report, contextual royal court. No fake office pay/rank, extra office roster, technical receipt screen or separate Kingdom/camp entry. SandboxOriginBehavior.NobleOfficerDialogues.cs (CanAskResidentToGovern, HasHouseholdCommandServiceAuthority, CanAssignHouseholdSpecialty, AssignHouseholdSpecialty), HouseholdDuties.cs, KingCouncil.cs, SandboxCulturalRetinue.cs, CUBUTRNativeScreens.cs. CU_v071c_household_duties and CU_retainer_service_1 legacy keys retained. Appointment memories use CU_v064_living_persons; actual assignments live in native saves.

## Acceptance
S3-1 through S3-10. Test all five current appointments, absent/incumbent/raw/effective slots, existing naval role, SaS, battle/siege/sea, minor/unrelated/captive, duplicate click, current duty removal and reload. No garrison mutation on full stack. R4 remains gated; native action acceptance precedes new delegated tasks.

## Feature-level scope (16 review entries)

- CU-REVIEW-023 | **KEEP** | Generated retinue and cultural skill/gear. One-time creation; no ongoing gear overwrite. Source: [SandboxCulturalRetinue.cs](../../Source/CalradiaUnified/SandboxCulturalRetinue.cs).
- CU-REVIEW-024 | **KEEP** | Squire, outlaw lieutenant and noble officer identities. Identity and service context, not invented power. Source: [SandboxRetinueOfficePolicy.cs](../../Source/CalradiaUnified/SandboxRetinueOfficePolicy.cs).
- CU-REVIEW-025 | **RETIRE** | Parallel household office appointment and pay. New artificial office/rank/pay routes removed; release old duty remains. Source: [SandboxOriginBehavior.HouseholdDuties.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.HouseholdDuties.cs).
- CU-REVIEW-026 | **IMPROVE** | Officer reports and counsel. Useful observed context only; no claim report executes an order. Source: [SandboxOriginBehavior.HouseholdDuties.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.HouseholdDuties.cs).
- CU-REVIEW-027 | **KEEP** | Governor appointment by conversation. Actual vacant governor action, no teleport or incumbent replacement. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-028 | **KEEP** | Quartermaster appointment. Native vacant role, no second stewardship model. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-029 | **KEEP** | Scout appointment. Native vacant role, no second scouting model. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-030 | **KEEP** | Surgeon appointment. Native vacant role, no competing medicine model. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-031 | **KEEP** | Engineer appointment. Native vacant role, no separate siege model. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-032 | **KEEP** | Royal court seat, minister, marshal and Kingsguard. Actual attendance and governorship; politics remain native. Source: [SandboxOriginBehavior.KingCouncil.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KingCouncil.cs).
- CU-REVIEW-033 | **KEEP** | Royal officer replacement and court restoration. Only existing verified authority; no free titles. Source: [SandboxOriginBehavior.KingCouncil.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KingCouncil.cs).
- CU-REVIEW-034 | **DEFER** | Royal hall preparation. Explicit CU_ROYAL_HALL_TRIAL only. Source: [SandboxOriginBehavior.KingCouncil.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KingCouncil.cs).
- CU-REVIEW-035 | **DEFER** | Garrison troop transfer experiment. No direct transfer with provider loaded. Source: [SandboxOriginBehavior.NobleOfficerDialogues.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NobleOfficerDialogues.cs).
- CU-REVIEW-036 | **DEFER** | Delegated external recruiting, patrol and estate budgets. No verified supported command API. Do not create competing AI. Source: [CU_COMPLEMENTARY_GAMEPLAY_AUDIT.md](../../docs/CU_COMPLEMENTARY_GAMEPLAY_AUDIT.md).
- CU-REVIEW-063 | **KEEP** | Household dossier in Clan and Party. Both open same household screen. No Kingdom duplicate. Source: [CUBUTRNativeScreens.cs](../../Source/CalradiaUnified/CUBUTRNativeScreens.cs).
- CU-REVIEW-069 | **KEEP** | Native encyclopedia profession row. Scoped native row, no global UI replacement. Source: [CUBUTRNativeScreens.cs](../../Source/CalradiaUnified/CUBUTRNativeScreens.cs).

## Current test cases (10, all native PENDING)

- S3-1 (D/E): Speak to present unassigned Hero, assign Quartermaster to vacant slot; inspect party and reload. Expected: Exact raw and effective native holder; five other land/naval slots unchanged; actual specialty behavior. Evidence: cu_household_native_party_duty PASS plus native before/after role screenshots and reload.
- S3-2 (D/E): Speak to present unassigned Hero, assign Scout to vacant slot; inspect party and reload. Expected: Exact raw and effective native holder; five other land/naval slots unchanged; actual specialty behavior. Evidence: cu_household_native_party_duty PASS plus native before/after role screenshots and reload.
- S3-3 (D/E): Speak to present unassigned Hero, assign Surgeon to vacant slot; inspect party and reload. Expected: Exact raw and effective native holder; five other land/naval slots unchanged; actual specialty behavior. Evidence: cu_household_native_party_duty PASS plus native before/after role screenshots and reload.
- S3-4 (D/E): Speak to present unassigned Hero, assign Engineer to vacant slot; inspect party and reload. Expected: Exact raw and effective native holder; five other land/naval slots unchanged; actual specialty behavior. Evidence: cu_household_native_party_duty PASS plus native before/after role screenshots and reload.
- S3-05 (D/E): Assign local eligible unassigned resident to vacant owned fortification governorship. Expected: Town.Governor and Hero.GovernorOf agree; no teleport or replacement. Evidence: cu_household_native_governor_order PASS plus native Clan/settlement readback and reload.
- S3-06 (D): Attempt occupied slot, absent incumbent, existing specialist/naval role, unrelated/dead/minor/captive, hostile relation, no authority. Expected: No option or clear refusal; no slot/resource changes. Evidence: Before/after exact slots, negative observation.
- S3-07 (D): Attempt appointment in provider service, army/battle/sea or siege. Expected: No mutation or unsafe command. Evidence: SaS/army/naval state and all slot readback.
- S3-08 (D): With Improved Garrisons enabled, inspect defense recommendation and held transfer commands. Expected: No CU recruit, patrol, garrison wage/training/transfer; provider remains owner. Evidence: Loaded module evidence, native rosters, absence of CU mutation receipt.
- S3-09 (D/E): Actual vacant royal officer replacement, restore court after seat loss. Expected: Native authority retained, no replacement of temporarily absent live incumbent. Evidence: Exact identities, governor and court records; reload.
- S3-10 (D): Attempt repeat or rapid appointment and re-open dialogue. Expected: No duplicate effect, no other specialist lost. Evidence: Native role slots and one confirmed receipt.
