# v0.70e RFC | Mercenary company solvency, contract choice and exit runway

**21 November 2026. Design and pure-policy implementation only. Not native gameplay.** Continues `V070D_MERCENARY_NATIVE_COMPANY_AUDIT.md` on draft PR #93. Do not merge or move the pinned installer.

## Player promise

A mercenary company must be able to survive initial unemployment, decline a contract that cannot sustain its native payroll, negotiate better terms, remain solvent under an actually funded contract, and leave with the **already-funded** savings to seek its next employer. Do not disable Bannerlord desertion, duplicate vanilla wages, create infinite treasury denars, make the player a landed lord, or invent an employer's consent.

## Source facts and blockers

- v0.70d **currently starts already contracted**: `SandboxRoleConsistencyPolicy.NativeType("Mercenary") == "mercenary"`, `SandboxNativeStartOptions.TryPrepare` requires `StartingKingdomId`, and phase-9 `SandboxStartingLifePreflight` rejects a kingdomless player. That is incompatible with the newly requested **initial uncontracted search**. A future implementation must migrate **new** Mercenary games to a genuinely independent native default start and perform a real `ChangeKingdomAction.ApplyByJoinFactionAsMercenary` after player acceptance. **Never silently reinterpret legacy Soldier or existing saved Mercenary contracts.**
- `SandboxMercenaryStartPolicy.Validate` currently checks real `Clan.IsUnderMercenaryService`, kingdom, zero fiefs and player party/clan leadership. Keep it for *contracted* state; add separate real independent preflight for *Uncontracted* state.
- Native `SandboxRoleAffiliations.TryApply` has a proven real mercenary join action used by older Soldier starts, with fixed example `CampaignTime.DaysFromNow(90f), 50`. Those example terms are NOT a negotiated pay offer. Do not present the call's 50 as guaranteed denars/day, and do not reuse it until the pinned game's API economics are inspected.
- Existing project policy `SandboxRoleEconomyPolicy` explicitly treats native mercenary cash and troop wages as authoritative. There is **no current contract-offer screen, money escrow, exit bond, offer rejection hook or wage guarantee** in v0.70d. This RFC designs them, not claims they are installed.
- Existing Outlaw's 14-day reserve and Wanderer's daily companion reimbursement must remain distinct, role-isolated mechanics.

## Starting the game: Uncontracted Company

- Start as a player-led, non-noble, independent clan with **no employer or fief**. Keep the player's real native character customization and optional family. Do not force marriage or artificial noble ancestors.
- Initially 6–10 *ordinary low-tier mercenary/cultural soldiers*, if they can be found as real game units. Cap initial party by actual native `PartySizeLimit`; **never create an unsupported regular troop or universal free companion**.
- **30-day cash runway** computed from actual native daily party wages plus conservative food and maintenance budgets. Starting treasury top-up = `max(0, ceil(30 × DailyOperatingCost × 1.10) − ActualNativePlayerGold)`. One grant only, verified in actual Hero gold and logged.
- **14 days of actual edible inventory** (mix can depend on culture, settlement and market availability), computed from the installed native food-consumption model. Do not hardcode 14 grain as 14 days of rations. Record the actual calculated days of food and all roster counts.
- Since the player's gold is real game money and spendable, 30 days is a **starting financial condition**. Purchases, injuries, troop growth, high prices, starvation, poor morale and unforeseen losses can shorten runway. No claim of desertion immunity.
- Put a “Find Contract” entry in the real diplomacy/kingdom or lord encounter UI, never in a fabricated menu that does not control native affiliation.

## Live negotiation: preview before acceptance

Present real offer terms, and **do not join a kingdom until the player clicks Accept**:

- Employer: exact native kingdom/ruler, known available funds, diplomatic obligations, confirmed duration / notice terms.
- Company: actual party roster, covered troop cap, player command rights, real native daily **TotalWage**, food replenishment estimate, small equipment/medical provision and replacement allowance.
- Guaranteed pay: employer-backed, payable daily or via verified escrow. Native battle loot/influence awards are uncertain bonuses, **not** counted toward guaranteed operating coverage.
- Default required guarantee: **125% of projected daily operating cost** (a 25% operating margin over costs). Company may select no-loss minimum 100%, prudent 125%, or premium 150%.
- **Three meaningful actions:** Accept at agreed rate, Negotiate better wages/advance/covered headcount, Decline without joining. Offer below 100% receives “Loss-making” status; player may explicitly override the warning at their own risk, never an automatic accept.
- Retainer: **7 days' operating cost** paid in advance as an actual employer-funded wage buffer.
- **21-day exit bond**: held in a save-backed segregated company escrow from actual employer money, payable on normal completion or employer termination without cause. This is not a fictional immediate gold grant, and it cannot be counted in spendable treasury during service. The contract is not “secured” if escrow funding fails. Employer bankruptcy must cause a declined/failed offer or recorded arrears, never conjure denars.
- Covered maximum headcount is explicit. On troop growth or upgrades, recompute actual native wages and offer renegotiation. An unexpectedly enlarged roster is NOT silently guaranteed as covered by yesterday's contract.

