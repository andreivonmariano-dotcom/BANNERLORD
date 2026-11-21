# Calradia Unified v0.71e | Traveling household, personal stories and sworn-lord service

**Target:** Bannerlord v1.5.7, reference assemblies `1.5.7.125802`.
**Branch:** `audit/v069e-current-gameplay-and-visual-acceptance`, draft PR #93.
**Release state:** COMPILED_UNTESTED only after a successful CI build. None of
these 3D or conversation changes may be labeled GAME_TESTED without a
fresh in-game run, save/reload and diagnostic export.

## Q1. Real traveling household camp, first playable scene candidate

The native Party screen gains **Visit traveling household camp**.
From there the player enters a named `cu_household_camp` menu and
may walk among the actual adult Hero members of the current main party.

The 3D mission opens a **real native outdoor terrain scene**, selected
from Bannerlord's campaign scene model. It creates transient mission
Agents using the real `CharacterObject`, equipment and
`PartyAgentOrigin` of the player and up to eight verified healthy,
active adult Heroes physically present in the main party. Real
companions and squires only, no cloned campaign Heroes, free troops,
new relationships or political commands. The scene includes the
native mission conversation logic and a leave control.

The mission currently uses a game-selected outdoor scene, not a
bespoke authored tent compound. Actual native 3D spawning,
navmesh, conversation triggers, camera, collision and returning to the
campaign map require live verification. If a battle scene is not
available, a game-selected conversation scene is a fallback.
No underage child living in a distant settlement is moved into the
camp, as the camp represents genuine physical presence.

Camp entry is unavailable in settlements, at sea, during a map event,
in another mission, or if the player is captured. Clan/Party screens
remain the portable read-only source for household status.

## Q2. Character-driven personal petition stories

A real agenda-driven NPC can begin a private petition in native
one-to-one conversation. The request remains linked to the actual
Hero's saved ID. Support or refusal follows native monetary and
relationship actions. The choice is recorded as a personal story
only when game-backed effects match the chosen response. Seven
days later, the same person may be approached in real conversation
for an outcome scene, which is one-shot across save/reload.

Supported petitions:
- Recognition: real relation change, followed by acknowledgment
- HouseholdAid: a verified transfer of actual denars, followed by a
  later household report and modest native relation opportunity
- LivelihoodAid: a verified transfer of actual denars, followed by a
  later statement of how the NPC is proceeding

The story dialogue never falsely asserts that a new business, item,
child, holding or game rank has actually been created. It records
personal aid, not an invented settlement economy.
Old `CU_v067_character_requests` data remains authoritative.
The bounded story record adds `CU_v071e_personal_petition_stories`.

## Q3. Sworn Knight service, missions and earned household standing

The real patron may issue an in-person order after the first oath
audience:
- **Reconnaissance:** inspect two different native towns belonging to
  the patron's actual kingdom, on separate verified days. Report to
  the same real lord within 14 campaign days.
- **Muster:** maintain at least eight actual main-party members and
  report in person after three days, within the 14-day deadline.

Native clan, lord, town, party and reputation evidence are checked at
the actual action. An expired order gives no reward. Reporting to a
different patron or during a released oath does not count.
An accepted report records one bounded native patron favor action.

Repeated verified orders establish personal household standing:
Newly sworn knight -> Proven sworn sword (one task) ->
Trusted household knight (three tasks) ->
Veteran retainer (six tasks).

These are Calradia Unified **personal service distinctions**.
They do not promote the player into the vanilla kingdom nobility,
transfer clan leadership, invent a native promotion or grant land.
The existing three-day escort and periodic audience remain part of
the service path. State is stored under
`CU_v071e_native_knight_orders`.

## New runtime acceptance checklist

1. Fresh land-based Knight campaign with real sworn patron.
2. On native Party screen, open traveling camp away from town, sea
   and combat. Confirm a real outdoor mission appears and adult
   companions/squires in the native party appear as Agents.
3. Interact with a visible companion. Confirm native conversation,
   equipment, non-hostile state, and reliable ESC/leave -> world map.
4. Test without companions, with an injured Hero, and with minor
   children staying at a home settlement. No false camp inhabitants.
5. Try camp button at sea, in a town, in captivity and during a battle.
   Refuse scene opening without a hard crash or party mutation.
6. Trigger a Recognition petition and a monetary petition with a real
   adult NPC. Choose refusal, postpone and support on different runs.
7. Return to that exact NPC at least seven days later. Verify a single
   follow-up, actual relation deltas and no repeated gold withdrawal.
8. Save and reload before a follow-up, before reporting a Knight order,
   and after reporting. Confirm the saved identity and one-shot receipts.
9. Ask the sworn patron for reconnaissance, visit two distinct genuine
   towns in their actual kingdom, and return within the deadline.
   Verify no credit for repeated visits to one town.
10. Accept muster with fewer than eight party members, then with eight
    real members and three days passed. Verify only the second meets
    the native proof.
11. Test order expiry and transfer/change of patron. No duplicated
    reward, native kingdom promotion or synthetic lord authority.
12. Revisit Clan/Party dossier at 1280x720, 1650x927, 1920x1080
    and check the new Party camp button does not overlap native UI.
13. Check War Sails separately. No naval camp compatibility claim
    follows from land-campaign compilation.

**Limitations:** No authored 3D tent environment or scripted character
AI routines yet. Camp lighting, spawn points, escort party AI and
combat coexistence remain game-only tests. Stories are short
two-encounter arcs. Knight orders are genuine campaign objectives,
not objects injected into the native TaleWorlds quest tracker.
