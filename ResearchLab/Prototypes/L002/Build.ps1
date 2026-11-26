[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot)
$src=[IO.Path]::GetFullPath($PSScriptRoot)
if($src.StartsWith($root.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or $src -eq $root){throw 'Build outside game directory.'}
$bin=Join-Path $root 'bin\Win64_Shipping_Client'
if((Get-FileHash (Join-Path $bin 'TaleWorlds.CampaignSystem.dll')).Hash.ToLowerInvariant() -ne 'd7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462'){throw 'Unverified CampaignSystem fingerprint'}
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$mod=Join-Path $src 'build\Modules\CUGovernorResearch'
$out=Join-Path $mod 'bin\Win64_Shipping_Client'
New-Item -ItemType Directory -Path $out -Force|Out-Null
$dll=Join-Path $out 'CUGovernorResearch.dll'
$refs=@('TaleWorlds.CampaignSystem','TaleWorlds.Core','TaleWorlds.MountAndBlade','TaleWorlds.Library','TaleWorlds.ObjectSystem','TaleWorlds.Localization','TaleWorlds.DotNet')|ForEach-Object {'/reference:'+(Join-Path $bin ($_+'.dll'))}
$refs+=('/reference:'+(Join-Path $bin 'mono\lib\mono\4.7.2-api\Facades\netstandard.dll'))
& $csc /nologo /target:library /warnaserror+ /optimize+ ("/out:"+$dll) @refs (Join-Path $src 'GovernorProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Native module compile failed'}
Copy-Item -LiteralPath (Join-Path $src 'SubModule.xml') -Destination $mod -Force
'LAB COMPILED_UNTESTED SHA256 '+(Get-FileHash $dll).Hash.ToLowerInvariant()
'STAGED '+$mod
