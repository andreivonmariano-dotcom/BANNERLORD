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
                     "No exception record does not exclude a native engine crash."]}
    raw = find(files, "finance.csv")
    if raw:
        rows = list(csv.DictReader(io.StringIO(raw)))
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
        result["finance"] = {
            "castle_rows": sum(map(len, castles.values())),
            "summary_rows": len(summaries),
            "castles": details,
            "mean_observed_player_gold_change": round(statistics.mean(observed), 1) if observed else None,
            "worst_observed_player_gold_change": min(observed) if observed else None}
    else:
        result["finance"]["status"] = "No finance.csv found"
    errs = find(files, "exceptions.csv")
    if errs:
        rows = list(csv.DictReader(io.StringIO(errs)))
        counts = collections.Counter((r.get("context", ""), r.get("exception_type", "")) for r in rows)
        result["exceptions"] = {"count": len(rows),
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
        "later,s,clan_summary,2,,,-3000\n")
    errors = "utc,session_id,context,exception_type,message,stack_trace\nnow,s,civic,System.Exception,Oops,Trace\n"
    d = analyze({"mod/finance.csv": finance, "mod/exceptions.csv": errors})
    assert d["finance"]["castles"]["castle_x"]["modeled_deficit_days"] == 1
    assert d["exceptions"]["count"] == 1
    with tempfile.TemporaryDirectory() as root:
        zipname = pathlib.Path(root) / "example.zip"
        with zipfile.ZipFile(zipname, "w") as zipfile_out:
            zipfile_out.writestr("mod/finance.csv", finance)
            zipfile_out.writestr("mod/exceptions.csv", errors)
        assert analyze(read_sources(zipname))["finance"]["castle_rows"] == 1
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
