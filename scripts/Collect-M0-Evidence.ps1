# Read-only verification collector for a Bannerlord v1.5.7 M0 test session.
# Does not start the game, change saves, install files, or certify a game test.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameDir,
    [string]$OutDir = (Join-Path (Get-Location).Path 'M0-Evidence')
)
$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $GameDir).Path
$module = Join-Path $game 'Modules\CalradiaUnified'
$manifest = Join-Path $module 'SubModule.xml'
$dll = Join-Path $module 'bin\Win64_Shipping_Client\CalradiaUnified.dll'
$native = Join-Path $game 'Modules\Native\SubModule.xml'
foreach ($p in @($manifest,$dll,$native)) {
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "Missing required file: $p" }
}
[xml]$m = Get-Content -LiteralPath $manifest -Raw
[xml]$n = Get-Content -LiteralPath $native -Raw
if ($m.Module.Id.value -ne 'CalradiaUnified') { throw 'Unexpected module ID' }
$logPath = Join-Path $env:LOCALAPPDATA 'CalradiaUnified\calradia-unified.log'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 250 } else { @() }
$summary = [ordered]@{
  collected_utc = [DateTime]::UtcNow.ToString('o')
  game_native_version = [string]$n.Module.Version.value
  module_version = [string]$m.Module.Version.value
  dll_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash
  dll_size_bytes = (Get-Item -LiteralPath $dll).Length
  log_exists = (Test-Path -LiteralPath $logPath)
  observed_submodule_init_log = [bool]($log | Select-String -SimpleMatch 'initialized;')
  observed_campaign_registration_log = [bool]($log | Select-String -SimpleMatch 'behaviors registered')
  observed_session_launch_log = [bool]($log | Select-String -SimpleMatch 'M0 campaign session launched')
  observed_civic_sync_log = [bool]($log | Select-String -SimpleMatch 'M0 civic state sync reached')
  notes = 'These log events indicate code paths ran; successful save reload and game stability require manual gameplay observation.'
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutDir 'm0-summary.json') -Encoding UTF8
if ($log.Count -gt 0) { $log | Set-Content -LiteralPath (Join-Path $OutDir 'calradia-unified-log-tail.txt') -Encoding UTF8 }
Write-Host "M0 evidence collected: $OutDir"
Write-Host 'Read m0-summary.json; the report does not replace in-game test steps.'
