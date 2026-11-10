using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Ghaforv3.Helpers
{
    // ساخت فایل xlsx بدون هیچ کتابخانهٔ خارجی (فقط ZIP + XML).
    // متن‌ها به‌صورت inlineStr نوشته می‌شوند؛ پس هیچ سلولی به‌عنوان «فرمول» اجرا نمی‌شود (جلوگیری از formula injection).
    public static class XlsxWriter
    {
        public const string Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static string Col(int i)
        {
            var s = "";
            i++;
            while (i > 0) { int r = (i - 1) % 26; s = (char)('A' + r) + s; i = (i - 1) / 26; }
            return s;
        }

        private static string Clean(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
                if (ch == '\t' || ch == '\n' || ch == '\r' || (ch >= ' ' && ch != '\uFFFE' && ch != '\uFFFF')) sb.Append(ch);
            return SecurityElement.Escape(sb.ToString());
        }

        private static bool IsNumber(object v)
        {
            return v is double || v is decimal || v is int || v is long || v is float || v is short || v is byte;
        }

        private static void Add(ZipArchive zip, string name, string content)
        {
            var e = zip.CreateEntry(name, CompressionLevel.Fastest);
            using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(content);
        }

        // سطر اول عنوان ستون‌هاست (پررنگ و ثابت). مقدار null یا خالی نوشته نمی‌شود.
        public static byte[] Write(string sheetName, IList<object[]> rows, double[] widths)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView rightToLeft=\"1\" workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetFormatPr defaultRowHeight=\"15\"/>");
            sb.Append("<cols>");
            for (int i = 0; i < widths.Length; i++)
                sb.Append("<col min=\"" + (i + 1) + "\" max=\"" + (i + 1) + "\" width=\"" + widths[i].ToString(CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>");
            sb.Append("</cols><sheetData>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("<row r=\"" + (r + 1) + "\">");
                var row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    var v = row[c];
                    if (v == null) continue;
                    var sv = Convert.ToString(v, CultureInfo.InvariantCulture);
                    if (sv.Length == 0) continue;
                    var refName = Col(c) + (r + 1);
                    if (IsNumber(v))
                        sb.Append("<c r=\"" + refName + "\" s=\"2\"><v>" + sv + "</v></c>");
                    else
                    {
                        if (sv.Length > 32000) sv = sv.Substring(0, 32000);     // سقف طول سلول در اکسل
                        sb.Append("<c r=\"" + refName + "\" t=\"inlineStr\"" + (r == 0 ? " s=\"1\"" : "") + "><is><t xml:space=\"preserve\">" + Clean(sv) + "</t></is></c>");
                    }
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData></worksheet>");

            var safeName = Clean(sheetName ?? "Sheet1");
            if (safeName.Length > 31) safeName = safeName.Substring(0, 31);

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    Add(zip, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                    Add(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    Add(zip, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"{0}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>".Replace("{0}", safeName));
                    Add(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                    Add(zip, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><color rgb=\"FF14213D\"/><name val=\"Calibri\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFE7B3\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/><xf numFmtId=\"3\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
                    Add(zip, "xl/worksheets/sheet1.xml", sb.ToString());
                }
                return ms.ToArray();
            }
        }
    }
}
