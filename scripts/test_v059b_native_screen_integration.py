#!/usr/bin/env python3
"""v0.59b generated native-right-pane visual source checks.

This validates prefab structure and stable callbacks, not engine rendering,
texture importing, mouse hit testing or the user's live gameplay.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

root=Path(__file__).resolve().parents[1]
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
source=(root/"Source/CalradiaUnified/CUNativeStatsLayer.cs").read_text(encoding="utf-8")
def read(name):
    return ET.parse(gui/(name+".xml")).getroot()

guided=read("CUGuidedCreator")
guided_fallback=read("CUGuidedCreatorPlainFallback")
stats=read("CUNativeStatsEditorV059")
legacy=read("CUNativeStatsEditor")

guided_commands={
    "ExecutePreviousChoice","ExecuteNextChoice","ExecuteToggle",
    "ExecuteDecrease","ExecuteIncrease","ExecuteExact",
    "ExecuteEditText","ExecuteNativeDefault","ExecuteBack",
    "ExecuteContinue","ExecuteAdvanced",
}
for name,movie in (("native",guided),("fallback",guided_fallback)):
    actual=[w.get("Command.Click") for w in movie.iter("ButtonWidget")]
    assert set(actual)==guided_commands,(name,set(actual)^guided_commands)
    assert len(actual)==len(guided_commands),name
    assert any(w.get("SuggestedWidth")=="604" and
        w.get("HorizontalAlignment")=="Right"
        for w in movie.iter("Widget")),name
    for key in ("@StepTitle","@Hint","@SelectedValue",
                "@Chapter","@Summary","@StepProgress","@ContinueLabel"):
        assert any(w.get("Text")==key
            for w in movie.iter("TextWidget")),(name,key)

stat_commands=[w.get("Command.Click") for w in stats.iter("ButtonWidget")]
for i in range(1,7):
    row=next(w for w in stats.iter("Widget")
        if w.get("Id")==f"CUFixedStatRow{i}")
    assert row.get("SuggestedHeight")=="66"
    assert row.get("PositionYOffset")==str((i-1)*68)
    for k in ("Name","Native","Selected"):
        assert any(w.get("Text")==f"@Slot{i}{k}"
            for w in row.iter("TextWidget")),(i,k)
    for action in ("Decrease","Increase","Exact"):
        assert stat_commands.count(f"ExecuteSlot{i}{action}")==1,(i,action)

for cmd in ("ExecuteBack","ExecuteContinue","ExecuteEditPlayer",
            "ExecuteEditSpouse","ExecuteNative","ExecuteBalanced",
            "ExecuteUnrestricted","ExecutePreviousGroup","ExecuteNextGroup",
            "ExecuteAgeDecrease","ExecuteAgeIncrease",
            "ExecuteEnterExactAge","ExecuteKeepNativeAge",
            "ExecutePreviousBonus","ExecuteNextBonus"):
    assert stat_commands.count(cmd)==1,cmd

sliders=list(stats.iter("SliderWidget"))
assert len(sliders)==1
age=sliders[0]
assert age.get("Id")=="CUPlayerAgeSlider"
assert age.get("ValueInt")=="@PlayerAge"
assert age.get("MinValueInt")=="18" and age.get("MaxValueInt")=="80"
assert age.get("IsDiscrete")=="true"
assert age.get("Handle")=="CUPlayerAgeHandle"
assert age.get("SliderArea")=="CUPlayerAgeTrack"
assert {w.get("Id") for w in age.iter()} >= {
    "CUPlayerAgeHandle","CUPlayerAgeTrack"
}
assert not list(stats.iter("ListPanel"))
assert not list(stats.iter("ScrollablePanel"))
assert not [w for w in stats.iter("SliderWidget")
            if (w.get("Id") or "").startswith("CUFixedStatSlider")]
assert 'next.LoadMovie("CUNativeStatsEditorV059",vm)' in source
assert 'next.LoadMovie("CUNativeStatsEditor",vm)' in source
assert 'next.LoadMovie(\n                            "CUNativeStatsEditorPlainFallback",vm)' in source
assert 'cu_native_v059_layout_fallback' in source
assert len(list(legacy.iter("SliderWidget")))==1
assert any(w.get("IsVisible")=="@IsAgePage"
    for w in stats.iter("Widget"))
assert any(w.get("IsVisible")=="@IsStatsPage"
    for w in stats.iter("Widget"))

print("PASS: v0.59b native-right guided/age/stats composition, commands and safe fallback (STATIC ONLY)")
