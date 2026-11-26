[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot,[switch]$FingerprintBinaries)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
if(!(Test-Path -LiteralPath (Join-Path $root 'bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll'))){throw 'Not a verified Bannerlord root'}
$out=Join-Path $env:LOCALAPPDATA ('BannerlordResearchLab\Simulator\Inventory\'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
if($out.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be outside game directory'}
if(Test-Path -LiteralPath $out){throw 'Output exists'}
New-Item -ItemType Directory -Path $out|Out-Null
$errors=@();$links=0;$stack=New-Object 'System.Collections.Generic.Stack[string]';$stack.Push($root)
$rows=New-Object 'System.Collections.Generic.List[object]'
while($stack.Count){$dir=$stack.Pop();try{$entries=Get-ChildItem -LiteralPath $dir -Force -ErrorAction Stop}catch{$errors+=($dir+' '+$_.Exception.GetType().Name);continue}
foreach($e in $entries){$rel=$e.FullName.Substring($root.Length+1);if($e.Attributes -band [IO.FileAttributes]::ReparsePoint){$links++;continue};if($e.PSIsContainer){$stack.Push($e.FullName);continue}
$ext=$e.Extension.ToLowerInvariant();$category=switch -Regex ($ext){'^\.(dll|exe)$'{'binary';break};'^\.(xml|xsd|json|ini|cfg|config|csv|txt|md|lua|cs|xaml|yaml|yml)$'{'text_data';break};'^\.(tpac|dds|png|jpg|jpeg|tga|bmp|wem|bnk|ogg|wav|mp3|fbx|obj|mesh|anim)$'{'asset';break};default{'other'}}
$module=if($rel -match '^Modules\\([^\\]+)\\'){$Matches[1]}else{''}
$rows.Add([pscustomobject]@{Path=$rel;Bytes=$e.Length;Extension=$ext;Category=$category;Module=$module;LastWriteUtc=$e.LastWriteTimeUtc.ToString('o')})}}
$csv=Join-Path $out 'files.csv';$rows|Sort-Object Path|Export-Csv -LiteralPath $csv -NoTypeInformation -Encoding UTF8
'INVENTORY '+$out;'FILES '+$rows.Count;'BYTES '+($rows|Measure-Object Bytes -Sum).Sum;'SKIPPED_REPARSE '+$links;'ERRORS '+$errors.Count;'CSV SHA '+(Get-FileHash $csv).Hash
$rows|Group-Object Category|ForEach-Object{'CATEGORY '+$_.Name+' '+$_.Count+' '+($_.Group|Measure-Object Bytes -Sum).Sum}
$rows|Group-Object Extension|Sort-Object Count -Descending|Select-Object -First 20|ForEach-Object{'EXT '+$_.Name+' '+$_.Count}
'MODULE DIRECTORIES '+@($rows|Where-Object Module|Group-Object Module).Count
if($FingerprintBinaries){
$managed=0;$native=0;$unknown=0;$index=0
$binaryRows=foreach($r in $rows|Where-Object Category -eq 'binary'){ $p=Join-Path $root $r.Path; $kind='unknown';$name='';$ver='';try{$a=[Reflection.AssemblyName]::GetAssemblyName($p);$kind='managed';$name=$a.Name;$ver=$a.Version.ToString();$managed++}catch [System.BadImageFormatException]{$kind='nonmanaged_or_invalid';$native++}catch{$unknown++};try{$hash=(Get-FileHash -LiteralPath $p).Hash}catch{$hash='ERROR';$errors+=($r.Path+' hash')};$index++;if($index%250 -eq 0){Write-Host "Binary scan $index"};[pscustomobject]@{Path=$r.Path;Kind=$kind;Assembly=$name;Version=$ver;SHA256=$hash}}
$binaryRows|Export-Csv -LiteralPath (Join-Path $out 'binaries.csv') -NoTypeInformation -Encoding UTF8
'MANAGED '+$managed+' NONMANAGED_OR_INVALID '+$native+' UNKNOWN '+$unknown
'BINARY HASH ERRORS '+@($binaryRows|Where-Object SHA256 -eq 'ERROR').Count
}
$manifest=foreach($r in $rows|Where-Object {$_.Path -match '^Modules\\[^\\]+\\SubModule\.xml$'}){try{$settings=New-Object Xml.XmlReaderSettings;$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$settings.XmlResolver=$null;$reader=[Xml.XmlReader]::Create((Join-Path $root $r.Path),$settings);try{$x=New-Object Xml.XmlDocument;$x.XmlResolver=$null;$x.Load($reader)}finally{$reader.Dispose()};[pscustomobject]@{Path=$r.Path;Id=$x.Module.Id.value;Version=$x.Module.Version.value;Status='parsed'}}catch{[pscustomobject]@{Path=$r.Path;Id='';Version='';Status='error'}}}
$manifest|Export-Csv -LiteralPath (Join-Path $out 'modules.csv') -NoTypeInformation -Encoding UTF8
'MANIFESTS '+@($manifest).Count+' PARSED '+@($manifest|Where-Object Status -eq 'parsed').Count
'ERRORS FINAL '+$errors.Count
[IO.File]::WriteAllLines((Join-Path $out 'errors.txt'),[string[]]$errors)
'INVENTORY_COMPLETE'
