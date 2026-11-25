#!/usr/bin/env python3
"""Synthetic evidence intake tests only. No game session is claimed."""
import csv
import importlib.util
import io
import json
import tempfile
import zipfile
from pathlib import Path
spec=importlib.util.spec_from_file_location("audit",Path(__file__).with_name("audit_cu_current_playtest.py"))
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
SHA="a"*64
def make_zip(root,name,sid,day,rows,sha=SHA):
    with zipfile.ZipFile(root/name,"w") as z:
        z.writestr("runtime-info.txt",f"utc=2026-11-{day}T12:00:00Z\nsession_id={sid}\nstatus=normal_shutdown:submodule_unloaded\nmodule_dll_sha256={sha}\nloaded_game_assemblies=\n")
        s=io.StringIO();w=csv.writer(s);w.writerow(["utc","session_id","context","detail"])
        for context,detail in [("session_capture_started","synthetic=true")]+rows:w.writerow([f"2026-11-{day}T11:00:00Z",sid,context,detail])
        z.writestr("events.csv",s.getvalue())
with tempfile.TemporaryDirectory() as d:
    root=Path(d)
    assert all(x["status"]=="BLOCKED" for x in a.evaluate(root,SHA,"fixture"))
    role="Lord";name="lord.json"
    rows=[("sandbox_start_contract","result=PASS|role=Lord"),("cu_starting_life_preflight","result=PASS"),("cu_campaign_affairs_native_open","custom_movie=false"),("cu_household_native_governor_order","result=PASS|native_action=true|no_teleport=true|no_incumbent_replaced=true")]
    rows.extend((ctx,"|".join(k+"="+v for k,v in req.items())) for ctx,req in a.ROLE_RECEIPTS[role])
    make_zip(root,"start.zip","synthetic-start","20",rows)
    make_zip(root,"reload.zip","synthetic-reload","21",[("sync_data_invoked","not_proof_of_save")])
    # Deliberately artificial PNG headers used only to test intake. Not visual evidence.
    import struct
    for n in ("before.png","after.png"):
        (root/n).write_bytes(b"\x89PNG\r\n\x1a\n"+b"\0\0\0\rIHDR"+struct.pack(">II",1280,720)+b"x"*1024)
    proof={"career":role,"profile":"fixture","candidate_dll_sha256":SHA,"start_archive":"start.zip","reload_archive":"reload.zip","start_screenshot":"before.png","reload_screenshot":"after.png","save_label":"synthetic","player_id_before":"p","player_id_after":"p","clan_id_before":"c","clan_id_after":"c","reviewer":"fixture","actual_reload_after_quit":True,"observations":{k:"synthetic" for k in ("native_start","role_action","native_ui","post_reload","negative_case")}}
    def save(): (root/name).write_text(json.dumps(proof))
    save();assert a.audit(root,role,SHA,"fixture")["status"]=="REVIEW_READY"
    assert a.audit(root,role,"b"*64,"fixture")["status"]=="BLOCKED"
    proof["player_id_after"]="heir";save();assert a.audit(root,role,SHA,"fixture")["status"]=="BLOCKED"
    proof["player_id_after"]="p";proof["reload_archive"]="start.zip";save();assert a.audit(root,role,SHA,"fixture")["status"]=="BLOCKED"
    proof["reload_archive"]="reload.zip";save()
    make_zip(root,"start.zip","synthetic-start","20",rows+[("sandbox_start_contract","result=FAIL|role=Lord")])
    assert a.audit(root,role,SHA,"fixture")["status"]=="BLOCKED"
    make_zip(root,"start.zip","synthetic-start","20",[x for x in rows if x[0]!="cu_campaign_affairs_native_open"])
    assert a.audit(root,role,SHA,"fixture")["status"]=="BLOCKED"
    proof["start_archive"]="../start.zip";save();assert a.audit(root,role,SHA,"fixture")["status"]=="BLOCKED"
print("PASS SYNTHETIC ONLY: current evidence intake rejects missing/mismatched DLL, reused session, changed identity, later FAIL, missing native inbox and path escape; readiness is not native PASS.")
