from pathlib import Path
from save_reader import inspect_save
import os,json
p=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab/ControlCampaigns/D002-R-start"
a,aid=inspect_save(p/"labd002rstart.sav")
b,bid=inspect_save(p/"labd002rrecruit.sav")
assert aid==bid and a["module_versions"]==b["module_versions"] and a["modules"]==b["modules"]
print("CAMPAIGN_AND_PROFILE_MATCH")
print(json.dumps(b))
print("DELTA",json.dumps({k:b[k]-a[k] for k in ("healthy","wounded","gold","food")}))