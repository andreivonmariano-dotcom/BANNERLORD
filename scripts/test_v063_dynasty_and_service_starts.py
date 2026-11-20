#!/usr/bin/env python3
"""v0.63: new dynastic-son and native lord-service contracts, source only."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
def read(file):return (root/file).read_text(encoding="utf-8")
dynasty=read("SandboxDynasticStart.cs")
son=read("SandboxDynasticSonStart.cs")
service=read("SandboxServicePatron.cs")
policy=read("SandboxServicePolicy.cs")
noble=read("SandboxNobleStartPolicy.cs")
retinue=read("SandboxCulturalRetinue.cs")
vm=read("CUGuidedCreatorVM.cs")
origin=read("SandboxOriginProfile.cs")
behavior=read("SandboxOriginBehavior.cs")
assert 'SandboxDynasticSonStart.TryBind' in dynasty
assert 'SandboxDynasticSonStart.Preview' in dynasty
assert 'HeroCreator.CreateSpecialHero' in son
assert 'father.Clan' in son
assert 'father=FindFather(p,player,true)' in son
assert 'mother=father.Spouse' in son
assert 'player.Father=father' in son and 'player.Mother=mother' in son
assert 'brother.Father=father' in son and 'brother.Mother=mother' in son
assert 'OlderSons(father,mother,player)' in son
assert 'generated_older_brothers=' in son
assert 'cu_dynastic_native_son_bound' in son
assert 'cu_dynastic_son_refused' in son
assert 'SandboxNobleStartPolicy.IsSonOrigin' in behavior
assert 'cu_dynastic_son_verified' in behavior
assert 'native_noble_lineage_not_bound' in behavior
assert 'CU_v063_native_father' in behavior and 'CU_v063_older_brothers' in behavior
assert 'SandboxServicePolicy.DefaultRetinue(' in vm
assert '"ServicePatronHeroId","Commanding lord Hero ID' in vm
assert '"FamilyOnly"' in vm
assert 'ServicePatronHeroId=' in origin
assert 'ServicePatronHeroId=b' not in origin
assert 'case "ServicePatronHeroId"' in origin
assert '"FamilyOnly"' in noble
assert 'profile.RetinueMode=="FamilyOnly"' in retinue
assert 'cultural_officers_created=0' in retinue
assert 'cu_service_patron_bound' in service
assert 'ChangeRelationAction.ApplyRelationChangeBetweenHeroes(' in service
assert 'h.Clan.Leader==h' in service and 'h.Clan.Kingdom==kingdom' in service
assert 'cu_service_start_status' in behavior
assert 'CU_v063_service_patron_id' in behavior
assert 'commanding_lord_not_bound' in behavior
assert 'role=="Soldier" || role=="Knight"' in policy
assert 'return "FamilyOnly";' in policy
print("PASS: father/older sons, personal native patron, Soldier family-only roster and start-contract gates (SOURCE ONLY)")
