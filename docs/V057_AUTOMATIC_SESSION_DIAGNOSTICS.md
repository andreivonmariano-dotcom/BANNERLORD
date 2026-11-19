# v0.57 | Automatic local session diagnostics for Calradia Unified

Target: Bannerlord PC 1.5.7.125802. Built on v0.56 creator fixes (the Age→Stats crash remains under live investigation).

## User goal
**Every time Bannerlord closes normally, Calradia Unified automatically writes a report ZIP** with its current session's events, errors and financial telemetry. No manual PowerShell collector should be needed for a normal game exit. When Bannerlord terminates hard, the mod cannot execute an exit handler, so it **writes the evidence continuously** and packages the interrupted session at the next successful startup.

## Where to find the report
- ZIPs: `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Reports\CU-Session-<ID>-CLOSED.zip`
- Recoveries: `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Reports\CU-Session-<ID>-INTERRUPTED.zip`
- Latest saved report path: `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\latest-report.txt`
- Raw per-session journals: `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Sessions\<ID>\`

Paste `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Reports` into File Explorer to review and upload the relevant ZIP. Reports are **never transmitted automatically**.

## How the data is recorded
- The existing finance, events and exceptions CSVs remain at their old paths for compatibility with manual collectors. Every log line is simultaneously flushed into an isolated `Sessions\<session-id>\` copy.
- Module load, session start, game start, game end, every existing CU screen/creator event, managed exceptions, and finance telemetry use one session ID.
- A read-only heartbeat runs every 45 seconds of valid game frame time and records whether a campaign and player exist, plus the player's level and age.
- Normal exit uses `MBSubModuleBase.OnSubModuleUnloaded`, plus `AppDomain.ProcessExit` and `DomainUnload` fallbacks. Only the first successful finish writes the session ZIP.
- Each session has an `active.marker`. On restart, a leftover marker identifies an interrupted session. The mod archives it as `-INTERRUPTED.zip`, preserving **the prior process's** DLL hash and metadata. It adds recovery time and up to four small RGL log tails captured during the following startup.
- Reports include DLL SHA-256, detected game root, CLR/OS and loaded TaleWorlds/War Sails assemblies. They intentionally **exclude** game saves, native memory dumps, raw screenshots, and private system files.
- Rolling CSV caps prevent unbounded disk use, and the report directory keeps the most recent 20 ZIPs.

## Your installed path
The game root is first located from the actually loaded Calradia Unified DLL or game executable. The user's current Steam location is a fallback:

`D:\Steam\steamapps\common\Mount & Blade II Bannerlord`

That location is not required for other installations. The game can continue even if it cannot be found.

## Controls
By default:
```ini
EnableDiagnostics=true
AutoSessionReports=true
```

To disable the automatic ZIPs but keep the existing diagnostic CSV logging, add this line to `%LOCALAPPDATA%\CalradiaUnified\calradia-user-config.ini`:
```ini
AutoSessionReports=false
```

Setting `EnableDiagnostics=false` disables the journal and automatic reports.

## Limitations and verification
- A hard native process exit cannot run managed code. The **next launch** packages the already-flushed journal as INTERRUPTED; its ZIP does not itself identify the native faulting instruction. Windows Error Reporting or the standalone collector may still be needed for a native stack.
- This is mod-specific telemetry, not a recording of every mouse movement, texture or native Bannerlord command. The whole game's private save files are not copied.
- Reports may contain character names, in-game choices, and paths from exception messages. Inspect before sharing.
- CI verifies policy tests, ZIP contents, recovery and installation. A real Windows game exit and abrupt engine crash still need confirmation before declaring the feature game-tested.
