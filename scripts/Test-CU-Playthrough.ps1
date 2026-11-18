# Calradia Unified player-side runtime audit, PowerShell 5.1 compatible.
# Read-only: no game launch, save edits, mod installation, or game API calls.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [string]$ExpectedVersion = "",
    [string]$ExpectedDllSha256 = "",
    [ValidateRange(-1,6)][int]$ExpectedChildren = -1,
    [switch]$RequireNativeReview,
    [ValidateRange(-1,12)][int]$ExpectedPortraits = -1,
    [string]$EventsCsv = "",
    [string]$ExceptionsCsv = "",
    [string]$ReportJson = "",
    [switch]$Assert
)
$ErrorActionPreference = 'Stop'

function Detail-Value([string]$detail, [string]$key) {
    if (-not $detail) { return '' }
    foreach ($entry in ($detail -split '\|')) {
        if ($entry.StartsWith($key + '=', [StringComparison]::Ordinal)) {
            return $entry.Substring($key.Length + 1)
        }
    }
    return ''
}
function Event-Last([object[]]$rows, [string]$name) {
    return @($rows | Where-Object { $_.context -eq $name } |
        Select-Object -Last 1) | Select-Object -Last 1
}
function Event-After([object]$row, [DateTimeOffset]$start) {
    try {
        return ([DateTimeOffset]::Parse([string]$row.utc) -ge $start)
    } catch {
        return $false
    }
}
function Check-Number([string]$label, [string]$observed, [int]$expected,
    [Collections.Generic.List[string]]$issues) {
    if (-not $observed) { return }
    $parsed = 0
    if (-not [int]::TryParse($observed,[ref]$parsed) -or $parsed -ne $expected) {
        $issues.Add("$label=$observed (expected $expected)")
    }
}

$game = [IO.Path]::GetFullPath($GameRoot)
$module = Join-Path $game 'Modules\CalradiaUnified'
$dll = Join-Path $module 'bin\Win64_Shipping_Client\CalradiaUnified.dll'
$manifest = Join-Path $module 'SubModule.xml'
if (-not (Test-Path -LiteralPath $dll -PathType Leaf) -or
    -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Missing installed CalradiaUnified.dll or SubModule.xml under $module"
}
[xml]$definition = Get-Content -LiteralPath $manifest -Raw
$version = [string]$definition.Module.Version.value
$moduleId = [string]$definition.Module.Id.value
if ($moduleId -ne 'CalradiaUnified') { throw "Unexpected module ID $moduleId" }
$hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
$bytes = (Get-Item -LiteralPath $dll).Length
$trusted = $ExpectedDllSha256.Trim().ToLowerInvariant()
if ($trusted -and $trusted -notmatch '^[0-9a-f]{64}$') {
    throw 'ExpectedDllSha256 must be 64 hexadecimal characters.'
}
if ($ExpectedVersion -and $ExpectedVersion -ne $version) {
    throw "Wrong installed version: $version (expected $ExpectedVersion)"
}
if ($trusted -and $trusted -ne $hash) {
    throw "Wrong installed DLL hash: $hash (expected $trusted)"
}

Write-Host '========== CU BUILD IDENTITY =========='
Write-Host "Module version: $version"
Write-Host "DLL bytes: $bytes"
Write-Host "DLL SHA256: $hash"
if ($trusted) {
    Write-Host 'CI HASH: PASS (exact DLL match)'
} else {
    Write-Host 'CI HASH: UNPINNED (supply -ExpectedDllSha256 from CI)'
}
if ($ExpectedVersion) { Write-Host 'MODULE VERSION: PASS' }
else { Write-Host 'MODULE VERSION: UNPINNED' }

$diagnostics = Join-Path $env:LOCALAPPDATA 'CalradiaUnified\Diagnostics'
if (-not $EventsCsv) { $EventsCsv = Join-Path $diagnostics 'events.csv' }
if (-not $ExceptionsCsv) { $ExceptionsCsv = Join-Path $diagnostics 'exceptions.csv' }
$events = @()
if (Test-Path -LiteralPath $EventsCsv -PathType Leaf) {
    $events = @(Import-Csv -LiteralPath $EventsCsv)
}
$accepted = Event-Last $events 'sandbox_overhaul_accepted'
$session = @()
$sessionUtc = ''
if ($null -ne $accepted -and $accepted.utc) {
    try {
        $start = [DateTimeOffset]::Parse([string]$accepted.utc)
        $sessionUtc = $start.ToString('o')
        $session = @($events | Where-Object { Event-After $_ $start })
    } catch {
        $sessionUtc = ''
    }
}

