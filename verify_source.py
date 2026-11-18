from pathlib import Path
import csv, re, xml.etree.ElementTree as ET, zipfile, json
P=Path(__file__).parent
src=P/'Source/CalradiaUnified'
files=list(src.glob('*.cs'))
assert len(files)>=18,len(files)
for f in files:
    s=f.read_text()
    assert 'namespace CalradiaUnified' in s,f
    # Removes strings, char literals, line and block comments before matching braces.
    tokens=re.sub(r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/','',s,flags=re.S)
    for a,b in ('{}','()','[]'):
        stack=0
        for c in tokens:
            if c==a: stack+=1
            if c==b: stack-=1
            assert stack>=0,(f,a,b)
        assert stack==0,(f,a,b,stack)
ET.parse(src/'CalradiaUnified.csproj')
manifest=ET.parse(P/'Modules/CalradiaUnified/SubModule.xml').getroot()
assert manifest.find('Id').get('value')=='CalradiaUnified'
assert manifest.find('Version').get('value')=='v0.28.0'
assert manifest.find('.//DLLName').get('value')=='CalradiaUnified.dll'
assert not any((P/'Modules').rglob('*.dll'))
settings=(src/'Settings.cs').read_text()
conf=(P/'Modules/CalradiaUnified/ModuleData/calradia-config.ini').read_text()
for flag in ('EnableAutonomousClanResupply','EnableAutonomousClanRecruitTrips','EnableAutonomousClanPatrol'):
    assert re.search(r'public bool '+flag+r'\s*=\s*false;',settings)
    assert flag+'=false' in conf
assert 's.Culture.BasicTroop' in (src/'CivicBehavior.cs').read_text()
behavior=(src/'KingdomAutonomyBehavior.cs').read_text()
policy=(src/'KingdomAutonomyPolicy.cs').read_text()
assert 'DailyTickPartyEvent' in behavior and 'SyncData' in behavior
assert 'party.Army != null' in behavior and 'party.MapEvent != null' in behavior
assert 'party.CurrentSettlement != null' in behavior
assert 'place.SiegeEvent != null' in behavior
assert 'Order.Resupply' in policy and 'Order.Recruit' in policy
assert 'KingdomAutonomyBehavior' in (src/'SubModule.cs').read_text()
assert 'KingdomStrategyPolicy.Choose(' in behavior
assert 'KingdomCommandPolicy.Choose(' in behavior
assert 'SetMovePatrolAroundSettlement' in behavior
assert 'SetMoveGoToSettlement' in behavior
assert 'KingdomCommandPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'DepotLedgerPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'LocalRecruitmentPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'GarrisonTrainingPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'GarrisonTrainingPolicy.cs' in (P/'Tests/CampaignSimulator/CampaignSimulator.csproj').read_text()
assert 'FeudalBudgetPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'CastleManagementPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'RegisterCastleManagementMenu' in (src/'CivicBehavior.cs').read_text()
assert 'CastleManagementPolicy.CanMove' in (src/'CivicBehavior.CastleManagement.cs').read_text()
assert 'CastleManagementPolicy.CanApplyCap' in (src/'CivicBehavior.CastleManagement.cs').read_text()
assert 'EnableManualGarrisonTransfers=false' in conf
assert 'EnableManualGarrisonWageCaps=false' in conf
assert 'DiagnosticJournal.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'new DiagnosticBehavior(config)' in (src/'SubModule.cs').read_text()
assert 'new NativeSettingsBehavior(config)' in (src/'SubModule.cs').read_text()
assert 'new SandboxOriginBehavior()' in (src/'SubModule.cs').read_text()
assert (src/'SandboxOriginProfile.cs').exists()
assert (src/'SandboxRolePolicy.cs').exists()
assert (P/'scripts/test_native_starting_life.py').exists()
assert (src/'SandboxRoleAffiliations.cs').exists()
assert (src/'NativeStartingLifeMenus.cs').exists()
assert (src/'SandboxFamilyCreator.cs').exists()
assert (src/'SandboxFamilyOriginPolicy.cs').exists()
assert (src/'SandboxGeneratedMarriagePolicy.cs').exists()
assert (P/'scripts/test_active_generated_marriage.py').exists()
assert (P/'scripts/test_child_count_native_wizard.py').exists()
assert (src/'SandboxFamilyHomePolicy.cs').exists()
assert (src/'SandboxFamilyHome.cs').exists()
assert (P/'scripts/test_family_home_settlement.py').exists()
assert (src/'SandboxGeneratedSpouseOrigin.cs').exists()
assert (P/'scripts/test_family_templates_readable_ui.py').exists()
assert 'NativeStartingLifeMenus.TryAttach' not in (src/'SandboxOriginBehavior.cs').read_text()
assert 'SandboxCreatorOverlay.Arm(manager)' not in (src/'SandboxOriginBehavior.cs').read_text()
assert 'SandboxCreatorOverlay.Tick()' in (src/'SubModule.cs').read_text()
assert (src/'SandboxCreatorOverlay.cs').exists()
assert (src/'SandboxNativeStartOptions.cs').exists()
assert (src/'SandboxStartContract.cs').exists()
assert (src/'SandboxFiefPolicy.cs').exists()
assert (src/'SandboxStartingFief.cs').exists()
assert not (src/'CUFaceGeneratorStageView.cs').exists()
assert not (P/'Modules/CalradiaUnified/GUI/Prefabs/CUFaceStageHeader.xml').exists()
assert 'CharacterCreationStageView(' not in ''.join(p.read_text() for p in src.glob('*.cs'))
assert 'candidate.NativeStageEnabled=false;' in (src/'SandboxCreatorOverlay.cs').read_text()
assert 'sandbox_start_contract' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'sandbox_appearance_engine_probe' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'OnCharacterCreationIsOverEvent' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'phase != 9' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'SandboxNativeStartOptions.TryPrepare' in (src/'SandboxCreatorOverlay.cs').read_text()
assert (P/'Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml').exists()
assert ET.parse(P/'Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml').getroot().tag=='Prefab'
assert (P/'scripts/test_sandbox_overhaul.py').exists()
assert 'OverhauledCreatorEnabled' in (src/'SandboxOriginProfile.cs').read_text()
assert 'SandboxFamilyCreator.TryCreate' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'SandboxRoleAffiliations.TryApply' in (src/'SandboxOriginBehavior.cs').read_text()
assert (src/'SandboxOriginBehavior.cs').exists()
assert (src/'SandboxOriginPanel.cs').exists()
assert (P/'scripts/test_sandbox_origin_panel.py').exists()
assert (P/'Modules/CalradiaUnified/GUI/Prefabs/CUOriginPanel.xml').exists()
assert 'CU_Sandbox_Origin' not in (src/'SubModule.cs').read_text()
assert 'OnBeforeInitialModuleScreenSetAsRoot' in (src/'SubModule.cs').read_text()
assert 'SandBoxNewGame' in (src/'SandboxMainMenuEntry.cs').read_text()
assert 'OverrideInitialStateOption' in (src/'SandboxMainMenuEntry.cs').read_text()
assert 'OnCharacterCreationInitializedEvent' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'cu_native_sandbox_menu' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'OnNewGameCreatedEvent' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'CU_v016_origin_applied' in (src/'SandboxOriginBehavior.cs').read_text()
assert 'SandboxOriginProfile.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert (src/'NativeSettingsBehavior.cs').exists()
assert (src/'SettingsMenuPolicy.cs').exists()
assert (src/'SettingsCatalog.cs').exists()
assert (src/'NativeSettingsPanel.cs').exists()
assert (P/'Modules/CalradiaUnified/GUI/Prefabs/CUSettingsPanel.xml').exists()
assert ET.parse(P/'Modules/CalradiaUnified/GUI/Prefabs/CUSettingsPanel.xml').getroot().tag=='Prefab'
assert 'NativeSettingsPanel' in (src/'NativeSettingsBehavior.cs').read_text()
assert 'SettingsCatalog.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'SettingsCatalog.Groups' in (src/'NativeSettingsBehavior.cs').read_text()
assert 'SettingsCatalog.All' in (src/'NativeSettingsBehavior.cs').read_text()
assert 'Settings LoadFromPaths' in (src/'Settings.cs').read_text()
assert 'SettingsMenuPolicy.cs' in (P/'Tests/PolicySmoke/PolicySmoke.csproj').read_text()
assert 'calradia-user-config.ini' in (src/'Settings.cs').read_text()
assert 'GameMenu.SwitchToMenu' in (src/'NativeSettingsBehavior.cs').read_text()
assert 'DiagnosticJournal.Exception' in (src/'CivicBehavior.cs').read_text()
assert 'DiagnosticJournal.Exception' in (src/'KingdomAutonomyBehavior.cs').read_text()
assert 'EnableDiagnostics=true' in conf
assert (P/'scripts/Export-CU-Diagnostics.ps1').exists()
assert (P/'scripts/analyze_diagnostics.py').exists()
assert (P/'scripts/validate_runtime_evidence.py').exists()
assert (P/'scripts/validate_settings_catalog.py').exists()
assert (P/'scripts/test_gauntlet_bindings.py').exists()
assert (P/'scripts/Run-CU-Runtime-Preflight.ps1').exists()
assert (P/'docs/V015_RUNTIME_ACCEPTANCE.md').exists()
assert 'events.csv.previous' in (P/'scripts/Export-CU-Diagnostics.ps1').read_text()
assert 'FeudalBudgetPolicy.cs' in (P/'Tests/CampaignSimulator/CampaignSimulator.csproj').read_text()
assert (P/'Tests/CampaignSimulator/FeudalStress.cs').exists()
assert 'RegisterFeudalMenu(starter, menu)' in (src/'CivicBehavior.cs').read_text()
assert 'TickFeudalEstates()' in (src/'CivicBehavior.cs').read_text()
assert 'EnableAutoGarrisonWageCaps=false' in conf
assert 'EnableAutoGarrisonTraining=false' in conf
assert 'EnableVillageHarvest=false' in conf
assert 'EnableSurplusEstateSales=false' in conf
assert 'RegisterGarrisonMenu(starter, menu)' in (src/'CivicBehavior.cs').read_text()
assert 'LocalToday' in (src/'CivicState.cs').read_text()
assert 'GarrisonToday' in (src/'CivicState.cs').read_text()
assert 'EnableGarrisonTraining=false' in conf
assert 'LocalRecruitDailyCap=2' in conf
assert 'CanDispatchToDepot' in (src/'CivicBehavior.Convoys.cs').read_text()
assert 'M1 convoy expired at full destination' in (src/'CivicBehavior.Convoys.cs').read_text()
assert (P/'Tests/CampaignSimulator/CampaignSimulator.csproj').exists()
assert (P/'Tests/CampaignSimulator/Program.cs').exists()
assert 'M0 civic state sync' in (src/'CivicBehavior.cs').read_text()
assert 'EnableAutonomousClanPatrol=false' in conf
checks=(P/'Tests/PolicySmoke/Program.cs').read_text()
assert checks.count('Check(')>122
with open(P/'Data/CalradiaUnified_78_Mod_Acceptance_Checklist.csv',encoding='utf-8-sig',newline='') as f:
    rows=list(csv.DictReader(f))
assert len(rows)==78,len(rows)
with open(P/'Data/Reconstruction_Priority_78.csv',encoding='utf-8-sig',newline='') as f:
    priorities=list(csv.DictReader(f))
assert len(priorities)==78
assert len({r['module_id'] for r in priorities})==78
assert [int(r['rank']) for r in priorities]==list(range(1,79))
assert all(r['v157_source_port_status']=='NOT REBUILT' for r in rows)
print(json.dumps({'source_cs_files':len(files),'policy_check_assertions':checks.count('Check(')-1,'tracked_original_mods':len(rows),'static_checks':'PASS','compilation':'not assessed by static checker','runtime_tested':'not assessed'},indent=2))

assert (P/'scripts/Install-CU-From-Artifact.ps1').exists()
assert (P/'docs/INSTALLATION_TROUBLESHOOTING.md').exists()
