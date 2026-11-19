#!/usr/bin/env python3
"""v0.47 native-first UI and age slider source contract.

Checks Bannerlord asset REFERENCES, not runtime sprite availability.
An actual 1.5.7 game installation and in-game mouse test are required.
"""
from pathlib import Path
from xml.etree import ElementTree as ET
r=Path(__file__).resolve().parent.parent
src=r/"Source/CalradiaUnified"
gui=r/"Modules/CalradiaUnified/GUI/Prefabs"
def read(name):return (src/name).read_text(encoding="utf-8")
def prefab(name):return ET.parse(gui/(name+".xml")).getroot()
stats=read("CUNativeStatsLayer.cs")
shell=read("CUNativeStateShell.cs")
culture=read("CUNativeCultureLayer.cs")
overlay=read("SandboxCreatorOverlay.cs")
no_intro=read("CUSandboxNoIntroGameManager.cs")
age_probe=read("CUNativeAgeMenuProbe.cs")
g=prefab("CUGuidedCreator")
s=prefab("CUNativeStatsEditor")
gp=prefab("CUGuidedCreatorPlainFallback")
sp=prefab("CUNativeStatsEditorPlainFallback")
safe=prefab("CUNativeStatsEditorSafe")
native_panel=r"SPGeneral\EscapeMenu\escape_panel"
native_gradient=r"General\CharacterCreation\character_creation_background_gradient"
for ui in (g,s):
    sprites={w.get("Sprite") for w in ui.iter("Widget")}
    assert native_panel in sprites
    assert native_gradient in sprites
    assert native_panel.replace("\\","\\\\") not in sprites
    assert native_gradient.replace("\\","\\\\") not in sprites
for ui in (gp,sp,safe):
    sprites={w.get("Sprite") for w in ui.iter("Widget")}
    assert native_panel not in sprites
    assert native_gradient not in sprites
house=read("CUNativeHouseLayer.cs")
assert '"CUGuidedCreatorPlainFallback",vm' in house
assert '"cu_guided_house_art_fallback"' in house
assert '"cu_guided_house_unavailable"' in house
assert '"CUGuidedCreatorPlainFallback"' in overlay
assert '"CUNativeStatsEditorPlainFallback",vm' in stats
assert '"CUNativeStatsEditorSafe",vm' in stats

slider=list(s.iter("SliderWidget"))
assert len(slider)==7  # unchanged native Age plus six fixed stats sliders
sl=next(w for w in slider if w.get("Id")=="CUPlayerAgeSlider")
assert sl.get("ValueInt")=="@PlayerAge"
assert sl.get("MinValueInt")=="18"
assert sl.get("MaxValueInt")=="80"
assert sl.get("DiscreteIncrementInterval")=="1"
assert sl.get("AlignmentAxis")=="Horizontal"
assert sl.get("Handle")=="CUPlayerAgeHandle"
assert sl.get("SliderArea")=="CUPlayerAgeTrack"
handle=next(w for w in sl.iter("ButtonWidget")
            if w.get("Id")=="CUPlayerAgeHandle")
assert handle.get("Brush")=="ButtonBrush"
assert "ReadNativeSliderValue();" in stats
assert 'root.FindChild("CUPlayerAgeSlider",true)' in stats
assert "FindSliderWidget(movie.Movie.RootWidget)" in stats
assert "root.GetChild(i)" in stats
assert "FindSliderInTree(root,0)" in stats
assert "private static SliderWidget cachedAgeSlider" in stats
assert "cachedAgeSlider.ValueInt" in stats
assert "cachedAgeSlider.Handle.IsPressed" in stats
assert "cu_native_age_slider_value_changed" in stats
assert 'source=real_gauntlet_tree' in stats
assert 'fallback=one_year_buttons' in stats
assert "cu_native_age_slider_drag" in stats
assert "public int PlayerAge" in stats
assert "ExecuteAgeIncrease" in stats and "ExecuteAgeDecrease" in stats

# Original native visuals and callbacks take precedence over CU chrome.
assert "if(SandboxCreatorOverlay.HasAcceptedCustomStart)" in culture
assert "if(SandboxCreatorOverlay.HasAcceptedCustomStart)" in shell
assert "CUNativeCultureLayer.Clear();" in shell
assert "CUNativeControlSkin.Clear();" in shell
assert "CUNativeLayoutAlignment.Clear();" in shell
assert 'CUNativeStatsLayer.Tick(screen,' in shell
assert 'CUNativeHouseLayer.Tick(screen,' in shell
assert 'CUNativeAgeMenuProbe.Tick(' in shell
assert 'CUNativeAgeMenuProbe.Clear();' in shell
assert 'cu_native_final_narrative_menu' in age_probe
assert 'manager.GetSuitableNarrativeMenuOptions()' in age_probe
assert 'manager.NarrativeMenus' in age_probe
assert 'manager.CurrentMenu' in age_probe
assert 'manager.OnNarrativeMenuOptionSelected(' not in age_probe
assert 'manager.TrySwitchToNextMenu(' not in age_probe
assert 'manager.NextStage(' not in age_probe

# Banner Back reenters Traits on the game-owned stage, then confirms
# through the original NextStage instead of creating a new game state.
assert "previousNativeStage" in stats
assert "returningFromNativeBanner=true;" in stats
assert "CUNativeStatsNavigationPolicy.Attributes" in stats
assert "manager.NextStage();" in stats
assert '"cu_native_banner_back_to_traits"' in stats
assert '"cu_native_stats_return_to_banner"' in stats
assert "no_double_previous_stage=true" in stats

# The pinned age narrative menu must remain game-owned until a live
# run confirms exact menu/option IDs and original bonus callbacks.
probe=read("CUNativeAgeMenuProbe.cs")
assert "manager.CurrentMenu" in probe
assert "manager.NarrativeMenus" in probe
assert "manager.GetSuitableNarrativeMenuOptions()" in probe
assert '"20" || x=="30" || x=="40" || x=="50"' in probe
assert '"cu_native_final_narrative_menu"' in probe
assert "OnNarrativeMenuOptionSelected(" not in probe
assert "CUNativeAgeMenuProbe.Tick(" in shell
assert "CUNativeAgeMenuProbe.Clear();" in shell

# Skip ONLY the accepted custom new-Sandbox intro. Preserve native
# GameState, saved-game/other game manager paths, and fallback.
assert "public override void OnLoadFinished()" in no_intro
assert "LoadingSavedGame" in no_intro
assert "base.OnLoadFinished();" in no_intro
assert 'CreateState<CharacterCreationState>()' in no_intro
assert 'new CUSandboxNoIntroGameManager(' in overlay

# Source should reference game-owned assets, not redistribute
# the game's textures in this project.
asset_dir=r/"Modules/CalradiaUnified/GUI/SpriteParts"
if asset_dir.exists():
    for path in asset_dir.rglob("*"):
        if path.is_file():
            assert not path.name.lower() in {
                "escape_panel.png","character_creation_background_gradient.png"}

print("PASS: v047 game sprite references, live-widget age slider path, truthful native-age probe, Banner Back bridge and intro skip (STATIC ONLY)")
