# v0.70d | Mercenary starting-path native contract and household audit

**Date:** 21 November 2026. **Status:** coded candidate, source/CI and native game acceptance pending. Draft PR #93 on `audit/v069e-current-gameplay-and-visual-acceptance`. Do not merge or change the installer pin before observed game acceptance.

## Goal: third reviewed path

After **Wanderer** and **Outlaw / Bandit**, make **Mercenary** an independent paid fighting company allied to one **player-selected native kingdom**. This is neither an enlisted Soldier under an NPC commander nor a landed Knight or Lord.

- The first three visible choices are now Wanderer, Outlaw, Mercenary. Unfinished legacy roles remain selectable for compatibility until redesigned.
- Add actual profile `Role=Mercenary`, mapped to Advanced Start `StartType=mercenary`; preserve historical `Soldier -> mercenary` and all old saved Soldier fields.
- Require native kingdom selection from the actual Advanced Start list. The king and fiefs of the source world are not rewritten.
- The player's own `Clan.PlayerClan` and `MobileParty.MainParty` stay player-led. They **serve** a kingdom as mercenaries. Do not merge the player's clan into a patron clan or declare the player enlisted in another lord's army.
- No named patron requirement for the Mercenary path. Kingdom mercenary affiliation is the contract.
- No automatically generated noble lineage, royal standing, clan fief, owned residence, privileged household officers, influence award, arbitrary extra troops, custom gold amount or fake contract income. Starting equipment and soldiers remain native Advanced Start state. Existing troop wages and food costs remain native.
- The family editor remains usable: optional spouse, children and siblings with native parent and marriage links. No owned home is promised to landless Mercenary family. When the player requests siblings and parents need creation, new genealogical anchors remain inactive, without turning the ancestry into living court members. Adult commoner spouse/children should not be presented as native aristocratic officers.
- Native player age, full stats, FaceGen, traits and appearance remain in the original creator, unchanged.

## Actual game contract, before any custom family mutation

The pure `SandboxMercenaryStartPolicy.Validate` requires all of:

1. New role is `Mercenary` with a nonempty intended native kingdom.
2. Observed `Clan.PlayerClan.Kingdom.StringId` exactly matches the chosen kingdom.
3. **`Clan.PlayerClan.IsUnderMercenaryService == true`** in the **actual installed game**. A regular vassal in the same kingdom does not satisfy this.
4. The player is the actual `Clan.PlayerClan.Leader` and the `MobileParty.MainParty.LeaderHero`.
5. The clan does not rule the kingdom and owns zero settlements.

`SandboxStartingLifePreflight.Check` checks these at phase 9 *before* family/retinue/item mutations. `SandboxOriginBehavior` checks again after the requested start operations and records `cu_mercenary_native_preflight`, `cu_mercenary_company_native_verification` and the final `sandbox_start_contract`. Failure refuses custom grants without falsifying the campaign's native state.

**Native contract means real game faction service.** We do **not** invent a salary number, 90-day term, artificial battle payment, zero wages or an enlistment rank. Native game rules control contract compensation, payments, disputes, terminations and troop upkeep. A role flag is not proof of those future events.

## Versioned fields and migration safety

`SandboxOriginProfile.MercenaryGuidedStart` is stored in the external next-game origin profile and defaults false; `ResetForNewCampaign` clears it. The guided creator sets the flag only for `Role=Mercenary`. Previous saved `Soldier` careers remain `mercenary` native start types **without** receiving the new profile flag or stricter new-game constraints. No previous campaign save is converted.

The ordinary House screen can create family for Mercenary, but estate and resource settings are hidden and their invalid stale values fail profile validation. The existing native marriage model must accept a requested spouse; creating a family label does not bypass native marriage eligibility.

## Native test plan

1. Launch `Mercenary`, choose Vlandia. Check native advanced `StartType=mercenary`; exact employer kingdom; `IsUnderMercenaryService`; player clan/party leadership; zero owned fiefs; no listed vassal title.
2. Repeat with another employer kingdom. Reject any misselected faction, invented client lord, missing contract or player-enlisted NPC-party state.
3. Compare actual initial troops, cash, equipment, party wages and contract income to native Advanced Start values. No CU freebies, no zero-wage exploit and no invented fixed contract term.
4. Test spouse-only, spouse with two age-valid children, and optional siblings. Verify actual two-way Hero family links, non-noble status and no extra active ancestor-court members.
5. Save, close, reload, advance at least 30 real game days. Verify identical native company, contract, treasury and person IDs. No repeated family creation, wage grants or new clan identities.
6. Test native contract termination, switching employer, capture, player illness, commander death, army call-up and a wage delinquency scenario with actual game mechanics.
7. Verify the unchanged existing Soldier legacy profile, Wanderer no-House/optional companion, and Outlaw six-bandit opening reserve.

**Evidence policy:** only native gameplay session logs/screenshots and save/reload can certify actual wages, diplomacy, AI, term and stability. A compiled reference build is necessary but not a gameplay PASS.
