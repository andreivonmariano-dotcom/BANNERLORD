using System;
using TaleWorlds.MountAndBlade;

namespace CalradiaUnified
{
    /// <summary>
    /// Replaces the built-in Sandbox main-menu action after vanilla modules
    /// have registered their own option. No Harmony patch or second menu entry.
    /// The untouched action remains available as the explicit opt-out route.
    /// </summary>
    internal static class SandboxMainMenuEntry
    {
        private const string SandboxOptionId = "SandBoxNewGame";
        private static readonly Action CustomClick = OpenCreator;
        private static Action nativeClick;
        private static bool missingLogged;

        public static void Install()
        {
            var module = Module.CurrentModule;
            if (module == null) return;

            var option = module.GetInitialStateOptionWithId(SandboxOptionId);
            if (option == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    DiagnosticJournal.Event("sandbox_main_menu_missing",
                        "id=" + SandboxOptionId);
                }
                return;
            }

            // OnBeforeInitialModuleScreenSetAsRoot may run on every menu visit.
            // Never capture our own click handler as the native fallback.
            if (option.DoAction == CustomClick) return;
            nativeClick = option.DoAction;
            module.OverrideInitialStateOption(SandboxOptionId,
                new InitialStateOption(option.Id, option.Name,
                    option.OrderIndex, CustomClick,
                    option.IsDisabledAndReason, option.EnabledHint,
                    option.IsHidden));
            DiagnosticJournal.Event("sandbox_main_menu_overridden",
                "id=" + SandboxOptionId + "|replaces_native_start_selection=true");
        }

        private static void OpenCreator()
        {
            SandboxCreatorOverlay.OpenFromMainMenu(nativeClick);
        }
    }
}
