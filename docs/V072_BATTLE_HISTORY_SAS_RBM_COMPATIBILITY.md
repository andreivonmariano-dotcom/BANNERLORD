# Calradia Unified v0.72 | Battle History + Serve as Soldier integration

Date: 22 November 2026. Base: draft PR #93, head `88809b1a9eeff8fba730503514d7fef12f141e21`.
Target: Bannerlord PC `1.5.7.125802`. Feature branch: `feature/v072-battle-history-sas-integration`.

Status: **SOURCE CANDIDATE until CI passes. No in-game compatibility certified.**

## New gameplay receipt

- `CampaignEvents.MapEventEnded` is a native campaign event with one `MapEvent` argument. The observer reads `HasWinner`, `WinningSide`, `PlayerSide`, `BattleStartTime`, encounter kind, native settlement ID and participating party names / lord IDs. It never edits the event.
- For the main party, require `IsPlayerMapEvent`. This includes simulated/auto-resolved encounters. The ledger describes **party involvement**, not personal kills or time personally spent fighting.
- For enlisted service, the existing optional `SandboxSaSBridge.Read()` obtains `IsPlayerEnlisted`, `HostParty`, `IsPlayerInsideHostParty` and the native lord from the official v1 public API. The observer checks that exact host party is involved, determines its attack/defense side and records the real outcome. It never treats a mercenary-clan contract, role label or invisible player party as proof of enlistment.
- New save key `CU_v072_battle_history`, version `CU_BATTLE_HISTORY_1`. The observer keeps the most recent **32 battle records**. Each has a deterministic hashed event fingerprint, campaign day, battle kind, result, provenance, optional settlement ID and native lord ID. Malformed records fail closed. Same event key does not produce a duplicate.
- Career Chronicle offers **Review witnessed battle service**. The household's Chronicle folio shows recent battle summaries. No extra Gauntlet asset is required. Both are read-only.
- The implementation has **no direct SaS binary reference or SaS code copy**, no Harmony patch to a mission, no new configuration dependency, no loot/gold/renown/relation transaction and no personal casualty inference.

### Known scope limits

1. A `MapEventEnded` outcome is not a guaranteed account of personal kills, wounds, individual casualties, formation command, or exact battlefield coordinates. Those are not recorded.
2. The deterministic event key uses native battle start time, involved party identity labels, encounter type and native place. Rare collisions between extremely similar encounters are possible. This is a bounded journal, not a transactional map-event database.
3. SaS mission lifecycle can temporarily move the hidden main party. A real SaS session must verify that `IsPlayerInsideHostParty` remains true when `MapEventEnded` fires. If false, the observer refuses the SaS receipt instead of inventing one. This is a primary live acceptance test.
4. The campaign callback is not guaranteed to cover custom mission-only battles that never create native `MapEvent`. Naval fights and scripted missions need separate work.
5. The UI and save key require real save/load validation. Existing saves lacking the key parse as an empty battle ledger.
6. Battle records **do not yet change** Knight standing, household survivor status, SaS promotion, or political relations. Future effects must be gated by additional native evidence and validated against the social record.

## Selected mods for stage testing

- **Serve as Soldier (SaS)**, official https://www.nexusmods.com/mountandblade2bannerlord/mods/3242 . The author updated the mod for Bannerlord 1.5.7 on 20 November 2026 and lists RBM and RTS Camera as compatible. Its public reflection API contract: https://www.nexusmods.com/mountandblade2bannerlord/articles/1764 . `ServeAsSoldier` remains optional for non-Soldier starting paths and **required at runtime for guided Soldier enlistment**.
- **Realistic Battle Mod (RBM)** https://www.nexusmods.com/mountandblade2bannerlord/mods/791 . Enable Combat/AI/Stance/Tournament, turn Campaign off initially. Detailed Auto Resolve off in first tests. Manual: https://fellow93.github.io/RealisticBattleProject/getting-started/ .
- **RTS Camera + Command System** https://www.nexusmods.com/mountandblade2bannerlord/mods/355 . Official author lists free camera, orders and save-state neutrality.
- **Immersive Battlefields** https://www.nexusmods.com/mountandblade2bannerlord/mods/4633 . Sergeants and horn orders. Introduce after SaS + RBM tests. Do not stack multiple battle voice mods in the first profile.
- **Banner Fix + Raise Your Banner** https://www.nexusmods.com/mountandblade2bannerlord/mods/3253 . Add the Banner Fix dependency before Raise Your Banner.
- **Realistic Blood** https://www.nexusmods.com/mountandblade2bannerlord/mods/8328 . Visual-only candidate, test after base stability.

### Optional additions after a passing baseline

