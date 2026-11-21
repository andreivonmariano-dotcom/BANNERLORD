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

The independent founder-house contract permits an optional spouse and native-generated children and siblings. No elder native lord is claimed as the player's personal clan leader. An older **brother** among newly generated siblings is refused, but an older **sister** is valid. The older-sibling gate is shared with existing independent Lord household validation. This is a starting seniority rule, not an act that changes native inheritance or installed succession politics.

The native spouse and child-parent links must pass the existing family gates, and home selection cannot promise an unowned fortification. Choosing `OwnedFief` or a named residence still requires a real eligible native clan-owned home. An estate and a family residence are different facts.

## Two real native household officers

The guided Lord starts with `RetinueMode=RoleBased`, with exactly the two existing culture-trained offices `MasterOfArms` and `Knight`. The existing native factory stages legitimate same-culture Hero templates, trains and equips them from native donor data, uses `AddCompanionAction` and `AddHeroToPartyAction`, and checks confirmed party and clan registration. Native starter wanderers are removed only through the existing verified `RemoveCompanionAction` path. The new Lord gate explicitly verifies:

- Successful full two-officer receipt, exactly two remaining native clan companions, one named Master of Arms and one named Knight.
- Same culture as player, active real Heroes with native lord occupation and matching `Clan.PlayerClan` and `CompanionOf` references.
- Each officer occurs exactly once in the real `MobileParty.MainParty.MemberRoster`.

No office is merely an invented UI label. Their cultural training, native weapons, recruitment, dismissals and save persistence require real game acceptance before merge. These officers do **not** receive a fief, native sovereign power or permanent clan offices that the engine cannot represent.

## Live-game acceptance required

Gate B is a pinned reference-assembly compilation, C# policy testing, native scenario source review, and deterministic campaign simulation. It is **not** native campaign play.

Before raising the gate, complete a real new-game run with the Lord path: world options, character age and culture, native `vassal` start, real selected kingdom, player-clan leadership, live clan and fief ownership, no accidental mercenary contract, the two officers in the same native party, actual spouse/children/sibling genealogies, residence authority, family save/quit/reload, real army invitation and vassal diplomacy, and retained native estate after reload. Negative tests must reject an unlanded native start, king start, wrong kingdom, missing party leader, missing officer or invented fief grant.

The working branch is the draft PR #93 stacked on PR #92. Do not merge, rename the internal v0.38.0 compiled-only package as a verified release or change the user's installed artifact pin.
