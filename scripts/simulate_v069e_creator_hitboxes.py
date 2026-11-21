#!/usr/bin/env python3
"""Simulate visibility and logical hit boxes in CURRENT generated creator XML.

Not a Bannerlord renderer. It cannot validate text glyphs, UI scaling,
native scene animation, mouse focus, hover, controller input or saved games.
The generated prefabs must first come from the pinned native Review shell.
"""
from __future__ import annotations

import json
from itertools import combinations
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
PREFABS=ROOT/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"
OUT=ROOT/"BuildResults"/"v069e_creator_hitbox_simulation.json"
SIZES=[(1650,927),(1280,720),(1920,1080)]
GUIDED="CUGuidedCreatorNativeReview.xml"
STATS="CUNativeStatsEditorNativeReview.xml"
NATIVE_COMMANDS={"ExecuteBack","ExecuteContinue"}

GUIDED_COMMANDS={
    "ExecutePreviousChoice","ExecuteNextChoice","ExecuteToggle",
    "ExecuteDecrease","ExecuteIncrease","ExecuteExact",
    "ExecuteEditText","ExecuteNativeDefault","ExecuteAdvanced"
}
AGE_COMMANDS={
    "ExecuteAgeDecrease","ExecuteEnterExactAge","ExecuteAgeIncrease",
    "ExecuteKeepNativeAge","ExecutePreviousBonus","ExecuteNextBonus"
}
STATS_COMMANDS={
    "ExecuteEditPlayer","ExecuteEditSpouse","ExecuteNative",
    "ExecuteBalanced","ExecuteUnrestricted",
    "ExecutePreviousGroup","ExecuteNextGroup"
}
STATS_COMMANDS.update(
    "ExecuteSlot%d%s"%(i,k)
    for i in range(1,7)
    for k in ("Decrease","Increase","Exact"))

def number(node,key,default=0):
    return int(node.get(key,str(default)))

def visible(node,flags):
    v=node.get("IsVisible","true")
    return flags.get(v[1:],False) if v.startswith("@") else v.lower()!="false"

def walk(node,flags,origin=(0,0),depth=0):
    # The descendants inherit their container's x/y. Native footer has
    # alignment and layout policies, so it is deliberately not modeled.
    if not visible(node,flags):
        return
    x=origin[0]+number(node,"PositionXOffset")
    y=origin[1]+number(node,"PositionYOffset")
    cmd=node.get("Command.Click","") if node.tag=="ButtonWidget" else ""
    if cmd:
        width=number(node,"SuggestedWidth",-1)
        height=number(node,"SuggestedHeight",-1)
        yield (cmd,x,y,width,height)
    for c in node:
        yield from walk(c,flags,(x,y),depth+1)

def intersection(a,b):
    _,ax,ay,aw,ah=a
    _,bx,by,bw,bh=b
    return max(0,min(ax+aw,bx+bw)-max(ax,bx))*max(
        0,min(ay+ah,by+bh)-max(ay,by))