- **Better Pikes + Troops Prefer Spears**: https://www.nexusmods.com/mountandblade2bannerlord/mods/ . Require an exact compatible pair, verify RBM integration and formation behavior before adopting.
- **Raise Your Torch**: evening siege/battle lighting; requires a current 1.5.7 asset and scene-loading test.
- **Battle Atmosphere - Native**: https://www.nexusmods.com/mountandblade2bannerlord/mods/13646 . Alternative battle voice suite; choose *either* this *or* Immersive Battlefields plus its horns during the first audio test. Do not treat simultaneous shout mods as proven sound compatibility.
- **Procedural Scenes**: separate terrain/navmesh experiment, never part of initial certification.
- **Hold the Line / Battle Instinct / Smarter Battle AI / Siege AI Fix**: competing formation/AI authorities. Keep disabled alongside RBM AI in the initial profile.
- **Realistic Auto Resolve** and **Realistic Combat Mod**: do not pair with overlapping RBM systems.

## Provisional launcher order

1. Harmony
2. ButterLib
3. UIExtenderEx
4. Mod Configuration Menu
5. Official game modules, native supported order (including `NavalDLC` only on the separate War Sails profile)
6. ServeAsSoldier
7. RTSCamera
8. RTSCamera.CommandSystem
9. RBM
10. ImmersiveBattlefields
11. BannerFix
12. RaiseYourBanner
13. RealisticBlood
14. CalradiaUnified

This is a **hypothesis for testing**. An installed mod's actual `SubModule.xml` dependencies override this outline. The order of SaS, RTS and RBM must be reconciled with their installed manifests. War Sails adds RBM_WS after its actual dependencies and requires distinct checks.

## Test profiles and evidence gates

- **B0**: Official + the four BUTR frameworks + CU branch. Start and load a saved independent campaign.
- **B1**: B0 + SaS. Test guided Soldier Recruit, Sergeant, Captain separately. Verify actual selected rank and lord. If SaS cannot offer the rank, mark BLOCKED, not PASS.
- **B2**: B1 + RBM. Compare melee damage, troop behavior, healing, equipment and actual battle result. RBM Campaign off.
- **B3**: B2 + RTS Camera and Command System. Exercise spectator transfer and formation orders after injury.
- **B4**: B3 + Immersive Battlefields. Confirm war horns and sergeant orders do not break SaS controlled commands.
- **B5**: B4 + banner pair. Confirm correct identities for the host army and player party.
- **B6**: B5 + Realistic Blood. Test arrow hits and large-battle frame pacing.
- **B7**: Optional pike, terrain, voice and torch add-ons, one per test. Never infer compatibility from the B6 result.

### Mandatory battle and save cases

1. **Normal campaign battle:** player party wins a field battle. Exactly one PlayerParty receipt; no changes to cash, renown or relations from CU.
2. **Normal defeat:** one Defeat receipt. Player capture/mission transitions remain native.
3. **Unresolved engagement/retreat:** record only native state, never a made-up victory.
4. **Siege/raid/hideout:** correct native battle type and location ID where present.
5. **Auto-resolve:** one party-level receipt, no personal service claim.
6. **Enlisted SaS battle:** host party involvement verified at end and exact SaS native lord recorded; outcome matches the host's side.
7. **Enlisted but outside host party:** no fabricated SaS battle receipt.
8. **Soldier wounded or host lord captured/killed:** battle exit, party transfer, enlistment status and ability to save survive.
9. **Promotion/demotion, wages, duty change, desertion/discharge:** SaS is authoritative. CU never awards rank or pays a second wage.
10. **Map event callback repeated:** no duplicate receipt.
11. **Save/reload:** no duplicate entries, full ledger retained.
12. **Old save without battle key:** empty journal, no failure.
13. **Long campaign:** 40+ distinct battles, ledger limited to 32 recent outcomes.
14. **RBM posture and RTS camera:** no mission crashes on entry, pause, formation order, injury and exit.
15. **Siege with atmosphere/banner/audio layers:** correct command voices and standards, no animation or spawn failure.
16. **60-minute stress and exit/restart:** inspect crashes, drift, FPS, exception and Save/load regression.
17. **War Sails:** separate land-to-ship, naval battle, shipboard service, shore return, campaign persistence.
18. **Isolation control:** reproduce the same case with and without each optional mod to identify ownership.

## Result semantics

Policy tests = deterministic model behavior. CI success = compiler and independent test evidence. Neither is equivalent to an installed SaS/RBM/RTS/ImmersiveBattlefields session. A complete stack can only be called **GAME_TESTED** after the actual in-game profile passes startup, feature, save, performance and regression gates.

Do not distribute third-party DLLs or art inside the CU module ZIP. SaS, RBM and every visual mod remain external installs. Never bundle or silently change their licenses.

## Follow-on milestone after live evidence

Extend proven receipts to named Hero death/capture, household loss and patron service evaluation using native campaign APIs with their own event witnesses, preserving per-feature save migration. Only adjust political standing after an actual game transaction with a verified source. A battle victory alone does **not** grant a title, land, rank, vassal status, SaS promotion or royal relationship.
