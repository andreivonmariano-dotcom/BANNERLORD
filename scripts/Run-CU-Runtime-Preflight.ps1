# Read-only Windows checks before the user runs Bannerlord v1.5.7.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [string]$OutJson = "BuildResults/cu-runtime-preflight.json"
)
$ErrorActionPreference = 'Stop'
$game = [IO.Path]::GetFullPath($GameRoot)
$engine = Join-Path $game 'bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll'
$manifest = Join-Path $game 'Modules\CalradiaUnified\SubModule.xml'
$dll = Join-Path $game 'Modules\CalradiaUnified\bin\Win64_Shipping_Client\CalradiaUnified.dll'
$movie = Join-Path $game 'Modules\CalradiaUnified\GUI\Prefabs\CUSettingsPanel.xml'
if (-not (Test-Path -LiteralPath $engine -PathType Leaf)) {
    throw 'Missing Bannerlord game assembly in the specified game root.'
}
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw 'Calradia Unified module manifest was not found under Modules.'
}
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw 'CalradiaUnified.dll is not installed.'
}
if (-not (Test-Path -LiteralPath $movie -PathType Leaf)) {
    throw 'The Calradia Unified Gauntlet settings prefab is not installed.'
}
[xml]$module = Get-Content -LiteralPath $manifest -Raw
if ([string]$module.Module.Id.value -ne 'CalradiaUnified') {
    throw 'Unexpected module ID.'
}
if ([string]$module.Module.Version.value -ne 'v0.17.0') {
    throw 'The installed module is not the v0.17.0 correction.'
}
$version = (Get-Item -LiteralPath $engine).VersionInfo
$report = [ordered]@{
    schema = 'CU-RUNTIME-PREFLIGHT-1'
    performed_utc = [DateTime]::UtcNow.ToString('o')
    expected_version = 'v0.17.0'
    installed_module_version = [string]$module.Module.Version.value
    game_campaign_assembly_file_version = [string]$version.FileVersion
    game_campaign_assembly_product_version = [string]$version.ProductVersion
    assembly_version = [Reflection.AssemblyName]::GetAssemblyName($engine).Version.ToString()
    module_dll_bytes = (Get-Item -LiteralPath $dll).Length
    module_dll_sha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    prefab_xml_present = $true
    expected_declared_official_dependencies = @('Native', 'SandBoxCore', 'Sandbox')
    result = 'STATIC_PREFLIGHT_PASSED'
    game_executed = $false
    game_v157_confirmed = $false
    settings_screen_rendered = $false
    save_reload_verified = $false
    note = 'Inspect the launcher and run a disposable campaign. Preflight alone cannot prove v1.5.7 or native gameplay.'
}
$directory = Split-Path -Parent ([IO.Path]::GetFullPath($OutJson))
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $OutJson -Encoding UTF8
Write-Output ("Preflight PASS. Results in " + ([IO.Path]::GetFullPath($OutJson)))
Write-Output 'Not a game launch test. Follow docs/V015_RUNTIME_ACCEPTANCE.md for runtime steps.'
