# Calradia Unified v0.73 | Native battle losses and patron-witnessed military service

Date: 22 November 2026. Target: Bannerlord PC v1.5.7.125802.
Parent feature: v0.72 Battle History and optional Serve as Soldier bridge.
Source branch: feature/v073-battle-casualties-knight-merit.
Status: **SOURCE CANDIDATE until pinned CI passes; game acceptance remains outstanding.**

## Q1 | Actual native killed and wounded party records

The read-only map-event end observer now reads one **specific participating party's** native `MapEventParty.DiedInBattle` and `WoundedInBattle` troop rosters. It records `TotalManCount` from each only when both are available. These figures are **party losses**, not enemy casualties or all-formation totals.

A fixed-size list (up to four each) of *verified own-clan Hero IDs* in these native casualty rosters is retained as battle-specific injured/fallen witness evidence. These are not invented character deaths, exact injury descriptions, damage sources, or personal player kills. Own-clan Hero identity comes from the native `Hero` and its real `Clan` / `CompanionOf` values. Heroes who have lost those relationships after a death may be omitted. This omission is better than assigning false household membership.

Native casualty properties have been described in 1.3.4 Doxygen docs; exact 1.5.7 source references must compile. A green build still does not prove event order or values at actual battle exit. The SaS host party is selected from the public SaS API, then matched against `MapEventParty.Party.MobileParty`. An independent player's own party matches `PartyBase.MainParty`. No side-wide casualty totals are assigned to a single clan.

Save schema `CU_BATTLE_HISTORY_2` keeps 32 outcomes, adds `PartyDead`, `PartyWounded`, `WoundedHeroIds`, `FallenHeroIds` and `PatronWitnessId`. The original save key `CU_v072_battle_history` remains unchanged. Old `CU_BATTLE_HISTORY_1` records migrate with `-1` unknown casualty values, never a fabricated zero. The parser rejects malformed IDs, impossible counts and duplicate battle fingerprints. All entries remain read-only.

## Q2 | Knight battle standing and authentic lord relationships

A patron battle witness requires **both actual native parties on the same battle side**, a genuine active personal oath, and the exact patron Hero controlling his own participating party. It does not presume the patron was physically present in the player's formation or observed an individual kill.

The player's real native relation, renown and completed service orders still determine the household standing labels. Battle participation does not itself create titles or rank.

Two separate, bounded personal acknowledgements are available:
- **After 2 witnessed joint victories**: report in a real conversation with the exact living, free sworn patron. The game receives a single native relationship increase of +1 if the engine accepts the action.
- **After 5 witnessed joint victories**: a second report can attempt +1 native relation, on a distinct campaign day. No third award.

Native `KnightOathNativeRelation` is the existing verified relation adapter. The separate `CU_v073_knight_battle_acknowledgements` save state records stage, patron ID, and last reporting day. The state is marked before attempting native relationship change to prevent repeated free attempts after callback re-entry. The journal distinguishes an attempted report from an observed native relation change. Neither report grants influence, fiefs, cash, a kingdom rank or a Serve as Soldier promotion. A player who never speaks with the patron receives no relationship change from battles alone.

A lord's army does not automatically qualify as the patron. Different personal lord IDs remain distinct. A lost or dead lord cannot receive the dialogue. SaS enlisted host battles are not counted as a Knight's independent sworn-household joint victory; SaS controls the Soldier career.

Known risk: military witness receipts are bounded to the last 32 battles. Very long campaigns can lose old unrecognized deeds. This is a retained-chronicle milestone, not an unbounded lifetime combat-service ledger. The stage/acknowledgement state, once awarded, survives eviction of those battle entries.

## Q3 | High-realism battle profile comparison

Three extensions target different systems.

