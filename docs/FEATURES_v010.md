# Calradia Unified v0.10.0: bounded recruitment and garrison training

## Player-visible functions

### Culture-specific local recruitment limit (M2)

At a player-owned town or castle, a player with `EnableRecruitment=true` may buy one local basic troop for 200 gold. The recruit follows the settlement's culture, not its current kingdom. New `LocalRecruitDailyCap=2` restricts buys by settlement and campaign day. Money, troop capacity, ownership and local troop identity are checked before the action. The counter is kept independently of kingdom levy recruitment.

### Training defenders in an existing player garrison (M4)

At an owned, unbesieged town or castle that already has a garrison party, a player with `EnableGarrisonTraining=true` may choose **CU: Train 2 garrison defenders (250 gold + 5 depot grain)**. Training adds two settlement-culture basic troops to the actual existing garrison party roster. It deducts five grain from that settlement's saved depot and 250 player gold. Training is limited by `GarrisonTrainingDailyCap=4` defenders per settlement per day and `GarrisonTrainingMaxTroops=250`. Missing garrisons are not spawned by this feature. Training refuses sieges, hostile owners, absent troops or resource shortages. It does not control vanilla garrison AI or training XP.

The two mechanics are separate from the existing kingdom levy. Both mutating systems remain disabled until explicitly enabled in the module's INI file and supervised in-game.

## Persistence

The `CU5` primitive state preserves two additional council counters, `LocalToday` and `GarrisonToday`. Old `CU3`/`CU4` council records load with both counters zero. Daily reset is shared with the existing council tick. The existing `CU_v030_civic_state` save-data key is retained. Actual compatibility with older in-game saves is not established by policy tests.

## Acceptance

- Test local levy purchase, insufficient gold, party cap and third same-day purchase rejection in every playable culture.
- Train exactly two defenders and verify **garrison party roster**, player gold and settlement depot stock. Confirm no phantom party is created.
- Reject training during a siege, when the garrison party is absent, after cap, and without gold or grain.
- Save/reload before the daily reset and confirm both counters persist. Confirm the next day's reset in-game.
- Inspect a fresh campaign, battle transition, settlement exit, and 30-day simulation under the tested Bannerlord version.
- Treat the automated campaign simulator as a model of production policy code, not an engine or save compatibility test.

## Rights and historical coverage

`RealisticRecruitment` and `ImprovedGarrisons` are historical feature references, not copied code or claims of complete replacement. The original 78-mod acceptance checklist remains unreplaced.
