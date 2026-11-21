# Calradia Unified: mandatory PowerShell install and playthrough test standard

## Current installer selection | v0.69f (21 November 2026)

The current **CI-verified development candidate** is **v0.69f**, based on source commit `eec666c252f468cd92475e8abc86731d16da3a4f`. It passed [Windows CI 44864139228](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44864139228): **907 executable C# policy checks**, 0 compiler warnings/errors, plus simulated campaign and creator checks. It has **not** been verified inside an installed Bannerlord game.

The legacy in-game `SubModule.xml` still says **v0.38.0**. That is *manifest metadata*, not the source-development version. The PowerShell installer now prints both separately and selects the exact pinned candidate using its DLL hash. Do **not** rename the ZIP or rewrite `SubModule.xml` to make it look like v0.69f.

- [Download the v0.69f compiled, game-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44864139228/artifacts/14690729491) from GitHub Actions (sign-in can be needed).
- Exact DLL SHA-256: `3a890f016bebe9b1a1a6941d09ef98922d479d744187727a812f3a65def83684`.
- Installer source pinned independently at [commit 42238c2](https://github.com/andreivonmariano-dotcom/BANNERLORD/blob/42238c276cea8c9e6519ea83c2f520663dcaf18f/scripts/Install-CU-From-Artifact.ps1).
- **List the candidate first, with no game path needed**: `-ShowLatest`. Install a downloaded artifact with `-LatestVerified`, which pins the exact expected manifest and DLL hash without manually copying the hash.

```powershell
$Installer = Join-Path $env:TEMP 'Install-CU-From-Artifact.ps1'
$Source = 'https://raw.githubusercontent.com/andreivonmariano-dotcom/BANNERLORD/42238c276cea8c9e6519ea83c2f520663dcaf18f/scripts/Install-CU-From-Artifact.ps1'
Invoke-WebRequest -UseBasicParsing -Uri $Source -OutFile $Installer
Set-ExecutionPolicy -Scope Process Bypass -Force

# Shows v0.69f, exact artifact page, Windows CI run, SHA-256 and legacy manifest
& $Installer -ShowLatest

# After downloading the exact Actions artifact ZIP, set its actual path.
$GameRoot = 'D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$Zip = Join-Path $env:USERPROFILE 'Downloads\CalradiaUnified-v0.38.0-compiled-untested.zip'
if (-not (Test-Path -LiteralPath $Zip -PathType Leaf)) {
    throw 'Download the pinned GitHub Actions artifact and set its actual ZIP filename.'
}

# The previous module is backed up. Saves are not touched.
& $Installer -GameRoot $GameRoot -ArtifactZip $Zip -LatestVerified -ReplaceExisting
```

After installation, fully close and reopen the game and launcher. Installation is file- and hash-verified only. It does **not** establish that Calradia Unified loads, displays correctly, or preserves real campaign saves. The earlier v0.29.0 instructions below are retained as **historical examples**, not the current release.


**Standing project decision:** Windows PowerShell 5.1 is the default player-facing method for installing the compiled Calradia Unified module, checking its exact identity, inspecting the latest campaign diagnostics, and collecting a private diagnostic ZIP. This replaces one-off chat-only install snippets. Follow it in every future BANNERLORD chat and every compiled test milestone.

**Game baseline:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Repository:** https://github.com/andreivonmariano-dotcom/BANNERLORD
**User's previously verified game root:** `D:\Steam\steamapps\common\Mount & Blade II Bannerlord` (confirm if the game is reinstalled).
**Authoritative game module:** `Modules\CalradiaUnified\`.
**Default Windows shell:** Windows PowerShell **5.1**, not PowerShell 7-only syntax. Paste entire multiline blocks at once.

## Two scripts to use every time

1. [`scripts/Install-CU-From-Artifact.ps1`](../scripts/Install-CU-From-Artifact.ps1) accepts the **exact downloaded ZIP**, verifies manifest/version and the **expected CI DLL SHA-256 before touching the existing module**, backs up the previous installation under the game root, copies the runtime module, and checks the installed DLL hash. ZIPs from GitHub Actions may contain a second module ZIP inside. This script accepts both direct and nested forms. It neither runs the game nor touches saves.
2. [`scripts/Test-CU-Playthrough.ps1`](../scripts/Test-CU-Playthrough.ps1) is **read-only**. It reports installed version, exact DLL hash, the latest accepted custom Sandbox start, requested versus created children, spouse creation, family verification, start contract, residence event and related exceptions. It filters the historical events log from the latest `sandbox_overhaul_accepted` timestamp to avoid treating previous campaigns as the current one. Optional `-Assert` rejects failed/incomplete contract evidence. A green log is **not** proof of a saved-game reload.

For crashes or deeper engine logs, the existing [`scripts/Export-CU-Diagnostics.ps1`](../scripts/Export-CU-Diagnostics.ps1) makes a private ZIP of local Calradia Unified and TaleWorlds logs. It does **not** collect saves or raw memory dumps by default. Review files before sharing.

## Required preparation for every build

Before writing installation commands in a future chat, use the connected GitHub repository to obtain all four values from the **same successful Windows workflow run**:

- The exact **module version**, for example `v0.28.0`.
- The specific **run's compiled-untested artifact URL**, not a generic or guessed ZIP.
- The **compiled DLL SHA-256** printed in its package verification step.
- The **source commit SHA containing the scripts**. Use an immutable SHA for the raw download URL, not an unreviewed floating branch. The workflow run's commit is a suitable source revision if it contains both scripts.

The artifact's name and module version are **not sufficient to identify a build**. Two different successful `v0.25.0` runs produced two different DLL hashes in the user's earlier tests. Never choose a ZIP only by latest filesystem timestamp. Do not invent an expected SHA-256. If the hash is unavailable, identify the build as unpinned and **do not label its identity verified against CI**.

## Current compiled candidate: v0.29.0 native creator and real family portraits

This snapshot **passed Windows compilation and automated policy tests, not a game UI or save-reload test**.

- [Successful Windows run #44356196239](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44356196239).
- [Download its exact compiled-untested artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44356196239/artifacts/14452476589), named `CalradiaUnified-v0.29.0-compiled-untested`.
- Pinned script/source commit `5de33a51501c63ecd8954f76f5d0d748f2831c0e`.
- Built `CalradiaUnified.dll`: **236,032 bytes**, SHA-256 **`52464b6e8adc60cd0e2d2fd72a4c8afe86466f4e955e4f2baf46b9136585795a`**.
- Verified **620 game-policy checks**, **40 simulations**, 0 C# warnings/errors, source-only Gauntlet/unique stage contract, complete packaged prefab list, CI-hash install refusal, negative PowerShell portrait/native-review assertions.

For this exact artifact, use:

```powershell
$ModuleVersion = 'v0.29.0'
$SourceCommit = '5de33a51501c63ecd8954f76f5d0d748f2831c0e'
$ExpectedHash = '52464b6e8adc60cd0e2d2fd72a4c8afe86466f4e955e4f2baf46b9136585795a'
$Zip = Join-Path $env:USERPROFILE 'Downloads\CalradiaUnified-v0.29.0-compiled-untested.zip'
```

The `scripts/Test-CU-Playthrough.ps1` script now accepts `-RequireNativeReview` for the **explicit opt-in new game only** and `-ExpectedPortraits 3` after **CU: View my household portraits** has been opened for a player, spouse and one daughter in the **same logged new-game session**. These switches detect missing actual view loading and wrong real-family row counts, respectively.

**Safety:** Native FaceGen/Culture/Review/Options views stay untouched. The additional CU 3D review is a distinct opt-in stage which wraps the native review without inheriting its registration attribute, **OFF by default**. It is not a single continuous CU/native creator. See [v0.29 real-game acceptance protocol](V029_NATIVE_CREATOR_AND_FAMILY_UI_ACCEPTANCE.md).

## Historical verified example: v0.28.0, 18 November 2026


For the current `v0.28.0` **compiled-untested** family-home candidate, the PowerShell procedure itself passed its Windows CI tests:

- [Successful Windows run #44345749188](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44345749188).
- [Download the exact CI artifact](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44345749188/artifacts/14445218238). Downloaded filename: `CalradiaUnified-v0.28.0-compiled-untested.zip`.
- Immutable source script commit: `4c0b1c452b7d32ecee9e2cc6d969226f38574659`.
- Compiled DLL SHA-256: `c182cc2ffb7fba279e59ac9413166e59b6bb3da49cb04b8ea98a64ce8b883b91`. DLL size: 224,256 bytes.
- CI executed **617 policy checks**, **40 simulations**, parsed both PowerShell scripts, verified wrong-hash refusal **before modifying installation**, and tested latest-session one-child report and count rejection. C# compile produced zero warnings/errors.

For that artifact, the three variable assignments in the generic block below become:

```powershell
$ModuleVersion = 'v0.28.0'
$SourceCommit = '4c0b1c452b7d32ecee9e2cc6d969226f38574659'
$ExpectedHash = 'c182cc2ffb7fba279e59ac9413166e59b6bb3da49cb04b8ea98a64ce8b883b91'
```

Do not reuse this hash for a later v0.28 build or any newer version. The module's game behavior, automatic family residence, and save persistence still require actual playtesting.

## Standard paste-ready installation block

Replace only the three uppercase placeholders and the selected ZIP filename with values verified for the actual successful run.

```powershell
$GameRoot = 'D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$ModuleVersion = 'v0.28.0'                       # Replace for the next build
$SourceCommit = 'REPLACE_WITH_VERIFIED_COMMIT_SHA'
$ExpectedHash = 'REPLACE_WITH_64_CHARACTER_CI_DLL_SHA256'
$Zip = Join-Path $env:USERPROFILE 'Downloads\CalradiaUnified-v0.28.0-compiled-untested.zip'

if (-not (Test-Path -LiteralPath $Zip -PathType Leaf)) {
    throw "Download the exact linked CI artifact first: $Zip"
}
Set-ExecutionPolicy -Scope Process Bypass -Force
$Installer = Join-Path $env:TEMP 'Install-CU-From-Artifact.ps1'
$Audit = Join-Path $env:TEMP 'Test-CU-Playthrough.ps1'
$Base = "https://raw.githubusercontent.com/andreivonmariano-dotcom/BANNERLORD/$SourceCommit/scripts"
Invoke-WebRequest -Uri "$Base/Install-CU-From-Artifact.ps1" -UseBasicParsing -OutFile $Installer
Invoke-WebRequest -Uri "$Base/Test-CU-Playthrough.ps1" -UseBasicParsing -OutFile $Audit

& $Installer -GameRoot $GameRoot -ArtifactZip $Zip -ExpectedVersion $ModuleVersion -ExpectedDllSha256 $ExpectedHash -ReplaceExisting
& $Audit -GameRoot $GameRoot -ExpectedVersion $ModuleVersion -ExpectedDllSha256 $ExpectedHash
```

The installer prints `INSTALL PASS` and a `Verified DLL against pinned successful CI SHA256` line. The audit prints `CI HASH: PASS`. **A missing game start in the audit immediately after installing is normal**. Play the game before testing the family.

If the installer rejects the archive **before installation**, do not bypass the hash. Confirm the downloaded artifact URL and hash belong to the same CI run. This preserves the previous installed module. A failed copy should restore its automatically created backup. Do not remove old backups or user saves as a quick workaround.

## Standard campaign test after playing

After configuring the custom screen and completing native culture, 3D appearance, backgrounds, age, and difficulty, reach the campaign map. Save the game. Run the entire PowerShell block below.

```powershell
$GameRoot = 'D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$Audit = Join-Path $env:TEMP 'Test-CU-Playthrough.ps1'
$ExpectedChildren = 1
& $Audit -GameRoot $GameRoot -ExpectedChildren $ExpectedChildren
```

For the **highest-confidence report**, include `-ExpectedVersion $ModuleVersion -ExpectedDllSha256 $ExpectedHash` with values from the installation block. The optional `-Assert` switch is for CI and test assertions. By default the script prints `PASS`, `FAIL`, or `INCOMPLETE` without throwing on a gameplay failure.

The audit checks, among other fields:

- `sandbox_overhaul_accepted`: final saved role and child count.
- `sandbox_creator_family_plan` and `cu_family_loaded_plan`: count before and after the native character wizard.
- `cu_family_spouse_created`, `cu_family_child_created`, and `cu_family_verification`: real family generation and number of children.
- `sandbox_start_contract`: actual native role/kingdom/fiefs, spouse present, requested children and actual children.
- `cu_family_home_result` and `cu_family_child_home`: family residence at an owned castle or town. An underage child's stock Clan label can remain `Holding`.
- `exceptions.csv`: related managed exceptions since the latest recorded accepted start.

A spouse created and a contract PASS are valuable first-run observations. They do not establish that the spouse or infant survives **save, full exit, and reload**. Open the Clan and Encyclopedia after reloading and inspect the actual people, parent links, age, location, and absence of duplicates. Record this as a distinct save/reload gate. This script never opens or alters save files.

Events are cumulative. A manual residence action on an older saved campaign can occur after the latest `sandbox_overhaul_accepted` from a different campaign. For such tests, inspect `cu_family_home_result` timestamps and the current in-game settlement separately. Do not claim a CSV event belongs to the loaded save unless identifiers or the test timeline support it.

## Optional local JSON report and diagnostic ZIP

```powershell
$GameRoot = 'D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$Audit = Join-Path $env:TEMP 'Test-CU-Playthrough.ps1'
$Report = Join-Path $env:USERPROFILE 'Desktop\CU-Playthrough-Report.json'
& $Audit -GameRoot $GameRoot -ExpectedChildren 1 -ReportJson $Report

$Exporter = Join-Path $env:TEMP 'Export-CU-Diagnostics.ps1'
# Download this exporter from the same pinned source commit, if needed.
& $Exporter -GameDir $GameRoot -RecentHours 72
```

Run the exporter only after downloading its exact source script. It defaults to a timestamped diagnostic ZIP on Desktop. Never upload private player saves, raw memory dumps, credentials, or unrelated personal logs to the public repository. Inspect diagnostic ZIP contents before sharing them in chat.

## Rules for every subsequent coding chat

- **Read first:** `AGENTS.md`, `PROJECT_INSTRUCTIONS.md`, this SOP, the current relevant `docs/V0xx_..._ACCEPTANCE.md`, and latest successful run. Treat the current branch and run as source of truth rather than recollections from a previous chat.
- **Deliver two commands after each successful candidate:** the CI-pinned PowerShell install command and a read-only PowerShell playthrough test command matched to the specific changed feature. Include current run URL, source commit, artifact link, exact DLL hash, version, and at least one observable success/failure contract.
- **Never label compilation as game proof.** Gate B is compilation and policy checks. User-observed in-game behavior and save/reload establish separate later gates.
- **Keep shell syntax compatible with Windows PowerShell 5.1.** Submit `if`, `elseif` and `else` as a single script block, not separate pasted commands. Do not use PS7-only operators. Avoid broad wildcard selection of ZIPs that share a version number.
- **Run CI on scripts:** PowerShell parser checks, wrong-hash rejection **before any module replacement**, nested artifact install, last-campaign filtering, expected-child mismatch detection, and successful synthetic fixture. These tests prevent regressions in the test procedure itself.
- **Do not bundle helper scripts inside the player-facing runtime ZIP.** The module remains directly paste-ready, without MSI, registry edits, or a binary installer. PowerShell is the user's selected optional installation and diagnostic interface.
- **Future goals:** After this process is standardized, integrate CU starting choices into a **single safe native character-creation sequence** and redesign the Gauntlet UI with genuine 3D player/family previews. Avoid the unverified competing FaceGen stage view that crashed the v0.22 culture screen. Keep family and save data working across UI changes.

## Maintaining this standard

Whenever a new compiled version is produced, refresh the example version, workflow URL and commit/hash **for that exact run**, but do not hardcode old hashes as universally valid. Modify `PROJECT_INSTRUCTIONS.md`, `CHATGPT_PROJECT_INSTRUCTIONS.txt`, and `AGENTS.md` together if the controlling procedure changes. Keep this file linked from the root README, so a future chat or contributor can discover it immediately.

**Standard adopted:** 18 November 2026 following real v0.25 same-version hash confusion, v0.26 marriage diagnostics, v0.27 one-child input verification, and v0.28 family residence diagnostics.
