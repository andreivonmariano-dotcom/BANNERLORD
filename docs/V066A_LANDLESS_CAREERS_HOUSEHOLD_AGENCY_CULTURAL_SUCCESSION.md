# v0.66a | Landless native careers, actual enlistment evidence, households and cultural succession

**Status:** Experimental implementation in draft PR #88. Final CI evidence is a policy simulation and Bannerlord 1.5.7 assembly compilation, **not** an in-game playthrough. Do not merge before runtime acceptance.

## Q1: Soldier, recruit and knight careers

Existing **Soldier** remains Bannerlord's *mercenary captain* start, and existing **Knight** remains a *landed vassal*. Their native political systems stay authoritative.

Two new **native independent starts** are available. **Recruit** is a landless enlistment applicant, and **HouseholdKnight** is a landless knight with a named potential lord. Neither is falsely given a native fief, kingdom contract, wages, troop-leader authority, or actual NPC-party membership. Their real patron is selected from the requested native kingdom. A missing patron fails preflight.

Recruit requires the compatible *Serve as Soldier* public API v1 to be installed during World confirmation. Without it, World Setup rejects Recruit and suggests the existing native mercenary-captain Soldier start. The optional adapter reads the documented `ServeAsSoldier.Compat.SaSPublicApi` **read-only** using reflection. It verifies actual `IsPlayerEnlisted`, `CurrentLord`/snapshot Lord, host-party membership, days, rank, duty, daily wage and term. The **other mod owns the actual enlistment dialogue, battle orders, wages, promotion and discharge**. Our adapter cannot start enlistment automatically because the public API documents no enlistment action. A Recruit remains pending until the player enlists through that provider's own supported game interaction. No hard dependency or private method calls are introduced.

The service career save format `CU_SERVICE_2` migrates `CU_SERVICE_1` and distinguishes PendingEnlistment, HouseholdContact, native Soldier/Knight affiliation and verified external EnlistedActive. It stops counting unearned service days for a waiting recruit. Actual paid wages are never duplicated. The record is derived again from provider state after loading. Removing the service mod cannot turn an unverified record into proven active enlisted membership.

## Q2: Real people, homes, goals and gradual relationships

`SandboxCharacterAgendaState` persists one bounded agenda per observed Hero with goal, actual native residence, verified provenance, occupation, clan and shared-service credit. It is save-backed under `CU_v066_character_agendas`, with a versioned read/write parser. A home is recorded only from the engine's actual `StayingInSettlement` or `HomeSettlement`. Unknown residences remain unknown.

Goal assignment is deterministic and limited: a sibling actually serving in the player's party receives `ProtectSharedHousehold`, while a factor, squire, navigator, child, spouse or noble elder receives an appropriate aspiration. Goals are **generated motivations**, not invented past events. A goal affects the game in one narrow, justified case: two verified adult siblings with the same parents in the same real native party earn at most **+5 native relation** through an actual `ChangeRelationAction`, one point per 30 continuous days, with a native relation cap of 80. Separation resets the continuous-service clock. No imaginary battles, friendships or dialogue events are created.

Older noble brothers **and older noble sisters** selected in House now stay with their actual father's clan. The mod uses native `EnterSettlementAction.ApplyForCharacterOnly` in a fortification genuinely owned by that clan, or joins their father's actual MobileParty using `AddHeroToPartyAction`. Their starting presence is verified, not only described. If neither placement exists, preflight rejects a genealogy that requires those older household members. Younger specified siblings still join the player by current House design.

## Q3: Landless noble cadets and culture-dependent inheritance review

New **NobleCadet** uses the game's true independent start. It requires second/third son or younger sibling ties to an eligible existing native noble from the chosen kingdom. It is not automatically a vassal, owns no fief, and does not pretend to be a member of its father's game-controlled clan. The two Heroes still have real shared native parents, and elder siblings stay in the real birth household.

The pure `SandboxCulturalInheritancePolicy` defines **six explicit regional gameplay models**: Vlandian HouseholdSeniority, Sturgian ClanConsensus, Battanian KindredCouncil, Imperial CivilAppointment, Aserai FamilyNegotiation and Khuzait ClanCoalition. These are game-design interpretations of cultural tradition, **not verified universal historical laws or new native game rules**. The v2 succession save stores the model, actual birth-clan fief count, older male kin and parentage, and migrates prior v1 data. A parent death produces a native-estate review. **Native settlement owners never change without the game's own grant action.** Unmeasured clan votes are not turned into a fake legal entitlement.

## Simulation

`SandboxMedievalLifeSimulation.Run(96,3650)` is an independent deterministic state-machine stress test covering **350,400 modeled campaign days**. It alternates four career types over six cultures, evaluates birth-rank reconciliation, rejected false fief/kingdom contracts, recruit waiting periods, externally verified service and departure, cumulative sibling comradeship limits, parent death, save/reload cycles and unchanged native estate counts. PolicySmoke logs cases, check count and first invariant failure. The ordinary existing CampaignSimulator still executes its 40 policy campaign trajectories.

This model is not Bannerlord's game engine: it cannot verify native party placement, noble father candidate pools, companion limits, external SaS installation, scene transitions, actual campaign AI, save serialization within the game, or named Hero appearance. Those need in-game diagnostics ZIPs and a fresh new-game acceptance run.

## Specific in-game acceptance

- **NobleCadet, Vlandian, age 20, SecondSonOfLord:** verify native independent clan, no fiefs, real father and mother, elder sibling in father's clan and native party/castle, and a readable household agenda after saving/reloading
- **HouseholdKnight:** verify independent clan, no fief, actual named lord in chosen kingdom, no invented vassal status or daily wage
- **Recruit with Serve as Soldier installed:** verify initial PendingEnlistment; enlist by the provider's legitimate lord dialogue; confirm actual current lord, rank, duty, wages and host party via its public snapshot; leave service and verify observation and save/reload
- **Sibling co-service:** keep a verified adult younger sibling in the player's actual party for 30 continuous days, confirm native +1 relationship, reset by separating, never exceed five total awards
- **Regional claims:** inspect all six models, verify parent-death review and no settlement-owner changes

Keep draft PR #88 unmerged until gameplay evidence supports these claims.
