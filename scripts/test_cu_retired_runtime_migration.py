#!/usr/bin/env python3
"""Current source-only cleanup and migration boundary. Historical code is not executable."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda n:(src/n).read_text(encoding="utf-8-sig")
excluded={x.get("Remove") for x in ET.parse(src/"CalradiaUnified.csproj").getroot().iter("Compile")}
legacy={"CivicBehavior.cs","CivicBehavior.CastleManagement.cs","CivicBehavior.Convoys.cs","CivicBehavior.Feudal.cs","CivicBehavior.Garrisons.cs","CivicBehavior.Kingdom.cs","CivicBehavior.Trade.cs","KingdomAutonomyBehavior.cs"}
assert legacy <= excluded
c=read("CivicMigrationBehavior.cs")
a=read("KingdomAutonomyMigrationBehavior.cs")
s=read("SupplyBehavior.cs")
oldc=read("CivicBehavior.cs")
olda=read("KingdomAutonomyBehavior.cs")
# Exact serialization and legacy menu bodies, not a substitute parser.
def between(text,start,end):
    i=text.index(start)
    return text[i:text.index(end,i)].strip()
assert between(c,"public override void SyncData","private void RegisterRetiredCivicExits")==between(oldc,"public override void SyncData","private void RegisterRetiredCivicExits")
old_exit=between(oldc,"private void RegisterRetiredCivicExits","// Historical implementation retained")
assert old_exit in c
assert between(a,"public override void SyncData","private static bool SafeId")==between(olda,"public override void SyncData","private void OnDay")
assert a[a.index("private static bool SafeId"):].strip()==olda[olda.index("private static bool SafeId"):].strip()
assert re.findall(r'SyncData\(\s*"([^"]+)"',c)==["CU_v030_civic_state"]
assert re.findall(r'SyncData\(\s*"([^"]+)"',a)==["CU_v060_kingdom_orders","CU_v060_kingdom_day"]
assert re.findall(r'SyncData\(\s*"([^"]+)"',s)==["CU_v020_days_since_forage"]
assert c.count("AddNonSerializedListener(")==1 and "RegisterRetiredCivicExits" in c
assert "AddNonSerializedListener(" not in a
for mutator in ("OnParty(","OnDay(","InvokeMovement(","TryOrderToSettlement(","SetMoveGoToSettlement","SetMovePatrolAroundSettlement"):
    assert mutator not in a,mutator
for mutator in ("OnPartyTick(","TickPhysicalConvoys(","DispatchPhysicalConvoy(","RegisterLocalAdministrationEntry(","Safe(Deposit)","Safe(Recruit)","RegisterGarrisonMenu("):
    assert mutator not in c,mutator
assert "AddToCounts(" not in s and "ShouldForage(" not in s and "DefaultItems.Grain" not in s
assert "EnableSupplyWarnings" in s and "EnableArmyWarnings" in s
assert "new CivicBehavior(config)" in read("SubModule.cs")
assert "new KingdomAutonomyBehavior(config)" in read("SubModule.cs")
assert "new SupplyBehavior(config)" in read("SubModule.cs")
compiled={p.name:p.read_text(encoding="utf-8-sig") for p in src.glob("*.cs") if p.name not in excluded}
for name in legacy:
    assert name not in compiled
assert sum(bool(re.search(r'\bclass CivicBehavior\b',v)) for v in compiled.values())==1
assert sum(bool(re.search(r'\bclass KingdomAutonomyBehavior\b',v)) for v in compiled.values())==1
assert not any("RegisterLocalAdministrationEntry(" in v for v in compiled.values())
print("PASS SOURCE ONLY: 8 retired adapters excluded; original 3 civic/autonomy keys and exact codecs, 4 exit menus and supply cooldown retained; no foraging mutator.")
