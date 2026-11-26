"""Predeclared component hypotheses only; never treat these as full campaign outcomes."""
from pathlib import Path
import os,hashlib,json,xml.etree.ElementTree as ET
from native_rules import market_change,troop_wage
from partial_native_rules import recruitment_base,base_food_change
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def build():
    root=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
    source=Path(r"D:\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBoxCore\ModuleData\spnpccharacters.xml")
    assert sha(source)=="0b83105df4ba877a8dfc403b0d5113356227640f2a040b16e14a49789f1bbfae"
    nodes=[n for n in ET.parse(source).getroot().iter("NPCCharacter") if n.get("id")=="imperial_infantryman"]
    assert len(nodes)==1 and nodes[0].get("level")=="11" and nodes[0].get("occupation")=="Soldier"
    old=root/"ControlCampaigns/D002-R-start/labd002rrecruit.sav"
    assert sha(old)=="a9535052bf487f6106d2223ca3c42d6215b91e97d81786ef60b32dff3a1ae1a0"
    cases=[
        {"id":"AUT004-R01","kind":"recruitment_base","inputs":{"troop":"imperial_infantryman","level":11,"occupation":"Soldier"},"expected_component":recruitment_base(11),"required":["new recruitable volunteer identity","effective runtime model chain","horse surcharge and perk context","quote before transaction","post-roster and gold receipt"],"status":"NOT_RUN"},
        {"id":"AUT004-W01","kind":"character_base_wage","inputs":{"tier":2,"mercenary":False},"expected_component":troop_wage(2,False),"required":["actual native tier and occupation","native per-character wage observation","separate final party wage and daily gold flow"],"status":"NOT_RUN"},
        {"id":"AUT004-F01","kind":"base_food_delta","inputs":{"members":3,"prisoners":0,"sea":False},"expected_component":base_food_change(3,0),"required":["new stable baseline and complete roster","prisoner count","perk and model-chain context","first subsequent food event","no other inventory actions"],"status":"NOT_RUN"},
        {"id":"AUT004-M01","kind":"isolated_market_correction","inputs":{"prosperity":4900,"market_gold":34302},"expected_component":market_change(4900,34302),"required":["new pre-tick state exactly matching inputs or preregister new case","actual active economy model","isolated correction receipt","separate other market flows"],"status":"NOT_RUN"}
    ]
    return {"scope":"conditional native-component hypotheses; no outcome input or scored native prediction","source_definition_sha":sha(source),"prior_checkpoint_sha":sha(old),"cases":cases}
if __name__=="__main__":
    o=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab/Simulator/ValidationRuns/AUT004/next_cases.json"
    result=build()
    assert [x["expected_component"] for x in result["cases"]]==[50,3,base_food_change(3,0),8624]
    with o.open("x",encoding="utf8") as f:json.dump(result,f,sort_keys=True,indent=2)
    print(json.dumps(result));print("MANIFEST_SHA",sha(o))
