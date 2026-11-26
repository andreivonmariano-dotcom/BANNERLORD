import json,os
from pathlib import Path
from SaveNamespaceGate import validate,live_gate
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
archive=r/"ControlCampaigns/D002-R-activation"
o={x["name"]:x["sha256"] for x in json.loads((archive/"save-manifest.private.json").read_text(encoding="utf-8-sig"))}
a=Path(os.environ["CU80_DOC"])/"Game Saves"
lab={"labd002rrecruit.sav":"a9535052bf487f6106d2223ca3c42d6215b91e97d81786ef60b32dff3a1ae1a0","labd002rstart.sav":"d740bcd873e9e91a66dd693dd0f1059319a5a70cb2cf05227168e497461d35ea","labd002rthree.sav":"e704f14fb4aa0b75e871e3b4bc8f99585405c9cd78196d28a3900326ffbfe652"}
assert len(o)==43
for name,fn in [("FILESYSTEM_ONLY",lambda:validate(archive/"Game Saves",a,o,lab)),("RUNTIME_GATE",lambda:live_gate(archive/"Game Saves",a,o,lab,True,True,False,False))]:
    try:fn()
    except ValueError as e:print(name,"BLOCKED",str(e))
    else:raise AssertionError("unexpected gate pass")