#!/usr/bin/env python3
"""Build gate for required BUTR 3.0.3, native screen mixins, bindings and installer.

Source checks cannot establish that native Clan/Party/Kingdom movie XPaths
match the exact game at runtime, so live acceptance remains mandatory.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
source=root/"Source"/"CalradiaUnified"
module=ET.parse(root/"Modules"/"CalradiaUnified"/"SubModule.xml").getroot()
declared={e.get("Id") for e in module.find("DependedModules")}
required={
  "Bannerlord.Harmony","Bannerlord.ButterLib",
  "Bannerlord.UIExtenderEx","Bannerlord.MBOptionScreen"}
assert required <= declared
assert len(required & declared)==4
metadata=module.find("DependedModuleMetadatas")
assert metadata is not None
order={e.get("id"):e.get("order") for e in metadata}
assert all(order.get(dep)=="LoadBeforeThis" for dep in required),order
csproj=(source/"CalradiaUnified.csproj").read_text()
assert 'Include="Bannerlord.UIExtenderEx" Version="3.0.3"' in csproj
assert 'Include="Bannerlord.UIExtenderEx.Analyzers"' in csproj
assert 'ExcludeAssets="runtime"' in csproj
assert 'Include="Bannerlord.UIExtenderEx.Analyzers" Version="3.0.3.9"' in csproj
assert 'Include="Bannerlord.MCM" Version="5.13.1"' in csproj
core=(source/"SubModule.cs").read_text()
assert 'UIExtender.Create("CalradiaUnified")' in core
assert 'Extender.Register(typeof(SubModule).Assembly);' in core
assert 'Extender.Enable();' in core
assert 'try\n            {\n                Extender.Register' not in core
adapter=(source/"CUBUTRNativeScreens.cs").read_text()
for movie,vm,prop in (
    ("Clan","ClanManagementVM","CUClanSummary"),
    ("Party","PartyVM","CUPartySummary"),
    ("Kingdom","KingdomManagementVM","CUKingdomSummary")):
    assert '[PrefabExtension("'+movie+'", "descendant::Window/Children")]' in adapter
    assert 'BaseViewModelMixin<'+vm+'>' in adapter
    assert '[DataSourceProperty]' in adapter
    assert prop in adapter
assert adapter.count('[ViewModelMixin(handleDerived: true)]')==3
assert adapter.count('PrefabExtensionText')==4
assert 'ExecuteCUCamp()' in adapter
assert 'Command.Click=\\\"ExecuteCUCamp' in adapter
assert adapter.count('ExecuteCUDossier()')==3
assert 'Command.Click=\\\"ExecuteCUDossier' in adapter
assert '"ButtonBrush"' in adapter or 'Brush=\\\"ButtonBrush' in adapter
native=(source/"SandboxOriginBehavior.NativeHouseholdUI.cs").read_text()
assert 'Campaign.Current' in native
assert 'GetCampaignBehavior<SandboxOriginBehavior>()' in native
assert 'ShowStartingHouseholdStatus();' in native
assert 'Hero.MainHero' in native and 'Clan.PlayerClan' in native
# All seven starts have a saved role-backed, readable dossier.
for part in ('roleEconomy.Started','roleEconomy.CareerStatus',
             'roleEconomy.VerifiedExternalDailyWage',
             'Wanderer','Outlaw','Mercenary','Soldier',
             'knightOath.IsConfigured','lordInheritance.IsRecorded',
             'kingSuccession.IsRecorded'):
    assert part in native,part
for title in ('Household and career dossier',
              'Company and officers','Royal and kingdom affairs'):
    assert title in native,title
assert 'NativeBUTRStatus("Clan")' in adapter
assert 'NativeBUTRStatus("Party")' in adapter
assert 'NativeBUTRStatus("Kingdom")' in adapter
assert 'ChangeKingdomAction' not in adapter
assert 'HeroCreator.Create' not in adapter
affairs=(source/"CUMapAffairsNative.cs").read_text()
assert '[PrefabExtension("MapBar", "descendant::Window/Widget/Children")]' in affairs
assert '[PrefabExtension("MapNotificationUI",' in affairs
assert "MapNotificationWidget" in affairs
assert 'BaseViewModelMixin<MapBarVM>' in affairs
assert 'BaseViewModelMixin<MapNotificationVM>' in affairs
assert 'CUHasPendingAffairs' in affairs
assert 'CUMapNotificationAffairsMixin.Pulse(dt);' in core
assert 'CUMapNotificationAffairsMixin.Clear();' in core
journal=(source/"SandboxOriginBehavior.Affairs.cs").read_text()
assert 'petitionStories.CanFollowUp(' in journal
assert 'agencyDecisions.PendingCount' in journal
assert 'ScreenManager.PushScreen(new CUAffairsScreen(' not in journal
assert 'if(CUAffairsScreen.TryOpenDiagnosticProbe(all.ToList()))return true;' in journal
probe=(source/"CUAffairsScreen.cs").read_text()
start=probe.index("internal static bool TryOpenDiagnosticProbe(")
end=probe.index("public CUAffairsScreen(",start)
guard=probe[start:end]
for token in ('diagnosticProbeConsumed ||','Environment.GetEnvironmentVariable("CU_AFFAIRS_GRAPHICAL_PROBE")',
              '"1",StringComparison.Ordinal','!(ScreenManager.TopScreen is MapScreen)',
              'diagnosticProbeConsumed=true;','phase=map_trial_requested'):
    assert token in guard,token
assert guard.index("diagnosticProbeConsumed=true;") < guard.index("ScreenManager.PushScreen(")
assert probe.count("ScreenManager.PushScreen(")==1
assert 'b.ShowAffairsNativePage(all,0)' in journal
assert 'InformationManager.ShowInquiry(new InquiryData(title,body,true,more || unread,' in journal
for forbidden in ('GiveGoldAction','ChangeRelationAction','ChangeOwnerOfSettlementAction','AnswerCharacterRequest(','GameMenu.ActivateGameMenu'):
    assert forbidden not in journal,forbidden
installer=(root/"scripts"/"Install-CU-From-Artifact.ps1").read_text()
for dep in required:
    assert '"'+dep+'"' in installer
assert 'BUTR PRECHECK PASS:' in installer
assert installer.index('BUTR PRECHECK PASS:') < installer.index(
    'if (Test-Path -LiteralPath $destination)')
assert 'LatestVerified' in installer
print("PASS v0.70y SOURCE: four nonoptional BUTR modules, compiled "+
      "UIExtenderEx 3.0.3 native Clan/Party/Kingdom mixins, no copied "+
      "runtime DLLs, and install precheck before changing game files.")

# Native encyclopedia uses existing stat rows, not custom movie patches.
assert 'BaseViewModelMixin<EncyclopediaHeroPageVM>' in adapter
assert 'page.IsInformationHidden' in adapter
assert 'hero.Clan!=Clan.PlayerClan' in adapter
assert 'page.Stats.Remove(row)' in adapter and 'page.Stats.Add(row)' in adapter
assert 'new StringPairItemVM("Standing / profession",title)' in adapter
