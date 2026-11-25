#!/usr/bin/env python3
"""Current evidence intake. REVIEW_READY is not a native PASS or release certification."""
import argparse
import csv
import io
import json
import re
import struct
import zipfile
from pathlib import Path
from datetime import datetime

ROLES=("Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King")
# Actual retained receipts. No retired graphical or five-recruit transfer requirements.
ROLE_RECEIPTS={
 "Wanderer":(("cu_wanderer_companion_start_contract",{"result":"PASS"}),),
 "Outlaw":(("cu_outlaw_band_start_contract",{"result":"PASS"}),("cu_outlaw_lieutenant_start_contract",{"result":"PASS"})),
 "Mercenary":(("cu_mercenary_company_native_verification",{"result":"PASS"}),),
 "Soldier":(("cu_soldier_native_start_verification",{"result":"PASS"}),("cu_sas_service_observed",{"result":"external_enlistment_verified","party_confirmed":"True"})),
 "Knight":(("cu_knight_oath_start",{"result":"oath_recorded","independent_house":"true","native_vassalage":"false"}),("cu_knight_patron_conversation_muster",{"result":"native_meeting_muster_recorded","native_conversation":"true"}),("cu_knight_native_escort_completed",{"result":"verified_native_party_escort_completed","native_hero_party":"True"})),
 "Lord":(("cu_lord_native_final_verification",{"result":"PASS"}),("cu_lord_native_family_verification",{"result":"PASS"})),
 "King":(("cu_king_native_final_verification",{"result":"PASS"}),("cu_king_royal_minister_native_governor",{"result":"PASS"}),("cu_king_native_council_meeting",{"result":"real_royal_hero_council_convened","native_minister_governs":"True"}))
}
def fields(s,sep="|"):
    out={}
    for part in s.split(sep):
        k,eq,v=part.partition("=")
        if not eq or not k or k in out:return {}
        out[k]=v
    return out
def iso(value):
    return datetime.fromisoformat(value.replace("Z","+00:00"))
def local(root,name):
    if not isinstance(name,str) or not name or Path(name).name!=name or "/" in name or "\\" in name:
        raise ValueError("evidence filename must be a local filename")
    p=root/name
    if not p.is_file() or p.is_symlink():raise ValueError("missing regular evidence file")
    return p
def read_session(root,name,sha):
    p=local(root,name)
    with zipfile.ZipFile(p) as z:
        names=z.namelist()
        for n in ("runtime-info.txt","events.csv"):
            if names.count(n)!=1:raise ValueError("missing or duplicate "+n)
            if z.getinfo(n).file_size>20_000_000:raise ValueError("oversized evidence entry")
        info={}
        for line in z.read("runtime-info.txt").decode("utf-8-sig").splitlines():
            if line.startswith("loaded_game_assemblies="):break
            k,e,v=line.partition("=")
            if e:
                if k in info:raise ValueError("duplicate runtime-info key")
                info[k]=v
        sid=info.get("session_id","")
        if not sid or info.get("module_dll_sha256","").lower()!=sha:raise ValueError("exact DLL hash mismatch")
        if info.get("status")!="normal_shutdown:submodule_unloaded":raise ValueError("closed normal shutdown report required")
        reader=csv.DictReader(io.StringIO(z.read("events.csv").decode("utf-8-sig")))
        if not {"session_id","context","detail","utc"}.issubset(reader.fieldnames or []):raise ValueError("invalid event schema")
        events=list(reader)
        if not events or any(e["session_id"]!=sid for e in events):raise ValueError("mixed or empty session")
        if not any(e["context"]=="session_capture_started" for e in events):raise ValueError("missing capture start")
        times=[iso(e["utc"]) for e in events]
        if any(t.tzinfo is None for t in times) or any(a>b for a,b in zip(times,times[1:])):raise ValueError("event timestamps invalid")
        if iso(info["utc"])<times[-1]:raise ValueError("report precedes events")
        return sid,events,times[0],iso(info["utc"])
def latest(events,context):
    rows=[fields(e["detail"]) for e in events if e["context"]==context]
    return rows[-1] if rows else {}
def receipt(events,context,expected):
    # Use latest receipt, fail closed on later contradictory result.
    actual=latest(events,context)
    return all(actual.get(k)==v for k,v in expected.items())
def screenshot(root,name):
    p=local(root,name)
    with p.open("rb") as stream:header=stream.read(24)
    if p.stat().st_size<1024 or len(header)!=24 or header[:8]!=b"\x89PNG\r\n\x1a\n" or header[12:16]!=b"IHDR":raise ValueError("invalid screenshot header")
    w,h=struct.unpack(">II",header[16:24])
    if not 640<=w<=16384 or not 360<=h<=16384:raise ValueError("invalid screenshot dimensions")
