# CU v0.74.0 | Native gameplay acceptance checklist
24 November 2026. Mandatory before release or installed-module replacement recommendation. This is an unexecuted test plan; CI or synthetic logs do not count as native evidence.

## Build and identity (Gate B)
- [ ] Record PR #99 exact head, checkout SHA (PR merge SHA if applicable), successful workflow ID, artifact ID and DLL SHA-256; require complete workflow success, zero compiler errors, review warnings.
- [ ] Pass static ownership/reachability gates, PolicySmoke, campaign and audit simulations, package and installer rejection regressions.
- [ ] Extract exactly one CalradiaUnified module; check SubModule.xml v0.74.0, one CalradiaUnified.dll, no retired pre-MCM/Origin prefabs, no third-party DLL or executable installer. Verify CU-BuildIdentity.json, actual DLL hash and source identity; do not install intermediate failed-workflow output.
- [ ] Preserve old module and saves separately. No existing save overwritten. Record actual game build 1.5.7.125802, official modules, all four required BUTR versions, enabled order and actual external binary hashes; if different, mark target mismatch.
- [ ] Run twice: official modules + four BUTR frameworks + CU control (Soldier provider-dependent tests require separate SaS-enabled profile), then exact 41-module baseline. Record any unavailable DLC/provider, never simulate its consent or status.

## Startup/creator (Gate C)
- [ ] Fresh launch, new standalone Sandbox, quit to menu and repeat. No errors, input traps or duplicate entry. MCM is sole active settings interface; verify all 20 held mutation flags remain off even with historical user override true. No hidden legacy settings menu.
- [ ] Each of seven starting careers selectable, no legacy extra paths. Native culture/face, age 18–80, House, stats/skills/traits, banner, clan/review/difficulty transitions and Back/Continue preserve choices. Verify at 1280x720, 1920x1080, 2560x1440 or equivalent; record screenshots and actual click paths. Invalid lineage/family/land blocks before irreversible native finalization. Verify spouse/children/siblings and actual clan/settlement ownership after start; failure must not replay on reload.
- [ ] Record separate character and campaign saves for each career, source identity, expected/observed results and diagnostic ZIP.

## Seven careers (Gate D)
| Career | Native test | Negative and duplication test |
|---|---|---|
| Wanderer | Independent clan, real town activity, optional one actual companion, native wages | No fabricated home, no CU wage refund, no extra sibling/companion on reload |
| Outlaw | Independent actual band and native/Fourberie crime evidence where applicable | Independent party alone not proof of criminality; no parallel crime AI or free troop duplication |
| Mercenary | Actual native contract and solvency, end contract and record another eligible calling | No duplicate pay, no independent authority during contract, no false rank |
| Soldier | Supported SaS public v1 snapshot, actual native lord/host party, verified grade/duty/wage, service end | Provider absent or incomplete: fail closed, no false discharge or CU payroll; ranks not granted by CU |
| Knight | Named real patron, correct personal oath holder, first audience, muster, service order and real escort | Repeated click or reload no duplicate favor; no extra battlefield favor; no successor using former oath, hearing or appointment |
| Lord | Native landed clan leader, verified family/heir claims, estate view and actual native officer | No automatic property transfer; direct CU garrison orders held, old live transfer gate remains BLOCKED until contract reconciled |
| King | Actual ruling clan, royal family and three real officer identities, native council meeting | No parallel treasury, political grants or garrison order; loss of crown reflected in present standing, no automatic succession |
- [ ] For all roles, verify recurring review cooldown, multiple voluntary lifetime episode receipts only from current verified conditions; original chapters separate and no fake uninterrupted service. On death/successor record new identity, no inherited personal gold, oath or rank. Retirement is not eighth career.

