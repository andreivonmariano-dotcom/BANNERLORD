# Calradia Unified v0.71e | Campaign-driven immersion without 3D cutscenes

**Target:** Bannerlord v1.5.7, references `1.5.7.125802`.
**Branch:** `audit/v069e-current-gameplay-and-visual-acceptance`, draft PR #93.
**State:** development candidate. Compile and live gameplay are separate gates.

## Artistic and interaction decision

Do not implement bespoke 3D cutscenes, staged camera movements,
outdoor scene loads, or spawned scene Agents for household roleplay.
Keep the world on Bannerlord's existing map and native NPC conversations.
Use concise native text/dialogue now, with illustrated Gauntlet
interfaces as an optional later UI-only milestone.

## Q1: Traveling household camp in the native campaign menu

The native Party screen has a **Visit traveling household camp** action.
Away from a settlement and outside sea travel or combat, it opens
`cu_household_camp`, a regular native **2D campaign menu**.

The camp offers the real traveling adult Hero roster (verified against
`MobileParty.MainParty.MemberRoster`), recorded officer duties,
current career objectives and read-only petition correspondence.
It exits to the normal campaign map. Family members who are not
currently in the player's party are not shown as physically present.
No cinematics, AgentBuildData, MissionState.OpenNew, or mission scene
assets are used.

Camp and office menus do not create native political offices, wage
payments, companion copies, distant family visits, or free troops.

## Q2: Private petitions as short relationship stories

An adult Hero with an existing agenda can raise Recognition,
HouseholdAid or LivelihoodAid in an actual native face-to-face
conversation. A native verified accept/refuse decision starts a
bounded, one-shot story. Seven campaign days later, the exact same
person can respond to a single new conversation that acknowledges
the player's earlier choice.

A modest native relation action occurs only on the supported follow-up.
No gold is paid automatically at follow-up. There is no claim that a
new building, business, child, holding or item has been created.
Saved data: `CU_v071e_personal_petition_stories`.

## Q3: Knight orders from the real sworn lord

After the first oath audience, the actual sworn patron can issue:

* **Reconnaissance**: inspect two different towns of the patron's
  real kingdom, on separate observed days, then report in person
  within 14 campaign days.
* **Household muster**: retain at least eight real party members,
  then report to the lord after three days and before the deadline.

The existing field-escort oath and recurring audience continue.
Reports require in-person conversation with the verified patron
and have bounded native favor. Expired or repeated orders grant no
free reward. Earned **personal** standing displays in the dossier:
Newly sworn knight, Proven sworn sword, Trusted household knight and
Veteran retainer. These are not native kingdom promotions.
Saved data: `CU_v071e_native_knight_orders`.

## Acceptance gates

1. Build against pinned game reference assemblies.
2. Open the Party menu on the map. Visit the 2D camp and return.
   No custom mission or cinematic should load.
3. Confirm the camp displays only actual adult Heroes in your party.
4. Check the camp entry is unavailable at sea, during combat,
   captivity or settlement occupation.
5. Test NPC private petitions, support/refusal and seven-day
   follow-up with native balance/relation evidence.
6. Test Knight's two town scouting order, muster and deadlines.
7. Save/reload after each story decision and each Knight report.
8. Verify UI controls at 1280x720, 1650x927 and 1920x1080.
9. Keep naval compatibility as a separate in-game acceptance gate.

Passing CI supports a **compiled candidate** only. In-game and
save/reload verification are still necessary.
