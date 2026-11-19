#!/usr/bin/env python3
"""Gate A/B only. Active CU starts must be identity-first, with native
culture, face, origins, banner and clan preserved, followed by guided family
and fortune. Does NOT certify live layout, a skipped cutscene or map entry.
"""
import re
import xml.etree.ElementTree as ET
from pathlib import Path

root=Path(__file__).resolve().parent.parent
s=root/"Source/CalradiaUnified"
entry=(s/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
flow=(s/"CUGuidedCreatorVM.cs").read_text(encoding="utf-8")
house=(s/"CUNativeHouseLayer.cs").read_text(encoding="utf-8")
shell=(s/"CUNativeStateShell.cs").read_text(encoding="utf-8")
stats=(s/"CUNativeStatsLayer.cs").read_text(encoding="utf-8")
profile=(s/"SandboxOriginProfile.cs").read_text(encoding="utf-8")
policy=(s/"CUNativeChapterPolicy.cs").read_text(encoding="utf-8")
map_policy=(s/"NativeStateShellPolicy.cs").read_text(encoding="utf-8")
xml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUGuidedCreator.xml").getroot()

# The ACTIVE game entrypoint must use guided World, not retired five-tab
# creator. Both front and post-clan panels use exactly the same movie.
assert 'viewModel = new CUGuidedCreatorVM(true,' in entry
assert 'layer.LoadMovie("CUGuidedCreator", viewModel);' in entry
assert 'if(viewModel!=null) viewModel.ExecuteBack();' in entry
assert 'new CUGuidedCreatorVM(false,' in house
assert 'nextMovie=next.LoadMovie("CUGuidedCreator",vm);' in house
assert 'CUNativeHouseLayer.Tick(screen,' in shell
assert 'CUNativeHouseLayer.Clear();' in shell
assert 'CUNativeStatsLayer.Tick(screen,' in shell
assert 'CUNativeStatsNavigationPolicy.IsFinal(pageIndex)' in stats
assert 'CUNativeStatsNavigationPolicy.NextPage(pageIndex)' in stats
assert 'CUNativeStatsNavigationPolicy.BackPage(pageIndex)' in stats
assert 'CUNativeStatsNavigationPolicy.NextLabel(pageIndex)' in stats

# World and role are the ONLY front-campaign questions. Fief and family
# are added by a separate VM after the live player, native clan and banner.
front=flow.split('if(preCampaign)\n            {',1)[1].split('\n            else\n            {',1)[0]
tail=flow.split('            else\n            {',1)[1].split('\n            return list;',1)[0]
for key in ("StartingScenario","Role","StartingKingdomId",
            "ScenarioFaction1","ScenarioFaction2","ScenarioVariant",
            "WorldSeed"):
    assert '"'+key+'"' in front,key
for key in ("SpouseEnabled","SpouseAge","ChildCount","Gold","Influence",
            "Troops","StartingFiefMode","AllowStartingFiefTransfer",
            "FamilyHomeMode","FamilyHomeId"):
    assert '"'+key+'"' in tail,key
    assert '"'+key+'"' not in front, "uninitialized family/resources asked before identity"
assert 'new Field("CHAPTER V | BEGIN YOUR LEGACY","Review"' not in tail
assert 'stage!="CharacterCreationReviewStage"' in house, "only the native final review remains"
assert 'profile.ResetForNewCampaign();' in flow
assert 'profile.SaveForPlayer(' not in front
assert 'SaveForPlayer(' in flow
assert 'int actualAge=hero==null?30:(int)hero.Age;' in flow
assert 'candidate.IsValid(actualAge,out error)' in flow
assert 'candidate.RealFamilyEnabled=candidate.SpouseEnabled ||' in flow
assert 'SandboxFiefPolicy.ValidateRequest(' in flow
assert 'SandboxFamilyHomePolicy.ValidateRequest(' in flow
assert 'SandboxNativeStartOptions.TryPrepare(' in flow
assert flow.index('SandboxNativeStartOptions.TryPrepare(')<flow.index('candidate.SaveForPlayer(')
assert 'candidate.NativeStateShellEnabled=true;' in flow
assert 'candidate.NativeFaceDockTrialEnabled=false;' in flow
assert '"cu_guided_house_fortune_committed"' in flow
assert 'ItemIds.Clear();' in profile and 'Overrides.Clear();' in profile
assert 'Gold=-1;' in profile and 'Troops=0;' in profile
assert 'public bool SaveForPlayer(string file,int actualPlayerAge,' in profile
assert 'if (!p.IsValid(100, out problem))' in profile

# Never create/skip native stages. House reappears only after real native
# banner AND clan, before review/options finalization, with native escape.
assert 'stage=="CharacterCreationBannerEditorStage"' in house
assert 'stage=="CharacterCreationClanNamingStage"' in house
assert 'stage!="CharacterCreationReviewStage"' in house
assert 'stage!="CharacterCreationOptionsStage"' in house
assert '!bannerSeen || !clanSeen || completed' in house
assert 'manager.PreviousStage();' in house
assert 'Input.IsKeyReleased(InputKey.Escape)' in house
assert 'screen.AddLayer(next);' in house
assert 'oldLayer.ReleaseMovie(oldMovie);' in house
assert 'oldHost.RemoveLayer(oldLayer);' in house
assert 'oldLayer.IsFocusLayer=false' in house
assert 'SandboxCreatorOverlay.HasAcceptedCustomStart' in house
for forbidden in ("new CharacterCreationScreen(","new BodyGeneratorView(",
                  "RemoveStage<","HeroCreator."):
    assert forbidden not in house,forbidden

# Visible rail reflects the revised decision order; no jump is allowed.
for label in ("01  YOUR WORLD","02  YOUR CHARACTER","03  YOUR HOUSE",
              "04  YOUR FORTUNE","05  BEGIN YOUR LEGACY"):
    assert label in policy,label
assert '"CharacterCreationNarrativeStage"' in map_policy
assert 'return "02  YOUR CHARACTER";' in map_policy
assert 'return "03  YOUR HOUSE";' in map_policy
assert 'return "05  BEGIN YOUR LEGACY";' in map_policy

# One compact question and its controls. No experimental opt-in or
# duplicate stats and family wizard in the pre-campaign movie.
assert xml.tag=="Prefab"
assert sum(node.get("MaxWidth")=="1820" and node.get("MaxHeight")=="960"
           for node in xml.iter("Widget"))==1
commands=[node.get("Command.Click") for node in xml.iter("ButtonWidget")]
for command in ("ExecutePreviousChoice","ExecuteNextChoice","ExecuteToggle",
                "ExecuteIncrease","ExecuteDecrease","ExecuteExact",
                "ExecuteEditText","ExecuteNativeDefault","ExecuteBack",
                "ExecuteContinue","ExecuteAdvanced"):
    assert commands.count(command)==1,command
    assert 'public void '+command+'()' in flow,command
assert not any("Experimental" in node.get("Text","")
    for node in xml.iter())
for field in ("Title","PhaseLabel","Chapter","StepProgress","StepTitle",
              "Hint","SelectedValue","Summary","AdvancedLabel","Status",
              "ContinueLabel"):
    assert any(node.get("Text")=="@"+field
        for node in xml.iter("TextWidget")),field
for key in ("IsChoice","IsNumber","IsText","IsBoolean"):
    assert any(node.get("IsVisible")=="@"+key
        for node in xml.iter("ButtonWidget")),key
assert 'Begin Campaign' not in ET.tostring(xml,encoding="unicode")
print("PASS: active World -> native identity/story/stats/banner/clan -> guided House/Fortune -> native review/difficulty; one shared CU layout (STATIC ONLY)")
