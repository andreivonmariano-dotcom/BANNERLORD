#!/usr/bin/env python3
"""Read-only, fail-closed audit of seven genuinely played Sandbox starts.

The source/build/simulation gates are not native-playthrough proof.
A PASS for a role requires real same-session runtime events, a separate
post-reload evidence record and one native UI screenshot file, in the
explicit evidence directory. It does not launch Bannerlord or edit saves.
"""
import argparse
import csv
import json
import pathlib
import struct
import sys
from collections import Counter

ROLES=("Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King")
# Native UI acceptance is an independent release gate, not an optional
# consequence of valid policy or compiling a Gauntlet view.
NATIVE_UI_FIELDS=(
 "ui_native_screen_visible_after_reload",
 "ui_no_overlap_or_clipping",
 "ui_native_back_navigation",
 "ui_matches_real_game_state",
)
ROLE_UI_SCREENSHOTS={
 "Knight":("knight_oath_ui.png","knight_household_ui.png"),
 "Lord":("lord_inheritance_ui.png","lord_household_ui.png"),
 "King":("king_council_ui.png","king_governor_ui.png",
         "king_household_ui.png"),
}
REQUIRED={
 "Wanderer":("cu_starting_life_preflight",
             "cu_wanderer_companion_start_contract"),
 "Outlaw":("cu_starting_life_preflight",
           "cu_outlaw_lieutenant_start_contract",
           "cu_outlaw_band_start_contract"),
 "Mercenary":("cu_starting_life_preflight",
              "cu_mercenary_native_preflight",
              "cu_mercenary_company_native_verification"),
 "Soldier":("cu_starting_life_preflight",
            "cu_soldier_real_enlistment_preflight",
            "cu_soldier_native_start_verification",
            "cu_sas_service_observed"),
 "Knight":("cu_starting_life_preflight",
           "cu_knight_oath_start"),
 "Lord":("cu_starting_life_preflight",
         "cu_lord_native_preflight",
         "cu_lord_native_family_verification",
         "cu_lord_native_noble_inlaws",
         "cu_lord_native_household_officers",
         "cu_lord_native_final_verification"),
 "King":("cu_starting_life_preflight",
         "cu_king_native_preflight",
         "cu_king_native_dynasty_verification",
         "cu_king_native_court_verification",
         "cu_king_native_final_verification",
         "cu_king_royal_minister_native_governor",
         "cu_royal_court_native_commission",
         "cu_king_native_council_meeting"),
}
# Details for these already represent status rather than a strict PASS tag.
GOOD={
 "cu_starting_life_preflight":("result=PASS",),
 "cu_outlaw_band_start_contract":("result=PASS",),
 "cu_outlaw_lieutenant_start_contract":("result=PASS",),
 "cu_wanderer_companion_start_contract":("result=PASS",),
 "cu_mercenary_native_preflight":("result=PASS",),
 "cu_mercenary_company_native_verification":("result=PASS",),
 "cu_soldier_real_enlistment_preflight":("result=PASS",),
 "cu_soldier_native_start_verification":("result=PASS",),
 # Exact values from SandboxServiceCareerState.ObserveExternal and
 # SandboxKnightOathState.Begin. A prior rank change is not a discharge.
 "cu_sas_service_observed":(
     "result=external_enlistment_verified|party_confirmed=True",
     "result=external_rank_changed_observed|party_confirmed=True",
     "result=external_duty_changed_observed|party_confirmed=True"),
 "cu_knight_oath_start":(
     "result=oath_recorded|independent_house=true|native_vassalage=false",),
 "cu_lord_native_preflight":("result=PASS",),
 "cu_lord_native_family_verification":("result=PASS",),
 "cu_lord_native_noble_inlaws":("result=PASS",),
 "cu_lord_native_household_officers":("result=PASS",),
 "cu_lord_native_final_verification":("result=PASS",),
 "cu_king_native_preflight":("result=PASS",),
 "cu_king_native_dynasty_verification":("result=PASS",),
 "cu_king_native_court_verification":("result=PASS",),
 "cu_king_native_final_verification":("result=PASS",),
 "cu_king_royal_minister_native_governor":("result=PASS",),
 "cu_royal_court_native_commission":(
     "result=native_royal_hero_council_commissioned|kingsguard_hero_only=true",),
 "cu_king_native_council_meeting":(
     "result=real_royal_hero_council_convened|native_minister_governs=True|marshal_in_party=True|kingsguard_single_hero_in_party=True",),
}

def detail(row):
    """Parse structured native journal fields, refusing duplicate or bad keys."""
    result={}
    for part in (row.get("detail") or "").split("|"):
        key,sep,value=part.partition("=")
        if not sep or not key or key in result:
            return {}
        result[key]=value
    return result

