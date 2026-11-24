# CU stabilization pass 2 | all-role, UI and persistence audit

Scope: existing mechanics only, no new role, menu, save key or third-party dependency. Draft PR #93 remains unmerged.

## Verified pass 1
Windows run 45351263526 completed successfully at 886b17fb7b57dcfbc14e83a2f37e8effe9678f91. Logs: 1127 executed policy checks, zero warnings/errors, DLL SHA-256 2f2fff4440d5736ff04cda63a690bbcbfb189dfaee62e214bcb91942a00a5dd6. Artifact 14900421690. This is not installed-game acceptance.

## Q1: actual selectable role inventory
The historical 12-role count is stale. SandboxOriginProfile.Roles contains 13 IDs; SandboxStartingLifePolicy.SevenPaths contains seven newer guided paths. Existing role IDs and old save mappings must be retained. Source screening, not live acceptance:

| Role | Native start contract | Live test focus |
|---|---|---|
| Wanderer | default; newer no-House option | companion identity, actual gold debit vs reimbursement, three-town chronicle |
| Outlaw | outlaw | six bandits and lieutenant, real food and payroll reserve, independent clan |
| Mercenary | mercenary | actual contract, own party, no fief, consent and costs |
| Soldier | guided default then real SaS; legacy mercenary | correct provider, requested rank, real lord, no fake wages or repeated phase-9 |
| Recruit | default | applicant, no unobserved enlistment |
| HouseholdKnight | default | real patron, landless, no automatic vassalage |
| Knight | guided default sworn household; legacy vassal | oath/escort, native authority, no fief for guided path |
| NobleCadet | default | real native elder/parents, landless, birth order, no implied title |
| Lord | vassal | actual fief, spouse/children, native offices and conserved officer transfer |
| King | king | ruling clan, fiefs, actual council, governor eligibility, succession claims only |
| Admiral | fleetadmiral | actual War Sails scenario, native fleet, land/sea handoffs |
| Trader | trader | native independent trade, no free or repeated funds |
| Beggar | beggar | native independent status and selected conditions |

Source defect: profile-aware Knight label existed but biography capture called legacy string-only Station/Occupation. Guided Soldier also got legacy mercenary labels in creator/biography. Use profile-aware policies, explicitly pass successful soldier receipt into initial historical capture. Keep historical mappings for old profiles; preflight UI describes requested status, not a current native appointment.

## Q2: active UI and lifecycle
- World: SandboxCreatorMenuLayer on actual animated native main menu; CUGuidedCreatorVM. Native Review-shell generated first, art/plain fallback retained.
- Native character creation: CUNativeStateShell native-first dispatch retains FaceGen; CUNativeStatsLayer and CUNativeHouseLayer own focused custom chapters. Experimental ribbon/skin/dock remain gated; do not delete compiled fallback sources without reachability evidence.
- Campaign: BUTR Clan/Party/Kingdom mixins, household camp, family gallery, noble dossier, MCM, settlement/hero dialogues. Check actual native screen ownership, text, focus, input and restoration with Customisable UI and Diplomacy. Old SandboxOriginPanel excluded from DLL; older NativeSettingsBehavior not registered by current SubModule.
- Fixed source lifecycle: unexpected World overlay detach formerly left creatorOpen true, blocking reopen. Cancel abandoned draft; clear accepted/queued state on failed attachment or refused launch; no new screens. Need actual Back/Escape, interrupted screen and native fallback tests. Intentional legacy opt-out unchanged.

## Q3: save and compatibility
- Family recovery reader previously accepted a counts-match status inconsistent with requested counts or with zero checks. Normalize to review-required while preserving failure and all counts. No native family retry.
- Incomplete SaS enlisted snapshot with missing lord was previously treated as actual absence by daily observer. Mark incomplete or unrecognized enlisted snapshots unavailable (not discharged); existing suspension route preserves uncertainty. No external payroll or party changes.
- Existing daily observation scans native Hero lists and creates maps of bounded records. Life history caps: 64 people, 192 bonds, 96 events. Social bonds cap 32. These are bounds on saved data, not measured frame time. UI Tick runs per frame; heartbeat writes every 45 seconds. Inspect real session size, daily tick latency, save latency and frame pacing before changing algorithms.
- Remaining overlap gates: automated garrison/levy/recruit/party mutations off in full stack; actual effective INI unknown. Officer transfer remains separate player action requiring rollback/conservation evidence. Wanderer reimbursement audit remains open. No assumption that exact third-party binaries have been inspected.
- Added five executable pure-policy checks plus a source lifecycle/bridge wiring check. No test passes claimed before CI.

## Test protocol
Exact installed DLL hash and effective user INI first. Control (official modules + four frameworks + CU) and 41-module integration profiles; do not rewrite active profile. Test all 13 roles, six world scenarios and nine noble birth positions as applicable, actual native family and resources, native stage navigation and three resolutions, five household folios, Clan/Party/Kingdom, MCM, all role menus and conversations, save/quit/reload, 30 campaign days and 60-minute smoke, naval transition. Record actual logs/screens and reject unknown evidence. No release until C–F acceptance.
