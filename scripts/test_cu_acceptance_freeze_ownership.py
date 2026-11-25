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

# Runtime ownership is enforced at registration rather than only through config flags.
autonomy=read("KingdomAutonomyBehavior.cs")
living=read("SandboxOriginBehavior.LivingMenus.cs")
civic_registration=civic[civic.index("public override void RegisterEvents()"):civic.index("public override void SyncData(")]
autonomy_registration=autonomy[autonomy.index("public override void RegisterEvents()"):autonomy.index("public override void SyncData(")]
assert "RegisterRetiredCivicExits" in civic_registration
assert "DailyTickEvent" not in civic_registration and "DailyTickPartyEvent" not in civic_registration
assert "AddNonSerializedListener" not in autonomy_registration
assert "RegisterLocalAdministrationEntry(starter)" not in living
assert 'CU_v030_civic_state' in civic and 'CU_v060_kingdom_orders' in autonomy
assert 'CU_v060_kingdom_day' in autonomy
assert 'public bool EnableNpcAdvisory' not in mcm

with (root/"Data"/"CU_Complementary_Feature_Disposition.csv").open(encoding="utf-8",newline="") as handle:
    dispositions=list(csv.DictReader(handle))
with (root/"Data"/"CU_Feature_Compatibility_Audit.csv").open(encoding="utf-8",newline="") as handle:
    historical=list(csv.DictReader(handle))
assert len(dispositions)==55 and {row["feature_id"] for row in dispositions}=={row["feature_id"] for row in historical}
assert all(row["disposition"] in {"KEEP","MERGE","RETIRED","OPTIONAL","DEFER"} and row["reason"] and row["owner"] for row in dispositions)
print("PASS all 55 historical features have explicit disposition; legacy civic and autonomy save keys retained without active tick subscriptions.")

# The expanded six-system review covers every canonical CU entry without treating historical references as implemented features.
with (root/"Data"/"CU_Core_System_Review.csv").open(encoding="utf-8",newline="") as handle:
    core_review=list(csv.DictReader(handle))
with (root/"Data"/"Feature_Registry.csv").open(encoding="utf-8",newline="") as handle:
    registered={row["feature_id"] for row in csv.DictReader(handle) if row["feature_id"].startswith("CU-")}
ids=[row["feature_id"] for row in core_review]
assert len(registered)==70 and registered <= set(ids)
assert len(ids)==len(set(ids)) and len(ids)>70
assert all(row["review_reason"] and row["owner"] and (root/row["source_path"]).is_file() for row in core_review)
assert all(row["disposition"] in {"KEEP","IMPROVE","MERGE","RETIRE","DEFER","ARCHIVE","OPTIONAL"} for row in core_review)
print("PASS six-system review includes every canonical CU feature and unique supplemental source contracts.")
