#!/usr/bin/env python3
"""v0.70f source-only policy check for all three approved contract choices.

Not an offer UI, employer payment, saved escrow or native game test.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
src=(root/"Source/CalradiaUnified/SandboxMercenarySolvencyPolicy.cs").read_text(encoding="utf-8")
tests=(root/"Tests/PolicySmoke/Program.cs").read_text(encoding="utf-8")
doc=(root/"docs/V070F_MERCENARY_THREE_CONTRACT_DECISIONS.md").read_text(encoding="utf-8")
for method in ("AcceptanceDecision(", "ProjectedDeficit(", "CoverageReview(",
               "RevisionDecision(", "ExitBondReleaseDecision("):
    assert method in src
    assert "SandboxMercenarySolvencyPolicy."+method in tests
for condition in (
    "explicit_loss_confirmation_required",
    "player_confirmed_loss_making_offer",
    "declined",
    "refused_unfunded_security",
    "accepted_below_preferred_margin",
    "protected_not_spendable",
    "no_verified_escrow",
    "already_released_no_repeat",
    "forfeit_unearned_bond",
    "eligible_once_on_native_exit",
    "renegotiate_headcount",
    "renegotiate_wages",
    "renegotiate_headcount_and_wages",
    "renegotiation_pending_both_consents",
    "revision_refused_unfunded",
    "revision_ready_for_native_commit",
):
    assert condition in src,condition
assert 'if(!playerClickedAccept)return "declined";' in src
assert 'if(!playerAccepted || !employerAccepted)' in src
assert 'bool moreWages=actualNativeDailyWage>coveredDailyWage;' in src
assert 'bool tooMany=currentHeadcount>coveredHeadcount;' in src
assert 'if(alreadyPaid)return "already_released_no_repeat";' in src
assert 'if(!realEscrowFunded)return "no_verified_escrow";' in src
assert '150,130,1050,3150,25,true,false' in tests
assert '150,130,1050,3150,25,true,true' in tests
assert 'CoverageReview(' in tests
assert 'ExitBondReleaseDecision(' in tests
for text in ("Q1 |", "Q2 |", "Q3 |", "two separate affirmative actions",
             "separate, campaign-save-backed escrow ledger",
             "both", "Not implemented"):
    assert text.lower() in doc.lower(),text
assert "GiveGoldAction" not in src
assert "ChangeKingdomAction" not in src
print("PASS v0.70f: explicit player risk acknowledgment, protected exit "
      "eligibility and consensual two-ceiling renegotiation. "
      "NATIVE PAYMENT AND NEGOTIATION GAMEPLAY NOT IMPLEMENTED.")
