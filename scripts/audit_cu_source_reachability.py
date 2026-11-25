#!/usr/bin/env python3
"""Inventory all CU runtime source/prefabs/settings/listeners/save keys.

Lexical candidates are not dead-code proofs. Reflection, Gauntlet bindings,
generated movies, attributes, callbacks and saved compatibility records can
make otherwise unreferenced identifiers necessary. Fail only on explicit
retirement boundary violations, never delete automatically.
"""
import json
import re
from collections import Counter, defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
module=root/"Modules"/"CalradiaUnified"
project=ET.parse(src/"CalradiaUnified.csproj").getroot()
excluded={n.get("Remove") for n in project.iter("Compile") if n.get("Remove")}
sources={p.name:p.read_text(encoding="utf-8-sig") for p in sorted(src.glob("*.cs"))}
compiled={k:v for k,v in sources.items() if k not in excluded}
# Keep string literals for key/movie/registration inspection; strip comments
# for lexical symbol counts, with strings consumed first to avoid URL issues.
comment=re.compile(r'("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')|//[^\n]*|/\*[\s\S]*?\*/')
def clean(s):
    return comment.sub(lambda m:m.group(1) or " ",s)
texts={k:clean(v) for k,v in compiled.items()}
blob="\n".join(texts.values())
types=defaultdict(list)
for file,s in texts.items():
    for name in re.findall(r'\b(?:class|struct|enum|interface)\s+(\w+)',s):
        types[name].append(file)
tokens=Counter(re.findall(r'\b[A-Za-z_]\w*\b',blob))
prefabs={p.stem:p for p in sorted((module/"GUI"/"Prefabs").glob("*.xml"))}
movies=set(re.findall(r'\bLoadMovie\(\s*"([^"]+)"',blob))
keys=defaultdict(list)
events=[]
settings=read_settings=sources["Settings.cs"]
for name,s in texts.items():
    for key in re.findall(r'\bSyncData\(\s*"([^"]+)"',s):
        keys[key].append(name)
    for event,handler in re.findall(r'CampaignEvents\.(\w+)\.AddNonSerializedListener\(\s*this\s*,\s*(\w+)',s):
        events.append({"file":name,"event":event,"handler":handler})
flags=re.findall(r'public\s+bool\s+(\w+)\s*=\s*(?:true|false)',settings)
numeric=re.findall(r'public\s+int\s+(\w+)\s*=',settings)
held=re.findall(r'case "(Enable\w+)":',settings.split("public static bool IsHeldMutation",1)[1].split("public void ApplyAcceptanceFreeze",1)[0])
prefab_report=[]
for name,p in prefabs.items():
    tree=ET.parse(p).getroot()
    commands=sorted({v for e in tree.iter() for k,v in e.attrib.items() if k.startswith("Command.")})
    refs=[f for f,s in texts.items() if '"'+name+'"' in s]
    prefab_report.append({"name":name,"literal_source_references":refs,
        "commands":commands,"retired":name in ("CUOriginPanel","CUSettingsPanel","CUHouseholdCamp")})
retired={"SandboxOriginPanel.cs","NativeSettingsBehavior.cs","NativeSettingsPanel.cs",
    "SandboxOriginBehavior.PeerSocial.cs","SandboxOriginBehavior.StoryDirector.cs","SandboxOriginBehavior.HouseholdCamp.cs","CUHouseholdCampScreen.cs",
    "CivicBehavior.cs",
    "CivicBehavior.CastleManagement.cs",
    "CivicBehavior.Convoys.cs",
    "CivicBehavior.Feudal.cs",
    "CivicBehavior.Garrisons.cs",
    "CivicBehavior.Kingdom.cs",
    "CivicBehavior.Trade.cs",
    "KingdomAutonomyBehavior.cs"}
assert retired<=excluded, "retired UI or gameplay adapter is compiled"
for symbol in ("SandboxOriginPanel","SandboxOriginPanelVM","NativeSettingsBehavior","NativeSettingsPanel","NativeSettingsPanelVM"):
    assert not re.search(r'\b'+symbol+r'\b',blob), "retired UI referenced by live source: "+symbol
