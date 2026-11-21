# v0.70o | Full seven-path native gameplay acceptance gate

**Scope:** draft PR #93, source branch `audit/v069e-current-gameplay-and-visual-acceptance`, stacked on draft PR #92. A pinned-reference DLL compile and model-policy simulation certify source behavior only. They cannot prove an in-game wedding, governor, member roster, native status, UI, battle, death or save/reload.

## Read-only auditor

`scripts/audit_v070_seven_live_start_evidence.py` is a strict seven-session runtime-audit utility, not a launcher. It reads live `events.csv` from Bannerlord's CalradiaUnified diagnostics and per-path evidence files, and emits a machine-readable JSON report. It never edits a save, game process, mod installation or native Hero.

Usage on a machine with actual playthrough records:

```powershell
python scripts/audit_v070_seven_live_start_evidence.py --events "$env:LOCALAPPDATA\CalradiaUnified\Diagnostics\events.csv" --evidence-dir ".\native-acceptance\v070" --report ".\native-acceptance\v070\seven-path-report.json"
```

Each role requires: a distinct actual native new-game session with final `sandbox_start_contract` result `PASS` and role-specific runtime effects, a `<role>.json` post-quit/reload observation tied to that start's exact `session_id`, and a `<role>.png` screenshot from live native Bannerlord. The reload observation records verified original Hero/clan IDs, the actual native status after reload, and real family and authority/service links. The audit checks missing inputs and fails with exit status 2, not a misleading PASS. A single local CSV does not certify live play or reload by itself.

The policy-only test `scripts/test_v070o_seven_game_acceptance_failclosed.py` deliberately constructs all seven fake `sandbox_start_contract=PASS` rows and confirms they remain **BLOCKED** with no genuine native event chain and reload/screenshot evidence. CI success on this test is evidence of a working rejection rule, not evidence that Bannerlord was launched.

## Acceptance matrix

**Wanderer:** actual landless native start, no clan/estate/kingdom invented, one real companion who remains in native party, native companion wage accounting, spouse/child and save/reload behavior only when selected. Refuse a second fabricated companion.

**Outlaw / Bandit:** actual independent, non-royal band status, native bandit lieutenant, paid real native band troops with food/wages, fourteen-day payroll and real losses, absence of counterfeit noble ranks, native recruitment source and save/reload.

**Mercenary:** actual kingdom mercenary service and player-led company, real native contract offers, agreed payment and bond, service exit, native economic security, no invented command. Negative tests for unfunded offers and low-pay consent.

**Soldier:** genuine external Serve as Soldier provider and real enlistment. Test Recruit, Sergeant and Captain requests without confusing requests with granted ranks. A pending/unsupported provider is **BLOCKED**, never a PASS. Check lord's actual mobile party, native rank, pay, equipment and resignation across reload.

**Knight:** independent sworn house with a real personal patron, recorded oath duties and actual relation effects, 30-day muster, patron capture/death/succession, release/break and save/reload. The younger-sibling alternative remains **BLOCKED** until the player can join the true native family clan without breaking party ownership or save references. A generated pedigree is insufficient proof of native clan membership.

**Lord:** real native vassal, true player-led noble clan and fief, mandatory spouse and at least one native child, spouse's real noble birth clan with two married native parents, accurate sibling seniority, real culture-trained Master of Arms and Knight Heroes, native estate and in-laws across reload. Inheritance preference is a recorded claim only, not a fief transfer.

**King:** real native King start, native ruling clan, royal estate, mandatory spouse and child, real Marshal, Royal Minister and **one Kingsguard Hero**, all native Heroes with distinct identities and same culture. Royal Minister must actually govern the verified owned royal court seat. Convening the court requires the King and two travel officers physically at that settlement. No Kingsguard troop unit, garrison transfer, recruits, custom gear template, or additional guard payroll. Verify kingdom rulership, court members, child claims, governor, actual native office, battle survival and save/reload. Household heir priority cannot appoint a native king or regent.

## Release-level requirements

All seven require native new-game and actual save/quit/reload, correct game UI display, identical native Hero IDs and clan identifiers after reload, party/garrison ownership and native politics unchanged except verified real actions, no exceptions, and matching installed DLL SHA-256. The full historical compatibility matrix must keep the 13 legacy role identifiers parseable. No profile flag from a new campaign may silently change the existing campaign after reload.

