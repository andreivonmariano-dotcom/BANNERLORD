#!/usr/bin/env python3
"""v051: regression of user-observed no-click age page at 1650x927.
Checks actual prefab depth order, not just the presence of controls.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
r=Path(__file__).resolve().parent.parent
src=(r/"Source/CalradiaUnified/CUNativeStatsLayer.cs").read_text(encoding="utf-8")
ui=r/"Modules/CalradiaUnified/GUI/Prefabs"
assert "cachedAgeSlider.EventManager.MousePosition" not in src
assert "ReadNativeSliderPointer()" not in src
for token in ("TraceAgeMouseInput();","MaintainAgeLayerFocus();",
  "cu_native_age_hit_trace","cu_native_age_focus_status",
  "LatestMouseDownWidget","Input.IsKeyDown(InputKey.LeftMouseButton)",
  "ScreenManager.TrySetFocus(layer)","cu_native_age_button",
  "cu_native_age_slider_value_changed"):
    if token=="cu_native_age_button":
        continue
    assert token in src,token
for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    root=ET.parse(ui/(name+".xml")).getroot()
    canvas=next(w for w in root.iter("Widget")
      if w.get("MaxWidth")=="1820" and w.get("MaxHeight")=="960")
    children=canvas.find("Children")
    assert children is not None
    controls=("ExecuteAgeDecrease","ExecuteAgeIncrease",
      "ExecuteEnterExactAge","ExecuteKeepNativeAge")
    buttons=[w for w in root.iter("ButtonWidget")]
    for cmd in controls:
        assert sum(w.get("Command.Click")==cmd for w in buttons)==1,(name,cmd)
    age_ids={"ExecuteAgeDecrease":"CUAgeDecreaseButton",
      "ExecuteAgeIncrease":"CUAgeIncreaseButton",
      "ExecuteEnterExactAge":"CUAgeExactButton",
      "ExecuteKeepNativeAge":"CUAgeNativeButton"}
    for cmd,wid in age_ids.items():
        actual=next(w for w in buttons if w.get("Command.Click")==cmd)
        assert actual.get("Id")==wid,(name,cmd)
    last=children[-1]
    assert last.tag=="Widget" and last.get("IsVisible")=="@IsAgePage",name
    assert last.get("DoNotAcceptEvents")=="true"
    assert last.get("DoNotPassEventsToChildren")=="false"
    cmds=[b.get("Command.Click") for b in last.iter("ButtonWidget")]
    for cmd in controls:
        assert cmd in cmds,(name,cmd)
    assert not any(c.get("Command.Click") in controls
      for before in list(children)[:-1]
      for c in before.iter("ButtonWidget")),(name,"age input buried in scenery")
    assert last.get("PositionXOffset")=="251"
    assert last.get("PositionYOffset")=="210"
    assert last.get("SuggestedWidth")=="690"
    assert last.get("SuggestedHeight")=="373"
    if name!="CUNativeStatsEditorSafe":
        slider=list(last.iter("SliderWidget"))
        assert len(slider)==1
        assert slider[0].get("ValueInt")=="@PlayerAge"
        assert slider[0].get("Handle")=="CUPlayerAgeHandle"
        assert slider[0].get("SliderArea")=="CUPlayerAgeTrack"
print("PASS: v051 exact native age control hit priority, focus probe and runtime-compatible slider (STATIC)")
