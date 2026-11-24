# Calradia Unified v0.71d: Immersive household and private petitions

Target: Bannerlord PC v1.5.7.125802 reference assemblies.
Draft PR: #93. Source changes are not a game-tested release.

## Playtest evidence received, 21 November 2026

The closed session archive CU-Session-20261121T160838_e5d3bb1a-CLOSED.zip
reported normal module-unload shutdown and no managed exception.
It does not rule out every game/UI error.

- Knight player bound to real Vlandian patron Ingalther (lord_4_16).
  His native relation rose from 2 to 40 on patron binding.
- Six real members of Ingalther's household were flagged as known to
  the player's Encyclopedia. This is NOT the same as changing the
  game's separate native first-meeting dialogue condition.
- The player arrived directly alongside Ingalther's real field party:
  distance before 66.28193, distance after zero.
- The in-person oath muster recorded once and raised actual relation
  40 to 44. A subsequent native field escort was accepted.
- A native NPC request from a generated squire had type Recognition.
  The requested relation increase was +1, yet the actual change was +2.
  The exact-only receipt reported false. This was a misleading
  verification message, not proof of missing native relation change.

The first screenshot still showed the vanilla lord greeting that a
character uses for a newly met stranger. The next screenshot showed
the correct CU sworn service option. This mismatch is not fixed by
Encyclopedia knowledge alone. Do not pretend the greeting is already
corrected or use an unverified engine "met" flag.

## New access rules

- Clan and Party native UIExtenderEx entry points remain the portable
  household, career, portraits, and duty ledger surfaces.
- In a physical town or castle, "Hold household court" appears ONLY
  at a fortification really owned by the player's current clan and
  not under siege. Foreign villages/towns are not universal private
  affairs hubs.
- A saved noble claim can still be brought before its real birth
  clan's castle or town with "Request an audience with my birth
  family". This preserves the actual secondary-estate petition
  and native grant safeguards for cadet branches.
- Legacy menu identifiers stay registered for old saves. The
  obsolete remote request accept/decline choices are hidden.
- The dossier's "View petitions" button now displays the actual
  pending request and last-known real Hero location. It never
  generates a payment or relation change.
- Requests are now heard through the real Hero's
  `hero_main_options` conversation, where the actor must match
  the pending saved Hero ID and be physically present. NPC lines
  depend on their actual request: Recognition, HouseholdAid,
  or LivelihoodAid.
- The player chooses support, refusal, or delay face-to-face.
  Support is gated by actual gold. No payment is possible from a
  distant ledger or legacy settlement request menu.

## Verified relation reporting

Existing strict `RelationConfirmed` remains unchanged for systems
that need an exact native delta. Petition responses now use an
additional bounded `RelationDirectionObserved` check, keeping the
actual delta and exact-match result separately in diagnostics.

An observed +2 after an intended +1 is recorded as an observed
improvement, with `relation_exact=false`. Zero movement, wrong
direction, or an implausibly large change still fail.
Gold gifts continue to require both actual native balance deltas.

## Live acceptance gate

1. Start a new SwornHouse Knight with Ingalther. Confirm the native
   first greeting still behaves as the game expects, then verify
   the CU oath and service dialogue after the introduction.
2. Open Clan or Party at a neutral settlement and in the open field.
   Confirm the CU household dossier is reachable without visiting
   a castle. The "View petitions" button must show only correspondence.
3. Visit a foreign castle and town. The generic private household
   court entry must be absent. Visit a player-owned fortified seat.
   "Hold household court" should appear once, with correct exits.
4. As a noble cadet with a real birth-clan claim, enter a genuine
   birth-clan fortification. Check the separate estate-petition
   audience remains available without granting free land.
5. Speak directly to a real companion with a generated agenda.
   Ask about their affairs. Confirm native responses for each
   supported type. Defer: no native gold/relation changes.
6. Accept a Recognition request with a native relation modifier.
   The message must report observed standing correctly. Export
   `relation_exact`, `relation_proof` and `actual_relation_delta`
   to diagnose an unexpected multiplier.
7. Accept a monetary request only with sufficient native gold.
   Confirm actual payer debit and recipient credit. Refuse and
   repeat after reload to check no duplicate transfers.
8. Revisit the dossier at 1280x720, 1650x927, 1920x1080.
   Verify the petition button, portrait click zones, Escape,
   and native Clan/Party keyboard navigation.
9. Test the real lord greeting separately. No assertion that
   changing `IsKnownToPlayer` must suppress the greeting.

Older saves lack no new record keys in v0.71d. Native family,
gold, clan and political entities remain authoritative. No new
NPCs, synthetic relationships, remote salaries or fiefs are made.
