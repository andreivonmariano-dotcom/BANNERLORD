param([ValidateSet('Activate','Restore')][string]$Mode)
$ErrorActionPreference='Stop'
$r=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab'
$o=Join-Path $r 'Simulator\ValidationRuns\CU80-L007'
$doc=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord'
$live=Join-Path $doc 'Game Saves'
$vault=Join-Path $o 'original-save-vault'
$stage=Join-Path $o 'lab-save-stage'
$returned=Join-Path $o 'lab-save-returned'
$manifest=Join-Path $o 'all-original-files.private.json'
function Closed{if(@(Get-Process|Where-Object{$_.ProcessName -match 'Bannerlord|TaleWorlds|MountAndBlade'}).Count -ne 0){throw 'game open'}}
function Hash($p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
function Plain($p){$d=Get-Item -LiteralPath $p -Force;if($d.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'reparse point'};if($d.PSIsContainer){foreach($f in Get-ChildItem -LiteralPath $p -Force){if($f.PSIsContainer -or ($f.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'unexpected directory or reparse'}}}}
function Verify($dir,$m){Plain $dir;if(@(Get-ChildItem -LiteralPath $dir -Force).Count -ne $m.Count){throw 'inventory mismatch'};foreach($f in $m){if((Hash (Join-Path $dir $f.name)) -ne $f.sha256){throw 'hash mismatch'}}}
Closed
if($Mode -eq 'Activate'){
  foreach($p in @($vault,$stage,$returned,$manifest)){if(Test-Path -LiteralPath $p){throw 'prior state exists'}}
  if([IO.Path]::GetPathRoot($live) -ne [IO.Path]::GetPathRoot($vault)){throw 'not same volume'}
  Plain $live
  $orig=ConvertFrom-Json -InputObject (Get-Content (Join-Path $r 'ControlCampaigns\D002-R-activation\save-manifest.private.json') -Raw)
  if($orig.Count -ne 43){throw 'original manifest count'}
  foreach($f in $orig){if((Hash (Join-Path $live $f.name)) -ne $f.sha256){throw 'personal original mismatch'}}
  $lab=@{'labd002rrecruit.sav'='A9535052BF487F6106D2223CA3C42D6215B91E97D81786EF60B32DFF3A1AE1A0';'labd002rstart.sav'='D740BCD873E9E91A66DD693DD0F1059319A5A70CB2CF05227168E497461D35EA';'labd002rthree.sav'='E704F14FB4AA0B75E871E3B4BC8F99585405C9CD78196D28A3900326FFBFE652'}
  $all=@(Get-ChildItem -LiteralPath $live -Force|ForEach-Object{[pscustomobject]@{name=$_.Name;sha256=Hash $_.FullName}})
  if($all.Count -ne 47 -or (Hash (Join-Path $live 'steam_autocloud.vdf')) -ne 'DEA880B611B3960B968733DF83EF988F9B857E17CC5432119D370BD7714B5E47'){throw 'unexpected save inventory'}
  foreach($n in $lab.Keys){if((Hash (Join-Path $live $n)) -ne $lab[$n]){throw 'checkpoint mismatch'}}
  New-Item -Path $stage -ItemType Directory|Out-Null
  foreach($n in $lab.Keys){[IO.File]::Copy((Join-Path $live $n),(Join-Path $stage $n),$false)}
  $lm=@($lab.Keys|ForEach-Object{[pscustomobject]@{name=$_;sha256=$lab[$_]}})
  Verify $stage $lm
  [IO.File]::WriteAllText($manifest,($all|ConvertTo-Json -Depth 3),(New-Object Text.UTF8Encoding($false)))
  Closed;Verify $live $all
  [IO.Directory]::Move($live,$vault)
  try{[IO.Directory]::Move($stage,$live);Verify $vault $all;Verify $live $lm}
  catch{if((Test-Path -LiteralPath $live) -and !(Test-Path -LiteralPath $stage)){[IO.Directory]::Move($live,$stage)};if(!(Test-Path -LiteralPath $live)){[IO.Directory]::Move($vault,$live)};throw}
  'VAULT_VERIFIED 47/47'
  'ACTIVE_LAB 3/3'
  'ACTUAL_RUNTIME_PATH NOT_RUN'
}else{
  $all=ConvertFrom-Json -InputObject (Get-Content -LiteralPath $manifest -Raw)
  Verify $vault $all
  if(Test-Path -LiteralPath $returned){throw 'returned path exists'}
  Plain $live
  Closed
  [IO.Directory]::Move($live,$returned)
  try{[IO.Directory]::Move($vault,$live);Verify $live $all}
  catch{if(!(Test-Path -LiteralPath $live)){[IO.Directory]::Move($returned,$live)};throw}
  'ORIGINAL_NAMESPACE_RESTORED 47/47'
  'LAB_RETURNED_PRESERVED'
}
