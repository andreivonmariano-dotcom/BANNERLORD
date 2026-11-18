using System;
using CalradiaUnified;

namespace CalradiaUnified.CampaignSimulator
{
    /// <summary>
    /// Model-only early-vassal finance tests. These estimates are NOT the
    /// game's native income or wage formulas.
    /// </summary>
    internal static class FeudalStress
    {
        internal sealed class Results
        {
            public int Seed { get; set; }
            public long EvaluatedDays { get; set; }
            public long Checks { get; set; }
            public long HealthyPositiveDays { get; set; }
            public long CrisisDeficitDays { get; set; }
            public long OversizedPartyDeficitDays { get; set; }
            public long GrainProduced { get; set; }
            public long GrainSold { get; set; }
            public long AbstractTradeGold { get; set; }
            public int HealthyExampleIncome { get; set; }
            public int HealthyExampleWageCap { get; set; }
            public int HealthyExampleProjectedNet { get; set; }
        }
        public static Results Run(int seed, int daysPerRun, int runs)
        {
            var rng = new Random(seed + 70000);
            var r = new Results { Seed = seed, EvaluatedDays = (long)daysPerRun * runs };
            void Check(bool condition, string label, int run, int day)
            {
                r.Checks++;
                if (!condition) throw new InvalidOperationException(
                    "FEUDAL SIM FAIL seed=" + seed + " run=" + run +
                    " day=" + day + " case=" + label);
            }
            r.HealthyExampleIncome = FeudalBudgetPolicy.EstimateEstateIncome(1500, 900);
            r.HealthyExampleWageCap = FeudalBudgetPolicy.WageBudget(
                r.HealthyExampleIncome, 500, 150, 250, 1800);
            r.HealthyExampleProjectedNet = FeudalBudgetPolicy.ProjectedBalance(
                r.HealthyExampleIncome, 500, r.HealthyExampleWageCap, 150, 0);
            Check(r.HealthyExampleIncome == 1450 && r.HealthyExampleWageCap == 550 &&
                r.HealthyExampleProjectedNet == 250, "reference castle + 2 villages", 0, 0);

            for (int run = 0; run < runs; run++)
            {
                for (int day = 1; day <= daysPerRun; day++)
                {
                    int prosperity = 1500 + rng.Next(-200, 201);
                    int totalHearths = 900 + rng.Next(-100, 101);
                    int fieldWage = 500 + rng.Next(-75, 76);
                    int gross = FeudalBudgetPolicy.EstimateEstateIncome(
                        prosperity, totalHearths);
                    int cap = FeudalBudgetPolicy.WageBudget(gross, fieldWage,
                        150, 250, 1800);
                    int projected = FeudalBudgetPolicy.ProjectedBalance(
                        gross, fieldWage, cap, 150, 0);
                    Check(cap >= 250 && cap <= 1800, "healthy estate wage cap", run, day);
                    Check(projected >= 250, "healthy projected positive budget", run, day);
                    if (projected >= 0) r.HealthyPositiveDays++;
                    int oversizedProjected = FeudalBudgetPolicy.ProjectedBalance(
                        gross, fieldWage, 8500, 150, 0);
                    Check(oversizedProjected < -5000,
                        "overstaffed garrison creates large deficit", run, day);
                    int unaffordableField = FeudalBudgetPolicy.WageBudget(
                        gross, 8000, 150, 250, 1800);
                    Check(unaffordableField == 0,
                        "no arbitrary positive balance for oversized field army",
                        run, day);
                    if (FeudalBudgetPolicy.ProjectedBalance(
                        gross, 8000, 0, 150, 0) < 0) r.OversizedPartyDeficitDays++;

                    // Both villages are raided: no extra village crop is produced.
                    int raidedIncome = FeudalBudgetPolicy.EstimateEstateIncome(
                        prosperity, 0);
                    int crisisBalance = FeudalBudgetPolicy.ProjectedBalance(
                        raidedIncome, fieldWage, 250, 150, 0);
                    Check(FeudalBudgetPolicy.VillageHarvest(0, 100, 150, 10) == 0,
                        "raided villages cannot harvest", run, day);
                    if (crisisBalance < 0) r.CrisisDeficitDays++;

                    // Market activity has an explicit inventory source and sink.
                    int beforeGrain = 60 + rng.Next(0, 35);
                    int freeCapacity = 100 - beforeGrain;
                    int harvested = FeudalBudgetPolicy.VillageHarvest(
                        totalHearths, freeCapacity, 150, 10);
                    int afterHarvest = beforeGrain + harvested;
                    int sold = FeudalBudgetPolicy.SurplusSale(afterHarvest, 30, 8);
                    int afterSale = afterHarvest - sold;
                    long value = (long)sold * 18;
                    Check(afterSale >= 30 && afterSale <= 100,
                        "surplus sale leaves stock reserve", run, day);
                    Check(beforeGrain + harvested == afterSale + sold,
                        "village and wholesale grain conservation", run, day);
                    Check(value >= 0 && value <= 144,
                        "wholesale gold only from consumed grain", run, day);
                    r.GrainProduced += harvested;
                    r.GrainSold += sold;
                    r.AbstractTradeGold += value;
                }
            }
            Check(r.HealthyPositiveDays == r.EvaluatedDays,
                "healthy one-castle two-village scenario stays positive", runs, daysPerRun);
            Check(r.OversizedPartyDeficitDays == r.EvaluatedDays,
                "oversized field parties remain unaffordable", runs, daysPerRun);
            return r;
        }
    }
}