## During a contract

- Keep Bannerlord's native troop pay and desertion active. Reserve liquidity should cover brief payroll delays, not remove consequences.
- **No double payment**: reconcile actual contract-specific native mercenary receipts and guaranteed payment. Pay **only the verified shortfall** from a funded employer deposit/transfer. Never infer mercenary earnings from the player's total gold delta (it includes trade, loot, expenses).
- If the pinned game exposes no reliable contract-payment transaction source, the implementation must first establish a single payment authority before enabling guaranteed pay. Do not run an unconditional daily top-up in parallel with native mercenary pay.
- Record wage liability, food stock, projected runway, arrears and employer solvency in an actual campaign save. Transactions must be idempotent across daily ticks, save/reload and campaign changes.
- After the first unpaid day show arrears. With a prolonged breach, allow Suspend Service, Demand Payment, or Leave Contract through a **native** kingdom-leaving action with consequences appropriate to employer default. No automatic party teleport, forced lordship, fake diplomacy or global wage patch.

## Contract end / next search

- On legitimate completion or employer-initiated termination without cause, release the existing funded **21-day exit bond** into actual player funds exactly once. Verify a corresponding escrow debit and player credit. The company can then seek another employer without instant wage insolvency.
- The objective is **at least 21 days' real operating reserve and 14 days' food** after exit. Actual food and gold must be checked. The security bond covers payroll, but cannot guarantee food prices, casualties or player spending.
- If the player breaks a contract prematurely, disclose that the bond may be forfeited. Earned unpaid wages remain a collectible debt only if they are actually funded/owed in the implemented native economy.
- Before accepting a new contract, compare against present company roster and operating costs. No stale guaranteed wage from the previous kingdom, no duplicate starting grant.

## Proposed UI

Contract board:
- `Uncontracted | Company size 9 | Treasury 4,950d | Costs 150d/day | Runway ~33d`
- `Offer from Vlandia | 60 days | Guaranteed 188d/day | 125% coverage`
- `Upfront wage buffer 1,050d | Exit bond 3,150d secured`
- Buttons: **Accept contract**, **Negotiate terms**, **Decline**.
- Warning: `Offered 130d/day < 150d/day operating cost: loses 20d/day`. Two explicit choices: **Decline** or **Accept loss-making contract**; no automatic agreement.
- Active service page: `Native wages | Guaranteed shortfall received | Escrow and runway | Troops covered | Departure bond | Terms`.

### Worked economy example (illustrative, not a live native quote)

9-person company native wages 120d/day, food 20d/day, equipment/medical/replacements budget 10d/day. Total cost **150d/day**.

- Guaranteed minimum for default 25% margin = `ceil(150 × 1.25) = 188d/day`; projected margin **38d/day**.
- Recommended initial funds = `ceil(150 × 30 × 1.10) = 4,950d`, plus 14 genuine days of food inventory.
- Payroll advance = `7 × 150 = 1,050d`. This is prepaid earned service, **not additional pay** on top of 188d/day.
- Secured exit bond = `21 × 150 = 3,150d`, escrowed from employer funds and disbursed at normal exit.
- 130d/day alternative = a **20d/day deficit**, or **600d loss in 30 days** before loot. Display as loss-making with a real right to decline.

## Engineering plan and acceptance gates

1. Create pure overflow-safe integer `SandboxMercenarySolvencyPolicy` for the math, profit thresholds and offer classification. Unit test large/zero/negative inputs and 100%, 125%, 150% coverage. **Pure model is not a gameplay feature.**
2. **Native version inspection:** verify precise v1.5.7 event and action hooks for mercenary join/leave, native pay history, party wage, food consumption, kingdom payment authority, and treasury transfers. Do not invent API members.
3. New-game-only state machine: `Uncontracted -> Offer -> Accepted -> UnderContract -> Arrears -> Termination -> Uncontracted`. Persist original employer, accepted terms, unit caps, gold transfers, balances and once-only transaction IDs in the campaign save. Never rewrite an old v0.70d saved contracted Mercenary.
4. Start grant: real 30-day money and 14-day native consumable food, once only, no fake wages.
5. Native negotiation UI and funding: offer evaluation, decline, counter-offer, explicit acceptance, employer debit, escrow deposit and native join action. Require outcome verification and avoid claiming an atomic operation that Bannerlord cannot roll back.
6. Settlement and exit bond: distinguish already paid native mercenary income from any guaranteed shortfall. Fund arrears and bond from actual sources. Correct once-only save/reload and aborted-contract behavior.
7. 30-day out-of-contract simulation under no loot; 60-day contract with receipts; 21-day search after completion; deliberate low offer, bankrupt client, late payroll, larger company, native dismissal, company casualties, reload, and employer switching. **Only in-game traces certify real implementation**.

**Do not mark v0.70e as usable negotiation gameplay based on these policy rules or documentation.** No deployment, pin change or release without live native checks.
