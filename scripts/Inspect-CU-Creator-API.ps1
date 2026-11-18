# Windows PowerShell 5.1 metadata-only inspection. This never launches Bannerlord.
# Run after restoring Bannerlord.ReferenceAssemblies 1.5.7.125802.
param(
    [string] $ReferenceVersion = '1.5.7.125802',
    [string] $OutputPath = 'BuildResults/character-creation-api-v157.txt'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    throw 'ReflectionOnlyLoadFrom requires Windows PowerShell 5.1.'
}
$root = Join-Path $env:USERPROFILE ('.nuget\packages\bannerlord.referenceassemblies\' + $ReferenceVersion)
if (-not (Test-Path -LiteralPath $root)) {
    throw ('Pinned Bannerlord reference package is missing: ' + $root)
}
$script:referencePaths = @{}
Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.dll' | ForEach-Object {
    $key = [IO.Path]::GetFileNameWithoutExtension($_.Name)
    if (-not $script:referencePaths.ContainsKey($key)) {
        $script:referencePaths[$key] = $_.FullName
    }
}
$assemblies = @(
    'TaleWorlds.CampaignSystem',
    'SandBox',
    'SandBox.View',
    'SandBox.GauntletUI',
    'TaleWorlds.Core',
    'TaleWorlds.Core.ViewModelCollection',
    'TaleWorlds.ScreenSystem'
)
foreach ($name in $assemblies) {
    if (-not $script:referencePaths.ContainsKey($name)) {
        throw ('Required game reference not found: ' + $name)
    }
}
$resolver = [ResolveEventHandler] {
    param($sender, $eventArgs)
    $simpleName = $eventArgs.Name.Split(',')[0]
    if ($script:referencePaths.ContainsKey($simpleName)) {
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom(
            $script:referencePaths[$simpleName])
    }
    try { return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name) }
    catch { return $null }
}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolver)
try {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('Calradia Unified native creator metadata audit')
    $lines.Add('Exact pinned reference package: ' + $ReferenceVersion)
    $lines.Add('Source: local NuGet reference assemblies, not a running game')
    $lines.Add('Stage view constructors and inherited registration attributes require live validation.')
    $flags = [Reflection.BindingFlags] 'Public,NonPublic,Instance,Static,DeclaredOnly'
    $seen = 0
    foreach ($name in $assemblies) {
        $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom(
            $script:referencePaths[$name])
        $lines.Add('')
        $lines.Add('ASSEMBLY ' + $name + ' | ' + $assembly.GetName().Version)
        try { $types = @($assembly.GetTypes()) }
        catch [Reflection.ReflectionTypeLoadException] {
            $types = @($_.Exception.Types | Where-Object { $null -ne $_ })
            $lines.Add('NOTE: partial type load, using the discoverable metadata types')
        }
        foreach ($type in ($types | Where-Object {
            $_.FullName -match '^TaleWorlds\.CampaignSystem\.CharacterCreationContent\.' -or
            $_.FullName -match '^SandBox\.View\.CharacterCreation\.' -or
            $_.FullName -match '^SandBox\.GauntletUI\.CharacterCreation\.' -or
            $_.FullName -match '^SandBox\.(SandBoxGameManager|SandBoxViewSubModule)$'
        } | Sort-Object FullName)) {
            $seen++
            $baseTypeName = if ($type.BaseType) { $type.BaseType.FullName } else { '(none)' }
            $lines.Add('')
            $lines.Add('TYPE ' + $type.FullName + ' | base=' + $baseTypeName)
            try {
                foreach ($attribute in $type.GetCustomAttributesData()) {
                    if ($attribute.AttributeType.Name -match 'CharacterCreationStageView') {
                        $lines.Add('  STAGE VIEW ATTRIBUTE ' + $attribute.ToString())
                    }
                }
            } catch { $lines.Add('  NOTE: attributes unreadable: ' + $_.Exception.Message) }
            try {
                foreach ($ctor in $type.GetConstructors($flags)) {
                    $lines.Add('  CTOR ' + $ctor.ToString())
                }
            } catch { $lines.Add('  NOTE: constructor metadata unavailable: ' + $_.Exception.Message) }
            try {
                foreach ($property in $type.GetProperties($flags)) {
                    $lines.Add('  PROPERTY ' + $property.ToString())
                }
            } catch { $lines.Add('  NOTE: property metadata unavailable: ' + $_.Exception.Message) }
            try {
                foreach ($method in $type.GetMethods($flags)) {
                    if (-not $method.IsSpecialName) {
                        $visibility = if ($method.IsPublic) { 'public' } elseif (
                            $method.IsFamily) { 'protected' } else { 'nonpublic' }
                        $lines.Add('  METHOD ' + $visibility + ' ' + $method.ToString())
                    }
                }
            } catch { $lines.Add('  NOTE: method metadata unavailable: ' + $_.Exception.Message) }
        }
    }
    if ($seen -lt 8) {
        throw ('Only ' + $seen + ' creator-related types found; the audit is incomplete.')
    }
    $out = [IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($out)) | Out-Null
    [IO.File]::WriteAllLines($out, $lines.ToArray(), [Text.Encoding]::UTF8)
    Write-Output ('CREATOR_API_AUDIT_PASS types=' + $seen +
        ' lines=' + $lines.Count + ' package=' + $ReferenceVersion)
    Write-Output ('CREATOR_API_AUDIT_REPORT ' + $out)
    # Print only the critical discovery names; full metadata stays in the build artifact.
    $lines | Where-Object {
        $_ -match '^TYPE .*CharacterCreation(Manager|State|Screen|FaceGeneratorView|CultureStageView|ReviewStageView|StageViewBase)' -or
        $_ -match '^  STAGE VIEW ATTRIBUTE'
    } | Select-Object -First 35
}
finally {
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolver)
}
