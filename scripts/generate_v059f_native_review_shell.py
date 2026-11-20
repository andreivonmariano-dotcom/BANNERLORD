#!/usr/bin/env python3
"""Generate TWO CU creator movies from ONE exact native Review shell.

No copyrighted textures are copied into the module. The shell references
native assets and native brush IDs already installed with the user's game.
Native panel, title region, inner-container and Back/green-Next footer are
read from the v1.5.7.125802 GUI.v3 package, never guessed from screenshots.

The only custom subtrees are the guided question UI and Age/All Stats UI.
The source generator and resulting prefabs are safe to run in CI. Runtime
Gauntlet rendering, screen scaling and mouse capture still need game QA.
"""
import copy
import hashlib
import io
import json
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path
from zipfile import ZipFile

VERSION="1.5.7.125802"
PKG="bannerlord.referenceassemblies.gui.v3"
URL=f"https://api.nuget.org/v3-flatcontainer/{PKG}/{VERSION}/{PKG}.{VERSION}.nupkg"
PATH="gui/Native/GUI/Prefabs/CharacterCreation/CharacterCreationReviewStage.json"
HERE=Path(__file__).resolve().parents[1]
OUT=HERE/"Modules/CalradiaUnified/GUI/Prefabs"
BUILD=HERE/"BuildResults"

def source():
    cached=BUILD/"native_gui_v157.nupkg"
    if cached.exists():
        data=cached.read_bytes()
    else:
        request=urllib.request.Request(URL,headers={"User-Agent":"CU-Gauntlet-Native-Shell-Builder"})
        with urllib.request.urlopen(request,timeout=40) as response:
            data=response.read()
        BUILD.mkdir(parents=True,exist_ok=True)
        cached.write_bytes(data)
    with ZipFile(io.BytesIO(data)) as z:
        tree=json.loads(z.read(PATH).decode("utf-8"))["root"]
    return tree,hashlib.sha256(data).hexdigest()

def find(root,predicate):
    if predicate(root):return root
    for node in root.get("c",[]):
        found=find(node,predicate)
        if found:return found
    return None

def descendants(node):
    yield node
    for n in node.get("c",[]):
        yield from descendants(n)

def to_xml(obj):
    element=ET.Element(obj["n"],{str(k):str(v) for k,v in obj.get("a",{}).items()})
    for child in obj.get("c",[]):
        element.append(to_xml(child))
    return element

def xml(tag,**attrs):
    return ET.Element(tag,{k.replace("_DOT_", "."):str(v)
         for k,v in attrs.items() if v is not None})

def child(parent,tag,**attrs):
    e=xml(tag,**attrs)
    parent.append(e)
    return e

def fixed(parent,kind,x,y,w,h,**extra):
    return child(parent,kind,WidthSizePolicy="Fixed",
        HeightSizePolicy="Fixed",SuggestedWidth=str(w),
        SuggestedHeight=str(h),PositionXOffset=str(x),
        PositionYOffset=str(y),**extra)

def label(parent,text,x,y,w,h,brush="Stage.Description.Text",
          font=None,alignment="Left",visible=None,color=None):
    props={"Text":text,"Brush":brush,
           "Brush.TextHorizontalAlignment":alignment,
           "Brush.TextVerticalAlignment":"Center"}
    if font is not None:props["Brush.FontSize"]=str(font)
    if visible is not None:props["IsVisible"]=visible
    if color is not None:props["Color"]=color
    return fixed(parent,"TextWidget",x,y,w,h,**props)

def action(parent,text,command,x,y,w,h,visible=None,
           brush="ButtonBrush",font=18,selected=None):
    props={"Brush":brush,"Command.Click":command,
           "DoNotPassEventsToChildren":"true",
           "UpdateChildrenStates":"true"}
    if visible:props["IsVisible"]=visible
    if selected:props["IsSelected"]=selected
    button=fixed(parent,"ButtonWidget",x,y,w,h,**props)
    children=child(button,"Children")
    t=child(children,"TextWidget",Text=text,Brush="Popup.Button.Text",
            WidthSizePolicy="StretchToParent",
            HeightSizePolicy="StretchToParent",
            HorizontalAlignment="Center",VerticalAlignment="Center")
    if font:t.set("Brush.FontSize",str(font))
    return button

def static_panel(parent,x,y,w,h,color):
    return fixed(parent,"Widget",x,y,w,h,
       Sprite="BlankWhiteSquare_9",Color=color,
       DoNotAcceptEvents="true")

