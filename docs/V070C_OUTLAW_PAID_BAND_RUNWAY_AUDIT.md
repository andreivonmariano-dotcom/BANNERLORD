# v0.70c | Outlaw / Bandit: paid native starting band and runway

**Date:** 21 November 2026. **Status:** implementation candidate, native gameplay acceptance pending. Draft PR #93 branch `audit/v069e-current-gameplay-and-visual-acceptance`. Historical module manifest still says v0.38.0. This is a new-game-only Outlaw feature, not a legacy-save conversion.

## Updated request

The Outlaw / Bandit starts with a small genuine bandit band and an actual non-noble band lieutenant. Wages must not cause immediate desertion. The band should still require food and wages. Loot, ransom, trade and other actual native game activity fund the band after its initial supplies are spent. No permanent payroll exemption or desertion immunity.

## Source behavior

- **Native origin:** `Outlaw` retains Bannerlord Advanced Start's actual `outlaw` type. No fabricated bandit-clan membership, kingdom, noble birth or fief.
- **Lieutenant:** existing `RoleBased` creates one `SecondInCommand` Hero in the player's culture with trained tactics, roguery and leadership. Title is plain **Band Lieutenant**, never Ser/Noyan/etc. The phase-9 contract demands exactly one registered native party/clan companion of non-noble occupation and correct culture.
- **Band:** target **6 living regular Bandit-occupation native troops**, with tier 1–2, chosen from the game's registered bandit cultures. The player culture directs preferred bandit region (Vlandia/mountain, Battania/forest, Sturgia/sea raiders, Khuzait/steppe, Aserai/desert, Empire/looters). No assumed troop `StringId`, template cloning, extra standard recruits or troop upgrades.
- **Roster guard:** inspect the native original member roster first, count all native Bandit-occupation regulars and grant only the missing number up to six. Reject a pre-existing large or invalid roster. Check the real native party-size cap before granting. Do not silently dismiss pre-existing nonbandit units or remove native world units.
- **Native wages:** obtain `Campaign.Current.Models.PartyWageModel.GetTotalWage(party)` after the grant, with a projected wage preflight. The actual model's measured daily wage is authoritative. Reject large bands above **250 denars/day** instead of creating a giant treasury. Keep native `PartyWageModel`, paid upkeep, unpaid-wage penalties and desertion active.
- **Starting cash:** grant only the deficit needed to reach `max(750, 14 × actual_daily_party_wage + 200)` denars after the player's chosen native/advanced gold. This is **14 days of initial wages** plus a small food/cost reserve. A native starting purse already above that target stays unchanged. No repeated wage credits or daily stipend.
- **Food:** top up to **12 native grain units** using the game's registered `ItemObject("grain")`; verify actual party inventory. Native food consumption remains intact.
- **Family:** still optionally spouse, children and siblings. An Outlaw cannot invent an owned castle/town as their family residence. New artificial parent genealogy anchors are inactive, not living court members. Existing native family is not deleted.
- **UI:** one Outlaw / Bandit option, player appearance and stat customization retained. Old generic `Troops`, invented court `RetinueMode`, influence, and owned family residence selectors are hidden for this new path. The future seven-path front-end is still a staged redesign.

## Limits and hazards

- A 14-day wage reserve is a **starting condition**, not a promise that no troop can ever desert. Genuine losses, starvation, capacity limits, low morale and spending the money still cause native consequences.
- Native payroll, `PartySizeLimit`, troop selection, item inventory and the registered lieutenant are all verified in source-level code and logged. These native values need a live gameplay run to substantiate the intended effect.
- Native troop-grant, gold and item actions are **not atomic**. If a later action fails after a troop was added, return a truthful `partial:` record, with actual bandit, gold and grain counts, rather than inventing a rollback.
- No changes are made to existing saves, world bandit factions, other role starts, the normal bandit recruitment mechanic or the prior Wanderer companion start.

## Live acceptance sequence

1. Launch new Outlaw / Bandit in Vlandia. Use matching compiled test DLL, save its hash. Require native start type `outlaw`, zero kingdom/fiefs and one actual Band Lieutenant. Verify occupation, gear, roguery and real party enrollment.
2. Inspect native party. Verify about six tier-1/2 bandit regulars and one named lieutenant, with no unexpected cultural peasant recruits. Verify party total under the real limit.
3. Record native daily total wages, player actual gold and grain. Require starting purse at or above `max(750, 14 × observed_daily_wage + 200)` and at least 12 grain. Inspect the actual inventory and clan budget in-game.
4. **Pass 14 days without new loot**, recording actual troop count, native wage debits, morale, hunger and gold daily. Under otherwise unchanged conditions, paid wages and starting food should not be an immediate desertion cause. If troops desert anyway, inspect native wage limits, food consumption, morale penalties and external mod contributions. A log-only check is insufficient.
5. Save, quit, reload, continue for another day. No duplicate bandits, lieutenant, food or cash. Verify original troop relationships persist.
6. Repeat as a second culture, with optional spouse and siblings. Check genealogy, family residence `None`, no aristocratic title and clean native case handling.
7. Run the clean Wanderer start again to rule out Outlaw branch leakage. Keep older twelve-role campaign saves loading without reinterpretation.

**Release gate:** compiled reference build plus native campaign and save/reload screenshots/logs. Draft PR remains unmerged, no installer pin changes.
