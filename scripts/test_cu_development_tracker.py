#!/usr/bin/env python3
"""Check tracker completeness and referential integrity; not native acceptance."""
import csv
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def rows(name):
    with (root/"Data"/name).open(encoding="utf-8",newline="") as f:return list(csv.DictReader(f))
core=rows("CU_Core_System_Review.csv")
feature=rows("CU_Feature_Development_Tracker.csv")
tests=rows("CU_Six_System_Native_Test_Matrix.csv")
runs=rows("CU_Native_Test_Execution_Tracker.csv")
road=rows("CU_Six_System_Roadmap.csv")
milestones=rows("CU_Milestone_Tracker.csv")
owners=rows("CU_Module_Ownership_Audit.csv")
compat=rows("CU_Compatibility_Tracker.csv")
bugs=rows("CU_Bug_Tracker.csv")
release=rows("CU_Release_Evidence_Tracker.csv")
assert len(core)==len(feature)==143
assert len({x["feature_id"] for x in feature})==143
assert {x["feature_id"] for x in feature}=={x["feature_id"] for x in core}
byid={x["feature_id"]:x for x in core}
assert all(x["disposition"]==byid[x["feature_id"]]["disposition"] and (root/x["source_path"]).is_file() and x["next_action"] for x in feature)
testids={x["test_id"] for x in tests}
assert len(testids)==56 and {x["test_id"] for x in runs}==testids
assert all(set(x["test_group"].split(";"))<=testids for x in feature)
assert {x["milestone"] for x in milestones}=={x["milestone"] for x in road} and len(milestones)==9
assert len(compat)==len(owners)==41
assert len({x["module_id"] for x in compat})==41
assert all(x["native_result"] in ("PENDING","PASS","FAIL","BLOCKED","NOT_APPLICABLE") for x in compat)
assert len({x["bug_id"] for x in bugs})==len(bugs)
assert all(x["priority"] in ("P0","P1","P2") and x["next_action"] for x in bugs)
assert all(x["status"]!="PASS" or (x["observed_evidence"] and x["candidate_commit"]) for x in runs)
assert all(x["status"]!="PASS_GATE_B_ONLY" or (x["gate"]=="B" and x["observed_evidence"]) for x in runs)
assert all(x["highest_gate"]!="G" or (x["ci_run"] and x["artifact_id"] and len(x["dll_sha256"])==64) for x in release)
for i,name in enumerate(["S1_STARTING_LIFE.md","S2_FAMILY_AND_DYNASTY.md","S3_HOUSEHOLD_AND_RETAINERS.md","S4_LIVING_RELATIONSHIPS.md","S5_CAREER_AND_SWORN_SERVICE.md","S6_PERSONAL_CHRONICLE.md"],1):
    assert (root/"docs"/"design"/name).is_file()
print("PASS SOURCE ONLY: six design specs, 143 feature rows, 56 test IDs, nine milestones, 41 ownership entries, unique bug IDs and candidate evidence.")