def guided_content(parent):
    holder=child(parent,"Widget",WidthSizePolicy="StretchToParent",
                 HeightSizePolicy="StretchToParent",
                 DoNotAcceptEvents="true",
                 DoNotPassEventsToChildren="false")
    c=child(holder,"Children")
    label(c,"@StepTitle",16,18,540,65,
          brush="Stage.Title.Text",alignment="Center")
    label(c,"@Hint",22,93,529,115,
          brush="Stage.Description.Text",alignment="Center")
    # Only content varies by step. No independent extra CU chrome.
    static_panel(c,22,245,529,76,"#3F2A1EFF")
    label(c,"@SelectedValue",29,249,514,67,
          brush="Stage.Title.Text",alignment="Center")
    label(c,"@AgeLimitLabel",20,323,530,28,
          font=17,alignment="Center",visible="@IsChildAge")
    action(c,"Previous choice","ExecutePreviousChoice",
           22,372,245,54,visible="@IsChoice")
    action(c,"Next choice","ExecuteNextChoice",
           303,372,245,54,visible="@IsChoice")
    action(c,"Toggle Yes / No","ExecuteToggle",
           110,372,350,54,visible="@IsBoolean")
    action(c,"−","ExecuteDecrease",22,372,166,54,
           visible="@IsNumber",font=23)
    action(c,"+","ExecuteIncrease",198,372,166,54,
           visible="@IsNumber",font=23)
    action(c,"Exact value","ExecuteExact",374,372,174,54,
           visible="@IsNumber")
    action(c,"Enter text","ExecuteEditText",110,372,350,54,
           visible="@IsText")
    action(c,"Use native default","ExecuteNativeDefault",
           115,437,350,48,visible="@IsNativeDefaultAvailable")
    action(c,"@AdvancedLabel","ExecuteAdvanced",115,535,350,50)
    label(c,"@Status",25,610,520,48,
          brush="Stage.Description.Text",font=17)
    label(c,"@StepProgress",25,675,520,29,
          brush="Stage.Description.Text",font=17,alignment="Center")
    return holder

def stat_row(parent,n):
    y=(n-1)*69
    row=fixed(parent,"Widget",10,y,548,67,Id=f"CUFixedStatRow{n}",
              IsVisible=f"@Slot{n}Visible",
              DoNotAcceptEvents="true",
              DoNotPassEventsToChildren="false")
    c=child(row,"Children")
    static_panel(c,0,0,548,66,"#322419FF" if n%2 else "#3A291BFF")
    label(c,f"@Slot{n}Name",10,3,193,27,
          brush="Popup.Button.Text",font=19)
    label(c,f"@Slot{n}Native",10,34,180,25,
          brush="Popup.Button.Text",font=15)
    label(c,f"@Slot{n}Selected",203,10,99,46,
          brush="Popup.Button.Text",font=18)
    action(c,"−",f"ExecuteSlot{n}Decrease",
           311,9,58,47,font=23)
    action(c,"+",f"ExecuteSlot{n}Increase",
           373,9,58,47,font=23)
    action(c,"Exact",f"ExecuteSlot{n}Exact",
           436,9,103,47,font=17)
    return row

