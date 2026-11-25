# Install Calradia Unified from a direct mod ZIP or GitHub's ZIP-wrapped ZIP.
# Does not run the game, change game settings, or touch saves.
# -ShowLatest displays the latest pinned, CI-verified DEVELOPMENT candidate.
# -LatestVerified checks the exact pinned DLL before any existing mod moves.
[CmdletBinding()]
param(
    [string]$GameRoot = "",
    [string]$ArtifactZip = "",
    [string]$ExpectedVersion = "v0.74.0",
    [string]$ExpectedDllSha256 = "",
    [switch]$ReplaceExisting,
    [switch]$ShowLatest,
    [switch]$LatestVerified
)
$ErrorActionPreference = "Stop"

# This pinned candidate was verified by the Windows pipeline. It was NOT
# play-tested in Bannerlord. Pin the exact artifact and compiled checkout,
# which for this PR workflow is the synthetic merge of the indicated head.
$latestLabel = "v0.74.0 family and personal memories (c2a4f25f)"
$latestStatus = "CI compiled and simulated; not tested inside Bannerlord"
$latestRun = "45637611622"
$latestCommit = "68a6f24fbd94e780279ee375d340c2c400fbac0a"
$latestDevelopmentHead = "c2a4f25fca25a61d5ca44357d678cc4d1c240c0d"
$latestManifest = "v0.74.0"
$latestDllSha256 = "e6bcbde295ecf0527ec97b0c4805d11916a28bd92ee099f18d838da68e6d0bba"
$latestArtifactPage = "https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/45637611622/artifacts/15035145061"

function Write-LatestBuild {
    Write-Output "========== CALRADIA UNIFIED: LATEST VERIFIED INSTALL CANDIDATE =========="
    Write-Output ("Development version: " + $latestLabel)
    Write-Output ("Verification: " + $latestStatus)
    Write-Output ("Windows CI run: " + $latestRun)
    Write-Output ("Development branch head: " + $latestDevelopmentHead)
    Write-Output ("Compiled source commit: " + $latestCommit)
    Write-Output ("Download artifact page: " + $latestArtifactPage)
    Write-Output ("Expected DLL SHA256: " + $latestDllSha256)
    Write-Output ("Internal Bannerlord manifest version: " + $latestManifest)
    Write-Output "Download the ZIP from the Actions artifact page, then run this installer:"
    Write-Output '  .\Install-CU-From-Artifact.ps1 -GameRoot "C:\Path\To\Bannerlord" -ArtifactZip "C:\Path\To\Downloaded.zip" -LatestVerified -ReplaceExisting'
    Write-Output "The latest candidate is not a released or game-tested build."
}

