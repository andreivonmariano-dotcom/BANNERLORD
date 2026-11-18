# Calradia Unified diagnostics: read-only export for private inspection in ChatGPT.
[CmdletBinding()]
param([string]$GameDir = "",
      [string]$OutZip = "",
      [int]$RecentHours = 72,
      [switch]$IncludeDumps)
$ErrorActionPreference = 'Stop'
if ($RecentHours -lt 1 -or $RecentHours -gt 720) { throw "RecentHours must be 1..720" }
$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
if (-not $OutZip) { $OutZip = Join-Path ([Environment]::GetFolderPath('Desktop')) "CalradiaUnified-Diagnostics-$timestamp.zip" }
$OutZip = [IO.Path]::GetFullPath($OutZip)
if (Test-Path -LiteralPath $OutZip) { throw "Output already exists: $OutZip" }
$staging = Join-Path ([IO.Path]::GetTempPath()) ("CU-Diagnostic-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging -Force | Out-Null
$cutoff = (Get-Date).AddHours(-$RecentHours)
$record = [Collections.Generic.List[object]]::new()
function Scrub([string]$text) {
  if ($null -eq $text) { return "" }
  $home = [Environment]::GetFolderPath('UserProfile')
  if ($home) { $text = $text.Replace($home, '<USERPROFILE>') }
  if ($env:USERNAME) { $text = [regex]::Replace($text,[regex]::Escape($env:USERNAME),'<USERNAME>','IgnoreCase') }
  if ($env:COMPUTERNAME) { $text = [regex]::Replace($text,[regex]::Escape($env:COMPUTERNAME),'<COMPUTER>','IgnoreCase') }
  return $text
}
function Copy-Text([string]$source, [string]$relative) {
  if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { return }
  $file = Get-Item -LiteralPath $source
  if ($file.Length -gt 8MB) { $record.Add([ordered]@{file=$relative;status='too_large';bytes=$file.Length});return }
  try {
    $destination = Join-Path $staging $relative
    $folder = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    [IO.File]::WriteAllText($destination,(Scrub ([IO.File]::ReadAllText($file.FullName))),[Text.UTF8Encoding]::new($false))
    $record.Add([ordered]@{file=$relative;status='included'})
  } catch { $record.Add([ordered]@{file=$relative;status='locked_or_unreadable'}) }
}
try {
  $local = Join-Path $env:LOCALAPPDATA 'CalradiaUnified'
  $diag = Join-Path $local 'Diagnostics'
  foreach ($n in @('finance.csv','finance.csv.previous','events.csv','exceptions.csv','exceptions.csv.previous')) {
    Copy-Text (Join-Path $diag $n) ("mod/" + $n)
  }
  Copy-Text (Join-Path $local 'calradia-unified.log') 'mod/calradia-unified.log'
  $pd = Join-Path $env:ProgramData 'Mount and Blade II Bannerlord'
  $engine = Join-Path $pd 'logs'
  if (Test-Path -LiteralPath $engine) {
    Get-ChildItem -LiteralPath $engine -File -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -match '^rgl_log' -and $_.LastWriteTime -ge $cutoff } |
      Sort-Object LastWriteTime -Descending | Select-Object -First 6 |
      ForEach-Object { Copy-Text $_.FullName ('engine_logs/' + $_.Name) }
  }
  $crashRoot = Join-Path $pd 'crashes'
  if (Test-Path -LiteralPath $crashRoot) {
    Get-ChildItem -LiteralPath $crashRoot -Directory -ErrorAction SilentlyContinue |
      Where-Object { $_.LastWriteTime -ge $cutoff } |
      Sort-Object LastWriteTime -Descending | Select-Object -First 3 |
      ForEach-Object {
        $folder = $_
        Get-ChildItem -LiteralPath $folder.FullName -File -ErrorAction SilentlyContinue |
          Where-Object { $_.Extension -in @('.log','.txt','.xml','.json') } |
          Select-Object -First 15 |
          ForEach-Object { Copy-Text $_.FullName ('engine_crashes/' + $folder.Name + '/' + $_.Name) }
        if ($IncludeDumps) {
          Get-ChildItem -LiteralPath $folder.FullName -File -Filter '*.dmp' -ErrorAction SilentlyContinue |
            Select-Object -First 1 | ForEach-Object {
              if ($_.Length -le 150MB) {
                $folderName = Join-Path $staging ('engine_crashes/' + $folder.Name)
                New-Item -ItemType Directory -Path $folderName -Force | Out-Null
                Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $folderName $_.Name)
                $record.Add([ordered]@{file=('engine_crashes/' + $folder.Name + '/' + $_.Name);status='raw_dump_included'})
              }
            }
        }
      }
  }
  try {
    $events = @(Get-WinEvent -FilterHashtable @{LogName='Application';StartTime=$cutoff} -ErrorAction Stop |
      Where-Object { $_.ProviderName -match 'Application Error|Windows Error Reporting' -and
        $_.Message -match 'Bannerlord|TaleWorlds' } |
      Sort-Object TimeCreated -Descending | Select-Object -First 15)
    $summary = @($events | ForEach-Object {
      [ordered]@{utc=$_.TimeCreated.ToUniversalTime().ToString('o');id=$_.Id;provider=$_.ProviderName;message=(Scrub $_.Message)}
    })
    [IO.File]::WriteAllText((Join-Path $staging 'windows_events.json'),
      (ConvertTo-Json -InputObject $summary -Depth 5),[Text.UTF8Encoding]::new($false))
  } catch {
    [IO.File]::WriteAllText((Join-Path $staging 'windows_events_status.txt'),
      'No accessible recent Windows Application Error events.')
  }
  $build = [ordered]@{module_version='unknown';dll_sha256='';dll_bytes=0}
  if ($GameDir -and (Test-Path -LiteralPath $GameDir -PathType Container)) {
    $manifest = Join-Path $GameDir 'Modules\CalradiaUnified\SubModule.xml'
    $dll = Join-Path $GameDir 'Modules\CalradiaUnified\bin\Win64_Shipping_Client\CalradiaUnified.dll'
    if (Test-Path -LiteralPath $manifest) {
      [xml]$m = Get-Content -LiteralPath $manifest -Raw
      $build.module_version = [string]$m.Module.Version.value
    }
    if (Test-Path -LiteralPath $dll) {
      $build.dll_sha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
      $build.dll_bytes = (Get-Item -LiteralPath $dll).Length
    }
  }
  $manifest = [ordered]@{
    schema='CU-DIAGNOSTIC-1'
    utc=[DateTime]::UtcNow.ToString('o')
    hours=$RecentHours
    raw_dumps_included=$IncludeDumps.IsPresent
    module=$build
    files=$record.ToArray()
    warning='Review before sharing: game logs can include user names or file paths; raw dumps may contain private memory.'
  }
  [IO.File]::WriteAllText((Join-Path $staging 'manifest.json'),
    (ConvertTo-Json -InputObject $manifest -Depth 8),[Text.UTF8Encoding]::new($false))
  [IO.File]::WriteAllText((Join-Path $staging 'README.txt'),
    'Read-only export for Bannerlord troubleshooting. No saves or raw memory dumps by default.' +
    [Environment]::NewLine +
    'Daily gold changes are not native tax receipts. A missing mod exception does not rule out a hard game crash.')
  $parent = Split-Path -Parent $OutZip
  New-Item -ItemType Directory -Force -Path $parent | Out-Null
  Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $OutZip -CompressionLevel Optimal
  Write-Host "Diagnostic export created: $OutZip"
} finally {
  Remove-Item -LiteralPath $staging -Force -Recurse -ErrorAction SilentlyContinue
}
