<#
.SYNOPSIS
  Audit the installed Native Review prefab and its exact brushes.
.DESCRIPTION
  Read-only. The v0.59f build draws the same native panel, title and
  footer via assets already installed with Bannerlord 1.5.7.125802.
  This checks the user's installed Native XML, not only NuGet metadata.
  It does not copy or redistribute TaleWorlds' files.
.EXAMPLE
  .\scripts\Check-CU-NativeReview-Installed.ps1 -GameRoot 'C:\Games\Mount & Blade II Bannerlord'
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$GameRoot,
  [string]$OutputJson = '.\BuildResults\installed-review-shell-audit.json'
)
$ErrorActionPreference='Stop'
$game=(Resolve-Path -LiteralPath $GameRoot).Path
$source=Join-Path $game 'Modules\Native\GUI\Prefabs\CharacterCreation\CharacterCreationReviewStage.xml'
if(-not (Test-Path -LiteralPath $source -PathType Leaf)){
  throw "Native Review prefab missing: $source"
}
[xml]$doc=Get-Content -LiteralPath $source -Raw -Encoding UTF8
$panel=@($doc.SelectNodes("//Widget[@Id='RightPanel']"))
if($panel.Count -ne 1){throw "Expected one native RightPanel, found $($panel.Count)"}
$attributes=@{
  SuggestedWidth='694'
  SuggestedHeight='1080'
  Sprite='General\CharacterCreation\character_creation_panel'
  HorizontalAlignment='Right'
  VerticalAlignment='Center'
}
foreach($k in $attributes.Keys){
  $actual=$panel[0].GetAttribute($k)
  if($actual -cne $attributes[$k]){
    throw "Installed Review changed: RightPanel.$k = '$actual', expected '$($attributes[$k])'"
  }
}
$title=@($panel[0].SelectNodes(".//TextWidget[@Brush='Stage.Title.Text'][@Text='@Title']"))
$inner=@($panel[0].SelectNodes(".//Widget[@Id='InnerPanel']"))
$back=@($panel[0].SelectNodes(".//ButtonWidget[@Brush='Standard.BackButton']"))
$next=@($panel[0].SelectNodes(".//ButtonWidget[@Brush='Popup.Done.Button.NineGrid']"))
if($title.Count -ne 1 -or $inner.Count -ne 1 -or $back.Count -ne 1 -or $next.Count -ne 1){
  throw "Native shell components do not match pinned Review (header $($title.Count), inner $($inner.Count), back $($back.Count), next $($next.Count))"
}
$wantInner=@{SuggestedWidth='573';SuggestedHeight='750';PositionXOffset='23';PositionYOffset='37'}
foreach($k in $wantInner.Keys){
  if($inner[0].GetAttribute($k) -cne $wantInner[$k]){
    throw "Native InnerPanel.$k differs from reference"
  }
}
if($back[0].GetAttribute('Command.Click') -ne 'OnPreviousStage' -or
   $next[0].GetAttribute('Command.Click') -ne 'OnNextStage'){
  throw 'Native Review navigation actions changed'
}
$brushNames=@('Stage.Title.Text','Stage.Description.Text',
  'Standard.BackButton','Popup.Done.Button.NineGrid','Popup.Button.Text')
$brushFiles=@()
foreach($module in @('Native','SandBoxCore','SandBox')){
  $brushRoot=Join-Path $game "Modules\$module\GUI\Brushes"
  if(Test-Path -LiteralPath $brushRoot){
    $brushFiles+=@(Get-ChildItem -LiteralPath $brushRoot -Recurse -Filter '*.xml' -File)
  }
}
$found=@{}
foreach($file in $brushFiles){
  try{
    [xml]$brushDoc=Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach($brush in $brushDoc.SelectNodes('//Brush[@Name]')){
      $name=$brush.GetAttribute('Name')
      if($name -in $brushNames){
        $found[$name]=[PSCustomObject]@{
          name=$name
          definition=$file.FullName.Substring($game.Length).TrimStart('\')
        }
      }
    }
  }
  catch{
    Write-Warning "Skipping unreadable brush file $($file.FullName)"
  }
}
$missing=@($brushNames|Where-Object{-not $found.ContainsKey($_)})
if($missing.Count -gt 0){
  Write-Warning "Native brush declarations not resolved in base modules: $($missing -join ', ')"
}
$report=[ordered]@{
  game_version_expected='1.5.7.125802'
  source=$source
  source_sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
  panel=$attributes
  inner=$wantInner
  title_brush='Stage.Title.Text'
  back_brush='Standard.BackButton'
  next_brush='Popup.Done.Button.NineGrid'
  missing_brushes=$missing
  resolved_brushes=@($found.Values|Sort-Object name)
  passed=($missing.Count -eq 0)
  note='Read-only audit of installed TaleWorlds native GUI; no textures or native prefab copied'
}
$dest=[IO.Path]::GetFullPath($OutputJson)
New-Item -Force -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest))|Out-Null
$report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $dest -Encoding UTF8
Write-Output "Saved $dest"
if($missing.Count -gt 0){
  Write-Warning "Found native frame, but $($missing.Count) brush definitions remain unresolved. Inspect the installed GUI/Brushes."
}
else{
  Write-Output 'PASS: installed Native Review geometry and all required brush references match v0.59f'
}
