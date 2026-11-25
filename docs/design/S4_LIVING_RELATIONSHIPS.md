# S4 | Living Relationships — game design specification

25 November 2026 | v2.0 design contract | source baseline 9e5aaf18e17afa767c42e3b7245db6f7620fd9df | Native acceptance pending. This document defines retained mechanics, gated next work and explicit non-goals. It does not declare any current native test passed. Read [master roadmap](../CU_MASTER_ROADMAP.md), [feature review](../../Data/CU_Core_System_Review.csv), [development tracker](../CU_DEVELOPMENT_TRACKER.md) and [native matrix](../../Data/CU_Six_System_Native_Test_Matrix.csv).

## Player promise
NPCs speak from their actual situation and prior dealings. Advice leads to an available choice or a clear limit, not a meaningless promise. Different people may respond differently without inventing a new simulation of all Calradia.

## Current contract
Private Concerns, Family and Standing. Native traits, relationship and context shape authored voice. Bounded exact-pair memory and local conversational initiative. Family visit is a recorded visit, not free relation farming. Actual personal gift uses verified native gold transfer. Financial and recognition requests can produce native gold and relation effects with readback. Audience acceptance is a hearing only. Deferral does not spend gold. Ordinary letters and pending requests appear in the shared correspondence inbox; read news archives, no remote payment. Speculative peer sampling, peer initiatives, story generation and new multi-person gatherings disconnected. Existing historical records remain readable where applicable.

## Interaction loop
Speak to actual eligible adult → surface real concern or topic → present a relevant choice (ask, decline, defer, gift, eligible S3 responsibility, native provider action) → revalidate target/authority/cost → apply only explicit native action → verified outcome or no confirmed effect → remember exact pair and event. Native scene/map exits always available. Do not mix a read-only opinion with a command. Never infer actual friendship/consent from a relationship threshold.

## Proposed depth
R3 first implement a small event-backed encounter for actual family milestone or actual completed duty. Each case has trigger identity, person, topic, response choices, actual available effect, refusal/defer, cooldown, history and negative tests. Native relationship repair is a separate deliberate action with meaningful cost/consent and before/after relation; no repeat-click reward, no passive talking bonus. New events only when they meet real local relevance; no independent peer-to-peer romances, rivalries or factions. Distinguish proposed intention, player-approved action and witnessed completion. Missing provider/target means explicitly unavailable, not simulated success.

## State and persistence
CU_v064_living_persons exact participant IDs, bounded events (current MaxEvents=96), CU_dialogue_variety_1, CU_v066_character_agendas, CU_v067_character_requests, CU_petition_recipient_1, CU_v071e_personal_petition_stories, CU_v071g_social_bonds, CU_news_read_1. Keep prior peer/story/gathering keys readable, no active scans. Consume selected petition once before irreversible transfer and confirm debit/credit/relation. Do not allow successor to reuse previous player's petition or memory. Distinguish authored dialogue, modeled news and observed facts.

## Interface and source
One personal face-to-face entry and three topics; one native responsibilities submenu from relevant concerns; map/local Correspondence defaults to native InquiryData with pending/unread, mark-read, close and archive. No forced graphical movie or extra simulated quest feed. SandboxOriginBehavior.LivingDialogueDepth.cs, HouseholdDuties.cs, PetitionConversations.cs, PetitionStories.cs, LivingMenus.cs, Affairs.cs, SandboxLifeHistoryState.cs, SandboxAffairsIndexPolicy.cs.

## Acceptance
S4-1 through S4-7 and S6-5. Actual speaker, safe scene/map exit, valid actual gold/relations, insufficient funds/refusal/defer/repeat, exact pair after reload, unread/archive, no background speculative generation. R3 only after relevant R1/R2 evidence.

## Feature-level scope (27 review entries)

