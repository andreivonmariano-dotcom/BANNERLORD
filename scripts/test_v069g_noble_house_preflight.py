#!/usr/bin/env python3
"""Regression for in-game v0.69f YoungerBrotherOfLord family-loss report.

Checks source ordering and recovery affordance, not true native NPC
availability, input focus, rendered UI, or actual Hero creation.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
base = root / "Source" / "CalradiaUnified"
read = lambda name: (base / name).read_text(encoding="utf-8")

vm = read("CUGuidedCreatorVM.cs")
dynasty = read("SandboxDynasticStart.cs")
son = read("SandboxDynasticSonStart.cs")
preflight = read("SandboxStartingLifePreflight.cs")
apply = read("SandboxOriginBehavior.cs")
family = read("SandboxFamilyCreator.cs")

house_fields = vm[vm.index("private List<Field> BuildFields()"):
                  vm.index("private bool Available(")]
house_only = house_fields[house_fields.index("            else\n            {"):]
assert '"NobleBirthPosition","Confirm your noble birth"' in house_only
assert '"NobleElderHeroId","Native elder Hero ID (optional)"' in house_only
assert house_only.index('"NobleBirthPosition","Confirm your noble birth"') < (
    house_only.index('"SpouseEnabled"'))
assert "choices:SandboxNobleStartPolicy.BirthPositions" in house_only

visible = vm[vm.index("private bool Available("):
             vm.index("private Field[] Visible")]
assert 'if(key=="NobleBirthPosition")' in visible
assert 'return SandboxCustomLifeRolePolicy.IsNobleFamilyRole(' in visible
assert 'if(key=="NobleElderHeroId")' in visible
assert 'profile.NobleBirthPosition!="Independent"' in visible

write = vm[vm.index("private bool TryWrite("):
           vm.index("public void ExecutePreviousChoice()")]
assert "houseDraft=SandboxOriginProfile.Parse(profile.Serialize());" in write
assert 'if(key=="NobleBirthPosition" ||' in write
assert ".PreviewElderName(profile,Hero.MainHero)" in write
assert "string currentKey=Current==null ? key : Current.Key;" in write

hint = vm[vm.index("[DataSourceProperty] public string Hint"):
          vm.index("private static string DisplayChoice")]
assert 'Current.Key=="NobleBirthPosition"' in hint
assert '"Eligible native family: "+projectedElderName+' in hint
assert '". An older sibling created in House cannot "' in hint
assert '"replace an existing native lord. If no lord "' in hint
assert '"qualifies, choose Independent here."' in hint

# Names and ages added to the House sibling list do not make a new
# clan leader or an existing titled noble for YoungerBrotherOfLord.
assert '"automatically your lord or a clan leader. "' in hint
assert 'An older brother created here does not become an existing lord,' in vm
assert '"native clan-leading lord. Creating an older sibling "' in vm

house = vm[vm.index("private void CommitHouse()"):
           vm.index("[DataSourceProperty] public bool IsWorldSetup")]
assert "candidate.RealFamilyEnabled=candidate.SpouseEnabled ||" in house
assert "SandboxNobleStartPolicy.RequiresNobleElder(" in house
assert "SandboxDynasticSonStart.HasProjectedNativeFather(" in house
assert "SandboxDynasticStart.HasProjectedNativeElder(" in house
assert '"cu_house_noble_preflight_blocked"' in house
assert "|draft_preserved=true" in house
assert "|native_finalization_not_invoked=true" in house
assert 'field=>field.Key=="NobleBirthPosition"' in house
assert "if(nobleStep>=0)cursor=nobleStep;" in house
assert "Refresh();" in house
assert "if(!candidate.SaveForPlayer(" in house

# A missing elder must be caught BEFORE writing a finalized House profile,
# clearing its draft, invoking native Review or creating a single Hero.
gate = house.index("if(SandboxNobleStartPolicy.RequiresNobleElder(")
save = house.index("if(!candidate.SaveForPlayer(")
clear = house.index("houseDraft=null;")
finish = house.index("if(complete!=null)complete();")
assert gate < save < clear < finish
assert "return;" in house[gate:save]

# House-stage checks deliberately do not require native phase-8 vassal
# affiliation. Strict phase-9 checks remain as a second independent gate.
assert "public static bool HasProjectedNativeElder(" in dynasty
assert "FindElder(profile,player,false)!=null" in dynasty
assert "public static bool HasNativeElder(" in dynasty
assert "FindElder(profile,player,true)!=null" in dynasty
assert "public static bool HasProjectedNativeFather(" in son
assert "FindFather(p,player,false)!=null" in son
assert 'return "noble_sibling:no_eligible_native_elder";' in preflight
assert "SandboxStartingLifePreflight.Check(" in apply
assert "no_custom_grants_applied=true" in apply
assert "SandboxFamilyCreator.TryCreate(profile,hero)" in apply
assert "HeroCreator.CreateChild(" in family

print("PASS v0.69g: unresolvable native nobility blocked at House with editable family draft; strict phase-9 gate intact (source only)")
