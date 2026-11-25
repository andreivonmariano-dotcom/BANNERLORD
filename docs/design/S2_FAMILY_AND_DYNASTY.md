# S2 | Family and Dynasty — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
A real household persists through marriage, children, adulthood, bereavement and succession. The people are native Heroes, not invented biographies detached from the world.

## Current contract
Starting family customization, reciprocal native spouse and parentage, actual siblings, cultural names and donor/appearance checks, initial traits and skills, safe actual residence and portrait gallery. Adult household members can be considered for eligible native appointments through S3. Read native kinship and actual inheritance/succession views. Historical claims remain readable/withdrawable; no automatic estate grant. Starting family generation is distinct from lifecycle: native/BirthAndDeath own birth, aging, education and death after creation. No automatic CU education stat gains.

## Loop and interface
From My Household select actual spouse/child/relative, see relationship, age, native residence, current assignment, and witnessed family history. Open the same shared dossier from Clan or Party; no duplicate family menu. Speak in person to adult family and act through S3 or S4. Minors remain read-only household members, not agents in parties. Actual events should surface only relevant messages, not daily chores. Native succession is controlling; CU shows prior house and heirs as records.

## Proposed event-led depth (not implemented as a complete feature)
Birth: observe actual new child's ID and reciprocal parents; optional visit or acknowledgement, no second child creation or invented bonus. Adulthood: observe actual age/activation; present existing native eligible duties only. Marriage: verify reciprocal native links; offer personal congratulations or actual family context, no auto-marriage. Separation/reunion: distinguish last known settlement and actual presence; only local reunion is witnessed. Death: observe actual death, acknowledge specific family relation, archive affected assignments and stop requests from unavailable Hero. Heir: actual native successor becomes player, never inherit predecessor's personal agreements. These event prompts are optional and require explicit event identity, deduplication, actor eligibility and tested save persistence; no physical feasts/funerals or autonomous romance in current scope.

## State and rules
Use native Hero.StringId and reciprocal family fields, native current/staying settlement and alive/active state. Native family home operations must preserve minor nonactivation and ownership eligibility. Record facts only after readback. Legacy CU_v052_family_growth remains parseable but no daily reward. CU_v065_succession_interest, CU_v070k_lord_native_heir_claims, CU_v070l_native_royal_household, CU_v067_family_negotiation, CU_v064_living_persons remain. Never reinterpret old claims as native ownership. Use bounded on-demand genealogy and read-only history. New event deduplication design requires versioned bounded keys and migration tests before implementation.

## Source and dependencies
SandboxFamilyCreator.cs, SandboxFamilyHome.cs, SandboxDynasticStart.cs, SandboxLivingPersons.cs, SandboxLifeHistoryState.cs, SandboxOriginBehavior.LordInheritance.cs, SandboxOriginBehavior.KingSuccession.cs, CUFamilyPortraitGallery.cs, SandboxOriginBehavior.NativeHouseholdUI.cs. Depends on S1 native integrity; S3/S4 consume eligible identities, S6 observes facts. Native and BirthAndDeath own lifecycle.

## Acceptance
R1/R2 verify existing family and read-only views; R3 only after acceptance adds one event-led interaction at a time. S2-1 through S2-5, applicable E1 and F04. Check real parentage, minor safety, actual marriage/death/adulthood when triggered, gift and visit differences, residence and copied legacy saves. No free property, education rewards or false children.

## Feature-level scope (20 review entries)

