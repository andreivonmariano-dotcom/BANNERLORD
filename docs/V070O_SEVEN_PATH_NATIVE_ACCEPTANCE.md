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
