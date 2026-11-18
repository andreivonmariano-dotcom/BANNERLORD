using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaUnified
{
    /// <summary>
    /// Original single-player Bannerlord module. Intentionally no Harmony patches,
    /// replacement actions, copied third-party code, or third-party DLL references.
    /// </summary>
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Log.Write("Calradia Unified 0.11.0 initialized; no Harmony patches applied.");
        }

        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            try
            {
                // A distinct behavior instance is registered per campaign start.
                var campaignStarter = starter as CampaignGameStarter;
                if (game.GameType is Campaign && campaignStarter != null)
                {
                    Settings config = Settings.Load();
                    campaignStarter.AddBehavior(new SupplyBehavior(config));
                    campaignStarter.AddBehavior(new CivicBehavior(config));
                    campaignStarter.AddBehavior(new KingdomAutonomyBehavior(config));
                    Log.Write("Campaign supply, civic depots, recruitment, economy and diplomacy behaviors registered; player-only foraging="
                        + config.EnablePlayerForaging);
                }
            }
            catch (Exception ex) { Log.Write("Campaign registration error: " + ex); }
        }
    }
}
