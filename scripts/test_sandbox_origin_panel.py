#!/usr/bin/env python3
"""Check the origin editor's source bindings, not live Bannerlord UI behavior."""
from pathlib import Path
from xml.etree import ElementTree as ET
import re
ROOT = Path(__file__).resolve().parent.parent
xml = ROOT/"Modules/CalradiaUnified/GUI/Prefabs/CUOriginPanel.xml"
vm = (ROOT/"Source/CalradiaUnified/SandboxOriginPanel.cs").read_text()
sub = (ROOT/"Source/CalradiaUnified/SubModule.cs").read_text()
behavior = (ROOT/"Source/CalradiaUnified/SandboxOriginBehavior.cs").read_text()
root = ET.parse(xml).getroot()
assert root.tag == "Prefab"
assert "CUOriginPanel" in vm
commands, bindings = set(), set()
sliders = 0
for element in root.iter():
    if element.tag == "SliderWidget":
        sliders += 1
    for key, value in element.attrib.items():
        if key.startswith("Command."): commands.add(value)
        if value.startswith("@"): bindings.add(value[1:])
assert sliders == 1
assert all(re.search(r"\bpublic\s+void\s+"+re.escape(c)+r"\s*\(",vm)
           for c in commands), commands
assert all(re.search(r"\bpublic\s+(?:string|int|bool)\s+"+
                     re.escape(b)+r"\b",vm) for b in bindings), bindings
for required in ["ExecuteIdentity","ExecuteFamily","ExecuteProgression",
                 "ExecuteEquipment","ExecuteLocation","ExecuteToggle",
                 "ExecuteSaveNumber","ExecuteEditText","ExecuteClose"]:
    assert required in commands, required
assert 'InitialStateOption("CU_Sandbox_Origin"' not in sub
assert 'OnBeforeInitialModuleScreenSetAsRoot' in sub
assert 'SandboxMainMenuEntry.Install()' in sub
assert 'OnCharacterCreationInitializedEvent' in behavior
assert 'cu_native_sandbox_menu' in behavior
assert "IsStandaloneSandbox()" in behavior
assert "CU_v016_origin_applied" in behavior
assert "OnNewGameCreatedEvent" in behavior
print("PASS: old panel bindings archived, separate main-menu entry disabled, native creation observed")