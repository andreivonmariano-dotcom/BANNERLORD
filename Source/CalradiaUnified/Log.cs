using System;
using System.IO;
using System.Reflection;

namespace CalradiaUnified
{
    internal static class Log
    {
        internal static string ModuleRoot
        {
            get
            {
                var dll = Assembly.GetExecutingAssembly().Location;
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dll), "..", ".."));
            }
        }

        internal static void Write(string message)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "CalradiaUnified");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "calradia-unified.log"),
                    DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
            }
            catch { /* A logging failure must not terminate a campaign. */ }
        }
    }
}
