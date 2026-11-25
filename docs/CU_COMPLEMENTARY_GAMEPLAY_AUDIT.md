# Calradia Unified complementary gameplay audit

25 November 2026. Baseline v0.74.0, PR #100. This is a source-level pruning and ownership milestone, not a stable release. The current 41 selected modules were read from the actual LauncherData.xml; the historical 55-feature inventory and 41-module ownership CSV are preserved. Every historical feature has an explicit disposition in Data/CU_Complementary_Feature_Disposition.csv. Counts: OPTIONAL=1, RETIRED=26, KEEP=20, MERGE=6, DEFER=2. Disposition refers to active gameplay, not deletion of all historical C# or serialized data. Current source includes later household/social features beyond that historical inventory.

## Installed provider evidence and boundaries

- ImprovedGarrisons v4.3.1.0 SHA256 5CB75C0291FFAF95D1D0B01BF6C6BC9DCF73449F817756792E3DF4AB9DCF0564. Reflection-only audit resolved 99 metadata assemblies and 134 exported types. GarrisonRecruitmentLogic, GarrisonUpgradeLogic, GarrisonCostModel, GarrisonFoodModel, GarrisonpartySizeLimitModel, GuardOrdersVM and recruiter/guard actions establish functional overlap. UIManager.TryInitializeImprovedGarrisonsUI(), RecruitFromSettlement(MobileParty,Settlement,Hero,int,Hero), GiveGarrisonExp(Settlement) are public implementation methods, not a verified supported integration contract. No API-named exported types found. Do not invoke or patch those methods. CU raw troop trial refuses loaded ImprovedGarrisons; no recruitment/training/patrol writes from CU.
- ServeAsSoldier v1.5.7.4 exact v1.5.7 DLL SHA256 9C623E5CB654B7673DC0F35BC0D3C796EFAD2918713907F6E6074E22C721D4BA. Keep read-only native rank and pay observation, no duplicate rank or payroll.
- Fourberie v1.5.7.0 SHA256 973253CE339B5E51048DD8737889FFA33199B830B65D3F813E2E129BDB7FCC48. No independent intrigue or criminal progression.
- Diplomacy selected, exact installed binary hooks not audited in this pass. No CU kingdom diplomacy automation or war/peace manipulation. Native personal relationship changes must remain face-to-face and verified.
- BirthAndDeath v1.5.7 and NavalDLC v1.3.7 selected. Preserve native lifecycle and sea ownership. Do not override combat, camera, formation, equipment, troop tree, purchases or map rendering owners.

### All selected module boundaries
- 1. Bannerlord.Harmony — Framework: Current mandatory dependency; inspect patch ownership, order and method signatures.
- 2. Bannerlord.ButterLib — Framework: Current mandatory dependency; use supported version.
- 3. Bannerlord.UIExtenderEx — Framework / UI: Current mandatory dependency; CU extends Clan, Party, Kingdom; audit prefab and mixin interactions.
- 4. Bannerlord.MBOptionScreen — Settings: Current mandatory dependency; CU MCM settings must remain uniquely scoped.
- 5. Native — Engine: Native authority for missions, stats, inventory; no duplicate side effects.
- 6. SandBoxCore — Core data: Preserve native item/character IDs and data definitions.
- 7. Sandbox — Campaign: Native Hero, Clan, party, fief, menu, creator, succession and save authority.
- 8. CustomBattle — Test mode: Do not register campaign-only behavior in non-campaign games.
- 9. StoryMode — Story campaign: Custom starting-life work scoped to Sandbox; audit no story mutation.
- 10. BirthAndDeath — Lifecycle: Native age, birth, death and family authority; don't duplicate births or force aging.
- 11. NavalDLC — Naval: Exclude unsupported sea travel/mission actions; observe native state.
- 12. ServeAsSoldier — Military service: Read-only SandboxSaSBridge; external owns enlistment/pay/rank/discharge.
- 13. RTSCamera — Mission camera: No competing camera/mission input hooks.
- 14. RTSCamera.CommandSystem — Formation command: No competing formation orders or input.
- 15. RBM — Combat: Do not replace combat formulas, armor or battle AI.
- 16. RBM_WS — Naval combat: Do not override naval combat; test at sea.
- 17. ImmersiveBattlefields — Battle presentation/behavior: Avoid competing mission/formation behavior; exact installed hooks uninspected.
- 18. BannerFix — Banner performance: No parallel banner performance patch.
- 19. Vexillum — Banner assets: Do not overwrite assets or native banner IDs.
- 20. RaiseYourBanner — Battle presentation: No banner-spawning or mission visual replacement.
- 21. RealisticBlood — Battle presentation: No blood/agent-effect patch.
- 22. RaiseYourTorch — Battle presentation: No parallel torch spawning or mission effects.
- 23. Bannerlord.Diplomacy — Politics: Owner of external diplomacy/factions; review CU relation, kingdom UI and ownership changes.
- 24. ImprovedGarrisons — Garrisons: Owner of recruitment/training/guard parties; hold parallel CU mutations.
- 25. Fourberie — Crime and intrigue: No competing criminal system, blood feuds or spymaster roles; inspect relation/trait hooks.
- 26. ArenaOverhaulRedux — Arena: Observe native tournament outcomes only; no parallel rewards.
- 27. OpenSourceArmory — Equipment: Read-only catalog; preserve item IDs/stats.
- 28. OpenSourceWeaponry — Equipment: Read-only catalog; preserve XML/XSLT weapons.
- 29. OpenSourceSaddlery — Equipment: Read-only catalog; preserve mount IDs.
- 30. OpenSourceArmoryRBM — Equipment balancing: Defer armor stats to existing patches.
- 31. OpenSourceWeaponryRBM — Equipment balancing: Defer weapon stats to existing patches.
- 32. OpenSourceSaddleryRBM — Equipment balancing: Defer mount stats to existing patches.
- 33. BreakablePolearms — Combat durability: No second breakage system.
- 34. ExpandedArmouryBL — Equipment: Preserve original item data and duplicate quarantine policy.
- 35. ExpandedArmouryBL.FullFixRBM — Equipment balancing: Preserve Standard Mod > Normal Names and RBM stats.
- 36. ZZZ_troop_overhaul — Troop progression: Use actual troop IDs; preserve upgrade trees/rosters.
- 37. NoblesBuyStuff — NPC purchasing: Observe equipment purchases; no competing purchase or equipment overwrite.
- 38. KingdomBorders — Map presentation: Do not assume it owns diplomacy; test map overlays, exact hooks uninspected.
- 39. CustomisableUI — Presentation: Test creator, Clan/Party/Kingdom prefab and menu layout; no blanket styling override.
- 40. CalradiaUnified — Own mod: Additive personal/household layer; current v0.38.0.0 manifest is not a DLL build identifier.
- 41. EBA_OSA_ZZZ_CultureGear — Own compatibility module: Preserve six targeted troop and four noble armor assignments; no new broad roster override.

