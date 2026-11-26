# Research encyclopedia | entry point
25 November 2026. A living navigation index, not a claim of exhaustive engine knowledge. Resolve current lab HEAD. Read [current handoff](Handoff/CURRENT.md), [failure register](Failures/REGISTER.csv), [knowledge protocol](KNOWLEDGE_PROTOCOL.md), then relevant actual files. Stable IDs are in [CATALOG.csv](CATALOG.csv).

- [Native engine reference](NATIVE_ENGINE_1_5_7.md); [economy and governors](Engine/ECONOMY_AND_GOVERNORS.md).
- [Gauntlet UI](UI/GAUNTLET.md), [mission lifecycle](Missions/LIFECYCLE.md), [save system](Persistence/SAVE_SYSTEM.md), [War Sails](WarSails/NAVAL.md).
- [Comparative mod studies](MOD_IMPLEMENTATION_STUDIES.md), [independent implementation patterns](Patterns/CONTRACTS.md).
- [Failure analysis](Failures/README.md), [cumulative learning log](Handoff/LEARNING_LOG.md).
- [L001 native probe](Prototypes/L001.md), [evidence](LAB_EVIDENCE.md), [acceptance matrix](EXPERIMENT_MATRIX.csv).
- [Research record template](Templates/RESEARCH_RECORD.md), [gameplay acceptance template](Templates/GAMEPLAY_ACCEPTANCE.md).
Evidence anchor 29d8233ea33404904df4fc829401c8c6a1106fa8; compiled L001 source fc771db2e67d3fd3545a857b6ccab4bba37fc861. New entries require exact sources, status and next tests.
- [ENG-002 market gold, construction reserve and governors](Engine/MARKET_AND_GOVERNOR_MODELS.md); [L002 observation probe](Prototypes/L002/README.md). L001 held; L002 compiled only.
- [AI-001 character motivations and decision ownership](Engine/NPC_DECISIONS.md), [QST-001 quests and personal obligations](Engine/QUESTS_AND_OBLIGATIONS.md). External/production source studied; installed verification pending.
- [AI-002 installed AI and persuasion](Engine/NATIVE_AI_MODELS.md), [QST-002 installed quest lifecycle](Engine/NATIVE_QUEST_LIFECYCLE.md). Static installed verification completed; native gameplay NOT_RUN.
- [SAV-002 native save registration](Persistence/NATIVE_SAVE_REGISTRATION.md), [CON-001 native conversations](Engine/NATIVE_CONVERSATIONS.md), [DUT-001 companion duties](Engine/COMPANION_DUTIES.md). Installed static findings; no native test.
- [DUT-002 delegated issue mechanics](Engine/DELEGATED_ISSUES.md); [D001 control campaign](ControlCampaigns/D001.md). Static trace and staged profile verified; gameplay pending.
- D001 control selection now activated; [protocol and activation evidence](ControlCampaigns/D001.md). Gameplay NOT_RUN.

D001 playthrough reported; game process and selected profile verified, delegation not run. See D001 record.
- D001 native dispatch visually observed; risk and troop allocation confirmed in selection UI. Persistence and outcome pending.
- D001 active journal and aggregate party count verified; save/reload next.
- D001 active journal/aggregate party persistence visually passed after reported reload and new process; individual roster and return pending.
- D001 post-reload companion status verified; one clan party listed. Resolution and return pending.
- D001 intermediate return journal observed with party still 54; final outcome pending.
- D001 native success and return notification plus reward observed; detailed roster and post-return reload pending.
- D001 completed native quest and companion return/aggregate roster observed; post-return reload, control clarification and player critique pending.
- D001 positive native lifecycle baseline observed; successful post-return reload reported by player; critique and independent duplicate-outcome audit remain pending.
- [SAV-003 offline save inspection](Persistence/OFFLINE_SAVE_INSPECTION.md): D001 exact save metadata, section framing and selected strings verified. No typed object reconstruction.
- [INV-001 installation inventory](Simulator/INVENTORY.md), [SIM-001 standalone simulator](Simulator/README.md) and [execution evidence](Simulator/EVIDENCE.md). Independent model tests, not native gameplay.

