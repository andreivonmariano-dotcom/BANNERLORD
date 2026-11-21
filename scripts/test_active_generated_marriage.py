#!/usr/bin/env python3
"""v0.26 source-only regression for generated adult NotSpawned spouse bug.
The actual Clan/marriage/child/save acceptance remains an in-game test.
"""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
src=root/"Source"/"CalradiaUnified"
family=(src/"SandboxFamilyCreator.cs").read_text()
policy=(src/"SandboxGeneratedMarriagePolicy.cs").read_text()
p=(root/"Tests/PolicySmoke/Program.cs").read_text()
project=(root/"Tests/PolicySmoke/PolicySmoke.csproj").read_text()
assert "SandboxGeneratedMarriagePolicy.cs" in project
assert "native fresh NotSpawned hero is refused" in p
assert 'result.ChangeState(Hero.CharacterStates.Active)' in family
assert 'cu_family_new_adult_activated' in family
assert 'cu_family_marriage_prerequisites' in family
assert 'SandboxGeneratedMarriagePolicy.Check(' in family
assert 'player_can_marry=' in family
assert 'spouse_can_marry=' in family
assert 'cu_family_provisional_cleanup' in family
assert 'SandboxFamilyCreationSafetyPolicy.MayClearUnmarriedSpouse(' in family
assert 'allocatedChildren.Add(child);' in family
assert 'DisableHeroAction.Apply(person)' in family
assert 'person.Clan=null;' in family
assert 'person.Father.Children.Remove(person)' in family
assert 'person.Mother.Children.Remove(person)' in family
assert family.index('DisableHeroAction.Apply(person)') < family.index(
    'person.Father.Children.Remove(person)')
ix_create=family.index('spouseHero = Create(')
ix_gate=family.index('IsCoupleSuitableForMarriage(player,spouseHero)')
ix_marriage=family.index('MarriageAction.Apply(player,spouseHero,false)')
ix_child=family.index('HeroCreator.CreateChild(childTemplate,')
assert ix_create<ix_gate<ix_marriage<ix_child
ix_state=family.index('result.ChangeState(Hero.CharacterStates.Active)')
ix_return=family.index('return result;',ix_state)
assert ix_state<ix_return
assert 'player.Spouse == spouseHero ||' in family
assert 'spouseHero.Spouse == player' in family
assert 'GetStaticBodyProperties(child,true)' in family
for c in ('generated_spouse_not_active','player_clan_invalid',
          'spouse_birth_clan_invalid','generated_spouse_not_eligible'):
    assert c in policy,c
print('PASS: activate generated adult spouse before native marriage, log failure predicate, clean rejected orphan, and wait to create children (STATIC ONLY)')
