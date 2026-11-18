using System;
using System.IO;
using TaleWorlds.MountAndBlade;

namespace CULoaderProbe
{
    /// <summary>
    /// Diagnostic only: never modifies game state or registers campaign behaviors.
    /// </summary>
    public sealed class LoaderProbeSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CalradiaUnified");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "loader-probe.log"),
                    DateTime.UtcNow.ToString("O") +
                    " | CULoaderProbe.OnSubModuleLoad reached | 1.5.7 target" +
                    Environment.NewLine);
            }
            catch (Exception)
            {
                // Probe must not affect game startup on read-only installations.
            }
        }
    }
}
