#!/usr/bin/env python3
"""v0.59c: static Gauntlet UI geometry audit at the user's 1650x927 view.

This is NOT a rendered Bannerlord screenshot or a pointer hit test.
Gauntlet scaling and font metrics require a real game session to confirm.
"""
import json
from pathlib import Path
from xml.etree import ElementTree as ET

root=Path(__file__).resolve().parents[1]
ui=root/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"
W,H=1650,927

def read(name):
    return ET.parse(ui/(name+".xml")).getroot()

def one(nodes, predicate, label):
    x=[w for w in nodes if predicate(w)]
    assert len(x)==1,(label,len(x))
    return x[0]

def num(node,attribute):
    return int(node.get(attribute))

def box(node):
    return (num(node,"PositionXOffset"),num(node,"PositionYOffset"),
            num(node,"SuggestedWidth"),num(node,"SuggestedHeight"))

report={"viewport":[W,H],"scope":"static Gauntlet geometry, not in-game render",
        "screen_layouts":{}}
for flavor in ("","Art"):
    guided=read("CUGuidedCreator"+("V059Art" if flavor else ""))
    panel=one(guided.iter("Widget"),
        lambda w:w.get("HorizontalAlignment")=="Right" and
            w.get("SuggestedWidth")=="604","guided right panel")
    guided_left=W-12-num(panel,"SuggestedWidth")
    assert guided_left>=1020 and guided_left+604<=W
    if flavor:
        scene=one(guided.iter("Widget"),
            lambda w:w.get("Sprite")=="cu_world_illustration",
            "optional World illustration")
        assert num(scene,"SuggestedWidth")==990
        assert 990+24<guided_left,"World illustration overlays decision panel"
    for command in ("ExecutePreviousChoice","ExecuteNextChoice",
                    "ExecuteDecrease","ExecuteIncrease","ExecuteExact",
                    "ExecuteEditText","ExecuteToggle"):
        btn=one(guided.iter("ButtonWidget"),
            lambda w:w.get("Command.Click")==command,command)
        x,y,w,h=box(btn)
        assert x>=0 and x+w<604 and y+h<700,(flavor,command,box(btn))
    report["screen_layouts"]["World/House"+flavor]={
        "panel_left":guided_left,"panel_width":604,
        "navigation":"inside right panel","scene_gap":guided_left-990}

    stats=read("CUNativeStatsEditorV059"+flavor)
    pane=one(stats.iter("Widget"),
        lambda w:w.get("SuggestedWidth")=="850" and
            w.get("HorizontalAlignment")=="Right","Age/Stats pane")
    pane_height=H-int(pane.get("MarginTop"))-int(pane.get("MarginBottom"))
    footer=one(stats.iter("Widget"),lambda w:
        w.get("SuggestedHeight")=="69" and
        w.get("VerticalAlignment")=="Bottom","footer")
    footer_top=pane_height-int(footer.get("PositionYOffset").lstrip("-"))-69
    assert footer_top==826
    # In both Age and Stats the live stage remains underneath the overlay.
    assert W-10-num(pane,"SuggestedWidth")==790

    group=one(stats.iter("Widget"),lambda w:
        w.get("IsVisible")=="@IsStatsPage" and
        w.get("SuggestedHeight")=="679","Stats group")
    group_y=num(group,"PositionYOffset")
    viewport=one(stats.iter("Widget"),
        lambda w:w.get("Id")=="CUStatsPageViewport","viewport")
    _,view_y,view_w,view_h=box(viewport)
    assert (view_y,view_w,view_h)==(225,812,408)
    rows=[one(stats.iter("Widget"),
        lambda w:w.get("Id")==f"CUFixedStatRow{i}",f"row {i}")
        for i in range(1,7)]
    last_row_bottom=group_y+view_y+num(rows[-1],"PositionYOffset")+num(rows[-1],"SuggestedHeight")
    controls=[]
    for command in ("ExecutePreviousGroup","ExecuteNextGroup"):
        btn=one(stats.iter("ButtonWidget"),lambda w:
            w.get("Command.Click")==command,command)
        controls.append(group_y+num(btn,"PositionYOffset"))
        assert num(btn,"PositionYOffset")==638
    page_buttons_top=min(controls)
    page_buttons_bottom=max(controls)+48
    assert last_row_bottom<=page_buttons_top-6,(flavor,"table / pager collision")
    assert page_buttons_bottom<=footer_top-8,(flavor,"pager / footer collision")
    assert (last_row_bottom,page_buttons_top,page_buttons_bottom,footer_top)==(761,768,816,826)

    age=one(stats.iter("SliderWidget"),lambda w:w.get("Id")=="CUPlayerAgeSlider","live Age slider")
    assert age.get("MinValueInt")=="18" and age.get("MaxValueInt")=="80"
    assert age.get("ValueInt")=="@PlayerAge"
    assert len(list(stats.iter("SliderWidget")))==1
    assert not list(stats.iter("ScrollablePanel"))
    assert not list(stats.iter("ListPanel"))
    # Native 1-year age slider remains inside Age content rather than
    # shadowing the footer. Last-sibling hit target stays in the art variant.
    age_group=one(stats.iter("Widget"),lambda w:
        w.get("IsVisible")=="@IsAgePage" and
        w.get("SuggestedHeight")=="673" and
        any(x.get("Id")=="CUPlayerAgeSlider" for x in w.iter()),
        "Age hit target layer")
    age_slider_bottom=num(age_group,"PositionYOffset")+num(age,"PositionYOffset")+num(age,"SuggestedHeight")
    assert age_slider_bottom<footer_top-30
    report["screen_layouts"]["Age/All Stats"+flavor]={
        "panel_left":790,"panel_width":850,
        "stat_rows":6,"stat_bottom":last_row_bottom,
        "page_buttons_top":page_buttons_top,
        "page_buttons_bottom":page_buttons_bottom,
        "footer_top":footer_top,
        "age_hit_slider_bottom":age_slider_bottom,
        "pager_footer_clearance":footer_top-page_buttons_bottom}

print("PASS: v0.59c World, House, Age and All Stats safe geometry at 1650x927 (STATIC ONLY)")
print(json.dumps(report,indent=2))