For strict release readiness, test all valid subtypes and negative cases separately, including three Soldier ranks and the currently blocked Knight younger-sibling native-clan branch. A passing seven-role audit alone does not unblock those missing subtype gameplay actions.

## Current status, 21 November 2026

This connected repository environment has source, policy tests, reference-assembly CI, diagnostic scripts and simulation reports. It does **not** have a running native Bannerlord process, real player screenshots, installed-game save/quit/reload records or seven separate live native playthroughs. The **live native acceptance gate remains BLOCKED**, even if the pinned compile succeeds. Do not merge PR #93, update the installed artifact pin, or call an unplayed test candidate a live-verified release.

The King's bodyguard is deliberately **one real Kingsguard Hero companion**, not a troop unit.

## Pinned v0.70n/o source-check result, 21 November 2026

- **Successful Windows workflow:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44893625785
- **Exact compiled source SHA:** `aead9f751e44d945c096a8eb2a085d0c8811ff28`
- **Pure C# gameplay policy smoke:** **1,062 passed**
- **Seven-path fail-closed audit regression:** PASS, proving that source-only success is never promoted to live native acceptance
- **Kingsguard one-Hero, actual royal council and minister governor source regression:** PASS
- **Deterministic audit simulator:** **210,240 modeled days, 1,159,206 assertions and 7,488 checkpoints**
- **Bannerlord pinned reference build:** **0 warnings, 0 errors**
- **Compiled DLL SHA-256:** `424dccba85b35f8ef798cc1829eae2ba4893be03e2005b27a160b5f90abbf870`
- **Game-untested build candidate:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44893625785/artifacts/14703052915
- **True native gameplay result:** **BLOCKED, 0 of 7 live starts verified.** Source/modeled acceptance is not an actual seven-start playthrough. Save/reload, all cultures, real diplomatic effects and native court presentation remain unverified.

## v0.70p | Native-evidence integrity correction (source-audit only)

The v0.70o auditor was too permissive about partial journal matches. It treated any substring from an accepted-marker list as success, rather than a complete exact receipt. Two legitimate native event formats were also misidentified: the Knight oath logs `result=oath_recorded` and a verified Serve as Soldier enlistment logs `result=external_enlistment_verified|party_confirmed=True`. Neither action normally emits `result=PASS` for those two events.

The stricter auditor now checks exact key/value pairs, refuses duplicate or invalid receipt keys, uses each context's latest event, and refuses a stale start PASS superseded by a later FAIL. Each of the seven accepted paths must have a different session ID. A session with a recorded exception is marked for review. PNG evidence must have a plausible PNG signature, IHDR and dimensions. User-entered `"False"` strings no longer pass boolean reload checks. These checks validate evidence structure. A fixture or hand-authored image **cannot prove** native Bannerlord execution.

**Royal court and single Kingsguard Hero evidence:** The King's post-reload `king.json` must preserve exact IDs from the real `cu_royal_court_native_commission` event: `royal_seat_id_after_reload`, `marshal_hero_id_after_reload`, `royal_minister_hero_id_after_reload`, and `kingsguard_hero_id_after_reload`. The native governor-action event must name that same minister and seat. Record **boolean true** for `minister_native_governor_after_reload`, `marshal_in_main_party_after_reload`, and `kingsguard_in_main_party_after_reload`. To cover the requested single-Hero equipment and combat acceptance, record true only after direct gameplay observation for `kingsguard_native_equipment_observed`, `kingsguard_entered_native_combat`, and `kingsguard_post_battle_identity_confirmed`. Attach both `king.png` (native court UI) and `king_kingsguard_battle.png` (actual Bannerlord combat scene). The audit compares source event IDs to those observations and refuses missing or inconsistent fields. It does not create a troop formation or give the Kingsguard special bodyguard AI.

**Live King test:** Load a new guided King game using the exact candidate DLL. Confirm the king's true ruling clan and player-owned court seat. Find the Royal Minister on that settlement's governor screen, then enter the same town/castle with Marshal and one Kingsguard Hero. Convene and review the court. Enter a native battle with the Kingsguard, inspect culturally appropriate equipment and the same Hero's survival/status after combat, save, exit fully, reload, inspect the same officer IDs, governor and roster, and fill only fields personally observed. Save the two unedited screenshots and the session's diagnostic `events.csv`. This covers Q1 and Q2 without treating source-only tests as live play.

