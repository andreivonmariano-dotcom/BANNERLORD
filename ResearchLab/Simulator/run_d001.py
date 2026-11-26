"""Run from the standalone Simulator directory using Python 3.14."""
import argparse
import json
from pathlib import Path
from save_reader import inspect_save
from delegation import replay, sample_risk

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--save-dir",type=Path,required=True)
    a=p.parse_args()
    results=[]; ids=[]
    for name in ("LABD001BASE.sav","LABD001ACTIVE.sav","LABD001RETURN.sav"):
        result,campaign_id=inspect_save(a.save_dir/name)
        results.append(result);ids.append(campaign_id)
    if len(set(ids))!=1 or any(r["modules"]!=results[0]["modules"] or r["module_versions"]!=results[0]["module_versions"] for r in results):
        raise ValueError("different campaign or module versions")
    print(json.dumps({"saves":results,"same_campaign":True,
        "replay":replay(*results),"risk_samples":[sample_risk(86),sample_risk(56)]},indent=2))

if __name__=="__main__":
    main()
