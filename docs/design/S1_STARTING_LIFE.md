# S1 | Starting Life — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
A new Sandbox begins with a coherent person, not an arbitrary set of bonuses. Choose one of seven lives, customize identity and applicable household, then enter the actual native campaign. Career is current social position, not a rigid class or skill tree. Native culture, FaceGen and game-owned character creation remain authoritative.

## Current playable contract (source / compiled baseline, native acceptance pending)
- Wanderer: independent landless clan, optional one actual companion, no House stage. Native wages; no reimbursement.
- Outlaw: independent landless band, six low-tier bandits, actual lieutenant and 14-day native wage reserve plus food. No inferred criminality from party size. Fourberie/native own crime.
- Mercenary: actual native employer contract, valid kingdom/party command, landless, native contract pay, optional family. No CU salary.
- Soldier: applicant until compatible ServeAsSoldier confirms actual recruit/private enlistment, host and lord. Provider owns grade, duties, wage and discharge. No rank selector or fabricated status.
- Knight: independent landless sworn house, real named patron, personal oath, 30-day muster, default Squire, optional family. No automatic vassalage or unsupported younger-sibling contract.
- Lord: real landed vassal, noble spouse, at least one child, actual Master of Arms and Knight. Native fief and affiliation verified.
- King: real ruling clan, owned royal seat, spouse/child, actual Marshal, Minister and Kingsguard; actual native governor seat. Loss of crown changes current standing.
Historical Recruit, HouseholdKnight, NobleCadet, Admiral, Trader and Beggar are parsed only for old saves.

## Loop and interface
World/identity → native culture and face → applicable House/fortune and native stats → final review → preflight → native application → readback. Exact creator stage sequence must match current guided controller; retain native banner/clan/review/difficulty and Back/Continue. No duplicate FaceGen or arbitrary additional career choices. Show conditional controls only. Explain fixed role grants, unavailable provider, missing elder and impossible family before irreversible finalization. Do not default hidden settings to a grant.

## State and rules
Starting request is a draft until native application. Normalize only fresh guided drafts, not loaded historical pending Soldier data. Record owner, requested role, status, reason and actual start contract. Validate culture IDs, legal native clan affiliation, actual parentage/spouse, companion limit, resources and holdings. No replay after partial failure or reload. Record partial state honestly, preserve recovery report, no unsupported property rollback. No new save schema in this design. Key families: CU_v016_origin_applied, CU_start_audit_*, CU_v070g_soldier_native_enlistment_pending, CU_v070g_soldier_pending_origin_profile, CU_v069d_family_recovery.

## Current source
SandboxOriginBehavior.cs (RegisterEvents, TryApplyOnce, RecordStartAudit, VerifyRequestedFamilyIdentity), SandboxStartingLifePolicy.cs, CUGuidedCreatorVM.cs, SandboxStartContract.cs, SandboxRoleAffiliations.cs, SandboxFamilyCreator.cs, SandboxSaSBridge.cs. Seven-role source contract: ../CU_V0741_SEVEN_SINGLE_PATH_STARTS.md.

## Next design increments
R1 verify actual start for each role, R2 repair reproduced creator/readability failures, then make role previews state the real starting responsibilities rather than adding fields. No eighth role or unsupported subtype. No custom education, equipment or income engine. Acceptance: C01, S1-1 through S1-7, S1-08/09, E1-1 through E1-7. Tests must show actual native clan, parents, roster, fiefs, contract, assets and reload, not a screenshot of a summary.

## Feature-level scope (25 review entries)