INV-001 terminal result: remote process 35128 completed exit 0 in reported 202.84s. All 4,986 DLL/EXE file paths hashed; 4,881 managed assembly metadata recognized, 105 nonmanaged_or_invalid (not proof every one is native), 0 unknown and 0 hash errors. All 42 root SubModule.xml manifests parsed; 43 populated module directories, PerformanceOptimizer lacks root manifest. Process 43556 confirmed 4,986 binary rows, 2,841 distinct binary SHA256 values, 2,738 distinct managed hashes, 42 manifest rows. Local binaries.csv 1,029,774 bytes SHA256 1df7907726a463192f4e302e6ce97539377d9b42a84410ddf1a7502380d05832; modules.csv 3,291 bytes SHA256 4c72925e20a0a26c26da5e5e57a47ae491219a357860626aa486012f76f8d825; files.csv hash unchanged. The verification command encountered missing errors.txt because the empty error pipeline did not create a file; inventory itself reported zero errors and finished successfully. Correct reusable scanner to write empty error file explicitly. Metadata/fingerprinting is not decompilation or source content analysis. Use unique binary hashes to avoid duplicate research. No proprietary files uploaded.
- [DATA-001 world source database](Simulator/WORLD_DATA.md), [API-001 managed metadata catalogue](Simulator/ASSEMBLY_INDEX.md), [SIM-002 persistent campaign model](Simulator/WORLD_CAMPAIGN.md). Local databases; independent research only.
- [ECO-001 economy](Engine/ECONOMIC_MODELS.md), [LIFE-001 character lifecycle](Engine/CHARACTER_LIFECYCLE.md), [DATA-002 tick/merge and source-world projection](Simulator/WORLD_IMPORT.md), [SIM-003 rule tests and market experiment](Simulator/NATIVE_RULES.md).
- [HAR-001 native managed execution](Simulator/NATIVE_HARNESS.md), [ECO-002 supply and demand](Engine/SUPPLY_DEMAND_AND_TRADE.md), [LIFE-002 synthetic dynasty](Simulator/DYNASTY.md), [SIM-004 evidence](Simulator/SIM004_EVIDENCE.md).
- [HAR-002 expanded managed model probe](Simulator/HAR002.md), [SIM-005 integrated research campaign and batch evidence](Simulator/SIM005_EVIDENCE.md).
- [HAR-003 constructor dependencies](Simulator/HAR003.md), [AI-003 competing NPC objectives](Engine/COMPETING_NPC_OBJECTIVES.md), [VAL-001 differential validation](Simulator/VALIDATION.md).
- [HAR-004 text service](Simulator/HAR004.md), [SAV-004 typed save records and relation](Persistence/TYPED_SAVE_RECORDS.md), [DUT-003 delegation outcomes](Engine/DELEGATION_OUTCOMES.md).
- [SAV-005 roster/skills/equipment projection](Persistence/ROSTER_AND_INVENTORY.md), [SIM-006 imported campaign experiment](Simulator/IMPORTED_CAMPAIGN.md).
- [SAV-006 broad world projection](Persistence/WORLD_PROJECTION.md), [ECO-003 daily settlement and finance](Engine/SETTLEMENT_DAILY_UPDATES.md), [MIL-001 military power and combat](Engine/MILITARY_POWER_AND_COMBAT.md).
- [GEO-001 campaign geography and navigation](Engine/CAMPAIGN_GEOGRAPHY.md), [MIL-002 regular-troop experiment](Simulator/MILITARY_EXPERIMENT.md), [UI-LAB-001 offline atlas](Simulator/ATLAS.md).
- [TIME-001 native clock and scheduler](Engine/CAMPAIGN_CLOCK.md), [SIM-007 experimental NPC decisions](Simulator/AUTONOMOUS_CAMPAIGN.md), [UI-LAB-002 campaign control](Simulator/CAMPAIGN_CONTROL.md).

