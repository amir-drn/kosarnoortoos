using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ghaforv3.Helpers
{
    // CSV ساده (UTF-8). جداکنندهٔ , یا ; یا Tab خودکار تشخیص داده می‌شود.
    public static class CsvReader
    {
        public static List<string[]> Read(Stream input, int maxRows)
        {
            string text;
            using (var sr = new StreamReader(input, new UTF8Encoding(false), true)) text = sr.ReadToEnd();
            if (text.Length > 6000000) throw new InvalidDataException("فایل بیش از حد بزرگ است");

            int end = text.IndexOf('\n');
            var first = end < 0 ? text : text.Substring(0, end);
            int commas = first.Count(x => x == ','), semis = first.Count(x => x == ';'), tabs = first.Count(x => x == '\t');
            char delim = (tabs > commas && tabs > semis) ? '\t' : (semis > commas ? ';' : ',');

            var rows = new List<string[]>();
            var row = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                        else quoted = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == delim) { row.Add(sb.ToString()); sb.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n')
                {
                    row.Add(sb.ToString()); sb.Clear();
                    rows.Add(row.ToArray()); row.Clear();
                    if (rows.Count > maxRows + 10) break;
                }
                else sb.Append(c);
            }
            if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row.ToArray()); }
            return rows;
        }
    }
}
