# v0.64a | Starting Life foundations and Living Persons

**Development target:** `CalradiaUnified`, Bannerlord 1.5.7.125802, branch `feature/v057-automatic-session-diagnostics`, draft PR #88.

**Scope:** First implementation milestone addressing all three requested directions. It does **not** claim full troop enlistment, changing medieval law or interactive dialogue. Those require separate game-tested campaign systems.

## World and House redesign

The visible World flow still asks about campaign scenario, occupation/start role, kingdom where required and noble birth. Read-only dossier text now identifies **birth, native social station, real occupation, early household, current service obligations, and chosen ambition**. The distinctions are based on the *actual native ASO start*, not fictional titles. Thus a Soldier is labeled a mercenary company leader rather than an enlisted private, a Knight is a vassal knight, and a second son is a cadet with a distinct native vassal fief rather than a landless dependent. Kingdoms with missing native rank support are not silently altered.

World **Advanced Settings** now offers early household (Rural, Town, Garrison, TradingHouse, Court, Displaced) and ambition (Duty, Recognition, Security, Prosperity, Independence, Land, Reconciliation). Auto chooses role-appropriate defaults. Selected household shapes the automatically generated family scenario and childhood context. Ambitions are *recorded for future interactions* and do not pretend to grant quests or existing social encounters. These values are reset between fresh campaigns, persisted in the profile and shown in House confirmation. Existing native Culture, appearance, narrative, Age/Stats, Banner, Clan and Review remain intact.

## P0 genealogy and kin safety

A second or third son requires exactly the native number of older male children from the two actual parents. The noble-father selector now counts older male siblings **planned in House** before it generates any missing native brothers. It rejects impossible age constraints, overcounts and conflicting older brother assignments. Planned older sisters are allowed. House creation checks the final brother rank after creating requested siblings, and final acceptance checks exact native sibling identity, sex and age. No native father or mother is fabricated to validate an already selected king/lord lineage.

Noble spouse origin now excludes the player father and mother's current and birth clans. Choosing a forbidden clan explicitly produces a close-family refusal. This is a conservative in-game safety filter, not a complete genealogical consanguinity model.

After native phase 8, a read-only preflight checks noble father/elder existence, ability to create needed brothers, real patron availability, permitted player marriage and consistent sibling birth order. A failure records `sandbox_start_contract=result=FAIL` **before applying any CU phase-9 grants**. The native phase-8 start cannot be rolled back, so such a failure still requires beginning a new campaign with a different selection. Subsequent native failures remain detectable through final contract logs, not transactionally reversible.

## Shared Living Persons

`SandboxLifeHistoryState` is pure, versioned `CU_LIFE_1`, bounded and string-serialized. It stores up to 64 person dossiers, 192 factual bonds and 96 recorded events per campaign. Names, ages, observed station, native occupation, clan, culture, declared origin, ambition and concise biographies are retained. Unicode and separators are encoded safely. A corrupted/unknown save block is not interpreted as valid records.

`SandboxLivingPersons` snapshots **actual living Heroes** after House and officers are applied. It records parents, spouse, children, shared-parent siblings, clan companions, any assigned personal commanding lord and native trait values. Actual parent/marriage/companion bonds are marked `VerifiedNative*`. Creator-selected upbringing and ambition have their own labels. Unknown prior histories are explicitly unmodeled. `SandboxOriginBehavior` persists the ledger under `CU_v064_living_persons`, allowing future dialogue, kin duties, obligations, memories and NPC goals to read a common source.

No event-based daily relationship changes, troop AI orders, simulated wages, estate succession or expanded character dialogue are implemented in this milestone. An optional living-character UI or integration into native Encyclopedia is a future game-side step.

## Verification and regression

`Tests/PolicySmoke` checks native-station mapping, profile roundtrip, birth-rank arithmetic, contradictory sibling ages, bounded biography serialization, Unicode, relationship deduplication and history event parsing. `scripts/test_v064_life_dossier_and_family_integrity.py` checks source wiring, preflight order, exact family checks, native spouse family exclusions, and saved life histories. Windows CI builds against pinned game assemblies. In-game acceptance: test Lord second/third son with additional older male siblings, a prohibited same-family noble marriage, Soldier/Knight patron starts, and save/reload of the v0.64 history key. Back up existing saves. Keep draft PR #88 unmerged until actual game evidence.
