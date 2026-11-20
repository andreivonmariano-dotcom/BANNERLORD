#!/usr/bin/env python3
"""v0.59e corrects overlaid native creator controls, keeps 3D scenes.

These are source/XML contracts only. The pinned game must still prove
native Gauntlet root visibility, text fit and pointer hit-testing live.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

root=Path(__file__).resolve().parents[1]
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
src=root/"Source/CalradiaUnified"
guard=(src/"CUNativeCreatorUiIsolation.cs").read_text(encoding="utf-8")
stats=(src/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
house=(src/"CUNativeHouseLayer.cs").read_text(encoding="utf-8")
world_layer=(src/"SandboxCreatorMenuLayer.cs").read_text(encoding="utf-8")
world_vm=(src/"CUGuidedCreatorVM.cs").read_text(encoding="utf-8")

assert "record.Root.IsHidden=true" in guard
assert "roots[i].Root.IsHidden=roots[i].WasHidden" in guard
assert "native.UIContext" in guard
assert 'new[]{"Root","RootWidget"}' in guard
assert "screen.Layers" in guard
assert 'name.StartsWith("CU"' in guard
assert "scene_layers_touched=0" in guard
assert "Restore();" in guard
assert "nativeUi.Hide(screen,next)" in stats
assert "nativeUi.Hide(screen,next)" in house
assert stats.index("nativeUi.Hide(screen,next)")<stats.index("screen.AddLayer(next);")
assert house.index("nativeUi.Hide(screen,next)")<house.index("screen.AddLayer(next);")
assert "nativeUi.Restore();" in stats
assert "nativeUi.Restore();" in house
assert "CUNativeStatsEditor" in stats
assert "CUGuidedCreatorLegacyOpaque" in house
assert (gui/"CUGuidedCreatorLegacyOpaque.xml").exists()

for name in ("CUGuidedCreator","CUGuidedCreatorPlainFallback",
             "CUGuidedCreatorV059Art"):
    path=gui/(name+".xml")
    xml=ET.parse(path).getroot()
    raw=path.read_text(encoding="utf-8")
    assert '#0E0A0690' not in raw,name
    assert 'cu_world_illustration' not in raw,name
    assert 'character_creation_background_gradient' not in raw,name
    assert raw.count('Color="#231B12FF"')==1,name
    assert any(w.get("SuggestedWidth")=="704" and
               w.get("IsVisible")=="@IsWorldSetup"
               for w in xml.iter("Widget")),name
    left_texts=[w for w in xml.iter("TextWidget")
                if w.get("Text") in ("@Title","@PhaseLabel",
                                    "@Summary","@StepProgress",
                                    "Your beginning")]
    assert len(left_texts)==5,name
    assert all(w.get("IsVisible")=="@IsWorldSetup"
               for w in left_texts),name
    assert any(w.get("Text")=="@Chapter" and
               w.get("IsVisible")!="@IsWorldSetup"
               for w in xml.iter("TextWidget")),name
    commands={x.get("Command.Click")
              for x in xml.iter("ButtonWidget")}
    assert {"ExecuteBack","ExecuteContinue",
            "ExecuteAdvanced","ExecuteNextChoice",
            "ExecutePreviousChoice"}<=commands,name

for name in ("CUNativeStatsEditorV059",
             "CUNativeStatsEditorV059Art"):
    path=gui/(name+".xml")
    xml=ET.parse(path).getroot()
    raw=path.read_text(encoding="utf-8")
    assert '#0A080625' not in raw,name
    assert 'Color="#241C15FF"' in raw,name
    assert len(list(xml.iter("SliderWidget")))==1,name
    assert any(w.get("Id")=="CUPlayerAgeSlider"
               for w in xml.iter("SliderWidget")),name
    assert all(any(w.get("Command.Click")==f"ExecuteSlot{i}{k}"
                   for w in xml.iter("ButtonWidget"))
               for i in range(1,7)
               for k in ("Decrease","Increase","Exact")),name
    assert not list(xml.iter("ListPanel")),name
    assert not list(xml.iter("ScrollablePanel")),name

assert "SandboxCreatorMenuLayer" in world_layer
assert "ScreenManager.PushScreen" not in world_layer.replace(
    "// Movie", "// Movie")
assert "Drag a slider" not in stats.split(
    "[DataSourceProperty] public string TargetHelp",1)[1].split(
    "[DataSourceProperty]",1)[0]
assert "scrollable page" not in stats.split(
    "[DataSourceProperty] public string TargetHelp",1)[1].split(
    "[DataSourceProperty]",1)[0]
assert 'IsWorldSetup' in world_vm
print("PASS: v0.59e native UI hide/restore, visible 3D scene and animated World (STATIC ONLY)")
