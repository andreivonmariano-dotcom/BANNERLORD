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
        private static bool IsLifecycle(string context)
        {
            return context=="session_capture_started" ||
                context=="session_exit" ||
                context=="session_heartbeat" ||
                context=="module_loaded" ||
                context=="game_start" ||
                context=="campaign_closed";
        }
        public static string Build(string directory,string status,
            bool interrupted)
        {
            var events=RecentRows(directory,"events.csv",32)
                .Where(r=>r.Length>=4).ToList();
            var exceptions=RecentRows(directory,"exceptions.csv",8)
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
