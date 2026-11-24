#!/usr/bin/env python3
"""Source-only effective ownership/retirement gate, not a runtime test."""
import csv
import re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda n:(src/n).read_text(encoding="utf-8")
settings=read("Settings.cs")
mcm=read("CUBUTRMCMSettings.cs")
civic=read("CivicBehavior.cs")
origin=read("SandboxOriginBehavior.cs")
officer=read("SandboxOriginBehavior.NobleOfficerDialogues.cs")
sub=read("SubModule.cs")
held=set(re.findall(r"public bool (Enable\w+) = false;",settings))
assert len(held)==20
for key in held:
    assert 'case "'+key+'":' in settings,key
    assert key+'=false;' in settings,key
    assert 'public bool '+key not in mcm,key
assert 'settings.ApplyAcceptanceFreeze();' in settings
assert 'normalized=="true" && IsHeldMutation(key)' in settings
assert 'Settings config = Settings.Load();' in sub
assert 'new NativeSettingsBehavior(' not in sub
assert 'if(settings.EnableEconomy)\n                foreach (var d in state.Depots)' in civic
assert 'if(settings.EnableAbstractShipments)\n                foreach (var shipment' in civic
assert 'if(settings.EnablePhysicalConvoys)TickPhysicalConvoys();' in civic
assert 'if(!Settings.NativeOfficerTransfersEnabled)return false;' in officer
assert 'NativeOfficerTransfersEnabled { get { return false; } }' in settings
assert 'RefundWandererCompanionWage' not in origin
assert 'CU_v070b_wanderer_wage_credit_day' in origin
assert 'wage_payment_policy=native_only' in read("SandboxWandererCompanion.cs")
assert 'no net wages while serving' not in read("CUGuidedCreatorVM.cs")
assert 'state.ObserveMarriage(hero.StringId,' in read("SandboxLivingPersons.cs")
assert 'NativeMarriageObserved' in read("SandboxLifeHistoryState.cs")
for path,count in (("CU_Feature_Compatibility_Audit.csv",55),("CU_Module_Ownership_Audit.csv",41)):
    with (root/"Data"/path).open(encoding="utf-8",newline="") as f:
        assert len(list(csv.DictReader(f)))==count
print("PASS SOURCE ONLY: 20 held flags, native payroll, guarded dormant logistics, inactive legacy settings, preserved save key and ownership inventories.")
