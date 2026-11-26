from pathlib import Path
import os
from save_reader import inspect_save
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
m,c=inspect_save(r/"Simulator/ValidationRuns/AUT007/saveauto2.changed.private.sav")
b,bc=inspect_save(r/"Simulator/ValidationRuns/AUT005/labd002rthree.sav")
print("SAME_RESEARCH_CAMPAIGN",c==bc)
print("SAME_MODULE_PROFILE",m["modules"]==b["modules"] and m["module_versions"]==b["module_versions"])
print("STATE",{k:m[k] for k in ("healthy","wounded","gold","food","sha256")})