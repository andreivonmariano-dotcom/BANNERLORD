#!/usr/bin/env python3
"""Summarize Calradia Unified user-exported finance and exception data."""
import argparse
import collections
import csv
import io
import json
import pathlib
import statistics
import tempfile
import zipfile

def read_sources(input_path):
    p = pathlib.Path(input_path)
    if p.is_dir():
        return {str(f.relative_to(p)).replace("\\", "/"): f.read_text(
            encoding="utf-8-sig", errors="replace") for f in p.rglob("*")
            if f.is_file() and f.stat().st_size < 12_000_000 and f.suffix.lower() != ".dmp"}
    with zipfile.ZipFile(p) as z:
        return {n: z.read(n).decode("utf-8-sig", errors="replace")
            for n in z.namelist() if not n.endswith("/")
            and z.getinfo(n).file_size < 12_000_000 and not n.lower().endswith(".dmp")}

def find(files, name):
    for path, text in sorted(files.items()):
        if path == name or path.endswith("/" + name):
            return text
    return None

def csv_records(files, name):
    """Read live and rotated journals, retaining each file's own CSV header."""
    paths = [path for path in files if path == name or path.endswith("/" + name)
             or path == name + ".previous" or path.endswith("/" + name + ".previous")]
    paths.sort(key=lambda path: (path.rsplit("/", 1)[0] if "/" in path else "",
                                 not path.endswith(".previous"), path))
    rows = []
    for path in paths:
        if files[path].strip():
            rows.extend(csv.DictReader(io.StringIO(files[path])))
    return rows, paths

def integer(value):
    try:
        return int(value)
    except (ValueError, TypeError):
        return None

def analyze(files):
    result = {"schema": "CU-ANALYSIS-1", "finance": {},
        "exceptions": {}, "game_engine_log_files": [],
        "cautions": ["Gold change contains trades and quest rewards, not just tax.",
                     "Income forecast is an estimate, not native Bannerlord tax.",
                     "No exception record does not exclude a native engine crash.",
                     "Session day indices restart per launch and cannot establish continuous campaign days."]}
    rows, finance_files = csv_records(files, "finance.csv")
    if finance_files:
        castles = collections.defaultdict(list)
        for item in rows:
            if item.get("record_type") == "castle":
                castles[item.get("settlement_id", "unknown")].append(item)
        details = {}
        for name, group in castles.items():
            wages = [integer(i.get("garrison_wages")) for i in group]
            models = [integer(i.get("estimated_net")) for i in group]
            wages = [n for n in wages if n is not None]
            models = [n for n in models if n is not None]
            details[name] = {
                "samples": len(group),
                "mean_garrison_wages": round(statistics.mean(wages), 1) if wages else None,
                "maximum_garrison_wages": max(wages) if wages else None,
                "modeled_deficit_days": sum(x < 0 for x in models),
                "mean_estimated_daily_balance": round(statistics.mean(models), 1) if models else None}
        summaries = [r for r in rows if r.get("record_type") == "clan_summary"]
        observed = [integer(r.get("change_in_player_gold")) for r in summaries]
        observed = [x for x in observed if x is not None]
        sessions = collections.defaultdict(list)
        for row in summaries:
            sessions[row.get("session_id") or "unknown"].append(row)
        coverage = {}
        for session, group in sorted(sessions.items()):
            days = sorted({day for row in group
                           if (day := integer(row.get("session_day"))) is not None})
            gold = [integer(row.get("change_in_player_gold")) for row in group]
            gold = [value for value in gold if value is not None]
            coverage[session] = {
                "recorded_session_days": len(days),
                "first_session_day": days[0] if days else None,
                "last_session_day": days[-1] if days else None,
                "observed_gold_change_samples": len(gold),
                "negative_gold_change_samples": sum(value < 0 for value in gold),
                "reported_5000_10000_deficit_band_samples":
                    sum(-10000 <= value <= -5000 for value in gold)}
        result["finance"] = {
            "source_csv_files": finance_files,
            "castle_rows": sum(map(len, castles.values())),
            "summary_rows": len(summaries),
            "castles": details,
            "session_coverage": coverage,
            "observed_gold_change_samples": len(observed),
            "negative_gold_change_samples": sum(value < 0 for value in observed),
            "reported_5000_10000_deficit_band_samples":
                sum(-10000 <= value <= -5000 for value in observed),
            "mean_observed_player_gold_change": round(statistics.mean(observed), 1) if observed else None,
            "worst_observed_player_gold_change": min(observed) if observed else None}
    else:
        result["finance"]["status"] = "No finance.csv found"
    rows, exception_files = csv_records(files, "exceptions.csv")
    if exception_files:
        counts = collections.Counter((r.get("context", ""), r.get("exception_type", "")) for r in rows)
        result["exceptions"] = {"count": len(rows),
            "source_csv_files": exception_files,
            "groups": [{"context": context, "exception": error, "count": count}
                       for (context, error), count in counts.most_common(25)]}
    else:
        result["exceptions"] = {"count": 0, "note": "No managed exception CSV found"}
    result["game_engine_log_files"] = sorted(n for n in files if
        n.startswith("engine_logs/") or n.startswith("engine_crashes/"))
    return result

