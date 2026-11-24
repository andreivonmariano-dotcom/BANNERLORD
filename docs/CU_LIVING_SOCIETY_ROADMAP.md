# Full-stack compatibility review and society roadmap

**24 November 2026 | Source snapshot 93c4883460fb66a8f7e7dc42f61f2c820e8019c8 | Gameplay acceptance pending**

## Q1: Existing-feature screening

55/55 CU registry entries and 41/41 installed module roles are classified in [CU_Feature_Compatibility_Audit.csv](../Data/CU_Feature_Compatibility_Audit.csv) and [CU_Module_Ownership_Audit.csv](../Data/CU_Module_Ownership_Audit.csv). These are source/registry and published-feature screening results, not 2,255 proven pairwise compatibility checks. Exact installed third-party binaries, effective configuration overrides, patch-owner inventory and live execution have not been examined. The 78 historical registry rows are not current installed modules. Registry counts: {"COMPILED":44,"BLOCKED":4,"GAME_TESTED":3,"SCOPED":1,"IMPLEMENTED_SOURCE":3}. Screening decisions: {"RETAIN_AUDIT":6,"OPT_IN_LATER":7,"HOLD_ADAPTER_REVIEW":6,"HOLD_EXTERNAL_OWNER":6,"RECONCILE_LEGACY_UI":4,"RETAIN_PRIORITY_GATE":21,"KEEP_BLOCKED":4,"DEFER_REDESIGN":1}. Existing registry status is retained, not promoted. Three GAME_TESTED rows refer to v0.26 historical observations. Some UI rows describe abandoned or superseded code. The registry lacks newer source families; reconcile them before claiming comprehensive runtime coverage.

### Source-confirmed findings

- [SubModule.xml](../Modules/CalradiaUnified/SubModule.xml) and [SubModule.cs](../Source/CalradiaUnified/SubModule.cs) require Harmony, ButterLib, UIExtenderEx and MCM alongside Native, SandBoxCore and Sandbox. This corrects the previous official-only dependency policy for the current branch. Preserve the one-module runtime distribution; do not bundle third-party binaries or add the other mods as hard dependencies. Control profile = official modules + four current required frameworks + CU. Full profile = pinned 41 modules.
- Settings.cs defaults diagnostics, supply/army warnings and NPC advisory to on, and 20 experimental mutation switches to off (verify actual overrides separately). CivicBehavior.Garrisons.cs adds troops to garrison MemberRoster; KingdomAutonomyBehavior.cs issues movement orders; CivicBehavior.Kingdom.cs changes roster, gold, influence and relations. These are real potential overlap surfaces; keep full-stack parallel mutations off pending audit.
- SandboxSaSBridge.cs reads ServeAsSoldier.Compat.SaSPublicApi; it does not enlist, pay, discharge or move the host party. Validate the installed API shape/version and native snapshot, then use observations for chronicles. Unknown API versions fail closed rather than introduce a second military career engine.
- SandboxMainMenuEntry.cs replaces SandBoxNewGame's action; CUBUTRNativeScreens.cs contains ClanManagementVM, PartyVM and KingdomManagementVM mixins and Clan/Party/Kingdom prefab extensions. CUBUTRMCMSettings.cs exposes CU settings. Current SubModule does not register the older NativeSettingsBehavior. Inspect exact live layout/input with CustomisableUI and Diplomacy; keep native fallback.
- SandboxOriginBehavior.cs registers daily career, family, wage, knight service, inheritance and succession observers and phase-8/9 start events. Some methods mutate state: SandboxLivingPersons.cs can award sibling comradeship relations; LivingMenus.cs contains verified gold/relation transfers and a guarded ownership action; HouseholdDuties.cs records gifts/audiences; LordInheritance.cs ranks heirs without assigning fiefs or clan leadership. Not all these systems are read-only. Preserve native authority and keep inferred consent distinct from native authorization. Inspect later source families in a separate registry reconciliation before expanding them.

### Additional current source clusters outside the 55-row registry

- Royal council and succession: SandboxKingNativeMinistry, SandboxOriginBehavior.KingCouncil, KingSuccession and KingFamily; observe actual native appointments/elections. Potential Diplomacy overlap.
- Lord inheritance and noble birth: SandboxDynasticStart, SandboxDynasticSonStart, SandboxLordInheritanceState, SandboxInheritanceNegotiationState and LordInheritance menus; separate claims from actual property/clan control. Native/Diplomacy review.
- Knight oath and service: SandboxKnightOathState, SandboxKnightServiceOrderState, SandboxKnightEscortState and corresponding menus; no competing SaS enlistment, party command or fictional titles.
- Household and personal continuity: SandboxFamilyDevelopment, SandboxHouseholdDutyState, SandboxCharacterAgendaState, SandboxAgencyDecisionState, SandboxLifeHistoryState, SandboxLivingPersons, SandboxLivingAffairsSimulation; inspect event ownership, relation and skill mutation, age/death and save lifecycle.
- Career and petitioner records: SandboxCareerChronicleState, SandboxServiceCareerState, SandboxPetitionStoryState and menus; inspect native facts, cooldown, duplicate transactions and dialogue collisions with Fourberie/SaS/Diplomacy.
- UI and native creator: CUNative* layers, CUNobleDossierScreen, CUHouseholdCampScreen, BUTR mixins and MCM; source inventory only, not a claim every class is reachable or working.

