[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\');$out=[IO.Path]::GetFullPath($Output)
if($out.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase) -or $out -eq $root -or (Test-Path $out)){throw 'New output outside game required'}
New-Item -ItemType Directory -Path $out -Force|Out-Null
$bin=Join-Path $root 'bin\Win64_Shipping_Client'
# Fresh process; official assemblies only; no methods or static properties invoked.
Get-ChildItem $bin -Filter 'TaleWorlds*.dll'|ForEach-Object{try{[void][Reflection.Assembly]::LoadFrom($_.FullName)}catch{}}
foreach($m in 'Native','SandBox','StoryMode','CustomBattle','BirthAndDeath','NavalDLC'){ $d=Join-Path $root ('Modules\'+$m+'\bin\Win64_Shipping_Client');if(Test-Path $d){Get-ChildItem $d -Filter '*.dll'|ForEach-Object{try{[void][Reflection.Assembly]::LoadFrom($_.FullName)}catch{}}}}
$files=@('TaleWorlds.CampaignSystem','TaleWorlds.CampaignSystem.ViewModelCollection','TaleWorlds.SaveSystem','TaleWorlds.Core','TaleWorlds.Library','TaleWorlds.ObjectSystem','TaleWorlds.MountAndBlade','TaleWorlds.Engine','TaleWorlds.GauntletUI','TaleWorlds.ModuleManager','TaleWorlds.NavigationSystem')|ForEach-Object {Join-Path $bin ($_+'.dll')}
$files+=@('Modules\SandBox\bin\Win64_Shipping_Client\SandBox.dll','Modules\SandBox\bin\Win64_Shipping_Client\SandBox.ViewModelCollection.dll','Modules\NavalDLC\bin\Win64_Shipping_Client\NavalDLC.dll','Modules\NavalDLC\bin\Win64_Shipping_Client\NavalDLC.ViewModelCollection.dll','Modules\StoryMode\bin\Win64_Shipping_Client\StoryMode.dll')|ForEach-Object{Join-Path $root $_}
$f=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
$enc=New-Object Text.UTF8Encoding($false)
$writer=New-Object IO.StreamWriter((Join-Path $out 'members.jsonl'),$false,$enc)
$summary=@()
function Emit($o){$writer.WriteLine(($o|ConvertTo-Json -Compress -Depth 4))}
try{foreach($file in $files){$name=[IO.Path]::GetFileNameWithoutExtension($file);$counts=@{types=0;methods=0;properties=0;events=0;fields=0;errors=0;missing_types=0}
try{$a=[Reflection.Assembly]::LoadFrom($file);$hash=(Get-FileHash -LiteralPath $file).Hash;Emit @{kind='assembly';assembly=$name;sha256=$hash;version=$a.GetName().Version.ToString();dependencies=@($a.GetReferencedAssemblies()|ForEach-Object FullName)}
try{$types=$a.GetTypes()}catch [Reflection.ReflectionTypeLoadException]{$types=@($_.Exception.Types|Where-Object {$_});$counts.missing_types=@($_.Exception.Types|Where-Object {!$_}).Count}
foreach($t in $types){try{Emit @{kind='type';assembly=$name;type=$t.FullName;base=if($t.BaseType){$t.BaseType.FullName}else{''};public=($t.IsPublic -or $t.IsNestedPublic)};$counts.types++
foreach($m in @($t.GetMethods($f))+@($t.GetConstructors($f))){Emit @{kind='method';assembly=$name;type=$t.FullName;name=$m.Name;signature=$m.ToString();public=$m.IsPublic;static=$m.IsStatic};$counts.methods++}
foreach($m in $t.GetProperties($f)){Emit @{kind='property';assembly=$name;type=$t.FullName;name=$m.Name;signature=$m.ToString()};$counts.properties++}
foreach($m in $t.GetEvents($f)){Emit @{kind='event';assembly=$name;type=$t.FullName;name=$m.Name;signature=$m.ToString()};$counts.events++}
foreach($m in $t.GetFields($f)){Emit @{kind='field';assembly=$name;type=$t.FullName;name=$m.Name;signature=$m.ToString();public=$m.IsPublic};$counts.fields++}
}catch{$counts.errors++;Emit @{kind='error';assembly=$name;type=$t.FullName;message=$_.Exception.GetType().Name}}}
$status='indexed'
}catch{$status='error';$counts.errors++;Emit @{kind='error';assembly=$name;message=$_.Exception.GetType().Name}}
$r=[pscustomobject]@{assembly=$name;status=$status;types=$counts.types;methods=$counts.methods;properties=$counts.properties;events=$counts.events;fields=$counts.fields;missing_types=$counts.missing_types;errors=$counts.errors};$summary+=$r;$r|ConvertTo-Json -Compress
}}finally{$writer.Dispose()}
$summary|Export-Csv -LiteralPath (Join-Path $out 'summary.csv') -NoTypeInformation -Encoding UTF8
'INDEX SHA '+(Get-FileHash (Join-Path $out 'members.jsonl')).Hash
'ASSEMBLY_INDEX_COMPLETE'
