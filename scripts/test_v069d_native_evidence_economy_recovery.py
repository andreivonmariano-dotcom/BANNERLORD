"""v0.69d native-effect proof, family recovery, economy, NPC source checks.

Static architecture checks, never a replacement for installed game testing.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
def read(path):
    return (src/path).read_text(encoding="utf-8")

menu=read("SandboxOriginBehavior.LivingMenus.cs")
behavior=read("SandboxOriginBehavior.cs")
retinue=read("SandboxCulturalRetinue.cs")
recovery=read("SandboxFamilyRecoveryState.cs")
role=read("SandboxRoleEconomyPolicy.cs")
economy=read("SandboxRoleEconomyState.cs")
effect=read("SandboxNativeEffectProof.cs")
agency=read("SandboxAgencyDecisionPolicy.cs")
court=read("SandboxInheritanceNegotiationState.cs")

assert 'SandboxNativeEffectProof.GiftResult(' in menu
assert menu.count('SandboxNativeEffectProof.GiftResult(')>=2
assert menu.count('SandboxNativeEffectProof.RelationConfirmed(')>=2
assert 'recipientBefore=actor.Gold' in menu
assert 'recipientBefore=leader.Gold' in menu
assert 'verified_denars=' in menu
assert 'CharacterGiftUnverified' in menu
assert 'CulturalMediationPaymentUnverified' in menu
assert 'giftProof!="no_native_effect"' in menu
assert 'relation_confirmed=' in menu
assert 'native_gift_status=' in menu
assert 'recipientAfter-recipientBefore' in effect

assert 'CU_v069d_family_recovery' in behavior
assert 'familyRecovery=SandboxFamilyRecoveryState.Parse(recovery)' in behavior
assert 'RecordFailedNativeFamily(profile,hero,postFamilyGate)' in behavior
assert 'RecordFailedNativeFamily(profile,hero,' in behavior
assert 'familyRecovery.Observe(recoveryPlayer.StringId' in behavior
assert 'cu_partial_family_review_' in menu
assert 'No new Heroes or starting gifts' in recovery
assert 'NativeCountsMatchReviewStillRequired' in recovery
assert 'no_matching_partial_start' in recovery

assert 'CU_v069d_role_economy' in behavior
assert 'SandboxRoleEconomyState.Parse(economy)' in behavior
assert 'roleEconomy.Start(profile.Role,hero.Gold' in behavior
assert 'roleEconomy.Observe(' in behavior
assert 'verifiedExternalDailyWage' not in economy
assert 'VerifiedExternalDailyWage' in economy
assert 'Unattributed native debits' in economy
assert 'NativePersonalGoldBalanceObserved' in behavior
assert 'SandboxRoleEconomyPolicy.CareerEvidence(' in behavior
for key in ('King','Lord','Knight','Soldier','Admiral','Trader',
            'Outlaw','Beggar','Wanderer','Recruit','HouseholdKnight',
            'NobleCadet'):
    assert ('case "'+key+'"') in role

assert 'SandboxAgencyDecisionPolicy.ChooseInitiative(' in menu
assert 'hero.GetTraitLevel(DefaultTraits.Honor)' in menu
assert 'hero.GetTraitLevel(DefaultTraits.Generosity)' in menu
assert 'hero.GetTraitLevel(DefaultTraits.Calculating)' in menu
assert 'nativeNpcGold>=1500' in agency
assert 'not_a_claim_of_native_ai_dialogue=true' in menu

assert '!inheritanceNegotiation.NativeHeadConsentObserved' in menu
assert 'public bool NativeHeadConsentObserved {get{return false;}}' in court
assert 'No estate transfer is authorized.' in menu

# The full new retinue must be present before native companions can be
# fired, and registered Heroes must never be disabled as fake orphans.
after_staging=retinue.index('for(int i=0;i<staged.Count;i++)')
officer_confirm=retinue.index('if(!confirmed)',after_staging)
replacement=retinue.index('if(replace)',after_staging)
assert after_staging < officer_confirm < replacement
assert 'multi_companion_dismissal_not_atomic' not in retinue
assert 'replaced!=existing.Length' in retinue
assert 'failed_removal_reported_partial=true' in retinue
assert 'native.Length>16' in retinue
assert 'pending.PartyBelongedTo!=null' in retinue
assert 'registered_preserved=' in retinue

print("PASS v0.69d: two-sided native effects, safe kin recovery, all-role finance and observed NPC choices (SOURCE ONLY)")
