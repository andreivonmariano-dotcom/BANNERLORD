#!/usr/bin/env python3
"""v0.48 creator and family runtime-contract source gate. Does not run Bannerlord."""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
src=root/"Source/CalradiaUnified"
gui=root/"Modules/CalradiaUnified/GUI/Prefabs"
def read(name):return (src/name).read_text(encoding="utf-8")

family=read("SandboxFamilyCreator.cs")
noble=read("SandboxGeneratedSpouseOrigin.cs")
guided=read("CUGuidedCreatorVM.cs")
stats=read("CUNativeStatsLayer.cs")
origin=read("SandboxOriginBehavior.cs")
age_policy=read("SandboxFamilyAgePolicy.cs")
home=read("SandboxFamilyHome.cs")
shell=read("CUNativeStateShell.cs")

assert 'birthClan = profile.SpouseBackground == "NobleRandom" && !preview ?' in noble
assert '"|ancestry=unrecorded"' in noble
assert 'father=unknown|mother=unknown' in noble
assert '"no_valid_noble_clan_in_selected_kingdom"' in noble
assert "h.Spouse.Spouse == h" in noble
assert 'profile.SpouseBackground == "NobleChosen"' in noble

assert "result.SetBirthDay(CampaignTime.YearsFromNow(-age));" in family
assert "cu_family_hero_exact_age" in family
assert "cu_family_spouse_age_verified" in family
assert "cu_family_child_age_verified" in family
assert "cu_family_child_life_stage" in family
assert "if (!child.IsChild)" in family
assert "child.SetNewOccupation(Occupation.Lord);" in family
assert "child.ChangeState(Hero.CharacterStates.Active);" in family
assert "child.Mother = mother;" in family
assert "child.Father = father;" in family
assert "child.StaticBodyProperties = Campaign.Current.Models" in family
assert "MarriageAction.Apply(player,spouseHero,false)" in family
assert "Math.Abs(actual-requested)<0.1f" in age_policy

assert "private void ClampChildrenToParents()" in guided
part=guided.split("private void ClampChildrenToParents()",1)[1].split(
    "private string Raw",1)[0]
assert "profile.Children[i].Age=max" not in part
assert "Nothing has been changed automatically" in part
assert "AGE CONFLICT:" in part
assert "CanChooseChildAge(" in guided
assert '"|spouse_age="+candidate.SpouseAge' in guided
assert '"|child_ages="+String.Join' in guided

assert '"cu_family_age_contract"' in origin
assert '"spouse_age_mismatch"' in origin
assert '"child_identity_or_age_mismatch"' in origin
assert '"|requested_spouse_age=" + profile.SpouseAge' in origin
assert "SandboxStartContract.Validate(" in origin
assert "SandboxFamilyHome.TryPlace(" in origin
assert "if (child.IsChild)" in home
assert "var nativeState = child.HeroState;" in home

assert "ExecuteEnterExactAge()" in stats
assert "cu_native_age_exact_entry" in stats
assert "cu_native_age_button" in stats
assert "cu_native_age_slider_drag" in stats
assert "MatchesRequestedAge(" in stats
assert 'if(SandboxCreatorOverlay.HasAcceptedCustomStart)' in shell
assert "CUNativeControlSkin.Clear();" in shell

for name in ("CUNativeStatsEditor","CUNativeStatsEditorPlainFallback",
             "CUNativeStatsEditorSafe"):
    xml=ET.parse(gui/(name+".xml")).getroot()
    cmds=[b.get("Command.Click") for b in xml.iter("ButtonWidget")]
    assert cmds.count("ExecuteEnterExactAge")==1,name
    assert "ExecuteAgeDecrease" in cmds and "ExecuteAgeIncrease" in cmds
    assert "ExecuteKeepNativeAge" in cmds

print("PASS: v048 noble ancestry fallback, exact Hero birthdays, six-adult/minor family contract, no silent child age change and exact age input (STATIC)")
