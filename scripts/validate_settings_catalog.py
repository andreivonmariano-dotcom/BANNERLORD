#!/usr/bin/env python3
"""Prevent drift between live Settings fields, parser clamps and game UI catalog."""
import re
from pathlib import Path
ROOT = Path(__file__).resolve().parent.parent
settings = (ROOT/"Source/CalradiaUnified/Settings.cs").read_text()
catalog = (ROOT/"Source/CalradiaUnified/SettingsCatalog.cs").read_text()

declared = dict((key, kind) for kind, key in re.findall(
    r"public\s+(bool|int)\s+(\w+)\s*=\s*(?:true|false|\d+);", settings))
metadata = re.findall(
    r'new SettingDescriptor\("([^"]+)",\s*"[^"]+",\s*"[^"]+",\s*'
    r'(true|false),\s*(\d+),\s*(\d+),\s*(\d+),\s*(true|false)\)', catalog)
assert len(metadata) == len(declared) == 64, (len(metadata), len(declared))
unique_keys = [row[0] for row in metadata]
assert len(unique_keys) == len(set(unique_keys))
assert set(unique_keys) == set(declared)
bounds = dict((key, (int(a), int(b))) for key, a, b in re.findall(
    r'key == "([^"]+)"[^\n]*?output\.\w+ = Clamp\(n, (\d+), (\d+)\);',
    settings))
for key, isbool, lo, hi, step, experimental in metadata:
    assert (isbool == "true") == (declared[key] == "bool"), key
    if declared[key] == "int":
        assert key in bounds, key
        assert (int(lo), int(hi)) == bounds[key], (key, lo, hi, bounds.get(key))
        assert int(step) >= 1 and int(lo) < int(hi), key
    assert experimental in ("true", "false")
print("PASS: 64 settings, parser field types, numeric clamp bounds and unique catalog keys")