def stats_content(parent):
    root=child(parent,"Widget",WidthSizePolicy="StretchToParent",
               HeightSizePolicy="StretchToParent",
               DoNotAcceptEvents="true",
               DoNotPassEventsToChildren="false")
    all_children=child(root,"Children")
    # AGE: a real one-year SliderWidget, last sibling hit target
    # remains paired with the visible age track.
    age=child(all_children,"Widget",IsVisible="@IsAgePage",
              WidthSizePolicy="Fixed",SuggestedWidth="573",
              HeightSizePolicy="Fixed",SuggestedHeight="750",
              DoNotAcceptEvents="true",
              DoNotPassEventsToChildren="false")
    c=child(age,"Children")
    label(c,"@AgeDescription",25,25,520,116,
          brush="Stage.Description.Text",alignment="Center")
    label(c,"18 YEARS",43,150,155,32,
          brush="Stage.Description.Text",font=17)
    label(c,"80 YEARS",377,150,155,32,
          brush="Stage.Description.Text",font=17,alignment="Right")
    action(c,"− 1 year","ExecuteAgeDecrease",
           24,281,164,50,font=18)
    action(c,"Enter exact age","ExecuteEnterExactAge",
           196,281,181,50,font=17)
    action(c,"+ 1 year","ExecuteAgeIncrease",
           384,281,165,50,font=18)
    action(c,"Keep native age","ExecuteKeepNativeAge",
           120,345,333,49,font=17)
    label(c,"@Status",34,415,503,40,
          brush="Stage.Description.Text",font=17,alignment="Center")
    static_panel(c,24,487,525,2,"#99764EFF")
    label(c,"@BonusSummary",34,504,504,122,
          brush="Stage.Description.Text",font=17)
    action(c,"Previous bonuses","ExecutePreviousBonus",
           30,650,237,43,font=16)
    action(c,"Next bonuses","ExecuteNextBonus",
           306,650,237,43,font=16)

    all_stats=child(all_children,"Widget",IsVisible="@IsStatsPage",
              WidthSizePolicy="Fixed",SuggestedWidth="573",
              HeightSizePolicy="Fixed",SuggestedHeight="750",
              DoNotAcceptEvents="true",
              DoNotPassEventsToChildren="false")
    c=child(all_stats,"Children")
    action(c,"Player","ExecuteEditPlayer",8,0,168,45,
           selected="@IsPlayerTarget",font=17)
    action(c,"Spouse","ExecuteEditSpouse",187,0,168,45,
           selected="@IsSpouseTarget",font=17)
    label(c,"@BudgetStatus",365,0,198,44,
          brush="Popup.Button.Text",font=14)
    action(c,"Native","ExecuteNative",10,51,168,43,
           selected="@IsNativeMode",font=16)
    action(c,"Balanced","ExecuteBalanced",191,51,170,43,
           selected="@IsBalancedMode",font=16)
    action(c,"Unrestricted","ExecuteUnrestricted",374,51,186,43,
           selected="@IsUnrestrictedMode",font=16)
    label(c,"@ModeDescription",18,98,525,38,
          brush="Stage.Description.Text",font=15)
    label(c,"@TargetHelp",18,135,525,38,
          brush="Stage.Description.Text",font=15)
    label(c,"@Status",18,174,525,26,
          brush="Popup.Button.Text",font=15)
    label(c,"STAT / BASE",18,202,180,29,font=15)
    label(c,"CURRENT",217,202,100,29,font=15)
    label(c,"ADJUST",365,202,185,29,font=15,alignment="Center")
    viewport=fixed(c,"Widget",2,236,568,412,
        Id="CUStatsPageViewport",DoNotAcceptEvents="true",
        DoNotPassEventsToChildren="false",ClipContents="true")
    content=child(viewport,"Children")
    for n in range(1,7):stat_row(content,n)
    action(c,"Previous stats","ExecutePreviousGroup",
           8,660,168,48,font=16)
    label(c,"@SkillGroup",187,661,194,45,font=17,alignment="Center")
    action(c,"Next stats","ExecuteNextGroup",
           387,660,168,48,font=16)

    # Age INPUT last, as in the verified working v0.58/v0.59e bridge.
    layer=child(all_children,"Widget",IsVisible="@IsAgePage",
        WidthSizePolicy="Fixed",SuggestedWidth="573",
        HeightSizePolicy="Fixed",SuggestedHeight="750",
        DoNotAcceptEvents="true",
        DoNotPassEventsToChildren="false")
    c=child(layer,"Children")
    slider=fixed(c,"SliderWidget",48,197,478,54,
        Id="CUPlayerAgeSlider",ValueInt="@PlayerAge",
        MinValueInt="18",MaxValueInt="80",
        IsDiscrete="true",DiscreteIncrementInterval="1",
        UpdateValueContinuously="true",UpdateValueOnRelease="false",
        AlignmentAxis="Horizontal",DoNotUpdateHandleSize="true",
        SliderArea="CUPlayerAgeTrack",Handle="CUPlayerAgeHandle")
    inside=child(slider,"Children")
    child(inside,"Widget",Id="CUPlayerAgeTrack",
          WidthSizePolicy="StretchToParent",HeightSizePolicy="Fixed",
          SuggestedHeight="13",VerticalAlignment="Center",
          Sprite="BlankWhiteSquare_9",Color="#9D7340FF")
    child(inside,"ButtonWidget",Id="CUPlayerAgeHandle",
          Brush="ButtonBrush",UpdateChildrenStates="true",
          WidthSizePolicy="Fixed",SuggestedWidth="38",
          HeightSizePolicy="Fixed",SuggestedHeight="46",
          VerticalAlignment="Center")
    return root

