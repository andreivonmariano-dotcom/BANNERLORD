# Calradia Unified | Master six-system roadmap v2.0

25 November 2026 | Bannerlord PC 1.5.7.125802 | War Sails 1.3.7.125802 | CU v0.74.0 | 41 selected modules | Gate B, COMPILED_UNTESTED.

Runtime source: `e0f94b48dc05e3e92e44afce8cfbce1faf7ca3eb`. Windows CI [45627703971](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45627703971) and [45627718762](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45627718762) succeeded. The exact runtime artifact [15029171592](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45627703971/artifacts/15029171592) was independently opened: outer and inner ZIP CRC pass, one Modules/CalradiaUnified directory, one CalradiaUnified.dll, manifest v0.74.0 and matching CU-BuildIdentity.json. DLL SHA-256 `495beb3fe7a62ca40df354c4fa918cb71e1a6ca6a1f5d9d7825043908a0357c5`. Inner runtime ZIP SHA-256 `bf9077bc0fde06eb3eaf655040dbc34408bc7edc7250891fa231f48409ac5551`, 463780 bytes. Outer artifact SHA-256 `11cc78cbaa44151851bc135d5e0ca375a16304311d0ef5fe6281190dceac1a86`. The package contains the pre-existing read-only Export-CU-Diagnostics.ps1 under ModuleData, not an installer; retain its known purpose or move it to source-only distribution in a later verified packaging change. No live installation, gameplay or save/reload claim. Prior playtest session 20261125T122817_ca382bca used a different DLL, 6726da1024f4754cc65fcca9381fe6b44f642b3c05f53bc147b2fd3b7fd7c656, and is historical evidence only.

## Mission and current scope

Calradia Unified is a medieval personal life, household, dynasty and roleplaying expansion for Bannerlord. Complement the active curated modlist rather than reconstructing competing systems. Favor genuine native consequences, a small comprehensible interface and character identity over feature count. Six systems only. Seven selectable careers only; historic Recruit, HouseholdKnight, NobleCadet, Admiral, Trader and Beggar IDs are legacy parsing/history, not new paths. No forced career ladder. Keep all old save keys readable. This roadmap supersedes older reconstruction plans and previous 12/13-role directives. The user's latest instruction authorizes consolidation, meaningful interaction and planning, not an unverified release or a new general-purpose NPC simulator.

### S1 | Starting Life

**Retain:** One guided creator, real native culture/face/identity/attributes, seven starting paths, resources and family preflight. Native affiliations, fiefs and registrations verified after actual actions.

**Next approved design:** Verify each start, no unsupported subtype; improve confusing steps only from live evidence. Keep creator native controls and existing safeguards.

**Done when:** Seven separate starts on full stack, actual kin/party/clan/fief/contract/identity, no duplicate grants after load.

### S2 | Family and Dynasty

**Retain:** Reciprocal spouse, children, siblings, initial traits/skills, cultural names, actual residence, portraits, genealogy, succession history. Native lifecycle, education and inheritance remain authoritative.

**Next approved design:** Event-led family interactions around actual births, marriage, adulthood, loss and residence. Distinct consented activities and requests with real available outcomes. Do not make minor children active agents or restore automatic education bonuses.

**Done when:** Exact native family links, location, age and death transitions; one event per witnessed occurrence; no false parentage or property; copied save/reload.

### S3 | Household and Retainers

**Retain:** Real native companions and personal offices. Governor plus four native specialist commands (Quartermaster, Scout, Surgeon, Engineer). Direct native effects not autonomous missions.

**Next approved design:** Unify context-sensitive assignment and current-duty report. Offer only feasible real responsibilities, disclose blocked reasons, verify before/after. Provider-approved remote jobs only after contract proof. No rival recruitment, patrol, salary or progression.

**Done when:** Vacant native slots only; raw and effective holder confirmed, other land/naval slots unchanged, correct roster and authority; negative cases, save/reload, actual gameplay benefit.

### S4 | Living Relationships

**Retain:** Private Concerns, Family, Standing, native-trait voice, exact-pair memory, real gold/relationship petitions, family gift, bounded correspondence. Routine family visit/affection/repair are memory-only.

**Next approved design:** Connect actual events to specific choices, contextual responsibility shortcuts, honest refusal and outcome memory. No arbitrary relation farming or fake completed tasks. Ordinary news nonactionable, speculative peers/stories off.

