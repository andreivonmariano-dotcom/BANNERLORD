#!/usr/bin/env python3
"""Static character-creation wiring audit. Not a Bannerlord UI or save test."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parent.parent
src=ROOT/"Source/CalradiaUnified"
page=(src/"NativeStartingLifeMenus.cs").read_text()
origin=(src/"SandboxOriginBehavior.cs").read_text()
profile=(src/"SandboxOriginProfile.cs").read_text()
family=(src/"SandboxFamilyCreator.cs").read_text()
role=(src/"SandboxRoleAffiliations.cs").read_text()
module=(src/"SubModule.cs").read_text()
names=dict(re.findall(r'private const string (\w+) = "(cu_life_[a-z_]+)";',page))
assert len(names)==6,names
chain=("First","Family","FamilyConfirm","Resources","Equipment","Affiliation")
assert set(chain)==set(names),names
for left,right in zip(chain,chain[1:]):
    assert re.search(r"NewPage\(nativeAge,\s*"+left+r",\s*[^,]+,\s*"+right+r",",page),left
assert re.search(r'NewPage\(nativeAge,\s*Affiliation,\s*Equipment,\s*nativeAge.OutputMenuId',page)
assert 'var rewrittenAge = new NarrativeMenu(nativeAge.StringId,' in page
assert 'nativeAge.InputMenuId, First, nativeAge.Title' in page
assert 'creator.DeleteNarrativeMenuWithId(nativeAge.StringId)' in page
assert 'creator.AddNewMenu(nativeAge)' in page
assert 'nativeAge.CharacterCreationMenuOptions' in page
assert 'Pair("Enabled","true")' not in page.split('var family =')[0]
assert 'Pair("RoleAffiliationsEnabled","false"),Pair("Enabled","true")' in page
assert 'Pair("RoleAffiliationsEnabled","true"),Pair("Enabled","true")' in page
assert 'if (creator == null || profile == null || !profile.NativeStageEnabled)' in page
assert 'ages.Count != 1' in page
assert 'String.IsNullOrEmpty(nativeAge.InputMenuId)' in page
assert 'String.IsNullOrEmpty(nativeAge.OutputMenuId)' in page
assert 'native_age_menu_graph_incomplete' in page
for child,parent in (("Family","First"),("FamilyConfirm","Family"),
                     ("Resources","FamilyConfirm"),("Equipment","Resources"),
                     ("Affiliation","Equipment")):
    assert re.search(r"NewPage\(nativeAge,\s*"+child+
                     r",\s*"+parent+r",",page),child
assert 'new NativeSandboxStageObserver()' in origin
assert 'NativeStartingLifeMenus.TryAttach(manager,' in origin
assert 'OnCharacterCreationInitializedEvent' in origin
assert 'OnBeforeInitialModuleScreenSetAsRoot' not in module
assert '"CU_Sandbox_Origin"' not in module
for key in ("NativeStageEnabled","RealFamilyEnabled","RoleAffiliationsEnabled"):
    assert re.search(r"public bool "+key+r";",profile)
    assert 'case "'+key+'":' in profile
    assert '"'+key+'=" +' in profile
assert 'if (profile == null || !profile.RealFamilyEnabled' in family
assert 'SandboxFamilyPolicy.Guard' in family
assert 'MarriageAction.Apply(player,spouseHero' in family
assert 'child.Mother=mother' in family and 'child.Father=father' in family
assert 'if (!profile.RoleAffiliationsEnabled)' in role
assert 'king_requires_official_advanced_start' in role
assert 'ApplyByJoinFactionAsMercenary' in role
assert 'ApplyByJoinToKingdom' in role
assert 'CU_v016_origin_applied' in origin
print("PASS: six native pages, Back/Next chain, age-menu rollback, opt-in and actual-hero gates")
