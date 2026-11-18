using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class CivicBehavior
    {
        private const int GarrisonBatch = 2;
        private const int GarrisonGoldCost = 250;
        private const int GarrisonGrainCost = 5;

        private void RegisterGarrisonMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_garrison_train",
                "CU: Train 2 garrison defenders (250 gold + 5 depot grain)",
                x => CanVisit() && CanTrainGarrison(), x => Safe(TrainGarrison), false);
        }
        private MobileParty CurrentGarrison()
        {
            var s = Settlement.CurrentSettlement;
            return s == null || s.Town == null ? null : s.Town.GarrisonParty;
        }
        private bool CanTrainGarrison()
        {
            if (!settings.EnableGarrisonTraining || !Owned() || Hero.MainHero == null)
                return false;
            var s = Settlement.CurrentSettlement;
            var garrison = CurrentGarrison();
            var depot = CurrentDepot();
            var record = CurrentLevyRecord();
            var basicTroop = LocalRecruit();
            return s != null && depot != null && record != null &&
                GarrisonTrainingPolicy.CanTrain(true, true, s.SiegeEvent != null,
                    garrison != null && garrison.MemberRoster != null, basicTroop != null,
                    PlayerGold(), GarrisonGoldCost, depot.Grain, GarrisonGrainCost,
                    garrison == null || garrison.MemberRoster == null ? 0 :
                        garrison.MemberRoster.TotalManCount,
                    settings.GarrisonTrainingMaxTroops, record.GarrisonToday,
                    settings.GarrisonTrainingDailyCap, GarrisonBatch);
        }
        private void TrainGarrison()
        {
            if (!CanTrainGarrison()) return;
            var garrison = CurrentGarrison();
            var troop = LocalRecruit();
            var depot = CurrentDepot();
            var record = CurrentLevyRecord();
            if (garrison == null || troop == null || depot == null ||
                record == null || Hero.MainHero == null) return;
            // Adds troops to the existing garrison, not a fake ledger.
            // A rejected roster mutation must not charge resources.
            garrison.MemberRoster.AddToCounts(troop, GarrisonBatch);
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(
                    null, Hero.MainHero, -GarrisonGoldCost, true);
            }
            catch
            {
                garrison.MemberRoster.AddToCounts(troop, -GarrisonBatch);
                throw;
            }
            depot.Grain -= GarrisonGrainCost;
            record.GarrisonToday += GarrisonBatch;
            Log.Write("M4 garrison trained: settlement=" + CurrentId() +
                " newTroops=" + GarrisonBatch + " gold=" + GarrisonGoldCost +
                " grain=" + GarrisonGrainCost);
            Notice("Trained two local defenders. Depot grain -5; player gold -250.");
        }
    }
}
