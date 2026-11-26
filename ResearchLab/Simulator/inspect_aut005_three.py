import os,json
from pathlib import Path
from save_reader import inspect_save
from world_projection import project
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
p=r/"Simulator/ValidationRuns/AUT005/labd002rthree.sav"
m,cid=inspect_save(p)
a,aid=inspect_save(r/"ControlCampaigns/D002-R-start/labd002rstart.sav")
assert cid==aid
assert m["sha256"]=="e704f14fb4aa0b75e871e3b4bc8f99585405c9cd78196d28a3900326ffbfe652"
assert (m["healthy"],m["wounded"],m["gold"],m["food"])==(3,0,960,.95)
assert m["modules"]==a["modules"] and m["module_versions"]["CUNativeObservation"]=="v0.0.3.2"
w=project(p,m["sha256"],m)
v=[x for x in w["nodes"]["parties"].values() if x["id"]=="player_party"]
assert len(v)==1
print("META",json.dumps(m))
print("PLAYER_PARTY",json.dumps(v[0]))
print("CAMPAIGN_MATCH")