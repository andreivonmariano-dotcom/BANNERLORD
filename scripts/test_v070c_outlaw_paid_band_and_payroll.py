#!/usr/bin/env python3
"""v0.70c single Outlaw start: native bandits, real wages and no desertion immunity.
These assertions inspect source structure only, never certify live gameplay.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
read=lambda name:(root/name).read_text(encoding="utf-8")
p=read("SandboxOriginProfile.cs")
vm=read("CUGuidedCreatorVM.cs")
behavior=read("SandboxOriginBehavior.cs")
band=read("SandboxOutlawStartingBand.cs")
policy=read("SandboxOutlawBandPolicy.cs")
retinue=read("SandboxCulturalRetinue.cs")
noble=read("SandboxNobleStartPolicy.cs")
native=read("SandboxRoleConsistencyPolicy.cs")
family=read("SandboxFamilyCreator.cs")

assert 'case "Outlaw":return "outlaw";' in native
assert 'public bool OutlawGuidedStart;' in p
assert 'OutlawGuidedStart=false;' in p
assert '"OutlawGuidedStart=" +' in p
assert 'case "OutlawGuidedStart":' in p
assert 'RetinueMode!="RoleBased" || Troops!=0 ||' in p
assert 'candidate.OutlawGuidedStart=candidate.Role=="Outlaw";' in vm
assert 'candidate.Troops=0;' in vm
assert 'candidate.FamilyHomeMode="None";' in vm
assert 'candidate.RetinueMode="RoleBased";' in vm
assert '"RoleBased":SandboxServicePolicy.DefaultRetinue(' in vm
assert 'key=="Troops"' in vm
assert 'Retinue: one culturally trained band lieutenant' not in vm
assert 'Band: six low-tier native bandits' in vm
assert 'Wages: fourteen-day native payroll reserve' in vm
assert 'if(profile.OutlawGuidedStart)' in behavior
assert 'troops = profile.OutlawGuidedStart ? 0 :' in behavior
assert 'SandboxOutlawStartingBand.TryGrant(' in behavior
assert 'reason=outlaw_band_not_verified' in behavior
assert 'cu_outlaw_band_start_contract' in behavior
assert 'outlaw_band="+outlawBand' in behavior
assert 'cu_outlaw_lieutenant_start_contract' in behavior
assert 'case "Lord":return new[]{"MasterOfArms","Knight"};' in noble
assert 'case "Outlaw":return new[]{"SecondInCommand"};' in noble
assert 'if(office=="SecondInCommand")return "Band Lieutenant";' in noble
assert 'Occupation.Lord:Occupation.Wanderer' in retinue
assert 'profile.Role=="Outlaw"' in family
assert 'cu_independent_outlaw_ancestry_recorded' in family

# There is no invented, permanently-free troop wage. The model's actual
# post-grant wage and party-size cap control a finite starting bank.
assert 'Occupation.Bandit' in band
assert 'CharacterObject.All.Where(' in band
assert 'c.Tier>=1 && c.Tier<=2' in band
assert 'SandboxOutlawBandPolicy.CulturalBanditFactions.Contains(' in band
assert 'GetCharacterWage(native)' in band
assert 'GetTotalWage(' in band
assert 'party.Party.PartySizeLimit' in band
assert 'party.MemberRoster.TotalManCount+add>limit' in band
assert 'party.MemberRoster.AddToCounts(native,add);' in band
assert 'banditsBefore+add' in band
assert 'RequiredStartingGold(' in band
assert 'GetObject<ItemObject>("grain")' in band
assert 'GetItemNumber(grain)' in band
assert 'GiveGoldAction.ApplyBetweenCharacters(' in band
assert 'cu_outlaw_band_wage_buffer' in band
assert 'cu_outlaw_band_partial_reconciliation' in band
assert 'normal_wages_and_desertion_preserved=true' in band
assert 'PayrollDays=14' in policy
assert 'TargetBandits=6' in policy
assert 'StartingGrain=12' in policy
assert 'MaximumInitialParty=12' in policy
assert 'MaximumDailyWage=250' in policy
assert 'actualDailyWage*PayrollDays+FoodCashReserve' in policy
assert 'MinimumCashReserve' in policy
assert 'party_size_limit_too_small' in policy

print("PASS v0.70c SOURCE: six native low-tier bandits + lieutenant, "
      "14-day observed wage reserve, starting grain, real party cap, "
      "no recurring subsidies or desertion exemptions. "
      "NATIVE GAME RETEST REQUIRED.")
