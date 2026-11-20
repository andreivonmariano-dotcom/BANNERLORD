#!/usr/bin/env python3
"""v0.61 automatic House, culture and native officer contracts (source only)."""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
def read(name):
    return (src/name).read_text(encoding="utf-8")

vm=read("CUGuidedCreatorVM.cs")
profile=read("SandboxOriginProfile.cs")
family=read("SandboxFamilyCreator.cs")
retinue=read("SandboxCulturalRetinue.cs")
auto=read("SandboxAutoHousePlan.cs")
names=read("SandboxCulturalNames.cs")
office=read("SandboxRetinueOfficePolicy.cs")
fields=vm.split("private List<Field> BuildFields()",1)[1]
world=fields.split("if(preCampaign)",1)[1].split("\n            else\n            {",1)[0]
house=fields.split("\n            else\n            {",1)[1].split("return list;",1)[0]
assert '"NobleBirthPosition"' in world and '"NobleElderHeroId"' in world
assert '"NobleBirthPosition"' not in house and '"SiblingCount"' in house
assert 'candidate.RetinueMode=SandboxNobleStartPolicy.EligibleForRetinue' in vm
assert 'SandboxAutoHousePlan.Prepare(profile' in vm
assert 'if(!profile.AutoHouseNamesPrepared)' in vm
assert 'houseDraft=SandboxOriginProfile.Parse(profile.Serialize())' in vm
assert 'houseDraft=null;' in vm
assert 'AutoHouseNamesPrepared=' in profile
assert 'AutoHouseNamesPrepared=b' in profile
for key in ("SpouseEnabled","ChildCount","SiblingCount"):
    assert f'"{key}"' in house
for key in ("SpouseName","SpouseAge","SpouseGender","SpouseBackground",
            "FamilyScenario","FamilyHomeMode","RetinueMode","Gold","Influence"):
    start=house.index(f'"{key}"')
    assert 'advanced:true' in house[start:house.index('));',start)],key
for culture in ("vlandia","sturgia","battania","empire","aserai","khuzait"):
    assert f'case "{culture}"' in names,culture
assert "SandboxCulturalNames.Suggest" in auto
assert "SandboxCulturalNames.Suggest" in family
assert "SandboxCulturalNames.Suggest(culture,female,index+2" in retinue
assert "TrainForOffice(officer,offices[i])" in retinue
assert "EquipFromNativeDonor(officer,sources[i],offices[i])" in retinue
assert "AssignNativeDuty(officer,offices[i])" in retinue
assert "SetPartyQuartermaster" in retinue
assert "SetPartyScout" in retinue
assert "SandboxRetinueCulturePolicy.Training" in retinue
assert "SandboxRetinueOfficePolicy.Skills" in read("SandboxRetinueCulturePolicy.cs")
assert 'case "Knight"' in office and 'case "HouseholdSteward"' in office
assert '"RoleBased" ||' in retinue
print("PASS: automated culture names, minimal House, advanced controls, officer specialization and native party duties (SOURCE ONLY)")