def verified(row,context):
    """Require every key/value of one allowed event receipt, not substrings."""
    fields=detail(row)
    for alternative in GOOD.get(context,("result=PASS",)):
        required=(part.partition("=") for part in alternative.split("|"))
        if all(sep and fields.get(key)==value
               for key,sep,value in required):
            return True
    return False

def screenshot_ok(path):
    """Check a minimally sound native UI PNG, not just a renamed text file."""
    if not path.is_file() or path.stat().st_size<1024:
        return False
    try:
        with path.open("rb") as stream:
            header=stream.read(24)
        return (len(header)==24 and
            header[:8]==b"\x89PNG\r\n\x1a\n" and
            header[12:16]==b"IHDR" and
            640<=struct.unpack(">I",header[16:20])[0]<=16384 and
            360<=struct.unpack(">I",header[20:24])[0]<=16384)
    except (OSError,struct.error):
        return False

def read_events(path):
    if not path.is_file():
        return [],"missing_runtime_events.csv"
    with path.open(encoding="utf-8-sig",newline="") as stream:
        reader=csv.DictReader(stream)
        if not {"session_id","context","detail","utc"}.issubset(
                reader.fieldnames or []):
            return [],"invalid_events_columns"
        return list(reader),""

def audit_role(role,rows,evidence_root):
    # Each role must be proven in a DIFFERENT real new-campaign session.
    # A historical contract or a pending Soldier invitation is not PASS.
    sessions={}
    for row in rows:
        sid=row.get("session_id") or ""
        if sid:
            sessions.setdefault(sid,[]).append(row)
    candidates=[]
    reused_session=False
    for session,group in sessions.items():
        contracts=[row for row in group
                   if row.get("context")=="sandbox_start_contract"]
        roles_in_session={detail(row).get("role") for row in contracts
                          if detail(row).get("role") in ROLES}
        # A single logger session cannot stand for two different new
        # campaigns, regardless of which start contract was logged last.
        if len(roles_in_session)>1:
            reused_session |= role in roles_in_session
            continue
        # A late native FAIL supersedes an earlier PASS in the same session.
        if (contracts and detail(contracts[-1]).get("role")==role
                and detail(contracts[-1]).get("result")=="PASS"):
            candidates.append((session,group))
    problems=[]
    if not candidates:
        missing=["same_session_native_start_contract_PASS"]
        if reused_session:
            missing.append("session_reused_between_starting_paths")
        return {"role":role,"result":"BLOCKED","missing":missing}
    # Latest chronological logged candidate, not a stale historical
    # unrelated session. This is evidence, not a game-memory mutation.
    session,group=max(candidates,key=lambda p:max(
        row.get("utc") or "" for row in p[1]))
    required=REQUIRED[role]
    for context in required:
        matches=[x for x in group if x.get("context")==context]
        if not matches:
            problems.append("missing_"+context)
            continue
        # The latest observed state for a given context is authoritative.
        # This rejects false flags and later external service termination.
        if not verified(matches[-1],context):
            problems.append("unverified_"+context)
    proof=evidence_root/(role.lower()+".json")
    screenshot=evidence_root/(role.lower()+".png")
    evidence={}
    if not proof.is_file():
        problems.append("missing_post_reload_native_identity_proof")
    else:
        try:
            evidence=json.loads(proof.read_text(encoding="utf-8"))
            if (not isinstance(evidence,dict) or
                evidence.get("role")!=role or
                evidence.get("start_session_id")!=session or
                evidence.get("native_reloaded_after_quit") is not True or
                not isinstance(evidence.get("same_player_hero_id"),str) or
                not evidence["same_player_hero_id"] or
                not isinstance(evidence.get("same_clan_id"),str) or
                not evidence["same_clan_id"] or
                not isinstance(evidence.get("observed_native_role_after_reload"),str) or
                not evidence["observed_native_role_after_reload"] or
                evidence.get("verified_family_after_reload") is not True or
                evidence.get("verified_service_or_authority_after_reload") is not True):
                problems.append("incomplete_native_reload_observation")
        except (ValueError,OSError,TypeError):
            problems.append("invalid_post_reload_proof")
    if not screenshot_ok(screenshot):
        problems.append("missing_live_native_ui_screenshot")
    # These are manual post-reload observations, not results inferred
    # from a compiled UI, a synthetic screen or raw image file existence.
    for field in NATIVE_UI_FIELDS:
        if evidence.get(field) is not True:
            problems.append("native_ui_unverified_"+field)
    for filename in ROLE_UI_SCREENSHOTS.get(role,()):
        if not screenshot_ok(evidence_root/filename):
            problems.append("missing_native_ui_"+filename)
    # Every Knight, Lord and King player must ACTUALLY open the native
    # live-Hero household inquiry, proving the menu can be reached.
    if role in ROLE_UI_SCREENSHOTS:
        hits=[x for x in group
              if x.get("context")=="cu_native_household_display_opened"]
        if not hits or not all((
            detail(hits[-1]).get("result")=="OPENED",
            detail(hits[-1]).get("role")==role,
            detail(hits[-1]).get("native_read_only")=="true")):
            problems.append("native_household_menu_not_opened")
    if any(x.get("context")=="exception_detected" for x in group):
        problems.append("native_session_exception_needs_review")
    if role=="King":
        # The post-reload officer IDs must match the exact native
        # commissioning and governor-action receipts from this session.
        commission=next((detail(x) for x in reversed(group)
            if x.get("context")=="cu_royal_court_native_commission"
            and verified(x,"cu_royal_court_native_commission")),None)
        governor=next((detail(x) for x in reversed(group)
            if x.get("context")=="cu_king_royal_minister_native_governor"
            and verified(x,"cu_king_royal_minister_native_governor")),None)
        expected={
            "royal_seat_id_after_reload":"native_court_seat",
            "marshal_hero_id_after_reload":"actual_marshal",
            "royal_minister_hero_id_after_reload":"actual_minister_governor",
            "kingsguard_hero_id_after_reload":"actual_kingsguard_hero"}
        if (commission is None or governor is None or
            not all(evidence.get(field) and
                evidence.get(field)==commission.get(key)
                for field,key in expected.items()) or
            governor.get("minister")!=commission.get("actual_minister_governor") or
            governor.get("settlement")!=commission.get("native_court_seat") or
            any(evidence.get(flag) is not True for flag in (
                "minister_native_governor_after_reload",
                "marshal_in_main_party_after_reload",
                "kingsguard_in_main_party_after_reload",
                "kingsguard_native_equipment_observed",
                "kingsguard_entered_native_combat",
                "kingsguard_post_battle_identity_confirmed"))):
            problems.append("king_native_officers_combat_or_reload_unverified")
        if not screenshot_ok(evidence_root/"king_kingsguard_battle.png"):
            problems.append("missing_kingsguard_native_battle_screenshot")
    if role=="Knight":
        # An unresolved younger-sibling fake clan assignment is never
        # accepted as a proven Knight origin.
        if any("native_same_clan_kinship_not_implemented" in
               (x.get("detail") or "") for x in group):
            problems.append("native_sibling_clan_join_blocked")
    if role=="Soldier" and any(
        x.get("context")=="cu_soldier_native_enlistment_pending"
        for x in group):
        # A later verified enlistment can supersede pending, but never
        # a mere request or source/model simulation.
        matches=[x for x in group if x.get("context")=="cu_sas_service_observed"]
        if not matches or not verified(matches[-1],"cu_sas_service_observed"):
            problems.append("external_native_enlistment_not_proven")
    return {"role":role,"result":"PASS" if not problems else "BLOCKED",
            "session_id":session,"missing":problems}

