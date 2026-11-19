#!/usr/bin/env python3
"""v053 native family editor and inheritance regression: STATIC ONLY."""
from pathlib import Path
from xml.etree import ElementTree as ET
r=Path(__file__).resolve().parents[1]
s=r/"Source/CalradiaUnified"
ui=r/"Modules/CalradiaUnified/GUI/Prefabs"
def read(n):return (s/n).read_text(encoding="utf-8")
code=read("CUNativeStatsLayer.cs")
dev=read("SandboxFamilyDevelopment.cs")
origin=read("SandboxOriginBehavior.cs")
creator=read("SandboxFamilyCreator.cs")
profile=read("SandboxOriginProfile.cs")
nav=read("CUNativeStatsNavigationPolicy.cs")
assert origin.index("ApplyPlayerProgression(hero,profile);")<origin.index("SandboxFamilyCreator.TryCreate(profile,hero)")
assert origin.count("ApplyPlayerProgression(hero,profile);")==1
for text in ("cu_family_inherited_skill","cu_family_inherited_attribute",
             "SandboxFamilySkillInheritancePolicy",
             "ApplySpouseCustomization(p,h);"):
    assert text in dev,text
assert "child.ModifyHair(-1,0,-1);" in creator
assert "SpouseOverrides" in profile and '"SpouseStat."+pair.Key' in profile
assert "public const int PageCount=2;" in nav
assert "ExecuteEditPlayer()" in code and "ExecuteEditSpouse()" in code
assert "public int SelectedInt" in code
for n in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
          "CUNativeStatsEditorSafe"):
    root=ET.parse(ui/(n+".xml")).getroot()
    panels=list(root.iter("ScrollablePanel"))
    assert len(panels)==1,n
    assert panels[0].get("InnerPanel")==r"CUStatsClip\CUStatsRows"
    assert panels[0].get("ClipRect")=="CUStatsClip"
    assert "CUStatsScrollbar" in panels[0].get("VerticalScrollbar","")
    rows=next(w for w in root.iter("ListPanel") if w.get("DataSource")=="{Rows}")
    slider=list(rows.iter("SliderWidget"))
    assert len(slider)==1
    assert slider[0].get("ValueInt")=="@SelectedInt"
    assert slider[0].get("MinValueInt")=="@Minimum"
    assert slider[0].get("MaxValueInt")=="@Maximum"
    assert len(list(root.iter("SliderWidget")))==2
    actions={b.get("Command.Click") for b in root.iter("ButtonWidget")}
    assert {"ExecuteEditSpouse","ExecuteEditPlayer","ExecuteEditExact","ExecuteContinue"}<=actions
print("PASS: v053 all-stats native scrolling, spouse sliders, parent inheritance and young face hooks (STATIC ONLY)")
