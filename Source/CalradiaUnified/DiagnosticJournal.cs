using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only, bounded local diagnostics. No network, game save or Harmony hooks.
    /// Reported clan gold delta is NOT native daily income or tax breakdown.
    /// </summary>
    public static class DiagnosticJournal
    {
        public const string FinanceHeader =
            "utc,session_id,record_type,session_day,settlement_id,prosperity,healthy_hearths," +
            "raided_villages,garrison_troops,garrison_wages,clan_lord_party_wages," +
            "player_party_troops,player_gold,change_in_player_gold,estimated_income," +
            "recommended_garrison_limit,estimated_net,depot_grain,physical_shipments," +
            "abstract_shipments,notes";
        public const string ExceptionHeader =
            "utc,session_id,context,exception_type,message,stack_trace";
        public static string OverrideRootForTests;
        public static bool Enabled = true;
        private static readonly object Gate = new object();
        private static readonly string SessionId = DateTime.UtcNow.ToString(
            "yyyyMMddTHHmmss", CultureInfo.InvariantCulture) + "_" +
            Guid.NewGuid().ToString("N").Substring(0, 8);
        private static bool hooksInstalled;
        private const long MaxCsvBytes = 8 * 1024 * 1024;

        public static string Root
        {
            get
            {
                if (!String.IsNullOrEmpty(OverrideRootForTests))
                    return OverrideRootForTests;
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "CalradiaUnified", "Diagnostics");
            }
        }

        public static string CsvField(string value)
        {
            value = value ?? "";
            // Doubled quotes plus enclosing quotes keep all records parseable
            // including multiline exceptions and native IDs containing commas.
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string StripPersonalPath(string text)
        {
            text = text ?? "";
            string home = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            return String.IsNullOrEmpty(home) ? text :
                text.Replace(home, "<USERPROFILE>");
        }

        private static string[] Metadata(string[] values)
        {
            var withMetadata = new string[values.Length + 2];
            withMetadata[0] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            withMetadata[1] = SessionId;
            Array.Copy(values, 0, withMetadata, 2, values.Length);
            return withMetadata;
        }

        private static void Append(string name, string header, string[] cells)
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Root);
                    string path = Path.Combine(Root, name);
                    // Rotate rather than grow forever. Keep latest and one predecessor.
                    if (File.Exists(path) && new FileInfo(path).Length > MaxCsvBytes)
                    {
                        string previous = path + ".previous";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(path, previous);
                    }
                    bool newFile = !File.Exists(path);
                    using (var stream = new FileStream(path, FileMode.Append,
                        FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        if (newFile) writer.WriteLine(header);
                        writer.WriteLine(String.Join(",", cells.Select(CsvField)));
                    }
                }
            }
            catch
            {
                // A log I/O problem cannot crash Bannerlord.
            }
        }

        public static void Finance(params string[] fields)
        {
            if (fields == null || fields.Length != 19) return;
            Append("finance.csv", FinanceHeader, Metadata(fields));
        }

        public static void Event(string context, string detail)
        {
            Append("events.csv", "utc,session_id,context,detail",
                Metadata(new[] { context ?? "", detail ?? "" }));
        }

        public static void Exception(string context, Exception error)
        {
            if (error == null) return;
            Append("exceptions.csv", ExceptionHeader, Metadata(new[]
            {
                context ?? "unknown", error.GetType().FullName ?? "Exception",
                StripPersonalPath(error.Message),
                StripPersonalPath(error.ToString())
            }));
            Event("exception_detected", context ?? "unknown");
        }

        public static void InstallManagedExceptionHooks()
        {
            if (hooksInstalled || !Enabled) return;
            hooksInstalled = true;
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Exception("unhandled_managed_exception", e.ExceptionObject as Exception ??
                        new System.Exception("Non-Exception unhandled object"));
                };
                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    Exception("unobserved_task_exception", e.Exception);
                    // Do not suppress or alter native exception policy.
                };
            }
            catch { /* Hooks must never prevent module startup. */ }
        }

        public static string ExportSnapshot()
        {
            string output = Path.Combine(Root, "Exports",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture)
                + "_" + SessionId);
            lock (Gate)
            {
                Directory.CreateDirectory(output);
                foreach (string name in new[] { "finance.csv", "events.csv",
                    "exceptions.csv", "finance.csv.previous", "events.csv.previous",
                    "exceptions.csv.previous" })
                {
                    string source = Path.Combine(Root, name);
                    if (File.Exists(source))
                    {
                        try { File.Copy(source, Path.Combine(output, name), true); }
                        catch (IOException) { /* Continue with files that are available. */ }
                    }
                }
                string legacy = Path.Combine(Directory.GetParent(Root).FullName,
                    "calradia-unified.log");
                if (File.Exists(legacy))
                {
                    try { File.Copy(legacy, Path.Combine(output, "mod-events.log"), true); }
                    catch (IOException) { }
                }
                File.WriteAllText(Path.Combine(output, "README.txt"),
                    "Calradia Unified diagnostic snapshot, UTC " +
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) +
                    Environment.NewLine +
                    "Finance CSV lists observed wages, gold, troop counts, and model forecasts." +
                    Environment.NewLine +
                    "Gold differences are not the native daily tax ledger." +
                    Environment.NewLine +
                    "Exceptions are managed failures only. Hard native crashes must be" +
                    " collected from ProgramData Bannerlord crashes/logs or Windows Event Log." +
                    Environment.NewLine +
                    "Inspect the archive for private paths and names before sharing." +
                    Environment.NewLine);
            }
            return output;
        }
    }
}
