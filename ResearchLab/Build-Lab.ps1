[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot)
$src=[IO.Path]::GetFullPath($PSScriptRoot)
if($src.StartsWith($root.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or $src -eq $root){throw 'Build outside the game directory.'}
$bin=Join-Path $root 'bin\Win64_Shipping_Client'
$campaign=Join-Path $bin 'TaleWorlds.CampaignSystem.dll'
if((Get-FileHash -LiteralPath $campaign -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'd7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462'){throw 'Unverified native CampaignSystem fingerprint; re-research before compiling.'}
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $csc)){throw 'Windows Framework C# compiler not available.'}
$build=Join-Path $src 'build'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$smoke=Join-Path $build 'TransferPolicySmoke.exe'
& $csc /nologo /target:exe /warnaserror+ ("/out:"+$smoke) (Join-Path $src 'TransferPolicy.cs') (Join-Path $src 'TransferPolicySmoke.cs')
if($LASTEXITCODE -ne 0){throw 'Policy compile failed.'}
& $smoke
if($LASTEXITCODE -ne 0){throw 'Policy tests failed.'}
$mod=Join-Path $build 'Modules\CUResearchLab'
$out=Join-Path $mod 'bin\Win64_Shipping_Client'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$dll=Join-Path $out 'CUResearchLab.dll'
$refs=@('TaleWorlds.CampaignSystem','TaleWorlds.Core','TaleWorlds.MountAndBlade','TaleWorlds.Library','TaleWorlds.ObjectSystem','TaleWorlds.Localization','TaleWorlds.DotNet') | ForEach-Object {"/reference:"+(Join-Path $bin ($_+'.dll'))}
$facade=Join-Path $bin 'mono\lib\mono\4.7.2-api\Facades\netstandard.dll'
if(-not(Test-Path -LiteralPath $facade)){throw 'Installed game netstandard 2.0 facade not found.'}
$refs+=('/reference:'+$facade)
& $csc /nologo /target:library /warnaserror+ /optimize+ ("/out:"+$dll) @refs (Join-Path $src 'TransferPolicy.cs') (Join-Path $src 'NativeProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Native module compile failed.'}
Copy-Item -LiteralPath (Join-Path $src 'SubModule.xml') -Destination $mod -Force
Write-Output ('LAB COMPILED_UNTESTED SHA256 '+(Get-FileHash -LiteralPath $dll).Hash.ToLowerInvariant())
Write-Output ('Staged only: '+$mod)
