using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CalradiaUnified
{
    /// <summary>
    /// Allow-list and persistence for the native in-game settings menu.
    /// Only approved options can be written by the UI. All other bundled
    /// and user-supplied options are parsed by the existing Settings loader.
    /// </summary>
    public static class SettingsMenuPolicy
    {
        public static bool TryNormalize(string key, string raw, out string normalized)
        {
            return SettingsCatalog.TryNormalize(key, raw, out normalized);
        }

        public static string UpdateLines(string existing, string key, string value)
        {
            string normalized;
            if (!TryNormalize(key, value, out normalized))
                throw new ArgumentException("Unsupported or invalid setting.");
            var lines = new List<string>();
            bool replaced = false;
            if (!String.IsNullOrEmpty(existing))
            {
                foreach (var raw in existing.Split(new[] { "\r\n", "\n" },
                    StringSplitOptions.None))
                {
                    if (raw.Length == 0) continue;
                    string trimmed = raw.TrimStart();
                    if (!trimmed.StartsWith("#", StringComparison.Ordinal))
                    {
                        int equals = trimmed.IndexOf('=');
                        if (equals > 0 &&
                            String.Equals(trimmed.Substring(0, equals).Trim(),
                                key, StringComparison.Ordinal))
                        {
                            if (!replaced)
                            {
                                lines.Add(key + "=" + normalized);
                                replaced = true;
                            }
                            // Drop duplicate definitions to avoid last-value override.
                            continue;
                        }
                    }
                    lines.Add(raw);
                }
            }
            if (!replaced) lines.Add(key + "=" + normalized);
            return String.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        /// <summary>
        /// Persist user overrides in LocalAppData, never in the installed module.
        /// A failed write leaves the previously committed configuration intact.
        /// </summary>
        public static void WriteOverride(string file, string key, string value)
        {
            if (String.IsNullOrEmpty(file))
                throw new ArgumentException("Missing configuration path.");
            string normalized;
            if (!TryNormalize(key, value, out normalized))
                throw new ArgumentException("Unsupported or invalid setting.");
            string folder = Path.GetDirectoryName(file);
            if (String.IsNullOrEmpty(folder))
                throw new ArgumentException("Missing configuration directory.");
            Directory.CreateDirectory(folder);
            string previous = File.Exists(file) ? File.ReadAllText(file) : "";
            if (previous.Length > 128 * 1024)
                throw new InvalidDataException("Override file exceeds safe size limit.");
            string updated = UpdateLines(previous, key, normalized);
            string temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, updated);
                if (File.Exists(file))
                    File.Replace(temp, file, null);
                else
                    File.Move(temp, file);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
