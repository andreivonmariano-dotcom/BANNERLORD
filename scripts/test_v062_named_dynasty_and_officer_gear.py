#!/usr/bin/env python3
"""v0.62 regression: runtime wiring, not an in-game acceptance test."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/"Source/CalradiaUnified"
read=lambda f:(r/f).read_text(encoding="utf-8")
dy=read("SandboxDynasticStart.cs")
ob=read("SandboxOriginBehavior.cs")
vm=read("CUGuidedCreatorVM.cs")
ret=read("SandboxCulturalRetinue.cs")
pol=read("SandboxRetinueCulturePolicy.cs")
assert "PreviewElderName" in dy
assert "FindElder(profile,player,false)" in dy
assert "FindElder(profile,player,true)" in dy
assert "elder_name=" in dy
assert "cu_dynastic_named_elder_confirmed" in ob
assert "dynasticElderName=elder.Name.ToString()" in ob
assert "CU_v062_dynastic_elder_name" in ob
assert "SandboxDynasticStart.PreviewElderName(" in vm
assert "Older relative: " in vm
assert "native parentage" not in vm.lower() or "native" in vm
assert "SandboxRetinueCulturePolicy.GearStyle(" in ret
assert "SandboxRetinueCulturePolicy.Training(" in ret
assert "SandboxRetinueCulturePolicy.PrimaryTrait(" in ret
assert "SandboxRetinueCulturePolicy.SecondaryTrait(" in ret
assert "EquipmentScore(selected,style)" in ret
assert "source.BattleEquipments.ToArray()" in ret
assert "officer.BattleEquipment[slot]=selected[slot]" in ret
assert "item.ItemType.ToString()" in ret
assert "cu_retinue_cultural_training" in ret
assert "cu_retinue_personality" in ret
assert "registry.Register(officer.StringId,offices[i])" in ret
assert '+" ("+title+")"' not in ret
assert 'RecordedRetinueOffice(hero)' in read("SandboxOriginBehavior.KingCouncil.cs")
assert 'CU_v074_retinue_offices' in ob
for culture,style in [("vlandia","Lancer"),("sturgia","Axeguard"),
                      ("battania","Longbow"),("empire","Shieldguard"),
                      ("khuzait","HorseArcher"),("aserai","Skirmisher")]:
    assert f'case "{culture}":return "{style}"' in pol
print("PASS: projected/confirmed named native elder and cultural gear/personality wiring (SOURCE ONLY)")

assert 'retinueOffices.OfficeFor(o.StringId)=="MasterOfArms"' in ob
assert 'retinueOffices.OfficeFor(o.StringId)=="MasterofArms"' not in ob
assert 'key.Length==0?null' in read("SandboxOriginBehavior.KingCouncil.cs")
assert 'Household office: "+assignedOffice' in read("SandboxOriginBehavior.NativeHouseholdUI.cs")

assert "officer.CivilianEquipment[slot]=selected[slot]" in ret
assert "officer.ModifyHair(-1,0,-1)" in ret
assert "SandboxRetinueOfficePolicy.RandomAge(offices[i],random)" in ret
