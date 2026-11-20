#!/usr/bin/env python3
"""Probe EXACT 1.5.7.125802 native Review/Family GUI metadata from BUTR.

The GUI.v3 NuGet package is a *derived JSON index* of TaleWorlds' game UI,
not native game DLLs or an independently licensed new art pack.
Never embed original native proprietary textures in our mod.

The probe does not modify the game and is safe as a CI diagnostic. The
game-local extractor remains the authoritative source for actual XML.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import urllib.error
import urllib.request
import zipfile

VER="1.5.7.125802"
PKG="bannerlord.referenceassemblies.gui.v3"
URL=f"https://api.nuget.org/v3-flatcontainer/{PKG}/{VER}/{PKG}.{VER}.nupkg"

def scan_node(n,path="",depth=0):
    if not isinstance(n,dict):return
    tag=n.get("n") or n.get("name") or ""
    a=n.get("a") or n.get("attributes") or {}
    kids=n.get("c") or n.get("children") or []
    cur=f"{path}/{tag}" if path else str(tag)
    meaningful={k:v for k,v in a.items()
                if k in ("Brush","Sprite","WidthSizePolicy","HeightSizePolicy",
                         "SuggestedWidth","SuggestedHeight","MarginLeft",
                         "MarginRight","MarginTop","MarginBottom",
                         "HorizontalAlignment","VerticalAlignment",
                         "Text","Command.Click","PositionXOffset",
                         "PositionYOffset","IsVisible") }
    if tag in ("Widget","TextWidget","ButtonWidget","ScrollablePanel","ListPanel","SliderWidget"):
        if depth<=7:yield {"depth":depth,"tag":tag,"attrs":meaningful}
    for k in kids:
        yield from scan_node(k,cur,depth+1)

def main():
    out=Path("BuildResults/native-review-reference-v157.json")
    out.parent.mkdir(exist_ok=True,parents=True)
    report={"game_version":VER,"pkg":PKG,"url":URL,"status":"UNAVAILABLE"}
    try:
        req=urllib.request.Request(URL,headers={
            "User-Agent":"CalradiaUnified-NativeParitySourceAudit/0.59f"})
        with urllib.request.urlopen(req,timeout=24) as response:
            blob=response.read()
        with zipfile.ZipFile(io.BytesIO(blob)) as z:
            names=z.namelist()
            candidates=[n for n in names
                if n.lower().endswith(".json") and
                ("charactercreationreviewstage" in n.lower() or
                 "charactercreationgenericstage" in n.lower() or
                 "charactercreationculturestage" in n.lower() or
                 "charactercreationfamily" in n.lower())]
            report["package_sha256"]=hashlib.sha256(blob).hexdigest()
            report["candidate_paths"]=candidates
            report["status"]="FOUND" if candidates else "PACKAGE_NO_REVIEW"
            details={}
            for name in candidates[:16]:
                data=json.loads(z.read(name).decode("utf-8"))
                print("NATIVE SHAPE",name,type(data).__name__,
                      list(data)[:30] if isinstance(data,dict) else "",
                      json.dumps(data,ensure_ascii=False)[:2400])
                nodes=(data if isinstance(data,list) else [data])
                rows=[]
                for node in nodes:
                    rows.extend(scan_node(node))
                details[name]={"rows":rows[:260],"row_count":len(rows)}
            report["prefab_structure"]=details
            for name in ("gui/brushes.json","gui/sprites.json","gui/prefabs.json"):
                if name in names:
                    item=json.loads(z.read(name).decode("utf-8"))
                    report[name]={"available":True,
                        "top_level_type":type(item).__name__,
                        "top_keys":list(item)[:15] if isinstance(item,dict) else [],
                        "records":len(item)}
    except Exception as e:
        report["status"]="FETCH_ERROR"
        report["error"]=repr(e)
    out.write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("NATIVE REVIEW REFERENCE AUDIT STATUS",report["status"])
    print("NATIVE REVIEW CANDIDATES",report.get("candidate_paths",[]))
    for name,detail in report.get("prefab_structure",{}).items():
        print("NATIVE PREFAB",name)
        for row in detail["rows"][:60]:
            print("NATIVE NODE",json.dumps(row,ensure_ascii=False))
    if "error" in report:print("NATIVE PROBE ERROR",report["error"])
    print("REPORT",out)
    # Do not fail the mod build solely because the NuGet service is down.
    return 0

if __name__=="__main__":raise SystemExit(main())
