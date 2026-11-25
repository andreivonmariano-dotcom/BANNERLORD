#!/usr/bin/env python3
"""Static coverage of actual Bannerlord UI adapters and UI acceptance.

This test does NOT render Bannerlord, exercise a mouse, or prove an
in-game screen. The separate native-game audit must receive evidence.
"""
from pathlib import Path
import importlib.util
root=Path(__file__).resolve().parents[1]
base=root/"Source"/"CalradiaUnified"
read=lambda name:(base/name).read_text(encoding="utf-8")
view=read("SandboxOriginBehavior.NativeHouseholdUI.cs")
living=read("SandboxOriginBehavior.LivingMenus.cs")
lord=read("SandboxOriginBehavior.LordInheritance.cs")
king=read("SandboxOriginBehavior.KingSuccession.cs")
council=read("SandboxOriginBehavior.KingCouncil.cs")
knight=read("SandboxOriginBehavior.KnightOath.cs")
creator=read("CUGuidedCreatorVM.cs")
assert 'starter.AddGameMenu("cu_people_lineage"' in living
assert '"cu_starting_household_native_status"' in living
assert 'args=>ShowStartingHouseholdStatus()' in living
assert '"Household"' in living
assert '"cu_native_household_display_opened"' in view
assert 'native_read_only=true' in view
assert 'ShowNativeDetail("Starting household | "+type,' in view
assert 'MBObjectManager.Instance' in view
assert 'Hero.MainHero' in view and 'Clan.PlayerClan' in view
assert 'ShowNativeDetail("Knight | Sworn service",' in knight
assert 'ShowNativeDetail("Lord household | Inheritance",' in lord
assert 'ShowNativeDetail("Royal household | Succession",' in king
assert 'ShowNativeDetail("Royal council | Current court",' in council
assert 'NativeHeroDisplayName(' in lord
assert 'NativeHeroDisplayNames(' in lord
assert 'NativeHeroDisplayName(' in king
assert 'NativeHeroDisplayNames(' in king
assert 'NativeHeroDisplayName(royalCouncil.MinisterId)' in council
assert 'NativeHeroDisplayName(royalCouncil.MarshalId)' in council
assert '"Royal court"' in council
assert 'args=>CurrentNobleAuthority(true) && royalCouncil.IsRecorded' in council
assert '"Royal court",\n                    args=>CurrentNobleAuthority(true) && royalCouncil.IsRecorded' in council
assert '"cu_royal_council_ui_location"' in council
assert 'ShowNativeDetail(' in council
assert 'NativeRoyalCouncilPresent(true)' in council
assert 'NativeCourtOfficer("Kingsguard")' in council
assert 'kingsguard.CharacterObject' in council
assert 'TeleportHeroAction.ApplyImmediateTeleportToSettlement(' in (
    read("SandboxKingNativeMinistry.cs"))
assert '"cu_knight_oath_entry"' in knight
assert 'knightOath.PatronId' in knight
assert 'patron.Clan.Name.ToString()' in knight
assert '"My household inheritance"' in lord
assert '"Dynastic succession"' in king
assert 'OnPropertyChangedWithValue(Summary,"Summary")' in creator
assert 'OnPropertyChangedWithValue(Hint,"Hint")' in creator
assert 'OnPropertyChangedWithValue(IsReview,"IsReview")' in creator
spec=importlib.util.spec_from_file_location("native_gate",
    root/"scripts"/"audit_v070_seven_live_start_evidence.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
assert len(mod.ROLES)==7
assert all(mod.NATIVE_UI_FIELDS)
assert all(role in mod.ROLE_UI_SCREENSHOTS
           for role in ("Knight","Lord","King"))
assert "king_council_ui.png" in mod.ROLE_UI_SCREENSHOTS["King"]
assert "lord_inheritance_ui.png" in mod.ROLE_UI_SCREENSHOTS["Lord"]
assert "knight_oath_ui.png" in mod.ROLE_UI_SCREENSHOTS["Knight"]
print("PASS v0.70s STATIC: native household inquiry uses game Hero state; "
      "Knight/Lord/King panels have readable records and discoverable "
      "Royal Council location; native UI play evidence remains separate.")

# Identity repair is opt-in, exact-ID and restricted to recorded family.
assert 'FamilyOccupationRepairs()' in view
assert 'h.CompanionOf==null && !clan.Companions.Contains(h)' in view
assert 'livingPersons.People.Any(p=>p.Id==h.StringId' in view
assert 'FamilyOccupationRepairs().Where(h=>ids.Contains(h.StringId))' in view
assert 'h.SetNewOccupation(Occupation.Lord)' in view

# Diagnostics have one entry per settlement; repair must be a second confirmation.
assert '"cu_household_records","cu_start_diagnostic_records","Troubleshooting"' in view
assert '"cu_family_occupation_repair_"+menu' not in view
assert 'CanReviewStartDiagnostics() && Hero.MainHero.StringId==owner' in view
assert 'if(!CanReviewStartDiagnostics() || Hero.MainHero.StringId!=owner)return;' in view
