# v0.62a | Named native noble elder and culturally specialized household officers

Experimental update following the successful v0.61a Windows workflow 44769935656. Native game acceptance remains pending.

## Noble lineage identity

- World retains noble origin choices and optional exact native elder ID (Advanced).
- House now **previews a possible older noble by actual Hero and Clan name** after culture and native age are known, using a read-only eligibility scan. Kingdom allegiance is not yet installed by native phase 8 at this point, so the label explicitly says provisional.
- During phase 9, the same deterministic selection runs with the actual player-clan kingdom requirement. Successful native parent linkage is confirmed through a player-facing notification naming the lord or ruler and clan. Hero ID and display name are written to campaign save data and a diagnostic `cu_dynastic_named_elder_confirmed` event.
- A missing eligible native elder produces an explicit refusal. No fictional named lord or forged parent is created.

## Cultural officer identity and equipment

- Native same-culture, sex-matched noble CharacterObject templates remain required. No third-party item ID is introduced.
- Each of the six cultures has a distinctive target equipment profile: Vlandian Lancer, Sturgian Axeguard, Battanian Longbow, Imperial Shieldguard, Khuzait Horse Archer, Aserai Skirmisher.
- Native BattleEquipments are ranked by the real installed ItemObject categories, such as mount, bow, shield, polearm and armor, and the best role/culture-fitting set is copied into the **new** officer. Gear and donor provenance are logged. If the native donor pool lacks appropriate equipment, actual available native gear takes precedence over invented items.
- Officer training now combines office specialization with cultural weapon practice. Cultural personality traits differ, including Vlandian honor, Sturgian valor, Battanian mercy, Imperial honor, Khuzait calculation, Aserai generosity. Court positions receive different secondary traits from military positions.
- Native companion replacement preflight, family/sibling relationships, party role assignment and Advanced setting choices remain in force.

## Testing

Static regression `scripts/test_v062_named_dynasty_and_officer_gear.py` and pure policy checks cover preview/commit path and equipment/personality selection. Windows CI compiles against pinned v1.5.7 assemblies. Actual same-culture kit suitability and named elder availability depend on real campaign contents and must be checked in a fresh Lord playthrough. PR #88 stays draft.
