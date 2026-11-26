# Bannerlord Research Lab

## Mandatory knowledge retrieval
Start with [INDEX.md](INDEX.md), [Handoff/CURRENT.md](Handoff/CURRENT.md), [Failures/REGISTER.csv](Failures/REGISTER.csv) and [learning log](Handoff/LEARNING_LOG.md). See [knowledge protocol](KNOWLEDGE_PROTOCOL.md). Domain records distinguish verified evidence from research gaps. Update records by stable ID after every milestone.

25 November 2026 | isolated research branch, not a CU release.

## Purpose and rules
Frozen production: PR #100, head a116a97b01e33d04b7dc3b392b5bcec36f32149a; pinned game artifact run 45637611622, artifact 15035145061, DLL e6bcbde295ecf0527ec97b0c4805d11916a28bd92ee099f18d838da68e6d0bba. Preserve that module and the user's saves. This folder is outside Source/CalradiaUnified and Modules/CalradiaUnified. Never ship lab binaries with CU.

Read NATIVE_ENGINE_1_5_7.md and MOD_IMPLEMENTATION_STUDIES.md before proposing mechanics. The authoritative evidence is the installed game version, exact assembly fingerprint, supported operation, pre/postconditions and native playtest. A method signature alone does not prove semantic behavior. Do not publish TaleWorlds binaries, full decompiled bodies, third-party code, credentials, private logs or saves. Study permitted sources; do not copy code or assets without license review.

## First experiment L001
A separate, opt-in module CUResearchLab performs one explicitly confirmed 100-denar transfer from the player's personal purse to a locally occupied, player-owned town/castle treasury, using the verified GiveGoldAction API. It records both balances before/after, verifies conservation, and refuses a second attempted transfer within that behavior lifetime. It observes governor changes and bounded owned-holding snapshots at session launch/load. This is a laboratory transaction, not a proposed CU feature or a claim that funding improves security/food/recruitment. It establishes the engine behavior and subsequent economy research question. No Harmony patches, no automatic mutations, no save keys. Native game behavior remains authoritative.

## Local build
PowerShell 5.1: run Build-Lab.ps1 -GameRoot '<Bannerlord root>' in this folder. It builds against actual installed assemblies with the existing Windows Framework C# compiler, executes pure transfer policy checks, and stages under this folder's build directory, not the game's Modules directory. It refuses an unexpected CampaignSystem fingerprint. Uses the installed game's Mono 4.7.2 API netstandard facade for the native references. No SDK installation required for this narrowly scoped prototype; full CU still uses CI and versioned references. If build fails, record exact errors. An SDK and ILSpy remain optional future tooling, not installed silently.

## Native test, separate authorization required
Use a disposable new Sandbox save and official control profile only, with the distinct CUResearchLab module added manually after backup. Do not activate CU or other economy providers during the control experiment. No production save. Establish a real owned town/castle, enter it, record purse and holding treasury, select Lab: transfer 100 denars to holding treasury, cancel once (no mutation), confirm once, compare native balances and log; attempt another transfer in the same session (not offered). Leave the holding and verify no action. Try foreign holding and insufficient balance (not offered). Appoint/remove a governor using the ordinary game UI; compare callback and both native links. Save a disposable copy, quit, relaunch, load, inspect balances and ownership against pre-save state. Never repeat a transfer merely to verify a prior receipt. Subsequent economy impact is a separate experiment, not assumed. Log location: LocalAppData/BannerlordResearchLab/native-probe.log. Keep this log private.

## Acceptance and review
No feature promotion on compilation alone. For each experiment, state player problem, distinct choices, actual native effects, limits/ownership, behavior after repetition, negative cases, save/reload, control and full-profile compatibility, visual clarity, and human playtest outcome. Record PASS, FAIL, BLOCKED, NOT_RUN separately for each. Use five review passes: design, native semantics, code/persistence, compatibility, player experience. A failure freezes integration until repaired. Read-only observations and tests are not gameplay features. No merge or release from this branch without explicit approval and all applicable gates.

## Current evidence
Native metadata and selected method call edges inspected locally; external source case studies recorded. L001 native execution, save/reload and meaningful gameplay acceptance NOT_RUN. Build evidence is recorded separately after actual execution.
