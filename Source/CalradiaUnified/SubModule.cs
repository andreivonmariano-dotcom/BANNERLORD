using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Bannerlord.UIExtenderEx;

namespace CalradiaUnified
{
    /// <summary>
    /// Native single-player module. The four BUTR framework modules are
    /// launcher-required; UIExtenderEx patches only game UI ViewModels.
    /// No third-party runtime binaries are distributed with this mod.
    /// </summary>
    public sealed class SubModule : MBSubModuleBase
    {
        private static readonly UIExtender Extender =
            UIExtender.Create("CalradiaUnified");
        private static bool nativeExtensionsRegistered;
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

        private float reportHeartbeatSeconds;
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            // Low-frequency read-only health trail. Avoid writing a
            // disk event every frame, and never alter simulation state.
            if(DiagnosticJournal.Enabled && dt>0f && dt<10f)
            {
                reportHeartbeatSeconds+=dt;
                if(reportHeartbeatSeconds>=45f)
                {
                    reportHeartbeatSeconds=0f;
                    try
                    {
                        var activeCampaign=Campaign.Current;
                        // Hero.MainHero is not safe before a campaign has
                        // been initialized. Do not probe it in the menu.
                        var player=activeCampaign==null ? null : Hero.MainHero;
                        DiagnosticSessionReporter.Heartbeat(
                            "campaign_active="+(activeCampaign!=null)+
                            "|player_exists="+(player!=null)+
                            "|player_level="+(player==null?"unavailable":
                                player.Level.ToString())+
                            "|player_age="+(player==null?"unavailable":
                                player.Age.ToString("0.0",
                                    System.Globalization.CultureInfo.InvariantCulture))+
                            "|screen_tick=alive");
                    }
                    catch(Exception ex)
                    {
                        DiagnosticJournal.Exception(
                            "session_heartbeat_failed",ex);
                    }
                }
            }
            SandboxCreatorOverlay.Tick();
            CUNativeStateShell.Tick();
        }

        public override void OnGameEnd(Game game)
        {
            DiagnosticJournal.Event("campaign_closed",
                "game_end=true|process_exit=false");
            CUNativeStateShell.Clear();
            SandboxCreatorOverlay.Clear();
            base.OnGameEnd(game);
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            // Failure is fatal: never run as a stripped-down optional UI.
            Extender.Register(typeof(SubModule).Assembly);
            Extender.Enable();
            nativeExtensionsRegistered=true;
            try
            {
                var settings=Settings.Load();
                DiagnosticJournal.Enabled=settings.EnableDiagnostics;
                if(DiagnosticJournal.Enabled)
                {
                    DiagnosticJournal.InstallManagedExceptionHooks();
                    if(settings.AutoSessionReports)
                        DiagnosticSessionReporter.Start();
                    DiagnosticJournal.Event("module_loaded",
                        "version="+typeof(SubModule).Assembly.GetName().Version+
                        "|auto_session_reports="+
                        settings.AutoSessionReports);
                }
            }
            catch (Exception ex) { Log.Write("Diagnostic initialization failed: " + ex); }
            Log.Write("Calradia Unified requires the four BUTR framework modules; "+
                "UIExtenderEx patches enabled="+nativeExtensionsRegistered);
        }

        protected override void OnSubModuleUnloaded()
        {
            // The native module unload is a second normal-exit path.
            // ProcessExit handles installations where it is skipped.
            DiagnosticSessionReporter.Complete("submodule_unloaded");
            base.OnSubModuleUnloaded();
        }

        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            DiagnosticJournal.Event("game_start",
                "game_type="+(game==null?"none":
                    game.GameType.GetType().Name)+
                "|module_auto_diagnostics=true");
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
                    // Settings are now in required BUTR MCM Mod Options,
                    // not registered as a tab in every town/castle.
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
