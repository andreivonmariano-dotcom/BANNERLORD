# v0.70l | King, seventh guided starting life

**Scope:** draft PR #93 stacked on PR #92. The King start exists only as `KingGuidedStart=true` for new guided campaigns. Historical 13 role IDs and old `Role=King` profiles remain parseable and keep their original behavior.

## Actual native crown

The guided King must use the exact native `king` Advanced Start type in a user-selected real kingdom. Before any family, officer or discretionary CU grants, read and check:

- `Clan.PlayerClan.Leader == Hero.MainHero` and `MobileParty.MainParty.LeaderHero == Hero.MainHero`.
- The actual player clan belongs to the requested native `Kingdom`, and `Kingdom.RulingClan == Clan.PlayerClan`. A title, trait, status label, kinship relationship or personal claim never substitutes for ruling-clan registration.
- The native kingdom and player clan are valid, active and non-mercenary. The native player is a genuine lord.
- At least one real native clan-owned fief exists. The game owns the kingdom's actual ruler, ownership and obligations.
- Re-run this verification again at final acceptance. Record observed native IDs, ruler and fiefs in the diagnostics, and refuse a false royal start.

The new creator fixes `NobleBirthPosition=Independent`, no fictional elder king, no second patron, no independent affiliation rewrite, `RetinueMode=RoleBased`, `StartingFiefMode=Native`, no requested extra fief, no transfer consent, no gold/influence overwrite and no extra troops. The selected native scenario's politics and revenue remain authoritative. A mod-created kingdom, instant treaty, fabricated court stipend or free army is outside this contract.

## Founding royal family

The **King** path now **requires a genuine native spouse and at least one genuine native child**. It permits 1 to 6 actual native children and optional siblings. The royal family requirement matches the new guided Lord's minimum, but the King retains a distinct native crown contract. Older sisters and younger brothers are permitted. A generated **older brother** contradicts the selected uncontested royal founder and is rejected before applying family or court changes. The separate future disputed-claim model requires verified native political action, and is not simulated by an older sibling's name.

Native spouse and every requested child use the existing marriage, child factory and reciprocal parentage checks. The guided King explicitly blocks any failed reciprocal marriage or missing native child before creating a court officer or appointing a governor. There is no invented royal family member. The user's spouse may have an eligible native noble clan when choosing a noble background, but `Role=King` does not force Lord's separate two-married-in-laws requirement.

## Exactly three real same-culture royal court Heroes

The guided King commissions a **Marshal**, **Royal Minister (native governor)**, and **Kingsguard**, all named, culture-trained and equipped native Hero companions. The Marshal and Kingsguard serve in the real main party and have field-officer identity (`Occupation.Lord`). The Royal Minister uses native stewardship training and `Occupation.Wanderer`, joins the actual player clan as a companion, then receives a genuine governorship through `ChangeGovernorAction.Apply(Town, Hero)`.

The governor position must be on an **already owned**, unbesieged town or castle with no incumbent. A deterministic lookup prefers the first vacant player-owned town, then castle. The start refuses a missing or occupied native governor seat before creating the new royal household. It does not remove an existing governor, seize a settlement, add a false capital, or grant the minister political power beyond native governorship. A successful appointment must be visible on both the minister's `Hero.GovernorOf` and the actual `Town.Governor`. The minister resides in the governed estate, not the King's traveling troop roster.

The native factory stages all three same-culture Hero companions from real noble donor templates and registers them through native companion and party/governor actions. Native starter wanderers are dismissed only through the existing verified Fire path. Final acceptance checks all three identities, distinct occupations, real clan registration, Marshal and Kingsguard in the main party, and the Royal Minister's actual governorship. Any missing office blocks other CU grants.

No fictional standing as regent, clan leader, diplomacy delegate, hereditary fief holder or sovereign replaces the native game authority.

## Save-backed royal household claims

`KingSuccessionRule` offers exactly two household ranking conventions, each explicitly chosen and serialized in the new starting profile:

- `EldestChild`: oldest living verified child first, regardless of sex.
- `MalePreference`: verified sons by descending age first, then verified daughters by descending age.

A descendant enters the family ranking only if both parents, reciprocal native `Children` memberships, alive state and actual founding-clan membership are verifiable. The guided King cannot begin without at least one verified native child, and its saved royal household record refuses an empty child roster. On death or family change the saved preference can change. Minor heirs are recorded **only as claimants**. No caretaker becomes a regent.

The read-only `SandboxKingSuccessionState` persists native founder Hero ID, initial ruling clan and kingdom, selected rule, preferred claimant, ranked verified children, observed native ruling clan and ruler and last observed campaign day through save key `CU_v070l_native_royal_household`. It recognizes native rulership that actually passed to the child, continued native rulership by someone else, and minor claimant reviews. The town/castle menu reports the saved custom and currently observed native authority.

**Do not confuse the ranked family claim with succession gameplay.** The native game still selects the legal kingdom ruler and clan leader and controls settlements, elections, succession, wages and army authority. No native death transfer, regency, crown election, or fief gift is executed by this patch. A real action with true save/reload acceptance is required before promoting this to an automatic royal inheritance feature.

## Required live acceptance

