# v0.13 castle economy and manual garrison controls

**Target:** Bannerlord PC v1.5.7, `Bannerlord.ReferenceAssemblies 1.5.7.125802`. **Status:** COMPILED_UNTESTED, Gate B verified by [GitHub Actions 44246191394](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44246191394) for commit `0f7c194240e0797001abec402127a86357cbc898`. The CI run passed 199 policy tests, ranking validation, ZIP integrity, 40 modeled campaigns and reference-DLL compilation with zero warnings/errors. In-game behavior and save reliability are not verified.

## New source behavior

The existing read-only castle wage briefing now displays modeled daily income required to pay the allocated clan-party wage share, actual garrison wages, an assumed expense reserve and the desired surplus. The difference from the modeled income forecast is explicitly a **model shortfall**, not observed tax revenue.

The new castle management menu reports the actual existing garrison troop count and wages, native-culture basic troop holdings in the player party and garrison, current wage limit, and forecast-affordable limit.

`EnableManualGarrisonTransfers=false` is the default. Turning it on exposes *manual* transfer of five existing native-culture basic recruits from the player's actual roster to an existing castle garrison, and back. Transfers reject enemy castles, siege state, missing parties, missing troop type, insufficient source troops, and destination caps. They refuse source troop stacks containing wounded recruits or stored experience until accurate roster-state preservation is implemented. They do not create troops, call party movement, or charge gold.

`EnableManualGarrisonWageCaps=false` is the default. The manual action lowers a castle wage limit only if the recommended modeled limit meets the defensive minimum, is below its existing positive limit, and is **at least the garrison's current observed wages**. It does not dismiss troops or raise the player's existing manual wage limit.

The old auto wage cap and auto garrison recruitment remain off by default.

## Evidence and gaps

Pure policy tests cover negative/overflow income inputs, modeled shortfalls, disabled gates, ownership, siege, source and destination rosters, and cap behavior. GitHub CI compiles against version-pinned public references. No test here certifies that the game has accepted the menu, native roster mutation, real estate tax readings, save persistence, troop wounded state, campaign capture transitions, or the full historic Improved Garrisons feature set.

## In-game acceptance matrix (not yet run)

1. With all manual feature flags false, enter a player-owned castle. The read-only staffing and budget options appear, but transfer/cap actions do not.
2. With `EnableManualGarrisonTransfers=true`, place at least five healthy culture basic recruits in the player party. Deposit five, record both native roster counts, withdraw five, and check that total troop counts stay constant.
3. Attempt transfers with four available recruits, full destination, under siege, captured castle, and after save/reload. Every rejection must leave both native rosters unchanged.
4. With `EnableManualGarrisonWageCaps=true`, test below-actual-wage rejection, above-actual-wage acceptance, siege rejection, and permanent save/reload of limits.
5. Track a castle and two villages over seven to thirty native days. Compare actual clan finance-screen tax and wage values with the model. Never describe modeled shortfall as verified native income.
6. Complete mission entry, exit, and settlement departure regression. Compare crashes against an official-only control. Record exact module and game version.

No separate party orders, scene hooks, Harmony patches, naval transitions, or original third-party binaries are added.
