# v0.46 | Creator navigation, household residence and custom-screen readability

**Base:** v0.45. **Target:** Bannerlord PC v1.5.7.125802.
**Status:** compiled and source-regression tested, **not yet verified in the running game**. Original native creator screens and game callbacks remain authoritative.

## Player-observed defects

The November 19 v0.45 screenshots showed flat oversized custom panels, excessive empty space, small text and a live technical status message such as `Selected: SpouseEnabled = true`. The native FaceGen, original narrative, banner and clan screens appeared functional. The player reported Back from Attributes did not follow the expected sequence. After entering a castle, the clan UI showed a generated son, Alden, assigned to the castle, but he was absent from the castle's visible resident/recall choices. Two daughters were visible in other family lists.

## One deterministic Chapter II navigation contract

The old `CUNativeStatsVM.ExecuteContinue` had a special **Native mode** shortcut. It could finish the entire stat overlay directly from Starting Age. Changing mode on another page changed the meaning of Continue, confusing the perceived Back/Next chain.

New `CUNativeStatsNavigationPolicy` fixes the order independent of stats mode:

| Current page | Back | Continue |
|---|---|---|
| Starting Age (1/4) | Native life story | Attributes |
| Attributes (2/4) | Starting Age | Skills |
| Skills (3/4) | Attributes | Traits |
| Traits (4/4) | Skills | Confirm and resume original native Banner |

**Native, Balanced and Unrestricted change only edit permissions and budgets, never the page sequence.** The stored age and stat drafts remain in the same VM across intra-overlay Back/Next. Only the last step validates and saves. This is wired to the original two footer buttons with native-style event handlers. `cu_stats_navigation` logs from/to page indices and the current mode.

The guided World/House controller now anchors its cursor to the **current field key** whenever a selected role, spouse, child count or fief changes the conditional list. Inserted or hidden fields no longer make Back/Continue jump to unrelated questions.

The one native-stage boundary remains under `CharacterCreationManager.PreviousStage()` with teardown outside the button callback. This is source-tested, but a full native in-game Back/Forward test remains necessary.

## Alden and the castle: recorded residence is not a scene spawn

The code review established that `SandboxFamilyCreator.TryCreate` uses actual `HeroCreator.CreateChild`, sets the native parents and clan, and checks both parents' child lists. The supplied clan screenshot shows Alden exists. **That does not prove he is a spawned adult resident.**

Underage children remain in Bannerlord's `NotSpawned`/child life stage. They can have `StayingInSettlement=Caleus Castle`, yet not appear in `Settlement.HeroesWithoutParty` or the native **Recall** list. This rule applies to sons and daughters. Female child portraits or top-row entries are not evidence of an adult scene character. With a 30-year-old parent, a generated child is no older than 14 under the creator's parent-age validation, so Alden is still a minor.

The old `SandboxFamilyHome.PlaceAt` conflated these states. It treated underage residence placement as successful only if the child was present in `HeroesWithoutParty`. That cannot be expected from native minors. v0.46 now records each underage child's native residence **without modifying their HeroState, teleporting them into a party or forcing adult scene spawning**. Adult children retain their separate native `TeleportHeroAction` path.

New `SandboxFamilyResidencePolicy` explicitly classifies `underage_home_recorded`, `adult_hero_present`, `adult_hero_not_present`, and `different_residence`. Native adult recall eligibility is only reported for real active, nonprisoner, non-disabled adults. Logs now include **each child's native ID, name-independent gender, age, HeroState, recorded castle, scene registration and recall eligibility**, plus family totals for underage residence and adults seen in the scene roster.

The existing **CU: View my household portraits** castle/town menu opens a native-Hero gallery. v0.46 puts a new **Presence** status beside every living linked child, including Alden, instead of only a long parentage column. Underage entries explicitly say that they have a home but cannot join the party. The portrait gallery has a matching warm stone/brass frame. **CU: Make this my family's home** remains the explicit re-home action for existing saves, including adults who need a native teleport. It cannot turn a minor into a summonable adult.

This is a truthful location/visibility contract, not an unsupported promise that Bannerlord will spawn a minor in its lord's hall. A live test should inspect Alden's recorded age and HeroState. If an **adult** son is missing despite the re-home action, that is a distinct adult scene-registration defect and requires the campaign log to diagnose.

## Visual revisions on custom pages

- World and House now use a **centered 610x369 framed choice card** with thin brass rules, dark recessed selection area and the **same eight command bindings** for selecting values. The previous 1000-pixel flat brown strip and separate distant option buttons are gone.
- Side summaries use shorter framed panels instead of empty full-height slabs, and text is larger where space permits.
- Internal `Selected: SettingKey = value` messages are no longer shown to players. Other errors and validation messages remain visible.
- Native stats are now explicitly separated from Starting Age. Mode selection is shown only for Attributes/Skills/Traits. Age keeps its 18-to-80 one-year slider and safe one-year-button fallback.
- Stats rows are 56 px high with a stronger title/value hierarchy and descriptive text on a separate line. No native Skill/Focus cap changes.
- Culture has a readable recessed description area, a shorter left-hand list header and player-facing lore copy. The original culture controller, recovery action and selection callbacks remain intact.
- The original FaceGen and Bannerlord Family/Banner/Clan/Review/Difficulty screens are never forcibly reparented or covered.

These custom pages use built-in `ButtonBrush` and layered Gauntlet panels. **They are native-inspired, not yet texture-identical to Bannerlord's original engraved stone and wood art.** No unverified external sprite names were added.

## Verification, limitations and next live tests

[Windows CI 44485361161](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44485361161) at source commit `decdf9b6d3e9ca5b2a7be178627ca2791256df71` passed **747 policy checks** and all Python creator, family, XML, native-stage, installer, ZIP CRC and reference metadata audits. Compiled `net472` DLL had **0 errors and 0 warnings**. DLL SHA-256: `e71ce307ac40e04b084e58474e7d2392974887fea7b9680e728dc90095f77085`. Verified direct-install module ZIP SHA-256: `13d600c23d194d7c15e30cb8192cc83438127fb95356629e04abd3b0985267b6`.

**Open test cases:** revisit the exact 1366x768 and 1650x927 screens, check Gauntlet button hitboxes and text clipping, go Starting Age -> Attributes -> Back -> Age -> Next -> Attributes, switch Native/Unrestricted at Attributes, check Skills -> Traits -> Back -> Skills and confirm only from Traits. Test after changing spouse/child count and using guided Back/Next. In a fresh game with three underage children, open CU household portraits and confirm all three IDs, parents and residences. Check Alden's actual age and `HeroState`. Test an adult child separately with older valid parents, enter the owned castle and use **CU: Make this my family's home**. Save/reload, check existing family records remain unchanged, and confirm the original native campaign map/FaceGen/navigation callbacks still work.

The package is a **compiled, untested development candidate**, not a finished pixel-matched or in-game-accepted release. The module's internal development manifest remains `v0.38.0`.
