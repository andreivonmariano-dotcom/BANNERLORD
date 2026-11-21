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

# These are synthetic parser fixtures, never actual Bannerlord acceptance.
# Exercise positive and negative branches, not only missing-file rejection.
import json
import struct
import zlib

def native_png():
    width,height=640,360
    raw=(b"\x00"+b"\x11\x22\x33"*width)*height
    def chunk(name,data):
        return (struct.pack(">I",len(data))+name+data+
                struct.pack(">I",zlib.crc32(name+data)&0xffffffff))
    return (b"\x89PNG\r\n\x1a\n"+
            chunk(b"IHDR",struct.pack(">IIBBBBB",width,height,8,2,0,0,0))+
            chunk(b"IDAT",zlib.compress(raw))+chunk(b"IEND",b""))

def row(role,context,status,session=None):
    return dict(utc="2026-11-21T02:00:00Z",
                session_id=session or "session_"+role,
                context=context,detail=status)

def fixtures(role):
    events=[]
    for context in module.REQUIRED[role]:
        receipt=module.GOOD.get(context,("result=PASS",))[0]
        extras=""
        if context=="cu_royal_court_native_commission":
            extras=("|native_court_seat=royal_fief"
                    "|actual_marshal=marshal_id"
                    "|actual_minister_governor=minister_id"
                    "|actual_kingsguard_hero=guard_id")
        if context=="cu_king_royal_minister_native_governor":
            extras="|minister=minister_id|settlement=royal_fief"
        events.append(row(role,context,receipt+extras))
    events.append(row(role,"sandbox_start_contract",
                      "result=PASS|role="+role))
    if role in module.ROLE_UI_SCREENSHOTS:
        events.append(row(role,"cu_native_household_display_opened",
                          "result=OPENED|role="+role+
                          "|native_read_only=true"))
    return events

def proof_for(role):
    proof=dict(role=role,start_session_id="session_"+role,
        native_reloaded_after_quit=True,same_player_hero_id="player",
        same_clan_id="clan",observed_native_role_after_reload=role,
        verified_family_after_reload=True,
        verified_service_or_authority_after_reload=True,
        ui_native_screen_visible_after_reload=True,
        ui_no_overlap_or_clipping=True,
        ui_native_back_navigation=True,
        ui_matches_real_game_state=True)
    if role=="King":
        proof.update(
            royal_seat_id_after_reload="royal_fief",
            marshal_hero_id_after_reload="marshal_id",
            royal_minister_hero_id_after_reload="minister_id",
            kingsguard_hero_id_after_reload="guard_id",
            minister_native_governor_after_reload=True,
            marshal_in_main_party_after_reload=True,
            kingsguard_in_main_party_after_reload=True,
            kingsguard_native_equipment_observed=True,
            kingsguard_entered_native_combat=True,
            kingsguard_post_battle_identity_confirmed=True)
    return proof