**Seven-path test:** Execute a distinct new campaign per start. For Soldier, verify the external enlistment action and actual host-party status; a pending request or ending service is not proof. For Knight, verify the actual personal oath. The separate younger-sibling native-clan transfer remains blocked. Repeat native family, ruler, fief, party and saved identity checks at reload for every path.

The regression suite creates synthetic fixtures to test both acceptance and rejection logic. They are **not actual playthrough results**. Until genuine native files, screenshots and game sessions exist, all seven live starts remain BLOCKED. The feature freeze, Kingsguard one-Hero design, old installer pin and draft PR #93 remain unchanged.

## v0.70q | Player-reported family/companion regression in three actual v0.70n sessions

**Source of evidence:** 21 November 2026 player-supplied closed-session ZIP, one diagnostic session with three distinct native new-game attempts. The exact installed v0.70n game DLL was SHA-256 `424dccba85b35f8ef798cc1829eae2ba4893be03e2005b27a160b5f90abbf870`. The private diagnostic ZIP and game paths remain off GitHub.

- **Knight (Sworn House):** World and House committed `spouse=False`, `children=0`, `retinue=FamilyOnly`. The native independent start succeeded, and the mod correctly created no requested family and no custom retinue. The creator previously forced a blank Knight default, even though a same-culture Squire exists in the native retinue factory. That was a poor starting preset, not a native creation failure.
- **Lord:** The user committed `spouse=True`, `children=2`, `siblings=2`, `retinue=RoleBased`. Pinned native start: `actual_type=vassal`, `kingdom=vlandia`, `fiefs=3`, `ruling=False`. The source gate returned `not_active_noble_house` and prevented the family and retinue factory from running.
- **King:** The user committed `spouse=True`, `children=1`, `siblings=1`, `retinue=RoleBased`. Pinned native start: `actual_type=king`, `kingdom=vlandia`, `fiefs=6`, `ruling=True`. The source gate returned `royal_house_ineligible` and prevented any custom family or court officer.

**Root-cause analysis:** The new King/Lord native preflight and later final-state check incorrectly required `Hero.IsLord` at the phase-9 character-creation boundary and also used `Clan.IsMinorFaction` as proof of noble political authority. These properties are not equivalent to the actual native kingdom/clan/ruling/party/fief contract. This redundant veto was introduced in the new v0.70j/l gates after the earlier working family pathway. The user logs prove the combined noble-house expression returned false, but did not record which particular flag failed. The v0.70q implementation therefore removes the unreliable status-bit veto **without guessing which one was false** and adds per-flag diagnostics for the next native session.

**Repair:** A shared pure `SandboxNativeNobleHandoffPolicy` checks the native `king` or `vassal` start, existing real kingdom, player native clan and party leadership, actual ruler/vassal relationship, non-mercenary/non-bandit/non-rebel state, and at least one real owned holding. The role-specific policy still rejects wrong kingdom, eliminated clan, vassal pretending to be King, false fiefs and false sovereignty. The preflight and final gate now both use this same evidence instead of `Hero.IsLord` or `IsMinorFaction`. The original native marriage, child parentage, noble in-law, culturally generated retinue and SaveGame checks remain unchanged.

**Knight:** The new Sworn House proposes a spouse, one child and a **single native Hero Squire** by default. House still lets the player opt out of marriage, and then clears the now-hidden child count. The optional `FamilyOnly` selection still means no officers. No fictional fief, automatic sworn wages, kingdom membership or new troop unit is granted. King still retains only one Kingsguard Hero.

**King governor seat:** If the preferred royal seat has an existing governor, select another unbesieged and vacant player-owned fief. Do not evict or replace an incumbent. If none exists, native ministry remains blocked. A court seat is not an official Bannerlord capital rewrite.

**Acceptance status:** This is a repair candidate, not a claim that the user's three failing starts now pass in the actual game. To close the issue, the next exact compiled candidate must show `cu_starting_life_preflight result=PASS` for both Lord and King, successful actual marriage/children/companion events, and the same people after save/quit/reload. The Knight House editor must show its new visible defaults and create a real Squire and the chosen family. The existing gate C through E remains required, and PR #93 stays draft.
