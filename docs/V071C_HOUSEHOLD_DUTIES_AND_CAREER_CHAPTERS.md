# Calradia Unified v0.71c: household duties and career chapters

Target: Bannerlord PC v1.5.7.125802 public references. Draft PR #93.
Status: source candidate, not a game-tested public release.

## Q2. Household duty appointments and family relationships

Three opt-in **personal** offices now exist: Steward, Captain, Envoy.
The actual current player-clan companion must be met through the
native one-to-one conversation. One named native Hero can hold only
one household duty. Reassignment replaces that office's prior holder
but creates no new Hero, item, party, or game-managed political office.

Each role has an in-person witnessed duty report:
- Steward: the player is physically at an actual player-clan-owned
  fortified settlement with the companion in the main party.
- Captain: the main party contains at least eight real party members.
- Envoy: the player is in an actual town owned by a different clan.

One duty report per office per campaign day. No free renown, native
companion party-role changes, governors, money, skills or promotions.
The ledger appears under Household & Personal Affairs. Actual office
names appear on dossier companion portrait rows. If a companion
leaves the clan, that Hero cannot continue the duty, and the saved
ledger shows the old holder as unavailable until reassigned.

Real adult spouses and children have two conversation options:
- A private in-person family audience. A recorded visit allows a
  native relationship increase of one, at most once per person per
  thirty days. The actual post-action relation is written to diagnostics.
- A voluntary gift of 100 native denars. The native transfer must
  debit the player and credit the recipient. The gift attempt is
  recorded before the native effect to block repeated withdrawals
  after failures. A failed/uncertain transfer is not claimed as paid.

Underage children are deliberately excluded from adult conversation
actions. No child is spawned, teleported, aged or changed into a party
Hero. Both conversation options are separate from the existing
independent household petition system.

## Q3. Seven role-specific campaign career chapters

The Calradia Unified career chronicle tracks up to three verified
chapters. It is save-backed and advances from actual native game
evidence, at most one chapter per verified day:

- Wanderer: visit three distinct native towns on separate observed
  days. Saved town IDs prevent repeatedly visiting the same town.
- Outlaw: recruit five native party members, maintain a band of at
  least five across three consecutive observed days, then maintain
  ten across seven consecutive observed days.
- Mercenary: join a real native mercenary service, remain hired for
  seven observed days, then remain hired for 21 further days.
  This proves active mercenary status, not one employer throughout.
- Soldier: verify actual Serve as Soldier enlistment, serve at least
  seven confirmed days inside the external host party, then reach
  thirty confirmed days.
- Knight: make the real first in-person sworn-lord audience, complete
  one genuine three-day native field escort, then complete a second.
- Lord: hold an actual player-clan fief, own native land across seven
  consecutive observed days, then hold two native settlements.
- King: verify the real ruling clan, convene the real royal council
  in the correct settlement, then complete three verified council
  meetings.

This is a **Calradia Unified campaign chronicle**, not a TaleWorlds
QuestBase in the native quest log. It does not fabricate game ranks,
influence, salaries, promotions, political authority, or troops.
The existing native Clan, Party, Kingdom, service provider, and
character conversation systems remain the authority.

## Save and rollback contract

New keys:
- CU_v071c_household_duties
- CU_v071c_career_chronicle

The values are bounded, versioned and strictly parsed. Older saves
missing these keys start with empty personal duty/chronicle records.
Previous v0.70x records and their IDs are not renamed or migrated.
A malformed record is rejected instead of granting a role or chapter.
Native campaign changes are never replayed during parse or load.

## Tests and release gate

1. Run v0.71c source verification plus the .NET PolicySmoke runner.
2. Compile against Bannerlord v1.5.7.125802 reference assemblies.
3. In-game, speak with actual companion Heroes, assign each duty,
   confirm no changes to native party roles, troop rosters or gold.
4. Meet each companion in its legitimate report setting and verify
   a single receipt on that day. Save and reload before attempting
   a repeat report.
5. Meet a real adult spouse or grown child. Confirm one audience
   relation gain and one verified 100-denar gift. Verify thirty-day
   cooldown, native balance receipts and no offers to minors.
6. Test all seven career paths across several campaign days.
   Verify every native prerequisite before each chapter receipt.
7. Test missed-day interruption and reloading a partial streak.
8. Open the household dossier and chronicle at 1280x720, 1650x927
   and 1920x1080. Check all portrait labels and click targets.
9. Repeat War Sails/naval starts separately. No naval support is
   inferred from a successful land-campaign test.

Do not label this GAME_TESTED or RELEASED without actual gameplay,
screenshots, diagnostics and save/reload acceptance. A passing CI
compile is only source and API verification.
