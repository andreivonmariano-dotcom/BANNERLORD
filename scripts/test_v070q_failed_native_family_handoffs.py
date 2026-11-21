#!/usr/bin/env python3
"""Source regression from the three real v0.70n user sessions.

The first native Lord had 3 fiefs; the native King had 6.
Neither was allowed to enter the custom-family phase after a
false noble-status gate. Knight was saved with no family and no
officers under the old forced FamilyOnly default.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda p:(src/p).read_text(encoding="utf-8")
gate=read("SandboxNativeNobleHandoffPolicy.cs")
pre=read("SandboxStartingLifePreflight.cs")
final=read("SandboxOriginBehavior.cs")
vm=read("CUGuidedCreatorVM.cs")
profile=read("SandboxOriginProfile.cs")
ministry=read("SandboxKingNativeMinistry.cs")
assert 'HasVerifiedNativeNobleAuthority(' in gate
assert 'nativeType=="king"?rulingClan:!rulingClan' in gate
assert 'mercenary || bandit' in gate and 'rebel || nativeFiefs<1' in gate
assert 'bool nativeNobleHouse=SandboxNativeNobleHandoffPolicy' in pre
assert 'bool verifiedNobleHandoff=SandboxNativeNobleHandoffPolicy' in final
assert pre.count('nativeNobleHouse,')==2
assert final.count('verifiedNobleHandoff,')==2
assert '&& player.IsLord,' not in pre
assert '&& hero.IsLord,' not in final
assert 'hero_is_lord_at_handoff=' in pre
assert 'clan_minor_flag=' in pre and 'clan_eliminated_flag=' in pre
assert 'candidate.SpouseEnabled=true;' in vm
assert 'candidate.ChildCount=Math.Max(1,candidate.ChildCount);' in vm
assert 'candidate.KnightGuidedStart ?' in vm
assert 'key=="SpouseEnabled" && !copy.SpouseEnabled' in vm
assert '(RetinueMode!="FamilyOnly" && RetinueMode!="RoleBased")' in profile
assert 'candidate.RetinueMode="RoleBased";' in vm
assert 'SandboxFamilyCreator.TryCreate(profile,hero)' in final
assert 'SandboxCulturalRetinue.TryCreate(' in final or 'SandboxCulturalRetinue' in final
assert 'return preferred;' in ministry
assert 's.Town.Governor==null' in ministry
assert 'ThenBy(s=>s.StringId,StringComparer.Ordinal)' in ministry
assert 'new[]{"Squire"}' in read("SandboxNobleStartPolicy.cs")
assert '"Marshal","Kingsguard","RoyalMinister"' in (
    read("SandboxKingStartPolicy.cs"))
print("PASS v0.70q: native role receipt passes through one shared "
      "preflight/final gate. Knight proposes spouse, child and one "
      "Squire, without forced marriage. Real playthrough still required.")
