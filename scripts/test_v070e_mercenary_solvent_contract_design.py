#!/usr/bin/env python3
"""v0.70e mercenary solvency DESIGN + PURE POLICY wire check.

This is NOT a native offer UI, employer escrow, contract join/leave, cash
transfer, actual payroll or live-game acceptance. It only locks the math.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
policy=(root/"Source/CalradiaUnified"/
        "SandboxMercenarySolvencyPolicy.cs").read_text(encoding="utf-8")
profile=(root/"Source/CalradiaUnified"/
         "SandboxOriginProfile.cs").read_text(encoding="utf-8")
native=(root/"Source/CalradiaUnified"/
        "SandboxRoleConsistencyPolicy.cs").read_text(encoding="utf-8")
preflight=(root/"Source/CalradiaUnified"/
           "SandboxStartingLifePreflight.cs").read_text(encoding="utf-8")
rfc=(root/"docs"/"V070E_MERCENARY_SOLVENCY_NEGOTIATION_RFC.md").read_text(
    encoding="utf-8")
csproj=(root/"Tests"/"PolicySmoke"/"PolicySmoke.csproj").read_text(
    encoding="utf-8")
tests=(root/"Tests"/"PolicySmoke"/"Program.cs").read_text(encoding="utf-8")
for symbol in (
    "StartingRunwayDays=30",
    "StartingContingencyPercent=10",
    "InitialFoodDays=14",
    "PayrollAdvanceDays=7",
    "ExitBondDays=21",
    "DefaultMarginPercent=25",
    "DailyOperatingCost(",
    "RequiredGuaranteedDailyPay(",
    "StartingTreasuryTarget(",
    "StartingTopUp(",
    "PayrollAdvance(",
    "ExitBond(",
    "OfferStatus(",
    "GuaranteedCoveragePercent(",
    "invalid_financial_quote",
    "loss_making",
    "below_requested_margin",
    "payroll_advance_not_funded",
    "exit_bond_not_funded",
    "contract_financially_eligible",
):
    assert symbol in policy,symbol
assert "SandboxMercenarySolvencyPolicy.cs" in csproj
assert "SandboxMercenarySolvencyPolicy" in tests
assert "StartingTreasuryTarget(150)==4950" in tests
assert "PayrollAdvance(150)==1050" in tests
assert "ExitBond(150)==3150" in tests
assert "OfferStatus(" in tests
for text in (
    "Uncontracted Company",
    "30-day cash runway",
    "14 days of actual edible inventory",
    "125%",
    "21-day exit bond",
    "actual employer money",
    "No double payment",
    "legacy Soldier",
    "not a gameplay feature",
):
    assert text.lower() in rfc.lower(),text

# The v0.70d native start is still contracted. No made-up compatibility
# claim about accepting / declining a contract in-game.
assert 'case "Mercenary":return "mercenary";' in native
assert "p.MercenaryGuidedStart" in preflight
assert 'clan.IsUnderMercenaryService' in preflight
assert 'ChangeKingdomAction.ApplyByJoinFactionAsMercenary' not in policy
assert 'GiveGoldAction' not in policy
print("PASS v0.70e: mercenary pure solvency math, funded-offer tests, "
      "native-integration blockers documented. GAMEPLAY NOT IMPLEMENTED.")
