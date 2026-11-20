#!/usr/bin/env python3
"""v0.60b source integration checks, not native game execution."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
vm = (root / "Source/CalradiaUnified/CUGuidedCreatorVM.cs").read_text(encoding="utf-8")
retinue = (root / "Source/CalradiaUnified/SandboxCulturalRetinue.cs").read_text(encoding="utf-8")
profile = (root / "Source/CalradiaUnified/SandboxOriginProfile.cs").read_text(encoding="utf-8")
policy = (root / "Source/CalradiaUnified/SandboxNobleStartPolicy.cs").read_text(encoding="utf-8")

world_marker = 'list.Add(new Field("CHAPTER I | YOUR WORLD","StartingScenario"'
house_marker = 'list.Add(new Field("CHAPTER III | YOUR HOUSE","SpouseEnabled"'
world_start = vm.index(world_marker)
house_start = vm.index(house_marker)
world = vm[world_start:house_start]
house = vm[house_start:vm.index("return list;",house_start)]
assert '"NobleBirthPosition","Your noble birth"' in world
assert '"NobleElderHeroId","Native family leader ID' in world
assert world.index('"Role"') < world.index('"NobleBirthPosition"')
assert world.index('"StartingKingdomId"') < world.index('"NobleBirthPosition"')
assert '"NobleBirthPosition"' not in house
assert '"NobleElderHeroId"' not in house
assert '"SiblingCount"' in house
assert '"RetinueMode"' in house
assert 'return preCampaign && profile.Role=="Lord"' in vm
assert 'candidate.NobleBirthPosition,candidate.Role' in vm
assert 'Birth: "+DisplayChoice("NobleBirthPosition"' in vm
assert 'Noble origin: "+DisplayChoice("NobleBirthPosition"' in vm
assert 'Household officers: "+DisplayChoice("RetinueMode"' in vm

assert 'choices:new[]{"Native","RoleBased","FamilyOnly"}' in house
assert '"RoleBased":' in vm
assert 'Replace native companions with cultural household officers' in vm
assert 'profile.RetinueMode=="RoleBased" ||' in retinue
assert 'profile.RetinueMode=="ReplaceNative"' in retinue
assert retinue.index('HeroCreator.CreateSpecialHero') < retinue.index('RemoveCompanionAction.ApplyByFire')
assert 'refused:native_companion_not_firable' in retinue
assert '!Clan.PlayerClan.Companions.Contains(old)' in retinue
assert 'cu_cultural_retinue_replacement' in retinue
assert 'native_companions_dismissed=' in retinue
assert 'DisableHeroAction.Apply(pending)' in retinue
assert 'public string RetinueMode = "Native";' in profile
assert '"Native", "RoleBased", "ReplaceNative"' in policy
print("PASS: noble birth World-first, siblings remain House, custom retinue replaces native (SOURCE ONLY)")
