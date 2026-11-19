# Pure PE metadata inspector: does not load native game assemblies,
# so System.Numerics.Vectors reference resolution cannot conceal widget
# geometry property names. Run with PowerShell 7 on Windows CI.
param(
    [string]$OutputPath='BuildResults/native-layout-metadata-v157.txt'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=Join-Path $env:USERPROFILE '.nuget\packages'
$names=@('TaleWorlds.GauntletUI.dll',
    'TaleWorlds.Engine.GauntletUI.dll',
    'TaleWorlds.TwoDimension.dll')
$interesting=@(
    'Widget','BrushWidget','TextWidget','ButtonWidget',
    'EditableTextWidget','InputTextWidget','SliderWidget',
    'ImageWidget','ListPanel','GridWidget','WidgetContainer',
    'GauntletLayer'
)
$lines=New-Object 'System.Collections.Generic.List[string]'
$lines.Add('Exact source: restored Bannerlord.ReferenceAssemblies 1.5.7.125802')
$lines.Add('Inspector: PortableExecutable metadata, no executing/loading game DLLs')
$found=0
foreach($name in $names){
    $file=Get-ChildItem -LiteralPath $root -File -Filter $name -Recurse |
        Select-Object -First 1
    if($null -eq $file){
        $lines.Add('ASSEMBLY_NOT_FOUND '+$name)
        continue
    }
    $lines.Add('ASSEMBLY '+$file.FullName)
    $stream=[IO.File]::OpenRead($file.FullName)
    $pe=$null
    try{
        $pe=[System.Reflection.PortableExecutable.PEReader]::new($stream)
        $reader=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        foreach($handle in $reader.TypeDefinitions){
            $definition=$reader.GetTypeDefinition($handle)
            $typeName=$reader.GetString($definition.Name)
            if($interesting -notcontains $typeName){continue}
            $namespace=$reader.GetString($definition.Namespace)
            $found++
            $lines.Add('TYPE '+$namespace+'.'+$typeName)
            foreach($propHandle in $definition.GetProperties()){
                $p=$reader.GetPropertyDefinition($propHandle)
                $propName=$reader.GetString($p.Name)
                $access=$p.GetAccessors()
                $settable=-not $access.Setter.IsNil
                $lines.Add('PROPERTY '+$propName+' SETTER='+$settable)
            }
            foreach($fieldHandle in $definition.GetFields()){
                $p=$reader.GetFieldDefinition($fieldHandle)
                $fieldName=$reader.GetString($p.Name)
                if($fieldName -match 'Position|Margin|Actual|Scale|Parent|Offset|Size'){
                    $lines.Add('FIELD '+$fieldName)
                }
            }
        }
    }
    finally{
        if($null -ne $pe){$pe.Dispose()}
        $stream.Dispose()
    }
}
if($found -lt 3){throw 'No native Widget types found. Audit incomplete.'}
# The source relocation policy cannot run without precisely these native
# measured-geometry members. Fail CI on a wrong reference version/API.
foreach($required in @(
    'PROPERTY Size SETTER=True',
    'PROPERTY GlobalPosition SETTER=False',
    'PROPERTY PositionXOffset SETTER=True',
    'PROPERTY PositionYOffset SETTER=True',
    'PROPERTY ParentWidget SETTER=True'
)){
    if(-not $lines.Contains($required)){
        throw ('Native geometry contract missing: '+$required)
    }
}
$output=[IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output))|Out-Null
[IO.File]::WriteAllLines($output,$lines.ToArray(),[Text.Encoding]::UTF8)
Write-Output ('NATIVE_LAYOUT_METADATA_PASS widget_types='+$found+
    ' lines='+$lines.Count)
$lines|Where-Object{
    $_ -match '^TYPE ' -or
    $_ -match '^PROPERTY (Position|Margin|Actual|Global|Parent|Width|Height|Suggested|Horizontal|Vertical|Id|Name|Text)' -or
    $_ -match '^FIELD .* (Position|Offset|Margin)'
}|Select-Object -First 155
