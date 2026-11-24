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
  'TaleWorlds.ScreenSystem','TaleWorlds.CampaignSystem','TaleWorlds.Core','TaleWorlds.MountAndBlade')
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
# Reflection-only GetType on real widget classes resolves Vector2
# signatures eagerly. Windows PowerShell 5.1 requires explicitly
# preloading the .NET Framework vectors assembly.
try {
    if($script:referencePaths.ContainsKey('System.Numerics.Vectors')){
        [void][Reflection.Assembly]::ReflectionOnlyLoadFrom(
            $script:referencePaths['System.Numerics.Vectors'])
    } else {
        [void][Reflection.Assembly]::ReflectionOnlyLoad(
            'System.Numerics.Vectors, Version=4.1.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a')
    }
} catch {
    Write-Output ('WIDGET_LAYOUT_VECTOR_PREFETCH_UNAVAILABLE '+$_.Exception.Message)
}
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
        $selected=@($types|Where-Object{
            $_.Name -match '^(Widget|TextWidget|ButtonWidget|Brush|BrushLayer|BrushState|GauntletLayer|GauntletMovieIdentifier|Color|BannerEditorView|CharacterCreation.+View|CharacterCreationScreen|LocationComplex|LocationCharacter|Location|MBInformationManager|MultiSelectionInquiryData|InquiryElement|ImageIdentifier|CharacterImageIdentifier|CharacterCode|BannerImageIdentifier)$'
        })
        foreach($nativeType in @(
            'TaleWorlds.GauntletUI.BaseTypes.Widget',
            'TaleWorlds.GauntletUI.BaseTypes.TextWidget',
            'TaleWorlds.GauntletUI.BaseTypes.ButtonWidget',
            'TaleWorlds.Engine.GauntletUI.GauntletLayer',
            'SandBox.GauntletUI.BannerEditor.BannerEditorView'
        )){
            try{
                $target=$asm.GetType($nativeType,$false)
                if($target -and -not ($selected|Where-Object{
                    $_.FullName -eq $target.FullName})){
                    $selected+=@($target)
                }
            }catch{$lines.Add('TYPE_LOOKUP_FAILED '+$nativeType+
                ' ERROR '+$_.Exception.Message)}
        }
        $w=@($types|Where-Object{
            $_.Name -match '^(Widget|BrushWidget|TextWidget|ButtonWidget|ListPanel|GridWidget|EditableTextWidget)$'
        })
        foreach($widgetType in $w){
            $lines.Add('LAYOUT_WIDGET_FOUND '+$widgetType.FullName+
                ' ASSEMBLY '+$name)
        }
        foreach($type in $selected|Sort-Object FullName -Unique){
            $lines.Add('')
            $baseName=if($null -ne $type.BaseType){$type.BaseType.FullName}else{'none'}
            $lines.Add('TYPE '+$type.FullName+' BASE '+$baseName)
            foreach($c in $type.GetConstructors($flags)){
                try{$lines.Add('CTOR '+$c.ToString())}
                catch{$lines.Add('CTOR '+$c.Name+' [dependency unresolved]')}
            }
            foreach($p in $type.GetProperties(
                [Reflection.BindingFlags]'Public,NonPublic,Instance,Static')){
                try{$lines.Add('PROPERTY '+$p.ToString()+' CANWRITE='+$p.CanWrite)}
                catch{$lines.Add('PROPERTY '+$p.Name+' CANWRITE='+$p.CanWrite+' [dependency unresolved]')}
            }
            foreach($f in $type.GetFields($flags)){
                try{$lines.Add('FIELD '+$f.ToString())}
                catch{$lines.Add('FIELD '+$f.Name+' [dependency unresolved]')}
            }
            foreach($m in $type.GetMethods($flags)){
                if(-not $m.IsSpecialName){
                    try{$lines.Add('METHOD '+$m.ToString())}
                    catch{$lines.Add('METHOD '+$m.Name+' [dependency unresolved]')}
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
        $_ -match '^TYPE_LOOKUP_FAILED' -or
        $_ -match '^LAYOUT_WIDGET_FOUND' -or
        $_ -match '^PROPERTY .* (PositionXOffset|PositionYOffset|MarginTop|MarginBottom|MarginLeft|MarginRight|SuggestedWidth|SuggestedHeight|ActualWidth|ActualHeight|HorizontalAlignment|VerticalAlignment|WidthSizePolicy|HeightSizePolicy)' -or
        $_ -match '^TYPE .*\.(Widget|TextWidget|ButtonWidget|Brush|BrushLayer|GauntletLayer|GauntletMovieIdentifier|Color)' -or
        $_ -match '^PROPERTY .* (Color|Brush|Sprite|IsEnabled|IsVisible|Movie|RootWidget|FontSize)'
    }|Select-Object -First 100
} finally{
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
