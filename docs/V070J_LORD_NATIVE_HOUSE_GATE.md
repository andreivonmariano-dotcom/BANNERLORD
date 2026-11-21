# v0.70j | Lord: verified native vassal house

## Chosen seven-path contract

Lord is the **sixth** guided Starting Life. It means the player actually leads their own native noble clan. The player is a **vassal**, not a king, the head of someone else's clan, a mercenary captain, an enlisted private or the owner of a fabricated estate.

This guided contract is separate from historical `Role=Lord` records. The new profile flag `LordGuidedStart` is serialized but defaults to false in old saves and resets on every new guided campaign. The historical `SandboxOriginProfile.Roles` identifiers and older Lord native mapping remain intact.

## Native start and land

The creator requests the exact native Advanced Starting Options `vassal` start and a real kingdom ID. It does **not** make a separate `ChangeKingdomAction`, apply a false title, create a kingdom or transfer another noble house's castle.

At preflight, after native phase 8 but before family and other CU actions, check:

- Actual native start type is `vassal`, the player's clan kingdom is the chosen real kingdom, and the same existing clan is not the kingdom's ruling clan.
- `Clan.PlayerClan.Leader` and `MobileParty.MainParty.LeaderHero` are `Hero.MainHero`.
- Native player is a real lord and the clan is active, non-rebel, non-minor, non-bandit and not under mercenary service.
- The player's native clan genuinely holds **at least one fief**. This is the contract implemented by the pinned v1.5.7 Advanced Start and original acceptance matrix. The RFC permits landless noble houses only once a separately verified native-start contract supports them. Never gift an unrelated lord's holding to fake this result.
- The final campaign state gets checked again after family and retinue setup. Failed identity or estate checks produce explicit diagnostics and refuse further CU grants.

For new guided Lord profiles, prohibit `StartingFiefMode` other than `Native`, fief ID, transfer consent, bonus gold, bonus influence, extra troops, personal patron, second noble birth-lineage claim, and role-affiliation overrides. The original native castle/town, political income, native equipment and affiliation remain authoritative.

## Family and household

The guided Lord's household **requires one genuine native spouse and at least one genuine native child**. The creator selects marriage and one child by default, and it never permits a childless or unmarried guided Lord to reach native start acceptance. The player chooses spouse and child names, and can request more children up to the existing family limit. Brothers and sisters remain optional. No elder native lord is claimed as the player's personal clan leader. An older **brother** among newly generated siblings is refused, but an older **sister** is valid. The older-sibling gate is shared with existing independent Lord household validation. This is a starting seniority rule, not an act that changes native inheritance or installed succession politics.

The native marriage must be reciprocal, and the spouse must actually belong to the player's clan. Every requested child must be a distinct, living native Hero. The child must match the requested name, sex and age, belong to the same player clan, and have complete mother, father and reciprocal parent-to-child references. The guided Lord's grant barrier checks this **before officers or additional grants**. The native spouse and child-parent links must pass the existing family gates, and home selection cannot promise an unowned fortification. Choosing `OwnedFief` or a named residence still requires a real eligible native clan-owned home. An estate and a family residence are different facts.

## Two real native household officers

The guided Lord starts with `RetinueMode=RoleBased`, with exactly the two existing culture-trained offices `MasterOfArms` and `Knight`. The existing native factory stages legitimate same-culture Hero templates, trains and equips them from native donor data, uses `AddCompanionAction` and `AddHeroToPartyAction`, and checks confirmed party and clan registration. Native starter wanderers are removed only through the existing verified `RemoveCompanionAction` path. The new Lord gate explicitly verifies:

- Successful full two-officer receipt, exactly two remaining native clan companions, one named Master of Arms and one named Knight.
- Same culture as player, active real Heroes with native lord occupation and matching `Clan.PlayerClan` and `CompanionOf` references.
- Each officer occurs exactly once in the real `MobileParty.MainParty.MemberRoster`.

No office is merely an invented UI label. Their cultural training, native weapons, recruitment, dismissals and save persistence require real game acceptance before merge. These officers do **not** receive a fief, native sovereign power or permanent clan offices that the engine cannot represent.

## Live-game acceptance required

Gate B is a pinned reference-assembly compilation, C# policy testing, native scenario source review, and deterministic campaign simulation. It is **not** native campaign play.

Before raising the gate, complete a real new-game run with the Lord path: world options, character age and culture, native `vassal` start, real selected kingdom, player-clan leadership, live clan and fief ownership, no accidental mercenary contract, the two officers in the same native party, mandatory spouse and at least one child with exact native genealogies, optional sibling genealogies, residence authority, family save/quit/reload, real army invitation and vassal diplomacy, and retained native estate after reload. Negative tests must reject an unlanded native start, king start, wrong kingdom, missing party leader, missing officer or invented fief grant.

The working branch is the draft PR #93 stacked on PR #92. Do not merge, rename the internal v0.38.0 compiled-only package as a verified release or change the user's installed artifact pin.

## Mandatory family update and Windows source gate, 21 November 2026

The guided Lord creator now preselects **married** and **one child**. The spouse toggle is removed from the guided Lord's House sequence, the child's minimum is one, the spouse name and first child's name are visible without opening Advanced, and siblings remain an optional separate choice. Other starting paths retain their prior family controls. Profiles saved by legacy Lord starts retain the original `LordGuidedStart=false` behavior.