def audit_all(rows,evidence_root):
    findings=[audit_role(role,rows,evidence_root) for role in ROLES]
    # Seven starting paths require seven different native campaign sessions.
    ids=Counter(x.get("session_id") for x in findings
                if x["result"]=="PASS")
    for finding in findings:
        sid=finding.get("session_id")
        if sid and ids[sid]>1:
            finding["result"]="BLOCKED"
            finding["missing"].append("session_reused_between_starting_paths")
    return findings

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--events",type=pathlib.Path,required=True)
    parser.add_argument("--evidence-dir",type=pathlib.Path,required=True)
    parser.add_argument("--report",type=pathlib.Path)
    args=parser.parse_args(argv)
    rows,issue=read_events(args.events)
    findings=audit_all(rows,args.evidence_dir)
    if issue:
        for finding in findings: finding["missing"].append(issue)
    summary={"schema":"CU_SEVEN_NATIVE_GAME_ACCEPTANCE_1",
             "source_build_simulation_is_not_live_proof":True,
             "native_game_executed_by_this_script":False,
             "roles":findings,
             "accepted_roles":sum(x["result"]=="PASS" for x in findings),
             "all_seven_live_accepted":all(x["result"]=="PASS"
                for x in findings)}
    text=json.dumps(summary,indent=2)
    if args.report:
        args.report.parent.mkdir(parents=True,exist_ok=True)
        args.report.write_text(text+"\n",encoding="utf-8")
    print(text)
    return 0 if summary["all_seven_live_accepted"] else 2

if __name__=="__main__":
    sys.exit(main())
