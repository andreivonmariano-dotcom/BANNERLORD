#!/usr/bin/env python3
"""v0.60a functional-source regression for guided noble/household starts.

The independent C# PolicySmoke project verifies the decision rules.
This regression checks their exact live wiring, age control and UI.
Native engine Hero creation/Army orders need real game acceptance.
"""
from pathlib import Path
import ast

root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
def read(p):return (src/p).read_text(encoding="utf-8")

profile=read("SandboxOriginProfile.cs")
vm=read("CUGuidedCreatorVM.cs")
family=read("SandboxFamilyCreator.cs")
noble=read("SandboxDynasticStart.cs")
retinue=read("SandboxCulturalRetinue.cs")
behavior=read("SandboxOriginBehavior.cs")
policy=read("SandboxNobleStartPolicy.cs")
generator=(root/"scripts/generate_v059f_native_review_shell.py").read_text(encoding="utf-8")

for field in ("NobleBirthPosition","NobleElderHeroId","RetinueMode",
              "SiblingCount","Sibling."):
    assert field in profile,field
    assert field in vm,field
assert 'candidate.ChildCount>0 || candidate.SiblingCount>0' in vm
assert 'prefix+"Name","Sibling "+i+" | Name"' in vm
assert 'prefix+"Age","Sibling "+i+" | Age"' in vm
assert 'prefix+"Gender","Sibling "+i+" | Gender"' in vm
assert '"NobleBirthPosition","Your noble birth"' in vm
assert '"RetinueMode","Your starting household officers"' in vm
assert 'position=="Independent" || role=="Lord"' in policy
assert 'existingOlderSons>=RequiredOlderSons(position)' in policy

for check in (
    'elder.Father!=null && elder.Mother!=null',
    'elder.Culture==player.Culture',
    'elder.Clan.Kingdom!=Clan.PlayerClan.Kingdom',
    'player.Father=elder.Father',
    'player.Mother=elder.Mother',
    'ChangeRelationAction.ApplyRelationChangeBetweenHeroes',
    'cu_dynastic_native_sibling_bound'
):
    # Equivalent normalized expressions can be spread across lines.
    assert check.replace(" ","") in noble.replace(" ","").replace("\n",""),check
assert 'NobleElderHeroId' in noble
assert 'private string dynasticElderHeroId' in behavior
assert 'CU_v060_dynastic_elder' in behavior
assert behavior.index("SandboxDynasticStart.TryBind")<behavior.index(
    "SandboxFamilyCreator.TryCreate")

assert "EnsureSiblingParents(profile,player)" in family
assert "player.Father=father" in family
assert "player.Mother=mother" in family
assert "sibling.Father=player.Father" in family
assert "sibling.Mother=player.Mother" in family
assert "sibling.Culture=siblingCulture" in family
assert "father.Children.Contains(player)" in family
assert "mother.Children.Contains(player)" in family

assert "c.Occupation==Occupation.Lord" in retinue
assert "h.IsLord" in retinue
assert "c.Culture==player.Culture" in retinue
assert "officer.Culture=player.Culture" in retinue
assert "officer.SetNewOccupation(Occupation.Lord)" in retinue
assert "AddCompanionAction.Apply" in retinue
assert "AddHeroToPartyAction.Apply" in retinue
assert "RemoveCompanionAction.ApplyByFire" in retinue
assert "if(replace)" in retinue
assert "no_random_wanderer_used=true" in retinue
assert "cu_cultural_retinue_officer" in retinue
assert behavior.index("SandboxFamilyCreator.TryCreate")<behavior.index(
    "SandboxCulturalRetinue.TryCreate")
assert "profile.RetinueMode" in behavior
assert "string dynastic" not in profile # no reusing native hero from character creator

# Exact original native stone panel/footer remain authoritative.
assert 'right=find(tree,lambda n:n.get("n")=="Widget"' in generator
assert '"SuggestedWidth":"694","SuggestedHeight":"1080"' in generator
assert 'brush="ButtonBrush",font=21' in generator
assert '262,66,visible="@IsChoice"' in generator
assert 'ExecuteSlot{n}Decrease' in generator
assert '301,6,66,55,font=28' in generator
assert '372,6,66,55,font=28' in generator
assert '443,6,101,55,font=20' in generator
assert 'Id="CUPlayerAgeSlider"' in generator

print("PASS: siblings/parents, native noble younger lineage, culture-matched officer recruitment, larger safe UI controls (SOURCE ONLY)")