def examine(name,flags,expected,viewport):
    tree=ET.parse(PREFABS/name).getroot()
    shell=[w for w in tree.iter("Widget") if w.get("Id")=="RightPanel"]
    assert len(shell)==1,(name,"missing native shell")
    p=shell[0]
    assert p.get("Sprite")==r"General\CharacterCreation\character_creation_panel"
    assert p.get("SuggestedWidth")=="694" and p.get("SuggestedHeight")=="1080"
    inners=[w for w in p.iter("Widget") if w.get("Id")=="InnerPanel"]
    assert len(inners)==1,(name,"missing native inner panel")
    inner=inners[0]
    assert (number(inner,"SuggestedWidth"),number(inner,"SuggestedHeight"))==(573,750)
    # Test in InnerPanel-local coordinates. Its own 23x37 offset is
    # relative to the outer native shell and must not be counted twice.
    all_buttons=[button for child in inner
        for button in walk(child,flags)]
    body=[r for r in all_buttons if r[0] in GUIDED_COMMANDS|AGE_COMMANDS|STATS_COMMANDS]
    names=[r[0] for r in body]
    assert set(names)==set(expected),(name,flags,sorted(set(names)^set(expected)))
    assert len(names)==len(set(names)),(name,"duplicate active command",names)
    for cmd,x,y,w,h in body:
        assert w>=50 and h>=40,(name,flags,cmd,"small hitbox",w,h)
        assert x>=0 and y>=0 and x+w<=573 and y+h<=750,(
            name,flags,cmd,"outside native content",x,y,w,h)
    collisions=[(a[0],b[0],intersection(a,b))
        for a,b in combinations(body,2) if intersection(a,b)>0]
    assert not collisions,(name,flags,"overlapping modeled hit targets",
                            collisions[:12])
    # Native 694x1080 panel is engine-scaled. An unscaled overflow is a
    # live-QA risk, not a simulated game FAIL.
    width,height=viewport
    return {
        "movie":name,"viewport":list(viewport),
        "activeButtons":len(body),"testedCommands":sorted(expected),
        "modeledOverlapPairs":len(collisions),
        "modeledContentBounds":[573,750],
        "nativePanelLogicalBounds":[694,1080],
        "unscaledPanelHeightExceedsScreen":1080>height,
        "nativeScaleAndTextMetrics":"NOT EXECUTED",
        "mouseInput":"NOT EXECUTED",
        "imagePixels":"NOT RENDERED"
    }

def main():
    cases=[]
    guided_states=[
        ("choice",{"IsChoice":True},
            {"ExecutePreviousChoice","ExecuteNextChoice","ExecuteAdvanced"}),
        ("boolean",{"IsBoolean":True},
            {"ExecuteToggle","ExecuteAdvanced"}),
        ("number",{"IsNumber":True},
            {"ExecuteDecrease","ExecuteIncrease","ExecuteExact",
             "ExecuteAdvanced"}),
        ("number-with-native-default",
            {"IsNumber":True,"IsNativeDefaultAvailable":True},
            {"ExecuteDecrease","ExecuteIncrease","ExecuteExact",
             "ExecuteNativeDefault","ExecuteAdvanced"}),
        ("text",{"IsText":True},
            {"ExecuteEditText","ExecuteAdvanced"})
    ]
    stats_states=[
        ("age",{"IsAgePage":True},AGE_COMMANDS),
        ("all-stats-six",dict({"IsStatsPage":True},
            **{"Slot%dVisible"%n:True for n in range(1,7)}),
            STATS_COMMANDS),
        ("all-stats-partial",dict({"IsStatsPage":True},
            **{"Slot%dVisible"%n:n<=3 for n in range(1,7)}),
            STATS_COMMANDS-{f"ExecuteSlot{n}{k}"
                for n in (4,5,6) for k in ("Decrease","Increase","Exact")})
    ]
    for viewport in SIZES:
        for label,flags,expected in guided_states:
            item=examine(GUIDED,flags,expected,viewport)
            item["state"]=label
            cases.append(item)
        for label,flags,expected in stats_states:
            item=examine(STATS,flags,expected,viewport)
            item["state"]=label
            cases.append(item)

    report={
        "status":"PASS",
        "method":"Static Gauntlet XML visibility and logical hitbox model",
        "nativeShellVersionTarget":"1.5.7.125802",
        "engineExecuted":False,
        "nativeScreensRendered":False,
        "fontClippingProvenAbsent":False,
        "actualMouseOrKeyboardInputExecuted":False,
        "screenshotsComparedToRealGame":False,
        "simulatedViewportCount":len(SIZES),
        "simulatedVisibilityHitboxCases":len(cases),
        "cases":cases,
        "limitations":[
            "Logical fixed-position widget math only, not Gauntlet scaling.",
            "Native Review footer is owned by engine layouts and is not pixel modeled.",
            "Generated XML presence does not prove a specific fallback movie loaded.",
            "Text wrapping, hover, focus, 3D layers and input need real game evidence."
        ]
    }
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("AUDIT UI MODEL PASS | viewports=%d | visibility-hitbox-cases=%d"
          %(len(SIZES),len(cases)))
    print("MODEL ONLY: NOT rendered screenshots, native input, or visual parity.")
    print("Report:",OUT)

if __name__=="__main__":
    main()
