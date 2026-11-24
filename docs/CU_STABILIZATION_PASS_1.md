# CU stabilization pass 1 | 24 November 2026

Scope: source-confirmed integrity defects in existing v0.71g-era systems. No new gameplay features, changes to external mods, new save keys, or launcher changes. Active branch remains draft PR #93; no merge.

## Current evidence
- Current source before this patch: 39ab3b49d6209ddcaa4ce4c365595c3e04518add. Source carries v0.71g-era systems, while the module manifest is v0.38.0. Launcher versions cannot identify the installed DLL.
- Latest successful pre-patch Windows workflow: [45350357919](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45350357919) at e0c9a05e9281932824512e0dfbc0e7b506ad22b2. Policy execution, reference compilation, ZIP validation and installer tests are marked success in job metadata. [Compiled, game-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45350357919/artifacts/14900385680). No actual installed-game acceptance for this build recorded.
- 55 registered CU features have prior screening, but newer sources include v0.70–0.71g systems not fully represented by the registry. Existing core UI, 12 roles, households and actual native state still require live tests.
- Current manifest requires Native, SandBoxCore, Sandbox and four BUTR frameworks. Effective user INI is unknown. Exact third-party binaries and patch ownership are uninspected.

## Confirmed source defects and repairs
1. SocialBondState.Parse accepted a row with zero petitions but fabricated follow-up/delta history. Require at least one accepted or declined petition. All valid current RecordPetition/RecordFollowUp states remain accepted.
2. NativeMilitaryWitnessState.Observe allowed an earlier day to create an army command receipt before recorded vassalage, which its own reader rejects. Refuse earlier observations; reject saved kingdom with no vassal receipt.
3. CareerChronicleState.Parse accepted impossible chapter/evidence-day and streak combinations, duplicate/missing Wanderer town receipts, and non-Wanderer town fields. Reject states not structurally reachable from Begin/Observe; keep valid initial, partial and complete states. Existing invalid records fail closed rather than fabricate proof. This does not prove a save is authentic.
4. SandboxSaSBridge previously accepted any ApiVersion >=1 and reported provider support without checking callable members. Require exactly documented public v1 contract with a static bool getter and parameterless non-void snapshot method. Reject ambiguous providers and unsupported versions. Failed snapshot reads return an unavailable snapshot instead of partially confirmed enlistment. Installed v1.5.7.4 API still needs live verification. The bridge stays read-only.

Eight new executable policy Check calls exercise these repairs. The reflection fixtures test shape compatibility, not real SaS. No source test is claimed executed until the corresponding CI result is checked.

## Remaining audit queue
- P0: identify installed DLL SHA-256, effective local settings, actual active module list and startup log. Record warnings, not just icons. Do not overwrite known working 41-module installation.
- P0: existing native creator and family handoff, including missing-elder refusal and real spouse/child/sibling counts, all 12 roles, original native Back, 3 resolutions and save/quit/reload.
- P1: shared Clan, Party, Kingdom UI mixins and menu behavior with Customisable UI and Diplomacy. Inspect exact third-party patches before any changes.
- P1: keep experimental garrison, recruitment, party orders and kingdom mutators off in full-stack testing; actual user overrides must be audited. Do not add speculative runtime suppression based on module names or alter active convoy state. Confirm ownership and migration before implementing a compatibility gate.
- P1: audit remaining current career, family residence, household gifts, petitions and military witness state writers and readers; distinguish observed native effects from inferred standing. Reconcile source families with feature registry.
- P1: real SaS provider contract, actual service and pay, native election/ownership and land/sea boundaries. Never duplicate external enlistment/payroll or claim a vassalage/army command from modeled records.
- Acceptance remains exact pinned compile plus real control/full-stack UI, gameplay, 30-day and 60-minute smoke and save/reload checks. No expansion until those gates pass.


## Build and overlap follow-up

- Pre-patch run 45350357919: actual job logs show 1,119 gameplay policy checks, 65 settings catalog entries, 0 warnings/errors; DLL SHA-256 `be09cd918fa6bb65daeeea81d6678ed0eed0f18fa70bbefd4cb29f1675f18ff2`. The installed hash is unknown.
- First repair run [45351134112](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45351134112) stopped at `scripts/test_v066_realistic_life_paths.py:44`; it expected the ApiVersion string in the old bridge file. The existing assertion now checks delegation to the helper and exact v1/parameterless nonvoid contract. Retry commit 886b17fb7b57dcfbc14e83a2f37e8effe9678f91, [run 45351263526](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45351263526). This is a real CI retry, not a gameplay test.
- Source-confirmed additional garrison path: `SandboxOriginBehavior.NobleOfficerDialogues.cs` performs five-troop native party-to-garrison transfers through Master of Arms and Marshal dialogue. It checks native ownership, siege, actual officer, and existing healthy, untrained troops; it does not consult the older manual-transfer setting. This is a player-initiated action, not proven collision with Improved Garrisons. Review rollback confirmation, repeated player actions and actual concurrent ownership before changing behavior. Preserve separate manual transfer and automated garrison policies.
- Source-confirmed economic gap: `RefundWandererCompanionWage()` in `SandboxOriginBehavior.cs` uses a character wage-model quote, not a proven clan gold debit for that companion; the event calls the amount reimbursement without checking the gold delta. Inspect actual wage timing and coexistence with external SaS payroll before changing the existing wage-free companion contract. Do not introduce another global wage model.
- New source repairs are not certified for installed-game use. Some historical uploaded evidence may need resubmission if no longer accessible. Exact installed DLL hash and effective CU config are the next local evidence needed.