- Verify the native world and review creator, the `king` Advanced Start ID, exact kingdom selection, and genuine native ruling-clan ownership in a running game.
- Verify native fiefs and household status, clan/army authority and kingdom politics, and no accidental mercenary, extra treasury or additional fief transfers.
- Verify all three real same-culture court Heroes. Marshal and Kingsguard must appear once in the native main-party roster. Royal Minister must appear as the true native governor of an existing player-owned, unoccupied fief and not as a traveling party member.
- Check a royal start with all native fiefs governed. It must refuse before CU family or royal-court mutations, rather than dispossess a governor.
- Reject unmarried and childless guided King profiles. Test a married King with one child, larger multi-child households, an older sister, forbidden older brother, noble spouse ancestry when chosen, and complete native parent references.
- Save, quit and reload, then verify the same real IDs, royal ownership and court membership.
- Observe a native ruler death, an older daughter and younger son, male preference, child death, actual native monarch change, and an underage claimant without an invented regent.
- If a verified native API does not support direct succession, keep the no-transfer boundary and report it accurately.

**Build result is Gate B only** until live native acceptance. PR #93 stays draft. Do not merge, rename the historical v0.38.0 compiled package as a live verified release or update the installed installer pin.

## v0.70m | Mandatory dynasty and three-office court, pinned Windows gate

The user revised King after v0.70l: marriage and **at least one native biological child are mandatory**, not optional. The normal House flow presets both requirements and exposes the spouse name and first child name. Profiles and House confirmation refuse a childless or unmarried guided King. After native family creation and before any governor/court grants, a separate King family gate verifies a living reciprocal spouse, same player clan, and each requested child's real `Hero.Mother`, `Hero.Father`, reciprocal parent `Children`, culture-compatible generated identity and exact requested birth age. Older King profiles remain unchanged.

The guided court is now three **distinct native Hero companions**: a culturally trained Marshal and Kingsguard in the main party, and a Royal Minister at a genuine native governorship. The minister is recruited into the clan but not inserted into the party troop roster. The game-native `ChangeGovernorAction.Apply` is used on a deterministic unoccupied town or castle already held by the ruling clan. `Hero.GovernorOf` and `Town.Governor` must agree, the fief remains owned by the player clan, and the minister remains a real active clan companion. If every native player fief already has a governor, the guided King start is refused at preflight without firing an existing governor. The office names and combat/stewardship training are culture-specific. Only native action results count, not a decorative court title.

- **Final pinned Windows CI:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44892404942
- **Exact compiled source:** `80d3ea6986b132dc341ab91eb275aeaea8611a09`
- **Independent gameplay-policy checks:** **1,057 passed**.
- **New King native-crown, required-family, three-office and governor source regression:** **passed**.
- **Deterministic campaign-model audit:** **210,240 modeled campaign days, 1,159,206 assertions and 7,488 checkpoints**.
- **Pinned game-reference compiler:** **0 errors and 0 warnings**.
- **DLL SHA256:** `a8559e967162aef92f5649ed318e7cf929fd26d23d354c6f1b37d725e9f6b856`.
- **Compiled, game-untested candidate:** https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44892404942/artifacts/14702815878 . The historical internal v0.38.0 label has not changed.
- **Gate remains B:** No live Bannerlord new-game start, real governor settlement placement, native army role, royal biological family, real crown succession, capture/death or save/quit/reload has been verified. The Marshal and Kingsguard are native party Hero companions, not automatically assigned independent army command or a permanent bodyguard unit. Royal Minister's governor link compiles and is source checked but must be observed in game. No vassal, fief, sovereign or regency rewrite is claimed.

PR #93 remains draft and unmerged. The installed candidate pin is unchanged, and no new live-tested release is declared.

## v0.70n correction | Kingsguard is one Hero, not troops

The Kingsguard remains **one named, culturally trained, equipped native Hero companion** who accompanies the King. It is one of the three royal court Heroes, alongside the Marshal and Royal Minister. It is not a new troop tree, formation, unit type, recruitable guard squad or garrison transfer. No Kingsguard troop counts, payroll, casualty ledger or guardsmen recruitment menu exist. The experimental troop detachment model was **deleted** before acceptance. The mod does not claim the Kingsguard Hero has battle-engine bodyguard AI or guaranteed protection from lethal attacks.

The royal council can convene only at a genuine player-owned town or castle that the Royal Minister governs natively. The court-seat selector prefers a verified native kingdom homeland when exposed by the pinned API, then chooses an actual player-owned town/castle by a stable order. If the preferred seat is already governed, the guided King preflight refuses the minister appointment rather than firing the incumbent or moving the court to a different property. This is a **CU royal court seat** designation, not a rewrite of Bannerlord's kingdom capital.

A council meeting requires the King physically at that seat, the Marshal and the single Kingsguard Hero present in the King's party, and the real Royal Minister holding the corresponding native governor office. The meeting count and the four genuine Hero IDs are saved through `CU_v070n_single_hero_royal_council`. The council can review those verified officeholders. It does not replace Bannerlord's native kingdom policies, voting, army command or legal crown succession.

## v0.70o | Seven-path pre-release native acceptance

The read-only `scripts/audit_v070_seven_live_start_evidence.py` audits **Wanderer, Outlaw, Mercenary, Soldier, Knight, Lord and King** separately. It requires a same-session successful native starting contract plus role-specific native event records, a post-quit/reload identity observation, and a separate live native UI screenshot. It deliberately returns **BLOCKED** when evidence is absent, when Soldier never actually enlisted with its supported provider, or when Knight younger-sibling native clan joining remains unimplemented.

The CI regression `scripts/test_v070o_seven_game_acceptance_failclosed.py` confirms that even seven simulated `PASS` labels remain blocked without real runtime actions and reload evidence. A successful compile, model simulation, scripted session fixture, or cosmetic role name does not promote the release to live-game acceptance.

This project execution context has **no running Bannerlord client, native save/reload, or seven verified playthrough evidence bundles**. As a result the complete seven-path **live gameplay acceptance remains BLOCKED**. Do not merge PR #93, change the installed release pin or claim real-game acceptance without those observations.
