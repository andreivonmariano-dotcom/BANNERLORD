using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class CivicBehavior
    {
        private void RegisterFeudalMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_feudal_budget",
                "Review estate accounts",
                x => CanVisit() && Owned() &&
                    Settlement.CurrentSettlement.IsCastle,
                x => Safe(ShowFeudalBudget), false);
        }

        private static int HealthyHearths(Settlement estate)
        {
            if (estate == null || estate.BoundVillages == null) return 0;
            long hearths = 0;
            foreach (var village in estate.BoundVillages)
            {
                if (village == null ||
                    village.VillageState != Village.VillageStates.Normal)
                    continue;
                hearths += Math.Max(0, (int)village.Hearth);
                if (hearths >= 20000) return 20000;
            }
            return (int)hearths;
        }

        private static int OwnClanFieldWages()
        {
            if (Clan.PlayerClan == null || MobileParty.AllLordParties == null) return 0;
            long wages = 0;
            foreach (var party in MobileParty.AllLordParties)
            {
                if (party == null || party.ActualClan != Clan.PlayerClan) continue;
                wages += Math.Max(0, party.TotalWage);
                if (wages >= 1000000) return 1000000;
            }
            return (int)wages;
        }

        private static int OwnedCastleCount()
        {
            if (Clan.PlayerClan == null) return 0;
            return Settlement.All.Count(x => x != null && x.IsCastle &&
                x.OwnerClan == Clan.PlayerClan);
        }

        private int EstateForecast(Settlement castle)
        {
            return castle == null || castle.Town == null ? 0 :
                FeudalBudgetPolicy.EstimateEstateIncome(
                    Math.Max(0, (int)castle.Town.Prosperity),
                    HealthyHearths(castle));
        }

        private int WageBudgetFor(Settlement castle, int count)
        {
            if (castle == null || count <= 0) return 0;
            int fieldShare = (OwnClanFieldWages() + count - 1) / count;
            return FeudalBudgetPolicy.WageBudget(EstateForecast(castle),
                fieldShare, settings.EstateOtherCostsReserve,
                settings.EstateTargetDailySurplus, settings.EstateMaxDailyGarrisonWage);
        }

        private void ShowFeudalBudget()
        {
            var castle = Settlement.CurrentSettlement;
            if (castle == null || !castle.IsCastle || !Owned()) return;
            int castles = Math.Max(1, OwnedCastleCount());
            int income = EstateForecast(castle);
            int fieldShare = (OwnClanFieldWages() + castles - 1) / castles;
            var garrison = castle.Town == null ? null : castle.Town.GarrisonParty;
            int actualGarrisonWages = garrison == null ? 0 :
                Math.Max(0, garrison.TotalWage);
            int cap = WageBudgetFor(castle, castles);
            int net = FeudalBudgetPolicy.ProjectedBalance(income,
                fieldShare, actualGarrisonWages, settings.EstateOtherCostsReserve, 0);
            int required = CastleManagementPolicy.RequiredIncome(fieldShare,
                actualGarrisonWages, settings.EstateOtherCostsReserve,
                settings.EstateTargetDailySurplus);
            int shortfall = CastleManagementPolicy.ModeledShortfall(income, required);
            Notice("Estate forecast for " + castle.Name + ": model income ~" +
                income + "/day, clan field wage share " + fieldShare +
                ", actual garrison wages " + actualGarrisonWages +
                ", other-expense allowance " + settings.EstateOtherCostsReserve +
                ", projected balance " + net + ", proposed garrison wage cap " +
                cap + ", minimum modeled income required " + required +
                ", modeled shortfall " + shortfall +
                ". This is NOT the game's exact tax or clan balance.");
            Log.Write("M1 fiscal forecast estate=" + castle.StringId +
                " modeledIncome=" + income + " fieldShare=" + fieldShare +
                " actualGarrisonWages=" + actualGarrisonWages +
                " proposedCap=" + cap + " projectedNet=" + net +
                " incomeRequired=" + required + " modeledShortfall=" + shortfall);
        }

        private void TickFeudalEstates()
        {
            if ((!settings.EnableAutoGarrisonWageCaps &&
                 !settings.EnableAutoGarrisonTraining &&
                 !settings.EnableVillageHarvest &&
                 !settings.EnableSurplusEstateSales) ||
                Clan.PlayerClan == null || Hero.MainHero == null) return;

            var castles = Settlement.All.Where(x => x != null && x.IsCastle &&
                x.OwnerClan == Clan.PlayerClan && x.Town != null).ToArray();
            int count = castles.Length;
            if (count == 0) return;
            foreach (var castle in castles)
            {
                // Always skip owner changes and active sieges. Never change a town.
                if (castle.SiegeEvent != null || !CivicState.Valid(castle.StringId))
                    continue;
                var garrison = castle.Town.GarrisonParty;
                int forecast = EstateForecast(castle);
                int fieldShare = (OwnClanFieldWages() + count - 1) / count;
                int cap = WageBudgetFor(castle, count);

                if (settings.EnableAutoGarrisonWageCaps &&
                    FeudalBudgetPolicy.MayLowerWageCap(true, true, true, false,
                        castle.GarrisonWagePaymentLimit, cap,
                        settings.EstateMinimumDefensiveWage))
                {
                    int previous = castle.GarrisonWagePaymentLimit;
                    castle.SetGarrisonWagePaymentLimit(cap);
                    Log.Write("M4 opt-in castle wage cap " + castle.StringId +
                        ": " + previous + " -> " + cap +
                        ". Actual troop wages may change later; verify in game.");
                }

                var depot = state.Get(castle.StringId);
                if (depot == null) continue;

                if (settings.EnableVillageHarvest)
                {
                    int crop = FeudalBudgetPolicy.VillageHarvest(
                        HealthyHearths(castle),
                        DepotLedgerPolicy.FreeCapacity(depot.Grain, settings.DepotCapacity),
                        settings.EstateHearthsPerGrain, settings.EstateMaxHarvestPerDay);
                    if (crop > 0)
                    {
                        // This is NEW virtual estate grain, not a native village item transfer.
                        depot.Grain += crop;
                        Log.Write("M1 abstract village harvest " + castle.StringId +
                            " grain=" + crop + ". No vanilla village inventory debited.");
                    }
                }

                if (settings.EnableSurplusEstateSales)
                {
                    int sold = FeudalBudgetPolicy.SurplusSale(depot.Grain,
                        settings.EstateGrainReserve, settings.EstateMaxDailyGrainSale);
                    if (sold > 0)
                    {
                        int proceeds = sold * settings.EstateSalePricePerGrain;
                        // Abstract market buyer: consume real saved estate grain first.
                        // This is NOT a physical caravan or a vanilla market transaction.
                        GiveGoldAction.ApplyBetweenCharacters(
                            null, Hero.MainHero, proceeds, true);
                        depot.Grain -= sold;
                        Log.Write("M1 abstract estate produce sale " + castle.StringId +
                            ": sold=" + sold + " grain, gold=" + proceeds);
                    }
                }

                if (settings.EnableAutoGarrisonTraining &&
                    settings.EnableGarrisonTraining &&
                    garrison != null && garrison.MemberRoster != null &&
                    castle.Culture != null && castle.Culture.BasicTroop != null &&
                    garrison.MemberRoster.TotalManCount < settings.EstateTargetGarrisonSize &&
                    FeudalBudgetPolicy.AffordableTraining(true, true, false,
                        forecast, fieldShare, Math.Max(0, garrison.TotalWage),
                        cap, settings.EstateOtherCostsReserve,
                        settings.EstateTargetDailySurplus, 50, PlayerGold(),
                        settings.EstateMinimumTrainingGold, 
                        garrison.MemberRoster.TotalManCount,
                        settings.EstateTargetGarrisonSize))
                {
                    var record = state.CouncilFor(castle.StringId);
                    if (record == null || !GarrisonTrainingPolicy.CanTrain(
                        true, true, false, true, true,
                        PlayerGold(), GarrisonGoldCost, depot.Grain, GarrisonGrainCost,
                        garrison.MemberRoster.TotalManCount,
                        Math.Min(settings.GarrisonTrainingMaxTroops,
                            settings.EstateTargetGarrisonSize),
                        record.GarrisonToday, settings.GarrisonTrainingDailyCap,
                        GarrisonBatch))
                        continue;
                    // Existing garrison only, one training action per castle-day.
                    garrison.MemberRoster.AddToCounts(castle.Culture.BasicTroop,
                        GarrisonBatch);
                    try
                    {
                        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero,
                            -GarrisonGoldCost, true);
                    }
                    catch
                    {
                        garrison.MemberRoster.AddToCounts(castle.Culture.BasicTroop,
                            -GarrisonBatch);
                        throw;
                    }
                    depot.Grain -= GarrisonGrainCost;
                    record.GarrisonToday += GarrisonBatch;
                    Log.Write("M4 budgeted garrison recruitment " + castle.StringId +
                        ": +" + GarrisonBatch + " local recruits, gold=-" +
                        GarrisonGoldCost + " depotGrain=-" + GarrisonGrainCost);
                }
            }
        }
    }
}