def self_test():
    finance = (
        "utc,session_id,record_type,session_day,settlement_id,garrison_wages,estimated_net,change_in_player_gold\n"
        "now,s,castle,1,castle_x,7000,-5000,\n"
        "now,s,clan_summary,1,,,,\n"
        "later,s,clan_summary,2,,,,-3000\n")
    previous_finance = (
        "utc,session_id,record_type,session_day,settlement_id,garrison_wages,estimated_net,change_in_player_gold\n"
        "older,s2,clan_summary,1,,,,-7000\n")
    errors = "utc,session_id,context,exception_type,message,stack_trace\nnow,s,civic,System.Exception,Oops,Trace\n"
    previous_errors = "utc,session_id,context,exception_type,message,stack_trace\nolder,s2,load,System.Exception,Old,Stack\n"
    files = {"mod/finance.csv": finance,
             "mod/finance.csv.previous": previous_finance,
             "mod/exceptions.csv": errors,
             "mod/exceptions.csv.previous": previous_errors}
    d = analyze(files)
    assert d["finance"]["castles"]["castle_x"]["modeled_deficit_days"] == 1
    assert d["finance"]["observed_gold_change_samples"] == 2
    assert d["finance"]["reported_5000_10000_deficit_band_samples"] == 1
    assert d["finance"]["worst_observed_player_gold_change"] == -7000
    assert d["finance"]["session_coverage"]["s2"]["recorded_session_days"] == 1
    assert d["exceptions"]["count"] == 2
    with tempfile.TemporaryDirectory() as root:
        zipname = pathlib.Path(root) / "example.zip"
        with zipfile.ZipFile(zipname, "w") as zipfile_out:
            zipfile_out.writestr("mod/finance.csv", finance)
            zipfile_out.writestr("mod/exceptions.csv", errors)
            zipfile_out.writestr("mod/finance.csv.previous", previous_finance)
            zipfile_out.writestr("mod/exceptions.csv.previous", previous_errors)
        report = analyze(read_sources(zipname))
        assert report["finance"]["castle_rows"] == 1
        assert report["finance"]["observed_gold_change_samples"] == 2
        assert report["exceptions"]["count"] == 2
    print("PASS diagnostic CSV, exception, ZIP reader self-tests")

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("input", nargs="?")
    p.add_argument("--out")
    p.add_argument("--self-test", action="store_true")
    a = p.parse_args()
    if a.self_test:
        self_test()
        return
    if not a.input:
        p.error("Provide a diagnostic ZIP or expanded folder")
    report = json.dumps(analyze(read_sources(a.input)), indent=2)
    if a.out:
        pathlib.Path(a.out).write_text(report + "\n", encoding="utf-8")
    print(report)
if __name__ == "__main__":
    main()
