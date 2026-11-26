import hashlib,json,os,sys
from pathlib import Path
from save_reader import inspect_save
from partial_native_rules import base_food_change,f32
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
o=r/"Simulator/ValidationRuns/AUT003"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
p=r/"ControlCampaigns/D002-R-start/labd002rrecruit.sav"
meta,_=inspect_save(p)
assert meta["sha256"]=="a9535052bf487f6106d2223ca3c42d6215b91e97d81786ef60b32dff3a1ae1a0"
assert (meta["healthy"],meta["wounded"],meta["food"],meta["gold"])==(2,0,.95,980)
b=o/"recruit.json"
assert sha(b)=="708e3dc16b4cca2e2e068cfbcd6c062556eaa9831f88a4a5bb15350bf11f4d8e"
w=json.loads(b.read_text())
assert w["clocks"][0]["ticks"]==78691175652889
logs=list((r/"D002-observations").glob("session-*.log"))
h="f9ea05c8c048ecc58ef57c2654317692a71a9a1428b0e89a229392711a59d6be"
assert sum(sha(p)==h for p in logs)==1
m={"experiment":"AUT003-FOOD-001","scope":"conditional isolated base food hypothesis, not full campaign forecast","source_save_sha":meta["sha256"],"source_projection_sha":sha(b),"prior_log_sha":h,"prior_seq":17,"baseline_tick":w["clocks"][0]["ticks"],"conditions":{"healthy":2,"wounded":0,"prisoners":0,"sea":False,"food_before":.95,"no_roster_or_inventory_change":True,"no_food_perk_modifier_assumed":True},"endpoint":"first subsequent food_event in same observer session; if absent or conditions fail, not scored","predicted_delta":base_food_change(2,0),"predicted_food":f32(f32(.95)+base_food_change(2,0)),"tolerance":.00001}
q=o/"food_forecast.json"
with q.open("x",encoding="utf8") as f:json.dump(m,f,sort_keys=True,indent=2)
print(json.dumps(m));print("FORECAST_SHA",sha(q))