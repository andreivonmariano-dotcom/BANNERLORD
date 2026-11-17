#!/usr/bin/env python3
"""Validate feature-level status provenance and one-to-one historical mod coverage."""
from pathlib import Path
import csv,sys
ROOT=Path(__file__).resolve().parent.parent
ALLOWED={"IDENTIFIED","SCOPED","IMPLEMENTED_SOURCE","STATIC_CHECKED","COMPILED","GAME_TESTED","REGRESSION_TESTED","RELEASED","BLOCKED"}
GATES={"NONE","A","B","C","D","E","F","G"}
REQUIRED={"feature_id","feature_name","milestone","source_path","inspired_by_module_id","status","highest_gate","build_evidence","gameplay_evidence","notes"}

def read_rows(path):
    with open(path,encoding="utf-8-sig",newline="") as f:
        return list(csv.DictReader(f))

def validate(rows,history):
    errors=[]
    ids=[row.get("feature_id") for row in rows]
    hist_ids={row["module_id"] for row in history}
    if len(hist_ids)!=78: errors.append("Expected exactly 78 original modules")
    if len(ids)!=len(set(ids)): errors.append("Duplicate feature IDs")
    if not rows or not REQUIRED.issubset(set(rows[0])): errors.append("Missing schema columns")
    by_mod={m:0 for m in hist_ids}
    for r in rows:
        ident=r.get("feature_id","")
        s=r.get("status","");gate=r.get("highest_gate","")
        if s not in ALLOWED: errors.append(f"{ident}: invalid status {s}")
        if gate not in GATES:errors.append(f"{ident}: invalid gate {gate}")
        if not r.get("milestone","").startswith("M"):errors.append(f"{ident}: missing milestone")
        if not (ROOT/r.get("source_path", "")).is_file(): errors.append(f"{ident}: missing source path")
        mid=r.get("inspired_by_module_id","")
        if mid and mid not in hist_ids: errors.append(f"{ident}: unknown historical mod {mid}")
        if ident.startswith("HIST-"):
            if mid in by_mod:by_mod[mid]+=1
            if s not in ("IDENTIFIED","SCOPED","BLOCKED"):
                errors.append(f"{ident}: unimplemented historical coverage asserted")
        if s in ("COMPILED","GAME_TESTED","REGRESSION_TESTED","RELEASED") and not r.get("build_evidence"):
            errors.append(f"{ident}: missing build proof")
        if s in ("GAME_TESTED","REGRESSION_TESTED","RELEASED") and not r.get("gameplay_evidence"):
            errors.append(f"{ident}: missing gameplay proof")
        if s=="RELEASED" and gate!="G": errors.append(f"{ident}: released without Gate G")
        if s=="COMPILED" and gate not in ("B","C","D","E","F","G"):
            errors.append(f"{ident}: compiled feature missing Gate B")
        if s=="IDENTIFIED" and gate!="NONE":errors.append(f"{ident}: identified feature has execution gate")
    missing=[m for m,count in by_mod.items() if count!=1]
    if missing: errors.append(f"Historical coverage must map each original once: {missing}")
    return errors

def main():
    rows=read_rows(ROOT/'Data/Feature_Registry.csv')
    history=read_rows(ROOT/'Data/Migration_78.csv')
    failures=validate(rows,history)
    if failures:
        print("Feature registry FAILED:\n"+"\n".join(failures))
        return 1
    print(f"Feature registry PASS: {len(rows)} records, 78 unique historical references, {len(rows)-78} implemented/source feature rows")
    return 0
if __name__=='__main__': sys.exit(main())
