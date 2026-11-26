[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference='Stop'
$bin=Join-Path $GameRoot 'bin\Win64_Shipping_Client'
# Run in a fresh Windows PowerShell process. Never invoke game methods or load a save.
Get-ChildItem -LiteralPath $bin -Filter 'TaleWorlds*.dll' | ForEach-Object {try{[void][Reflection.Assembly]::LoadFrom($_.FullName)}catch{}}
foreach($name in 'CampaignSystem','Core','MountAndBlade','CampaignSystem.ViewModelCollection','GauntletUI','SaveSystem','Library'){
    $path=Join-Path $bin ('TaleWorlds.'+$name+'.dll')
    if(Test-Path -LiteralPath $path){Write-Output ($name+' '+(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant())}
}
$a=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'TaleWorlds.CampaignSystem.dll'))
$flags=[Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly'
foreach($name in 'CampaignEvents','Actions.ChangeGovernorAction','Actions.GiveGoldAction','Actions.ChangeRelationAction','Actions.MarriageAction','Actions.AddHeroToPartyAction','Actions.ChangeOwnerOfSettlementAction','Party.MobileParty','Hero','Settlements.Town','CampaignBehaviorBase','IDataStore','CampaignGameStarter','Clan','Settlements.Settlement','Settlements.Fief','PartyRole'){
    $t=$a.GetType('TaleWorlds.CampaignSystem.'+$name)
    if(!$t){throw ('Missing type: '+$name)}
    Write-Output ('TYPE '+$t.FullName)
    if($t.IsEnum){[Enum]::GetNames($t);continue}
    if($name -eq 'CampaignEvents'){
        $t.GetProperties($flags)|Where-Object {$_.Name -match 'GivenBirth|ComesOfAge|Married|HeroKilled|DailyTick|SettlementEntered|SettlementLeft|Governor|GameLoaded|SessionLaunched|GameLoadFinished|PartyRole'}|ForEach-Object {$_.Name+' : '+$_.PropertyType.ToString()}
    }else{
        $t.GetMethods($flags)|Where-Object {!$_.IsSpecialName -and $_.Name -match '^Apply|SetHeroPartyRole|CanAssign|GetEffective|GetHeroPartyRole|^SyncData|^RegisterEvents|^AddDialogLine|^AddPlayerLine|^AddGameMenuOption|^GetRelation|^GetTraitLevel'}|ForEach-Object {$_.ToString()}
        $t.GetProperties()|Where-Object {$_.Name -match '^(Spouse|Children|Father|Mother|Age|IsAlive|IsActive|IsPrisoner|Clan|CurrentSettlement|PartyBelongedTo|GovernorOf|Gold|Governor|FoodStocks|FoodChange|GarrisonParty|Security|Loyalty|Prosperity|OwnerClan|Fiefs|Settlements|PlayerClan|IsTown|IsCastle|IsUnderSiege|EffectiveQuartermaster|EffectiveScout|EffectiveSurgeon|EffectiveEngineer)$'}|ForEach-Object {$_.ToString()+' public_set='+($_.GetSetMethod() -ne $null)}
    }
}
