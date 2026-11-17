# Calradia Unified 0.2.0: Windows-only local build and output staging.
# Reads the player's Bannerlord 1.5.7 binaries in place, doesn't upload them.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [switch]$RunPolicyTests
)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root 'Source\CalradiaUnified\CalradiaUnified.csproj'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$Bin = Join-Path $GameDir 'bin\Win64_Shipping_Client'
$Required = @('TaleWorlds.MountAndBlade.dll','TaleWorlds.Core.dll',
  'TaleWorlds.Library.dll','TaleWorlds.CampaignSystem.dll','TaleWorlds.ObjectSystem.dll', 'TaleWorlds.Localization.dll')
foreach($Name in $Required) {
    if (-not (Test-Path -LiteralPath (Join-Path $Bin $Name))) {
        throw "Game reference missing: $Name in $Bin"
    }
}
$NativeManifest = Join-Path $GameDir 'Modules\Native\SubModule.xml'
if (-not (Test-Path -LiteralPath $NativeManifest)) { throw "Missing Native manifest: $NativeManifest" }
[xml]$Native = Get-Content -LiteralPath $NativeManifest -Raw
$Version = [string]$Native.Module.Version.value
if ($Version -notmatch '(^|\D)1\.5\.7(\D|$)') {
    throw "Target v1.5.7 not confirmed. Native reports [$Version]. Aborting."
}
$Dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $Dotnet) { throw "dotnet not found. Install the .NET SDK and .NET Framework 4.7.2 targeting pack." }
$LogsDir = Join-Path $Root 'BuildResults'
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null
$LogPath = Join-Path $LogsDir 'last-build.log'
$Output = Join-Path $Root 'Output\Modules\CalradiaUnified'
$BuildDir = Join-Path $Root 'Source\CalradiaUnified\bin\Release\net472'
$Params = @('build', $Project, '--configuration', 'Release', '--nologo',
  '--verbosity', 'normal', "-p:BannerlordGameDir=$GameDir")
Write-Host "Compiling against [$Version] installed at $GameDir"
& $Dotnet.Source @Params 2>&1 | Tee-Object -FilePath $LogPath
$Exit = $LASTEXITCODE
if ($Exit -ne 0) {
    throw "Compilation failed, exit=$Exit. Review $LogPath. Nothing was staged for installation."
}
$Dll = Join-Path $BuildDir 'CalradiaUnified.dll'
if (-not (Test-Path -LiteralPath $Dll)) {
    throw "Build returned success without expected DLL: $Dll. Nothing staged."
}
if ($RunPolicyTests) {
    $TestProject = Join-Path $Root 'Tests\PolicySmoke\PolicySmoke.csproj'
    & $Dotnet.Source run --project $TestProject --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Policy test run failed. Nothing was staged.' }
}
# Stage into a fresh output folder only AFTER successful compilation.
if (Test-Path -LiteralPath $Output) { Remove-Item -LiteralPath $Output -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $Output 'bin\Win64_Shipping_Client') | Out-Null
Copy-Item -LiteralPath (Join-Path $Root 'Modules\CalradiaUnified\SubModule.xml') -Destination $Output
New-Item -ItemType Directory -Force -Path (Join-Path $Output 'ModuleData') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $Root 'Modules\CalradiaUnified\ModuleData') -Force |
  Copy-Item -Destination (Join-Path $Output 'ModuleData') -Recurse
Copy-Item -LiteralPath $Dll -Destination (Join-Path $Output 'bin\Win64_Shipping_Client\CalradiaUnified.dll')
$Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $Output 'bin\Win64_Shipping_Client\CalradiaUnified.dll')).Hash
"Game=$Version`nDLL_SHA256=$Hash`nBuilt_UTC=$([DateTime]::UtcNow.ToString('o'))" | Set-Content -LiteralPath (Join-Path $LogsDir 'build-proof.txt')
Write-Host "Success: Output\Modules\CalradiaUnified"
Write-Host "Copy that ONE CalradiaUnified folder to your game's Modules directory."
