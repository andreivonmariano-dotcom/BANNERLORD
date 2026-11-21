# v0.70 proposed design | Seven starting paths, military service, and credible family history

**Status:** Design RFC, not implemented or game-tested. Recorded 21 November 2026 in the draft acceptance branch, PR #93. No source gameplay code or saved game is changed by this document.

## User decisions already given

Show exactly seven top-level starting paths, in a single screen:

1. **Wanderer**, the native/default free start.
2. **Outlaw / Bandit**, criminal life with native outlaw consequences.
3. **Mercenary**, paid fighter or mercenary company under a time-limited agreement.
4. **Soldier**, attached to a named real lord and clan, selecting Recruit, Sergeant, or Captain.
5. **Knight**, serving a named real lord, choosing a real younger sibling of the clan head or a separate sworn house.
6. **Lord**, active head of a noble clan.
7. **King**, head of a recognized kingdom.

All seven may request an eligible spouse and children. Soldier hides sibling creation. A Knight choosing Younger Sibling of Lord hides sibling creation. For a Lord whose own house is founded with the player as head, no generated male sibling can be older than the player. No new character should silently claim political leadership merely from sex or age.

These are starting **life paths**, not seven native engine StartType IDs. The current native contracts do not support every requested distinction.

## Design recommendations pending gameplay acceptance

### Wanderer

- Keep initial clan, troop, resources and diplomacy equivalent to the verified default native start. Family is entirely optional.
- Allow personal siblings, spouse and children only through verified native Heroes, age and residence rules. No arbitrary offices or automatic noble estate.
- Move Trader and Beggar out of the main selector into optional background or starting wealth labels, preserving their special history only where a native behavior supports it.

### Outlaw / Bandit

- One top-level entry. Offer independent outlaw or attached bandit-band service only when supported.
- Marriage and dependent children are allowed. Dependents are not automatically bandit troop units or sent into raids.
- No noble fief, heraldic title or legal liege claim. Native faction hostility, criminal penalties and party loyalty must match the requested start.

### Mercenary

- Represent a **contract** with patron, company, wages, term, current status and discharge. Do not confuse that contract with hereditary allegiance.
- Family and siblings allowed as independent persons. No invented noble birth or automatic land title.
- Default to the player's company rather than a native enlisted soldier inside the lord's party.
- The existing source's Soldier role actually maps to native `mercenary`, so migration cannot change an existing saved role's semantics invisibly.

### Soldier

- Sub-ranks: `Recruit`, `Sergeant`, `Captain`, in that order. Starting at Sergeant or Captain requires believable experience, starting skills, age, personal relation and patron appointment. No rank grants a fief or makes the player a clan leader.
- Named patron lord and **patron clan** are mandatory. The player's own native `Clan.PlayerClan` identity remains independent of this service affiliation until a tested game API supports true native clan membership. Do not set `Hero.Clan` to the patron clan as a shortcut.
- A real enlisted life needs proven command-chain gameplay: following a named lord's party, battle participation, wages, duty progression, service leave, capture handling, injury, patron death, dismissal, desertion and persistence. A simulated `mercenary` banner and relation bonus are not enlisted service.
- Advancement: Recruit -> Sergeant -> Captain. Use tenure, training, command skill, battle record, disciplinary status and explicit appointments, rather than arbitrary calendar ticks or one kill count. Record promotion and demotion events.
- Optional spouse and children, subject to actual age and living arrangements. Hide the SiblingCount control, force zero on **new** Soldier profiles, and refuse a stale nonzero value at the start gate without silently deleting saved family people. Family dependents cannot travel as active military soldiers by default.
- No random player-clan wanderer companions or privileged household court. Captain aides need separate verified appointments.

### Knight