def build(name,binding,native_shell,constants,header,inner,footer,content):
    prefab=xml("Prefab")
    if constants is not None:prefab.append(to_xml(constants))
    window=child(prefab,"Window")
    stage=child(window,"Widget",
        WidthSizePolicy="StretchToParent",
        HeightSizePolicy="StretchToParent",
        DoNotAcceptEvents="true",
        DoNotPassEventsToChildren="false")
    stage_children=child(stage,"Children")
    pane=to_xml({"n":"Widget","a":native_shell.get("a",{})})
    stage_children.append(pane)
    pane_children=child(pane,"Children")
    hd=to_xml(header)
    hd.set("Text",binding["title"])
    pane_children.append(hd)
    inner_w=to_xml({"n":"Widget","a":inner.get("a",{})})
    pane_children.append(inner_w)
    body=child(inner_w,"Children")
    content(body)
    ft=to_xml(footer)
    for button in ft.iter("ButtonWidget"):
        cmd=button.get("Command.Click")
        if cmd=="OnPreviousStage":
            button.set("Command.Click","ExecuteBack")
        if cmd=="OnNextStage":
            button.set("Command.Click","ExecuteContinue")
    for element in ft.iter():
        if element.get("Text")=="@NextStageText":
            element.set("Text",binding["next"])
        if element.get("IsVisible")=="@CanAdvance":
            # CU's own Continue already validates the exact question.
            del element.attrib["IsVisible"]
        for key,value in list(element.attrib.items()):
            if value.startswith("@") and value not in (
                binding["title"],binding["next"]):
                # Preserve visuals, not unrelated native stage ViewModel
                # bindings absent from the CU ViewModel.
                del element.attrib[key]
    pane_children.append(ft)
    ET.indent(prefab,space="  ")
    dest=OUT/name
    dest.parent.mkdir(parents=True,exist_ok=True)
    data=ET.tostring(prefab,encoding="utf-8",xml_declaration=True)
    dest.write_bytes(data)
    print("GENERATED",dest,"BYTES",len(data),"SHA256",
          hashlib.sha256(data).hexdigest())
    return dest,data

def main():
    tree,digest=source()
    right=find(tree,lambda n:n.get("n")=="Widget" and
        n.get("a",{}).get("Id")=="RightPanel")
    if right is None:raise RuntimeError("Native Review has no RightPanel")
    a=right["a"]
    expected={"SuggestedWidth":"694","SuggestedHeight":"1080",
              "Sprite":r"General\CharacterCreation\character_creation_panel",
              "HorizontalAlignment":"Right","VerticalAlignment":"Center"}
    for k,v in expected.items():
        if a.get(k)!=v:raise RuntimeError(f"Native Review changed: {k}: {a.get(k)}")
    header=find(right,lambda n:n.get("n")=="TextWidget" and
        n.get("a",{}).get("Brush")=="Stage.Title.Text" and
        n.get("a",{}).get("Text")=="@Title")
    inner=find(right,lambda n:n.get("n")=="Widget" and
        n.get("a",{}).get("Id")=="InnerPanel")
    footer=find(right,lambda n:n.get("n")=="ListPanel" and
        {"OnPreviousStage","OnNextStage"}.issubset({
            z.get("a",{}).get("Command.Click") for z in descendants(n)}))
    constants=find(tree,lambda n:n.get("n")=="Constants")
    if not all((header,inner,footer,constants)):
        raise RuntimeError("Native Review frame/header/inner/footer missing")
    assert inner["a"].get("SuggestedWidth")=="573"
    assert inner["a"].get("SuggestedHeight")=="750"
    brushes={n.get("a",{}).get("Brush") for n in descendants(footer)}
    if not {"Standard.BackButton","Popup.Done.Button.NineGrid"}.issubset(brushes):
        raise RuntimeError("Native Review footer brushes changed")

    generated=[]
    generated.append(build("CUGuidedCreatorNativeReview.xml",
       {"title":"@Chapter","next":"@ContinueLabel"},
       right,constants,header,inner,footer,guided_content))
    generated.append(build("CUNativeStatsEditorNativeReview.xml",
       {"title":"@Page","next":"@NextLabel"},
       right,constants,header,inner,footer,stats_content))
    report={"native_version":VERSION,"native_prefab":PATH,
       "nuget_sha256":digest,"shared_shell":{
          "native_panel":a,"native_inner":inner["a"],
          "native_title_brush":header["a"].get("Brush"),
          "native_footer_brushes":sorted(str(x) for x in brushes if x)},
       "generated":[{"file":str(p.relative_to(HERE)),
                     "sha256":hashlib.sha256(b).hexdigest(),
                     "bytes":len(b)} for p,b in generated]}
    BUILD.mkdir(exist_ok=True,parents=True)
    (BUILD/"native-review-shell-v157.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("PASS: two CU screens share exact native Review panel, header and footer")

if __name__=="__main__":
    main()
