#!/usr/bin/env python3
"""v057 self-contained local reporting contract (source/static only).
Actual game normal-exit and native hard-crash behavior remain user tests.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
journal=(src/"DiagnosticJournal.cs").read_text(encoding="utf-8")
reporter=(src/"DiagnosticSessionReporter.cs").read_text(encoding="utf-8")
summary=(src/"DiagnosticSummaryPolicy.cs").read_text(encoding="utf-8")
module=(src/"SubModule.cs").read_text(encoding="utf-8")
settings=(src/"Settings.cs").read_text(encoding="utf-8")
project=(src/"CalradiaUnified.csproj").read_text(encoding="utf-8")
tests=(root/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
for key in ("CurrentSessionId","Sessions","AppendOne(",
            "events.csv","exceptions.csv","finance.csv"):
    assert key in journal,key
assert 'private const long MaxCsvBytes = 8 * 1024 * 1024;' in journal
assert "16L*1024L*1024L" in journal
for key in (
    "ProcessExit+=","DomainUnload+=","OnSubModuleUnloaded",
    "RecoverInterruptedSessions()", "active.marker",
    "recovered.marker","completed.marker",
    "ZipFile.Open(partial,ZipArchiveMode.Create)","CompressionLevel.Fastest",
    '"CU-Session-"', "GetObjectTypeList<" if False else "WriteRuntimeInfo(",
    "native-rgl-tail-","MaxReports=20","MaxNativeLogBytes=512*1024",
    "latest-report.txt","D:\\\\Steam" if False else
        "Mount & Blade II Bannerlord",
    "module_dll_sha256=","No data is uploaded automatically."
):
    assert key in (reporter+module),key
assert 'AppDomain.CurrentDomain.ProcessExit+=' in reporter
assert 'protected override void OnSubModuleUnloaded()' in module
assert 'DiagnosticSessionReporter.Complete("submodule_unloaded")' in module
assert 'DiagnosticSessionReporter.Start();' in module
assert 'DiagnosticSessionReporter.Heartbeat(' in module
assert 'reportHeartbeatSeconds>=45f' in module
assert 'DiagnosticJournal.Event("campaign_closed"' in module
assert 'AutoSessionReports = true;' in settings
assert 'key == "AutoSessionReports"' in settings
assert 'Reference Include="System.IO.Compression"' in project
assert 'Reference Include="System.IO.Compression.FileSystem"' in project
assert "Environment.SpecialFolder.DesktopDirectory" in reporter
assert '"Calradia Unified Reports"' in reporter
assert 'OverrideDesktopDirectoryForTests' in reporter
assert 'missing Desktop falls back to local diagnostic folder' in tests
assert 'automatic reports use a dedicated Desktop folder' in tests
assert 'local latest report pointer refers to Desktop ZIP' in tests
assert '"READ_ME_FIRST.txt"' in reporter
assert '"LATEST-REPORT-SUMMARY.txt"' in reporter
assert 'DiagnosticSummaryPolicy.Build(session,status,interrupted)' in reporter
assert 'No managed exception recorded.' in summary
assert 'A native crash cause is NOT' in summary
assert 'interrupted report has readable summary in ZIP' in tests
assert 'Desktop latest summary opens without extracting ZIP' in tests
assert 'normal summary parses multiline quoted CSV and exception' in tests
assert 'FIRST REPORTED MANAGED STACK FRAME' in summary
assert 'FirstManagedStackFrame(string stack)' in summary
assert 'Source (filename only):' in summary
assert 'No parseable managed method in the saved' in summary
assert 'managed exception summary reports the first method' in tests
assert 'nested exception reports first inner frame with caveat' in tests
assert 'async state machine receives compiler-frame caveat' in tests
assert 'missing managed frame stays unknown' in tests
assert 'LAST RECORDED CHARACTER-CREATOR PAGE' in summary
assert 'RECORDED ERROR AREA (NOT A CRASH CAUSE)' in summary
assert 'Interruption is unclassified.' in summary
assert 'Later requested transition:' in summary
assert 'reports confirmed Age and requested All Stats separately' in tests
assert 'mixed UI and campaign errors remain separately classified' in tests
assert 'unhandled exception without subsystem context stays unknown' in tests
assert 'interrupted process without exception remains unclassified' in tests
for creator_source in ("CUNativeStatsLayer.cs","CUGuidedCreatorVM.cs",
                       "CUNativeCultureLayer.cs","CUNativeStateShell.cs",
                       "CUNativeHouseLayer.cs"):
    assert '"cu_creator_page_checkpoint"' in (src/creator_source).read_text(encoding="utf-8"),creator_source
assert "ZipFile.CreateFromDirectory(session" not in reporter
assert '.Where(x=>File.Exists(Path.Combine(x,"active.marker")))' in reporter
assert 'recovery is not starved by 26 newer completed folders' in tests
assert 'recovery excludes unexpected saves and dumps' in tests
assert 'unexpected-screenshot.png' in tests
assert 'DiagnosticSessionReporter.Start();' in tests
assert 'DiagnosticSessionReporter.Complete("unit_test_normal_exit")' in tests
assert '"CU-Session-20261118T100000_deadbeef-INTERRUPTED.zip"' in tests
assert "ZipFile.OpenRead(" in tests
# Do not upload, open URLs, take screenshots or read any save files.
for dangerous in ("HttpClient","WebClient","HttpWebRequest","UploadFile",
                  "UploadString","File.ReadAllBytes(save","*.sav",
                  "*.dmp","CreateDump","MiniDumpWriteDump"):
    assert dangerous not in reporter,dangerous
print("PASS: v057 continuous local logs, normal-exit ZIP, hard-crash recovery, bounded native log tails and no upload (STATIC ONLY)")