- CU-M2-004 | **KEEP** | Opt-in real Sandbox family hero registration. Real family at start, native lifecycle after creation. Source: [SandboxFamilyCreator.cs](../../Source/CalradiaUnified/SandboxFamilyCreator.cs).
- CU-M6-012 | **KEEP** | Native spouse donor fallback and offspring source fix. Guard family creation and compensation. Source: [SandboxFamilyCreator.cs](../../Source/CalradiaUnified/SandboxFamilyCreator.cs).
- CU-M6-014 | **KEEP** | Generated spouse with commoner random noble or chosen noble ancestry. Spouse origin and native marriage. Source: [SandboxGeneratedSpouseOrigin.cs](../../Source/CalradiaUnified/SandboxGeneratedSpouseOrigin.cs).
- CU-M6-015 | **KEEP** | Child facial inheritance from actual native mother and father. Actual parental appearance at creation. Source: [SandboxFamilyCreator.cs](../../Source/CalradiaUnified/SandboxFamilyCreator.cs).
- CU-M6-017 | **KEEP** | Activate generated spouse and compensate failed marriage before children. Preserve reciprocal marriage and rollback. Source: [SandboxFamilyCreator.cs](../../Source/CalradiaUnified/SandboxFamilyCreator.cs).
- CU-M6-020 | **KEEP** | Automatically reside generated spouse and offspring in safe owned home fief. Initial safe household placement only. Source: [SandboxFamilyHome.cs](../../Source/CalradiaUnified/SandboxFamilyHome.cs).
- CU-M6-021 | **KEEP** | Explicit settlement-menu home action for existing campaign families. Explicit home action; verify save/reload. Source: [SandboxOriginBehavior.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.cs).
- CU-M6-022 | **KEEP** | Record minors in saveable settlement without activating or recruiting babies. Never activate or recruit minors. Source: [SandboxFamilyHomePolicy.cs](../../Source/CalradiaUnified/SandboxFamilyHomePolicy.cs).
- CU-M6-024 | **MERGE** | Read-only genuine spouse and offspring portrait gallery in native settlements. One household dossier and family gallery, not separate primary settlement paths. Source: [CUFamilyPortraitGallery.cs](../../Source/CalradiaUnified/CUFamilyPortraitGallery.cs).
- CU-REVIEW-012 | **KEEP** | Generated siblings and genealogy. Real reciprocal kin; retired subtype remains unavailable. Source: [SandboxDynasticStart.cs](../../Source/CalradiaUnified/SandboxDynasticStart.cs).
- CU-REVIEW-013 | **KEEP** | Initial family skills, personality and relationships. At creation only. No ongoing extra progression. Source: [SandboxFamilyDevelopment.cs](../../Source/CalradiaUnified/SandboxFamilyDevelopment.cs).
- CU-REVIEW-014 | **RETIRE** | Child education milestones after creation. Daily milestone subscription removed; preserve data. Source: [SandboxOriginBehavior.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.cs).
- CU-REVIEW-015 | **RETIRE** | Co-parent upbringing conversation. No active extra education choice. Source: [SandboxOriginBehavior.LivingDialogueDepth.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingDialogueDepth.cs).
- CU-REVIEW-016 | **KEEP** | Native home residence and minor safety. Explicit safe residence; no activation of minors. Source: [SandboxFamilyHome.cs](../../Source/CalradiaUnified/SandboxFamilyHome.cs).
- CU-REVIEW-017 | **KEEP** | Cultural naming and starting customs. Use actual culture IDs; no legal privilege inferred. Source: [SandboxCulturalNames.cs](../../Source/CalradiaUnified/SandboxCulturalNames.cs).
- CU-REVIEW-018 | **MERGE** | Cultural inheritance descriptions. History and culture reference, not an alternate succession law. Source: [SandboxCulturalInheritancePolicy.cs](../../Source/CalradiaUnified/SandboxCulturalInheritancePolicy.cs).
- CU-REVIEW-019 | **RETIRE** | Modeled family-estate mediation and grant. Mutation gates fail closed and no new offers. Source: [SandboxOriginBehavior.LivingMenus.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingMenus.cs).
- CU-REVIEW-020 | **ARCHIVE** | Historical family claim. Read or withdraw old claim; not an obligation. Source: [SandboxInheritanceNegotiationState.cs](../../Source/CalradiaUnified/SandboxInheritanceNegotiationState.cs).
- CU-REVIEW-021 | **KEEP** | Lord family succession and branch review. Observe actual kin and property; no automatic fief transfer. Source: [SandboxOriginBehavior.LordInheritance.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LordInheritance.cs).
- CU-REVIEW-022 | **KEEP** | King family succession and branch review. Observe native succession; no artificial crown. Source: [SandboxOriginBehavior.KingSuccession.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.KingSuccession.cs).

## Current test cases (5, all native PENDING)

- S2-1 (D/E): Inspect spouse, children and siblings in native Clan and dossier. Expected: Actual reciprocal kin, ages and identity; no invented kin. Evidence: Native family tree/state, logs, balances and screenshots.
- S2-2 (D/E): Inspect family residence, minor/adult transition. Expected: Actual residence; no recruitment or activation of minors. Evidence: Native family tree/state, logs, balances and screenshots.
- S2-3 (D/E): Observe native marriage, birth, adulthood or death where triggered. Expected: One witnessed event; no extra lifecycle or education mutation. Evidence: Native family tree/state, logs, balances and screenshots.
- S2-4 (D/E): Review Lord/King succession and descendant/collateral branches. Expected: No automatic grant or crown, actual native leadership/property authoritative. Evidence: Native family tree/state, logs, balances and screenshots.
- S2-5 (D/E): Repeat same gift and family visit after reload. Expected: 100 denars actual debit/credit for gift, cooldown; visit only memory, no bonus. Evidence: Native family tree/state, logs, balances and screenshots.