## Current gameplay surfaces, after pruning
- Private conversations: three direct personal subjects rather than nine; restrict opener to actual relationship/service or pending petition context. Keep family gift, upbringing, real requests, native relation effects, personal Knight service. Remove four abstract story response choices. Preserve bounded memories for context, not separate gameplay objectives.
- Remove new artificial career charges across all roles, recurring review menu, manual career recording choices, fake household appointments and parallel retainer enrollment/rank/pay. Old charges can be closed; old retainer records readable; existing duties releasable. No new artificial title grants.
- Group discussions: remove new gathering entry and scheduling; existing saved gathering can attend/cancel/expire. Keep old menus and exits for persistence. No new standalone social simulation detail switch.
- Camp: retain illustrated roster and correspondence, remove duplicated officer/next-duty/recurring menu items. Clan dossier remains family and officers reference; map correspondence remains a separate inbox. Do not claim every duplicate Gauntlet tab has been removed yet.
- Civic and autonomy: remove all active event-driven civic/AI mutations and log-only advisory scans. Keep primitive SyncData keys and exit-only legacy civic menus. Remove old civic entry and MCM advisory switch. No new custom depot, recruitment, training, convoys, patrol or political transactions. Supply warnings remain optional; foraging hard-disabled.
- Governor assignment: one genuine conversational shortcut via verified public ChangeGovernorAction.Apply(Town,Hero). Require actual adult same-clan spouse/companion, present in owned non-besieged fortification, not in a party, no governor incumbent, no prison/travel/fugitive state, nonnegative native relation and live player clan leader. Check reciprocal native references after action. No teleport, title-only record, replacement or custom garrison simulation. Native playtest pending.
- Garrison transfer trial is restricted to no ImprovedGarrisons loaded. No full-stack garrison delegation implemented. Provider-supported bridge requires an actual stable contract and controlled runtime evidence.

## Seven careers: intended distinct loop and remaining test
| Career | Native/provider owner | CU's complementary loop | Retired or prohibited | Native acceptance |
|---|---|---|---|---|
| Wanderer | Native travel/trade | Origin, one loyal companion, meaningful family ties | Visit-town chores, custom foraging/trade | Start, relation, save/reload |
| Outlaw | Fourberie / native | Personal background, actual outlaw band at start, consequences | Duplicate crime ranks, custom crime missions | Band, criminal provider, native law |
| Mercenary | Native / Diplomacy | Real contract, history of actual victories and companions | Fake mercenary tasks, parallel wages | Contract and battle/reload |
| Soldier | SaS | Family, native service record and personal relationships | CU grades, wages, promotion and serve-week chores | Actual rank/provider and native service |
| Knight | Native / CU oath | Sworn patron, genuine scouting/muster/escort and witnessed service | Parallel generic career tasks or fake vassal rank | Oath, report and native service |
| Lord | Native / ImprovedGarrisons / Diplomacy | Family, real local governor, personal petitions, inheritance records | Artificial estate chores, duplicate recruitment/training, fake offices | Governor, petition, actual estate outcomes |
| King | Native / Diplomacy / ImprovedGarrisons | Royal household, council with actual authority, native minister, dynastic history | Fake command, parallel political authority and decorative deliberation | Royal court, governor, native political authority |

## Validation and remaining work
Historical source inventory disposition and runtime registration guards do not establish all module compatibility. Check 41-module startup, native conversation, governor eligibility/rejection and native references, no raw transfer with IG active, family gift and education, petitions, seven career loops, save/load and 60-minute session. Preserve old saved records and state keys; no destructive migration. No merge/release or live installation change. Use a disposable test save for first native governor action; no claim of recruitment delegation until a supported IG integration exists.
