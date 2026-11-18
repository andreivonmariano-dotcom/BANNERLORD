using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
namespace CalradiaUnified
{
    /// <summary>Castle-only manual controls. No autonomous map parties.</summary>
    public sealed partial class CivicBehavior
    {
        private static bool OwnedCastle()
        {
            var s = Settlement.CurrentSettlement;
            return Owned() && s != null && s.IsCastle && s.Town != null;
        }
        private void RegisterCastleManagementMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_castle_staffing_report",
                "CU: Review castle staffing and modeled wage affordability",
                x => OwnedCastle(), x => Safe(ShowCastleStaffing), false);
            starter.AddGameMenuOption(menu, "cu_castle_deposit_recruits",
                "CU: Move 5 owned local recruits into castle garrison",
                x => CanTransferRecruits(true),
                x => Safe(() => TransferRecruits(true)), false);
            starter.AddGameMenuOption(menu, "cu_castle_withdraw_recruits",
                "CU: Move 5 local recruits back to player party",
                x => CanTransferRecruits(false),
                x => Safe(() => TransferRecruits(false)), false);
            starter.AddGameMenuOption(menu, "cu_castle_manual_wage_limit",
                "CU: Apply affordable castle wage cap (no troop layoffs)",
                x => CanApplyManualWageLimit(), x => Safe(ApplyManualWageLimit), false);
        }
        private void ShowCastleStaffing()
        {
            if (!OwnedCastle()) return;
            var castle = Settlement.CurrentSettlement;
            var g = castle.Town.GarrisonParty;
            var p = MobileParty.MainParty;
            var troop = LocalRecruit();
            int wages = g == null ? 0 : Math.Max(0, g.TotalWage);
            int troops = g == null || g.MemberRoster == null ?
                0 : g.MemberRoster.TotalManCount;
            int gLocal = g == null || g.MemberRoster == null || troop == null ?
                0 : g.MemberRoster.GetTroopCount(troop);
            int pLocal = p == null || p.MemberRoster == null || troop == null ?
                0 : p.MemberRoster.GetTroopCount(troop);
            int recommended = WageBudgetFor(castle, Math.Max(1, OwnedCastleCount()));
            Notice("Castle " + castle.Name + ": garrison " + troops +
                " (" + gLocal + " local recruits), party local recruits " + pLocal +
                ", garrison wage " + wages + "/day, current wage limit " +
                castle.GarrisonWagePaymentLimit + ", forecast-affordable limit " +
                recommended + ", siege " + (castle.SiegeEvent != null) +
                ". Native tax is not measured by this report.");
        }
        private bool CanTransferRecruits(bool toGarrison)
        {
            if (!OwnedCastle()) return false;
            var s = Settlement.CurrentSettlement;
            var g = s.Town.GarrisonParty;
            var p = MobileParty.MainParty;
            var troop = LocalRecruit();
            if (g == null || p == null || troop == null ||
                g.MemberRoster == null || p.MemberRoster == null) return false;
            var from = toGarrison ? p.MemberRoster : g.MemberRoster;
            var to = toGarrison ? g.MemberRoster : p.MemberRoster;
            int cap = toGarrison ? settings.GarrisonTrainingMaxTroops :
                settings.RecruitmentMaxPartySize;
            return CastleManagementPolicy.CanMove(
                settings.EnableManualGarrisonTransfers, true, true,
                s.SiegeEvent != null, true, true,
                from.GetTroopCount(troop), to.TotalManCount,
                cap, CastleManagementPolicy.TransferBatch);
        }
        private void TransferRecruits(bool toGarrison)
        {
            if (!CanTransferRecruits(toGarrison)) return;
            var troop = LocalRecruit();
            var g = Settlement.CurrentSettlement.Town.GarrisonParty.MemberRoster;
            var p = MobileParty.MainParty.MemberRoster;
            var from = toGarrison ? p : g;
            var to = toGarrison ? g : p;
            int fromBefore = from.GetTroopCount(troop);
            int toBefore = to.GetTroopCount(troop);
            from.AddToCounts(troop, -CastleManagementPolicy.TransferBatch);
            try
            {
                to.AddToCounts(troop, CastleManagementPolicy.TransferBatch);
                if (from.GetTroopCount(troop) != fromBefore -
                    CastleManagementPolicy.TransferBatch ||
                    to.GetTroopCount(troop) != toBefore +
                    CastleManagementPolicy.TransferBatch)
                    throw new InvalidOperationException("Roster transfer changed unexpected counts");
            }
            catch
            {
                int extra = to.GetTroopCount(troop) - toBefore;
                if (extra > 0) to.AddToCounts(troop, -extra);
                int missing = fromBefore - from.GetTroopCount(troop);
                if (missing > 0) from.AddToCounts(troop, missing);
                throw;
            }
            DiagnosticJournal.Event("manual_garrison_transfer",
                "settlement=" + CurrentId() + "|direction=" +
                (toGarrison ? "deposit" : "withdraw") + "|count=5");
            Notice("Moved 5 existing local recruits " +
                (toGarrison ? "into the castle garrison." : "back to your party."));
        }
        private bool CanApplyManualWageLimit()
        {
            if (!OwnedCastle()) return false;
            var s = Settlement.CurrentSettlement;
            var g = s.Town.GarrisonParty;
            int wages = g == null ? 0 : Math.Max(0, g.TotalWage);
            return CastleManagementPolicy.CanApplyCap(
                settings.EnableManualGarrisonWageCaps, true, true,
                s.SiegeEvent != null, s.GarrisonWagePaymentLimit,
                WageBudgetFor(s, Math.Max(1, OwnedCastleCount())),
                settings.EstateMinimumDefensiveWage, wages);
        }
        private void ApplyManualWageLimit()
        {
            if (!CanApplyManualWageLimit()) return;
            var s = Settlement.CurrentSettlement;
            int previous = s.GarrisonWagePaymentLimit;
            int newLimit = WageBudgetFor(s, Math.Max(1, OwnedCastleCount()));
            s.SetGarrisonWagePaymentLimit(newLimit);
            DiagnosticJournal.Event("manual_castle_wage_limit",
                "settlement=" + CurrentId() + "|previous=" +
                previous + "|new=" + newLimit);
            Notice("Castle wage limit set to " + newLimit +
                ". Actual troop wages are already within the limit.");
        }
    }
}
