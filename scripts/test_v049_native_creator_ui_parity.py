#!/usr/bin/env python3
"""Game-native creator continuity and typed slider gate. STATIC ONLY."""
from pathlib import Path
from xml.etree import ElementTree as ET
r=Path(__file__).resolve().parent.parent
src=r/"Source/CalradiaUnified"
gui=r/"Modules/CalradiaUnified/GUI/Prefabs"
def text(n):return (src/n).read_text(encoding="utf-8")
stats=text("CUNativeStatsLayer.cs")
shell=text("CUNativeStateShell.cs")
house=text("CUNativeHouseLayer.cs")
menu=text("CUNativeAgeMenuProbe.cs")
assert "private static SliderWidget cachedAgeSlider" in stats
assert "root.FindChild(\"CUPlayerAgeSlider\",true)" in stats
assert "root.GetChild(i)" in stats
assert "cachedAgeSlider.MinValueInt!=18" in stats
assert "cachedAgeSlider.MaxValueInt!=80" in stats
assert "cachedAgeSlider.Handle.IsPressed" in stats
assert "lastModelValue" in stats and "lastSliderValue" in stats
assert 'cu_native_age_slider_value_changed' in stats
assert 'cu_native_age_slider_drag' in stats
assert 'fallback=one_year_buttons_and_exact_entry' in stats
assert "CUNativeControlSkin.Clear();" in shell
assert "CUNativeCultureLayer.Clear();" in shell
assert "CUNativeFaceDockTrial.Clear();" in shell
assert "CUNativeLayoutAlignment.Clear();" in shell
assert "CUNativeStatsLayer.Tick(screen," in shell
assert "CUNativeHouseLayer.Tick(screen," in shell
assert '"CUGuidedCreatorPlainFallback",vm' in house
assert "cu_guided_house_unavailable" in house
assert "manager.GetSuitableNarrativeMenuOptions()" in menu
for name in ("CUGuidedCreator","CUNativeStatsEditor"):
    node=ET.parse(gui/(name+".xml")).getroot()
    sprites=[w.get("Sprite") for w in node.iter("Widget")]
    assert r"SPGeneral\EscapeMenu\escape_panel" in sprites
    assert r"General\CharacterCreation\character_creation_background_gradient" in sprites
    assert sum(b.get("Brush")=="ButtonBrush" for b in node.iter("ButtonWidget"))>=5
for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    node=ET.parse(gui/(name+".xml")).getroot()
    cmds=[w.get("Command.Click") for w in node.iter("ButtonWidget")]
    assert cmds.count("ExecuteEnterExactAge")==1
    assert cmds.count("ExecuteAgeIncrease")==1
    assert cmds.count("ExecuteAgeDecrease")==1
assert (r/"scripts/Audit-CU-Native-Creator-Assets.ps1").exists()
print("PASS: native Gauntlet age input, fallback household art, original character creation screens and installed-art audit (STATIC ONLY)")