- CU-M2-003 | **KEEP** | Standalone Sandbox starting profile and guarded new-game resources. Guided creation only, guarded one-shot native resources. Source: [SandboxOriginBehavior.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.cs).
- CU-M6-003 | **KEEP** | Embedded native Sandbox Starting Life narrative menus. Keep supported guided native flow; no second creator. Source: [NativeStartingLifeMenus.cs](../../Source/CalradiaUnified/NativeStartingLifeMenus.cs).
- CU-M2-005 | **KEEP** | Opt-in native mercenary and vassal starting affiliations. Use native start authority; no fabricated rank or political ownership. Source: [SandboxRoleAffiliations.cs](../../Source/CalradiaUnified/SandboxRoleAffiliations.cs).
- CU-M6-004 | **MERGE** | Independent Sandbox character creator front-end. Existing shared guided overlay, not separate active creator route. Source: [SandboxCreatorOverlay.cs](../../Source/CalradiaUnified/SandboxCreatorOverlay.cs).
- CU-M6-005 | **KEEP** | Direct custom Sandbox main-menu entry. One guided new-game entry; no in-campaign next-game editor. Source: [SandboxMainMenuEntry.cs](../../Source/CalradiaUnified/SandboxMainMenuEntry.cs).
- CU-M2-006 | **KEEP** | Native Advanced Start and post-creation family handoff. Guarded native start and family handoff. Source: [SandboxNativeStartOptions.cs](../../Source/CalradiaUnified/SandboxNativeStartOptions.cs).
- CU-M6-006 | **KEEP** | Observed start contract for real rank kingdom fief spouse and children. Start contract and diagnostics, not a player chore. Source: [SandboxStartContract.cs](../../Source/CalradiaUnified/SandboxStartContract.cs).
- CU-M6-009 | **KEEP** | Consented kingdom-valid starting fief transfer. Only consented valid new-game start; no later competing estate grants. Source: [SandboxStartingFief.cs](../../Source/CalradiaUnified/SandboxStartingFief.cs).
- CU-M6-013 | **MERGE** | Five-step Gauntlet start conditions and campaign summary. Historical five-step prefab not an extra entry. Source: [CUSandboxCreator.xml](../../Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml).
- CU-M6-016 | **KEEP** | Normalize Vlandian display culture to exact vlandia ID. Data correctness, not an extra feature. Source: [SandboxFamilyOriginPolicy.cs](../../Source/CalradiaUnified/SandboxFamilyOriginPolicy.cs).
- CU-M6-018 | **KEEP** | Commit numeric slider values and reset old dependent counts for each new Sandbox. Correct UI state and dependent counts. Source: [SandboxCreatorOverlay.cs](../../Source/CalradiaUnified/SandboxCreatorOverlay.cs).
- CU-M6-019 | **KEEP** | Coordinate native character creation as phase two without duplicate appearance page. One native appearance stage. Source: [CUSandboxCreator.xml](../../Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml).
- CU-M6-023 | **DEFER** | Optional appended native 3D origin review preserving culture and FaceGen. Keep graphical review optional, require actual native evidence. Source: [CUNativeOriginReviewStage.cs](../../Source/CalradiaUnified/CUNativeOriginReviewStage.cs).
- CU-M6-025 | **KEEP** | Revised five-chapter Gauntlet household creator with native handoff cues. Retain current single native handoff and prune older routes. Source: [CUSandboxCreator.xml](../../Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml).
- CU-REVIEW-001 | **KEEP** | Guided identity, age, attributes, traits and skills. One guided route. Preserve valid character choices. Source: [CUGuidedCreatorVM.cs](../../Source/CalradiaUnified/CUGuidedCreatorVM.cs).
- CU-REVIEW-002 | **KEEP** | World scenario, kingdom and named patron. Native supported affiliation only. Source: [SandboxNativeStartOptions.cs](../../Source/CalradiaUnified/SandboxNativeStartOptions.cs).
- CU-REVIEW-003 | **KEEP** | Seven single-path selector and retired subtype parsing. Keep seven routes and old-save parsing; no new unsupported subtypes. Source: [SandboxStartingLifePolicy.cs](../../Source/CalradiaUnified/SandboxStartingLifePolicy.cs).
- CU-REVIEW-004 | **KEEP** | Wanderer optional single companion. Real companion; no artificial wage subsidy. Source: [SandboxWandererCompanion.cs](../../Source/CalradiaUnified/SandboxWandererCompanion.cs).
- CU-REVIEW-005 | **KEEP** | Outlaw starting paid band. Starting roster and resources only; no crime engine. Source: [SandboxOutlawStartingBand.cs](../../Source/CalradiaUnified/SandboxOutlawStartingBand.cs).
- CU-REVIEW-006 | **KEEP** | Mercenary native contract start. No second payroll, contract or rank system. Source: [SandboxMercenaryStartPolicy.cs](../../Source/CalradiaUnified/SandboxMercenaryStartPolicy.cs).
- CU-REVIEW-007 | **KEEP** | Soldier provider enlistment and observation. Provider authority, no CU enlistment mutation. Source: [SandboxSaSBridge.cs](../../Source/CalradiaUnified/SandboxSaSBridge.cs).
- CU-REVIEW-008 | **KEEP** | Knight single sworn-house start. No new younger-sibling start or free fief. Source: [SandboxKnightStartPolicy.cs](../../Source/CalradiaUnified/SandboxKnightStartPolicy.cs).
- CU-REVIEW-009 | **KEEP** | Lord real landed house start. Verify real clan, title, fief, family and retinue. Source: [SandboxLordStartPolicy.cs](../../Source/CalradiaUnified/SandboxLordStartPolicy.cs).
- CU-REVIEW-010 | **KEEP** | King actual ruling-house start. Verify real crown, royal seat, family and officers. Source: [SandboxKingStartPolicy.cs](../../Source/CalradiaUnified/SandboxKingStartPolicy.cs).
- CU-REVIEW-011 | **KEEP** | Starting resource and cultural equipment validation. One-time guarded creation; never continuous gear override. Source: [SandboxStartContract.cs](../../Source/CalradiaUnified/SandboxStartContract.cs).

## Current test cases (10, all native PENDING)

- C01 (C): Clean launch and new Sandbox; return menu, repeat. Expected: Native launcher recognises module and no reproducible crash. Evidence: Exact DLL hash, modules, session log.
- S1-1 (C/D): Create independent character with optional companion. Expected: No fiefs or contract; real chosen companion; no wage subsidy. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-2 (C/D): Create independent band and lieutenant. Expected: Real roster, reserve and native status; no inferred criminality. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-3 (C/D): Choose kingdom contract; later release. Expected: Actual mercenary status, native pay and no extra salary. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-4 (C/D): Enlist through compatible SaS. Expected: Actual host, lord, grade, wage/duty and provider confirmation. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-5 (C/D): Create independent sworn house; meet real patron and Squire. Expected: Exact personal oath, no automatic land/vassalage. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-6 (C/D): Create landed vassal, noble family and two officers. Expected: Native clan leader, owned fief, real kin and officers. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-7 (C/D): Create ruling house and native royal seat. Expected: Actual crown, family, Marshal/Minister/Kingsguard, governor. Evidence: Same-session native start PASS, actual screenshots and native state.
- S1-08 (C/D): Back/Continue through all creator controls; test 1280x720, 1650x927 and 1920x1080. Expected: Seven selectable careers, native culture and face stages intact; invalid requests blocked. Evidence: Screenshots and actual click paths.
- S1-09 (D/E): Try impossible family/fief/patron and provider absent. Expected: Preflight fails closed, no replay on reload. Evidence: Contract refusal and state.