- [VAL-002 retrospective world differential](Simulator/RETROSPECTIVE_VALIDATION.md): security and gold worse than no-change benchmark; native endpoint movement quantified; prospective validation pending.

- [API-002 military and economic observation contracts](Engine/NATIVE_OBSERVATION_CONTRACTS.md); [VAL-003 exact-clock validation](Simulator/EXACT_CLOCK_VALIDATION.md): zero tick residual, town/castle metrics, accuracy gap retained.

- [API-003 native implementation call edges](Engine/NATIVE_IMPLEMENTATION_TRACE.md), [ECO-004 economic observation](Simulator/ECONOMIC_OBSERVATION.md), [D002 prospective control](ControlCampaigns/D002.md), [CU evidence matrix](CU_EVIDENCE_MATRIX.csv): offline tooling tested; native control not run.

- [API-004 selected native arithmetic](Engine/DEFAULT_RECRUITMENT_FOOD_HEALING.md), [L003 native observer](Prototypes/L003.md), [SAV-007 single baseline](Simulator/SINGLE_BASELINE_IMPORT.md), [DESIGN-001 surgeon duty candidate](Prototypes/SURGEON_DUTY_DESIGN.md).

- [ROLE-001 specialist effects](Engine/NATIVE_SPECIALIST_EFFECTS.md); [D002-R role observation protocol](ControlCampaigns/D002_ROLE_TEST.md), L003.1 compiled only.

- [D002-R authorized activation and launcher checkpoint](ControlCampaigns/D002_R_ACTIVATION.md): installed observer, 43 saves backed up, native test not run.

- [AUT-001 guarded remote graphical control](Autonomous/AUT001.md): launcher and native mouse/text input verified, fresh paused Sandbox and L003.1 session callback observed, no saved checkpoint or recovery experiment.

- [NAV-002 active War Sails model chains](WarSails/ACTIVE_MODELS.md): 48 selected static bodies and isolated native factor arithmetic; full live outcomes pending.

- [NAV-003 crew and ship contracts](WarSails/CREW_SHIPS_AND_SPEED.md); [AI-004 political and marriage contracts](Engine/POLITICAL_AND_MARRIAGE_CONTRACTS.md); [SIM-008 provider terms](Simulator/PROVIDER_TERMS.md): static findings and isolated arithmetic 120/120; full native outcomes not run.

- [AUT-002 native checkpoint, reload, arrival and recruitment](Autonomous/AUT002.md): two verified saves, actual transaction and food/arrival evidence; callback gap open.

- [AUT-003 recruitment event repair and conditional food forecast](Autonomous/AUT003.md): L003.2 compiled only; SAV-008 two pinned observer checkpoint imports; first-event numerical match and restored checkpoint.

- [AUT-004 observer upgrade and full restart](Autonomous/AUT004.md): v032 load verified; scene controls limited; four unscored component hypotheses.

- [AUT-005 native scene escape and recruitment receipts](Autonomous/AUT005.md): L003.2 recruitment callbacks and independent new three-member save verified; VAL005 outcomes pending.

- [AUT-006 bounded native control and companion negotiation](Autonomous/AUT006.md): two normal-speed pulses paused, forward/lateral movement, camera and direct scene exit observed; native wanderer quote and decline verified.

- [AUT-007 prospective food trial and shared autosave isolation failure](Autonomous/AUT007.md): eight pulses; endpoint absent; original autosave backup verified; native advancement blocked.

- [CU80 redevelopment milestone](CU80/EVIDENCE.md): 143-entry audit, six-system GDD, architecture and L004 r2 pure C# prototype 61/61; native/player acceptance held.

- [L005 native adapter, autosave recovery and contextual policies](CU80/L005.md): compiled native port; 46 additional pure checks; 43/43 originals restored; native isolation still blocked.

- [L006 actual isolated CU integration and persistence](CU80/L006.md): full candidate CI138/138; 275 local checks; save namespace gate blocks live experiment.

- [L007 native candidate and reversible save isolation](CU80/L007.md): real Lord startup, unique save and same-process reload; rollback verified; surgeon encounter still untested.
