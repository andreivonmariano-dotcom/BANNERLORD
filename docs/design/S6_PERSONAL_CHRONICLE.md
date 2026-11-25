# S6 | Personal Chronicle — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
A person's life history follows their real accomplishments, kin and service. The chronicle is a record of play, not an extra task list.

## Current contract
One consolidated My Chronicle from shared dossier and local hub. Historical founding chapters silent; lifetime episodes record automatic verified native current role evidence. Current vs historical title separated. Role evidence ambiguous or unavailable remains unverified. Actual battle history, personal events, household observations and native gold balance history remain read-only. No manual record-a-calling button or separate duplicate lifetime/battle/ledger menus. A native balance change has unknown cause unless direct evidence establishes it.

## Observation rules
Observe relevant native event or bounded daily state → verify exact player/actor and actual status → record unique dated evidence → display with source and historical/current distinction. Native combat and SaS host witnesses determine battle attribution; never infer personal kills, rewards or casualties from a text label. Unrelated NPC battles excluded. Record loss of office/land/current role separately from historical event. A successor has their own identity, personal episodes and oath; founder's records remain historical. No extra money, skills, renown, influence or relation for opening the chronicle.

## Proposed display
Single timeline with optional filters by family, sworn service, battle and personal affairs; default recent verified entries and a concise current status. No new top-level screen unless actual usability requires it; use current dossier/inquiry first. Historical records can be read without loading every archive into Correspondence. Show observed native fact, CU-authored statement and modeled news distinctly. Record unknown rather than infer causality. Proposed filters and additional event cards are not implemented yet.

## State and source
CU_lifetime_career_1, CU_v071c_career_chronicle, CU_v072_battle_history, CU_v073_knight_battle_acknowledgements, CU_household_accounts_1, CU_v069d_role_economy, CU_v064_living_persons. SandboxOriginBehavior.CareerChronicle.cs (ObserveLifetimeCareerAutomatically, PersonalChronicleText), BattleHistory.cs, NativeMilitaryWitness.cs, SocialRecords.cs, SandboxLifeHistoryState.cs. Keep old keys and bounds; no whole-world pairwise scan. S1/S2/S3/S4/S5 provide verified facts; S6 does not execute their actions.

## Acceptance
S6-1 through S6-5, E1 and F01/F04. Same native identities from Clan/Party, actual career transition, actual host battle/defeat/unrelated battle, read-only resource check, predecessor/successor separation, old archive and repeated reload. R6 follows verified retained systems. No retrospective fabricated accomplishments.

## Feature-level scope (8 review entries)

- CU-REVIEW-055 | **MERGE** | Native military appointments witness. Read-only history of actual role and army appointment. Source: [SandboxOriginBehavior.NativeMilitaryWitness.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.NativeMilitaryWitness.cs).
- CU-REVIEW-056 | **MERGE** | Historical three-part starting career milestones. Founder-specific and silent, no new tasks. Source: [SandboxOriginBehavior.CareerChronicle.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.CareerChronicle.cs).
- CU-REVIEW-057 | **RETIRE** | Manual career episode recording. No manual player task. Source: [SandboxOriginBehavior.CareerChronicle.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.CareerChronicle.cs).
- CU-REVIEW-058 | **KEEP** | Automatic lifetime career observations. Only verified role evidence; no guessed transitions. Source: [SandboxOriginBehavior.CareerChronicle.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.CareerChronicle.cs).
- CU-REVIEW-059 | **RETIRE** | Recurring career review and personal charges. No new chores; legacy withdrawal retained. Source: [SandboxOriginBehavior.CareerChronicle.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.CareerChronicle.cs).
- CU-REVIEW-060 | **MERGE** | Battle and service history. Record observed outcomes only. Source: [SandboxOriginBehavior.BattleHistory.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.BattleHistory.cs).
- CU-REVIEW-061 | **MERGE** | Personal chronicle with livelihood, episodes, battles and personal history. One history interface. Source: [SandboxOriginBehavior.CareerChronicle.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.CareerChronicle.cs).
- CU-REVIEW-062 | **MERGE** | Household account observations. Historical balance observations; no separate economy. Source: [SandboxOriginBehavior.SocialRecords.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.SocialRecords.cs).

## Current test cases (5, all native PENDING)

- S6-1 (D/E): Open shared dossier from Clan and Party, local My Chronicle. Expected: Same native identities and current vs historical role, one history, no duplicate Kingdom/camp entry. Evidence: Native screenshots and event logs.
- S6-2 (D/E): Observe real role change and successor. Expected: Automatic observation only with actual evidence, no false Outlaw/Wanderer inference, no founder history attributed to heir. Evidence: Native screenshots and event logs.
- S6-3 (D/E): Witness battle, defeat and unrelated battle. Expected: Exact party/host identity and verified outcome; no invented casualties or reward. Evidence: Native screenshots and event logs.
- S6-4 (D/E): Read livelihood and personal chronicle repeatedly. Expected: Read-only, actual native gold/roles, no manual career chores. Evidence: Native screenshots and event logs.
- S6-5 (D/E): Open correspondence from map, pending notification and local/archive; close/reload. Expected: Native inquiry fallback, accurate unread/pending order, no forced graphical screen, no remote effect. Evidence: Native screenshots and event logs.
