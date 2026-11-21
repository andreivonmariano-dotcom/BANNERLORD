# v0.65a | Starting-role logic audit and campaign continuity

**Base:** successful v0.64a Windows CI 44842928660, DLL SHA-256 8703c700bd197e7ad7847303f5df7ff774d39ae3f5b1946e40ed73bbc2799da0. Draft PR #88. Work is experimental until an actual new-game/save-reload test.

## Confirmed incoherent flows

1. **Two-Faction War: a blocked World choice.** The native scenario requires both faction IDs but the pre-campaign editor hid `ScenarioFaction1` for `twofactionwar`. Now the first field is visible and explained, alongside the second.
2. **Unrealistic occupations.** Native generated adult siblings and retinue officers were categorically promoted to `Occupation.Lord`. Commoner adult siblings and non-noble household officers now receive `Occupation.Wanderer`; only noble mounted household field officers retain native Lord occupation. Bannerlord's limited Occupation enum is not a true medieval job taxonomy. Marriage-created spouses keep native marriage-compatible occupation until more game testing.
3. **False passing contracts.** Wrong spouse identity, partial family creation, refused explicit fief transfer, refused RoleBased officers and failed FamilyOnly companion removal now mark the final `sandbox_start_contract` as failed. A correct native kingdom alone is no longer sufficient evidence of success.
4. **Misleading biographies on failed campaigns.** The initial Living Persons snapshot is now created only after all currently implemented acceptance checks pass. Failed post-phase-8 grants are still not transactionally reversible. The game can remain started with a failed chosen biography.
5. **Memory saturation.** The v0.64 bounded ledger stopped storing new memories after 96 events. It now discards the oldest event to preserve the latest observations.
6. **Kingdom service vs personal enlistment.** Soldier remains the native mercenary *company* role; Knight the native landed-vassal role. The personal patron is genuine, but neither becomes a soldier inside a lord's native MobileParty. This distinction remains explicit in the World dossier and campaign record. Do not claim that following a lord's army, salaries or service promotions work.
7. **Unsettled noble dependence.** Second/third son starts still use an independent native vassal clan and require a fief. They are NOT actual landless cadets. Native estate law/transfer requires its own game-verified start type; do not suppress the fief invisibly.
8. **Birth-order questions remain context-sensitive.** Living male brothers currently determine rank. A deceased older son, birth legitimacy, half-siblings and maternal succession rights are not modeled. Historical law varies by region. Keep lineage labels explicit and record verified relationships rather than granting titles from assumptions.

## Implemented Q1: native career continuity, without fabricated wages

`SandboxServiceCareerState` records a real Soldier/Knight patron, native kingdom, daily days in service and observed changes in the player's total gold. The game-native mercenary/vassal economy is authoritative, so **CU does not add a second salary** or call an unrelated gold-change event a paid wage. Day 30 and 90 produce *tenure markers*, not invented promotions. A lord's disappearance from the live-Hero list is not treated as proven death. Native kingdom changes are recorded as divergent contracts, not silently dismissed obligations. A town/castle option provides the saved service and family status. Actual enlisted service, wages, AI orders, permission to leave and ranks remain future engine-backed work.

## Implemented Q2: observed relationship memory

`SandboxSocialObservationState` stores relationship scores from real Heroes across daily ticks. `SandboxLivingPersons.ObserveDaily` records observed changes in native relation, actual marriage ties and clan assignment, with **cause unverified**. A changed relation is not automatically described as love, hatred, betrayal or victory. Save-backed `CU_v065_social_observations` supports reload without inventing changes. The living history ledger retains the latest 96 recorded events. Expanded conversations and NPC decision agents are not active.

## Implemented Q3: future succession interest, not unearned inheritance

`SandboxSuccessionInterestState` persists a younger noble's actual father, mother, birth clan, and number of preceding male children. It records `PotentialFamilyInterest` only, not a legally resolved entitlement. If the verified native father dies, the ledger requests a future succession review. **No estate is transferred, no landless role is silently simulated, and no culture's inheritance rules are falsely assumed universal.** Correct landless service must be engineered around the native StartType and clan/kingdom models before being exposed as a selectable start.

## Remaining gate

Windows CI tests source wiring, pure state roundtrips and policy simulations against Bannerlord 1.5.7.125802. Actual in-game checks are needed for commoner companion occupation, native party membership, patron status through death/kingdom changes, 30 days of play, service/kin UI option, save/reload of three new keys, a correctly configured Two-Faction War, and a failed requested fief/retinue showing FAIL. Keep draft PR #88 unmerged until those pass.

## Next systems, gated on actual native behavior

- Verified rank-and-file **NPC party enlistment**, including battle participation, captain orders, wages paid once, desertion, leave, terms and promotion
- Event-derived character relationships, personal goals, promises and dialogue with real decisions
- Culture- and succession-rule-aware landless households, inheritance arbitration, fief grants, marriage settlement and inheritance disputes
