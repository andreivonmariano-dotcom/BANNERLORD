#!/usr/bin/env python3
"""Source-level contract for native Review/name Back -> House and Fortune.

The C# PolicySmoke project checks the decision table itself. This file
checks integration and refusal to replace native Bannerlord stages.
A live 1.5.7 playtest is still needed to verify engine callbacks.
"""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
src=r/"Source/CalradiaUnified"
house=(src/"CUNativeHouseLayer.cs").read_text(encoding="utf-8")
policy=(src/"CUNativeHouseNavigationPolicy.cs").read_text(encoding="utf-8")
shell=(src/"CUNativeStateShell.cs").read_text(encoding="utf-8")
smoke=(r/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
project=(r/"Tests/PolicySmoke/PolicySmoke.csproj").read_text(encoding="utf-8")

assert "CharacterCreationReviewStage" in policy
assert "CharacterCreationClanNamingStage" in policy
assert "CharacterCreationBannerEditorStage" in policy
assert "CharacterCreationOptionsStage" in policy
assert "ReviewBackNeedsHouse" in house
assert "previousNativeStage,stage,houseWasShown,completed" in house
assert "returnToHousePending=true;" in house
assert "completed=false;" in house
assert "returnToReviewAttempts<3" in house
assert "manager.NextStage();" in house
assert "manager.PreviousStage();" in house
assert "cu_native_review_back_to_house" in house
assert "cu_native_review_house_reentry" in house
assert "cu_native_review_house_rejoin" in house
assert "cu_native_review_house_fallback" in house
assert "houseWasShown=true;" in house
assert "houseWasShown=false;" in house
assert "returnToHousePending=false;" in house
assert 'new CUGuidedCreatorVM(false,' in house
assert 'CUNativeHouseLayer.Tick(screen,' in shell
assert "CUNativeHouseNavigationPolicy.cs" in project
assert "native Review Back to Clan Name restores House/Fortune" in smoke
assert "native Review Back directly to Banner restores House/Fortune" in smoke
assert "House overlay Back stays native without recursive reentry" in smoke
for forbidden in ("new CharacterCreationScreen(",
    "RemoveStage<","new CharacterCreationReviewStage(",
    "new CharacterCreationClanNamingStage("):
    assert forbidden not in house,forbidden
print("PASS: native Review/name Back reopens House/Fortune with bounded game-owned Next transitions (STATIC ONLY)")