### De Re Militari (DRM)
Public source: https://www.nexusmods.com/mountandblade2bannerlord/mods/2505
Current listed version 6.1.4, last updated 18 July 2026. The **current main package is De Re Militari - War Sails and explicitly requires the expansion**. This is a troop tree and equipment overhaul; the author identifies RBM Combat as required for intended balance, loaded *before* DRM. DRM changes culture and partyTemplate XML, creating direct overlap with troop recruitment and equipment assumptions. Author warns that DRM troop additions cannot simply be removed from a save. For this project, **do not install the current main DRM on our base-game-only profile**. Test it only on a new War Sails-specific profile and preserve original saves. A historic 2021 file labeled "1.5.7" belongs to the old early-access generation, not evidence that current DRM supports the 2026 v1.5.7 target.

### Better Pikes
Public source: https://www.nexusmods.com/mountandblade2bannerlord/mods/5150
Current main file (Nexus listing) v1.2.7, dated 18 July 2026, **author states tested only for Bannerlord 1.4.x**. Requires Harmony, MCM, RBM Combat + AI, and Troops Prefer Spears. Targets pike length, brace animation and pikeman formations. It overlaps with troop equipment and formation logic; do not assume support for our v1.5.7 until exact installed build and combined 100v100 battle show working behavior. The historical 78-mod inventory's archived `v1.4.18` is not a verified replacement for the current download.

### Procedural Scenes
Public source: https://www.nexusmods.com/mountandblade2bannerlord/mods/14407
Current listing v1.0.1. Author reports testing on Bannerlord 1.5.7. Generates geography-based terrain at runtime, with pathfinding and generated scene data. The author reports about 3-4 seconds generation, not a benchmark recorded by this project. Higher risk on terrain/map transitions, navmesh, navigation and performance. This mod has the **clearest stated v1.5.7 target** among the three and is the best first advanced test, in a separate slot after the basic combat stack is stable.

### Decision

1. Keep SaS + RBM + RTS and the existing visuals/audio stack as the control profile.
2. Add **Procedural Scenes alone** on a fresh test profile. Compare plains, hills, river crossing, coast, cavalry pathing, siege start, soldier host-party controls and memory use.
3. Try Better Pikes only after an explicit 1.5.7-compatible file / full prerequisite set is available or a supervised isolated test supports it. Do not assume seamless compatibility with DRM troop gear.
4. Run current DRM **only with War Sails**, after native naval and troop XML compatibility checks. Do not copy DRM troop definitions or any third-party DLL into CU.
5. Do not activate all three simultaneously in the first high-realism test.

### Required gate-C to gate-F cases

- Player field victory with 3 native killed and 8 wounded party troops: exact counts must match native battle summary, including verified Hero IDs.
- A different allied party suffers losses: they must not be attributed to the main party.
- A wounded companion survives: named receipt, unchanged actual Hero status and native healing.
- An own-clan Hero dies or is captured: only a verified death roster is labeled fallen, no false death caused by CU.
- SaS enlisted host casualty roster: match actual host party, not hidden or independent player party.
- End event after SaS releases its host: no invented casualty record if host is no longer valid.
- Save/reload a v0.72 battle ledger and the v0.73 new schema. No duplicate battle, relation or Hero casualty.
- Win two battles on the same side as a real sworn patron. Meet the exact lord and report once: +1 observed native relation, no secondary grant on same-day repeat.
- Win five such battles total. Return on a later day: at most one second +1. A different lord, a defeat, a random lordless victory or an unverified oath grants nothing.
- Check real knight standing labels remain dependent on native relation, renown and service counts.
- Perform an isolated 100v100 pike cavalry charge for Better Pikes when eligible.
- Validate three generated field scenes, navigation and retreat for Procedural Scenes.
- With a separately tested War Sails installation, check DRM troop recruitment, party templates and land/ship battle transitions.
- Run 60-minute mixed-battle play, then save, quit, reload, and review managed and native crash reports.

**CI pass = tested model and exact compiler references. Native battle and external-mod compatibility require actual in-game evidence.**