def audit(root,role,sha,profile):
    sessions=[]
    errors=[]
    try:
        d=json.loads(local(root,role.lower()+".json").read_text(encoding="utf-8"))
        if not isinstance(d,dict) or d.get("career")!=role or d.get("profile")!=profile or d.get("candidate_dll_sha256","").lower()!=sha:
            raise ValueError("wrong role/profile/candidate in evidence")
        sid,events,start,close=read_session(root,d.get("start_archive"),sha)
        rid,revents,rstart,rclose=read_session(root,d.get("reload_archive"),sha)
        sessions=[sid,rid]
        if sid==rid or rstart<=close:errors.append("reload must be separate later session")
        starts=[fields(e["detail"]) for e in events if e["context"]=="sandbox_start_contract"]
        if not starts or starts[-1].get("role")!=role or starts[-1].get("result")!="PASS" or any(e.get("role")!=role for e in starts):
            errors.append("same-session role start contract missing or contradictory")
        if not receipt(events,"cu_starting_life_preflight",{"result":"PASS"}):errors.append("missing valid preflight")
        for ctx,expected in ROLE_RECEIPTS[role]:
            if role=="Soldier" and ctx=="cu_sas_service_observed":
                history=[fields(e["detail"]) for e in events if e["context"]==ctx]
                initial=any(x.get("result")=="external_enlistment_verified" and x.get("party_confirmed")=="True" for x in history)
                current=history[-1] if history else {}
                if not initial or current.get("party_confirmed")!="True" or current.get("result") not in ("external_enlistment_verified","external_rank_changed_observed","external_duty_changed_observed"):
                    errors.append("missing or no longer verified SaS service")
            elif not receipt(events,ctx,expected):errors.append("missing/unconfirmed "+ctx)
        # The current acceptance UI is native InquiryData, not the retired graphical probe.
        if not any(receipt(group,"cu_campaign_affairs_native_open",{"custom_movie":"false"}) for group in (events,revents)):
            errors.append("missing current native correspondence receipt")
        if any(e["context"]=="sandbox_start_contract" for e in revents):errors.append("reload archive contains a new start contract; separate reload required")
        if not any(e["context"]=="sync_data_invoked" for e in revents):errors.append("missing reload sync observation (not reload proof alone)")
        if role in ("Lord","King") and not any(receipt(group,"cu_household_native_party_duty",{"result":"PASS","native_role_confirmed":"True","other_specialists_unchanged":"True"}) or receipt(group,"cu_household_native_governor_order",{"result":"PASS","native_action":"true","no_teleport":"true","no_incumbent_replaced":"true"}) for group in (events,revents)):
            errors.append("missing retained native household appointment receipt")
        for key in ("save_label","player_id_before","player_id_after","clan_id_before","clan_id_after","reviewer"):
            if not isinstance(d.get(key),str) or not d[key].strip():errors.append("missing manual "+key)
        if d.get("player_id_before")!=d.get("player_id_after") or d.get("clan_id_before")!=d.get("clan_id_after"):errors.append("manual reload identity differs")
        if d.get("actual_reload_after_quit") is not True:errors.append("manual reload observation missing")
        obs=d.get("observations",{})
        for key in ("native_start","role_action","native_ui","post_reload","negative_case"):
            if not isinstance(obs.get(key),str) or not obs[key].strip():errors.append("missing observation "+key)
        if d.get("start_screenshot")==d.get("reload_screenshot"):errors.append("distinct screenshot filenames required")
        screenshot(root,d.get("start_screenshot"));screenshot(root,d.get("reload_screenshot"))
    except (ValueError,OSError,KeyError,TypeError,AttributeError,zipfile.BadZipFile,UnicodeError,csv.Error) as e:
        errors.append(str(e))
    return {"career":role,"status":"BLOCKED" if errors else "REVIEW_READY","sessions":sessions,"issues":errors}
def evaluate(root,sha,profile):
    results=[audit(root,role,sha,profile) for role in ROLES]
    counts={}
    for row in results:
        for sid in row["sessions"]:counts[sid]=counts.get(sid,0)+1
    for row in results:
        if any(counts[sid]>1 for sid in row["sessions"]):
            row["status"]="BLOCKED";row["issues"].append("session reused across evidence records")
    return results
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("evidence_directory",type=Path);p.add_argument("--dll-sha256",required=True);p.add_argument("--profile",required=True)
    a=p.parse_args();sha=a.dll_sha256.lower()
    if not re.fullmatch("[0-9a-f]{64}",sha) or not a.profile.strip():p.error("exact SHA256 and named profile required")
    results=evaluate(a.evidence_directory,sha,a.profile)
    print(json.dumps({"candidate_dll_sha256":sha,"profile":a.profile,"native_certification":False,"note":"REVIEW_READY requires manual native evidence review; not PASS.","results":results},indent=2))
    return 0 if all(x["status"]=="REVIEW_READY" for x in results) else 1
if __name__=="__main__":raise SystemExit(main())