- CU-M7-001 | **IMPROVE** | Persistent personal dialogue and contextual initiative. Native personal actions and CU memories; three topics, verified petitions, no generic chores. Source: [SandboxOriginBehavior.LivingDialogueDepth.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingDialogueDepth.cs).
- CU-M7-002 | **RETIRE** | Multi-person campaign menu deliberation. No new group entry. Existing meeting exits retained. Source: [SandboxOriginBehavior.GroupDiscussions.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.GroupDiscussions.cs).
- CU-M7-003 | **RETIRE** | Local social gathering invitations and attendance. No new scheduling; existing plans may complete, cancel or expire. Source: [SandboxGatheringPlan.cs](../../Source/CalradiaUnified/SandboxGatheringPlan.cs).
- CU-M7-004 | **RETIRE** | Bounded native peer standing and priority observations. No ordinary peer sampling; retain historical state. Source: [SandboxPeerSocialState.cs](../../Source/CalradiaUnified/SandboxPeerSocialState.cs).
- CU-M7-005 | **KEEP** | Campaign-specific dialogue variation and bounded exposure. Bounded dialogue variation only for useful player-facing interaction. Source: [SandboxDialogueVarietyState.cs](../../Source/CalradiaUnified/SandboxDialogueVarietyState.cs).
- CU-M7-006 | **RETIRE** | Adaptive observed story threads and delayed followup. No new adaptive story threads; old receipt data preserved. Source: [SandboxStoryDirectorState.cs](../../Source/CalradiaUnified/SandboxStoryDirectorState.cs).
- CU-M7-007 | **RETIRE** | Saved optional social simulation detail. No normal social detail menu. Source: [SandboxOriginBehavior.StoryDirector.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.StoryDirector.cs).
- CU-M7-008 | **RETIRE** | Observed multi-character peer narrative threads. No speculative multi-character story generation. Source: [SandboxStoryDirectorState.cs](../../Source/CalradiaUnified/SandboxStoryDirectorState.cs).
- CU-M7-009 | **RETIRE** | Evidence-limited sampled peer reputations. No ongoing peer-reputation calculation. Source: [SandboxPeerSocialState.cs](../../Source/CalradiaUnified/SandboxPeerSocialState.cs).
- CU-M7-010 | **ARCHIVE** | Optional peer pass timing and automatic pause. Keep historical timing guard code, no live peer pass. Source: [SandboxOriginBehavior.PeerSocial.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.PeerSocial.cs).
- CU-M7-011 | **DEFER** | Physical native multi-agent group conversation scenes. Native multi-agent scene lifecycle not established. Source: [CU_V0741_SOCIAL_GATHERINGS_AND_PEERS.md](../../docs/CU_V0741_SOCIAL_GATHERINGS_AND_PEERS.md).
- CU-M7-012 | **DEFER** | Physical weddings feasts funerals and traveling guests. No real ceremonies implemented. Source: [CU_V0741_SOCIAL_GATHERINGS_AND_PEERS.md](../../docs/CU_V0741_SOCIAL_GATHERINGS_AND_PEERS.md).
- CU-M7-013 | **DEFER** | Autonomous NPC friendships romances rivalries and factions. No autonomous romance/faction actions. Source: [CU_V0741_LIVING_SOCIETY_DEPTH.md](../../docs/CU_V0741_LIVING_SOCIETY_DEPTH.md).
- CU-M7-014 | **DEFER** | Cross-generation personal and political story networks. No generational political network. Source: [CU_V0741_LIVING_SOCIETY_DEPTH.md](../../docs/CU_V0741_LIVING_SOCIETY_DEPTH.md).
- CU-REVIEW-037 | **IMPROVE** | Family visit and affectionate conversation. Memory only; do not falsely claim relationship increase. Source: [SandboxOriginBehavior.HouseholdDuties.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.HouseholdDuties.cs).
- CU-REVIEW-038 | **KEEP** | Personal gold gift. Verify actual native debit and credit; no repeat reward. Source: [SandboxOriginBehavior.HouseholdDuties.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.HouseholdDuties.cs).
- CU-REVIEW-039 | **IMPROVE** | Private concerns, family and standing topics. Restrict initiative to relevant topics; no speculative story generation. Source: [SandboxOriginBehavior.LivingDialogueDepth.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingDialogueDepth.cs).
- CU-REVIEW-040 | **IMPROVE** | Relationship intentions and advice. Record intentions honestly; no automatic relationship gain. Source: [SandboxOriginBehavior.LivingDialogueDepth.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingDialogueDepth.cs).
- CU-REVIEW-041 | **KEEP** | Character agendas and bounded actual native life observation. Use context for actual relevant petitions, not independent world simulation. Source: [SandboxLivingPersons.cs](../../Source/CalradiaUnified/SandboxLivingPersons.cs).
- CU-REVIEW-042 | **IMPROVE** | Petition generation and correspondence contacts. Model requests with exact contact and eligibility, not autonomous native action. Source: [SandboxOriginBehavior.LivingMenus.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingMenus.cs).
- CU-REVIEW-043 | **KEEP** | Financial and recognition petitions. Explicit face-to-face choice with verified native effects. Source: [SandboxOriginBehavior.PetitionConversations.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.PetitionConversations.cs).
- CU-REVIEW-044 | **IMPROVE** | Noble audience and negotiated terms. No fake resolution; provider-owned actions remain separate. Source: [SandboxOriginBehavior.PetitionConversations.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.PetitionConversations.cs).
- CU-REVIEW-045 | **KEEP** | Petition follow-up and outcome history. Exact speaker and recorded outcome. Source: [SandboxOriginBehavior.PetitionStories.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.PetitionStories.cs).
- CU-REVIEW-046 | **KEEP** | Social support/refusal history and cooldowns. No separate loyalty score. Source: [SandboxSocialBondState.cs](../../Source/CalradiaUnified/SandboxSocialBondState.cs).
- CU-REVIEW-047 | **IMPROVE** | Ordinary modeled personal letters. Unread letter inbox, no claim of native autonomous speech. Source: [SandboxOriginBehavior.LivingMenus.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.LivingMenus.cs).
- CU-REVIEW-048 | **RETIRE** | Modeled peer initiatives. No new peer initiatives; historical keys preserved. Source: [SandboxOriginBehavior.SocialRecords.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.SocialRecords.cs).
- CU-REVIEW-067 | **KEEP** | Correspondence inbox and archive. Unread and actionable matters separated from historical letters. Source: [SandboxOriginBehavior.Affairs.cs](../../Source/CalradiaUnified/SandboxOriginBehavior.Affairs.cs).

## Current test cases (7, all native PENDING)

- S4-1 (D/E): Open private concerns/family/standing; answer, back and close in map and scene. Expected: Exact actor context, no dialogue loop or native re-entry. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-2 (D/E): Hear and accept actual financial petition. Expected: Both gold balances confirm one transfer, native relation effect verified if requested. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-3 (D/E): Decline, defer, insufficient funds, stale actor and repeat click. Expected: Correct state, no unauthorized gold/relationship effect. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-4 (D/E): Recognition and audience with no payment. Expected: Recognition actual relation evidence when requested; audience explicitly listening only. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-5 (D/E): Reopen family/companion after real gift, request outcome or appointment. Expected: Exact-pair memory and live state, no fake consent or action. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-6 (D/E): Wait for distant personal news and actual request; read/archive. Expected: Only pending/unread inbox, read letters archive, no remote payment; old sender/recipient not inherited. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
- S4-7 (D/E): Inspect daily observations over 30 days. Expected: No new speculative peer initiatives, peer sampling or adaptive story generation. Evidence: Actual dialogue/screenshots, exact session receipts and native state.