# Listing works even on a machine where Bannerlord is not installed.
if ($ShowLatest -or ($PSBoundParameters.Count -eq 0)) {
    Write-LatestBuild
    return
}
if ($LatestVerified) {
    if (($PSBoundParameters.ContainsKey('ExpectedVersion') -and $ExpectedVersion -ne $latestManifest) -or
        ($ExpectedDllSha256 -and
         $ExpectedDllSha256.Trim().ToLowerInvariant() -ne $latestDllSha256)) {
        throw "The selected version/hash conflicts with the latest pinned CI candidate."
    }
    $ExpectedVersion = $latestManifest
    $ExpectedDllSha256 = $latestDllSha256
    Write-Output ("Selected pinned development build " + $latestLabel +
        " (CI " + $latestRun + "; game-untested).")
}
if ([string]::IsNullOrWhiteSpace($GameRoot) -or
    [string]::IsNullOrWhiteSpace($ArtifactZip)) {
    throw "Supply both -GameRoot and -ArtifactZip. Use -ShowLatest to view the pinned build and download URL."
}
$game = [IO.Path]::GetFullPath($GameRoot)
$download = [IO.Path]::GetFullPath($ArtifactZip)
$trustedHash = $ExpectedDllSha256.Trim().ToLowerInvariant()
if ($trustedHash -and $trustedHash -notmatch '^[0-9a-f]{64}$') {
    throw "ExpectedDllSha256 must contain exactly 64 hexadecimal characters."
}
$mods = Join-Path $game "Modules"
if (-not (Test-Path -LiteralPath (Join-Path $mods "Native\SubModule.xml") -PathType Leaf)) {
    throw "Choose the installed Bannerlord game root containing Modules/Native/SubModule.xml."
}
if (-not (Test-Path -LiteralPath $download -PathType Leaf)) {
    throw "The downloaded artifact ZIP does not exist."
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ("CUInstall_" + [Guid]::NewGuid().ToString("N"))
$outer = Join-Path $temp "outer"
$inner = Join-Path $temp "inner"
$destination = Join-Path $mods "CalradiaUnified"
$backup = $null
$installed = $false
try {
    New-Item -ItemType Directory -Path $outer -Force | Out-Null
    Expand-Archive -LiteralPath $download -DestinationPath $outer -Force
    $nested = @(Get-ChildItem -LiteralPath $outer -Recurse -File |
        Where-Object { $_.Name -match '^CalradiaUnified_v[0-9.]+_COMPILED_UNTESTED[.]zip$' })
    if ($nested.Count -gt 1) { throw "More than one compiled mod ZIP in the download." }
    $extracted = $outer
    if ($nested.Count -eq 1) {
        New-Item -ItemType Directory -Path $inner -Force | Out-Null
        Expand-Archive -LiteralPath $nested[0].FullName -DestinationPath $inner -Force
        $extracted = $inner
        Write-Output "Found nested GitHub Actions artifact ZIP."
    }
    $candidate = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Filter "SubModule.xml" |
        Where-Object { $_.Directory.Name -eq "CalradiaUnified" })
    if ($candidate.Count -ne 1) {
        throw ("Expected one CalradiaUnified/SubModule.xml, found " + $candidate.Count)
    }
    $source = $candidate[0].Directory.FullName
    [xml]$manifest = Get-Content -LiteralPath $candidate[0].FullName -Raw
    if ([string]$manifest.Module.Id.value -ne "CalradiaUnified" -or
        [string]$manifest.Module.Version.value -ne $ExpectedVersion -or
        [string]$manifest.Module.SubModules.SubModule.DLLName.value -ne "CalradiaUnified.dll") {
        throw "The module manifest ID, version or DLL name did not match."
    }
    # The four BUTR modules are mandatory for this UI-integrated line.
    # Refuse unmodernized artifacts and missing runtime dependencies
    # BEFORE moving or backing up an installed Calradia Unified module.
    $requiredButr = @(
        "Bannerlord.Harmony",
        "Bannerlord.ButterLib",
        "Bannerlord.UIExtenderEx",
        "Bannerlord.MBOptionScreen"
    )
    $declared = @($manifest.Module.DependedModules.DependedModule |
        ForEach-Object { [string]$_.GetAttribute("Id") })
    foreach ($dependency in $requiredButr) {
        if ($declared -notcontains $dependency) {
            throw ("Artifact is not a required-BUTR build: missing " +
                $dependency + " in its SubModule.xml.")
        }
    }
    $installedDependencies = @{}
    Get-ChildItem -LiteralPath $mods -Directory | ForEach-Object {
        $folder = $_.FullName
        $files = @(
            (Join-Path $folder "SubModule.xml"),
            (Join-Path $folder "_Module\SubModule.xml")
        )
        foreach ($file in $files) {
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
            try {
                [xml]$modInfo = Get-Content -LiteralPath $file -Raw
                $id = [string]$modInfo.Module.Id.value
                if (-not [string]::IsNullOrWhiteSpace($id)) {
                    $installedDependencies[$id] = $folder
                }
            }
            catch {
                throw ("Invalid Bannerlord dependency manifest: " + $file)
            }
        }
    }
    foreach ($dependency in $requiredButr) {
        if (-not $installedDependencies.ContainsKey($dependency)) {
            throw ("Required BUTR module not installed: " + $dependency +
                ". Install and activate Harmony, ButterLib, UIExtenderEx " +
                "and Mod Configuration Menu before Calradia Unified.")
        }
    }
    Write-Output ("BUTR PRECHECK PASS: " +
        ($requiredButr -join ", "))
    $dllRelative = "bin\Win64_Shipping_Client\CalradiaUnified.dll"
    $sourceDll = Join-Path $source $dllRelative
    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
        throw "The expected compiled CalradiaUnified.dll is missing."
    }
    # A version number alone cannot distinguish two successful CI builds.
    # Verify the artifact against its chosen successful workflow BEFORE
    # moving the existing module or modifying the game directory.
    $sourceHash = (Get-FileHash -LiteralPath $sourceDll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($trustedHash -and $sourceHash -ne $trustedHash) {
        throw ("Artifact rejected BEFORE installation. Expected CI DLL SHA256: " +
            $trustedHash + "; downloaded DLL SHA256: " + $sourceHash)
    }
    if ($ExpectedVersion -eq 'v0.74.0') {
        $identityPath = Join-Path $source 'ModuleData\CU-BuildIdentity.json'
        if (-not (Test-Path -LiteralPath $identityPath -PathType Leaf)) {
            throw 'Artifact rejected BEFORE installation: missing build identity.'
        }
        $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
        if ($identity.module_id -ne 'CalradiaUnified' -or
            $identity.manifest_version -ne $ExpectedVersion -or
            $identity.assembly_version -ne ($ExpectedVersion.Substring(1)+'.0') -or
            $identity.dll_sha256 -ne $sourceHash -or
            $identity.acceptance -ne 'COMPILED_UNTESTED') {
            throw 'Artifact rejected BEFORE installation: inconsistent build identity.'
        }
        if ($LatestVerified -and ($identity.source_commit -ne $latestCommit -or
            [string]$identity.ci_run_id -ne $latestRun)) {
            throw 'Artifact rejected BEFORE installation: pinned source or workflow identity mismatch.'
        }
    }
    if (Test-Path -LiteralPath $destination) {
        if (-not $ReplaceExisting) {
            throw ("Module already exists at " + $destination +
                ". Use -ReplaceExisting to back up and replace it.")
        }
        $backup = Join-Path $game ("CalradiaUnified_Backup_" +
            [DateTime]::UtcNow.ToString("yyyyMMdd_HHmmss_ffff"))
        Move-Item -LiteralPath $destination -Destination $backup
    }
    # Remove partial copies on any failure and restore the backup.
    $installed = $true
    Copy-Item -LiteralPath $source -Destination $destination -Recurse
    $dll = Join-Path $destination $dllRelative
    if (-not (Test-Path -LiteralPath (Join-Path $destination "SubModule.xml") -PathType Leaf)) {
        throw "The installed SubModule.xml is missing."
    }
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
        throw "The installed DLL is missing."
    }
    $actual = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $sourceHash) { throw "Installed DLL hash mismatch." }
    if ($trustedHash -and $actual -ne $trustedHash) {
        throw "Installed DLL does not match the selected CI SHA-256."
    }
    Write-Output ("INSTALL PASS: " + $destination)
    if ($LatestVerified) {
        Write-Output ("Installed selected development candidate: " + $latestLabel)
        Write-Output ("Verified Windows CI run: " + $latestRun)
        Write-Output "Game runtime acceptance: NOT TESTED"
    }
    Write-Output ("Manifest version: " + $manifest.Module.Version.value)
    Write-Output ("Installed DLL SHA256: " + $actual)
    if ($trustedHash) {
        Write-Output ("Verified DLL against pinned successful CI SHA256: " + $trustedHash)
    } else {
        Write-Warning "CI identity UNPINNED. Pass -ExpectedDllSha256 from the chosen successful workflow."
    }
    if ($backup) { Write-Output ("Older module backed up at: " + $backup) }
    Write-Output "Fully close and reopen Bannerlord, then check Singleplayer > Mods."
    Write-Output "File installation is verified, not in-game loading or launcher detection."
}
catch {
    if ($installed -and (Test-Path -LiteralPath $destination)) {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    if ($backup -and (Test-Path -LiteralPath $backup)) {
        Move-Item -LiteralPath $backup -Destination $destination
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
