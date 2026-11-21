#!/usr/bin/env python3
"""v0.70d new Mercenary path regression.

Pure/source checks only. A PASS cannot certify native mercenary pay,
faction standing, party leadership or save/reload in the actual game.
"""
from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
read=lambda n:(root/n).read_text(encoding="utf-8")
p=read("SandboxOriginProfile.cs")
vm=read("CUGuidedCreatorVM.cs")
native=read("SandboxRoleConsistencyPolicy.cs")
aso=read("SandboxNativeStartOptions.cs")
service=read("SandboxServicePolicy.cs")
family=read("SandboxFamilyCreator.cs")
preflight=read("SandboxStartingLifePreflight.cs")
behavior=read("SandboxOriginBehavior.cs")
economy=read("SandboxRoleEconomyPolicy.cs")
policy=read("SandboxMercenaryStartPolicy.cs")
role=read("SandboxRolePolicy.cs")
retinue=read("SandboxCulturalRetinue.cs")
legacy=read("SandboxStartingLifePolicy.cs")

# The first three reviewed starts sit before existing compatibility roles.
roles_match=re.search(
    r'public static readonly string\[\] Roles\s*=\s*\{(.*?)\};',p,re.S)
assert roles_match
roles=re.findall(r'"([A-Za-z]+)"',roles_match.group(1))
assert roles[:3]==["Wanderer","Outlaw","Mercenary"],roles
assert len(roles)==len(set(roles))==13,roles
assert 'case "Soldier":' in native
assert 'case "Mercenary":return "mercenary";' in native
assert 'case "Outlaw":return "outlaw";' in native
assert 'type == "mercenary"' in aso
assert 'types.SetValue(type);' in aso
assert 'RequiresKingdom(type)' in aso

# New roles have a separate versioned flag. Old saved Soldier profiles
# keep their existing native mercenary meaning and field/parser keys.
assert 'public bool MercenaryGuidedStart;' in p
assert 'MercenaryGuidedStart=false;' in p
assert 'case "MercenaryGuidedStart":' in p
assert '"MercenaryGuidedStart=" +' in p
assert 'candidate.MercenaryGuidedStart=' in vm
assert 'candidate.Role=="Mercenary";' in vm
assert 'candidate.OutlawGuidedStart=candidate.Role=="Outlaw";' in vm
assert 'candidate.WandererNoHouseStart=candidate.Role=="Wanderer";' in vm

# Mercenary asks for a REAL kingdom via native Advanced Start and
# has no automatic fief, static stipend, invented court or lord patron.
for snippet in (
    'candidate.StartingFiefMode="Native";',
    'candidate.FamilyHomeMode="None";',
    'candidate.RoleAffiliationsEnabled=false;',
    'candidate.RetinueMode="FamilyOnly";',
    'candidate.ServicePatronHeroId="";',
    'candidate.Gold=-1;',
    'candidate.Influence=-1;',
    'candidate.Troops=0;',
    'candidate.Role=="Mercenary";',
    'No noble rank, fief or NPC-party enlistment',
):
    assert snippet in vm,snippet
assert 'String.IsNullOrEmpty(StartingKingdomId)' in p
assert 'RetinueMode!="FamilyOnly"' in p
assert 'Gold!=-1 || Influence!=-1 || Troops!=0' in p
assert 'NativeMercenaryContract' in economy
assert 'case "Mercenary":' in economy
assert 'role=="Mercenary"' in role
assert 'role=="Mercenary"' in service
assert '"MercenaryCompany"' in legacy
assert 'if(profile.MercenaryGuidedStart)' in family
assert 'cu_independent_mercenary_ancestry_recorded' in family

# The native role must actually be hired, not simply in a kingdom.
assert 'SandboxMercenaryStartPolicy.Validate(' in preflight
assert 'clan.IsUnderMercenaryService' in preflight
assert 'clan.Leader==player' in preflight
assert 'MobileParty.MainParty.LeaderHero==player' in preflight
assert 'cu_mercenary_native_preflight' in preflight
assert 'SandboxMercenaryStartPolicy.Validate(' in behavior
assert 'observedClan.IsUnderMercenaryService' in behavior
assert 'cu_mercenary_company_native_verification' in behavior
assert 'native_mercenary_contract_unverified:' in behavior
assert 'mercenary_receipt="+mercenaryReceipt' in behavior
for phrase in (
    'native_mercenary_contract_missing',
    'native_contract_kingdom_mismatch',
    'mercenary_land_or_rulership_invalid',
    'player_does_not_lead_company_clan',
    'player_not_leading_independent_party',
):
    assert phrase in policy
assert 'profile.RetinueMode' in behavior
assert 'profile.MercenaryGuidedStart' in behavior
assert 'RemoveCompanionAction.ApplyByFire(' in retinue

print("PASS v0.70d SOURCE: separate Mercenary company, native "
      "kingdom contract, self-command, landless optional household, "
      "no invented wage or forced troop grants. LIVE GAME TEST REQUIRED.")
