import hashlib,json,os
from pathlib import Path
from save_reader import inspect_save
from partial_native_rules import base_food_change,f32
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
o=r/"Simulator/ValidationRuns/AUT007"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
p=r/"Simulator/ValidationRuns/AUT005/labd002rthree.sav"
m,_=inspect_save(p)
assert m["sha256"]=="e704f14fb4aa0b75e871e3b4bc8f99585405c9cd78196d28a3900326ffbfe652"
assert (m["healthy"],m["wounded"],m["gold"],m["food"])==(3,0,960,.95)
h="dc62f3d22883a7604e7fc757dd47a928bee9e231f9c20ccfca128979485cc9bb"
logs=[p for p in (r/"D002-observations-v032").glob("session-*.log") if sha(p)==h]
assert len(logs)==1
rows=[dict(t.split("=",1) for t in l.split()) for l in logs[0].read_text().splitlines()]
assert len(rows)==4 and rows[-1]["reason"]=="loaded" and rows[-1]["tick"]=="78690884321455"
assert rows[-1]["healthy"]=="3" and rows[-1]["food"]=="0.95" and rows[-1]["sea"]=="False"
assert rows[2]["food"]=="NavalDLC.GameComponents.NavalDLCMobilePartyFoodConsumptionModel"
frame=r/"DesktopLab/game-aut007-party.png"
assert sha(frame)=="52210866e7e9c9c1f530ce29411f20f4843dad7293e387ebf8b8906c474ff165"
out={"experiment":"AUT007-FOOD-001","registered_case":"AUT004-F01","scope":"conditional prospective base food hypothesis","source_save_sha":m["sha256"],"prior_log_sha":h,"prior_seq":4,"baseline_tick":78690884321455,"prisoner_ui_evidence_sha":sha(frame),"conditions":{"healthy":3,"wounded":0,"prisoners":0,"sea":False,"food_before":.95,"no_roster_or_inventory_change":True,"no_food_perk_modifier_assumed":True},"active_food_model":rows[2]["food"],"endpoint":"first subsequent main-party food_event in same session; no retrospective selection; abort on changed roster or other intervention","predicted_delta":base_food_change(3,0),"predicted_food":f32(f32(.95)+base_food_change(3,0)),"tolerance":.00001,"maximum_supervised_pulses":8,"maximum_play_seconds_per_pulse":10,"limitations":["full active predecessor and complete perk context not verified","initial prisoner count verified by UI; not observed in every callback","event endpoint, not exact fixed horizon"]}
with (o/"food_forecast.json").open("x",encoding="utf8") as f:json.dump(out,f,sort_keys=True,indent=2)
print(json.dumps(out));print("FORECAST_SHA",sha(o/"food_forecast.json"))