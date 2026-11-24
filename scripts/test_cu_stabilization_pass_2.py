#!/usr/bin/env python3
"""Source wiring checks only; no live Bannerlord test."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
overlay=read("SandboxCreatorOverlay.cs")
living=read("SandboxLivingPersons.cs")
origin=read("SandboxOriginBehavior.cs")
bridge=read("SandboxSaSBridge.cs")
assert "if(wasAttached && !SandboxCreatorMenuLayer.IsAttached &&" in overlay
assert "creatorOpen)\n            {\n                Cancel();" in overlay
assert 'cancelled=true;\n                    acceptedCustomStart=false;' in overlay
assert 'SandboxStartingLifePolicy.Station(profile,verifiedSoldierStart)' in living
assert 'SandboxStartingLifePolicy.Occupation(profile,verifiedSoldierStart)' in living
assert 'profile.SoldierGuidedStart &&\n                            soldierReceipt.Length==0' in origin
assert 'SandboxSoldierEnlistmentPolicy.NativeRankNumber(' in bridge
assert 'result.RankId)==0)return new Snapshot();' in bridge
print("PASS: stabilization pass 2 source wiring; live UI and gameplay untested")
