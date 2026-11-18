# Install Calradia Unified from a direct mod ZIP or GitHub's ZIP-wrapped ZIP.
# Does not run the game, change game settings, or touch saves.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [Parameter(Mandatory=$true)][string]$ArtifactZip,
    [string]$ExpectedVersion = "v0.33.0",
    [string]$ExpectedDllSha256 = "",
    [switch]$ReplaceExisting
)
$ErrorActionPreference = "Stop"
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