for name in ("CUOriginPanel","CUSettingsPanel","CUHouseholdCamp"):
    assert name not in movies, "retired prefab loaded by live source: "+name
    assert name+".xml" in (root/"scripts"/"Package-Compiled.ps1").read_text(), "retired package exclusion missing"
# Literal registrations are exact; constructed identifiers are reported separately.
registration=re.compile(r'\b(AddGameMenuOption|AddGameMenu|AddPlayerLine|AddDialogLine)\s*\(\s*')
menu_rows=[]
dynamic=[]
groups=defaultdict(list)
for file,body in texts.items():
    for m in registration.finditer(body):
        tail=body[m.end():]
        args=re.match(r'"([^"]+)"\s*(,|\+)',tail)
        line=body.count("\n",0,m.start())+1
        kind=m.group(1)
        if not args or args.group(2)!=",":
            dynamic.append({"file":file,"line":line,"kind":kind})
            continue
        first=args.group(1)
        rest=tail[args.end():].lstrip()
        second=re.match(r'"([^"]+)"\s*,',rest)
        if kind=="AddGameMenuOption" and not second:
            dynamic.append({"file":file,"line":line,"kind":kind,"parent":first})
            continue
        key=("option:"+first+"/"+second.group(1) if kind=="AddGameMenuOption" else
             "menu:"+first if kind=="AddGameMenu" else "dialog:"+first)
        row={"file":file,"line":line,"kind":kind,"key":key}
        menu_rows.append(row)
        groups[key].append(row)
assert not any('args=>false' in texts[n] for n in ("SandboxOriginBehavior.LivingMenus.cs",)), "permanently disabled petition option reintroduced"
assert "cu_origin_preview" not in texts["SandboxOriginBehavior.cs"], "retired origin editor reintroduced"
report={
    "scope":"lexical inventory, not runtime reachability or compatibility proof",
    "source_file_count":len(sources),"compiled_source_file_count":len(compiled),
    "excluded_sources":sorted(excluded),"all_source_files":[{"file":k,"lines":len(v.splitlines()),"excluded":k in excluded} for k,v in sources.items()],
    "type_declarations":dict(types),"single_mention_types":[{"type":k,"files":v} for k,v in types.items() if tokens[k]==1],
    "menu_dialog_literal_registrations":menu_rows,
    "menu_dialog_duplicate_candidates":{k:v for k,v in groups.items() if len(v)>1},
    "menu_dialog_dynamic_registrations":dynamic,
    "prefabs":prefab_report,"literal_movie_loads":sorted(movies),
    "literal_movie_loads_without_current_prefab":sorted(movies-set(prefabs)),
    "sprite_configs":[str(p.relative_to(root)) for p in (module/"GUI"/"SpriteParts").rglob("*.xml")],
    "settings":[{"key":k,"held":k in held,"mentions_in_compiled_source":tokens[k]} for k in flags+numeric],
    "campaign_event_listeners":events,
    "duplicate_event_listener_candidates":[{"event":a,"handler":b,"count":n} for (a,b),n in Counter((e["event"],e["handler"]) for e in events).items() if n>1],
    "literal_save_keys":dict(keys),"duplicate_save_key_candidates":{k:v for k,v in keys.items() if len(v)>1},
    "notes":["Generated native Review movies appear after the build generation step.",
        "No deletion or automatic consolidation from single-mention counts.",
        "Old save fields remain compatible; a low reference count is not removal evidence.",
        "Runtime attribute discovery, reflected calls and native input hooks need live acceptance."]}
out=root/"BuildResults"/"cu-source-reachability-inventory.json"
out.parent.mkdir(exist_ok=True)
out.write_text(json.dumps(report,indent=2),encoding="utf-8")
print("CU lexical inventory:",len(sources),"source files,",len(compiled),"compiled candidates,",len(prefabs),"prefabs,",len(flags+numeric),"settings,",len(events),"literal campaign listeners,",len(keys),"literal save keys")
print("PASS: explicit retired UI boundaries; inventory candidates require manual verification.")
