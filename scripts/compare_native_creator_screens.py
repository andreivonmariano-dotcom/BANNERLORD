#!/usr/bin/env python3
"""Compare Bannerlord creator screenshot CHROME, excluding scene/content.

Usage:
  python scripts/compare_native_creator_screens.py --native native-review.png \
      --custom cu-age.png --screen age --out BuildResults/ui-age
  python scripts/compare_native_creator_screens.py --self-test

Reference resolution: 1650x927. Pixel coordinates here are approximate
from the USER'S screenshot, not a substitute for installed native XML.
This checks geometry plus edge consistency; fonts, 3D input and actual
native UI state need the real game. Outputs PNG overlays and JSON.
Requires Pillow; no OpenCV or OCR.
"""
import argparse
import json
from pathlib import Path

try:
    from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageStat
except ImportError as e:
    raise SystemExit("Install Pillow: python -m pip install Pillow") from e

SIZE=(1650,927)
# Restrict to the native Review chrome. The hero scene, UI content and
# changing text are intentionally masked out. Rectangles in screen pixels.
CHROME={
    "header-left-edge":(1074,0,1140,91),
    "header-right-edge":(1520,0,1649,91),
    "outer-left-rail":(1074,93,1128,826),
    "outer-right-rail":(1615,93,1649,826),
    "lower-rail":(1074,807,1120,895),
    "footer-left-cap":(1133,834,1161,888),
    "footer-middle-border":(1227,842,1294,878),
    "footer-right-cap":(1593,834,1630,889),
}
# See native screenshot at 1650: left side of the outer stone column.
EXPECTED_NATIVE_LEFT=1075
MAX_LEFT_DELTA=4


def open_image(path):
    im=Image.open(path).convert("RGB")
    if im.size!=SIZE:
        raise ValueError(f"{path}: expected {SIZE}, got {im.size}. "
                         "Capture both at 1650x927 and the same UI scaling.")
    return im


def edges(im):
    return im.convert("L").filter(ImageFilter.FIND_EDGES)


def median_strength_by_x(img,search_left=840,search_right=1200,
                         y0=175,y1=808):
    grey=img.convert("L")
    # Native frame has one strong continuous vertical transition,
    # not an isolated high-contrast hero/model/scene element.
    results=[]
    for x in range(search_left,search_right):
        a=list(grey.crop((x,y0,x+1,y1)).getdata())
        b=list(grey.crop((x+2,y0,x+3,y1)).getdata())
        # Measure how much of the column has a visible edge, reducing
        # the effect of a single character or isolated menu text.
        large=sum(1 for u,v in zip(a,b) if abs(u-v)>=16)
        total=sum(abs(u-v) for u,v in zip(a,b))
        results.append((large,total,x))
    return sorted(results,reverse=True)[:12]


def edge_similarity(reference,candidate,box):
    er=edges(reference.crop(box))
    ec=edges(candidate.crop(box))
    a=[1 if p>=42 else 0 for p in er.getdata()]
    b=[1 if p>=42 else 0 for p in ec.getdata()]
    hit=sum(1 for p,q in zip(a,b) if p and q)
    denom=sum(a)+sum(b)
    return (2*hit/denom) if denom else 1.0


def compare(native,custom,screen,out,fail_on_mismatch=False):
    reference=open_image(native)
    candidate=open_image(custom)
    dest=Path(out)
    dest.mkdir(parents=True,exist_ok=True)
    overlays=Image.blend(reference,candidate,0.5)
    draw=ImageDraw.Draw(overlays)
    for rect in CHROME.values():
        draw.rectangle(rect,outline=(246,210,119),width=2)
    overlays.save(dest/"native_custom_chrome_overlay.png")

    diff=ImageChops.difference(edges(reference),edges(candidate))
    diff.save(dest/"native_custom_chrome_edge_difference.png")

    vals={k:round(edge_similarity(reference,candidate,b),4)
          for k,b in CHROME.items()}
    native_candidates=median_strength_by_x(reference)
    custom_candidates=median_strength_by_x(candidate)
    native_x=native_candidates[0][2]
    custom_x=custom_candidates[0][2]
    delta=custom_x-native_x
    report={
        "screen":screen,"resolution":list(SIZE),
        "reference":str(native),"custom":str(custom),
        "reference_left_estimate_x":native_x,
        "custom_left_estimate_x":custom_x,
        "left_delta_estimate_px":delta,
        "native_left_from_user_screenshot":EXPECTED_NATIVE_LEFT,
        "left_delta_limit_px":MAX_LEFT_DELTA,
        "edge_similarity_by_chrome_region":vals,
        "average_chrome_edge_similarity":round(sum(vals.values())/len(vals),4),
        "geometry_gate":abs(delta)<=MAX_LEFT_DELTA,
        "parity_certified":False,
        "notes":[
            "Approximate screenshot-only measurement.",
            "Only the outer native chrome is compared, not changing text or 3D scene.",
            "Confirm native brush IDs and control behavior in the installed game.",
            "A match here is not 1:1 proof without native prefab and live input tests."
        ]
    }
    (dest/"native_chrome_comparison.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))
    if fail_on_mismatch and not report["geometry_gate"]:
        return 2
    return 0


def self_test():
    from tempfile import TemporaryDirectory
    # Make a synthetic right panel and a version shifted 24 pixels left.
    # This test covers the CLI + graphics output, not the native estimate
    # precision on actual Bannerlord art.
    with TemporaryDirectory() as td:
        d=Path(td)
        base=Image.new("RGB",SIZE,(34,22,15))
        for name,offset in [("native",0),("same",0),("shifted",-24)]:
            im=base.copy()
            g=ImageDraw.Draw(im)
            x=1075+offset
            g.rectangle((x,40,1640,895),fill=(90,75,53))
            g.rectangle((x+38,150,1605,823),fill=(21,16,11))
            g.line((x,40,x,895),fill=(225,198,140),width=4)
            im.save(d/f"{name}.png")
        assert compare(d/"native.png",d/"same.png","age",d/"equal")==0
        same=json.loads((d/"equal/native_chrome_comparison.json").read_text())
        assert same["geometry_gate"],same
        assert compare(d/"native.png",d/"shifted.png","age",d/"shift")==0
        changed=json.loads((d/"shift/native_chrome_comparison.json").read_text())
        assert not changed["geometry_gate"],changed
        print("PASS: 1650x927 screenshot chrome comparison detects misaligned panel")
    return 0


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native",type=Path)
    parser.add_argument("--custom",type=Path)
    parser.add_argument("--screen",choices=("world","age","stats","house"),default="age")
    parser.add_argument("--out",default="BuildResults/native-chrome-comparison")
    parser.add_argument("--strict",action="store_true")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args()
    if args.self_test:return self_test()
    if not args.native or not args.custom:
        parser.error("--native and --custom are required without --self-test")
    return compare(args.native,args.custom,args.screen,args.out,args.strict)

if __name__=="__main__":raise SystemExit(main())