- A knight is noble or martial status, **not** automatically the next enlisted military rank after Captain. A Captain can be knighted through a supported, recorded appointment. Service command and noble status are different attributes.
- **Lord's younger sibling:** Select an eligible named **existing** clan head from a verifiable list, bind authentic native parents and actual kinship, obey birth order, and hide manual sibling generation. Existing native brothers and sisters remain. If no valid lord qualifies, disable this subtype on its own selection screen. Never fabricate a native elder, rewrite their parents, or create a fake inherited estate.
- **Sworn knight of a separate house:** The player heads a small independent household with a **personal oath to a named lord and that lord's clan**. No family-tree link to the lord. A personal service contract does not silently merge two clans or turn the player into a kingdom vassal. Landless is the normal default.
- Both subtypes may request spouse and children. Independent sworn houses may request siblings. If the player claims to head that separate house under male-preference primogeniture, generated older brothers are refused. The lord's younger-sibling path creates no extra siblings.
- Default companions are an appropriate squire or armsman, not unrelated native wanderers.

### Lord

- Start as the actual `Clan.PlayerClan.Leader` of one verified noble house. A Lord is not automatically a king. Native vassal or other supported political affiliation must match.
- Spouse and children optional. Generated siblings allowed. An older **male** sibling is refused for the selected founding-house seniority contract, but older sisters and younger siblings are permitted. Native leadership still requires a real leader assignment, not just an age comparison.
- Historical parents exist only as preserved genealogy anchors where required. Newly generated parents must not become active court officials or replace the lord.
- Residence and estate are distinct. A clan can be noble and landless if the native political model supports it. Never seize an unrelated fief to satisfy a character origin.
- Starting court personnel are named, trained and culturally equipped Master of Arms and Knight. Their native Hero/party enrollment and removal of original wanderers must be verified.

### King

- The player must hold **verified native ruling-clan status** over a valid kingdom with the appropriate royal estate and political relations. The King option cannot be satisfied by setting a title string.
- Spouse, children and siblings optional, with succession custom controlling heir ranking. An older brother normally makes the founding-king claim contested, not a silent automatic overthrow. Default founding monarchy restricts older male siblings, with a future advanced Disputed Claim option only after tested succession.
- Royal household officers, guards, council and income must match actual native authority. A regent may act for an underage heir only through a tested regency system.
- Do not silently kill, reparent or replace an established native sovereign to install the player. If the native starting provider cannot establish the requested ruler status, block the start prior to irreversible finalization.

## Family eligibility and inheritance contract

- **All paths** have spouse and child choices. No role promises marriage that the native marriage model refuses. Children require two valid parent links, compatible actual birthdays and genuine `Children` collections. Adult children and minors receive correct native Hero states and living arrangements.
- **Soldier:** spouse and children only, no generated siblings.
- **Knight, Lord's younger sibling:** spouse and children only. Native existing siblings belong to the selected real clan and cannot be removed or regenerated.
- **Other Knight:** spouse, children and optional generated siblings. Default separate-house founding rule rejects older male siblings where the player is head.
- **Lord:** spouse, children, optional siblings, default reject older male siblings.
- **King:** spouse, children, optional siblings, default founding-royal restriction against older male siblings. More flexible succession requires explicit cultural rule and disputed-claim system.
- **Wanderer, Outlaw, Mercenary:** spouse, children, optional siblings, with no unjustified noble rank or inheritance.
- Distinguish **parentage**, **birth order**, **eligibility to inherit property**, **title ownership**, **clan leader office** and **military command**. The same person can be first in a succession line without presently controlling the clan. Do not infer exact birth order from rounded integer ages.
- Preserve genuine family ancestors as historical genealogical references. If the user chooses an Independent House, newly generated father/mother are not activated as visible living court officials. Do not remove actual parent IDs from native Heroes after a family has been linked.
- A living spouse or child in a soldier's family needs a valid home or safely represented separate residence. Dependent children are not active companions or troop roster entries by default.

## Safe native Hero strategy: generation and replacement

**Create new Heroes for the player's requested household, valid retainers and proven contracts. Replace only disposable start-state companion slots whose removal is supported by native actions. Never wholesale replace the game's existing lords, monarchs, clans or all relations.**

