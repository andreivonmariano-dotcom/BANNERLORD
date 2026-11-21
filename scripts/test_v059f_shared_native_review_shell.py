#!/usr/bin/env python3
"""v0.59f: shared native Review shell, safe CU bindings and real controls."""
import copy
import json
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
dir=root/"Modules/CalradiaUnified/GUI/Prefabs"
names=["CUGuidedCreatorNativeReview.xml","CUNativeStatsEditorNativeReview.xml"]
prefabs=[ET.parse(dir/n).getroot() for n in names]

def exact_one(root,tag,predicate):
    matches=[x for x in root.iter(tag) if predicate(x)]
    assert len(matches)==1,(tag,len(matches))
    return matches[0]

panels=[exact_one(p,"Widget",lambda x:x.get("Id")=="RightPanel") for p in prefabs]
for panel in panels:
    for key,val in {
        "Sprite":r"General\CharacterCreation\character_creation_panel",
        "SuggestedWidth":"694",
        "SuggestedHeight":"1080",
        "HorizontalAlignment":"Right",
        "VerticalAlignment":"Center",
        "DoNotUseCustomScaleAndChildren":"true"
    }.items():
        assert panel.get(key)==val,(key,panel.attrib)
    inner=exact_one(panel,"Widget",lambda x:x.get("Id")=="InnerPanel")
    assert inner.get("SuggestedWidth")=="573"
    assert inner.get("SuggestedHeight")=="750"
    assert inner.get("PositionXOffset")=="23"
    assert inner.get("PositionYOffset")=="37"
    assert any(t.get("Brush")=="Stage.Title.Text" for t in panel.iter("TextWidget"))
    buttons=list(panel.iter("ButtonWidget"))
    for command,brush in (
        ("ExecuteBack","Standard.BackButton"),
        ("ExecuteContinue","Popup.Done.Button.NineGrid")
    ):
        assert len([b for b in buttons
            if b.get("Command.Click")==command and
               b.get("Brush")==brush])==1,(command,brush)
    assert not any(v.startswith("OnPreviousStage") or v.startswith("OnNextStage")
        for b in buttons for v in b.attrib.values())

# To compare shell identity, remove only CU content and substituted label
# text. Everything else is a single shared exact native frame.
def shell_signature(panel):
    p=copy.deepcopy(panel)
    header=exact_one(p,"TextWidget",
        lambda x:x.get("Brush")=="Stage.Title.Text"
        and x.get("Text") in ("@Chapter","@Page"))
    header.set("Text","@SHELL_TITLE")
    inner=exact_one(p,"Widget",lambda x:x.get("Id")=="InnerPanel")
    for child in list(inner):inner.remove(child)
    for t in p.iter("TextWidget"):
        if t.get("Text") in ("@ContinueLabel","@NextLabel"):
            t.set("Text","@SHELL_NEXT")
    return ET.tostring(p,encoding="unicode")
assert shell_signature(panels[0])==shell_signature(panels[1])

guided,stats=prefabs
guided_actions={w.get("Command.Click") for w in guided.iter("ButtonWidget")}
assert {
    "ExecuteBack","ExecuteContinue","ExecuteAdvanced",
    "ExecutePreviousChoice","ExecuteNextChoice","ExecuteToggle",
    "ExecuteDecrease","ExecuteIncrease","ExecuteExact",
    "ExecuteEditText","ExecuteNativeDefault"
}<=guided_actions
assert len(guided_actions)==11
assert any(t.get("Text")=="@SelectedValue" for t in guided.iter("TextWidget"))
assert not any(w.get("Sprite")=="cu_world_illustration" for w in guided.iter("Widget"))
assert not any(w.get("SuggestedWidth")=="704" for w in guided.iter("Widget"))

sliders=list(stats.iter("SliderWidget"))
assert len(sliders)==1
age=sliders[0]
for key,val in {"Id":"CUPlayerAgeSlider","ValueInt":"@PlayerAge",
                "MinValueInt":"18","MaxValueInt":"80",
                "IsDiscrete":"true","Handle":"CUPlayerAgeHandle",
                "SliderArea":"CUPlayerAgeTrack"}.items():
    assert age.get(key)==val
assert any(w.get("Id")=="CUPlayerAgeHandle" for w in age.iter("ButtonWidget"))
assert not list(stats.iter("ListPanel")) or all(
    "ExecutePreviousGroup" not in (x.get("Command.Click") or "")
    for x in stats.iter("ListPanel"))
# Native shell itself contains one footer ListPanel. Stats table does NOT.
assert not list(stats.iter("ScrollablePanel"))
controls=[w.get("Command.Click") for w in stats.iter("ButtonWidget")]
for n in range(1,7):
    row=exact_one(stats,"Widget",lambda w:
        w.get("Id")==f"CUFixedStatRow{n}")
    assert row.get("PositionYOffset")==str((n-1)*69)
    for k in ("Name","Native","Selected"):
        assert any(w.get("Text")==f"@Slot{n}{k}"
            for w in row.iter("TextWidget"))
    for k in ("Decrease","Increase","Exact"):
        assert controls.count(f"ExecuteSlot{n}{k}")==1
for cmd in ("ExecutePreviousGroup","ExecuteNextGroup",
            "ExecuteEditPlayer","ExecuteEditSpouse",
            "ExecuteNative","ExecuteBalanced","ExecuteUnrestricted",
            "ExecuteAgeDecrease","ExecuteAgeIncrease",
            "ExecuteEnterExactAge","ExecuteKeepNativeAge",
            "ExecutePreviousBonus","ExecuteNextBonus"):
    assert controls.count(cmd)==1,cmd

report=root/"BuildResults/native-review-shell-v157.json"
assert report.exists()
meta=json.loads(report.read_text())
assert meta["native_version"]=="1.5.7.125802"
assert len(meta["generated"])==2
print("PASS: Q1/Q2 two generated movies share exact Native Review frame and footer, and preserve 18 safe stat edits + native Age slider")
