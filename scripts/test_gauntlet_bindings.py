#!/usr/bin/env python3
"""Static Gauntlet source/VM binding audit; does not test in-game rendering."""
import re
import xml.etree.ElementTree as ET
from pathlib import Path
ROOT = Path(__file__).resolve().parent.parent
path = ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUSettingsPanel.xml"
vm = (ROOT/"Source/CalradiaUnified/NativeSettingsPanel.cs").read_text()
module = (ROOT/"Source/CalradiaUnified/NativeSettingsBehavior.cs").read_text()
root = ET.parse(path).getroot()
assert root.tag == "Prefab"
commands = []
data_sources = []
sliders = 0
for element in root.iter():
    if element.tag == "SliderWidget":
        sliders += 1
    for name, value in element.attrib.items():
        if name.startswith("Command."):
            commands.append(value)
        if value.startswith("@"):
            data_sources.append(value[1:])
assert sliders >= 1
assert len(commands) >= 9
assert len(set(commands)) >= 9
for name in commands:
    assert re.search(r"\bpublic\s+void\s+" + re.escape(name) + r"\s*\(", vm), name
for name in data_sources:
    assert re.search(r"\bpublic\s+(?:int|bool|string)\s+" +
                     re.escape(name) + r"\b", vm), name
assert set(("ExecuteCoreTab", "ExecuteKingdomTab", "ExecuteCastleTab")).issubset(commands)
assert {"ExecuteApply", "ExecuteToggle", "ExecuteClose"}.issubset(commands)
assert "OpenExperimentalPanel()" in module
print("PASS: Gauntlet XML syntax, three sections, slider, click handlers and VM bindings")
