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
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                SandboxMainMenuEntry.Install();
            }
            catch (Exception error)
            {
                DiagnosticJournal.Exception("sandbox_main_menu_override_failed", error);
                Log.Write("Sandbox menu replacement failed: " + error);
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            SandboxCreatorOverlay.Tick();
            CUNativeStateShell.Tick();
        }

        public override void OnGameEnd(Game game)
        {
            CUNativeStateShell.Clear();
            SandboxCreatorOverlay.Clear();
            base.OnGameEnd(game);
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                DiagnosticJournal.Enabled = Settings.Load().EnableDiagnostics;
                if (DiagnosticJournal.Enabled)
                {
                    DiagnosticJournal.InstallManagedExceptionHooks();
                    DiagnosticJournal.Event("module_loaded", "version=0.36.0");
                }
            }
            catch (Exception ex) { Log.Write("Diagnostic initialization failed: " + ex); }
            Log.Write("Calradia Unified 0.36.0 initialized; diagnostics configured; no Harmony patches applied.");
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
                    campaignStarter.AddBehavior(new DiagnosticBehavior(config));
                    campaignStarter.AddBehavior(new NativeSettingsBehavior(config));
                    campaignStarter.AddBehavior(new SandboxOriginBehavior());
                    Log.Write("Campaign supply, civic depots, recruitment, economy and diplomacy behaviors registered; player-only foraging="
                        + config.EnablePlayerForaging);
                }
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("campaign_registration", ex);
                Log.Write("Campaign registration error: " + ex);
            }
        }
    }
}