A full replacement rewrites stable Hero IDs, settlement owners, troop rosters, quests, claims, kingdoms, army leadership, AI memories, marriage links and every stored reference. A visually acceptable replacement can still corrupt a campaign on its first save or lord death. Re-creating every network relationship is a separate world-generation project, not a safe start-menu implementation.

- Preflight native identities before any mutation. Locate actual culture, available clan, designated patron, title owner, full parent links and starting fiefs.
- Allocate new player-specific Heroes once, with stable identity and source provenance. Stage all candidates first.
- Bind both directions of each native relation: `Father`, `Mother`, `Spouse`, `Children`, and only authorized `Clan` and party assignments. Do not clone an existing native lord into a new person then claim their title and inheritance.
- Apply native joining, gift, marriage and party actions. Compare exact state after each action. If a step fails, retain a truthful partial-result dossier instead of pretending rollback of a registered Hero is safe.
- Keep companion replacement sequential and auditable. Staged cultural officers must be verified before native wanderers are fired. The current v0.69i source repair passed CI, not native gameplay testing.
- One-shot commitment, versioned and stable save keys, clean new games, identity mapping and save/quit/reload checks are mandatory. Never regenerate the household on save load or transfer every native NPC into a new character identity.

## Reduced creator interface

**Page 1: Life.** Seven paths. On selecting Soldier, show exactly three ranks. On Knight, show exactly two origins. No second giant noble-birth selector.

**Page 2: Allegiance.** For Soldier, Knight, Mercenary, Lord and King only show relevant native clan/kingdom/patron controls. Choose by **named visible native lord**, with a clear clan and political allegiance, not by typing a raw `Hero.StringId`. Block missing patrons immediately.

**Page 3: Family.** One toggle for spouse, age/sex and name. Children and their ages appear only after spouse selection. One siblings toggle only where allowed. Show a compact family tree and intended household head. Keep extra genealogy details behind Advanced.

**Page 4: Fortune and Review.** Give a short, credible start-state summary with real clan, role, rank, patron, land, money, family, companions, and a clear warning about any missing native capability. Native FaceGen, culture, skills and Banner remain native stages, rather than duplicating every control in CU.

Hide detailed lineage IDs, fief transfers, source template IDs, role retinue modes, world event switches, and alternate scenario modifiers under **Advanced**. Remove unsupported options from the ordinary flow until a native implementation has an observed test result.

## Migration and acceptance

- **Do not mutate the existing twelve-role profile semantics.** Preserve old saved games and existing `Role` values. Introduce a new versioned seven-path creation schema, or a compatibility mapping only for *new games*. Existing Soldier means native mercenary in v0.69i and must not quietly become a low-rank recruit.
- Existing Trader becomes a Wanderer with trade focus in the **new creator**. Beggar becomes a destitute Wanderer. Admiral becomes an Advanced naval career only if the exact official expansion path is verified. Legacy Recruit, HouseholdKnight and NobleCadet become hidden compatibility types until a new service/kinship contract works. Do not delete their parsers.
- Build and test a pure `StartingPathPolicy` with fields for path, rank, knight origin, patron, family permissions, title contract and default retinue. Keep game-facing adapters separate.
- First deliver the menu and immediate eligibility preview on a **new-game-only feature flag**. Preserve v0.69i's currently audited path until the newer service implementation works.
- Then implement the Soldier service loop and promotion, Knight oath/kin contracts, household restrictions, and verified retinue defaults. Run the existing independent policy suite and pinned game-reference compiler on each milestone.
- Native acceptance must inspect new game, actual faction/party/leader relationships, rank progression and pay, companion identities, spouse/children/siblings, estate and title, save/quit/reload, patron death, promotion, dismissal, capture, and character death. Compilation and simulations alone do not certify these.

**Current status:** RFC accepted into the working draft only. None of the seven-path UI, three Soldier ranks, Knight new subtypes, or new service model is represented as completed code. Feature stabilization remains the release gate for PR #93.