$plan = Event-Last $session 'sandbox_creator_family_plan'
$loaded = Event-Last $session 'cu_family_loaded_plan'
$verification = Event-Last $session 'cu_family_verification'
$contract = Event-Last $session 'sandbox_start_contract'
$homeEvent = Event-Last $session 'cu_family_home_result'
$homeStatus = Event-Last $session 'cu_family_home_status'
$childEvents = @($session | Where-Object { $_.context -eq 'cu_family_child_created' })
$spouse = Event-Last $session 'cu_family_spouse_created'
$nativeReviewAttached = Event-Last $session 'cu_native_review_attached'
$nativeReviewLoaded = Event-Last $session 'cu_native_review_view_loaded'
$nativeReviewSkipped = Event-Last $session 'cu_native_review_skipped'
$nativeReviewFallback = Event-Last $session 'cu_native_review_summary_fallback'
$galleryOpened = Event-Last $session 'cu_family_portraits_open'
$galleryFailed = Event-Last $session 'cu_family_portrait_screen_failed'

$expected = $ExpectedChildren
if ($expected -lt 0) {
    foreach ($value in @(
        (Detail-Value $loaded.detail 'requested_children'),
        (Detail-Value $plan.detail 'child_count'),
        (Detail-Value $accepted.detail 'child_count')
    )) {
        $parsed = 0
        if ([int]::TryParse($value,[ref]$parsed)) {
            $expected = $parsed
            break
        }
    }
}
$issues = New-Object 'System.Collections.Generic.List[string]'
$contractOutcome = Detail-Value $contract.detail 'result'
$familyOutcome = Detail-Value $verification.detail 'result'
if ($null -ne $contract -and $contractOutcome -ne 'PASS') {
    $issues.Add('Starting contract did not PASS: ' + $contract.detail)
}
if ($null -ne $verification -and $familyOutcome -ne 'PASS') {
    $issues.Add('Native family verification did not PASS: ' + $verification.detail)
}
if ($expected -ge 0 -and $null -ne $contract) {
    Check-Number 'accepted child_count' (Detail-Value $accepted.detail 'child_count') $expected $issues
    if ($null -ne $plan) {
        Check-Number 'creator child_count' (Detail-Value $plan.detail 'child_count') $expected $issues
    }
    if ($null -ne $loaded) {
        Check-Number 'loaded requested_children' (Detail-Value $loaded.detail 'requested_children') $expected $issues
    }
    Check-Number 'contract requested_children' (Detail-Value $contract.detail 'requested_children') $expected $issues
    Check-Number 'contract children' (Detail-Value $contract.detail 'children') $expected $issues
    if ($childEvents.Count -ne $expected) {
        $issues.Add('Current start child_created events=' +
            $childEvents.Count + ' (expected ' + $expected + ')')
    }
}
if ($RequireNativeReview) {
    if ($null -eq $nativeReviewAttached -or $null -eq $nativeReviewLoaded) {
        $skipped = if ($null -ne $nativeReviewSkipped) { $nativeReviewSkipped.detail } else { 'no skip logged' }
        $issues.Add('Expected opt-in native 3D CU stage was not opened: ' + $skipped)
    }
    if ($null -ne $nativeReviewFallback) {
        $issues.Add('Optional CU native review overlay could not load: ' +
            $nativeReviewFallback.detail)
    }
}
if ($ExpectedPortraits -ge 0) {
    if ($null -eq $galleryOpened) {
        $issues.Add('Expected real-Hero portrait gallery was not opened in this logged session')
    } else {
        Check-Number 'genuine Hero gallery rows' (Detail-Value $galleryOpened.detail 'actual_hero_portraits') $ExpectedPortraits $issues
    }
    if ($null -ne $galleryFailed) {
        $issues.Add('Gauntlet family gallery failed: ' + $galleryFailed.detail)
    }
}
$exceptions = @()
if (Test-Path -LiteralPath $ExceptionsCsv -PathType Leaf) {
    $allExceptions = @(Import-Csv -LiteralPath $ExceptionsCsv)
    if ($sessionUtc) {
        $start = [DateTimeOffset]::Parse($sessionUtc)
        $exceptions = @($allExceptions | Where-Object { Event-After $_ $start })
    } else {
        $exceptions = @($allExceptions | Select-Object -Last 10)
    }
}
if (-not $sessionUtc -or $null -eq $contract) {
    $outcome = 'INCOMPLETE'
} elseif ($issues.Count -gt 0) {
    $outcome = 'FAIL'
} else {
    $outcome = 'PASS'
}
Write-Host ''
Write-Host '========== LATEST ACCEPTED SANDBOX START =========='
if ($sessionUtc) {
    Write-Host "Start UTC: $sessionUtc"
    Write-Host "Accepted: $($accepted.detail)"
    Write-Host "Creator plan: $(if ($null -eq $plan) { 'not logged' } else { $plan.detail })"
    Write-Host "Native-loaded plan: $(if ($null -eq $loaded) { 'not logged' } else { $loaded.detail })"
    Write-Host "Child count expected: $(if ($expected -lt 0) { 'unknown' } else { $expected })"
    Write-Host "Child created events this start: $($childEvents.Count)"
    Write-Host "Spouse: $(if ($null -eq $spouse) { 'not logged' } else { $spouse.detail })"
    Write-Host "Family verification: $(if ($null -eq $verification) { 'not logged' } else { $verification.detail })"
    Write-Host "Start contract: $(if ($null -eq $contract) { 'not logged' } else { $contract.detail })"
    Write-Host "Family residence: $(if ($null -ne $homeEvent) { $homeEvent.detail } elseif ($null -ne $homeStatus) { $homeStatus.detail } else { 'not logged' })"
    Write-Host "CU native review stage: $(if ($null -ne $nativeReviewLoaded) { 'view loaded' } elseif ($null -ne $nativeReviewAttached) { 'attached, view not logged' } elseif ($null -ne $nativeReviewSkipped) { $nativeReviewSkipped.detail } else { 'not logged' })"
    Write-Host "CU native review summary fallback: $(if ($null -ne $nativeReviewFallback) { 'FAILED' } else { 'none logged' })"
    Write-Host "Real Hero portrait gallery: $(if ($null -ne $galleryOpened) { $galleryOpened.detail } else { 'not opened in latest accepted start' })"
} else {
    Write-Host 'No accepted custom Sandbox start found in events.csv.'
}
Write-Host "SESSION STATUS: $outcome"
foreach ($issue in $issues) { Write-Host "MISMATCH: $issue" }
Write-Host "Exceptions since this start: $($exceptions.Count)"
foreach ($row in @($exceptions | Select-Object -Last 3)) {
    Write-Host ("EXCEPTION: " + $row.utc + ' ' + $row.context + ' ' +
        $row.exception_type + ' ' + $row.message)
}
Write-Host 'SAVE/RELOAD: NOT PROVEN by CSV; inspect the actual reloaded Clan panel.'
Write-Host 'Note: events.csv is historical. Results are anchored to its latest accepted custom Sandbox start.'
Write-Host 'Note: this script changes neither saves nor installed files.'

