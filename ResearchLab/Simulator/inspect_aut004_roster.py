from pathlib import Path
from save_reader import inspect_save
from world_projection import project
import os,json
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
p=r/"ControlCampaigns/D002-R-start/labd002rrecruit.sav"
m,_=inspect_save(p)
assert m["sha256"]=="a9535052bf487f6106d2223ca3c42d6215b91e97d81786ef60b32dff3a1ae1a0"
w=project(p,m["sha256"],m)
n=w["nodes"]
v=[x for x in n["parties"].values() if x["id"]=="player_party"]
assert len(v)==1
print("ROSTER",json.dumps(v[0]["roster"],default=str))
print("PERSONNEL",v[0]["personnel"])