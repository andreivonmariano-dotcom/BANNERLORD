#!/usr/bin/env python3
"""Two real 21 Nov 2026 reports: unreachable noble House birth and
independent Lord's artificial active parents and undelivered cultural officers.

Source wiring check ONLY. It cannot prove native game replacement actions,
3D House UI, save/load, parent display, or the availability of a native elder.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
read=lambda p: (root/p).read_text(encoding="utf-8")
vm=read("CUGuidedCreatorVM.cs")
family=read("SandboxFamilyCreator.cs")
retinue=read("SandboxCulturalRetinue.cs")
pol=read("SandboxNobleStartPolicy.cs")
contract=read("SandboxStartContract.cs")
apply=read("SandboxOriginBehavior.cs")

# Session 05:05: selected YoungerBrotherOfLord, preview had no elder and
# House refused fourteen Confirm attempts. Stay at noble choice at entry.
start=vm[vm.index("fields=BuildFields();"):vm.index("private List<Field> BuildFields()")]
assert "cu_house_unavailable_noble_birth_focused" in start
assert 'f=>f.Key=="NobleBirthPosition"' in start
assert "HasEligibleProjectedNoble(profile)" in start
assert "No eligible native noble relative for " in start
next_step=vm[vm.index("public void ExecuteContinue()"):
             vm.index("private void CommitWorld()")]
assert 'Current.Key=="NobleBirthPosition"' in next_step
assert "cu_house_noble_choice_unavailable" in next_step
assert "HasEligibleProjectedNoble(profile)" in next_step
assert "return;" in next_step
house=vm[vm.index("private void CommitHouse()"):
         vm.index("[DataSourceProperty] public bool IsWorldSetup")]
assert "HasEligibleProjectedNoble(candidate)" in house
assert "native_finalization_not_invoked=true" in house
assert house.index("HasEligibleProjectedNoble(candidate)")<house.index(
    "candidate.SaveForPlayer(")
assert "SandboxDynasticSonStart.HasProjectedNativeFather(" in vm
assert "SandboxDynasticStart.HasProjectedNativeElder(" in vm

# Session 05:12 Independent Lord: two new unrelated-looking ACTIVE parents
# were generated solely to provide the player's two siblings with links.
# Preserve the native genealogy without activating new parent court Heroes.
prepare=family[family.index("private static string EnsureSiblingParents("):
               family.index("private static Hero Create(")]
assert 'profile.Role=="Lord"' in prepare
assert 'profile.NobleBirthPosition=="Independent"' in prepare
assert "fatherAge,null,!historicalOnly" in prepare
assert "motherAge,null,!historicalOnly" in prepare
assert "cu_independent_lord_ancestry_recorded" in prepare
assert "parents_not_removed_from_family_tree=true" in prepare
create=family[family.index("private static Hero Create("):
              family.index("private static string CleanupProvisionalHero(")]
assert 'Clan originClan=null,bool activate=true' in create
assert "if (age >= 18 && activate)" in create
assert "if (!activate && result.IsActive)" in create
assert "cu_family_parent_historical_only" in create
assert "if(player.Father==null)player.Father=father;" in prepare
assert "if(player.Mother==null)player.Mother=mother;" in prepare

# Original native start provided TWO unrelated wandering companions.
# RoleBased should stage culturally trained officers, then use verified
# native Fire actions for any actual companions rather than abort at >1.
assert "refused:multi_companion_dismissal_not_atomic" not in retinue
assert "existing.Length>16" in retinue
assert "native.Length>16" in retinue
assert "cu_cultural_retinue_roster_preflight" in retinue
assert "staged_before_dismissal=true" in retinue
assert "failed_removal_reported_partial=true" in retinue
assert retinue.index("HeroCreator.CreateSpecialHero(")<retinue.index(
    "RemoveCompanionAction.ApplyByFire(",
    retinue.index("string[] offices=SandboxNobleStartPolicy.RetinueOffices("))
assert "replaced!=existing.Length" in retinue
assert "fired!=native.Length" in retinue
assert '"partial:officers="+created+' in retinue
assert '"|same_culture=true"' in retinue
assert 'return officers>0 && officers<=16' in contract
assert 'dismissed<0 || dismissed>16' in contract
assert "SandboxStartContract.RetinueReadyForOtherGrants(" in apply

# A real Lord always commissions Master of Arms + Knight, even if a
# younger brother is already traveling with the player.
assert 'case "Lord":return new[]{"MasterOfArms","Knight"};' in pol
assert "EquipForStyle(officer,sources[i]," in retinue
assert "EquipCivilian(officer,sources[i],offices[i]);" in retinue
assert "TrainForOffice(officer,offices[i]);" in retinue
assert "AddCompanionAction.Apply(Clan.PlayerClan,officer);" in retinue
assert "AddHeroToPartyAction.Apply(" in retinue

print("PASS v0.69i: unsupported noble birth blocked at its choice, "
      "historical parents kept inactive, two wanderers replaceable "
      "with verified Knight/MasterOfArms. NATIVE GAME RETEST REQUIRED.")
