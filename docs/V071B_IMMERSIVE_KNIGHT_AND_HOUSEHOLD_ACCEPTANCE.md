# Calradia Unified v0.71b | Immersive Knight audience and seven-role household ledger

Target: Bannerlord PC v1.5.7, pinned reference package 1.5.7.125802.
Development branch: audit/v069e-current-gameplay-and-visual-acceptance (draft PR #93).
Implementation status: IMPLEMENTED_SOURCE. Static tests and Windows CI required;
a successful compile does not qualify as a game-tested release.

## Q1: first encounter with the actual sworn lord

- The first in-person patron conversation now opens a multi-step native
  conversation rather than granting the first audience on selection.
- The lord explains the separate household. The Knight can ask about
  duties, ask about the patron's real clan, pledge, or defer.
- Only the pledge invokes the existing RecordNativePatronAudience, which
  uses the native one-shot oath muster and a bounded native relation
  action. Asking or deferring does not grant relation.
- An actual patron selected at creation still receives a guarded
  one-time field-party or settlement-gate arrival attempt. Native
  campaign party and settlement state remain authoritative.
- This is not an automatically launched 3D cutscene, new castle scene,
  or invented full kingdom vassal oath. That requires a separately
  verified engine entry point and staged mission camera.

## Q2: actionable portrait-led household dossier

- Two populated Gauntlet lists now show real native Hero portraits:
  family members and the current clan companions.
- Portrait inspection checks that the actor is truly the player,
  spouse, reciprocal child, or current clan companion before showing
  native kinship, actual home/party and observed standing.
- Master of Arms and Marshal descriptions point to the real,
  pre-existing native garrison-order conversations and their
  five-recruit restrictions. Actual governors display their current
  settlement.
- Family gallery shows additional children and reciprocal parentage,
  without creating, teleporting or aging child Heroes.
- Petition control asks for the physical actor, then confirms either
  native-gold support or decline. The existing CharacterRequest policy
  and GiveGoldAction/ChangeRelationAction remain the authority.
- Native Clan, Party and Kingdom views still own status and politics.
- The 960x620 layout is bounded for 1280x720. Source geometry is not
  screenshot acceptance. Verify portrait texture binding and hitboxes.

## Q3: seven career paths

The same dossier now has distinct portraits, titles and status actions:
Wanderer (road), Outlaw (band), Mercenary (contract and confirmed wage),
Soldier (game-backed enlistment), Knight (lord and oath),
Lord (fiefs and inheritance), King (royal council and native crown).
All seven are accessible from the existing campaign screen adapters;
settlement menus now include all role-economy starts.
These are native-backed duty summaries and household decisions, not
new, unverified campaign systems or free NPC commands.

## Source and runtime gates

- scripts/test_v071a_knight_arrival_and_immersion.py
- scripts/test_v071b_household_portraits_roles_and_decisions.py
- scripts/test_v070x_gauntlet_dossier_layout.py
- scripts/test_v070y_butr_required_native_integration.py
- scripts/test_v070t_knight_patron_dialogue.py
- CI compile uses Bannerlord.ReferenceAssemblies 1.5.7.125802.

Game-only requirements:
1. Knight fresh start. Confirm arrival receipt and actual patron position.
2. Begin first conversation, ask both questions, defer. Confirm no
   native relation or first muster reward.
3. Return to the patron and pledge. Confirm one recorded muster and the
   actual native relation delta, then check that the first-audience option
   disappears.
4. Save, quit and reload. Confirm no repeated reward or relocation.
5. Open Clan/Party/Kingdom dossier entries and test character portrait
   hitboxes, detail inquiry, family gallery, petition support and decline.
6. Test text wrapping and Escape on 1280x720, 1650x927, 1920x1080.
7. Repeat the dossier and duty action for all seven starts, verifying
   native statuses from each career rather than accepting screen labels.
8. Confirm the Master of Arms and Marshal order options remain gated to
   the correct native companion conversation and real recruiting stock.
9. Keep War Sails/naval placement as a separate game-tested gate.

Do not mark the module GAME_TESTED or RELEASED from CI success alone.