## Shared systems (Gate D/E)
- [ ] Real spouse reciprocal, children verified parentage, generated siblings verified kinship, actual births/deaths observed once. Education milestone corresponds to native evidence; compare skill gains against BirthAndDeath and native education. Check lord/king family obligations and inheritance claims never confer ownership.
- [ ] Native relationship and gold before/after petition, acceptance/refusal, insufficient funds, unchanged/partial native effect, rapid repeat click, follow-up, exit/reopen, save/load. Confirm at most one payment per resolved request; story failures cannot reopen it; verify no phantom consent. Distinguish modeled NPC peer interests from actual relationship changes. At most one every 7 days, 30-day personal cooldown, capped 64, no scan once full.
- [ ] Household duty report, gift and comradeship individually tested at native relation cap and after reload; examine distinct evidence and quotas. Knight service order, escort and recommendation retain distinct receipts. Compare actual gold/relations/renown to witnessed receipts, not just UI text.
- [ ] Clan estate assets separate from personal Hero gold, negative balance change allowed but no fabricated income. No automatic CU harvest, sales, depot production, shipment processing, convoy tick, levy or patrol; inspect old funded depot/active convoy saves separately (migration unresolved).
- [ ] Battle: actual player-party victory/defeat, enlisted host battle, unrelated NPC battle, absent provider, incomplete host snapshot, same-side patron, duplicate event and reload. Compare native casualty roster including unknown vs zero, exclude contradictory wounded/fallen identity. No personal kill/reward inference or additional combat rules. Existing battle v1/v2 schemas and acknowledgement record reload, no extra reward activated.
- [ ] Dossier/camp/career views show same native identities, selected calling, recorded observations, present political status and historic evidence without duplicate mechanics. Read-only views do not spend gold or grant skills.

## Campaign correspondence and native presentation (Gate C/D, all seven)
- [ ] In each actual role, open correspondence from the campaign map, click the pending notice, open a sender, verify the native portrait and actual clan banner, last known settlement/encyclopedia and back/escape. Verify all five tabs, pagination, empty state, active/expired requests and reopening after save/reload. Check no remote payment or relationship change. Native canvas, frame, clan header, quest journal and map notice art must render without missing sprites or overlap.
- [ ] Save role-specific screenshots <role>_affairs_journal.png, <role>_affairs_notice.png and <role>_affairs_sender.png. Record the actual cu_campaign_affairs_open event, and the new ui_affairs_* manual observations in the role JSON. Synthetic fixtures only exercise the parser, never count as played evidence.
- [ ] Repeat at 1280x720, 1920x1080 and wide resolution, with baseline and full modlist. Verify notifications stay in the native stack, no repeated quest/settlement menus and no UI lock after escape. Native testing remains required even after successful CI.

## Generated companions (Gate C/D)
- [ ] Verify plain cultural names, separate office labels, native skill targets and cultural role-specific battle kits, weapon/ammunition, horse/harness and readback. Check Lord, King, Knight, Outlaw and selected Wanderer specialty individually. Rename a new officer, save/reload, verify same native identity and office. Existing saves retain names/equipment, no automatic retroactive grants. Manual Wanderer stats remain exact.

## Ordinary correspondence and living relationships (Gate D)
- [ ] Wait through the native daily observer with known separated adult family, companions and patron. Verify a modeled personal letter appears only in Letters/History, never a pending petition badge or native quest. Observe global minimum 14 days and same sender minimum 60 days, and no letter to a successor written to the former player. Only latest ordinary letter per tracked person retained.
- [ ] Compare native gold, relation, rank, clan and troop state before and after receiving and reading ordinary news. No effect. Reopen after reload, check original wording, culture and recipient. Compare current native relation and recorded relationship dialogue at positive, neutral and negative values; no claim of an actual native friendship or rivalry action. Change actual native occupation/kinship and compare inferred agenda changes, never manufacture autonomous action.
 
## Persistence and performance (Gate E/F)
- [ ] For each career: day 0, day 7, day 30, save/quit/relaunch/reload, repeat actual action and compare gold, relations, troops, skills, receipts and roles. Test successor, old pre-v0.74 save and v0.72/v0.73 battle saves in copies only. Record any manifest-version warning; corrupted optional records must fail closed without replaying benefits. Historical chapter ledger not personally keyed; do not attribute it to heir.
- [ ] At least 60-minute full modlist session, 30 in-game days, town/siege/battle/naval travel, menu reopen and autosave. Review CPU/frame pacing versus CU-off same save/test route, exceptions, event rate, save size and memory. Capture baseline and repeat measured runs, not invented thresholds.
- [ ] Inspect native/SaS/RBM, Diplomacy, Improved Garrisons, Fourberie, ZZZ, NoblesBuyStuff, UI and War Sails ownership against exact DLL and Harmony patch list. Check no extra troops, money, wages, relations, diplomacy or AI actions from held CU features.
- [ ] Mark PASS only with observed native evidence. BLOCKED for unavailable provider or unresolved old convoy/garrison gate. FAIL for mismatch, crash, duplicate effect, corrupt save or unexpected mutation. Attach exact diagnostic session, screenshot and before/after state for each failure. No release until all required gates pass or explicit revised acceptance contract is agreed.