**Done when:** Exact speaker and owner, valid request and cost, actual gold debit/credit/relation before/after, no replay; safe exits and meaningful choice feedback.

### S5 | Career and Sworn Service

**Retain:** Wanderer, Outlaw, Mercenary, Soldier, Knight, Lord, King. Knight has personal oath, muster, scouting, escort, report, recognition, recommendation and release/renewal. All native/provider ranks and political actions remain authoritative.

**Next approved design:** Verify actual role-specific obligations first; improve Knight service only where action is distinct. No mandatory career ladder or eighth role. Native transitions are witnessed, not invented.

**Done when:** Each career's native/provider start contract and one genuine role action; service loss, changed authority, oath identity, death/capture/reload and no duplicate pay.

### S6 | Personal Chronicle

**Retain:** One history access point for witnessed career, personal history, battle, livelihood and lifetime observations. Founder chapters historical; no manual career chores.

**Next approved design:** Event evidence tied to actor IDs and dates, clearer current vs historical titles, merge family/service receipts, reduce repeated account/record UI. Keep legacy records readable without assigning them to heirs.

**Done when:** Read-only views agree with actual native state; no mutation; observed role change and successor identity remain distinct, no fictional victories or income.

## Player experience and interface budget

One guided creator. Four primary campaign access points: My Household (same shared dossier from Clan and Party), Correspondence (map and local affairs, native InquiryData default, pending/unread only), My Chronicle (merged career/livelihood/battle/personal history), and personal face-to-face conversations. A role-specific Knight oath, royal court or succession view may remain where it is actually relevant. Archive history and troubleshooting away from immediate decisions. Do not create a new top-level menu for an existing report. The removed Kingdom and illustrated camp entries, separate portraits entry, duplicate ledger and manual career actions stay retired. Preserve old exit-only routes and saved state. Native correspondence graphical probe is explicit one-shot diagnostic only; no requirement to enable it in ordinary acceptance. Royal hall preparation is opt-in diagnostic only.

Interaction design: display what is happening, who is involved, what can be done and the actual outcome. A player should reach a relevant action with no unnecessary report browsing. Hide actions that cannot execute, give concise reasons on request and keep an unconditional native close/return. Prioritize useful choices over additional dialogue volume. No technical receipt text, artificial chores or unearned rank/relationship claims in normal player interface.

## Household command contract

A command is not a menu click, a report or a promise. It names the acting Hero, actual target, owner, native/provider authority, eligible state, explicit player instruction, resource budget (zero for current appointments), current status and observable completion. Use native ownership as the sole source of actual assignment. Do not introduce a second office roster, salary system or simulated party controller.

Current local commands: governor via ChangeGovernorAction.Apply(Town,Hero), and Quartermaster, Scout, Surgeon and Engineer via MobileParty.SetHeroPartyRole. The actual raw slot must be vacant, including absent incumbents. Check actor adult, alive/active, reciprocal spouse or adult child or actual companion, same clan, local presence, correct party roster, relation >= 0, authority, no siege/battle/army/sea or incompatible SaS service. Use CanAssignMoreRolesToHero and no existing specialist assignments. Confirm raw holder and effective holder plus all other land/naval slots unchanged. For governor verify Town.Governor and Hero.GovernorOf. Never claim genuine NPC consent merely from successful assignment; the dialogue acknowledgement is CU-authored.

Proposed reusable command state for genuinely asynchronous provider tasks: Offered -> Rejected or Accepted -> In progress -> Completed, Failed or Cancelled. Persist exact player, actor, provider, target, agreed budget, deadline, unique command ID and verified receipt. Never charge, spawn, move or mark complete twice. Fail closed on provider absent, role change, dead/captive actor, lost settlement, unavailable target, unconfirmed post-state or save mismatch. No guessed private API calls. Direct local assignments do not need an invented asynchronous task ledger.

Example: a Master of Arms notes a weak garrison. CU must show the real concern and the native/Improved Garrisons owner. If an approved provider API is available, the future command specifies actual holding, target roster, budget, ETA and evidence. Otherwise use the existing provider/native screen and state plainly that CU cannot issue that order. CU must never falsely say recruits were obtained or silently run its own recruiter. Direct manual five-recruit transfer trial remains disabled when Improved Garrisons is loaded; normal launches keep legacy officer transfers off.

## Relationship and event contract

