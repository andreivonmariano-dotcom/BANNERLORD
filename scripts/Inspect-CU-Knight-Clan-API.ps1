# Metadata-only examination of the exact v1.5.7.125802 native references.
# Never attempts to move a player hero or modify campaign state.
param([string] $OutputPath='BuildResults/knight-clan-api-v157.txt')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -eq 'Core'){throw 'Windows PowerShell 5.1 required'}
$assets=Join-Path $PSScriptRoot '..\Source\CalradiaUnified\obj\project.assets.json'
if(!(Test-Path $assets) -or !([IO.File]::ReadAllText((Resolve-Path $assets).Path).Contains('Bannerlord.ReferenceAssemblies/1.5.7.125802'))){
    throw 'Exact pinned game references not restored'
}
$root=Join-Path $env:USERPROFILE '.nuget\packages'
$script:map=@{}
Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.dll' | ForEach-Object {
    $key=[IO.Path]::GetFileNameWithoutExtension($_.Name)
    if(!$script:map.ContainsKey($key)){$script:map[$key]=$_.FullName}
}
$resolve=[ResolveEventHandler]{
    param($sender,$args)
    $key=$args.Name.Split(',')[0]
    if($script:map.ContainsKey($key)){
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom($script:map[$key])
    }
    try {return [Reflection.Assembly]::ReflectionOnlyLoad($args.Name)}
    catch {return $null}
}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($resolve)
try{
    $assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($script:map['TaleWorlds.CampaignSystem'])
    $flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
    $lines=New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('Pinned Bannerlord 1.5.7.125802 | Knight clan and native oath capability')
    $lines.Add('Metadata only; not a safe main-player native clan-join demonstration')
    $targetTypes=@('TaleWorlds.CampaignSystem.Hero','TaleWorlds.CampaignSystem.Clan',
        'TaleWorlds.CampaignSystem.Campaign','TaleWorlds.CampaignSystem.Party.MobileParty',
        'TaleWorlds.CampaignSystem.Settlements.Settlement')
    foreach($name in $targetTypes){
        $type=$assembly.GetType($name,$false)
        $lines.Add('TYPE '+$name+' found='+($null -ne $type))
        if($null -eq $type){continue}
        foreach($prop in $type.GetProperties($flags)){
            if($prop.Name -match '^(Clan|PlayerClan|Leader|CurrentSettlement|PartyBelongedTo|Father|Mother|Children|All|IsPrisoner|IsActive|IsAlive|IsPlayerCompanion|MainParty)$'){
                $setter=if($prop.GetSetMethod($true)){$prop.GetSetMethod($true).ToString()}else{'NO SETTER'}
                $lines.Add('  PROPERTY '+$prop.ToString()+' | setter='+$setter)
            }
        }
        foreach($method in $type.GetMethods($flags)){
            if(!$method.IsSpecialName -and $method.Name -match '(Clan|Party|Adopt|Join|Member|SetLeader)'){
                $lines.Add('  METHOD '+$method.ToString())
            }
        }
    }
    $actionNames=@('ChangeClanAction','ChangeClanLeaderAction',
        'AdoptHeroAction','AddCompanionAction','MarriageAction',
        'ChangeKingdomAction','ChangePlayerCharacterAction')
    foreach($name in $actionNames){
        $full='TaleWorlds.CampaignSystem.Actions.'+$name
        $type=$assembly.GetType($full,$false)
        $lines.Add('ACTION '+$full+' found='+($null -ne $type))
        if($null -eq $type){continue}
        foreach($method in $type.GetMethods($flags)){
            if(!$method.IsSpecialName){$lines.Add('  METHOD '+$method.ToString())}
        }
    }
    $out=[IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($out))|Out-Null
    [IO.File]::WriteAllLines($out,$lines.ToArray(),[Text.Encoding]::UTF8)
    $lines | Where-Object {$_ -match '^ACTION|^TYPE|  PROPERTY .*Clan|  PROPERTY .*CurrentSettlement'} |
        Select-Object -First 60
    Write-Output ('KNIGHT_API_AUDIT_PASS lines='+$lines.Count)
}finally{
    [AppDomain]::CurrentDomain.remove_ReflectionOnlyAssemblyResolve($resolve)
}
