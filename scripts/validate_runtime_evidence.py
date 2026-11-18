#!/usr/bin/env python3
"""Summarize user-supplied game-session callbacks without inventing a game-test pass."""
import argparse
import collections
import io
import csv
import json
from analyze_diagnostics import csv_records, read_sources

EVENTS = (
    "module_loaded", "campaign_session_launched", "settings_menu_registered",
    "settings_menu_open", "settings_menu_close", "native_settings_change",
    "gauntlet_settings_push", "gauntlet_settings_change",
    "gauntlet_settings_close", "sync_data_invoked", "civic_sync_data_invoked",
    "exception_detected",
)

def inspect(files):
    events, sources = csv_records(files, "events.csv")
    sessions = collections.defaultdict(lambda: collections.Counter())
    for row in events:
        sid = row.get("session_id", "")
        name = row.get("context", "")
        if sid and name:
            sessions[sid][name] += 1
    report = {
        "schema": "CU-RUNTIME-EVIDENCE-1",
        "input_journals": sources,
        "sessions": [],
        "observed_any_game_callbacks": False,
        "observed_menu_open": False,
        "observed_settings_change": False,
        "observed_gauntlet_screen_request": False,
        "verified_v157_game_startup": False,
        "verified_gauntlet_visual_render": False,
        "verified_save_reload": False,
        "verified_feature_safety": False,
        "highest_automated_gate": "B_NOT_EVALUATED_FROM_THIS_INPUT",
        "cautions": [
            "Log events are evidence of executed callbacks, not a screenshot or proof that a screen rendered.",
            "A save/load callback alone never proves restored campaign state.",
            "The exact v1.5.7 game version needs independent launcher or assembly evidence.",
            "No gameplay regression status can be inferred without a controlled in-game test.",
        ],
    }
    for sid, counts in sorted(sessions.items()):
        data = {"session_id": sid,
            "events": {name: counts[name] for name in EVENTS if counts[name]},
            "callback_chain_seen": all(counts[k] > 0 for k in
                ("module_loaded","campaign_session_launched","settings_menu_registered"))}
        report["sessions"].append(data)
        report["observed_any_game_callbacks"] |= data["callback_chain_seen"]
        report["observed_menu_open"] |= counts["settings_menu_open"] > 0
        report["observed_settings_change"] |= (
            counts["native_settings_change"] > 0 or
            counts["gauntlet_settings_change"] > 0)
        report["observed_gauntlet_screen_request"] |= (
            counts["gauntlet_settings_push"] > 0)
    return report

def self_test():
    header = "utc,session_id,context,detail\n"
    files = {
        "mod/events.csv.previous": header +
            '2026-11-18T00:00:00Z,A,module_loaded,version=0.15.0\n' +
            '2026-11-18T00:01:00Z,A,campaign_session_launched,version=0.15.0\n',
        "mod/events.csv": header +
            '2026-11-18T00:02:00Z,A,settings_menu_registered,definitions=64\n' +
            '2026-11-18T00:03:00Z,A,settings_menu_open,source=castle\n' +
            '2026-11-18T00:04:00Z,A,native_settings_change,SupplyWarningDays=4\n' +
            '2026-11-18T00:05:00Z,A,gauntlet_settings_push,screen_requested\n',
    }
    r = inspect(files)
    assert len(r["sessions"]) == 1
    assert r["observed_any_game_callbacks"]
    assert r["observed_menu_open"] and r["observed_settings_change"]
    assert r["observed_gauntlet_screen_request"]
    assert not r["verified_v157_game_startup"]
    assert not r["verified_gauntlet_visual_render"]
    assert not r["verified_save_reload"] and not r["verified_feature_safety"]
    assert not inspect({})["observed_menu_open"]
    assert not inspect({"mod/events.csv": header})["observed_any_game_callbacks"]
    print("PASS: rotated event journals, executed callbacks, and conservative gate assignment")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("evidence", nargs="?", help="Existing diagnostics directory or exported ZIP")
    p.add_argument("--self-test", action="store_true")
    args = p.parse_args()
    if args.self_test:
        self_test()
    elif args.evidence:
        print(json.dumps(inspect(read_sources(args.evidence)), indent=2))
    else:
        p.error("Provide a diagnostics ZIP/directory or --self-test")
