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
for name in (
    "Input.IsKeyDown(InputKey.LeftMouseButton)",
    "cachedAgeSlider.EventManager.MousePosition",
    "track.GlobalPosition.X",
    "cachedAgeSlider.GlobalPosition.Y",
    "track.Size.X", "handle.Size.X",
    "ScreenManager.FocusedLayer!=layer",
    "pointerWasDown", "pointerDragActive", "pointerGrabOffset",
    "pointerDragChangeCount",
    "ReadNativeSliderPointer();",
    "activeVM.PlayerAge=age;",
    "cachedAgeSlider.ValueInt=age;",
    "cu_native_age_pointer_press",
    "cu_native_age_pointer_drag",
    "cu_native_age_pointer_release",
    "cu_native_age_slider_value_changed",
):
    assert name in layer,name
assert "AgeFromSliderPointer(" in policy
assert "trackWidth-handleWidth" in policy
assert "MidpointRounding.AwayFromZero" in policy
for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback"):
    prefab=ET.parse(ui/(name+".xml")).getroot()
    sliders=list(prefab.iter("SliderWidget"))
    assert len(sliders)==1
    sl=sliders[0]
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
print("PASS: v050 pointer drag, actual Gauntlet geometry, bounded ages and fixed end labels (STATIC)")
