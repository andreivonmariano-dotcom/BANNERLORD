# Calradia Unified v0.71f | Portrait Camp, Personable Requests, Earned Knight Standing

Target build: Bannerlord `1.5.7.125802` public reference assemblies.
Branch: `audit/v069e-current-gameplay-and-visual-acceptance`, draft PR #93.
Status: **Source candidate**. CI compilation is not game-tested approval.

## Design rule: no custom 3D cutscenes

Q1, Q2 and Q3 use native campaign menus, real NPC conversations and
custom 2D Gauntlet presentation. No MissionState.OpenNew, custom cameras,
Agent spawning, mission scenes, invented camp followers, or fictional
kingdom appointments.

## Q1. Illustrated household camp

From the native Party screen, choose "Visit traveling household camp"
on safe land outside a settlement and combat. The camp has a 2D text
entry and a new "Open my illustrated household register" action.

The `CUHouseholdCamp` Gauntlet screen offers a 970x618 parchment and
bronze layout with real CharacterImageIdentifierVM portrait cards.
On the minimum 1280x720 display, the panel fits the strict prefab
geometry gate. Six native Hero portraits fit the visible column. If
more than six verified adult Heroes travel with the party, the
attendance header says so without falsely showing them all.

Four interactive tabs:
- People: actual heroes in MobileParty.MainParty.MemberRoster. Select
  a portrait to inspect current identity, role, relation and location.
- Officers: save-backed Steward/Captain/Envoy roles and real reports.
- Letters: correspondence and actual petitioner identity/location.
  No remote denar or relationship action.
- Service: the native-evidence career chapters and the Knight oath
  report, including personal standing and active deadline.

Escape and Return close only the UI and leave the player in the
existing campaign camp menu. All four tabs stay read-only. Their data
are derived from the actual party and native game records.

## Q2. Character personalities and ambition

The existing native-trait-based initiative policy determines whether
the real adult Hero raises Recognition, HouseholdAid or LivelihoodAid.
The new `SandboxPetitionVoicePolicy` selects a mutually exclusive
conversation tone using real Honor, Generosity, Calculating, personal
relation and the already observed agenda Goal:
- Guarded: distrust or calculating caution
- Duty: an honorable household-related goal
- Ambitious: calculating livelihood or trade interests
- Warm: generous, trusted interpersonal rapport
- Direct: no stronger verified trait pressure

The in-person conversation has a new "What is most important to you?"
option leading to five distinctive explanations. Asking this option
changes no game currencies or relationships. The player's final
support, refusal or defer action still passes the existing real NPC
authority and native payment/relationship verification gates.

The seven-day follow-up now distinguishes Recognition, HouseholdAid and
LivelihoodAid, and uses a cautious or conciliatory refusal response
based on current native character evidence. It never claims that an
offscreen business, child, holding, or military appointment was made.

## Q3. Knight standing and lawful political audiences

The same existing sworn-lord duty orders and completed field escorts
remain. Personal household standing now depends on ALL available
native evidence, not merely the number of orders:

- **Newly sworn knight**: default status
- **Proven sworn sword**: one completed service order and actual native
  relation of at least 10 with the sworn lord
- **Trusted household knight**: three completed orders, one field
  escort, lord relation at least 25, native clan renown at least 50
- **Veteran retainer**: six completed orders, two escorts, relation at
  least 40, native clan renown at least 125

A Knight whose independent player clan has at least three completed
orders, one real escort, patron relation at least 30, native clan
renown at least 75 and a patron belonging to an actual native kingdom
may speak personally with the patron and ask to present their
household's record at court.

The lord's recommendation becomes a one-shot saved receipt. The
player must then meet the real king/queen of the patron kingdom,
verified from `Kingdom.RulingClan.Leader` in native conversation. A
bounded native relation action and a recorded hearing are permitted
once. Kingdom membership, vassalage, clan ownership and fief transfers
remain completely untouched. The monarch explicitly states that
formal political admission follows native kingdom authority.

Compatibility: `SandboxKnightServiceOrderState.Parse` accepts old
eleven-field `CU_KNIGHT_SERVICE_1` records or the new thirteen-field
form. Recommendation and royal-hearing fields default to `-1` in
older saves. Existing previous save keys are not renamed.

## Required live acceptance

1. Start a new Knight and visit the safe-land Party camp. Open the
   new Gauntlet portrait register, switch every tab, click portraits,
   close using Return and Escape, and exit to the campaign map.
2. At 1280x720, 1650x927 and 1920x1080, test text wrapping,
   portrait clipping, overlays, mouse clicks and keyboard focus.
3. Repeat with 0, 1, 6 and 8 real adult party Hero members. No
   family member in a distant holding should appear in camp.
4. Test the in-person petition "why" branch with each voice policy
   style. Confirm no remote or repeated rewards.
5. Verify the correct kinds of seven-day follow-up and one-shot
   save/reload semantics for supported and refused requests.
6. Test Knight standing thresholds against real renown, relationship
   and field escort counts. Confirm values displayed in both camp
   and the general household dossier.
7. Obtain patron's recommendation and find the actual native
   kingdom ruler. Confirm one audience, relation receipt and no
   clan, fief, troop or kingdom status mutation.
8. Save and load before/after a recommendation and ruler audience.
   Confirm old eleven-field records load, with no automatic audience.
9. Repeat naval and War Sails-specific checks separately.

Never mark game-tested solely from successful Windows CI or pure
state policy tests.