with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder)
    native=folder/"proof"
    native.mkdir()
    for role in module.ROLES:
        (native/(role.lower()+".json")).write_text(
            json.dumps(proof_for(role)),encoding="utf-8")
        (native/(role.lower()+".png")).write_bytes(native_png())
    (native/"king_kingsguard_battle.png").write_bytes(native_png())
    for screenshots in module.ROLE_UI_SCREENSHOTS.values():
        for filename in screenshots:
            (native/filename).write_bytes(native_png())
    cases=[x for role in module.ROLES for x in fixtures(role)]
    good=module.audit_all(cases,native)
    assert len(good)==7 and all(x["result"]=="PASS" for x in good)
    # A signed-looking fixture passes format validation, not genuine-game
    # provenance. It must never be published as a live session result.
    assert all(x["session_id"].startswith("session_") for x in good)
    def mutate(role,context,value):
        rows=[dict(x) for x in cases]
        i=next(i for i,x in enumerate(rows)
               if x["session_id"]=="session_"+role and
               x["context"]==context)
        rows[i]["detail"]=value
        return rows
    assert "unverified_cu_starting_life_preflight" in (
        module.audit_role("Wanderer",mutate("Wanderer",
            "cu_starting_life_preflight","result=PASSIVE"),native)["missing"])
    assert "unverified_cu_starting_life_preflight" in (
        module.audit_role("Wanderer",mutate("Wanderer",
            "cu_starting_life_preflight","result=FAIL|result=PASS"),native)["missing"])
    assert "unverified_cu_knight_oath_start" in (
        module.audit_role("Knight",mutate("Knight",
            "cu_knight_oath_start","result=PASS|native_personal_oath"),native)["missing"])
    assert "unverified_cu_sas_service_observed" in (
        module.audit_role("Soldier",mutate("Soldier",
            "cu_sas_service_observed",
            "result=external_service_ended_observed|party_confirmed=False"),
            native)["missing"])
    assert "unverified_cu_king_native_council_meeting" in (
        module.audit_role("King",mutate("King",
            "cu_king_native_council_meeting",
            "result=real_royal_hero_council_convened|native_minister_governs=False|marshal_in_party=True|kingsguard_single_hero_in_party=True"),
            native)["missing"])
    poisoned=cases+[row("King","sandbox_start_contract",
                       "result=FAIL|reason=council_unverified")]
    assert module.audit_role("King",poisoned,native)["result"]=="BLOCKED"
    same=[dict(x) for x in cases]
    for x in same:
        if x["session_id"]=="session_Outlaw":
            x["session_id"]="session_Wanderer"
    proof_outlaw=proof_for("Outlaw")
    proof_outlaw["start_session_id"]="session_Wanderer"
    (native/"outlaw.json").write_text(
        json.dumps(proof_outlaw),encoding="utf-8")
    duplicated=module.audit_all(same,native)
    assert duplicated[0]["result"]=="BLOCKED"
    assert duplicated[1]["result"]=="BLOCKED"
    assert "session_reused_between_starting_paths" in duplicated[1]["missing"]
    (native/"outlaw.json").write_text(
        json.dumps(proof_for("Outlaw")),encoding="utf-8")
    false_reloaded=proof_for("King")
    false_reloaded["native_reloaded_after_quit"]="False"
    (native/"king.json").write_text(
        json.dumps(false_reloaded),encoding="utf-8")
    assert "incomplete_native_reload_observation" in (
        module.audit_role("King",cases,native)["missing"])
    false_reloaded=proof_for("King")
    false_reloaded["kingsguard_hero_id_after_reload"]="different_hero"
    (native/"king.json").write_text(
        json.dumps(false_reloaded),encoding="utf-8")
    assert "king_native_officers_combat_or_reload_unverified" in (
        module.audit_role("King",cases,native)["missing"])
    (native/"king.json").write_text(
        json.dumps(proof_for("King")),encoding="utf-8")
    (native/"king_kingsguard_battle.png").write_bytes(b"not_a_png"*300)
    assert "missing_kingsguard_native_battle_screenshot" in (
        module.audit_role("King",cases,native)["missing"])
    (native/"king_kingsguard_battle.png").write_bytes(native_png())
    assert module.audit_role("King",cases+[row("King",
        "exception_detected","context=king_critical")],native)["result"]=="BLOCKED"
    king_screens=native/"king_council_ui.png"
    king_screens.unlink()
    assert "missing_native_ui_king_council_ui.png" in (
        module.audit_role("King",cases,native)["missing"])
    king_screens.write_bytes(native_png())
    missing_ui=proof_for("Lord")
    missing_ui["ui_native_back_navigation"]="True"
    (native/"lord.json").write_text(json.dumps(missing_ui),encoding="utf-8")
    assert "native_ui_unverified_ui_native_back_navigation" in (
        module.audit_role("Lord",cases,native)["missing"])
    (native/"lord.json").write_text(json.dumps(proof_for("Lord")),encoding="utf-8")
    missing_entry=[x for x in cases
        if not (x["context"]=="cu_native_household_display_opened"
                and x["session_id"]=="session_Knight")]
    assert "native_household_menu_not_opened" in (
        module.audit_role("Knight",missing_entry,native)["missing"])
print("PASS v0.70s: exact native receipts, unique sessions, terminal "
      "status, governor/one-Hero King identity, readable native UI "
      "interaction, viewport/Back evidence and fail-closed checks.")
