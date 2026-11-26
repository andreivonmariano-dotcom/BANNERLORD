param([ValidateSet('Activate','Restore')][string]$Mode)
$ErrorActionPreference='Stop'
$r=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab'
$o=Join-Path $r 'Simulator\ValidationRuns\CU80-L007'
$g='D:\Steam\steamapps\common\Mount & Blade II Bannerlord'
$live=Join-Path $g 'Modules\CalradiaUnified'
$new=Join-Path $o 'candidate\Modules\CalradiaUnified'
$old=Join-Path $o 'production-module-private'
$doc=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord'
$ld=Join-Path $doc 'Configs\LauncherData.xml'
$ldBackup=Join-Path $o 'LauncherData.before-candidate.private.xml'
$selectedBefore='Bannerlord.Harmony|Bannerlord.ButterLib|Bannerlord.UIExtenderEx|Bannerlord.MBOptionScreen|Native|SandBoxCore|Sandbox|CustomBattle|StoryMode|BirthAndDeath|NavalDLC|CUNativeObservation'
$selectedAfter=$selectedBefore.Replace('|CUNativeObservation','|CalradiaUnified|CUNativeObservation')
$paths=@('bin\Win64_Shipping_Client\CalradiaUnified.dll','ModuleData\CU-BuildIdentity.json')
function Closed{if(@(Get-Process|Where-Object{$_.ProcessName -match 'Bannerlord|TaleWorlds|MountAndBlade'}).Count -ne 0){throw 'game open'}}
function Hash($p){(Get-FileHash -LiteralPath $p).Hash}
function Selection($p){[xml]$x=Get-Content -LiteralPath $p -Raw;($x.SelectNodes('//UserModData')|Where-Object{$_.IsSelected -eq 'true'}|ForEach-Object{$_.Id}) -join '|'}
function Inventory($a,$b){$aa=@(Get-ChildItem -LiteralPath $a -Recurse -File -Force);$bb=@(Get-ChildItem -LiteralPath $b -Recurse -File -Force);if($aa.Count -ne 25 -or $bb.Count -ne 25){throw 'module file count'};foreach($f in $aa){$rel=$f.FullName.Substring($a.Length+1);if((Hash $f.FullName) -ne (Hash (Join-Path $b $rel))){throw 'module file mismatch'}}}
function Replace($source,$target){
  $stage=$target+'.cu80stage';$prev=$target+'.cu80prev'
  foreach($p in @($stage,$prev)){if(Test-Path -LiteralPath $p){throw 'existing staging path'}}
  [IO.File]::Copy($source,$stage,$false)
  if((Hash $stage) -ne (Hash $source)){throw 'stage mismatch'}
  Closed
  [IO.File]::Replace($stage,$target,$prev)
  if((Hash $target) -ne (Hash $source)){throw 'target mismatch'}
  Remove-Item -LiteralPath $prev
}
Closed
if($Mode -eq 'Activate'){
  if((Hash (Join-Path $live $paths[0])) -ne 'E6BCBDE295ECF0527EC97B0C4805D11916A28BD92EE099F18D838DA68E6D0BBA' -or (Hash (Join-Path $new $paths[0])) -ne '30CD4C8C638432C574A5F35BF10053694718937E15D2C8C3CD8B3CE4E714AC79'){throw 'dll pins'}
  if((Selection $ld) -ne $selectedBefore -or (Hash $ld) -ne '79A4F07D8F3F39667DFC23742FB56A2143B79AA4B9765364D0B6FAA5AF2EA84F'){throw 'launcher pin'}
  if(Test-Path $old){throw 'backup exists'}
  $a=@(Get-ChildItem $live -Recurse -File -Force);$b=@(Get-ChildItem $new -Recurse -File -Force)
  if($a.Count -ne 25 -or $b.Count -ne 25){throw 'inventory count'}
  foreach($f in $a){$rel=$f.FullName.Substring($live.Length+1);if($rel -notin $paths -and (Hash $f.FullName) -ne (Hash (Join-Path $new $rel))){throw 'unexpected difference'}}
  Copy-Item -LiteralPath $live -Destination $old -Recurse
  Inventory $live $old
  [IO.File]::Copy($ld,$ldBackup,$false)
  [xml]$x=Get-Content $ld -Raw
  $nodes=@($x.SelectNodes('//UserModData')|Where-Object{$_.Id -eq 'CalradiaUnified'})
  if($nodes.Count -ne 1 -or $nodes[0].IsSelected -ne 'false'){throw 'CU selection'}
  $nodes[0].IsSelected='true'
  $stageLd=Join-Path $o 'LauncherData.candidate.xml'
  $x.Save($stageLd)
  if((Selection $stageLd) -ne $selectedAfter){throw 'candidate order'}
  try{
    foreach($rel in $paths){Replace (Join-Path $new $rel) (Join-Path $live $rel)}
    Inventory $live $new
    Replace $stageLd $ld
    if((Selection $ld) -ne $selectedAfter){throw 'selection verification'}
  }catch{
    foreach($rel in $paths){if((Hash (Join-Path $live $rel)) -ne (Hash (Join-Path $old $rel))){Replace (Join-Path $old $rel) (Join-Path $live $rel)}}
    if((Hash $ld) -ne (Hash $ldBackup)){Replace $ldBackup $ld}
    throw
  }
  'CANDIDATE_MODULE 25/25'
  'CANDIDATE_DLL '+(Hash (Join-Path $live $paths[0]))
  'PROFILE '+(Selection $ld)
}else{
  if((Hash (Join-Path $old $paths[0])) -ne 'E6BCBDE295ECF0527EC97B0C4805D11916A28BD92EE099F18D838DA68E6D0BBA'){throw 'backup dll'}
  if((Hash $ldBackup) -ne '79A4F07D8F3F39667DFC23742FB56A2143B79AA4B9765364D0B6FAA5AF2EA84F'){throw 'backup launcher'}
  foreach($rel in $paths){if((Hash (Join-Path $live $rel)) -ne (Hash (Join-Path $new $rel))){throw 'candidate altered'}}
  foreach($rel in $paths){Replace (Join-Path $old $rel) (Join-Path $live $rel)}
  Inventory $live $old
  Replace $ldBackup $ld
  'PRODUCTION_MODULE_RESTORED 25/25'
  'PROFILE '+(Selection $ld)
}
