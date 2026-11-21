#!/usr/bin/env python3
"""v0.70k source-only native noble spouse and Lord heir contract."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/"Source"/"CalradiaUnified"
read=lambda name:(src/name).read_text(encoding="utf-8")
policy=read("SandboxLordStartPolicy.cs")
spouse=read("SandboxGeneratedSpouseOrigin.cs")
family=read("SandboxOriginBehavior.LordFamily.cs")
profile=read("SandboxOriginProfile.cs")
creator=read("CUGuidedCreatorVM.cs")
preflight=read("SandboxStartingLifePreflight.cs")
origin=read("SandboxOriginBehavior.cs")
inherited=read("SandboxLordSuccessionPolicy.cs")
record=read("SandboxLordInheritanceState.cs")
native=read("SandboxOriginBehavior.LordInheritance.cs")
for token in ("ValidateNobleSpouseRequest(", "ValidateNativeNobleSpouse(",
              "native_noble_married_parents_missing",
              "native_noble_origin_clan_mismatch"):
    assert token in policy,token
assert '!profile.LordGuidedStart' in spouse
assert 'guided_lord_requires_native_married_noble_parents' not in spouse
assert 'return birthClan!=null?' in spouse
assert 'unrecordedNativeParents' in family
assert 'unrecordedNativeParents);' in family
assert 'h.Spouse.IsLord' in spouse
assert 'PreflightGuidedLord(' in spouse
assert 'SandboxGeneratedSpouseOrigin' in preflight
assert '.PreflightGuidedLord(p,player);' in preflight
assert 'noble=SandboxLordStartPolicy.ValidateNativeNobleSpouse(' in family
for token in ('spouse.OriginClan','nativeFather.Spouse==nativeMother',
              'nativeMother.Spouse==nativeFather',
              'nativeFather.Children.Contains(spouse)',
              'nativeMother.Children.Contains(spouse)',
              'nativeFather.Clan==bornClan'):
    assert token in family,token
assert 'public string LordSuccessionRule="EldestChild";' in profile
assert 'LordSuccessionRule="EldestChild";' in profile
assert 'case "LordSuccessionRule":' in profile
assert '"LordSuccessionRule=" + LordSuccessionRule' in profile
assert 'SandboxLordStartPolicy.ValidateNobleSpouseRequest(' in profile
assert 'SandboxLordSuccessionPolicy.Rules' in creator
assert 'new[]{"NobleRandom","NobleChosen"}' in creator
assert 'candidate.SpouseBackground="NobleRandom";' in creator
assert 'candidate.LordSuccessionRule=' in creator
assert 'CanSetNativeClanLeader(){return false;}' in inherited
assert 'CanTransferNativeFief(){return false;}' in inherited
assert 'HasNativeRegency(){return false;}' in inherited
assert 'EldestChild="EldestChild"' in inherited
assert 'MalePreference="MalePreference"' in inherited
assert 'NativeParentsVerified' in inherited
assert 'InFoundingClan' in inherited
assert 'NativeHeirLeadershipObserved' in inherited
assert 'MinorClaimNativeDecisionPending' in inherited
assert 'public const string Version="CU_LORD_HEIRS_1";' in record
assert 'public static SandboxLordInheritanceState Parse' in record
assert 'SandboxLordSuccessionPolicy.Order(Rule,heirs)' in record
assert 'Status=SandboxLordSuccessionPolicy.Status(' in record
assert 'VerifiedLordChildCandidates(' in native
assert 'c.Father.Children.Contains(c)' in native
assert 'c.Mother.Children.Contains(c)' in native
assert 'Clan.All.FirstOrDefault(' in native
assert 'lordInheritance.Observe(' in native
assert 'lordInheritance.Begin(' in native
assert 'RegisterLordInheritanceMenus(' in native
assert 'ObserveLordInheritanceDaily);' in origin
assert 'RegisterLordInheritanceMenus(starter);' in origin
assert 'CU_v070k_lord_native_heir_claims' in origin
assert 'SandboxLordInheritanceState.Parse(heirs)' in origin
assert 'BeginGuidedLordInheritance(' in origin
# Native state should be read, not overwritten by our family-claim model.
for text in (native,inherited,record,policy):
    for forbidden in ('ChangeOwnerOfSettlementAction','ChangeClanLeaderAction',
                      'ChangeKingdomAction.Apply','Hero.MainHero.Clan=',
                      'Clan.PlayerClan.Leader='):
        assert forbidden not in text,forbidden
print("PASS v0.70k SOURCE: guided Lord has real noble in-laws, "
      "read-only saved heir claims, and no forged native inheritance. "
      "LIVE GAME, DEATH, AND SAVE/RELOAD NOT TESTED.")
