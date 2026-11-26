$ErrorActionPreference='Stop'
$r=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab'
$stage=Join-Path $r 'Simulator\ValidationRuns\D002-role-preflight\build\Modules\CUNativeObservation'
$b=Join-Path $r 'ControlCampaigns\D002-R-activation'
function Get-ResearchHash($p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Assert-Research($ok,$msg){if(!$ok){throw $msg}}
function Closed(){Assert-Research (@(Get-Process -Name 'Bannerlord*','TaleWorlds*' -ErrorAction SilentlyContinue).Count -eq 0) 'game/launcher running'}
Closed
$steam=(Get-ItemProperty 'HKCU:\Software\Valve\Steam').SteamPath
$paths=@($steam)
$paths+=@(Select-String -Path (Join-Path $steam 'steamapps\libraryfolders.vdf') -Pattern '"path"\s+"([^"]+)"' | ForEach-Object {$_.Matches[0].Groups[1].Value.Replace('\\','\')})
$roots=@($paths|ForEach-Object {[IO.Path]::GetFullPath((Join-Path $_ 'steamapps\common\Mount & Blade II Bannerlord')).ToLowerInvariant()}|Where-Object {Test-Path (Join-Path $_ 'bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll')}|Select-Object -Unique)
Assert-Research ($roots.Count -eq 1) 'ambiguous root'
$g=$roots[0];$doc=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord'
$ld=Join-Path $doc 'Configs\LauncherData.xml';$saves=Join-Path $doc 'Game Saves'
$dest=Join-Path $g 'Modules\CUNativeObservation';$rel='bin\Win64_Shipping_Client\CUNativeObservation.dll'
$old='6ef3d9764ef72f8f2dc66310dc6074a15baf7bee83ccd58b4bc83a26e722dca1'
$new='40af4c6970ef300224c85d7e2d5ad3139616852436eb8f5de0e0eb2f5efd3506'
$cu='e6bcbde295ecf0527ec97b0c4805d11916a28bd92ee099f18d838da68e6d0bba'
$dll='635441668173993d4c5e86319d9ed7a5da026572cc57ac87c818fe6d0a5f6db9'
$manifest='b1b3e2130a81c596a8be41a8f3cc22256e48c5430cbcc3f413b70bed117f1fda'
$cupath=Join-Path $g 'Modules\CalradiaUnified\bin\Win64_Shipping_Client\CalradiaUnified.dll'
$staged=Join-Path $b 'LauncherData.observer12.STAGED.xml'
$backup=Join-Path $b 'LauncherData.control11.original.xml'
$full=Join-Path $r 'ControlCampaigns\D001\LauncherData.full41.original.xml'
Assert-Research ((Get-ResearchHash $full) -eq '08f6d29e6ab3c31d85ed988005cf0d21667601a8ac15e8d8f693c7073d236513') 'full profile backup'
Assert-Research ((Get-ResearchHash (Join-Path $g 'bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll')) -eq 'd7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462') 'campaign hash'
Assert-Research ((Get-ResearchHash (Join-Path $g 'Modules\NavalDLC\bin\Win64_Shipping_Client\NavalDLC.dll')) -eq '6294d1bd181c4a7366d4bafca4fbf446a1e9523512a33ecb3d818aa19c8ccb45') 'naval hash'
Assert-Research ((Get-ResearchHash $cupath) -eq $cu) 'CU hash'
Assert-Research ((Get-ResearchHash $ld) -eq $old -and (Get-ResearchHash $backup) -eq $old -and (Get-ResearchHash $staged) -eq $new) 'profile hash'
Assert-Research ((Get-ResearchHash (Join-Path $stage $rel)) -eq $dll -and (Get-ResearchHash (Join-Path $stage 'SubModule.xml')) -eq $manifest) 'stage hash'
Assert-Research (@(Get-ChildItem $stage -File -Recurse).Count -eq 2 -and !(Test-Path $dest)) 'stage or destination'
[xml]$a=Get-Content -LiteralPath $ld -Raw;[xml]$x=Get-Content -LiteralPath $staged -Raw
$nodes=@($x.SelectNodes('//UserModData'))
Assert-Research ($nodes.Count -eq 42 -and $nodes[-1].Id -eq 'CUNativeObservation' -and $nodes[-1].LastKnownVersion -eq 'v0.0.3.1' -and $nodes[-1].IsSelected -eq 'true') 'new node'
$selected=@($nodes|Where-Object {$_.IsSelected -eq 'true'}|ForEach-Object {$_.Id})
Assert-Research ($selected.Count -eq 12 -and $selected -notcontains 'CalradiaUnified') 'selection'
[void]$nodes[-1].ParentNode.RemoveChild($nodes[-1]);Assert-Research ($a.OuterXml -ceq $x.OuterXml) 'unrelated XML changes'
$records=ConvertFrom-Json -InputObject (Get-Content -LiteralPath (Join-Path $b 'save-manifest.private.json') -Raw)
Assert-Research ($records.Count -eq 43 -and @(Get-ChildItem $saves -File -Filter '*.sav').Count -eq 43) 'save count'
foreach($rec in $records){Assert-Research ((Get-ResearchHash (Join-Path $saves $rec.name)) -eq $rec.sha256 -and (Get-ResearchHash (Join-Path (Join-Path $b 'Game Saves') $rec.name)) -eq $rec.sha256) 'save backup verification'}
Closed;Assert-Research ((Get-ResearchHash $ld) -eq $old -and !(Test-Path $dest)) 'concurrent edit'
$installed=$false;$activated=$false
try{
 [IO.Directory]::CreateDirectory($dest)|Out-Null;$installed=$true
 New-Item (Join-Path $dest 'bin\Win64_Shipping_Client') -ItemType Directory -Force|Out-Null
 Copy-Item -LiteralPath (Join-Path $stage 'SubModule.xml') -Destination (Join-Path $dest 'SubModule.xml')
 Copy-Item -LiteralPath (Join-Path $stage $rel) -Destination (Join-Path $dest $rel)
 Assert-Research ((Get-ResearchHash (Join-Path $dest $rel)) -eq $dll -and (Get-ResearchHash (Join-Path $dest 'SubModule.xml')) -eq $manifest) 'installed hash'
 Closed; Assert-Research ((Get-ResearchHash $ld) -eq $old) 'launcher changed'
 $tmp=$ld+'.D002-R.tmp';Assert-Research (!(Test-Path $tmp)) 'temp exists'
 Copy-Item -LiteralPath $staged -Destination $tmp
 [IO.File]::Replace($tmp,$ld,(Join-Path $b 'LauncherData.activation.rollback.xml'));$activated=$true
 Assert-Research ((Get-ResearchHash $ld) -eq $new -and (Get-ResearchHash $cupath) -eq $cu) 'activation or CU mismatch'
 foreach($rec in $records){Assert-Research ((Get-ResearchHash (Join-Path $saves $rec.name)) -eq $rec.sha256) 'original save changed'}
 Assert-Research (@(Get-ChildItem $saves -File -Filter '*.sav').Count -eq 43) 'save count changed'
 'ACTIVATED '+(Get-ResearchHash $ld)
 'INSTALLED_DLL '+(Get-ResearchHash (Join-Path $dest $rel))
 'ORIGINAL_SAVES_UNCHANGED '+$records.Count
 'SELECTED '+($selected -join '|')
 'ROLLBACK '+(Get-ResearchHash (Join-Path $b 'LauncherData.activation.rollback.xml'))
 'UTC '+[DateTime]::UtcNow.ToString('o')
}catch{
 if($activated -and (Get-ResearchHash $ld) -eq $new){$restore=$ld+'.D002-R.restore';Copy-Item -LiteralPath $backup -Destination $restore;[IO.File]::Replace($restore,$ld,(Join-Path $b 'LauncherData.failed.xml'))}
 if($installed -and (Test-Path $dest)){Move-Item -LiteralPath $dest -Destination (Join-Path $b 'quarantined-new-module')}
 throw
}