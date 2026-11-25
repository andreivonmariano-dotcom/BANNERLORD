"""Regression gates for existing creator state and NPC save integrity.

Source wiring verification, not rendered UI or native game save testing.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"Source"/"CalradiaUnified"
def code(name):
    return (root/name).read_text(encoding="utf-8")

vm=code("CUGuidedCreatorVM.cs")
stats=code("CUNativeStatsLayer.cs")
house=code("CUNativeHouseLayer.cs")
navigation=code("CUNativeHouseNavigationPolicy.cs")
agency=code("SandboxAgencyDecisionState.cs")
kin=code("SandboxFamilyRecoveryState.cs")

# The House overlay must keep each edited choice as a profile snapshot,
# survive conditional field removal and reopen after native Review Back.
assert "profile=SandboxOriginProfile.Parse(houseDraft.Serialize());" in vm
assert "houseDraft=SandboxOriginProfile.Parse(profile.Serialize());" in vm
write_start=vm.index("private bool TryWrite(")
write_end=vm.index("public void ExecutePreviousChoice()",write_start)
write=vm[write_start:write_end]
assert "var copy=SandboxOriginProfile.Parse(profile.Serialize());" in write
assert "if(!copy.TrySet(key,value))" in write
assert write.index("if(!copy.TrySet(key,value))")<write.index("profile=copy;")
assert "string currentKey=Current==null ? key : Current.Key;" in write
assert "Array.FindIndex(afterWrite," in write
assert "f=>f.Key==currentKey" in write
assert "houseDraft=SandboxOriginProfile.Parse(profile.Serialize());" in write

adv_start=vm.index("public void ExecuteAdvanced()")
adv_end=vm.index("public void ExecuteBack()",adv_start)
adv=vm[adv_start:adv_end]
assert "string selected=Current==null?\"\":Current.Key;" in adv
assert "cursor=matched>=0" in adv

house_commit=vm[vm.index("private void CommitHouse()"):
    vm.index("[DataSourceProperty] public bool IsWorldSetup")]
assert "candidate.SaveForPlayer(" in house_commit
assert house_commit.index("candidate.SaveForPlayer(")<house_commit.index(
    "houseDraft=null;")
assert "HouseRevision++;" in house_commit

# Stats uses a separate requested spouse dictionary and founder baseline.
assert "profile=SandboxOriginProfile.Parse(source.Serialize());" in stats
assert "profile.SpouseOverrides" in stats
assert "profile.Overrides" in stats
assert "public void ExecuteEditSpouse()" in stats
assert "public void ExecuteEditPlayer()" in stats
assert "CUNativeStatsNavigationPolicy.BackToNativeNarrative(pageIndex)" in stats
assert "CUNativeStatsNavigationPolicy.IsFinal(pageIndex)" in stats
s=stats[stats.index("public void ExecuteContinue()"):
    stats.index("[DataSourceProperty] public string Title")]
assert s.index("if(!profile.SaveForPlayer(")<s.index("completed();")
assert "oldBirthday" in s and "hero.SetBirthDay(oldBirthday)" in s

assert "CUNativeHouseNavigationPolicy.ReviewBackNeedsHouse(" in house
assert "CUNativeHouseNavigationPolicy.CanReplayNativeNext(stage)" in house
assert "returnToReviewAttempts<3" in house
assert "stage==ClanStage || stage==BannerStage" in navigation

# Saved character requests can resume only from matching quota history.
assert "if(result.HasPending)" in agency
assert "pendingRecord.LastDay!=result.PendingOpenedDay" in agency
assert "pendingRecord.RequestsMade<1" in agency
assert "last<0 || last>day" in agency
assert "(long)Day-r.LastDay" in agency

# Corrupt count evidence does not mutate a native family recovery dossier.
assert 'return "invalid_native_counts";' in kin
assert "observedChildren=Math.Min(99,children)" in kin
assert "Status!=previousStatus?" in kin

print("PASS v0.69f: creator drafts, native Back, saved NPC quota and family recovery are wired fail-closed (SOURCE ONLY)")
