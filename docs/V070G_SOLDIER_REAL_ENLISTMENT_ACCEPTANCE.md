# v0.70g | Soldier: real lord enlistment, Recruit / Sergeant / Captain

**Date:** 21 November 2026. **Status:** new-game source candidate and native verification wiring, native playthrough pending. Draft PR #93; keep unmerged and do not move latest verified installer pin.

## Fourth reviewed starting-life option

**Soldier** is no longer a newly created mercenary-clan captain. The player is an individual serving in an actual existing lord's army. The requested career entries are **Recruit**, **Sergeant** and **Captain**. Native Serve as Soldier supports seven real ranks, with stable public API IDs:

`private`, `soldier`, `corporal`, `sergeant`, `veteran`, `captain`, `champion`.

Requested `Recruit` maps to native `private` (rank 1), `Sergeant` to native `sergeant` (rank 4) and `Captain` to native `captain` (rank 6). For career progress display, native ranks 1–3 are Recruit stage, 4–5 Sergeant stage, 6–7 Captain stage. The original seven native rank IDs remain stored and shown. Ranks progress through Serve as Soldier's **actual service XP and promotions**. There is no invented CU award, rank switch or daily wage.

### Native enlistment source

- Official developer contract: https://www.nexusmods.com/mountandblade2bannerlord/articles/1764
- Supported v1.5.7 SaS mod page: https://www.nexusmods.com/mountandblade2bannerlord/mods/3242

The integer `Rank` field's 0-based versus 1-based indexing is not established by the public v1 documentation. The stable `RankId` string is the authoritative rank identity. Do not reject a true enlistment on a guessed numeric index.

The SaS API v1 is **read-only** for starting or changing service. It exposes `IsPlayerEnlisted`, `CurrentLord`, `HostParty`, `GetSnapshot()`, `RankId`, `Rank`, `DailyWage`, `DaysServed`, `DutyId`, `TermDaysLeft`, `IsPlayerInsideHostParty`. Its own **start-enlisted screen** at the end of character creation lets the player select a kingdom and a troop grade, leading to real service under a native lord. SaS confirms rank, wage, lord, host and term, and controls the actual party, battles, training, equipment, pay, promotions and discharge. CU does **not** infer these from role labels or from a political mercenary contract.

The public interface has **no exposed enlist-at-named-lord / grant-rank command**. CU may ask for an optional exact native lord Hero ID, but the SaS start-enlisted or lord dialogue is the actual authoritative action. CU verifies the actual resulting lord. An empty preferred lord ID accepts an eligible real service commander in the selected kingdom. A nonempty exact ID requires that **same** native Hero, not merely another lord from the same kingdom.

## Implemented new-game behavior in source

- `SandboxOriginProfile.SoldierGuidedStart`, false for old saves, and `SoldierEntryRank` default Recruit. Both serialize/parse/reset; explicit flags are never inferred from an old `Role=Soldier` save.
- The new guided Soldier maps to native **`StartType=default`** with an **independent, no-land player clan**. The old legacy Soldier path remains mapped to `mercenary`, preserving saves and tests. Requested `StartingKingdomId` is the service employer, **not** an automatic `Clan.Kingdom` affiliation.
- The creator shows one requested entry field with three choices, optional real commanding lord Hero ID and the selected employer kingdom. Player appearance, full attributes, skills and traits remain native; spouse, children and siblings remain optional House choices.
- No fiefs, political grant, court, free household officer, troop subsidy, artificially recorded native marriage or fictional wages. Artificial ancestry anchors remain historical/nonactive for Soldier instead of becoming extra active parents or lords.
- `SandboxSaSBridge.Read()` reads actual SaS Rank/RankId, live lord, lord kingdom, host-party leadership and wage. The bridge does **not** call a private SaS method, patch the mod, fake an enlistment, or rewrite troop rosters.
- `SandboxStartingLifePreflight` validates the independent native start, real SaS service, actual lord host party and intended kingdom/entry rank before any family or resource mutation.
- `SandboxServicePatron.TryAssign` for guided Soldier returns the **actual verified native lord**. It skips the earlier +20 artificial personal relation action and does not declare lordship.
- `SandboxOriginBehavior.TryApplyOnce` defers the one-shot grant barrier if SaS's start-enlisted UI has not completed. `soldierPendingStartSaved` and a save-backed exact serialized new-game request prevent stale external .ini data from rewriting a pending save. Each day, the system can detect the provider's new authoritative service state. It **never marks a pending or wrong-rank Soldier start as PASS**.
- The final start contract checks real SaS again. Only a verified match sets `SandboxServiceCareerState` using the observed real rank/duty/wage and `SandboxRoleEconomyState` using the internal nonselectable `EnlistedSoldier` wage authority. A false/absent SaS snapshot cannot become a native soldier salary or promotion.

