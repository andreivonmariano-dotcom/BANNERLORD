# Calradia Unified v0.12.0: finance and crash diagnostic exports

## Player intent

The user will capture a real Bannerlord campaign and upload its diagnostic ZIP into this chat. Future budget changes, wage decisions, convoy fixes and crash diagnosis should reference the **observed evidence**, its module and game versions, and the relevant GitHub commit. The historic −5,000 to −10,000 daily vassal deficit remains an unverified report until it is reproduced.

## Where data lives

Read-only mod diagnostics are written under:
- \`%LOCALAPPDATA%\CalradiaUnified\Diagnostics\finance.csv\`
- \`%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv\`
- \`%LOCALAPPDATA%\CalradiaUnified\Diagnostics\exceptions.csv\`
- Previous rotated files are suffixed \`.previous\`. Each journal is limited to roughly 8 MB before rotation.

The in-game menu creates timestamped subfolders under \`%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Exports\`. The Windows exporter creates a ZIP on the user's Desktop containing the mod diagnostics and available external crash evidence.

## Finance schema

The \`finance.csv\` fields are UTC, session ID, record type, **session-local** day index, castle ID, prosperity, healthy bound-village hearths, raided bound-village count, actual existing garrison troop count, actual existing garrison wage, aggregate player-clan lord-party wages, player-party troops, observed player gold, change in player gold between samples, **estimated** estate income, suggested garrison wage limit, **estimated** net, depot grain, shipment counts and notes.

Blank fields mean **unknown or not captured**. They must not be interpreted as zero. The current v0.12.0 castle diagnostics leave depot and shipment fields blank because the diagnostic behavior does not read the separate saved civic ledger.

**Observed player-gold change is not native daily tax revenue.** Rewards, trades, loot, wages and other actions are mixed in. The native clan-income breakdown still requires a screenshot or separate engine API integration. Forecast amounts come from \`FeudalBudgetPolicy\`, not native tax data.

## Crash coverage and limits

- Managed exceptions thrown through our guarded campaign code appear in \`exceptions.csv\` with context and stack.
- An unhandled managed exception or unobserved task failure can be logged by the installed process handler. This is best-effort and does not suppress the failure.
- A native access violation, forced process termination or crash before Calradia Unified loads may generate no mod journal. TaleWorlds \`%PROGRAMDATA%\Mount and Blade II Bannerlord\logs\rgl_log*\` and \`crashes\` directories and the Windows Application event log are external sources.
- The PowerShell exporter reads those sources but never changes engine files or saves. It includes text/XML/JSON reports subject to a size cap and collects recent Application Error/Windows Error Reporting events.
- Raw \`.dmp\` files can expose process memory and are excluded by default. Only \`-IncludeDumps\` explicitly adds them, and only subject to size limits. Check private details in all files before uploading.
- Event absence does not prove that a crash did not occur. A stack trace may implicate a mod without proving causality.

## Windows export

From the Bannerlord game root, after installing the compiled module:

\`\`\`powershell
powershell.exe -ExecutionPolicy Bypass -File ".\Modules\CalradiaUnified\ModuleData\Export-CU-Diagnostics.ps1" -GameDir "."
\`\`\`

The script creates \`CalradiaUnified-Diagnostics-YYYYMMDDTHHMMSSZ.zip\` on the Desktop, and copies only logs and diagnostic evidence. Optional \`-RecentHours 168\` expands the external crash-search period to one week.

## How to analyze in ChatGPT

Upload the ZIP directly. For finance, inspect at least seven daily records, separate castle and clan-summary rows, check differences between estimated and observed figures, and identify extraordinary outflows. For crashes, read the exception context/type/stack and corroborate its timestamp with \`rgl_log_errors\`, TaleWorlds crash text and Windows errors. Compare the DLL hash from the ZIP manifest to the exact GitHub Actions artifact before proposing a code change.

\`python scripts/analyze_diagnostics.py <diagnostic-zip>\` produces a machine-readable summary with distinct observed versus modeled fields. It does not infer causality automatically.

## Release acceptance

Compilation, analyzer tests and artificial exception tests are Gate B evidence only. M0 game startup, real financial capture, post-crash capture and save/reload remain open until tested with a live v1.5.7 installation.