### Third-party published scope evidence (not exact installed source verification)

- Improved Garrisons: https://www.nexusmods.com/mountandblade2bannerlord/mods/688 — recruitment, training, guard parties.
- Diplomacy: https://www.nexusmods.com/mountandblade2bannerlord/mods/832 — kingdom diplomacy/factions/civil war; version-specific mechanics require inspection.
- Serve as Soldier: https://www.nexusmods.com/mountandblade2bannerlord/mods/3242 — enlistment, wages, ranks and service; API presence must be checked in installed v1.5.7.4.
- Fourberie: https://www.nexusmods.com/mountandblade2bannerlord/mods/2969 — criminal play, traits, blood feud and clan offices; don't duplicate crime/spymaster systems.
- NoblesBuyStuff: https://www.nexusmods.com/mountandblade2bannerlord/mods/12366 — equipment shopping; leave purchasing/stock control with existing owner.
- Customisable UI: https://www.nexusmods.com/mountandblade2bannerlord/mods/13944 — interface styling; test native UI coexistence. Other module roles are conservative boundaries based on the baseline, not audited exact code.

## Q2: Living medieval society roadmap (design only)

P0 — Existing acceptance and compatibility audit. Keep PR #93 draft. Verify the installed CU DLL against the pinned v0.69g run, effective flags, full list, UI warnings and logs. Reconcile current source coverage and existing feature registry. Run minimal control and curated full-stack cases. Diagnose reproducible P0/P1 issues before expansion. Existing 12-role, 3-resolution, 30-day, 60-minute and save/quit/reload protocol remains binding.

P1 — Starting Life integrity. All 12 existing roles must produce verified native family, birth order, affiliation, possessions and actual residence. No eligible older native lord => remain in House with choices intact, or explicit independent start. Test ages, spouse/children/siblings, failed retinue/grants, native Review Back, and save reload. Do not introduce new creator screens before acceptance.

P2 — Social stations and household contract. Build on existing origins, knight oath, lord/king records and service observations. Define social station, occupation, actual title, land and residence as different properties. Landed and landless roles have different obligations and lawful transitions. Existing SaS owns enlistment and payroll; native kingdom owns actual rank/ownership. Verify a service transition and a refusal with no duplicate reward.

P3 — Independent personal agency. Extend existing goals, relationship/agenda, requests and memory records using bounded event-driven decisions. Start with co-located adults and read-only decisions or requests; native action only after eligibility and consent. NPCs can decline, postpone or pursue their own objectives. Keep movement, faction diplomacy, equipment buying and garrison logic under existing owners. Test absent/dead/captured Hero, cooldown, reloaded requests and simultaneous interactions.

P4 — Household and dynasty. Build family duties, documented gifts and support, residence, child upbringing, credible succession claims and family chronicle. Native BirthAndDeath owns lifecycle; actual leader/fief ownership remains native or authorized integration. No automatic land or leadership award based on modeled claim. Test death, coming of age, marriage, estrangement, household relocation and no duplication.

P5 — Feudal obligations and career chronicles. Link witnessed service, sponsorship, personal contracts and household obligations; record provenance and consequences in a persistent chronicle. Read observed wages, troop outcomes, estates and native diplomacy rather than adding payroll or simulated taxes that double count. Test transitions across allegiance and generations; bounded histories, opt-in state migration, 30-day and longer soak tests.

## Q3: Candidate distinctive gameplay gaps

No claim of absolute novelty across the mod ecosystem or absence from every installed binary. The baseline and published role descriptions do not show a dedicated equivalent for the following combinations; inspect exact mods first. Prefer extensions of current CU work.

1. **Personal station contract** — distinguish a birth-house cadet, landed vassal, household knight and independent adventurer using real native rights and obligations. A household knight may ask a patron for a witnessed commission; not a second SaS job or automatic fief.
2. **Verified family claim register** — culturally grounded heir ordering, claims and reasons, plus family council requests; native elections/ownership remain authoritative. Separate expected, consented and observed outcomes. Avoid Diplomacy's political/civil-war mechanics.
3. **Household continuity and personal obligations** — residence, adult audiences, gifts/support, dependent children and household history. Native Hero lifecycle owns births/deaths; no autonomous pregnancy or new wage engine. Fourberie office systems stay separate.
4. **NPC agenda and evidence-backed memory** — bounded ambitions, loyalty, obligations, witnessed events and accepted/refused requests tied to actual Hero IDs. No general party AI replacement, spymaster or faction system. Do not present inferred goals as NPC self-report.
5. **Career-to-dynasty chronicle** — service milestones from SaS snapshots, actual native rank/land/marriage/death events, and personal achievements; show native vs inferred provenance and carry history across succession. No duplicate wages or arena rewards.
6. **Personal patronage and petitions** — an eligible person can request a meeting, sponsorship, mediation or household assistance; refusal, cooldown, costs and native confirmations. No parallel diplomatic treaties or Fourberie crime chain. Start as a narrow optional adapter-free request flow.

## Release rules

For every feature, record exact native/API target, owner, installed version, mutation type, shared state, opt-in behavior, failure mode and save migration. Classify independent/read-only/adapter/hold. Require a real pinned build, exact hash, control and full-stack tests, actual native changes and save/reload. Never elevate historical status or claim absolute conflict-free operation. No gameplay code, config defaults, launcher or assets changed by this audit.
