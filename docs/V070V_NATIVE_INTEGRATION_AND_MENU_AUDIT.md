# Calradia Unified v0.70v | Native integration and menu-surface audit

## Evidence, scope and status

Source audit performed 21 November 2026 against draft PR #93. The last player-supplied ZIP `CU-Session-20261121T115957_a1e76bb9-CLOSED.zip` recorded three Knight/Lord/King starting-contract passes but **not three distinct completed save/quit/reload campaigns**. The new Knight dialogue and escort from v0.70u compiled cleanly in Windows Gate B. Its actual patron talk and map escort remain unverified. No release promotion is authorized by the menu audit.

## Root cause of the crowded settlement screen

The source previously registered numerous independent options directly under both `town` and `castle`:

- `CivicBehavior.OnSessionLaunched`: six depot/recruitment/aiding options plus registered physical convoys, garrison training, castle management, feudal budget, four kingdom entries and depot trade. The total is approximately 17 separate civic actions per location, including projected accounts and abstract shipments.
- `SandboxOriginBehavior.RegisterMenus`: legacy **Design my NEXT Sandbox starting life** entry even inside an ongoing campaign, separate household portraits, and family residence.
- `SandboxOriginBehavior.LivingMenus`: two more entries, People & Lineage and a failed-family review.
- Knight oath, Lord inheritance, King succession, and Royal Council: one independent entry each at both towns and castles, irrespective of whether the location had any connection to the player's household or royal court.

Before this patch the static registration surface was about 27 independent added settlement options, many with conditions that hid them at particular locations. Registration count is not the number simultaneously visible. Still, the user saw multiple unnatural role/configuration tabs.

## v0.70v source repair

Now only **two contextual top-level entry routes** are registered by these behaviors, each with `town` and `castle` variants:

1. **Household and personal affairs**, visible for a real native household, saved role, native history or recovery case. Includes spouse/children/officers, family portraits, residence move at a real owned seat, Knight oath review, Lord inheritance claims, King succession, court guidance, and Living Persons requests. Knight service actions use the actual patron's conversation. The old in-game next-start editor entry has been removed. The new-game guided creator remains the intended start editor.
2. **Manage local administration**, visible at a player-owned holding or an actual allied-kingdom settlement. The original civic actions retain their real resource, gold, native troop, city ownership and daily-cap guards. They are grouped under **Stores and trade**, **Recruitment and defenses**, and **Kingdom correspondence**. Existing saved `CU_v030_civic_state` records are unchanged. Labels are now local descriptions without the repetitive **CU:** prefix.

Grouping remains an interim interface. Some civic actions truly require presence at an owned holding, so they belong in a settlement. The kingdom correspondence section should later be a kingdom/minister interaction rather than a local submenu. No fake button is a substitute for the game's Clan, Party, Encyclopedia, or Kingdom pages.

## Current feature maturity, by system

- **Wanderer:** optional actual companion and wage reimbursement are in source. Real game load and repeat wage checks outstanding.
- **Outlaw:** actual low-level bandits, one lieutenant Hero, payroll and provisions. Native crime/reputation and quest consequences are not established.
- **Mercenary:** source requires a genuine native kingdom mercenary company and no fictional subsidies. Separate native acceptance pending.
- **Soldier:** depends on observed external Serve as Soldier enlistment. Native provider gameplay acceptance pending.
- **Knight:** independent real clan, genuine named lord relationship and saved oath. v0.70u introduced native patron dialogue, immediate audience, 30-day reports and three-day real MobileParty escort. No actual kingdom membership and no full quest journal or battle commissions. Younger Sibling's real native clan transition remains blocked.
- **Lord:** real native vassal clan/fiefs, spouse/children, Master of Arms and household Knight as native companions. Neither martial title automatically commands troops or a real army. Succession currently records biological claim order, not native transfer.
- **King:** real native ruling house, real Minister governorship and real Marshal/Kingsguard companion Heroes. The Kingsguard is ONE Hero, never troops. The council currently verifies the Minister's seat and officer attendance but does not execute kingdom votes, war orders, or native policy decisions. This is a meeting, not functional government.
- **Living People & claims:** tracked native family/settlement data and character requests can trigger guarded real gold and relation actions. Inheritance petitions and property-grant proposals remain custom settlement dialogue, not yet true personal hearings with a clan leader.
- **Economy and civic:** garrison and troop roster actions, guarded convoys, grain items and payments are native-backed. Abstract shipments and wage estimates do not represent actual caravan parties or native tax revenues.
- **Kingdom autonomy:** existing source is opt-in, restricted to real player-clan NPC lord parties, disabled by default, and has guarded native movement methods. Do not assume the Marshal has a personal command or a standing kingdom army.

## Next integrations, with acceptance gates

**Knight career:** Named lord first audience and native party escort must pass real play/save. Next add a real quest journal and battlefield commission with actual native battle record, then a sovereign-conversation admission request that uses verified native `ChangeKingdomAction` and preserves player clan ownership. The exact v1.5.7 API signatures are now included in the metadata audit output. No automatic or imaginary kingdom membership.

**Lord officer work:** Master of Arms must operate on an actual party/garrison roster, with real authority and observations. Household Knight must participate in actual native combat and retain the same Hero ID. Keep family succession separate from native leadership and fief-ownership actions.

**King governance:** Royal Minister should continue to be a verified native governor, Marshal should receive genuine native military authority only through a real clan/army command, and the Kingsguard remains one Hero. Royal decisions should use native kingdom interfaces, not only raise a meeting counter. Audit named delegates, legal authority, effects and reload.

**Live UI:** After real save, quit, and reload, each start must observe true flags for duplicate-free settlement choices, context-appropriate actions and absence of the old next-game editor. Retain existing readable portrait, party/Clan/Kingdom state, screenshots, and Back checks. Test at 1280×720, 1650×927 and 1920×1080. A source/build pass is not a rendered in-game pass.

**Release:** Keep PR #93 draft, current installer pinned, and Gate C through E blocked until actual Bannerlord sessions prove the behavior.

## v0.70w | First real duties for Lord and King officers

The real Master of Arms and Marshal are no longer purely informational titles:

- **Lord:** In an actually player-owned castle, speak to the **Master of Arms** companion in the native Hero conversation. With at least five unwounded, zero-XP local recruits already present in the player's party, the officer may issue a transfer of exactly five to the existing castle garrison. The source verifies the before/after native troop counts and rolls back any partial roster change. No troop is created or paid.
- **King:** Convene the royal council at its real player-owned court seat with the real Minister governor, Marshal and single Kingsguard Hero present. On that same campaign day, speak directly to the **Marshal** companion at the seat. An explicit order moves five actual local recruits from the player party to the native court garrison, never creates new troops, and leaves kingdom political decisions untouched.
- Both actions are separate from the generic civic recruitment/garrison controls. These are personally issued orders to existing native Hero officers, grounded in the same physical roster records and ownership guards.
- To mark either accepted, diagnostic events must show **five existing troops transferred, zero spawned, real garrison true**, and role-specific screenshot `lord_master_order.png` or `king_marshal_order.png`. In-game dialogue and roster checks are not certified by CI.

**Remaining gaps:** This does not appoint the Master of Arms as a native army commander or confer independent command on the Marshal. Full military expeditions, actual Army leadership, officer survival/injury, native Kingdom proposals, the Minister's administrative recommendations and rulership policy remain separate work. Do not call the council a native kingdom government until those actions are proven.
