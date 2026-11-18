# M1 implementation notes: bounded depot grain and physical convoys

This iteration hardens the existing depot and physical convoy code. Its changes compiled successfully in [GitHub Actions run 44143396054](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44143396054) and passed independently executed policy checks. This is **Gate B only**. In-game convoy behavior remains unverified until D1.1 through E1.1.

## Exact runtime changes

- A player depot cannot accept a five-grain deposit that exceeds its configured capacity.
- Abstract shipments require room at dispatch. Arrival credits up to destination capacity, then refunds safe overflow to a still-owned source depot. Any unplaceable grain is logged as lost. Abstract shipments remain ledger transfers, not visible actors.
- A physical convoy requires an owned source and destination, a nonbesieged recipient, enough source grain, capacity for its cargo, available guards and an open active slot.
- Dispatch uses an actual `MobileParty` with a guard roster and actual grain in its `ItemRoster`. The source ledger is debited after creation and movement order success.
- At destination, unloading removes grain from the real party before crediting the destination depot. A partly full depot accepts only the space available. Undelivered grain remains aboard a visible party and can unload at later daily ticks. A destroyed or timed-out party forfeits its remaining cargo.
- The manifest and remaining cargo stay in the serialized `CU4` state format, preserving the prior save-state structure. No unrelated gameplay feature toggles are enabled automatically.

## Known runtime questions

The target game's `CreateCustomPartyWithTroopRoster` and movement adapter compiled in v0.8.0, but actual spawning, movement, arrival event timing, save rebinding, and destruction need in-game verification. The existing movement adapter uses reflection to accommodate signature differences. Port it to an exact public API after engine verification. This source update does not claim convoy completion in Bannerlord.

## Test scenarios

1. Deposit 5 with depot stock 95/100, then try stock 96/100. Expect the second operation to reject without subtracting carried grain.
2. Dispatch 10 grain from a player-owned town to another with at least 10 free capacity. Expect an actual guarded party on the world map, 10 grain removed from the source ledger, and party inventory holding 10 grain.
3. Fill the recipient depot before convoy arrival. Expect the party to remain at the destination with undelivered grain. Withdraw stock, advance one day, and expect only free space to be unloaded.
4. Destroy the convoy en route. Expect the outstanding manifest removed, with no credit at the destination.
5. Save/reload with the convoy moving and with it holding cargo at the destination. Check stable party ID, inventory, manifest and ledger state.
6. Conquer the recipient while the convoy travels. Expect no credit to the lost settlement and removal after arrival or timeout.

**Tracking:** [M1 issue](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/2). Status: compiled candidate with C# policy tests passed. Real gameplay, save/load, and movement tests pending.

## v0.9.1 timeout correction

The previous arrival path continued waiting on a full receiving depot even after the convoy timeout elapsed. This could leave an indefinitely parked shipment. The game-facing arrival branch now checks the timeout, destroys the abandoned party, removes the shipment record, and records the remaining cargo loss. The headless simulator exercises the corresponding policy with a full depot. Runtime party cleanup remains a separate Bannerlord test.