$report = [ordered]@{
    schema = 'CU-POWERSHELL-PLAYTHROUGH-1'
    utc = [DateTime]::UtcNow.ToString('o')
    module_version = $version
    dll_sha256 = $hash
    dll_bytes = $bytes
    ci_hash_pinned = [bool]$trusted
    start_utc = $sessionUtc
    expected_children = $expected
    child_created_events = $childEvents.Count
    spouse_event_recorded = ($null -ne $spouse)
    contract_result = $contractOutcome
    family_verification = $familyOutcome
    family_home_result = $(if ($null -ne $homeEvent) { $homeEvent.detail } else { '' })
    native_review_required = [bool]$RequireNativeReview
    native_review_attached = ($null -ne $nativeReviewAttached)
    native_review_loaded = ($null -ne $nativeReviewLoaded)
    native_review_summary_fallback = ($null -ne $nativeReviewFallback)
    expected_portraits = $ExpectedPortraits
    portrait_gallery_opened = ($null -ne $galleryOpened)
    portrait_gallery_rows = $(if ($null -ne $galleryOpened) { Detail-Value $galleryOpened.detail 'actual_hero_portraits' } else { '' })
    exceptions_in_scope = $exceptions.Count
    status = $outcome
    mismatches = $issues.ToArray()
    save_reload_verified = $false
}
if ($ReportJson) {
    $path = [IO.Path]::GetFullPath($ReportJson)
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    $report | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $path -Encoding UTF8
    Write-Host "JSON report: $path"
}
if ($Assert -and $outcome -ne 'PASS') {
    throw "Playthrough test did not PASS: $outcome. See mismatch lines above."
}
