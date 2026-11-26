"""Evidence-aware native vs independent simulator comparison. No game writes.
Price grid is an independent actual managed output. D001 replay uses observed
outcomes as inputs and is NOT an independent prediction of those outcomes.
"""
import argparse,csv,hashlib,itertools,json,math
from pathlib import Path
from economy_lab import base_price_factor
from native_rules import f32
from delegation import Campaign,risk
from save_reader import inspect_save

TOLERANCE=2e-5
GRID=set(itertools.product((0,1),(0,1),(0,10,100,1000,10000),(0,1,10,100),(0,1,10,100,1000),(0,1),(15,)))
FIELDS=("animal","trade_good","stock","supply","demand","selling","transfer","native")
SAVE_HASHES=("3b32e8197ebd833827bdb820e974f4a5224cb24b522a1e49f37e20304f2df5cd","6aa00693931a35c5c08b597e9d077e74cd7d0441f32c8a54d1f9630aae3c6437","23f9116522444a3f73eaebecb8fd2bb98b72c58b2891c19bd0c9a0fbda2f312b")
def compare_grid(rows):
    seen=set();details=[];max_error=0;exact=0
    for row in rows:
        if set(row)!=set(FIELDS):raise ValueError("wrong columns")
        key=tuple(int(row[k]) for k in FIELDS[:-1])
        if key not in GRID or key in seen:raise ValueError("unexpected or duplicate grid key")
        seen.add(key)
        a,t,stock,supply,demand,selling,transfer=key
        native=f32(float(row["native"]))
        if not math.isfinite(native):raise ValueError("nonfinite native output")
        predicted=base_price_factor(stock,supply,demand,bool(selling),transfer,bool(a),bool(t))
        if not math.isfinite(predicted):raise ValueError("nonfinite prediction")
        error=abs(native-predicted);max_error=max(max_error,error);exact+=error==0
        details.append({"input":key,"native":native,"simulator":predicted,"absolute_error":error,"status":"MATCH" if error<=TOLERANCE else "MISMATCH"})
    if seen!=GRID:raise ValueError("incomplete grid")
    return {"scope":"actual default managed method vs Python; synthetic category fixtures","cases":len(seen),
        "matches":sum(d["status"]=="MATCH" for d in details),"mismatches":sum(d["status"]=="MISMATCH" for d in details),
        "exact_float32_matches":exact,"maximum_absolute_error":max_error,"absolute_tolerance":TOLERANCE,"details":details}

def metric(name,native,modeled,basis):
    if basis=="unmodeled":status="UNMODELED"
    elif basis=="supplied_outcome":status="INPUT_REPLAY_MATCH" if native==modeled else "INPUT_REPLAY_MISMATCH"
    else:status="MATCH" if native==modeled else "MISMATCH"
    return {"metric":name,"native":native,"simulator":modeled,"basis":basis,"status":status,
        "native_minus_simulator":native-modeled if type(native) in (int,float) and type(modeled) in (int,float) else None}

def compare_d001(saves):
    base,active,returned=saves
    c=Campaign(base["healthy"],base["wounded"],base["gold"])
    c.dispatch(86,10,6)
    rows=[metric("dispatch healthy",active["healthy"],c.healthy,"accounting"),
        metric("dispatch wounded",active["wounded"],c.wounded,"accounting"),
        metric("dispatch gold",active["gold"],c.gold,"accounting"),
        metric("Nerric UI risk percent",17,round(100*risk(86),10),"native_ui_vs_formula"),
        metric("Tholrik UI risk percent",32,round(100*risk(56),10),"native_ui_vs_formula")]
    c.advance(6);c.finish(2,5,550)
    rows.extend([metric("return healthy",returned["healthy"],c.healthy,"supplied_outcome"),
        metric("return wounded",returned["wounded"],c.wounded,"supplied_outcome"),
        metric("return total",returned["healthy"]+returned["wounded"],c.healthy+c.wounded,"supplied_outcome"),
        metric("return gold",returned["gold"],c.gold,"partial_economy"),
        metric("food change",returned["food"]-base["food"],None,"unmodeled"),
        metric("relation change",None,None,"unmodeled"),
        metric("native post-reload duplicate reward audit",None,None,"unmodeled"),
        metric("native casualty prediction",None,None,"unmodeled")])
    return {"scope":"D001 observed-outcome replay; no native RNG or casualty prediction","metrics":rows}

def read_saves(directory):
    records=[];ids=[]
    for name,expected in zip(("LABD001BASE.sav","LABD001ACTIVE.sav","LABD001RETURN.sav"),SAVE_HASHES):
        r,id=inspect_save(directory/name)
        if r["sha256"]!=expected:raise ValueError("D001 evidence changed")
        records.append(r);ids.append(id)
    if len(set(ids))!=1 or any(r["module_versions"]!=records[0]["module_versions"] for r in records):raise ValueError("campaign/profile mismatch")
    return records

def main():
    p=argparse.ArgumentParser();p.add_argument("grid",type=Path);p.add_argument("save_dir",type=Path);p.add_argument("report",type=Path);a=p.parse_args()
    with a.grid.open(encoding="utf-8",newline="") as f:grid=compare_grid(csv.DictReader(f))
    records=read_saves(a.save_dir)
    d=compare_d001(records)
    result={"grid":grid,"d001":d,"grid_sha256":hashlib.sha256(a.grid.read_bytes()).hexdigest(),"save_hashes":[r["sha256"] for r in records]}
    if a.report.suffix.lower()!=".json":raise ValueError("JSON only")
    with a.report.open("x",encoding="utf-8") as f:json.dump(result,f,indent=2,sort_keys=True,allow_nan=False)
    print("GRID",json.dumps({k:v for k,v in grid.items() if k!="details"}))
    print("D001",json.dumps(d))
    print("REPORT_SHA256",hashlib.sha256(a.report.read_bytes()).hexdigest())
    return 1 if grid["mismatches"] or any(r["status"] in ("MISMATCH","INPUT_REPLAY_MISMATCH") and r["basis"]!="partial_economy" for r in d["metrics"]) else 0
if __name__=="__main__":raise SystemExit(main())
