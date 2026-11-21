# v0.69i | Live session regressions: unavailable noble birth, synthetic parents, cultural officers

Date: 21 November 2026.
Branch: audit/v069e-current-gameplay-and-visual-acceptance (draft PR #93).
Status: source repair / real-game retest pending. No user saves, raw diagnostic ZIPs or identifiable data committed.

## Session A: 05:05 UTC, Younger Brother of a Lord

User session: CU-Session-20261121T050515_41f8476b-CLOSED.zip. The running game DLL SHA-256 was `58203d10407a96651e022749911228b1d3861d23e2411e1d255a373d433b246c`. Role Lord, kingdom Vlandia, birth YoungerBrotherOfLord, and native age 30. A House preview explicitly said **No eligible native elder found for this background**. The House confirmation gate logged **14** consecutive refusals between 05:06:37 and 05:07:01 UTC (`cu_house_noble_preflight_blocked`). The player later used House Back. No native spouse/sibling creation or finalization occurred in this session. The archive shows a normal shutdown, no managed exception and no screenshots.

The requested background could not legitimately bind to an existing qualifying clan-leading native elder with validated parents. No fictional older lord, parent, title or fief is invented to satisfy a choice. This is not evidence that a genuine YoungerBrotherOfLord start is now fully supported.

**Change:** On House attachment, focus `NobleBirthPosition` immediately if the projected native relative is absent. Display a concrete unavailable-birth message. On the actual noble-choice Continue action, block progress with explicit corrective instructions. The existing final irreversible House preflight remains. Changing to Independent preserves the entire House draft. A different native lord origin must still pass projected and final native tests. The objective is to stop the **end-of-House confirmation loop**, not to claim an unqualified elder exists.

## Session B: 05:12 UTC, Independent Lord with family

User session: CU-Session-20261121T051231_c1e97521-CLOSED.zip. Role Lord, kingdom Vlandia, birth Independent, native age 40, spouse age 38, two children ages 9 and 7, and two siblings. The original House/phase-9 preflight **passed**. The native spouse, two children and two siblings were created, with `cu_family_verification|result=PASS`.

The original creator allocated **two generated parent Heroes** `CharacterObject_3278` and `CharacterObject_3279` (ages 64, 62), linked them as the player and siblings' shared parents and activated them as native Lord-occupation Heroes. They were not requested separately. The clan began with **two native wanderer companions** `CharacterObject_1869` and `CharacterObject_1868`. Requested retinue mode was **RoleBased**, but `SandboxCulturalRetinue.TryCreate` returned `refused:multi_companion_dismissal_not_atomic`, leading to `sandbox_start_contract|result=FAIL|reason=retinue_not_verified`. Optional money, items, troops and renown were withheld; the already created family was retained with a recovery record. The archive reports normal shutdown and no managed exception. No save/reload or rendered UI inspection is provided.

### Parent record boundary

A real **native sibling relationship** in this code depends on shared `Hero.Father` and `Hero.Mother` references and parents' native `Children` lists. Clearing both would silently turn the requested siblings into unrelated Heroes. For an Independent Lord whose parents were originally null, generate those native genealogy anchors **without activating the new adult Heroes**, appointing them as officers or moving them into a party. Existing native parent references are not erased or changed. The ancestors **can still appear in Bannerlord's native family tree**, which is the correct genealogy. This is not a feature to delete historic ancestors or forcibly kill existing parents.

### Retinue replacement boundary

The old code simply refused if the native start contained more than one companion. The new path:

1. Checks the native companion roster and validates that each companion is legally eligible for the native `RemoveCompanionAction.ApplyByFire` action.
2. Resolves same-culture native lord templates and stages all cultural officers **before any native companion dismissal**. Allocations and registration are checked.
3. Dismisses the existing native companions one by one using the native action, confirming each removal and the final roster. If any native action fails, it returns `partial:`, records the actual changes, and never claims success or releases optional grants. This is not a true atomic transaction.
4. For `Role=Lord`, officers are always a named, trained and equipped **Master of Arms and Knight**, even if a younger brother already accompanies the player. The brother remains family, not a substitute for a professional court office.
5. The pure structured receipt accepts up to 16 verified native dismissals. Malformed and partial receipts still fail.

**Status:** The fail-closed replacement is a candidate repair. Actual dismissal, officer roles, skill/gear, appearance, genealogy and save/reload behavior still require a fresh in-game run with the matching DLL.

## Recheck in native game

- Start **Lord / Vlandia / YoungerBrotherOfLord**, proceed to House. The user should see a clear unavailable noble-origin choice promptly, not 14 final confirmations. Select Independent and confirm that spouse, child and sibling choices are preserved. A legitimate younger-brother start requires a real named eligible lord with native genealogy; an unavailable choice must not be called successful.
- Start **Lord / Vlandia / Independent** at age 40, spouse age 38, children 9 and 7, siblings 38 and 44, Retinue RoleBased. Inspect actual player Clan and Family screens. The two newly synthesized parent Heroes must not be active court members or party officers. They remain genealogical references.
- Verify **no native wanderers remain in the player's companion roster**, and exactly the intended same-culture **Master of Arms and Knight** exist, with their native Hero IDs, equipment and trained skills. Require `cu_cultural_retinue_replacement` with the true native count dismissed, and `sandbox_start_contract|result=PASS`.
- Save, quit, reload and verify spouse, both children, siblings, court officers and landed status. The saved family should not duplicate, and no new grants should run on load. Check the true game interface rather than trusting a log line.
