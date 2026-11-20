using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CalradiaUnified
{
    /// <summary>
    /// Reads the journal using the same escaped CSV format that the
    /// diagnostic writer produces. These are observations, never proof
    /// of a native exception or its cause.
    /// </summary>
    public static class DiagnosticSummaryPolicy
    {
        private const int MaxFieldChars=2048;
        private static void AddField(List<string> cells,StringBuilder field)
        {
            cells.Add(field.ToString());
            field.Clear();
        }
        private static void AddRecord(List<string[]> records,
            List<string> cells,StringBuilder field,int limit)
        {
            AddField(cells,field);
            // The first record of each CSV file is its header.
            if(cells.Count>0 && !String.Equals(cells[0],"utc",
                StringComparison.OrdinalIgnoreCase))
            {
                records.Add(cells.ToArray());
                if(records.Count>limit)records.RemoveAt(0);
            }
            cells.Clear();
        }
        private static void ReadLatest(string path,List<string[]> records,
            int limit)
        {
            if(!File.Exists(path))return;
            try
            {
                using(var reader=new StreamReader(new FileStream(path,
                    FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
                {
                    var cells=new List<string>();
                    var field=new StringBuilder();
                    bool quoted=false;
                    while(reader.Peek()>=0)
                    {
                        char c=(char)reader.Read();
                        if(quoted)
                        {
                            if(c=='"')
                            {
                                if(reader.Peek()=='"')
                                {
                                    reader.Read();
                                    if(field.Length<MaxFieldChars)
                                        field.Append('"');
                                }
                                else quoted=false;
                            }
                            else if(field.Length<MaxFieldChars)
                                field.Append(c);
                        }
                        else if(c=='"' && field.Length==0)quoted=true;
                        else if(c==',')AddField(cells,field);
                        else if(c=='\r' || c=='\n')
                        {
                            if(c=='\r' && reader.Peek()=='\n')
                                reader.Read();
                            AddRecord(records,cells,field,limit);
                        }
                        else if(field.Length<MaxFieldChars)field.Append(c);
                    }
                    // A hard process exit can leave a partly written row.
                    // Never treat an unterminated quoted row as evidence.
                    if(!quoted && (cells.Count>0 || field.Length>0))
                        AddRecord(records,cells,field,limit);
                }
            }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
        private static List<string[]> RecentRows(string directory,
            string file,int limit)
        {
            var rows=new List<string[]>();
            ReadLatest(Path.Combine(directory,file+".previous"),rows,limit);
            ReadLatest(Path.Combine(directory,file),rows,limit);
            return rows;
        }
        private static string Cell(string[] row,int index)
        {
            return row!=null && index<row.Length ? row[index] : "";
        }
        private static string Clean(string value,int limit)
        {
            value=DiagnosticJournal.StripPersonalPath(value??"")
                .Replace("\r"," ").Replace("\n"," ").Replace("\t"," ");
            if(value.Length>limit)
                return value.Substring(0,limit)+" [truncated]";
            return value;
        }
        // Areas follow known mod exception context prefixes.
        // They identify source code context, not the cause of a shutdown.
        private static string ErrorArea(string context)
        {
            context=context??"";
            foreach(string prefix in new[]{
                "cu_native_","cu_stats_","cu_guided_","cu_creator_",
                "cu_all_stats_","cu_spouse_stat_","sandbox_creator_",
                "sandbox_main_menu_","sandbox_overhaul_"})
                if(context.StartsWith(prefix,StringComparison.Ordinal))
                    return "Creator / UI";
            foreach(string prefix in new[]{
                "campaign_","cu_family_","sandbox_direct_game_start_"})
                if(context.StartsWith(prefix,StringComparison.Ordinal))
                    return "Campaign / family";
            return "Other / unknown";
        }
        private static string DetailValue(string details,string key)
        {
            foreach(string part in (details??"").Split('|'))
            {
                int equal=part.IndexOf('=');
                if(equal>0 && String.Equals(part.Substring(0,equal),
                    key,StringComparison.Ordinal))
                    return part.Substring(equal+1);
            }
            return "";
        }
        private static string StatsName(string raw)
        {
            return raw=="-1"?"Age":raw=="0"?"All Stats":"";
        }
        private static string CreatorPage(string context,string detail)
        {
            if(context=="cu_creator_page_checkpoint")
                return DetailValue(detail,"page");
            // Existing older reports lack explicit page checkpoints.
            if(context=="cu_stats_page_refresh_enter")
                return StatsName(DetailValue(detail,"creator_page"));
            if(context=="cu_stats_page_rows_built")
                return "All Stats";
            if(context=="cu_native_stats_step_attached")
            {
                string entry=DetailValue(detail,"entry_page");
                return entry=="Age"?"Age":entry=="Traits"?"All Stats":"";
            }
            if(context=="cu_native_face_layers_verified")return "Face";
            if(context=="cu_native_culture_composer_attached")return "Culture";
            if(context=="cu_guided_house_attached")return "House and Fortune";
            if(context=="sandbox_creator_page_clicked")
                return DetailValue(detail,"page");
            return "";
        }
        private static void AppendCreatorPage(StringBuilder b,
            List<string[]> events)
        {
            b.AppendLine();
            b.AppendLine("LAST RECORDED CHARACTER-CREATOR PAGE");
            int lastIndex=-1;
            string lastPage="";
            for(int i=0;i<events.Count;i++)
            {
                string candidate=CreatorPage(Cell(events[i],2),
                    Cell(events[i],3));
                if(!String.IsNullOrWhiteSpace(candidate))
                {
                    lastPage=candidate;
                    lastIndex=i;
                }
            }
            if(lastIndex<0)b.AppendLine("Not recorded in this session.");
            else
            {
                var row=events[lastIndex];
                b.AppendLine("Page: "+Clean(lastPage,120));
                b.AppendLine("Last checkpoint (UTC): "+Clean(Cell(row,0),80));
                foreach(var item in new[]{
                    new[]{"phase","Checkpoint stage"},
                    new[]{"chapter","Chapter"},
                    new[]{"step","Step"},
                    new[]{"field","Question"},
                    new[]{"row_page","Stats row page"}})
                {
                    string value=DetailValue(Cell(row,3),item[0]);
                    if(value.Length>0)
                        b.AppendLine(item[1]+": "+Clean(value,160));
                }
            }
            for(int i=events.Count-1;i>lastIndex;i--)
            {
                var row=events[i];
                if(Cell(row,2)!="cu_stats_navigation")continue;
                string from=StatsName(DetailValue(Cell(row,3),"from"));
                string to=StatsName(DetailValue(Cell(row,3),"to"));
                if(from.Length>0 && to.Length>0)
                {
                    b.AppendLine("Later requested transition: "+from+
                        " -> "+to+" (destination not confirmed visible).");
                    break;
                }
            }
            b.AppendLine("A page checkpoint records mod activity, "+
                "not proof that the game rendered the entire page.");
        }
        private static void AppendErrorArea(StringBuilder b,
            List<string[]> exceptions,List<string[]> events,bool interrupted)
        {
            b.AppendLine();
            b.AppendLine("RECORDED ERROR AREA (NOT A CRASH CAUSE)");
            if(exceptions.Count==0)
            {
                b.AppendLine("No managed exception recorded.");
                b.AppendLine(interrupted?
                    "Interruption is unclassified. The last UI event "+
                    "alone cannot distinguish a UI fault from a campaign "+
                    "or native engine fault.":
                    "No managed exception evidence to classify.");
            }
            else
            {
                int ui=exceptions.Count(r=>
                    ErrorArea(Cell(r,2))=="Creator / UI");
                int campaign=exceptions.Count(r=>
                    ErrorArea(Cell(r,2))=="Campaign / family");
                int other=exceptions.Count-ui-campaign;
                b.AppendLine("Managed exception contexts: Creator / UI="+ui+
                    ", Campaign / family="+campaign+
                    ", Other / unknown="+other+".");
                var last=exceptions[exceptions.Count-1];
                b.AppendLine("Most recent exception area: "+
                    ErrorArea(Cell(last,2))+
                    " (context="+Clean(Cell(last,2),160)+").");
                if(ui>0 && campaign>0)
                    b.AppendLine("Both UI and campaign exception contexts "+
                        "were logged in this session. Neither is a "+
                        "confirmed cause of shutdown.");
                else b.AppendLine("Area determined from CU exception "+
                    "context, not an engine stack or verified root cause.");
            }
            var eventFailures=events.Where(r=>
                Cell(r,2).EndsWith("_failed",StringComparison.Ordinal) &&
                ErrorArea(Cell(r,2))!="Other / unknown").TakeLastCompat(4);
            if(eventFailures.Count>0)
            {
                b.AppendLine("Failure-named event contexts (may have "+
                    "been handled or recovered):");
                foreach(var row in eventFailures)
                    b.AppendLine("- "+ErrorArea(Cell(row,2))+": "+
                        Clean(Cell(row,2),140));
            }
        }
        private static bool IsLifecycle(string context)
        {
            return context=="session_capture_started" ||
                context=="session_exit" ||
                context=="session_heartbeat" ||
                context=="module_loaded" ||
                context=="game_start" ||
                context=="campaign_closed" ||
                context=="exception_detected";
        }
        public static string Build(string directory,string status,
            bool interrupted)
        {
            var events=RecentRows(directory,"events.csv",256)
                .Where(r=>r.Length>=4).ToList();
            var exceptions=RecentRows(directory,"exceptions.csv",32)
                .Where(r=>r.Length>=5).ToList();
            var b=new StringBuilder();
            b.AppendLine("CALRADIA UNIFIED - READ ME FIRST");
            b.AppendLine("===============================");
            b.AppendLine("Session: "+Path.GetFileName(directory));
            b.AppendLine("Report made (UTC): "+DateTime.UtcNow.ToString("o",
                CultureInfo.InvariantCulture));
            b.AppendLine("Result: "+(interrupted?
                "INTERRUPTED - prior session recovered on the next launch":
                "CLOSED - normal shutdown callback completed"));
            b.AppendLine("Report status: "+Clean(status,120));
            b.AppendLine();
            b.AppendLine("WHAT THE REPORT CAN ESTABLISH");
            if(interrupted)
                b.AppendLine("A previous session had no completed exit marker. "+
                    "Possible explanations include a game crash, forced close "+
                    "or incomplete shutdown. A native crash cause is NOT "+
                    "confirmed by this marker.");
            else b.AppendLine("The module's normal shutdown callback ran. "+
                "This does not prove the entire play session had no errors.");
            b.AppendLine();
            b.AppendLine("LAST RECORDED GAME ACTIVITY");
            var meaningful=events.LastOrDefault(r=>
                !IsLifecycle(Cell(r,2)));
            if(meaningful==null)
                b.AppendLine("No gameplay-specific event was recorded.");
            else
            {
                b.AppendLine("UTC: "+Clean(Cell(meaningful,0),80));
                b.AppendLine("Event: "+Clean(Cell(meaningful,2),140));
                b.AppendLine("Details: "+Clean(Cell(meaningful,3),350));
            }
            b.AppendLine();
            AppendCreatorPage(b,events);
            AppendErrorArea(b,exceptions,events,interrupted);
            b.AppendLine();
            b.AppendLine("MANAGED EXCEPTION EVIDENCE");
            if(exceptions.Count==0)
                b.AppendLine("No managed exception recorded. A native "+
                    "process exit can occur without a managed exception.");
            else
            {
                var last=exceptions[exceptions.Count-1];
                b.AppendLine("Last recorded at UTC: "+
                    Clean(Cell(last,0),80));
                b.AppendLine("Context: "+Clean(Cell(last,2),140));
                b.AppendLine("Type: "+Clean(Cell(last,3),160));
                b.AppendLine("Message: "+Clean(Cell(last,4),450));
                b.AppendLine("This is recorded evidence, not proof that "+
                    "this exception caused the session to end.");
                b.AppendLine("Full managed stack: exceptions.csv");
            }
            b.AppendLine();
            b.AppendLine("RECENT EVENTS (MOST RECENT LAST)");
            var recent=events.Where(r=>!IsLifecycle(Cell(r,2)))
                .TakeLastCompat(5);
            if(recent.Count==0)b.AppendLine("None recorded.");
            foreach(var row in recent)
                b.AppendLine("- "+Clean(Cell(row,0),40)+" | "+
                    Clean(Cell(row,2),90)+" | "+
                    Clean(Cell(row,3),180));
            b.AppendLine();
            b.AppendLine("NEXT DIAGNOSTIC STEP");
            if(interrupted)
                b.AppendLine("Check events.csv and any native-rgl-tail-*.log "+
                    "for the last recorded transition. A native call stack "+
                    "or Windows crash evidence is needed to prove the "+
                    "faulting method.");
            else
                b.AppendLine("Review any exception above and compare the "+
                    "reported activity with what happened in game.");
            b.AppendLine("The report does not include saves, screenshots "+
                "or memory dumps.");
            b.AppendLine("Names and error messages can contain personal "+
                "details. Review before sharing.");
            return b.ToString();
        }
        private static List<T> TakeLastCompat<T>(
            this IEnumerable<T> source,int n)
        {
            var recent=new List<T>();
            foreach(var item in source)
            {
                recent.Add(item);
                if(recent.Count>n)recent.RemoveAt(0);
            }
            return recent;
        }
    }
}
