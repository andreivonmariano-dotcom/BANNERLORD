# v0.70f decision record: risk acceptance, protected exit bond, renegotiation

**Approved by user:** all three questions Q1, Q2 and Q3, 21 November 2026. Design and pure source policy only; no native contract offer/treasury integration is implemented.

This decision amends [v0.70e mercenary solvency and negotiation RFC](V070E_MERCENARY_SOLVENCY_NEGOTIATION_RFC.md), without changing historical `Soldier` or the old already-contracted v0.70d Mercenary native startup.

## Q1 | Explicitly accepting a loss-making offer

**Decision: YES.** The player may take a contract with guaranteed daily payment below the actual daily company operating cost, but it requires **two separate affirmative actions**: click Accept, then acknowledge the exact daily and projected loss. The default action is Decline, with Negotiate offered.

The initial screen must state native party wages, provisions and equipment costs, promised guaranteed pay, daily deficit, selected contract term and projected total deficit. All numbers are refreshed from live company/offer state immediately before confirmation. If the contract changes or the player grows their company, invalidate prior acknowledgment. Income from uncertain battle victories, loot and influence must not be included in *guaranteed* coverage.

Use `SandboxMercenarySolvencyPolicy.AcceptanceDecision` for **pure validation**, `ProjectedDeficit` for warnings. The current test cases check that a `150d` cost/`130d` offer with a 30-day term displays `20d` daily loss and `600d` total expected loss.

**Never waive unfunded security because the player opted into risk.** Advance and protected exit bond must be genuinely funded before a binding contract. A player who wants to accept a lower but still profitable offer below the preferred 25% margin may do so without a second loss acknowledgment.

## Q2 | Protected, employer-funded exit bond

**Decision: YES.** The exit bond equals **21 days of the operating cost at signature**, plus any mutually agreed funded revision. It must consist of actual employer-backed money in a **separate, campaign-save-backed escrow ledger**. Do not add the bond to `Hero.Gold` while the company is employed and do not count it as spendable payroll or free starting money.

The bond is releasable only after an actual, validated native transition: normal term completion or employer dismissal without cause. A player-initiated early breach forfeits the unearned bond under accepted terms. If the native game lacks contract-expiry cause data, prevent release until there is an unambiguous saved event, rather than inventing an outcome.

Protect against replays with a stable contract ID, escrow deposit transaction ID, employer debit receipt, once-only exit settlement ID, recorded gold delta and save/reload idempotence. Company bankruptcy or employer insolvency is not corrected through a money spawn. Escrow held in an unaudited variable is **not** confirmed funding.

`SandboxMercenarySolvencyPolicy.ExitBondReleaseDecision` returns a **pure eligibility status** only. It never creates or moves native money.

## Q3 | Company headcount, wage ceilings and explicit renegotiation

**Decision: YES.** Each signed contract specifies two **independent coverage ceilings**: (a) the total paid party headcount, and (b) actual native daily covered wages. The count is derived from real party roster, including any wage-charged heroes, and excludes prisoners. Exact v1.5.7 roster model must be inspected before binding it to native code. Neither extra troops nor upgrades automatically change an accepted contract.

Every roster change or wage recomputation compares the current native numbers to the signed ceilings. Growing the troop count beyond the agreed cap triggers `renegotiate_headcount`. Upgrading existing soldiers into higher wage bands triggers `renegotiate_wages` **even when the headcount is unchanged**. Both exceeded gives `renegotiate_headcount_and_wages`.

Triggering renegotiation merely opens a proposed amendment. The employer and player must *both* accept the new term, and any increased guaranteed income/advance/exit bond must be verified as funded. Then and only then can a real native contract amendment be attempted. The old terms remain active if the employer declines; the player must pay excess costs, trim the force or leave under applicable contract consequences. No unilateral government subsidies or automatic binding amendments.

`SandboxMercenarySolvencyPolicy.CoverageReview` and `RevisionDecision` only evaluate status. They do not patch native payment code or silently sign a contract.

## UI example

`Vlandia | 60 days | 10 paid members covered | Native wage ceiling 150d/day`

`Current company: 11 paid members | 175d/day native wages`

`Change detected: 1 extra member and 25d/day over coverage`

Actions: **Request updated contract**, **Keep old contract and self-fund excess**, **Reduce company**, **Decline/leave under contract terms**.

No automatic replacement of existing signed terms. The player must confirm the new price, term, headcount and escrow before the employer-backed native contract is revised.

## Evidence status and implementation sequence

- **Implemented now:** pure arithmetic, explicit risk-choice classification, bond-release eligibility and roster-change classification, C# policy tests, source checks and this decision record.
- **Not implemented:** native independent pre-contract game start, actual offer screen, consenting UI, employer financial authority, funded escrow/ledger, contractual daily pay/reconciliation, native change-in-term action, once-only exit transfer, live headcount/wage observation and in-game acceptance. No claim of a working playable negotiation mechanic.
- **Next engineering stage:** inspect pinned v1.5.7 API, implement independent initial company and 30-day starting runway, offer/accept/decline with native kingdom transitions, then save-backed funded money flows and automatic change-detection prompts. Test normal expiry, betrayal, bankruptcy, loss acceptance, wage-upgrade-only renegotiation, signed revision, refusal, employer switching and repeated save/reload.
- **Release:** the candidate stays in draft PR #93. Do not merge, repoint installer, call escrow secured or mark gameplay PASS before a real game session verifies all payment and contract claims.
