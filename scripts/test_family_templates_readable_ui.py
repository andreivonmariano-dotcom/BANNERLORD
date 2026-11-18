#!/usr/bin/env python3
"""v0.24 source-only regression for the 2026-11-18 spouse_template_missing report."""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
family=(root/"Source/CalradiaUnified/SandboxFamilyCreator.cs").read_text()
origin=(root/"Source/CalradiaUnified/SandboxOriginBehavior.cs").read_text()
overlay=(root/"Source/CalradiaUnified/SandboxCreatorOverlay.cs").read_text()
xml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").getroot()

# The original restrictive source selector returned spouse_template_missing
# for the user's Vlandian Lord family. Native living noble donors are a
# controlled last resort and must be CLONED, never reassigned as family.
assert "CharacterObject.All.Where" in family
assert "Hero.AllAliveHeroes.Where" in family
assert 'method = "local_existing_lord_clone"' in family
assert 'method = "foreign_existing_lord_clone"' in family
assert "HeroCreator.CreateSpecialHero(source" in family
assert "spouseHero.Culture = desiredCulture;" in family
assert "cu_family_template_scan" in family
assert "total_gender_templates=" in family
assert "local_templates=" in family
assert "IsCoupleSuitableForMarriage(player,spouseHero)" in family
assert "MarriageAction.Apply(player,spouseHero,false)" in family
assert "spouseHero.Spouse != player" in family
assert "spouseHero.Clan != Clan.PlayerClan" in family

# Children must come from the native parent-specific template and CreateChild
# path, not arbitrary adult wanderer source characters.
assert "GetCharacterTemplateForOffspring(mother, father," in family
assert "HeroCreator.CreateChild(childTemplate," in family
assert "child.Father != father || child.Mother != mother" in family
assert "cu_family_child_source" in family
assert "cu_family_verification" in family
assert "cu_native_companion_snapshot" in origin
assert "clan_companions=" in origin

# Screenshot regression: actual tinted planes behind all text and controls.
widgets=[n for n in xml.iter() if n.tag=="Widget"
         and n.get("Sprite")=="BlankWhiteSquare_9"
         and n.get("Color","").startswith("#")]
assert len(widgets)>=16,len(widgets)
assert any(n.get("WidthSizePolicy")=="StretchToParent" and
           n.get("HeightSizePolicy")=="StretchToParent" for n in widgets)
for binding in ("RoleSummary","FamilySummary","LandSummary",
                "WorldSummary","StartNotice"):
    assert any(n.tag=="TextWidget" and n.get("Text")=="@"+binding
               for n in xml.iter()),binding
    assert "public string "+binding in overlay,binding
assert "profile.SpouseEnabled ?" in overlay
assert "family: off" not in overlay
assert "REAL FAMILY" not in overlay

print("PASS: spouse donor fallback, native offspring creation, distinct companions, opaque layout and right-side family summary (STATIC ONLY)")