Use actual Hero IDs and native relationship, kinship, age, residence, office and personal history. Native events (birth, adulthood, death, marriage, verified appointment, battle loss, completed service, actual gift or petition response) trigger relevant optional encounters. Deduplicate by event identity and participant, store bounded memory, prefer local/contact-specific eligibility and use cooldowns. Distinguish observed fact, CU-authored dialogue and modeled inference. No invented world event, autonomous romance, secret conspiracy or global pairwise simulation. An interaction earns its place through a real choice, verified native effect, useful information or a distinctly personal memory. Ordinary conversation is not a relationship reward loop. Do not claim full independent NPC agency until native execution is proven.

## Compatibility and ownership

The curated 41 selected modules are the full-stack integration profile, not 41 hard dependencies. Minimal control profile: Native, SandBoxCore, Sandbox, four required frameworks (Harmony 2.4.2.248, ButterLib 2.12.0.0, UIExtenderEx 3.0.3.0, MCM 5.13.1.0) and CU. Add official BirthAndDeath/NavalDLC for their specific tests and ServeAsSoldier 1.5.7.4 for Soldier. Current stack includes ImprovedGarrisons 4.3.1.0, Diplomacy 1.5.7.0, Fourberie 1.5.7.0, RBM/RBM_WS 4.8.2.0, RTS Camera, equipment and visual mods. Use the recorded exact launcher order and validate installed versions/hashes at each test. Current module boundary record: [Data/CU_Module_Ownership_Audit.csv](../Data/CU_Module_Ownership_Audit.csv), [Data/Current_Modlist_Baseline.json](../Data/Current_Modlist_Baseline.json), and [docs/CURRENT_MODLIST_COMPATIBILITY_BASELINE.md](CURRENT_MODLIST_COMPATIBILITY_BASELINE.md). Prior launcher selection proves configuration only.

Native and BirthAndDeath own aging, education and life-cycle. Diplomacy/native own politics, vassalage, war and settlement transfer. Improved Garrisons owns recruitment, garrison training, guards and patrols. SaS owns enlisted status, grade, duty, wage and release. Fourberie owns its crime and intrigue mechanics. RBM, RTS Camera, troop/equipment mods, Nobles Buy Stuff and War Sails retain their respective combat, troop, gear, command and naval systems. CustomisableUI and existing native screen layout retain their scope. No copying external DLLs, inventing API contracts or changing other mods without explicit authorization. EBA/OSA/ZZZ culture gear patch remains separate.

The 78-mod inventory is research and migration provenance, not a replacement target. The current reviewed scope is Data/CU_Core_System_Review.csv: 143 review rows (70 canonical + 73 supplemental). 67 KEEP, 43 RETIRE, 12 MERGE, 10 DEFER, 8 IMPROVE, 2 ARCHIVE, 1 OPTIONAL. Rows are subfeatures and engineering contracts, not 143 playable independent systems. Preserve the original registry and historical evidence statuses. Retired means inactive or inaccessible in normal gameplay, not necessarily physically deleted. Remove dead code only after save/serialized-key and cross-reference audit. No resurrection of depots, levies, convoys, custom economy, automated patrols, fake offices/wages, duplicate education or speculative peer/story observers.

## Dependency-driven milestones

No promised calendar dates. Close evidence gates in order; an approved design is not implemented merely by being listed. New expansion follows native acceptance of the relevant baseline, except narrow fixes to an identified blocker or user-authorized scoped refinement. Record each milestone's actual source SHA, tests, package and next blocker.

### R0 | Freeze and establish exact runtime identity (P0)

Depends on: none. Acceptance: Gate A/B, candidate ZIP and DLL verified; full-stack actual install baseline recorded; prior source stale checks resolved, no old DLL assumed. Current state: COMPILED_UNTESTED; package verified; live preflight pending.

### R1 | Native acceptance of existing systems (P0)

Depends on: R0. Acceptance: All seven starts, native dialogue exits, one actual action per applicable system, full 41-module profile; save/reload; record all failures. Current state: BLOCKED_PENDING_GAME.

### R2 | Usability and compatibility repair (P0)

Depends on: R1 reproduced findings. Acceptance: One household dossier, correspondence native fallback, chronicle, one direct conversation hub; no crash or duplicate mutations; verify fixes using real evidence. Current state: SOURCE_CONSOLIDATED; live acceptance pending.

### R3 | Living family and relationship depth (P1)

Depends on: R1/R2 baseline accepted. Acceptance: Actual native family events feed bounded personal interactions, explicit choice and verified outcome, no fake education/kinship/relationships; no generic dialogue grinding. Current state: PLANNED; gated.

