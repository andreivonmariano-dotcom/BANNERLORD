#!/usr/bin/env python3
"""Static verification that settlement tabs are grouped by native context.

Engine menu registration, in-game scenes and menu click behavior are
still subject to the separate native game acceptance gate.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
civic=read("CivicBehavior.cs")
living=read("SandboxOriginBehavior.LivingMenus.cs")
origin=read("SandboxOriginBehavior.cs")
knight=read("SandboxOriginBehavior.KnightOath.cs")
lord=read("SandboxOriginBehavior.LordInheritance.cs")
king=read("SandboxOriginBehavior.KingSuccession.cs")
council=read("SandboxOriginBehavior.KingCouncil.cs")
assert '"cu_civic_admin_entry_local"' in civic
assert 'RegisterLocalAdministrationEntry(starter)' in living
assert 'starter.AddGameMenuOption("cu_people_lineage",' in civic
assert '"cu_civic_admin_entry_"+source' not in civic
assert '"cu_people_lineage_entry_"+from' in living
assert '"Manage local administration"' in civic
assert '"Attend to local affairs"' in living
assert 'Settlement.CurrentSettlement.OwnerClan==' in living
assert '"cu_birth_family_council_entry_"+from' in living
for hub in ("cu_civic_administration","cu_civic_stores",
            "cu_civic_garrison","cu_civic_correspondence"):
    assert 'starter.AddGameMenu("'+hub+'"' in civic,hub
assert 'GameMenu.SwitchToMenu("cu_civic_administration")' in civic
assert 'GameMenu.SwitchToMenu("cu_people_lineage")' in living
assert 'RegisterPhysicalConvoyMenu(starter, menu)' in civic
assert 'RegisterGarrisonMenu(starter, defenseMenu)' in civic
assert 'RegisterCastleManagementMenu(starter, defenseMenu)' in civic
assert 'RegisterFeudalMenu(starter, defenseMenu)' in civic
assert 'RegisterKingdomMenu(starter,' in civic
assert 'RegisterDepotTradeMenu(starter, menu)' in civic
# No legacy next-game creator from an active campaign settlement.
assert 'starter.AddGameMenu("cu_origin_main"' in origin
assert '"cu_origin_entry_" + sourceMenu' not in origin
assert '"CU: Design my NEXT Sandbox starting life"' not in origin
# Every private/family/royal office action is behind one hub.
for marker in ('"cu_family_portraits"','"cu_family_residence_here"'):
    assert marker in origin,marker
assert '"cu_partial_family_review"' in living
assert '"cu_partial_family_review_"+from' not in living
for code,marker in (
    (knight,'"cu_knight_oath_entry"'),
    (lord,'"cu_lord_inheritance_entry"'),
    (king,'"cu_king_succession_entry"'),
    (council,'"cu_native_royal_council_entry"')):
    assert marker in code,marker
    assert 'starter.AddGameMenuOption("cu_people_lineage"' in code
    assert 'new[]{"town","castle"}' not in code
    assert 'foreach(var source in new[]{"town","castle"})' not in code
dialogue=read("SandboxOriginBehavior.KnightDialogue.cs")
assert 'Hero.OneToOneConversationHero' in dialogue
assert 'lordParty.GetPosition2D.Distance(playerParty.GetPosition2D)' in dialogue
# Civic controls are not renamed; old saved state keys must remain.
assert 'CU_v030_civic_state' in civic
assert 'CU: Deposit 5 grain' not in civic
assert 'CU: Review castle' not in read("CivicBehavior.CastleManagement.cs")
# No unverified native clan/kingdom membership rewrites.
assert 'ChangeKingdomAction' not in knight+lord+king+council
print("PASS v0.70v STATIC: estate/household hubs and contextual native "+
      "operations; direct next-game, service, lineage and royal tabs "+
      "removed from town/castle source. Native UI still unverified.")
