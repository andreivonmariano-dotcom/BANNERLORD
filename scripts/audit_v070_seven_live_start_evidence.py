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
import sys

ROLES=("Wanderer","Outlaw","Mercenary","Soldier","Knight","Lord","King")
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
 "cu_sas_service_observed":("result=PASS","enlisted=True"),
 "cu_knight_oath_start":("result=PASS","native_personal_oath"),
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
     "result=native_royal_hero_council_commissioned",),
 "cu_king_native_council_meeting":(
     "result=real_royal_hero_council_convened",),
}

def detail(row):
    return {x.partition("=")[0]:x.partition("=")[2]
            for x in (row.get("detail") or "").split("|")
            if "=" in x}

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
    for session,group in sessions.items():
        passed=[row for row in group
                if row.get("context")=="sandbox_start_contract"
                and detail(row).get("role")==role
                and detail(row).get("result")=="PASS"]
        if passed:
            candidates.append((session,group))
    problems=[]
    if not candidates:
        return {"role":role,"result":"BLOCKED",
                "missing":["same_session_native_start_contract_PASS"]}
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
        allowed=GOOD.get(context,("result=PASS",))
        if not any(any(mark in (x.get("detail") or "")
                       for mark in allowed) for x in matches):
            problems.append("unverified_"+context)
    proof=evidence_root/(role.lower()+".json")
    screenshot=evidence_root/(role.lower()+".png")
    if not proof.is_file():
        problems.append("missing_post_reload_native_identity_proof")
    else:
        try:
            evidence=json.loads(proof.read_text(encoding="utf-8"))
            if (evidence.get("role")!=role or
                evidence.get("start_session_id")!=session or
                not evidence.get("native_reloaded_after_quit") or
                not evidence.get("same_player_hero_id") or
                not evidence.get("same_clan_id") or
                not evidence.get("observed_native_role_after_reload") or
                not evidence.get("verified_family_after_reload") or
                not evidence.get("verified_service_or_authority_after_reload")):
                problems.append("incomplete_native_reload_observation")
        except (ValueError,OSError):
            problems.append("invalid_post_reload_proof")
    if not screenshot.is_file() or screenshot.stat().st_size<1024:
        problems.append("missing_live_native_ui_screenshot")
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
        if not any(x.get("context")=="cu_sas_service_observed" and
                   "enlisted=True" in (x.get("detail") or "")
                   for x in group):
            problems.append("external_native_enlistment_not_proven")
    return {"role":role,"result":"PASS" if not problems else "BLOCKED",
            "session_id":session,"missing":problems}

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--events",type=pathlib.Path,required=True)
    parser.add_argument("--evidence-dir",type=pathlib.Path,required=True)
    parser.add_argument("--report",type=pathlib.Path)
    args=parser.parse_args(argv)
    rows,issue=read_events(args.events)
    findings=[audit_role(role,rows,args.evidence_dir) for role in ROLES]
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
