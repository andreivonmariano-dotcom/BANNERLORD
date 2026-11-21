# v0.70h | Knight origins and seven-path selector

**Status:** Experimental source on draft PR #93, not native-playthrough accepted.

The new-game menu shows Wanderer, Outlaw/Bandit, Mercenary, Soldier, Knight, Lord and King. All historical role identifiers stay readable in older saves.

## Sworn House

New `KnightGuidedStart=true` with `KnightOrigin=SwornHouse` requests the native `default` start. The player heads a distinct, landless and politically independent clan. The chosen kingdom indicates the real personal lord's kingdom. The existing native service-patron adapter selects an actual living clan leader and changes native personal relations. It does **not** implement feudal duties, a binding oath contract, military commission, estate, title grant, salary, household officer or army command. Those require verified mechanics. The start gives no invented troops, estate, resources or squire.

Spouse, children and optional siblings are valid under native family rules. The founding-house rule bans older generated brothers but permits older sisters. Existing relatives in saves are not deleted.

## Younger Sibling

The choice is visible but blocked before irreversible native start, with `native_same_clan_kinship_not_implemented`. Reparenting the player to an existing lord's parents alone cannot give the player real membership in that lord's clan. The subtype's sibling generator is suppressed. Native clan joining must pass live game testing before this origin can proceed.

## Soldier correction and migration

The guided Soldier House hides sibling creation and its profile rejects any stale nonzero sibling count. All 13 legacy starting role IDs still parse. Legacy Knight still maps to native `vassal`, not the new `default` start. Do not migrate an existing save silently.

## Live acceptance outstanding

Check seven visible choices, Knight Sworn House native independent clan and native patron receipt, family links, no fief, Younger Sibling early rejection, Soldier sibling suppression, older-sister acceptance, older-brother refusal, legacy Knight save, patron death/capture and save/quit/reload. Compilation is not a gameplay pass.

## Verified Windows source/compile milestone

- **Exact compiled source commit:** `5ddba9ddf84c0cc2fd0553399be3c3de11a5411e`.
- **CI:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44879904659 , completed success on 21 November 2026.
- **Independent policy tests:** 999 checks passed. Extended audit simulation: 1,159,206 modeled policy assertions. Compilation against pinned Bannerlord v1.5.7.125802: **0 compiler warnings, 0 errors**.
- **Runtime artifact:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44879904659/artifacts/14697289228 . Historical internal manifest/artifact label remains `v0.38.0-compiled-untested`, despite v0.70h source changes.
- **Compiled DLL SHA-256:** `b71741a9cb30a3a7629b3b3c4983195d6cce9670cf235c4d3b31c9fbbbce2d98`.
- **Highest evidence gate: B (compiled and policy tested).** Real game creator UI, true oath mechanics, same-clan younger sibling, clan/estate relationships, and save/reload have not passed native game tests. Leave PR #93 draft and installer pin unchanged.
