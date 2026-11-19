# Read-only installed Bannerlord UI asset reference inventory.
# This collects text metadata, not textures, DLLs, proprietary XML or saves.
param(
    [string]$GameDir,
    [string]$OutFile,
    [switch]$SelfTest
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
function Inspect-Assets([string]$Root) {
    $modules=@('Native','SandBoxCore','SandBox','StoryMode')
    $sources=New-Object System.Collections.Generic.List[object]
    foreach($mod in $modules){
        $gui=Join-Path $Root ('Modules\'+$mod+'\GUI')
        if(-not (Test-Path -LiteralPath $gui -PathType Container)){continue}
        Get-ChildItem -LiteralPath $gui -Filter '*.xml' -Recurse -File -ErrorAction SilentlyContinue |
            ForEach-Object{
                # Limit to configuration XML, never touch texture archives.
                $text=[IO.File]::ReadAllText($_.FullName)
                $refs=@()
                foreach($token in @(
                    'character_creation_background_gradient',
                    'escape_panel',
                    'ButtonBrush',
                    'SliderWidget')){
                    if($text.IndexOf($token,[StringComparison]::OrdinalIgnoreCase) -ge 0){
                        $refs+= $token
                    }
                }
                if($refs.Count -gt 0){
                    $sources.Add([pscustomobject]@{
                        module=$mod
                        relative_path=$_.FullName.Substring($Root.TrimEnd('\').Length+1)
                        references=$refs
                    })
                }
            }
    }
    $items=@()
    foreach($token in @(
        'character_creation_background_gradient','escape_panel',
        'ButtonBrush','SliderWidget')){
        $matches=@($sources|Where-Object{$_.references -contains $token})
        $items+= [pscustomobject]@{
            token=$token
            readable_native_xml_refs=$matches.Count
            evidence=if($matches.Count -gt 0){'REFERENCED_IN_INSTALLED_XML'}else{'UNKNOWN_COMPILED_OR_MISSING'}
            sample_files=@($matches|Select-Object -First 5 -ExpandProperty relative_path)
        }
    }
    return [pscustomobject]@{
        collector='CU native creator asset inventory'
        utc=[DateTime]::UtcNow.ToString('o')
        game_root_found=(Test-Path -LiteralPath (Join-Path $Root 'Modules') -PathType Container)
        notes='No textures or native prefab contents copied. Missing readable XML does not prove a sprite is absent from compiled assets.'
        tokens=$items
    }
}
if($SelfTest){
    $sample=Join-Path ([IO.Path]::GetTempPath()) ('cu-native-art-'+[Guid]::NewGuid().ToString('N'))
    try{
        $dir=Join-Path $sample 'Modules\Native\GUI\Prefabs'
        [IO.Directory]::CreateDirectory($dir)|Out-Null
        [IO.File]::WriteAllText((Join-Path $dir 'CharacterCreationFixture.xml'),
            '<Prefab><Widget Sprite="General\CharacterCreation\character_creation_background_gradient"/><ButtonWidget Brush="ButtonBrush"/><SliderWidget/></Prefab>')
        $report=Inspect-Assets $sample
        if(@($report.tokens|Where-Object{
            $_.token -eq 'character_creation_background_gradient' -and
            $_.readable_native_xml_refs -gt 0}).Count -ne 1){
            throw 'Native asset collector fixture failed'
        }
        Write-Output 'PASS native creator asset collector self-test (fixture only)'
    }finally{
        if(Test-Path -LiteralPath $sample){
            Remove-Item -LiteralPath $sample -Recurse -Force
        }
    }
    return
}
if([string]::IsNullOrWhiteSpace($GameDir)){
    throw 'Specify -GameDir with the installed Bannerlord game root. This script does not guess a library path.'
}
$GameDir=[IO.Path]::GetFullPath($GameDir)
if(-not(Test-Path -LiteralPath (Join-Path $GameDir 'Modules') -PathType Container)){
    throw 'GameDir does not contain a Modules folder.'
}
$report=Inspect-Assets $GameDir
$json=$report|ConvertTo-Json -Depth 7
if([string]::IsNullOrWhiteSpace($OutFile)){
    $OutFile=Join-Path (Get-Location) 'CU-Native-Creator-Art-Audit.json'
}
$path=[IO.Path]::GetFullPath($OutFile)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))|Out-Null
[IO.File]::WriteAllText($path,$json,[Text.Encoding]::UTF8)
Write-Output ('NATIVE_ASSET_AUDIT_REPORT '+$path)
$report.tokens|ForEach-Object{
    Write-Output ($_.token+': '+$_.evidence+' (XML refs '+$_.readable_native_xml_refs+')')
}
