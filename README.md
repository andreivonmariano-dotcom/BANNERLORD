# Calradia Unified v0.12.0: exportable finance and crash diagnostics

**Target:** Mount & Blade II: Bannerlord PC v1.5.7. **Verified build:** [run 44236922635](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44236922635). **Product:** one `Modules/CalradiaUnified` folder with no installer or historical third-party DLLs.

## New in v0.12.0

**Daily real-game financial records.** Read-only `DiagnosticBehavior` writes daily clan gold snapshots, gold changes since previous samples, actual player-clan lord-party wages, actual castle garrison wages and troop counts, castle prosperity, healthy village hearths, raided village counts, and **separately labelled modeled** income projections. This is a diagnostic layer. It does **not** read the exact native daily fief-tax ledger or prove that the fiscal changes cured your original `−5,000 to −10,000` starting deficit.

**Managed crash journal.** Exceptions caught in civic menus, campaign ticks, NPC movement and campaign setup write context, exception type, message and stack trace to `exceptions.csv`. The module registers a limited unhandled-managed-exception handler. A hard native crash may prevent it from running.

**Engine crash collector.** The read-only `ModuleData/Export-CU-Diagnostics.ps1` PowerShell utility collects available recent TaleWorlds `rgl_log*` files and crash-report text from `%PROGRAMDATA%\Mount and Blade II Bannerlord`, alongside Windows Application Error events. Saves and memory dumps are excluded by default. Nothing is uploaded automatically.

**In-game export:** A town/castle menu action, `CU: Export campaign economy and crash diagnostics`, copies the mod's own log records into `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\Exports`. The Windows script packages mod and engine evidence into a single shareable ZIP. `EnableDiagnostics=true` defaults to on, and can be disabled in the module INI.

## Export after a session or crash

1. Run the current compiled candidate and record your castle with two villages for 7 to 30 game days. The new finance CSV is written once per campaign day.
2. From the game menu, choose **CU: Export campaign economy and crash diagnostics** for the mod-only snapshot. For a crash that ends the game, skip the menu.
3. Run the PowerShell script included in your installed module folder, `Modules\CalradiaUnified\ModuleData\Export-CU-Diagnostics.ps1`, to create a Desktop ZIP:
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File ".\Modules\CalradiaUnified\ModuleData\Export-CU-Diagnostics.ps1" -GameDir "."
   ```
   Run this from the Bannerlord installation root. By default it searches the previous 72 hours of engine logs and crash reports. The `-IncludeDumps` switch adds raw dumps that can contain private memory and is **not** recommended for normal reports.
4. Inspect the ZIP for private text, then upload it directly to this conversation. I can compare observed wages, gold movements and exception stacks with our simulator and trace them to the mod's source revision.

You can analyze an exported ZIP locally with:

```powershell
python scripts/analyze_diagnostics.py CalradiaUnified-Diagnostics-YYYYMMDD.zip
```

**Read [the diagnostic export handbook](docs/DIAGNOSTICS_v012.md) for schemas, caveats and evidence handling.**

## Existing v0.11.0 gameplay systems

Calradia Unified retains guarded depot transfers, physical convoy candidate code, local culture recruits, garrison training, budgeted castle wage limits, village production and abstract surplus sales. All gameplay-modifying flags remain **disabled by default** until real v1.5.7 in-game acceptance tests pass.

## Build and release verification

GitHub Actions runs structural source tests, feature-registry checks, independent C# policy tests, diagnostic analyzer tests, the seeded headless simulator, and a Windows v1.5.7 reference compilation. The v0.12.0 Windows build passed. A `COMPILED_UNTESTED` archive is not a certified game-tested release.

- [Build workflow](.github/workflows/bannerlord-build.yml)
- [Game test procedure](docs/M0_TEST_PROCEDURE.md)
- [Fiscal model](docs/FEUDAL_BUDGET_v011.md)
- [Feature registry](Data/Feature_Registry.csv)
- [Original 78-mod coverage](docs/78_MOD_COVERAGE.md)
- [Starting vassal deficit regression](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/32)

## First verified hosted diagnostics build

**GitHub Actions run:** [44236922635](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44236922635), source revision `23b366dc4ac21504bb08dbf21f3bcf7413dd69a8`.

- **183 executable C# policy/diagnostic checks passed** including filesystem CSV writing, managed exception capture, manual snapshot export and diagnostic opt-out.
- The Python diagnostic ZIP reader's synthetic tests passed.
- The Windows PowerShell exporter parsed and executed on the hosted runner, producing a ZIP with `manifest.json`.
- **40 simulated campaigns / 14,600 modeled days / 303,098 model checks passed**.
- The v1.5.7-reference DLL compiled with **zero warnings and zero errors**.
- The compiled module ZIP passed CRC and one-DLL package checks, and contains `Modules/CalradiaUnified/ModuleData/Export-CU-Diagnostics.ps1`.
- Compiled DLL SHA-256: `35491baa6d17c8ba192c892839c8733c2220aa020a6d062ff5eb7a19eff0405e`.
- Inner module ZIP SHA-256: `b0834ffb7f49c56376a722cfbbcf5e623dd1be6a1625ec079a2c04542ddc490c`.

**Scope:** This is Gate B only. No actual Bannerlord play session, save/load, engine crash or game finance screen has been observed. The test ZIP created on the runner did not contain real Bannerlord crash evidence.
