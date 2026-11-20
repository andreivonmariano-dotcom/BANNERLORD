#!/usr/bin/env python3
"""v050 screenshot regression: real mouse drag of visible age slider.
Only source/XML checks. A game test still decides mouse acceptance.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
source=root/"Source/CalradiaUnified"
ui=root/"Modules/CalradiaUnified/GUI/Prefabs"
layer=(source/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
policy=(source/"SandboxFamilyAgePolicy.cs").read_text(encoding="utf-8")
# Superseded by v051. The installed Bannerlord EventManager has an
# incompatible MousePosition return signature: never call that getter.
assert "cachedAgeSlider.EventManager.MousePosition" not in layer
assert "ReadNativeSliderPointer()" not in layer
assert "cu_native_age_hit_trace" in layer
assert "TraceAgeMouseInput();" in layer
assert "MaintainAgeLayerFocus();" in layer
assert "cachedAgeSlider.ValueInt" in layer
assert "cu_native_age_slider_value_changed" in layer
assert "AgeFromSliderPointer(" in policy

for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback"):
    prefab=ET.parse(ui/(name+".xml")).getroot()
    sliders=list(prefab.iter("SliderWidget"))
    # v0.58 isolates the six stat-slider binding sites, retaining
    # only the known-working native Age slider.
    assert len(sliders)==1
    sl=next(x for x in sliders if x.get("Id")=="CUPlayerAgeSlider")
    assert sl.get("ValueInt")=="@PlayerAge"
    assert sl.get("MinValueInt")=="18" and sl.get("MaxValueInt")=="80"
    assert sl.get("Handle")=="CUPlayerAgeHandle"
    assert sl.get("SliderArea")=="CUPlayerAgeTrack"
for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    prefab=ET.parse(ui/(name+".xml")).getroot()
    labels=[x.get("Text") for x in prefab.iter("TextWidget")]
    assert "18 YEARS" in labels and "80 YEARS" in labels
    assert "18                                         80 YEARS" not in labels
    commands=[x.get("Command.Click") for x in prefab.iter("ButtonWidget")]
    assert {"ExecuteAgeIncrease","ExecuteAgeDecrease",
            "ExecuteEnterExactAge"}<=set(commands)
print("PASS: v050 age input contracts retained, incompatible pointer API removed by v051 (STATIC)")
