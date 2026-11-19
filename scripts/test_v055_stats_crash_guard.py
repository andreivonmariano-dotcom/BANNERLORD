#!/usr/bin/env python3
"""v055: age->stats hard-exit regression contracts on all 3 prefabs.

A successful CI run is not evidence of actual Gauntlet input stability.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
code=(src/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
policy=(src/"SandboxStatsPagePolicy.cs").read_text(encoding="utf-8")
assert "public const int VisibleRows=6;" in policy
assert 'rows.Clear();' in code
assert 'SandboxStatsPagePolicy.PageSize(' in code
assert 'SandboxStatsPagePolicy.StartIndex(' in code
assert 'statsRowPageIndex' in code
assert 'statsRowPageCount' in code
assert 'cu_stats_page_refresh_enter' in code
assert 'cu_stats_page_rows_built' in code
assert 'cu_stats_rows_page_button' in code
assert 'STATS "+(statsRowPageIndex+1)' in code
assert "OnPropertyChangedWithValue(SkillGroup,\"SkillGroup\")" in code
assert "statsRowPageIndex=0;" in code
assert 'CUNativeStatsNavigationPolicy.NextPage(pageIndex)' in code
assert 'ReadNativeSliderValue();' in code

for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    document=ET.parse(gui/(name+".xml")).getroot()
    assert len(list(document.iter("ScrollablePanel")))==0,name
    assert len(list(document.iter("Standard.VerticalScrollbar")))==0,name
    viewport=next(x for x in document.iter("Widget")
        if x.get("Id")=="CUStatsPageViewport")
    assert viewport.get("IsVisible")=="@IsStatsPage",name
    assert viewport.get("ClipContents")=="true",name
    assert viewport.get("DoNotAcceptEvents")=="true",name
    assert viewport.get("DoNotPassEventsToChildren")=="false",name
    assert viewport.get("SuggestedHeight")=="342"
    assert not list(viewport.iter("ListPanel"))
    assert not list(viewport.iter("ItemTemplate"))
    stat_sliders=[x for x in viewport.iter("SliderWidget")
        if (x.get("Id") or "").startswith("CUFixedStatSlider")]
    assert len(stat_sliders)==6,name
    for i,slider in enumerate(stat_sliders,1):
        assert slider.get("ValueInt")==f"@Slot{i}Value",name
        assert slider.get("MinValueInt")==f"@Slot{i}Min",name
        assert slider.get("MaxValueInt")==f"@Slot{i}Max",name
    assert len(list(document.iter("SliderWidget")))==(
        6 if name.endswith("Safe") else 7),name
    btns=list(document.iter("ButtonWidget"))
    for command in ("ExecutePreviousGroup","ExecuteNextGroup"):
        candidates=[w for w in btns if w.get("Command.Click")==command]
        assert len(candidates)==1,(name,command)
        assert candidates[0].get("IsVisible")=="@IsStatsPage",(name,command)
    assert any(w.get("Text")=="Previous stats"
        for w in document.iter("TextWidget"))
    assert any(w.get("Text")=="Next stats"
        for w in document.iter("TextWidget"))
    assert any(w.get("Text")=="@SkillGroup"
        and w.get("IsVisible")=="@IsStatsPage"
        for w in document.iter("TextWidget"))
    assert any(w.get("Command.Click")=="ExecuteEditPlayer" for w in btns)
    assert any(w.get("Command.Click")=="ExecuteEditSpouse" for w in btns)
    assert any(w.get("Command.Click")=="ExecuteContinue" for w in btns)
print("PASS: v055 bounded 6-row native stats viewport, safe page input and preserved age widgets (STATIC ONLY)")