### R4 | Practical household commands (P1)

Depends on: R1/R2 baseline accepted. Acceptance: Local native governor and party assignments pass; single command lifecycle and reports. Authority/availability checks, target, cost, result and cancellation. External job support only with verified contract. Current state: LOCAL_SOURCE_EXISTS; remote deferred.

### R5 | Sworn service and career continuity (P1)

Depends on: R1/R2 and native SaS checks. Acceptance: Knight oath, actual duty completion, renewal and native transition evidence; no overlapping progression or payroll; founder vs successor verified. Current state: EXISTING_SOURCE; native tests pending.

### R6 | Unified personal chronicle and dynasty continuity (P2)

Depends on: R3/R4/R5 evidence. Acceptance: One accessible read-only history, actor/date/source receipts, real kinship and succession, no fabricated causality; old saves preserved. Current state: SOURCE_CONSOLIDATED; integration pending.

### R7 | Compatibility, cleanup and performance (P0 release gate)

Depends on: R1–R6 for approved scope. Acceptance: Remove unreachable code/assets only after dependency and old-save migration tests. 30 campaign days, 60-minute live session, sea/siege/battle/settlement transitions, save size and CPU/frame measurements, no unresolved blocking defects. Current state: BLOCKED_PENDING_GAME.

### R8 | Candidate and stable distribution (Release)

Depends on: R7 Gate F. Acceptance: Exact tested ZIP, version, hashes, changelog, tested profile, known issues, clean install/removal and permissions; publish only after Gate G approval. Current state: NOT_RELEASED.

## Scope control and acceptance

Use [CU_Six_System_Native_Test_Matrix.csv](../Data/CU_Six_System_Native_Test_Matrix.csv) and [CU_V074_GAMEPLAY_ACCEPTANCE_CHECKLIST.md](CU_V074_GAMEPLAY_ACCEPTANCE_CHECKLIST.md). Gate A source checks, B compiled and policy tested, C actual startup, D actual feature state and negative cases, E save/reload and regression, F exact candidate full-stack and 60-minute/30-day session, G released with exact tested artifact. Every test starts PENDING. A blocked provider action remains BLOCKED, not a pass. No claims that the full-stack has passed. The older seven-start parser still requires retired graphical correspondence and held garrison transfer receipts; retain historical evidence and mark that parser's old contract superseded for current gameplay, never fabricate or bypass receipts. Implement a distinct current acceptance verifier before release.

## Risk register

- P0 native creator/mission screen lifecycle: preserve native UI; test map, settlement and sea transitions. Known graphical correspondence crash history, native fallback is default.
- P0 native mutations and save continuity: exact identity, authority, idempotency, partial failure and persistence; do not replay grants or alter incumbent roles.
- P0 external overlap: exact module load order, binary versions and public provider contracts; no new owner until confirmed.
- P1 UI clutter and nonconsequential dialogue: measure actual player path and post-state; remove duplicate records and meaningless prompts.
- P1 performance: disconnected speculative peer/story ticks stay off. Measure native campaign speed, frame pacing, memory and save size with comparable CU-on/off runs. Model simulations do not prove runtime FPS.
- P2 source pruning: archive old implementation after finding all references, verifying old menu/save compatibility and testing migrations. Never remove serialized keys casually.

## Source-of-truth links

[Canonical handbook](https://docs.google.com/document/d/1A6AZpYpCyY6PoI1J6hn1rC2NQqnvPt9dsvZIZ0vwJpk/edit), [PROJECT_INSTRUCTIONS.md](../PROJECT_INSTRUCTIONS.md), [CHATGPT_PROJECT_INSTRUCTIONS.txt](../CHATGPT_PROJECT_INSTRUCTIONS.txt), [AGENTS.md](../AGENTS.md), [six-system review](CU_SIX_SYSTEM_CONSOLIDATION.md), [feature review](../Data/CU_Core_System_Review.csv), [historical feature registry](../Data/Feature_Registry.csv), [ownership freeze](CU_ACCEPTANCE_FREEZE_OWNERSHIP_AUDIT.md), [PowerShell standard](POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Active runtime draft PR #100 (feature/cu-v074-living-dialogue-depth), base PR #99 (integration/cu-living-society-acceptance), not main. Verify current heads and CI each session. Never independently merge stacked provenance PRs.