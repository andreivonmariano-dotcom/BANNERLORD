#!/usr/bin/env python3
"""CI verifies native-game audit refuses absent or source-only evidence."""
from pathlib import Path
import csv
import importlib.util
import tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("seven_live",
    root/"scripts"/"audit_v070_seven_live_start_evidence.py")
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
assert len(module.ROLES)==7
assert module.ROLES==(
    "Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King")
with tempfile.TemporaryDirectory() as folder:
    root=Path(folder)
    events=root/"events.csv"
    proof=root/"proof"
    proof.mkdir()
    # A source-modeled contract is not actual native family/service.
    with events.open("w",encoding="utf-8",newline="") as stream:
        writer=csv.DictWriter(stream,fieldnames=[
            "utc","session_id","context","detail"])
        writer.writeheader()
        for i,role in enumerate(module.ROLES):
            writer.writerow(dict(
                utc=f"2026-11-21T01:{i:02d}:00Z",
                session_id=f"synthetic_{i}",
                context="sandbox_start_contract",
                detail=f"result=PASS|role={role}|native_type=default"))
    rows,error=module.read_events(events)
    assert not error
    findings=[module.audit_role(role,rows,proof)
              for role in module.ROLES]
    assert len(findings)==7
    assert all(x["result"]=="BLOCKED" for x in findings)
    assert all("missing_post_reload_native_identity_proof" in
               x["missing"] for x in findings)
    assert all("missing_live_native_ui_screenshot" in
               x["missing"] for x in findings)
    # The external Soldier enlistment path cannot pass on a label.
    assert "missing_cu_sas_service_observed" in findings[3]["missing"]
    assert "missing_cu_king_native_council_meeting" in findings[6]["missing"]
    assert module.read_events(root/"absent.csv")[1]==(
        "missing_runtime_events.csv")
print("PASS v0.70o: seven live acceptance roles require actual "
      "native runtime sessions, post-reload evidence and screenshots; "
      "source/CI simulations alone remain BLOCKED.")
