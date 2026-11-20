#!/usr/bin/env python3
"""Static regression: opening World must not deactivate animated main menu.

The game engine owns the actual animated scene. This checks that the same
ScreenBase stays active and that CU only adds/removes a GauntletLayer.
A live Bannerlord visual check is still required to confirm animation.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
src=root/"Source/CalradiaUnified"
world=(src/"SandboxCreatorOverlay.cs").read_text(encoding="utf-8")
menu=(src/"SandboxCreatorMenuLayer.cs").read_text(encoding="utf-8")
entry=(src/"SandboxMainMenuEntry.cs").read_text(encoding="utf-8")
module=(src/"SubModule.cs").read_text(encoding="utf-8")
guided=(root/"Modules/CalradiaUnified/GUI/Prefabs/CUGuidedCreator.xml").read_text(encoding="utf-8")

open_method=world.split("public static void OpenFromMainMenu(Action nativeStart)",1)[1].split("public static void Tick()",1)[0]
assert "SandboxCreatorMenuLayer.Open(" in open_method
assert "ScreenManager.PushScreen(" not in open_method
assert "ScreenManager.PopScreen(" not in open_method
assert "ScreenManager.TopScreen == null" in open_method
assert "if (nativeStart != null) nativeStart();" in open_method
assert "ScreenManager.TopScreen" in menu
assert "active.AddLayer(next);" in menu
assert "next.InputRestrictions.SetInputRestrictions();" in menu
assert "ScreenManager.TrySetFocus(next);" in menu
assert "GauntletMovieIdentifier" in menu
assert "oldLayer.ReleaseMovie(oldMovie);" in menu
assert "oldHost.RemoveLayer(oldLayer);" in menu
assert "ScreenManager.TryLoseFocus(oldLayer);" in menu
assert "Input.IsKeyReleased(InputKey.Escape)" in menu
assert "vm.ExecuteBack();" in menu
assert "ScreenManager.PushScreen(" not in menu
assert "ScreenManager.PopScreen(" not in menu
assert "SandboxCreatorMenuLayer.RequestClose();" in world
assert "SandboxCreatorMenuLayer.Tick();" in world
assert "bool wasAttached=SandboxCreatorMenuLayer.IsAttached;" in world
assert "if(wasAttached || SandboxCreatorMenuLayer.IsAttached ||" in world
assert "MBGameManager.StartNewGame(new CUSandboxNoIntroGameManager(" in world
assert "SandboxCreatorOverlay.Tick();" in module
assert "SandBoxNewGame" in entry
assert "CUGuidedCreator" in menu
assert "Command.Click=\"ExecuteBack\"" in guided
assert "Command.Click=\"ExecuteContinue\"" in guided
print("PASS: animated native main menu remains active under World Gauntlet layer (STATIC ONLY)")
