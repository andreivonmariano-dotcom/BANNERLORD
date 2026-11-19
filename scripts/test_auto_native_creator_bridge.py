#!/usr/bin/env python3
"""v0.39 source gate: accepted CU Sandbox starts auto-attach the native bridge.
These assertions are STATIC. They do not prove single-window gameplay, camera
input, a persistent real 3D viewport, or successful finalization.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
src = root / "Source/CalradiaUnified"
creator = (src / "SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
profile = (src / "SandboxOriginProfile.cs").read_text(encoding="utf-8")
shell = (src / "CUNativeStateShell.cs").read_text(encoding="utf-8")
culture = (src / "CUNativeCultureLayer.cs").read_text(encoding="utf-8")
face = (src / "CUNativeFaceDockTrial.cs").read_text(encoding="utf-8")
menu = (src / "SandboxMainMenuEntry.cs").read_text(encoding="utf-8")

# The no-extra-click menu entry is still intercepted once.
assert 'SandBoxNewGame' in menu and 'OverrideInitialStateOption' in menu
assert 'SandboxCreatorOverlay.OpenFromMainMenu(nativeClick)' in menu

# No user has to enter More options and set a bridge / FaceGen trial flag.
hidden = re.search(
    r'if \(key == "NativeIntegrationReviewEnabled" \|\|'
    r'.*?key == "NativeFaceDockTrialEnabled"\)\s*return false;',
    creator, re.S)
assert hidden, "legacy experimental toggles must not be player-facing"
accept = creator.split('public void ExecuteApplyAndContinue()', 1)[1]
accept = accept.split('public void ExecuteReturnToNative()', 1)[0]
required = (
    'candidate.NativeIntegrationReviewEnabled=false;',
    'candidate.NativeFaceWorkshopEnabled=false;',
    'candidate.NativeStateShellEnabled=true;',
    'candidate.NativeFaceDockTrialEnabled=true;',
    'SandboxNativeStartOptions.TryPrepare(candidate,',
    'SandboxCreatorOverlay.Accept();',
    '"cu_native_creator_auto_bridge"',
)
for marker in required:
    assert marker in accept, marker
assert accept.index('candidate.NativeStateShellEnabled=true;') < accept.index(
    'SandboxNativeStartOptions.TryPrepare(candidate,')
assert accept.index('candidate.NativeFaceDockTrialEnabled=true;') < accept.index(
    'candidate.Save(SandboxOriginProfile.UserPath,out error)')
assert accept.count('SandboxCreatorOverlay.Accept();') == 1

# Fresh profile does NOT auto-grant any family/fief/role authority until accepted.
for marker in ('Enabled=false;', 'NativeStageEnabled=false;',
               'NativeIntegrationReviewEnabled=false;',
               'NativeFaceWorkshopEnabled=false;',
               'NativeStateShellEnabled=false;',
               'NativeFaceDockTrialEnabled=false;',
               'ChildCount=0;', 'AllowStartingFiefTransfer=false;'):
    assert marker in profile, marker
assert "profile.ResetForNewCampaign();" in creator

# Native callbacks remain the owner of culture, face editing, origins and finish.
assert 'CanAttach(' in shell and 'profile.NativeStateShellEnabled' in shell
assert 'CUNativeCultureLayer.Tick(screen,' in shell
assert 'CUNativeFaceDockTrial.Tick(screen,' in shell
assert 'UseNativeFallback(' in shell
assert 'controller.Continue();' in culture
assert 'controller.SelectCulture(id)' in culture
assert 'Input.IsKeyReleased(InputKey.F9)' in face
assert 'SetScale(new Vec2(1f,1f))' in face
assert 'SetOffset(new Vec2(0f,0f))' in face
assert 'new BodyGeneratorView(' not in face
assert 'new CharacterCreationScreen(' not in face

# This is a deployable source milestone, NOT a falsely labelled final screen.
prefab = (root / "Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").read_text(encoding="utf-8")
assert 'NEXT: NATIVE CREATOR' in prefab
assert 'Begin Campaign' not in prefab
assert 'RemoveStage<' not in creator
print("PASS: automatic CU native bridge on accepted Sandbox; F9 fallback, no opt-in, native stages preserved (STATIC ONLY)")
