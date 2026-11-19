#!/usr/bin/env python3
"""Inspect a compiled Calradia Unified package without claiming in-game testing."""
import argparse
import hashlib
import json
import pathlib
import xml.etree.ElementTree as ET
import zipfile

def verify(filename, version):
    source = pathlib.Path(filename)
    assert source.is_file() and source.stat().st_size > 0, "Missing ZIP"
    with zipfile.ZipFile(source, "r") as archive:
        names = [n for n in archive.namelist() if not n.endswith("/")]
        prefix = "Modules/CalradiaUnified/"
        base = prefix + "bin/Win64_Shipping_Client/CalradiaUnified.dll"
        manifest = prefix + "SubModule.xml"
        ini = prefix + "ModuleData/calradia-config.ini"
        exporter = prefix + "ModuleData/Export-CU-Diagnostics.ps1"
        assert len(names) == len(set(names)), "Duplicate ZIP entries"
        assert all(n.startswith(prefix) and ".." not in n.split("/") for n in names), "Unexpected path"
        assert all(n in (base, manifest) or n.startswith(prefix+"ModuleData/")
                   or n.startswith(prefix+"GUI/Prefabs/") for n in names), "Non-runtime file"
        gauntlet_prefab = prefix + "GUI/Prefabs/CUSettingsPanel.xml"
        assert gauntlet_prefab in names, "Native UI prefab missing"
        assert ET.fromstring(archive.read(gauntlet_prefab)).tag == "Prefab", "Invalid prefab XML"
        # The former pre-game screen has an observed broken layout and blocked
        # Back navigation. It is intentionally excluded from shipped builds.
        origin_prefab = prefix + "GUI/Prefabs/CUOriginPanel.xml"
        assert origin_prefab not in names, "Broken standalone origin UI must not ship"
        new_creator = prefix + "GUI/Prefabs/CUSandboxCreator.xml"
        assert new_creator in names, "New Sandbox creator movie missing"
        assert ET.fromstring(archive.read(new_creator)).tag == "Prefab", "Invalid Sandbox creator XML"
        # The opt-in stage has a UNIQUE type; native FaceGen remains intact.
        for name in ("CUNativeOriginReview.xml","CUFamilyPortraits.xml","CUFaceWorkshop.xml","CUNativeStateShell.xml"):
            path = prefix + "GUI/Prefabs/" + name
            assert path in names, "Required new native or family movie missing: " + name
            assert ET.fromstring(archive.read(path)).tag == "Prefab", "Invalid native/family XML"
        # Never ship the experimental view that conflicted with native
        # character creation during the user's v0.22 culture-stage crash.
        assert prefix+"GUI/Prefabs/CUFaceStageHeader.xml" not in names, "Unsafe FaceGen header retained"
        assert all(not n.lower().endswith((".pdb", ".exe", ".cs", "not_compiled.txt")) for n in names), "Developer file"
        assert all(n in names for n in (base, manifest, ini, exporter)), "Required module file absent"
        assert [n for n in names if n.lower().endswith(".dll")] == [base], "Unexpected DLL count"
        assert archive.testzip() is None, "ZIP CRC failure"
        dll = archive.read(base)
        assert len(dll) > 1024, "DLL is empty or unexpectedly small"
        root = ET.fromstring(archive.read(manifest))
        assert root.find("Id").attrib["value"] == "CalradiaUnified"
        assert root.find("Version").attrib["value"] == version
        assert root.find(".//DLLName").attrib["value"] == "CalradiaUnified.dll"
    return {"result":"PASS","version":version,"runtime_files":len(names),
            "dll_bytes":len(dll),
            "dll_sha256":hashlib.sha256(dll).hexdigest(),
            "zip_bytes":source.stat().st_size,
            "zip_sha256":hashlib.sha256(source.read_bytes()).hexdigest(),
            "test_scope":"CRC, runtime layout, DLL nonempty, version, identities only"}

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("zipfile")
    p.add_argument("--version", required=True)
    args = p.parse_args()
    print(json.dumps(verify(args.zipfile,args.version),indent=2))