`SandboxLordStartPolicy.ValidateRequiredFamily` refuses an unmarried or childless guided Lord at profile validation and House confirmation. After native marriage and birth, `VerifyGuidedLordNativeFamily` checks a reciprocal living spouse in `Clan.PlayerClan` and a distinct verified native Hero for every requested child. Each must match the requested name, sex and age and appear in both actual parents' child collections. Any failure stops before starting Lord officers or additional CU grants. This does not invent a spouse, a family record or an heir by altering the creator's labels.

- **CI passed:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44888333663
- **Compiled source commit:** `ecdfdcef06978fe673ddcbc63ae4af0c1988b813`
- **Result:** 1,026 C# gameplay policy checks passed; Lord family source gate passed; audit simulation passed with 210,240 modeled days, 1,159,206 assertions and 7,488 checkpoints; native reference compiler reported 0 warnings and 0 errors.
- **DLL SHA-256:** `9fb3f2038032219a838ca5b212971307a78741df8795e0fef56969162a0c5e39`
- **Compiled, game-untested candidate:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44888333663/artifacts/14700499263 . Its historical internal label remains v0.38.0.
- **Limit:** highest acceptance remains **Gate B**. The optional, nonblocking Knight clan metadata reflection probe still reports a missing reference dependency, independent of this successful Lord compile. Actual native new-game marriage, child links, household residence and save/reload remain untested. PR #93 stays draft and the installed artifact pin stays unchanged.

## v0.70k | Native noble in-laws and saved child inheritance

**Q1: The spouse must have a genuine native noble birth family.** A newly created adult spouse is drawn from an eligible, existing noble clan in the selected native kingdom, not from a new fictitious parent or an existing noble forced to marry. The native in-law clan must be distinct from the player's own clan and close parental clans. Both actual native parents must be alive, noble, members of the chosen birth clan, a reciprocal married couple, and old enough to parent the new spouse. The prior NobleRandom fallback that accepted a clan name without parents is **blocked for guided Lord only**. NobleChosen requires a user-selected, eligible real clan. Read-only preflight checks the native ancestry before any new spouse Hero is allocated. After the marriage, the campaign checks the spouse's real `OriginClan`, both `Father` and `Mother`, the parents' reciprocal marriage and their actual `Children` collections. No parent, existing NPC, clan leader or fief is replaced. Missing native nobles must stop the start rather than invent the family.

**Q2: Starting household inheritance order.** The guided Lord can select `EldestChild` (default: eldest living verified biological child first, regardless of sex) or `MalePreference` (verified sons by birth age, then verified daughters by birth age). Each candidate must be a real living native Hero of the founding clan, with both native parents and reciprocal child links. Equal observed ages use stable recorded family order and Hero ID to settle ties. The rank is recalculated upon native life changes. The first-ranked minor remains a *claimant*, with no invented regent or premature appointment. If that child dies, the next qualifying child takes the first position in the household claim order.

`SandboxLordInheritanceState` saves the founder, spouse, birth clan, kingdom, selected custom, first-ranked claimant, ranked native child IDs, last observed real native clan leader and interpretation to `CU_v070k_lord_native_heir_claims`. Campaign days and a town/castle review menu report changes. It does **not** use the external pregame profile to change an active save.

**Native political boundary:** These rules rank household claims, not legal entitlements or actual property ownership. `Clan.Leader`, settlement ownership, kingdom-election systems and native regency are not silently rewritten. On the founder's death, the mod reports whether the observed native clan leader is the preferred child, someone else, or unverified, without inventing a transfer. A tested, safe native succession action and live save/reload proof are still required for an enforced transfer. The guided Lord's status remains Gate B until true game acceptance.

**Extra live checks:** Vlandian and other cultures with eligible married native in-laws, NobleChosen exact-clan selection, no eligible native parents (must refuse), noble parent death, both inheritance customs, eldest daughter and younger son, minor inheritor, claimant death, native leadership change, founder death, and save/quit/reload. Verify no second clan owner, no fief or political standing granted by household claim order, and no mutation of unrelated noble families.

## v0.70k | Pinned Windows verification (21 November 2026)

- **Final passing source run:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44889522117
- **Exact compiled source commit:** `ffaaeee47b84c3c590d7a1be8072c746e41f7e9c`.
- **Source-only regressions:** the new native noble-marriage and save-backed heir rule check passed, together with the earlier Lord, Knight, Soldier, Mercenary, Outlaw and Wanderer checks.
- **C# independent gameplay policies:** **1,037 checks passed**.
- **Model audit:** 210,240 modeled campaign days, 1,159,206 assertions and 7,488 checkpoints.
- **Pinned Bannerlord reference compile:** **0 errors, 0 warnings**.
- **Compiled DLL SHA-256:** `74d724975ca7e0fbdb4f88b01324323f22291a485587924a493e4e679b48acf1`.
- **Compiled, game-untested artifact:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44889522117/artifacts/14700839648
- **Exact acceptance limit:** Gate B. The separate experimental native-clan reflection probe remains inconclusive, and the required Lord noble in-laws, Native Hero origin-clan retention, character-creator UI, young-heir claims, live death succession, and actual campaign save/reload still require real-game evidence. No automatic clan/fief transfer has been implemented. Draft PR #93 remains unmerged, and the installed artifact pin remains unchanged.