## Real gameplay caveats

1. A usable Soldier start **requires an installed compatible SaS v1.5.7 provider**. CU does not bundle its DLL and does not silently add it to the general module requirements. The API v1 observes service; CU does not possess an authorized direct enlist/promote method. The SaS native start-enlisted screen must complete and present the requested grade for a verified start. If the provider is unavailable, starting a new Soldier path is rejected rather than recategorized as Mercenary.
2. The exact timing of SaS's start-enlisted UI relative to CU's native phase-9 callback **has not been tested in-game**. The saved pending state is designed to wait for SaS's authoritative snapshot. If the menu is refused or no rank matches, CU never claims success. We do not assert that SaS guarantees start at an exact selected Hero when the SaS screen may select its own commander.
3. SaS has a **seven-rank career**, not three artificially restricted game ranks. CU presents three initial *requested* grades and groups real subsequent promotions into three career stages. SaS can award intermediate ranks such as Corporal and Veteran.
4. SaS's NPC-party and personal-retinue management may interact with CU's optional family creation, native character stats UI or companion dismissal. Real integration tests must exercise newborn children, active spouses, injured lords, mission transitions, battles, sea battles if War Sails is active, discharge and save/reload. Do not claim this works from compilation.
5. Old profiles with `Role=Soldier` and without `SoldierGuidedStart` retain historical mercenary semantics. Existing `Recruit` profiles retain their independent SaS application role. Do not silently migrate either.

## Acceptance plan

- Launch Soldier/Recruit with SaS v1.5.7 present: after CU's native appearance/stats and any House choices, confirm SaS's real start-enlisted UI. Select the requested kingdom and a rank-1 native troop. The actually resulting lord/party/clan/kingdom, rank ID `private` and actual wages must appear in the diagnostic receipt. No native mercenary-clan service or starting fief. `sandbox_start_contract=result=PASS` only on observed full agreement.
- Repeat as Sergeant with native rank ID `sergeant` (4) and Captain with native ID `captain` (6). If the SaS menu cannot offer a target grade, mark this case **BLOCKED**, not "rank awarded".
- Enter a precise actual lord ID and test a mismatched native lord: must fail instead of accepting another lord. Empty ID can accept any eligible actual commander from the selected kingdom.
- Track real SaS XP, pay, weapon kit and rank progress. Verify Corporal/Sergeant/Veteran/Captain/Champion milestones are **observed**, not CU-awarded. Capture both promotions and demotions; ensure no false wages.
- Save in pending state, reload, finish enlistment and check exact same requested rank/kingdom. Save again while enlisted, then test lord death, capture, party destruction, discharge, faction switch, rebellion, mission entry and exit. No duplicate relatives or fake rank state.
- Check previous v0.70b Wanderer and v0.70c Outlaw starts, v0.70d Mercenary native contract, and old legacy Soldier saves.

## Versioned test boundary

Unit tests cover exact rank mapping, genuine lord and host-party checks, independent status, requested-vs-observed rank, promotion receipts and profile serialization/reset. CI only proves source and pinned reference compilation. Native Gate C/D/E tests are still required. No default installer pin change and no merge before the live tests pass.
