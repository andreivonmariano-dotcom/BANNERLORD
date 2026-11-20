using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CalradiaUnified
{
    /// <summary>
    /// Creates a local, private diagnostic ZIP on normal game exit.
    /// Events are written DURING play, so a native hard exit can be
    /// recovered as an interrupted session on the next launch.
    /// The reporter never uploads files, touches saves or requires Harmony.
    /// </summary>
    public static class DiagnosticSessionReporter
    {
        private static readonly object Gate=new object();
        private static bool started;
        private static bool closed;
        private static bool hooksInstalled;
        private const int MaxReports=20;
        private const int MaxNativeLogBytes=512*1024;
        // User's known Steam installation is a fallback only.
        // Normal discovery follows the loaded DLL and running executable.
        public static string PreferredGameRoot =
            @"D:\Steam\steamapps\common\Mount & Blade II Bannerlord";
        public static string OverrideGameRootForTests;
        // Test-only override. In Windows gameplay use the shell's actual
        // Desktop folder, including OneDrive-redirection when configured.
        public static string OverrideDesktopDirectoryForTests;

        public static string SessionDirectory
        {
            get{return Path.Combine(DiagnosticJournal.Root,
                "Sessions",DiagnosticJournal.CurrentSessionId);}
        }
        public static string ReportsDirectory
        {
            get
            {
                string fallback=Path.Combine(DiagnosticJournal.Root,"Reports");
                string desktop=OverrideDesktopDirectoryForTests ??
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory);
                if(String.IsNullOrWhiteSpace(desktop) ||
                    !Directory.Exists(desktop))
                    return fallback;
                string target=Path.Combine(desktop,"Calradia Unified Reports");
                try
                {
                    Directory.CreateDirectory(target);
                    // An existing Desktop can still be read-only or offline.
                    // Probe before selecting it, so reports stay recoverable.
                    string probe=Path.Combine(target,
                        ".cu-probe-"+Guid.NewGuid().ToString("N")+".tmp");
                    try
                    {
                        using(new FileStream(probe,FileMode.CreateNew,
                            FileAccess.Write,FileShare.None)) { }
                    }
                    finally
                    {
                        if(File.Exists(probe))File.Delete(probe);
                    }
                    return target;
                }
                catch
                {
                    return fallback;
                }
            }
        }
        private static string Utc()
        {
            return DateTime.UtcNow.ToString("o",
                CultureInfo.InvariantCulture);
        }
        private static string FindParentGameRoot(string path)
        {
            try
            {
                if(String.IsNullOrWhiteSpace(path))return null;
                DirectoryInfo dir=new DirectoryInfo(
                    File.Exists(path)?Path.GetDirectoryName(path):path);
                for(int i=0;i<10 && dir!=null;i++,dir=dir.Parent)
                {
                    if(Directory.Exists(Path.Combine(dir.FullName,"Modules")) &&
                        Directory.Exists(Path.Combine(dir.FullName,"bin")))
                        return dir.FullName;
                }
            }
            catch { }
            return null;
        }
        public static string ResolveGameRoot()
        {
            if(!String.IsNullOrWhiteSpace(OverrideGameRootForTests))
                return OverrideGameRootForTests;
            string fromModule=FindParentGameRoot(
                typeof(DiagnosticSessionReporter).Assembly.Location);
            if(fromModule!=null)return fromModule;
            try
            {
                string fromExe=FindParentGameRoot(
                    Process.GetCurrentProcess().MainModule.FileName);
                if(fromExe!=null)return fromExe;
            }
            catch { }
            if(Directory.Exists(Path.Combine(
                PreferredGameRoot,"Modules")))return PreferredGameRoot;
            return null;
        }
        private static string Hash(string file)
        {
            try
            {
                if(String.IsNullOrEmpty(file)||!File.Exists(file))
                    return "unavailable";
                using(var stream=new FileStream(file,FileMode.Open,
                    FileAccess.Read,FileShare.ReadWrite))
                using(var sha=SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream))
                        .Replace("-","").ToLowerInvariant();
            }
            catch(Exception error)
            {
                return "hash_error:"+error.GetType().Name;
            }
        }
        private static void WriteRuntimeInfo(string directory,string status)
        {
            var b=new StringBuilder();
            b.AppendLine("Calradia Unified session report");
            b.AppendLine("utc="+Utc());
            b.AppendLine("session_id="+DiagnosticJournal.CurrentSessionId);
            b.AppendLine("status="+status);
            b.AppendLine("game_root="+DiagnosticJournal.StripPersonalPath(
                ResolveGameRoot()??"not_detected"));
            var assembly=typeof(DiagnosticSessionReporter).Assembly;
            string file=assembly.Location;
            b.AppendLine("module_version="+assembly.GetName().Version);
            b.AppendLine("module_dll_sha256="+Hash(file));
            b.AppendLine("process_id="+Process.GetCurrentProcess().Id);
            b.AppendLine("os="+Environment.OSVersion.VersionString);
            b.AppendLine("clr="+Environment.Version);
            b.AppendLine("loaded_game_assemblies=");
            foreach(var loaded in AppDomain.CurrentDomain.GetAssemblies()
                .Select(x=>x.GetName())
                .Where(x=>x.Name.StartsWith("TaleWorlds",
                    StringComparison.OrdinalIgnoreCase) ||
                    x.Name.StartsWith("SandBox",
                    StringComparison.OrdinalIgnoreCase) ||
                    x.Name.StartsWith("Naval",
                    StringComparison.OrdinalIgnoreCase) ||
                    x.Name.StartsWith("CalradiaUnified",
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(x=>x.Name,StringComparer.Ordinal)
                .Take(120))
                b.AppendLine("  "+loaded.Name+"="+loaded.Version);
            b.AppendLine("No saves, memory dumps or screenshots included.");
            b.AppendLine("No data is uploaded automatically.");
            File.WriteAllText(Path.Combine(directory,"runtime-info.txt"),
                b.ToString(),new UTF8Encoding(false));
        }
        private static void NativeLogTail(string directory)
        {
            // Only a few bounded text log tails. Never enumerate or
            // package game savegames, native crash dumps or other files.
            if(DiagnosticJournal.OverrideRootForTests!=null)return;
            try
            {
                string programData=Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData);
                var candidates=new[]{
                    Path.Combine(programData,
                        "Mount and Blade II Bannerlord","logs"),
                    Path.Combine(programData,
                        "Mount and Blade II Bannerlord")
                };
                var files=new List<FileInfo>();
                foreach(string path in candidates)
                {
                    if(!Directory.Exists(path))continue;
                    foreach(string file in Directory.GetFiles(
                        path,"rgl_log*",SearchOption.TopDirectoryOnly))
                    {
                        var info=new FileInfo(file);
                        if(info.Exists && info.LastWriteTimeUtc >
                            DateTime.UtcNow.AddDays(-2) &&
                            !files.Any(x=>String.Equals(x.FullName,
                                info.FullName,StringComparison.OrdinalIgnoreCase)))
                            files.Add(info);
                    }
                }
                int index=0;
                foreach(var info in files.OrderByDescending(
                    x=>x.LastWriteTimeUtc).Take(4))
                {
                    byte[] tail;
                    using(var stream=new FileStream(info.FullName,
                        FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                    {
                        int count=(int)Math.Min(MaxNativeLogBytes,stream.Length);
                        stream.Seek(-count,SeekOrigin.End);
                        tail=new byte[count];
                        int read=0;
                        while(read<count)
                        {
                            int size=stream.Read(tail,read,count-read);
                            if(size<=0)break;
                            read+=size;
                        }
                        if(read<count)Array.Resize(ref tail,read);
                    }
                    // Retain the original text encoding (some RGL logs
                    // are UTF-16). Do not include the absolute path.
                    File.WriteAllBytes(Path.Combine(directory,
                        "native-rgl-tail-"+(++index)+".log"),tail);
                }
            }
            catch(Exception error)
            {
                File.WriteAllText(Path.Combine(directory,
                    "native-log-status.txt"),
                    "RGL tail not readable: "+error.GetType().Name);
            }
        }
        private static bool IsOtherProcessStillRunning(string marker)
        {
            try
            {
                int pid;
                if(!Int32.TryParse(File.ReadAllText(marker).Trim(),
                    NumberStyles.Integer,CultureInfo.InvariantCulture,
                    out pid) || pid<=0)return false;
                if(pid==Process.GetCurrentProcess().Id)return false;
                using(var process=Process.GetProcessById(pid))
                    return !process.HasExited &&
                        process.ProcessName.IndexOf("bannerlord",
                            StringComparison.OrdinalIgnoreCase)>=0;
            }
            catch { return false; }
        }
        private static string ReportPath(string session,bool interrupted)
        {
            string id=Path.GetFileName(session);
            return Path.Combine(ReportsDirectory,"CU-Session-"+id+
                (interrupted?"-INTERRUPTED":"-CLOSED")+".zip");
        }
        private static string Package(string session,bool interrupted,
            string status)
        {
            Directory.CreateDirectory(ReportsDirectory);
            if(interrupted)
            {
                // Keep the original game's DLL/version fingerprint.
                // Recovery occurs in a different process and must not
                // replace the metadata captured before the hard exit.
                File.WriteAllText(Path.Combine(session,"recovery-info.txt"),
                    "status="+status+"\r\n"+
                    "recovered_at_utc="+Utc()+"\r\n"+
                    "recovery_game_root="+DiagnosticJournal
                        .StripPersonalPath(ResolveGameRoot()??"not_detected"));
            }
            else WriteRuntimeInfo(session,status);
            NativeLogTail(session);
            File.WriteAllText(Path.Combine(session,"README.txt"),
                "CU automatic session diagnostics\r\n"+
                "Status: "+status+"\r\n"+
                "Mod events, exceptions and finance records were saved"+
                " as they happened. A native hard crash does not run"+
                " managed cleanup; INTERRUPTED means recovery on next"+
                " launch, not a confirmed crash cause.\r\n"+
                "Review logs for player names and paths before sharing.\r\n"+
                "Raw memory dumps, game saves, screenshots and private"+
                " application data are never included.\r\n");
            string result=ReportPath(session,interrupted);
            string partial=result+".partial";
            if(File.Exists(partial))File.Delete(partial);
            // Package an explicit set of diagnostic records only.
            // Unknown files such as saves, dumps and images stay local.
            using(var archive=ZipFile.Open(partial,ZipArchiveMode.Create))
            {
                foreach(string name in new[]{
                    "events.csv","events.csv.previous",
                    "exceptions.csv","exceptions.csv.previous",
                    "finance.csv","finance.csv.previous",
                    "runtime-info.txt","recovery-info.txt",
                    "README.txt","native-log-status.txt"})
                {
                    string source=Path.Combine(session,name);
                    if(File.Exists(source))
                        archive.CreateEntryFromFile(source,name,
                            CompressionLevel.Fastest);
                }
                for(int i=1;i<=4;i++)
                {
                    string name="native-rgl-tail-"+i+".log";
                    string source=Path.Combine(session,name);
                    if(File.Exists(source))
                        archive.CreateEntryFromFile(source,name,
                            CompressionLevel.Fastest);
                }
            }
            if(File.Exists(result))File.Delete(result);
            File.Move(partial,result);
            File.WriteAllText(Path.Combine(DiagnosticJournal.Root,
                "latest-report.txt"),result);
            return result;
        }
        private static void RecoverInterruptedSessions()
        {
            string dir=Path.Combine(DiagnosticJournal.Root,"Sessions");
            if(!Directory.Exists(dir))return;
            // Select only recoverable sessions before the 24-report cap.
            // Newer completed folders must not starve older crashes.
            foreach(string session in Directory.GetDirectories(dir)
                .OrderByDescending(x=>x,StringComparer.Ordinal)
                .Where(x=>File.Exists(Path.Combine(x,"active.marker")))
                .Where(x=>!IsOtherProcessStillRunning(
                    Path.Combine(x,"active.marker")))
                .Take(24))
            {
                if(String.Equals(session,SessionDirectory,
                    StringComparison.OrdinalIgnoreCase))continue;
                string marker=Path.Combine(session,"active.marker");
                if(!File.Exists(marker) ||
                    IsOtherProcessStillRunning(marker))continue;
                try
                {
                    Package(session,true,"interrupted_previous_process");
                    File.Delete(marker);
                    File.WriteAllText(Path.Combine(session,
                        "recovered.marker"),Utc());
                }
                catch(Exception error)
                {
                    // Never block the next game's startup. Keep the
                    // marker so that recovery can be retried.
                    try
                    {
                        File.WriteAllText(Path.Combine(
                            DiagnosticJournal.Root,"report-error.txt"),
                            "Recover: "+error.GetType().Name);
                    }
                    catch { }
                }
            }
        }
        private static void TrimReports()
        {
            try
            {
                foreach(string path in Directory.GetFiles(
                    ReportsDirectory,"CU-Session-*.zip")
                    .OrderByDescending(x=>x,StringComparer.Ordinal)
                    .Skip(MaxReports))
                    File.Delete(path);
            }
            catch { }
        }
        private static void TrimArchivedSessions()
        {
            try
            {
                string directory=Path.Combine(DiagnosticJournal.Root,
                    "Sessions");
                if(!Directory.Exists(directory))return;
                // Retain all interrupted sessions not yet recovered.
                // Only old, fully archived session folders are pruned.
                foreach(string session in Directory.GetDirectories(directory)
                    .OrderByDescending(x=>x,StringComparer.Ordinal)
                    .Skip(MaxReports))
                {
                    if(File.Exists(Path.Combine(session,"active.marker")))
                        continue;
                    string id=Path.GetFileName(session);
                    string closed=Path.Combine(ReportsDirectory,
                        "CU-Session-"+id+"-CLOSED.zip");
                    string interrupted=Path.Combine(ReportsDirectory,
                        "CU-Session-"+id+"-INTERRUPTED.zip");
                    if(File.Exists(closed)||File.Exists(interrupted) ||
                        File.Exists(Path.Combine(session,"completed.marker")) ||
                        File.Exists(Path.Combine(session,"recovered.marker")))
                        Directory.Delete(session,true);
                }
            }
            catch { }
        }
        public static void Start()
        {
            if(!DiagnosticJournal.Enabled)return;
            lock(Gate)
            {
                if(started)return;
                try
                {
                    Directory.CreateDirectory(SessionDirectory);
                    RecoverInterruptedSessions();
                    TrimArchivedSessions();
                    File.WriteAllText(Path.Combine(
                        SessionDirectory,"active.marker"),
                        Process.GetCurrentProcess().Id.ToString(
                            CultureInfo.InvariantCulture));
                    WriteRuntimeInfo(SessionDirectory,"running");
                    started=true;
                    closed=false;
                    if(!hooksInstalled)
                    {
                        hooksInstalled=true;
                        AppDomain.CurrentDomain.ProcessExit+=(s,e)=>
                            Complete("process_exit");
                        AppDomain.CurrentDomain.DomainUnload+=(s,e)=>
                            Complete("domain_unload");
                    }
                    DiagnosticJournal.Event("session_capture_started",
                        "automatic_zip_on_exit=true"+
                        "|native_crash_recovery_next_launch=true"+
                        "|game_root_detected="+(ResolveGameRoot()!=null));
                }
                catch(Exception error)
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(
                            DiagnosticJournal.Root,"report-error.txt"),
                            "Start: "+error.GetType().Name);
                    }
                    catch { }
                }
            }
        }
        public static void Heartbeat(string detail)
        {
            if(!started || closed || !DiagnosticJournal.Enabled)return;
            DiagnosticJournal.Event("session_heartbeat",detail??"");
        }
        public static void Complete(string reason)
        {
            lock(Gate)
            {
                if(!started || closed)return;
                try
                {
                    DiagnosticJournal.Event("session_exit",
                        "reason="+(reason??"unknown")+
                        "|status=normal_shutdown");
                    Package(SessionDirectory,false,
                        "normal_shutdown:"+reason);
                    // Keep this marker until the ZIP has been written.
                    // A hard termination in Package is recovered later.
                    File.Delete(Path.Combine(
                        SessionDirectory,"active.marker"));
                    File.WriteAllText(Path.Combine(SessionDirectory,
                        "completed.marker"),Utc());
                    closed=true;
                    TrimArchivedSessions();
                    TrimReports();
                }
                catch(Exception error)
                {
                    // A diagnostic feature must never prevent exit.
                    try
                    {
                        File.WriteAllText(Path.Combine(
                            DiagnosticJournal.Root,"report-error.txt"),
                            "Complete: "+error.GetType().Name);
                    }
                    catch { }
                }
            }
        }
    }
}
