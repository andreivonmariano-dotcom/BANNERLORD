#!/usr/bin/env python3
"""v056 real-crash-driven regression: no Gauntlet runtime row template.

Latest user session: Age->All Stats, six row VMs built at 15:14:33Z,
then abrupt game exit without managed exception or Windows event.
The entire ListPanel ItemTemplate is now removed.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=(root/"Source/CalradiaUnified/CUNativeStatsLayer.cs").read_text(encoding="utf-8")
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
assert "private readonly List<CUNativeStatRowVM> rows" in src
assert "MBBindingList<CUNativeStatRowVM>" not in src
assert "OnPropertyChangedWithValue(selected,\"SelectedInt\")" not in src
assert "NotifyStaticSlots();" in src
assert "cu_stats_static_slots_notified" in src
assert 'item_template=false' in src
assert 'fixed_slots=6' in src
assert "SandboxStatsPagePolicy.PageSize(" in src
for i in range(1,7):
    for name in (f"Slot{i}Value",f"Slot{i}Min",f"Slot{i}Max",
                 f"Slot{i}Increment",f"Slot{i}Native",
                 f"Slot{i}Selected",f"Slot{i}Name",f"Slot{i}Visible"):
        assert f"public " in src and name in src,name
        if name.endswith(("Value","Min","Max","Increment")):
            assert f'OnPropertyChangedWithValue({name},"{name}")' not in src
        else:
            assert f'OnPropertyChangedWithValue({name},"{name}")' in src
    for method in (f"ExecuteSlot{i}Increase",f"ExecuteSlot{i}Decrease",
                   f"ExecuteSlot{i}Exact"):
        assert f"public void {method}()" in src

for kind in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    x=ET.parse(gui/(kind+".xml")).getroot()
    assert not list(x.iter("ListPanel")),kind
    assert not list(x.iter("ItemTemplate")),kind
    assert not list(x.iter("ScrollablePanel")),kind
    assert not list(x.iter("Standard.VerticalScrollbar")),kind
    assert '{Rows}' not in (gui/(kind+".xml")).read_text(),kind
    viewport=next(w for w in x.iter("Widget")
        if w.get("Id")=="CUStatsPageViewport")
    assert viewport.get("IsVisible")=="@IsStatsPage"
    assert viewport.get("DoNotPassEventsToChildren")=="false"
    assert viewport.get("ClipContents")=="true"
    ids=set()
    for i in range(1,7):
        row=next(w for w in viewport.iter("Widget")
            if w.get("Id")==f"CUFixedStatRow{i}")
        assert row.get("IsVisible")==f"@Slot{i}Visible"
        assert row.get("PositionYOffset")==str((i-1)*55)
        # v0.58 isolation: no numeric slider in the stats rows. The
        # native engine closed after all six numeric stat bindings were
        # refreshed, so keep the same six rows but use button-only edits.
        assert not list(row.iter("SliderWidget")),kind
        assert any(w.get("Text")=="EDIT WITH - / + OR EXACT"
            for w in row.iter("TextWidget")),kind
        for bound in (f"Slot{i}Value",f"Slot{i}Min",
                      f"Slot{i}Max",f"Slot{i}Increment"):
            assert f"@{bound}" not in ET.tostring(row,encoding="unicode")
        for command in (f"ExecuteSlot{i}Increase",
                        f"ExecuteSlot{i}Decrease",
                        f"ExecuteSlot{i}Exact"):
            assert any(w.get("Command.Click")==command
                for w in row.iter("ButtonWidget")),command
        assert any(w.get("Text")==f"@Slot{i}Name"
            for w in row.iter("TextWidget"))
        ids.add(row.get("Id"))
    assert len(ids)==6
    age=[w for w in x.iter("SliderWidget")
        if w.get("Id")=="CUPlayerAgeSlider"]
    assert len(age)==(0 if kind.endswith("Safe") else 1)
    assert len(list(x.iter("SliderWidget")))==len(age)
    assert any(w.get("Command.Click")=="ExecuteContinue"
        for w in x.iter("ButtonWidget"))
print("PASS: six fixed stat rows use +/- and exact entry, Age slider retained, no stat-slider binding (STATIC ONLY)")
