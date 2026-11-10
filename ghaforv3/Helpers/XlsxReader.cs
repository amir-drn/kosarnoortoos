using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Ghaforv3.Helpers
{
    // خواندن اولین شیت فایل xlsx بدون کتابخانهٔ خارجی. فرمت قدیمی xls پشتیبانی نمی‌شود.
    public static class XlsxReader
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const long MaxEntryBytes = 40L * 1024 * 1024;     // سقف حجم هر بخش پس از باز شدن (جلوگیری از zip bomb)

        // XML را بدون DTD و بدون منابع خارجی می‌خواند (جلوگیری از XXE)
        private static XDocument Load(ZipArchiveEntry e)
        {
            if (e.Length > MaxEntryBytes) throw new InvalidDataException("فایل بیش از حد بزرگ است");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var s = e.Open())
            using (var r = XmlReader.Create(s, settings)) return XDocument.Load(r);
        }

        private static List<string> ReadShared(ZipArchive zip)
        {
            var list = new List<string>();
            var e = zip.GetEntry("xl/sharedStrings.xml");
            if (e == null) return list;
            foreach (var si in Load(e).Root.Elements(Main + "si"))
                list.Add(string.Concat(si.Descendants(Main + "t").Where(t => t.Parent != null && t.Parent.Name != Main + "rPh").Select(t => t.Value)));
            return list;
        }

        private static string FirstSheetPath(ZipArchive zip)
        {
            var wb = zip.GetEntry("xl/workbook.xml");
            var rels = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (wb == null || rels == null) return null;
            var sheet = Load(wb).Root.Descendants(Main + "sheet").FirstOrDefault();
            if (sheet == null) return null;
            var rid = (string)sheet.Attribute(Rel + "id");
            var rel = Load(rels).Root.Elements(PkgRel + "Relationship").FirstOrDefault(x => (string)x.Attribute("Id") == rid);
            if (rel == null) return null;
            var target = (string)rel.Attribute("Target");
            if (string.IsNullOrEmpty(target)) return null;
            return target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
        }

        private static int ColIndex(string cellRef)
        {
            int n = 0;
            foreach (var ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z') n = n * 26 + (ch - 'A' + 1);
                else if (ch >= 'a' && ch <= 'z') n = n * 26 + (ch - 'a' + 1);
                else break;
            }
            return n - 1;
        }

        public static List<string[]> ReadFirstSheet(Stream input, int maxRows)
        {
            using (var zip = new ZipArchive(input, ZipArchiveMode.Read, true))
            {
                var shared = ReadShared(zip);
                var entry = zip.GetEntry(FirstSheetPath(zip) ?? "xl/worksheets/sheet1.xml");
                if (entry == null) throw new InvalidDataException("شیت پیدا نشد");

                var rows = new List<string[]>();
                foreach (var row in Load(entry).Descendants(Main + "row"))
                {
                    if (rows.Count > maxRows + 10) break;
                    var cells = new Dictionary<int, string>();
                    int next = 0, max = -1;
                    foreach (var c in row.Elements(Main + "c"))
                    {
                        var r = (string)c.Attribute("r");
                        int idx = string.IsNullOrEmpty(r) ? next : ColIndex(r);
                        next = idx + 1;
                        if (idx < 0 || idx > 200) continue;
                        var t = (string)c.Attribute("t");
                        string val;
                        if (t == "inlineStr") val = string.Concat(c.Descendants(Main + "t").Select(x => x.Value));
                        else
                        {
                            var v = (string)c.Element(Main + "v");
                            if (v == null) val = null;
                            else if (t == "s") { int si; val = int.TryParse(v, out si) && si >= 0 && si < shared.Count ? shared[si] : null; }
                            else if (t == "b") val = v == "1" ? "1" : "0";
                            else val = v;
                        }
                        cells[idx] = val;
                        if (idx > max) max = idx;
                    }
                    // شمارهٔ سطر واقعی اکسل را حفظ می‌کنیم تا ردیف‌های خالی وسط جدول جابه‌جا نشوند
                    int rowNum;
                    var rowAttr = (string)row.Attribute("r");
                    if (int.TryParse(rowAttr, out rowNum) && rowNum > 0)
                        while (rows.Count < rowNum - 1 && rows.Count <= maxRows + 10) rows.Add(new string[0]);
                    var arr = new string[max + 1];
                    foreach (var kv in cells) arr[kv.Key] = kv.Value;
                    rows.Add(arr);
                }
                return rows;
            }
        }
    }
}
