# Windows PowerShell 5.1 metadata-only audit of native Gauntlet widget
# styling affordances and game-owned stage view private layer handles.
# Refuse to infer UI runtime from an unrelated Bannerlord version.
param(
  [string]$ReferenceVersion='1.5.7.125802',
  [string]$OutputPath='BuildResults/native-widget-api-v157.txt'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -eq 'Core'){
    throw 'ReflectionOnlyLoadFrom requires Windows PowerShell 5.1'
}
$assets=Join-Path $PSScriptRoot '..\Source\CalradiaUnified\obj\project.assets.json'
$text=[IO.File]::ReadAllText([IO.Path]::GetFullPath($assets))
if(-not $text.Contains('Bannerlord.ReferenceAssemblies/'+$ReferenceVersion)){
    throw 'Wrong reference assembly version'
}
$root=Join-Path $env:USERPROFILE '.nuget\packages'
$script:referencePaths=@{}
Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.dll' | ForEach-Object {
    $key=[IO.Path]::GetFileNameWithoutExtension($_.Name)
    if(-not $script:referencePaths.ContainsKey($key)){
        $script:referencePaths[$key]=$_.FullName
    }
}
$names=@('TaleWorlds.GauntletUI','TaleWorlds.Engine.GauntletUI',
  'TaleWorlds.Library','SandBox.GauntletUI','SandBox.View',
  'TaleWorlds.GauntletUI.PrefabSystem','TaleWorlds.Engine',
  'TaleWorlds.ScreenSystem')
$resolver=[ResolveEventHandler]{
    param($sender,$eventArgs)
    $name=$eventArgs.Name.Split(',')[0]
    if($script:referencePaths.ContainsKey($name)){
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom(
            $script:referencePaths[$name])
    }
    try{return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)}
    catch{return $null}
}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolver)
try{
    $lines=New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('PINNED '+$ReferenceVersion)
    $flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
    foreach($name in $names){
        if(-not $script:referencePaths.ContainsKey($name)){continue}
        $asm=[Reflection.Assembly]::ReflectionOnlyLoadFrom(
            $script:referencePaths[$name])
        try{$types=@($asm.GetTypes())}
        catch [Reflection.ReflectionTypeLoadException]{
            $types=@($_.Exception.Types|Where-Object{$null -ne $_})
        }
        foreach($type in $types|Where-Object{
            $_.Name -match '^(Widget|TextWidget|ButtonWidget|Brush|BrushLayer|BrushState|GauntletLayer|GauntletMovieIdentifier|Color|CharacterCreation.+View|CharacterCreationScreen)$'
        }|Sort-Object FullName){
            $lines.Add('')
            $lines.Add('TYPE '+$type.FullName+' BASE '+$type.BaseType.FullName)
            foreach($c in $type.GetConstructors($flags)){
                $lines.Add('CTOR '+$c.ToString())
            }
            foreach($p in $type.GetProperties($flags)){
                $lines.Add('PROPERTY '+$p.ToString()+' CANWRITE='+$p.CanWrite)
            }
            foreach($f in $type.GetFields($flags)){
                $lines.Add('FIELD '+$f.ToString())
            }
            foreach($m in $type.GetMethods($flags)){
                if(-not $m.IsSpecialName){
                    $lines.Add('METHOD '+$m.ToString())
                }
            }
        }
    }
    $out=[IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($out))|Out-Null
    [IO.File]::WriteAllLines($out,$lines.ToArray(),[Text.Encoding]::UTF8)
    Write-Output ('WIDGET_API_AUDIT_PASS lines='+$lines.Count+
        ' version='+$ReferenceVersion)
    Write-Output ('WIDGET_API_AUDIT_REPORT '+$out)
    $lines|Where-Object{
        $_ -match '^TYPE .*\.(Widget|TextWidget|ButtonWidget|Brush|BrushLayer|GauntletLayer|GauntletMovieIdentifier|Color)' -or
        $_ -match '^PROPERTY .* (Color|Brush|Sprite|IsEnabled|IsVisible|Movie|RootWidget|FontSize)'
    }|Select-Object -First 100
} finally{
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
