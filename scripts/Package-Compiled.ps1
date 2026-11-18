param(
    [Parameter(Mandatory=$true)][string]$Dll,
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $Dll).Path
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Missing compiled DLL. No package produced.' }
if ([IO.Path]::GetFileName($source) -ne 'CalradiaUnified.dll') { throw 'Unexpected binary name.' }
$assembly = [Reflection.AssemblyName]::GetAssemblyName($source)
if ($assembly.Name -ne 'CalradiaUnified') { throw 'Binary identity does not match the module.' }
$module = Join-Path $ProjectRoot 'Modules\CalradiaUnified'
$manifest = Join-Path $module 'SubModule.xml'
if (-not (Test-Path $manifest)) { throw 'Module manifest missing.' }
[xml]$x = Get-Content $manifest
if ($x.Module.Id.value -ne 'CalradiaUnified' -or
    $x.Module.SubModules.SubModule.DLLName.value -ne 'CalradiaUnified.dll') { throw 'Module manifest mismatch.' }
$root = Join-Path $ProjectRoot 'Output'
$stage = Join-Path $root 'Modules\CalradiaUnified'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item $manifest (Join-Path $stage 'SubModule.xml')
# Only our own native Gauntlet movie XML is packaged; no third-party UI asset.
$gui = Join-Path $module 'GUI'
if (Test-Path -LiteralPath $gui) {
    Copy-Item $gui (Join-Path $stage 'GUI') -Recurse
    $discontinued = Join-Path $stage 'GUI\Prefabs\CUOriginPanel.xml'
    if (Test-Path -LiteralPath $discontinued) {
        Remove-Item -LiteralPath $discontinued -Force
    }
}
Copy-Item (Join-Path $module 'ModuleData') (Join-Path $stage 'ModuleData') -Recurse
# The read-only crash/finance ZIP exporter ships inside the one-folder module.
Copy-Item (Join-Path $ProjectRoot 'scripts\Export-CU-Diagnostics.ps1')  (Join-Path $stage 'ModuleData\Export-CU-Diagnostics.ps1')
$bins = Join-Path $stage 'bin\Win64_Shipping_Client'
New-Item -ItemType Directory -Path $bins -Force | Out-Null
Copy-Item $source (Join-Path $bins 'CalradiaUnified.dll')
$version = [string]$x.Module.Version.value
if ($version -notmatch '^v[0-9]+[.][0-9]+[.][0-9]+$') { throw 'Unexpected manifest version.' }
$package = Join-Path $root ("CalradiaUnified_" + $version + "_COMPILED_UNTESTED.zip")
if (Test-Path $package) { Remove-Item $package -Force }
Compress-Archive -Path (Join-Path $root 'Modules') -DestinationPath $package -CompressionLevel Optimal
if (-not (Test-Path $package)) { throw 'Archive creation failed' }
Write-Host "Compiled, untested module artifact: $package"
Write-Host 'Not certified in-game. Do not call this a final release until runtime/save tests pass.'
