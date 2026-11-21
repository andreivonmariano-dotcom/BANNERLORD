# Calradia Unified v0.71a | Knight arrival and first live UI review

**Target:** Bannerlord PC v1.5.7, pinned reference package 1.5.7.125802.
**Branch:** audit/v069e-current-gameplay-and-visual-acceptance, draft PR #93.
**Status:** Source changes pending exact-build CI and fresh live-game acceptance.

## Evidence supplied on 21 November 2026

The player supplied screenshots at 1650x927 and a closed session archive.
The session recorded normal module-unload shutdown, not a proof that
every possible exception was absent. In the actual Knight session:

- Native patron Ingalther of dey Cortain, Vlandia.
- Native relationship advanced from 0 to 22 during patron binding.
- The sworn personal oath recorded successfully without native vassalage
  or automatic party enlistment.
- Player start included the explicit log field `relocation_pending=true`.
- No native patron conversation-muster event was observed in the archive.
- The household dossier loaded as a native Gauntlet movie.
- The same archive includes a verified character-request relationship
  change. It does not establish seven-role or long-term save acceptance.

The screenshots show readable landscape and castle scenes, but custom
text menus still contain developer language. Two custom Gauntlet panels
showed very large empty areas with small, centered detail text. The
family list appeared anchored toward the bottom of the panel. The
screenshot evidence is not a clean repro of all screen resolutions.

## Implemented source changes

- A newly verified **SwornHouse Knight only** now attempts exactly one
  arrival near the real, living patron. A field lord uses the existing
  native party position. A lord currently resident in a settlement uses
  that settlement's gate position.
- The arrival checks that the native player party is self-led, that the
  patron is the exact recorded oath owner, and that both parties or the
  native residence resolve. The relocation never moves the patron,
  creates a new NPC, transfers the player clan, or changes fief ownership.
- Existing native patron-house nobles and direct spouse/children are set
  as known to the player in the game's Encyclopedia. This does not
  fabricate family bonds, change standing, or grant friendship.
- A distinct first-audience line is available through a **real native
  conversation** with the exact living patron. It invokes the existing
  one-shot muster receipt and normal native relation action.
- Dossier body and headings now use explicit left/top text alignment;
  base body size is raised. The family gallery lists its entries from
  the top. Backdrops become translucent rather than opaque black.
- Household and castle hub descriptions use player-facing terms rather
  than engine/API labels; menu IDs and save-state keys are unchanged.

## Release blockers: live-game acceptance

1. Run a new **Knight / SwornHouse** campaign. Confirm the actual
   starting lord belongs to the requested kingdom and leads a real
   party or occupies a native settlement. Check the `cu_knight_patron_arrival`
   receipt for `ARRIVED` and a consistent native distance. The lord
   must remain in his own party or residence.
2. Open the native Encyclopedia and verify the patron and authentic
   spouse/children or clan nobles are known, without invented kinship or
   free relation increases.
3. Enter the patron's real conversation. Check the first audience line,
   native reply, one `cu_knight_patron_conversation_muster` event, and
   the actual recorded relation gain. Confirm the first audience option
   disappears after that success.
4. Save, quit, reload and cross one in-game day. Confirm no second
   relocation, extra audience reward, changed clan affiliation or
   broken starting position.
5. Revisit the household dossier and portrait gallery at 1280x720,
   1650x927 and 1920x1080. Inspect text wrapping, buttons, rows,
   backdrop opacity, mouse/controller input, Escape and navigation.
6. Re-test a lord **inside** a castle and a patron temporarily without
   a self-led field party. Verify the documented skip behavior instead
   of a teleport into an invalid location.
7. Exercise War Sails coastal/naval starts separately. Base-map
   placement is not proof of naval navigation compatibility.

**Explicitly not implemented:** forced first-frame native conversation
with the lord. That requires a separately verified campaign-map
conversation entry point and an input-safe start phase. The player can
speak to the nearby real lord in the native interface.

**Source verification is not game verification.** No user save file,
private diagnostic ZIP, or personally identifying Windows path was
committed with this record.
