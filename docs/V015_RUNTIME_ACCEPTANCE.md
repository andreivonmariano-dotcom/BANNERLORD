# v0.15.0 native settings, Gauntlet panel and game runtime acceptance

**Game target:** Bannerlord PC v1.5.7. **Status:** Gate A source, Gate B once the exact candidate passes Windows CI. No Windows game instance is available in the hosted code runner, so no genuine Gate C/D/E claims have been made.

## Q1: Full settings coverage

The catalog comes directly from public `Settings` fields and each parser's numeric clamp. All **24 booleans** and **40 integers** are visible in nine groups. Each category links to an individual setting page with its current value, supported bounds, and active Enable/Disable or bounded adjustment controls.

Experimental flags use an explicit setting detail page, then a separate Enable action. Existing defaults remain unchanged, including disabled autonomous NPC orders and physical convoys.

## Q2: Experimental Gauntlet panel

Open **CU: Configure Calradia Unified** from a town or castle. Choose **EXPERIMENTAL: Open native tabbed slider panel**. The panel uses a separate `ScreenBase`, `GauntletLayer`, ViewModel and local XML prefab. Three tabs switch between core/supply, kingdom/economy and castles/estates. Next/Previous cycles through each setting, and a native numeric slider exposes its bounded value. The Save action commits the value only after user selection. The default category menus remain intact even if the experimental panel fails to open.

This UI **has not been rendered or exercised in Bannerlord**. An XML parse and C# compile are not a proof that slider dragging, focus, positioning, gamepad controls or screen closing work. The user must test it explicitly on v1.5.7 before relying on it.

## Q3: Controlled v1.5.7 runtime evidence plan

### Preflight, offline

From the repo root on a Windows machine containing the game, run:

```powershell
pwsh ./scripts/Run-CU-Runtime-Preflight.ps1 -GameRoot "C:\Path\To\Mount & Blade II Bannerlord" -OutJson "BuildResults\cu-runtime-preflight.json"
```

Preflight checks the actual installed module ID/version, DLL, prefab, official CampaignSystem reference assembly and hashes. It **does not launch Bannerlord** and explicitly records game verification as false.

### First game session

1. Record the exact installed game build from the launcher and screenshots. Start the game with the official modules and only the Calradia Unified candidate.
2. Start a disposable new campaign, enter a town. Open **CU: Configure Calradia Unified** and verify all nine categories open and their settings display.
3. Change a harmless read-only/report flag. Visit the detail page again and confirm its displayed current value. Change a numeric warning threshold; exit and return.
4. Record a screenshot before and after opening the separate experimental Gauntlet panel. Test tab changes, next/previous setting, slider dragging, Save, bool toggle, Close and reopening. Record mouse/controller behavior, any overlap or missing sprites.
5. Examine `%LOCALAPPDATA%\CalradiaUnified\calradia-user-config.ini` and verify values were committed. Record a screenshot of settings and the profile file. Exit Bannerlord.
6. Relaunch the **same save**, inspect current settings and confirm persistence. Record the exact active save name and before/after state outside the diagnostic ZIP. These facts cannot be inferred solely from `SyncData` callbacks.
7. Test a separate disposable save with one experimental option enabled only after the baseline passes. Observe campaign daily ticks, town/castle departure, mission entry and exit, siege guards and garrison roster invariants. Report exceptions and outcome.

### Collect and analyze evidence

```powershell
pwsh ./scripts/Export-CU-Diagnostics.ps1 -GameDir "C:\Path\To\Mount & Blade II Bannerlord" -OutZip "BuildResults\cu-runtime.zip"
python ./scripts/validate_runtime_evidence.py "BuildResults\cu-runtime.zip"
python ./scripts/analyze_diagnostics.py "BuildResults\cu-runtime.zip"
```

The validator reads live and rotated `events.csv`, groups observed callbacks by session and reports menu opens, setting changes and screen push requests. It never calls a save/load pass or successful screen render without separate human evidence.

**Gate C:** live v1.5.7 startup proof, no startup exceptions and menu screenshots. **Gate D:** observed settings and gameplay. **Gate E:** same-save exit/relaunch, persistence and scenario regression, with logs. The compiled module alone only satisfies Gate B.
