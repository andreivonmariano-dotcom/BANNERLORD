# Investigates a missing/conflicting CalradiaUnified.dll load.
# Read-only by default. Windows PowerShell 5.1 or PowerShell 7.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [string]$OutJson = "",
    [switch]$UnblockCalradiaDll
)
$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($GameRoot)
$modules = Join-Path $root "Modules"
$native = Join-Path $modules "Native\SubModule.xml"
$manifest = Join-Path $modules "CalradiaUnified\SubModule.xml"
$dll = Join-Path $modules "CalradiaUnified\bin\Win64_Shipping_Client\CalradiaUnified.dll"
$campaign = Join-Path $root "bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll"
$result = [ordered]@{
    schema = "CU-LOAD-DIAG-1"
    generated_utc = [DateTime]::UtcNow.ToString("o")
    game_root = $root
    native_manifest_present = (Test-Path -LiteralPath $native -PathType Leaf)
    native_game_version = ""
    module_manifest_present = (Test-Path -LiteralPath $manifest -PathType Leaf)
    module_id = ""
    module_version = ""
    dll_present = (Test-Path -LiteralPath $dll -PathType Leaf)
    dll_bytes = 0
    dll_sha256 = ""
    clr_identity = ""
    clr_metadata_error = ""
    windows_download_zone_present = $false
    unblock_requested = $UnblockCalradiaDll.IsPresent
    game_campaign_assembly_present = (Test-Path -LiteralPath $campaign -PathType Leaf)
    game_campaign_assembly_file_version = ""
    game_campaign_assembly_product_version = ""
    missing_taleworlds_assemblies = @()
    checked_taleworlds_assemblies = @()
    result = "NOT_EVALUATED"
    game_launch_verified = $false
    note = "File checks do not prove Bannerlord can bind native types or execute the module."
}
if (Test-Path -LiteralPath $native -PathType Leaf) {
    try {
        [xml]$n = Get-Content -LiteralPath $native -Raw
        $result.native_game_version = [string]$n.Module.Version.value
    } catch {}
}
if (Test-Path -LiteralPath $manifest -PathType Leaf) {
    try {
        [xml]$m = Get-Content -LiteralPath $manifest -Raw
        $result.module_id = [string]$m.Module.Id.value
        $result.module_version = [string]$m.Module.Version.value
    } catch {}
}
if (Test-Path -LiteralPath $campaign -PathType Leaf) {
    $info = Get-Item -LiteralPath $campaign
    $result.game_campaign_assembly_file_version = [string]$info.VersionInfo.FileVersion
    $result.game_campaign_assembly_product_version = [string]$info.VersionInfo.ProductVersion
}
if (Test-Path -LiteralPath $dll -PathType Leaf) {
    $result.dll_bytes = (Get-Item -LiteralPath $dll).Length
    $result.dll_sha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($UnblockCalradiaDll) { Unblock-File -LiteralPath $dll }
    try {
        $zone = Get-Item -LiteralPath $dll -Stream Zone.Identifier -ErrorAction SilentlyContinue
        $result.windows_download_zone_present = ($null -ne $zone)
    } catch {}
    try {
        $identity = [Reflection.AssemblyName]::GetAssemblyName($dll)
        $result.clr_identity = [string]$identity.FullName
    } catch {
        $result.clr_metadata_error = $_.Exception.GetType().Name + ": " + $_.Exception.Message
    }
}
$names = @(
    "TaleWorlds.MountAndBlade.dll",
    "TaleWorlds.Core.dll",
    "TaleWorlds.CampaignSystem.dll",
    "TaleWorlds.Library.dll",
    "TaleWorlds.Localization.dll",
    "TaleWorlds.ObjectSystem.dll",
    "TaleWorlds.ScreenSystem.dll",
    "TaleWorlds.Engine.GauntletUI.dll",
    "TaleWorlds.GauntletUI.dll"
)
$missing = @()
$checked = @()
foreach ($name in $names) {
    $normal = Join-Path (Join-Path $root "bin\Win64_Shipping_Client") $name
    if (Test-Path -LiteralPath $normal -PathType Leaf) {
        $checked += $name
    }
    elseif (Test-Path -LiteralPath $modules -PathType Container) {
        $found = @(Get-ChildItem -LiteralPath $modules -Filter $name -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1)
        if ($found.Count -gt 0) { $checked += $name }
        else { $missing += $name }
    }
    else { $missing += $name }
}
$result.missing_taleworlds_assemblies = @($missing)
$result.checked_taleworlds_assemblies = @($checked)
if (-not $result.dll_present) {
    $result.result = "MOD_DLL_MISSING"
}
elseif ($result.clr_metadata_error) {
    $result.result = "INVALID_OR_UNREADABLE_CLR_ASSEMBLY"
}
elseif (-not $result.module_manifest_present -or
    $result.module_id -ne "CalradiaUnified" -or
    $result.module_version -ne "v0.16.0") {
    $result.result = "MODULE_MANIFEST_MISMATCH"
}
elseif ($result.windows_download_zone_present) {
    $result.result = "WINDOWS_FILE_BLOCK_POSSIBLE"
}
elseif ($result.native_game_version -and
    $result.native_game_version -notmatch "1[.]5[.]7") {
    $result.result = "NATIVE_GAME_VERSION_MISMATCH"
}
elseif ($missing.Count -gt 0) {
    $result.result = "GAME_DLL_LOCATIONS_NEED_INSPECTION"
}
else {
    $result.result = "FILES_PRESENT_LOADER_FAILURE_UNRESOLVED"
}
if ($OutJson) {
    $full = [IO.Path]::GetFullPath($OutJson)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $full) | Out-Null
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $full -Encoding UTF8
}
Write-Output ("Diagnostic result: " + $result.result)
Write-Output ("Game Native version: " + $result.native_game_version)
Write-Output ("CampaignSystem file version: " + $result.game_campaign_assembly_file_version)
Write-Output ("Mod DLL exists: " + $result.dll_present + " size: " + $result.dll_bytes)
Write-Output ("Mod DLL SHA256: " + $result.dll_sha256)
Write-Output ("CLR identity: " + $result.clr_identity)
Write-Output ("Blocked download mark: " + $result.windows_download_zone_present)
if ($result.clr_metadata_error) { Write-Output ("CLR error: " + $result.clr_metadata_error) }
if ($missing.Count -gt 0) { Write-Output ("Missing game DLLs: " + ($missing -join ", ")) }
if ($OutJson) { Write-Output ("Report saved: " + [IO.Path]::GetFullPath($OutJson)) }
Write-Output "No game was launched. The DLL is changed only if -UnblockCalradiaDll was specified."
