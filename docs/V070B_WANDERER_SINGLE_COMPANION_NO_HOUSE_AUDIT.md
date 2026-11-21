# v0.70b | New Wanderer start: no House, optional one free companion

Date: 21 November 2026. Draft PR #93, not merged. This supersedes the Wanderer family option described in `V070A_WANDERER_START_ACCEPTANCE_CONTRACT.md` for **new guided Wanderer games only**. Existing save/profile formats keep their old semantics.

## Exact user request

- Wanderer remains the native `default` player start.
- The entire **House and Fortune post-clan overlay does not appear** for new Wanderer games.
- The native player age, appearance, culture, 3D FaceGen, story, attributes, skills, traits, focus and stat override choices are **retained**.
- Optional **one** companion. Without the toggle, begin without any companion, including incidental native starting wanderers.
- The new companion starts with **100 mutual native personal relation**. Relations can subsequently change through ordinary gameplay.
- Companion requires **no net daily wage payment by the player**. The current candidate does not patch the game-wide native wage model. Instead, a campaign-save-backed daily refund credits the model's listed `GetCharacterWage` to the player once per day only if the designated companion is still serving in the player's actual main party. This is an **economic offset, not a zero-cost native model entry**. Effective deductions, native party wage UI and timing require live-game verification.
- Companion customization in World: enable, name, age 18–70, gender, Guard/Scout/Healer/Steward/Trader specialty, exact primary specialty skill 0–220 (Advanced).
- Companion customization in the existing post-narrative stats editor: separate target for companion **level, attributes, all registered native skills, focus and traits**. The spouse tab is renamed and conditioned on the opt-in in **every shipped stats-editor movie**. The player's existing full stat editor is unchanged. No dedicated 3D companion FaceGen or civilian-equipment editor is claimed.
- No spouse, children, siblings, noble lineage or House residence creation for the new Wanderer starting path. Those relationships can be developed during gameplay. The other starting paths retain their prior House flow.

## Implementation boundary

- `SandboxOriginProfile` retains all legacy keys, adds a versioned opt-in `WandererNoHouseStart`, `WandererCompanionEnabled`, fields for unique companion identity and specialty, and `WandererCompanionOverrides` stored as `WandererCompanionStat.` keys. New-game reset clears all new flags and overrides. An invalid new Wanderer House, spouse, child, sibling, noble, fief or retinue request fails validation.
- `CUGuidedCreatorVM` gathers the companion at World before the true game-native creator. It sets `RetinueMode=FamilyOnly` to dismiss incidental native wanderers. It hides the EarlyHousehold advanced field. Native player stats remain in their current stage.
- `CUNativeHouseLayer` caches the committed new Wanderer flag at native creator manager entry and returns **without attaching** the post-clan House interstitial. The game-owned Review/Options, Back, Banner and Clan stages are untouched. Other starts and old profiles keep their House editor.
- `CUNativeStatsVM` reuses its second target for the companion. All 5 shipped XML prefabs bind a dynamic button label and visibility. Companion overrides never overwrite the founder or legacy spouse overrides.
- `SandboxWandererCompanion` stages a same-culture, sex-matched, native-template-derived new Hero, uses genuine character creation, native equipment, exact age, a chosen specialty skill and any manual stats. It joins the player's clan and party through native companion actions. It sets and verifies 100 mutual native relation and an exact **one companion** roster. It returns an honest partial outcome on failure.
- `SandboxOriginBehavior` creates the companion **only after the native replacement receipt passes** and before discretionary resources. If the companion or its absence cannot be verified, the grant barrier fails. A stable generated Hero ID and last wage-credit day belong to the actual campaign save, not to the external next-game .ini.
- Legacy pre-v0.70b profiles, other roles and installed saves are not migrated or silently edited. No wholesale replacement of world NPCs or clans.

## Required real-game acceptance

1. Wanderer without companion: no House screen; native appearance/stats/review work; exactly zero starting clan companions; native independent landless status; `sandbox_start_contract=result=PASS`.
2. Wanderer with one companion: name, gender, culture, age and exact skill/overrides appear in the actual native Hero; exactly one companion in the actual main party and player clan; both relation directions read 100 at campaign start.
3. Verify full companion stat editing in installed 1.5.7 Gauntlet XML: editable level, attribute, focus, skill and trait fields. All unedited stats inherit the native donor; edited values match the request. No ghost spouse selector or House page.
4. Examine the **real party wages, clan budget and gold transaction timestamps over at least seven in-game days**. Verify daily native wage deductions plus refunds net out to **zero**, without allowing a positive subsidy when the companion no longer receives a native wage. If native wage modifiers or external mods change the charge, adjust the accounting before claiming a guaranteed zero net cost. The party-wage tooltip may continue showing a wage, since this code does not override the game's wage model.
5. Save, close, reload. Verify companion `StringId`, 100-at-start relation history, native development, no duplicate recruitment, no replay of grants, wage credit only once per calendar day. Dismiss or send companion away and confirm the wage credit stops.
6. Confirm the House/Family creation UI and spouse statistics still work for non-Wanderer and older legacy profiles. Confirm stat editing and Back/Continue at 1280×720, 1650×927, and 1920×1080.

## Evidence status

Pending Windows CI and native gameplay retest. A passing source regression proves wiring and assertions only. It cannot prove that Bannerlord will successfully dismiss incidental native companions, register the new one, grant exactly 100 native relationship in both directions, charge/offset wages accurately, render the renamed Gauntlet button or save/load without effects. No release tag, merge or default installer pin change is authorized without those checks.
