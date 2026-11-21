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

The **King** path permits an optional spouse, 0 to 6 actual native children, and optional siblings. It does not inherit the Lord path's mandatory marriage and child rule. Older sisters and younger brothers are permitted. A generated **older brother** contradicts the selected uncontested royal founder and is rejected before applying family or court changes. The separate future disputed-claim model requires verified native political action, and is not simulated by an older sibling's name.

Native spouse and children, if selected, use the existing marriage, child factory and reciprocal parentage checks. There is no invented royal family member. The user's spouse may have an eligible native noble clan when choosing a noble background, but `Role=King` does not force Lord's separate two-married-in-laws requirement.

## Exactly two real same-culture court Heroes

The guided King commissions **Marshal** and **Chamberlain**, both as new, named, culturally trained and equipped native Hero companions. The Marshal has genuine `Occupation.Lord` for field-officer identity, and the Chamberlain has `Occupation.Wanderer` with an actual native companion and party appointment. The existing source chooses native same-culture lord donors, and the native factory creates and registers these new Heroes. Every officer is checked for real clan membership, companion identity, native party roster inclusion, culture, occupation and named office. Remaining unrelated starting native wanderers are removed using the existing verified sequential native Fire method.

No fictional standing as regent, clan leader, diplomacy delegate, hereditary fief holder or sovereign replaces the native game authority.

## Save-backed royal household claims

`KingSuccessionRule` offers exactly two household ranking conventions, each explicitly chosen and serialized in the new starting profile:

- `EldestChild`: oldest living verified child first, regardless of sex.
- `MalePreference`: verified sons by descending age first, then verified daughters by descending age.

A descendant enters the family ranking only if both parents, reciprocal native `Children` memberships, alive state and actual founding-clan membership are verifiable. Childless Kings have no preferred heir, not an invented distant relative. On death or family change the saved preference can change. Minor heirs are recorded **only as claimants**. No caretaker becomes a regent.

The read-only `SandboxKingSuccessionState` persists native founder Hero ID, initial ruling clan and kingdom, selected rule, preferred claimant, ranked verified children, observed native ruling clan and ruler and last observed campaign day through save key `CU_v070l_native_royal_household`. It recognizes native rulership that actually passed to the child, continued native rulership by someone else, and minor claimant reviews. The town/castle menu reports the saved custom and currently observed native authority.

**Do not confuse the ranked family claim with succession gameplay.** The native game still selects the legal kingdom ruler and clan leader and controls settlements, elections, succession, wages and army authority. No native death transfer, regency, crown election, or fief gift is executed by this patch. A real action with true save/reload acceptance is required before promoting this to an automatic royal inheritance feature.

## Required live acceptance

- Verify the native world and review creator, the `king` Advanced Start ID, exact kingdom selection, and genuine native ruling-clan ownership in a running game.
- Verify native fiefs and household status, clan/army authority and kingdom politics, and no accidental mercenary, extra treasury or additional fief transfers.
- Verify exactly two true same-culture court Heroes in the party, and no duplicate unrelated starter companions.
- Run married and unmarried start cases, childless and multi-child royal households, older sister and forbidden older brother, optional noble spouse ancestry, and real parent links.
- Save, quit and reload, then verify the same real IDs, royal ownership and court membership.
- Observe a native ruler death, an older daughter and younger son, male preference, child death, actual native monarch change, and an underage claimant without an invented regent.
- If a verified native API does not support direct succession, keep the no-transfer boundary and report it accurately.

**Build result is Gate B only** until live native acceptance. PR #93 stays draft. Do not merge, rename the historical v0.38.0 compiled package as a live verified release or update the installed installer pin.
