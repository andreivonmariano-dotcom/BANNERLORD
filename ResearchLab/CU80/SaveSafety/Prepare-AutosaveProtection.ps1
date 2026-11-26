$ErrorActionPreference='Stop'
$r=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab'
$o=Join-Path $r 'Simulator\ValidationRuns\CU80-L005'
$doc=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord'
$config=Join-Path $doc 'Configs\BannerlordConfig.txt'
function Hash($p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
function Closed{if(@(Get-Process|Where-Object{$_.ProcessName -match 'Bannerlord|TaleWorlds|MountAndBlade'}).Count -ne 0){throw 'game open'}}
Closed
$old=[IO.File]::ReadAllBytes($config)
# Latin-1 roundtrip preserves every byte outside the single ASCII setting.
$enc=[Text.Encoding]::GetEncoding(28591)
$s=$enc.GetString($old)
$m=[regex]::Matches($s,'(?m)^AutoSaveInterval=30(?=\r?$)')
if($m.Count -ne 1 -or [regex]::Matches($s,'(?m)^AutoSaveInterval=').Count -ne 1){throw 'unexpected configuration'}
$new=$enc.GetBytes($s.Substring(0,$m[0].Index)+'AutoSaveInterval=-1'+$s.Substring($m[0].Index+$m[0].Length))
$backup=Join-Path $o 'BannerlordConfig.autosave.original.private.txt'
$stage=Join-Path (Split-Path $config) 'cu80-config-stage.tmp'
$prev=Join-Path (Split-Path $config) 'cu80-config-previous.tmp'
foreach($p in @($backup,$stage,$prev)){if(Test-Path $p){throw 'target exists'}}
$before=Hash $config
[IO.File]::Copy($config,$backup,$false)
if((Hash $backup) -ne $before){throw 'backup mismatch'}
[IO.File]::WriteAllBytes($stage,$new)
$expected=Hash $stage
Closed
if((Hash $config) -ne $before){throw 'config changed'}
[IO.File]::Replace($stage,$config,$prev)
if((Hash $config) -ne $expected -or (Hash $prev) -ne $before){throw 'replace mismatch'}
if($enc.GetString([IO.File]::ReadAllBytes($config)) -cne ($s.Substring(0,$m[0].Index)+'AutoSaveInterval=-1'+$s.Substring($m[0].Index+$m[0].Length))){throw 'byte verification'}
Remove-Item -LiteralPath $prev
'CONFIG_BEFORE '+$before
'CONFIG_AFTER '+$expected
'SETTING AutoSaveInterval=-1'
# Create an independent offline fixture directory; no redirection of actual game save path.
$sv=Join-Path $doc 'Game Saves'
$lab=Join-Path $o 'isolated-save-fixture'
if(Test-Path $lab){throw 'fixture exists'}
New-Item $lab -ItemType Directory|Out-Null
$checkpoints=@(Get-ChildItem $sv -Filter 'labd002r*.sav' -File)
if($checkpoints.Count -ne 3){throw 'checkpoint count'}
foreach($f in $checkpoints){$dest=Join-Path $lab $f.Name;[IO.File]::Copy($f.FullName,$dest,$false);if((Hash $dest) -ne (Hash $f.FullName)){throw 'copy mismatch'}}
$fixture=Join-Path $lab 'saveauto2.sav'
[IO.File]::WriteAllText($fixture,'synthetic rotation test only')
[IO.File]::WriteAllText($fixture,'synthetic second rotation')
if((Hash (Join-Path $sv 'saveauto2.sav')) -ne 'B214449172C6EC2EB3C5D6AD3690B41CF2319DF34C518BAB50D43ACF604F1E17'){throw 'live original changed'}
$records=ConvertFrom-Json -InputObject (Get-Content (Join-Path $r 'ControlCampaigns\D002-R-activation\save-manifest.private.json') -Raw)
$ok=0;foreach($rec in $records){if((Hash (Join-Path $sv $rec.name)) -eq $rec.sha256){$ok++}}
if($ok -ne 43){throw 'originals mismatch'}
'ORIGINALS '+$ok+'/43'
'LAB_FIXTURE 3 copied checkpoints; synthetic rotation did not touch live directory'
'RUNTIME_ISOLATION NOT_RUN'
