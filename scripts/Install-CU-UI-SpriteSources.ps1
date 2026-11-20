<#
.SYNOPSIS
  Stage the approved v0.59 UI PNGs for the official Bannerlord sprite importer.
.DESCRIPTION
  Does not forge a .tpac. Only the matching Windows Bannerlord Modding Kit
  can generate/import the engine texture. The game's live buttons, Age slider
  and all native creators are untouched by this script.
.EXAMPLE
  .\scripts\Install-CU-UI-SpriteSources.ps1 -GameRoot 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord' -SourceZip '.\CalradiaUnified_v059b_Art_Import_Sources.zip' -RunGenerator
.EXAMPLE
  .\scripts\Install-CU-UI-SpriteSources.ps1 -GameRoot 'C:\Games\Mount & Blade II Bannerlord' -VerifyOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [string]$SourceZip = '.\CalradiaUnified_v059b_Art_Import_Sources.zip',
    [switch]$RunGenerator,
    [switch]$VerifyOnly
)
$ErrorActionPreference='Stop'
$game=(Resolve-Path -LiteralPath $GameRoot).Path
$module=Join-Path $game 'Modules\CalradiaUnified'
if(-not (Test-Path -LiteralPath (Join-Path $module 'SubModule.xml') -PathType Leaf)){
    throw "CalradiaUnified module was not found under $module"
}
$parts=Join-Path $module 'GUI\SpriteParts'
$config=Join-Path $parts 'Config.xml'
$category='ui_calradiaunified'
$categoryRoot=Join-Path $parts $category
$spriteData=Join-Path $module 'GUI\CalradiaUnifiedSpriteData.xml'
$atlasDir=Join-Path $module 'Assets\GauntletUI'
$atlasPattern='ui_calradiaunified_*_tex.tpac'

if(-not $VerifyOnly){
    $zip=(Resolve-Path -LiteralPath $SourceZip).Path
    $scratch=Join-Path ([IO.Path]::GetTempPath()) ('cu-ui-sprites-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $scratch | Out-Null
    try {
        Expand-Archive -LiteralPath $zip -DestinationPath $scratch -Force
        $manifestPath=Join-Path $scratch 'SPRITE_MANIFEST.json'
        if(-not (Test-Path -LiteralPath $manifestPath)){throw 'Missing source manifest'}
        $manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if($manifest.category -ne $category -or [int]$manifest.count -ne 19){
            throw 'Unexpected sprite category or source PNG count'
        }
        $src=Join-Path $scratch 'Modules\CalradiaUnified\GUI\SpriteParts'
        if(-not (Test-Path (Join-Path $src 'Config.xml'))){throw 'Missing source category config'}
        foreach($item in $manifest.sprites){
            $name=[string]$item.filename
            if($name -notmatch '^cu_[a-z0-9_]+\.png$'){throw "Unexpected sprite filename: $name"}
            $input=Join-Path (Join-Path $src $category) $name
            if(-not (Test-Path -LiteralPath $input -PathType Leaf)){throw "Missing $name"}
            $hash=(Get-FileHash -LiteralPath $input -Algorithm SHA256).Hash.ToLowerInvariant()
            if($hash -ne ([string]$item.sha256).ToLowerInvariant()){
                throw "Sprite checksum mismatch: $name"
            }
        }
        New-Item -ItemType Directory -Force -Path $categoryRoot | Out-Null
        foreach($item in $manifest.sprites){
            Copy-Item -LiteralPath (Join-Path (Join-Path $src $category) $item.filename) -Destination (Join-Path $categoryRoot $item.filename) -Force
        }
        Copy-Item -LiteralPath (Join-Path $src 'Config.xml') -Destination $config -Force
        Write-Host "PASS: 19 checked PNG sources and AlwaysLoad Config.xml installed under $categoryRoot"
    }
    finally {
        if(Test-Path -LiteralPath $scratch){Remove-Item -LiteralPath $scratch -Recurse -Force}
    }
    if($RunGenerator){
        $editor=Join-Path $game 'bin\Win64_Shipping_wEditor'
        $exe=Join-Path $editor 'TaleWorlds.TwoDimension.SpriteSheetGenerator.exe'
        if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){
            throw "Matching Bannerlord Modding Kit sprite generator missing: $exe"
        }
        Write-Host 'Launching official sprite sheet generator. Respond to its completion prompt.'
        $p=Start-Process -FilePath $exe -WorkingDirectory $editor -Wait -PassThru
        if($p.ExitCode -ne 0){throw "Sprite generator exit code: $($p.ExitCode)"}
    }
}
if(Test-Path -LiteralPath $spriteData){
    $txt=Get-Content -LiteralPath $spriteData -Raw
    if($txt -notmatch 'ui_calradiaunified' -or $txt -notmatch 'cu_right_stone_panel' -or $txt -notmatch 'cu_world_illustration'){
        throw 'SpriteData exists but lacks the CU category or required art names'
    }
    Write-Host "PASS: sprite metadata at $spriteData"
}
else {
    if($VerifyOnly){throw 'SpriteData missing. Run the official SpriteSheetGenerator.exe first.'}
    Write-Warning 'Sprite metadata not baked yet. Run the official generator in Win64_Shipping_wEditor.'
}
$atlas=@(Get-ChildItem -LiteralPath $atlasDir -Filter $atlasPattern -File -ErrorAction SilentlyContinue)
if($atlas.Count -eq 0){
    if($VerifyOnly){throw 'Imported .tpac missing. Import ui_calradiaunified in the Modding Kit Resource Browser.'}
    Write-Warning 'Next required step: launch the Bannerlord Modding Kit, press Alt+grave, enter resource.show_resource_browser, select CalradiaUnified / GauntletUI, Scan new asset files, then Import ui_calradiaunified.'
}
else {
    foreach($a in $atlas){
        if($a.Length -lt 1024){throw "Unexpectedly small sprite atlas: $($a.FullName)"}
        Write-Host "PASS: imported engine sprite atlas $($a.FullName) ($($a.Length) bytes)"
    }
}
if($VerifyOnly -and $atlas.Count -gt 0){
    Write-Host 'PASS: CU UI art category has the expected metadata and imported engine texture'
}
