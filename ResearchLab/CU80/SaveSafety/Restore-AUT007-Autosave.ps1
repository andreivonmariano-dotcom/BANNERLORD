param([switch]$Restore)
$ErrorActionPreference='Stop'
$r=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab'
$sv=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord\Game Saves'
$archive=Join-Path $r 'ControlCampaigns\D002-R-activation'
$o=Join-Path $r 'Simulator\ValidationRuns\AUT007'
$expected='B214449172C6EC2EB3C5D6AD3690B41CF2319DF34C518BAB50D43ACF604F1E17'
$modified='C117E2118F80826CEFFCCF5B7A4495D65D634AC00485FBDF588DC02BC08102AA'
function Hash($path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
function Closed {if(@(Get-Process|Where-Object{$_.ProcessName -match 'Bannerlord|TaleWorlds|MountAndBlade'}).Count -ne 0){throw 'game/launcher not closed'}}
Closed
$records=ConvertFrom-Json -InputObject (Get-Content (Join-Path $archive 'save-manifest.private.json') -Raw)
if($records.Count -ne 43){throw 'manifest count'}
$live=Join-Path $sv 'saveauto2.sav';$original=Join-Path $archive 'Game Saves\saveauto2.sav';$research=Join-Path $o 'saveauto2.changed.private.sav'
if((Hash $original) -ne $expected -or (Hash $research) -ne $modified -or (Hash $live) -ne $modified){throw 'pinned versions mismatch'}
$bad=@();foreach($rec in $records){if((Hash (Join-Path $archive ('Game Saves\'+$rec.name))) -ne $rec.sha256){throw 'backup mismatch'};if((Hash (Join-Path $sv $rec.name)) -ne $rec.sha256){$bad+=$rec.name}}
if($bad.Count -ne 1 -or $bad[0] -ne 'saveauto2.sav'){throw 'unexpected originals mismatch'}
$lab=@{};Get-ChildItem $sv -Filter 'labd002r*.sav' -File|ForEach-Object{$lab[$_.Name]=Hash $_.FullName}
if($lab.Count -ne 3){throw 'research checkpoints'}
if(@(Get-ChildItem $sv -Filter '*.sav' -File).Count -ne 46){throw 'save count'}
'PRECHECK PASS original backups43, changed copy, three lab checkpoints'
if(!$Restore){return}
$staged=Join-Path $sv 'cu80-recovery-stage.tmp'
$replaced=Join-Path $sv 'cu80-recovery-replaced.tmp'
if((Test-Path $staged) -or (Test-Path $replaced)){throw 'recovery temp exists'}
[IO.File]::Copy($original,$staged,$false)
if((Hash $staged) -ne $expected){throw 'stage mismatch'}
Closed
if((Hash $live) -ne $modified){throw 'live changed'}
[IO.File]::Replace($staged,$live,$replaced)
if((Hash $live) -ne $expected -or (Hash $replaced) -ne $modified){throw 'replace verify failed; preserve temps'}
$extra=Join-Path $o 'saveauto2.replaced.private.sav'
if(Test-Path $extra){throw 'extra backup exists'}
[IO.File]::Copy($replaced,$extra,$false)
if((Hash $extra) -ne $modified){throw 'extra backup mismatch'}
Remove-Item -LiteralPath $replaced
Closed
$ok=0;foreach($rec in $records){if((Hash (Join-Path $sv $rec.name)) -eq $rec.sha256){$ok++}}
if($ok -ne 43){throw 'final originals mismatch'}
foreach($n in $lab.Keys){if((Hash (Join-Path $sv $n)) -ne $lab[$n]){throw 'lab mismatch'}}
'RESTORED '+(Hash $live)
'ORIGINALS '+$ok+'/43'
'LAB '+$lab.Count+'/3'
'TOTAL '+@(Get-ChildItem $sv -Filter '*.sav' -File).Count
'CHANGED_COPY '+(Hash $extra)
