#!/usr/bin/env python3
"""v0.33: unsupported CU profile options are visible but not editable.

Source and Gauntlet XML only; do NOT claim native game launch,
real-time player 3D model, or a single-screen character creator.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
overlay=(src/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
profile=(src/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
creator=root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml"
xml=ET.parse(creator).getroot()
doc=(root/"docs/V033_THREE_LOCKED_CREATOR_UI_DECISIONS.md").read_text(encoding="utf-8")

# These four user-facing features cannot be delivered in the current
# pre-campaign UI. Never let a choice appear working before native parity.
disabled={
    "PlayerAge": "Starting age (use native creator)",
    "SiblingCount": "Starting siblings (requires parent Heroes)",
    "EquipmentKit": "Equipment theme (not applied yet)",
    "StartingTown": "Preferred starting town (relocation unavailable)",
}
for key,label in disabled.items():
    expr=r'new Field\("[^"]+","'+re.escape(key)+r'",\s*"'+re.escape(label)+r'","unavailable"\)'
    assert re.search(expr,overlay),f"{key} must be read-only"
    assert f'if (key=="{key}") return ' in overlay, f"missing reason for {key}"

assert 'Current.Kind=="unavailable"' in overlay
assert 'return "NOT AVAILABLE IN THIS BUILD";' in overlay
assert 'public bool IsUnavailable' in overlay
assert 'OnPropertyChangedWithValue(IsUnavailable,"IsUnavailable")' in overlay
assert 'IsVisible="@IsUnavailable"' in ET.tostring(xml,encoding="unicode")
assert any(n.get("Text")=="Unavailable" and
           n.get("IsVisible")=="@IsUnavailable"
           for n in xml.iter("TextWidget"))
# An unavailable field has none of these editable widget types.
assert 'Current.Kind=="number"' in overlay
assert 'Current.Kind=="boolean"' in overlay
assert 'Current.Kind=="choice"' in overlay
assert 'Current.Kind=="text"' in overlay
assert 'if(!IsText) return;' in overlay
assert 'if (!IsNumber || Current==null) return;' in overlay
assert 'if(IsBoolean)' in overlay

# No editable sibling name/age/gender page remains. Keep sibling feature
# described but disabled until its actual parent Hero graph exists.
assert 'if (key.StartsWith("Sibling.",StringComparison.Ordinal))\n                return false;' in overlay
assert not re.search(r'key\.StartsWith\("Sibling\.",StringComparison\.Ordinal\)\s*\|\|',overlay)
for reset in ('PlayerAge=-1;', 'StartingTown="";', 'EquipmentKit="Vanilla";',
              'SiblingCount=0;','NativeFaceWorkshopEnabled=false;'):
    assert reset in profile,reset
native_default_section=overlay.split("public void ExecuteUseNative()",1)[1].split("public void ExecuteEditText()",1)[0]
assert '"PlayerAge"' not in native_default_section, "do not offer native-default button on disabled age"

# One five-tab CU flow, existing safe exit and real single native handoff
# stay intact. No fake pre-campaign player Agent or character creator override.
assert '"Identity", "Household", "Resources", "Location", "Review"' in overlay
assert 'SandboxCreatorOverlay.Accept();' in overlay
assert 'MBGameManager.StartNewGame(new CUSandboxNoIntroGameManager(' in overlay
assert 'Create character' in ET.tostring(xml,encoding="unicode")
assert 'new BodyGeneratorView(' not in overlay
assert 'CharacterCreationState' in doc
assert "still uses the separate native character creation" in doc

print("PASS: supported/irrelevant/unavailable field semantics, locked CU tabs and truthful noneditable advanced controls (STATIC)")
