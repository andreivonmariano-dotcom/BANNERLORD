#!/usr/bin/env python3
"""v0.25: exact source contract for generated spouse, birth clan and offspring.
STATIC ONLY. A real Clan/Encyclopedia and save/reload test remains necessary.
"""
from pathlib import Path

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
family=(src/"SandboxFamilyCreator.cs").read_text()
noble=(src/"SandboxGeneratedSpouseOrigin.cs").read_text()
profile=(src/"SandboxOriginProfile.cs").read_text()
overlay=(src/"SandboxCreatorOverlay.cs").read_text()
policy=(src/"SandboxFamilyOriginPolicy.cs").read_text()
origin=(src/"SandboxOriginBehavior.cs").read_text()

assert 'case "vlandian":' in policy
assert 'case "vlandia": return "vlandia";' in policy
assert '"SpouseBackground=" + SpouseBackground' in profile
assert '"SpouseBirthClanId=" + SpouseBirthClanId' in profile
assert 'NormalizeCultureId(value)' in profile
assert '"NobleRandom"' in policy and '"NobleChosen"' in policy
assert '"spouse_noble_clan_required"' in policy
assert 'new Field("Household","SpouseBackground"' in overlay
assert 'new Field("Household","SpouseBirthClanId"' in overlay
assert 'new Field("Household","SpouseCulture"' in overlay
assert 'profile.SpouseBackground == "NobleChosen"' in overlay

# The user wants a NEW spouse, not a preexisting Hero joined as the player wife.
assert 'HeroCreator.CreateSpecialHero(source' in family
assert 'SandboxGeneratedSpouseOrigin.TryResolve(profile,player' in family
assert 'Create(spouseTemplate,profile.SpouseName,' in family
assert 'profile.SpouseAge,birthClan)' in family
assert 'spouseHero.Father = birthFather' in family
assert 'spouseHero.Mother = birthMother' in family
assert 'MarriageAction.Apply(player,spouseHero,false)' in family
assert 'spouseHero.OriginClan' in family
assert 'cu_family_generated_spouse' in family
assert 'cu_family_noble_origin_selected' in noble
assert 'birthClan = Clan.PlayerClan' in noble
assert 'clan!=Clan.PlayerClan' in noble
assert 'EligibleForeignNobleClan(clan,kingdom)' in noble
assert 'kingdom.Clans.Where' in noble
assert 'h.Spouse.Spouse == h' in noble
assert 'SandboxFamilyOriginPolicy.CanBeNobleParents(' in noble
assert 'no_valid_parent_couple_in_selected_kingdom_or_clan' in noble

# The child's visual inheritance must happen AFTER both actual parents are linked.
assert 'HeroCreator.CreateChild(childTemplate' in family
ixMother=family.index('child.Mother = mother;')
ixFather=family.index('child.Father = father;')
ixVisual=family.index('.HeroCreationModel.GetStaticBodyProperties(child,true)')
assert ixMother < ixVisual and ixFather < ixVisual
assert 'child.StaticBodyProperties = Campaign.Current.Models' in family
assert 'cu_family_inherited_appearance' in family
assert 'GetCharacterTemplateForOffspring(mother, father,' in family
assert 'cu_family_verification' in family
assert 'sandbox_start_contract' in origin
stage=(src/"CUNativeOriginReviewStage.cs").read_text()
assert '[CharacterCreationStageView(typeof(CUNativeOriginReviewStage))]' in stage
assert sum(p.read_text().count('[CharacterCreationStageView(')
           for p in src.glob("*.cs")) == 2
assert "CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))" not in stage
assert "CharacterCreationStageView(typeof(CharacterCreationCultureStage))" not in stage
print("PASS: generated commoner/noble spouse, chosen/eligible native birth clan, Vlandian alias and parent-blended child appearance (STATIC ONLY)")
